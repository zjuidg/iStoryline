using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;
using mosek;
using Nancy.Extensions;
using Storyline;
using StackExchange.Redis;
using StorylineBackend.upload;

/// <summary>
/// Summary description for positionoptimizer
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    public class PersistentOptimizer: IOptimizer
    {
        private StorylineApp _app;
        private Story story;
        // order in a single timeframe
        private PositionTable<int> perm;
        private PositionTable<int> segment;

        private mosek.Task task;
        private mosek.Env env;

        private int[,] index;
        private int xCount;
        List<Tuple<int, int>> innerList = new List<Tuple<int, int>>();
        List<Tuple<int, int>> outerList = new List<Tuple<int, int>>();

        public PersistentOptimizer(StorylineApp app, Story story, PositionTable<int> perm, PositionTable<int> segment, double innerDist, double outerDist)
        {
            // restore data structures
            this._app = app;
            this.story = story;
            this.perm = perm;
            this.segment = segment;

            // initialize
            // index for Q at character i, timeframe j 
            index = new int[story.Characters.Count, story.FrameCount];
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int frame = 0; frame < story.FrameCount; ++frame)
                    index[i, frame] = -1; // invalid value

            //
            List<double> X = new List<double>();
            int count = -1;
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        if (frame > 0 && story.SessionTable[i, frame - 1] != -1 && segment[i, frame] == segment[i, frame - 1])
                        {
                            index[i, frame] = count;
                        }
                        else
                        {
                            index[i, frame] = ++count;
                            X.Add(perm[i, frame]); // assign perm to X
                        }
                    }
                }
            }
            int NumVariable = X.Count;
            xCount = X.Count;

            // calculate sparse objective matrix Q
            #region Matrix
            Dictionary<Tuple<int, int>, double> matrix = new Dictionary<Tuple<int, int>, double>();
            for (int i = 0; i < X.Count; ++i)
            {
                matrix.Add(new Tuple<int, int>(i, i), 0.1);//simon, ctk
            }
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount - 1; ++frame)
                {
                    int left = frame;
                    int right = frame + 1;
                    var leftSession = story.SessionTable[i, left];
                    var rightSession = story.SessionTable[i, right];

                    var needBreak = false;
                    foreach (var sessionBreak in _app.Status.Config.SessionBreaks)
                    {
                        if (sessionBreak.session1 == leftSession && sessionBreak.session2 == rightSession
                            && sessionBreak.frame == left)
                        {
                            needBreak = true;
                            break;
                        }
                    }
                    
                    if (leftSession != -1 && rightSession != -1 && segment[i, left] != segment[i, right] && !needBreak)
                    {
                        Tuple<int, int> ll = new Tuple<int, int>(index[i, left], index[i, left]);
                        Tuple<int, int> lr = new Tuple<int, int>(index[i, left], index[i, right]);
                        Tuple<int, int> rl = new Tuple<int, int>(index[i, right], index[i, left]);
                        Tuple<int, int> rr = new Tuple<int, int>(index[i, right], index[i, right]);
                        if (!matrix.ContainsKey(ll))
                            matrix.Add(ll, 0);
                        if (!matrix.ContainsKey(lr))
                            matrix.Add(lr, 0);
                        if (!matrix.ContainsKey(rl))
                            matrix.Add(rl, 0);
                        if (!matrix.ContainsKey(rr))
                            matrix.Add(rr, 0);
                        matrix[ll] += 2;
                        matrix[rr] += 2;
                        matrix[lr] -= 2;
                        matrix[rl] -= 2;
                    }
                }
            }

            // sparse representation to matrix Q
            List<int> li = new List<int>();
            List<int> lj = new List<int>();
            List<double> lv = new List<double>();
            foreach (KeyValuePair<Tuple<int, int>, double> pair in matrix)
            {
                if (pair.Key.Item1 >= pair.Key.Item2 && Math.Abs(pair.Value) > 0.000001)
                {
                    li.Add(pair.Key.Item1);
                    lj.Add(pair.Key.Item2);
                    lv.Add(pair.Value);
                }
            }
            // input must be array instead of list
            int[] qsubi = li.ToArray();
            int[] qsubj = lj.ToArray();
            double[] qval = lv.ToArray();
            #endregion

            // calculate inner and outer constraints
            #region Constraints
            // constraint { <index of k, index of k+1>: bundle}
            Dictionary<Tuple<int, int>, int> constraints = new Dictionary<Tuple<int, int>, int>();
            
            List<int>[] asubList = new List<int>[NumVariable];
            List<double>[] avalList = new List<double>[NumVariable];
            for (int i = 0; i < NumVariable; ++i)
            {
                asubList[i] = new List<int>();
                avalList[i] = new List<double>();
            }
            
            List<mosek.boundkey> bkcList = new List<boundkey>();
            List<double> blcList = new List<double>();
            List<double> bucList = new List<double>();

            var sessionInnerGaps = _app.Status.Config.sessionInnerGaps;
            var sessionOuterGaps = _app.Status.Config.sessionOuterGaps;
            // for each time frame
            int constraintCounter = 0;
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                // charaters at timeframe
                List<Tuple<int, int>> l = new List<Tuple<int, int>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        l.Add(new Tuple<int, int>(i, perm[i, frame]));
                    }
                }
                // get character order in current frame
                // apply result in location tree sort
                l.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                for (int k = 0; k < l.Count - 1; ++k)
                {
                    int x = l[k].Item1;
                    int y = l[k + 1].Item1;
                        // inner constraints
                        // x is upper character
                    var indexX = index[x, frame];
                    // y is lower character
                    var indexY = index[y, frame];
                    var sessionX = story.SessionTable[x, frame];
                    var sessionY = story.SessionTable[y, frame];
                    if (sessionX == sessionY)
                    {
                        // lower character index and higher character index
                        Tuple<int, int> tuple = new Tuple<int, int>(indexX, indexY);
                        if (constraints.ContainsKey(tuple))
                        {
                            var i = constraints[tuple];
                            Debug.Assert(i >= 0);
                            // change default gaps with respect to sessionInnerGaps
                            buildInnerGapConstraints(sessionInnerGaps, sessionX, blcList, i, bucList);
                        }
                        else
                        {
                            int i = constraintCounter++;
                            // type ra for range, fx for fixed
                            bkcList.Add(mosek.boundkey.ra);
                            // add contraint of innergap +- 2
                            // lower 
                            blcList.Add(Math.Max(innerDist - 2, 2));
                            // upper
                            bucList.Add(innerDist + 2);
                            buildInnerGapConstraints(sessionInnerGaps, sessionX, blcList, i, bucList);

                            // Row index of non - zeros in column i
                            // sparse array for i-th constraint
                            asubList[indexX].Add(i);
                            // Non - zero Values of column i.
                            avalList[indexX].Add(-1);
                            asubList[indexY].Add(i);
                            avalList[indexY].Add(1);
                            // positive for inner gap
                            // store the index for further modification
                            // assigned in sessionInnerGaps
                            constraints.Add(tuple, i);
                        }
                    }
                    else
                    {
                        Tuple<int, int> tuple = new Tuple<int, int>(indexX, indexY);
                        if (constraints.ContainsKey(tuple))
                        {
                            var j = constraints[tuple];
                            Debug.Assert(j <= 0);
                            j *= -1;
                            // change default gaps with respect to sessionOuterGaps
                            buildOuterGapConstraints(sessionOuterGaps, sessionX, sessionY, blcList, j, bucList, bkcList);
                        }
                        else
                        {
                            int j = constraintCounter++;
                            // default setting 
                            bkcList.Add(mosek.boundkey.lo);
                            blcList.Add(outerDist);
                            bucList.Add(1000);
                            buildOuterGapConstraints(sessionOuterGaps, sessionX, sessionY, blcList, j, bucList, bkcList);
                            asubList[indexX].Add(j);
                            avalList[indexX].Add(-1);
                            asubList[indexY].Add(j);
                            avalList[indexY].Add(1);
                            // negative for outer gap
                            // store the index for further modification
                            // assigned in sessionInnerGaps
                            constraints.Add(tuple, -j);
                        }
                    }
                }
            }

            foreach (KeyValuePair<Tuple<int, int>, int> pair in constraints)
            {
                if (pair.Value >= 0)
                {
                    innerList.Add(pair.Key);
                }
                else
                {
                    outerList.Add(pair.Key);
                }
            }
            int NumConstraint = innerList.Count + outerList.Count;

            // to array
            int[][] asub = new int[NumVariable][];
            double[][] aval = new double[NumVariable][];
  
            for (int i = 0; i < NumVariable; ++i)
            {
                asub[i] = asubList[i].ToArray();
                aval[i] = avalList[i].ToArray();
            }

            mosek.boundkey[] bkc = bkcList.ToArray();
            double[] blc = blcList.ToArray();
            double[] buc = bucList.ToArray();
            Debug.Assert(constraintCounter == NumConstraint);
            #endregion

            // calculate variable bound
            double[] blx = new double[NumVariable];
            double[] bux = new double[NumVariable];
            mosek.boundkey[] bkx = new mosek.boundkey[NumVariable];
            for (int i = 0; i < NumVariable; ++i)
            {
                bkx[i] = mosek.boundkey.ra;
                blx[i] = -12000;
                bux[i] = 12000;
            }
            
            // addCharacterYConstraints in a +- 10 range
            foreach (var yConstraint in _app.Status.Config.CharacterYConstraints)
            {
                if (yConstraint.frame < 0 || yConstraint.frame > story.FrameCount)
                {
                    continue;
                }
                var i = index[yConstraint.characterId, yConstraint.frame];
                if (i != -1)
                {
                    blx[i] = yConstraint.lowerY;
                    bux[i] = yConstraint.upperY;
                }
            }
            
            // setup mosek
            #region Mosek
            try
            {
                env = new mosek.Env();
                env.init();

                task = new mosek.Task(env, 0, 0);
                task.putmaxnumvar(NumVariable);
                task.putmaxnumcon(NumConstraint);

                task.append(mosek.accmode.con, NumConstraint);
                task.append(mosek.accmode.var, NumVariable);

                task.putcfix(0.0);
                
                for (int j = 0; j < NumVariable; ++j)
                {
                    task.putcj(j, 0);
                    task.putbound(mosek.accmode.var, j, bkx[j], blx[j], bux[j]);
                    task.putavec(mosek.accmode.var, j, asub[j], aval[j]);
                }
                
                for (int i = 0; i < NumConstraint; ++i)
                {
                    task.putbound(mosek.accmode.con, i, bkc[i], blc[i], buc[i]);
                }

                task.putobjsense(mosek.objsense.minimize);
                task.putqobj(qsubi, qsubj, qval);
            }
            catch (mosek.Exception e)
            {
                Console.WriteLine(e.Code);
                Console.WriteLine(e);
            }
            #endregion
        }

        private static void buildInnerGapConstraints(List<Pair<int, double>> sessionInnerGaps, int sessionX, List<double> blcList, int i, List<double> bucList)
        {
            foreach (var sessionInnerGap in sessionInnerGaps)
            {
                if (sessionInnerGap.Item1 == sessionX)
                {
                    // reset inner gap according to requirement
                    blcList[i] = Math.Max(sessionInnerGap.Item2 - 2, 2);
                    bucList[i] = sessionInnerGap.Item2 + 2;
                    break;
                }
            }
        }

        private static void buildOuterGapConstraints(List<Pair<Pair<int, int>, Pair<int, int>>> sessionOuterGaps, int sessionX, int sessionY, List<double> blcList, int j,
            List<double> bucList, List<boundkey> bkcList)
        {
            foreach (var sessionOuterGap in sessionOuterGaps)
            {
                if (sessionOuterGap.Item1.toTuple().Equals(new Tuple<int, int>(sessionX, sessionY)) ||
                    sessionOuterGap.Item1.toTuple().Equals(new Tuple<int, int>(sessionY, sessionX)))
                {
                    var min = sessionOuterGap.Item2.Item1;
                    var max = sessionOuterGap.Item2.Item2;

                    if (min == max && min == -1)
                    {
                        Console.WriteLine("min and max are both infinity, skipping");
                        break;
                    }

                    if (min != -1)
                    {
                        blcList[j] = min;
                    }

                    if (max != -1)
                    {
                        bucList[j] = max;
                    }

                    if (min == -1)
                    {
                        // no lower bound
                        bkcList[j] = mosek.boundkey.up;
                    }
                    else if (max == -1)
                    {
                        // no upper bound
                        bkcList[j] = mosek.boundkey.lo;
                    }
                    else
                    {
                        bkcList[j] = mosek.boundkey.ra;
                    }

                    break;
                }
            }
        }

        public void SetReferenceMode(PositionTable<double> reference)
        {
            double[] x = new double[xCount];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        x[index[i, frame]] = reference[i, frame];
                    }
                }
            }
            int[] qsubi = new int[xCount];
            int[] qsubj = new int[xCount];
            double[] qval = new double[xCount];
            for (int j = 0; j < xCount; ++j)
            {
                qsubi[j] = j;
                qsubj[j] = j;
                qval[j] = 2;
                task.putcj(j, -2 * x[j]);
            }
            task.putqobj(qsubi, qsubj, qval);
        }

        public void ResetInnerDist(/*List<Tuple<int, int>> frame_session, */double innerDist)
        {
            // dummy for bundle
        }

        public PositionTable<double> Optimize()
        {
            DateTime start = DateTime.Now;
            double[] x = new double[xCount];
            try
            {
                task.optimize();
                task.solutionsummary(mosek.streamtype.msg);

                mosek.solsta solsta;
                mosek.prosta prosta;

                task.getsolutionstatus(mosek.soltype.itr,
                    out prosta,
                    out solsta);
                task.getsolutionslice(mosek.soltype.itr,
                    mosek.solitem.xx,
                    0,
                    xCount,
                    x);

                switch (solsta)
                {
                    case mosek.solsta.optimal:
                    case mosek.solsta.near_optimal:
                        Console.WriteLine("Optimal primal solution\n");
                        break;
                    case mosek.solsta.dual_infeas_cer:
                    case mosek.solsta.prim_infeas_cer:
                    case mosek.solsta.near_dual_infeas_cer:
                    case mosek.solsta.near_prim_infeas_cer:
                        Console.WriteLine("Primal or dual infeasibility.\n");
                        break;
                    case mosek.solsta.unknown:
                        Console.WriteLine("Unknown solution status.\n");
                        break;
                    default:
                        Console.WriteLine("Other solution status");
                        break;
                }
            }
            catch (mosek.Exception e)
            {
                Console.WriteLine(e.Code);
                Console.WriteLine(e);
            }

            Console.WriteLine("{0}", DateTime.Now - start);

            PositionTable<double> position = new PositionTable<double>(story.Characters.Count, story.FrameCount);
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        position[i, frame] = x[index[i, frame]];
                    }
                }
            }
            return position;
        }
    }
}