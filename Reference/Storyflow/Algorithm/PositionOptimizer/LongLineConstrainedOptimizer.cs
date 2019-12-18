using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;
using Storyline;

/// <summary>
/// Summary description for longlineconstrainedoptimizer
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    class LongLineConstrainedOptimizer : IPositionOptimizer
    {
        private StorylineApp _app;

        public LongLineConstrainedOptimizer(StorylineApp app)
        {
            _app = app;
        }

        public void Optimize(Story story, PositionTable<double> position)
        {

        }

        public void Optimize(Story story, PositionTable<double> position, PositionTable<int> segment)
        {
            int count = -1;
            List<double> X = new List<double>();
            int[,] index = new int[story.Characters.Count, story.FrameCount];
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int frame = 0; frame < story.FrameCount; ++frame)
                    index[i, frame] = -2;
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
                            X.Add(position[i, frame]);
                        }
                    }
                }
            }

            Dictionary<Tuple<int, int>, double> Q = new Dictionary<Tuple<int, int>, double>();

            for (int i = 0; i < X.Count; ++i)
            {
                Q.Add(new Tuple<int, int>(i, i), 1);
            }

            //int[,] Q = new int[X.Count, X.Count];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount - 1; ++frame)
                {
                    int left = frame;
                    int right = frame + 1;
                    if (story.SessionTable[i, left] != -1 && story.SessionTable[i, right] != -1 && segment[i, left] != segment[i, right])
                    {
                        if (!Q.ContainsKey(new Tuple<int, int>(index[i, left], index[i, left])))
                        {
                            Q.Add(new Tuple<int, int>(index[i, left], index[i, left]), 0);
                        }
                        if (!Q.ContainsKey(new Tuple<int, int>(index[i, right], index[i, right])))
                        {
                            Q.Add(new Tuple<int, int>(index[i, right], index[i, right]), 0);
                        }
                        if (!Q.ContainsKey(new Tuple<int, int>(index[i, left], index[i, right])))
                        {
                            Q.Add(new Tuple<int, int>(index[i, left], index[i, right]), 0);
                        }
                        if (!Q.ContainsKey(new Tuple<int, int>(index[i, right], index[i, left])))
                        {
                            Q.Add(new Tuple<int, int>(index[i, right], index[i, left]), 0);
                        }
                        Q[new Tuple<int, int>(index[i, left], index[i, left])] += 2;
                        Q[new Tuple<int, int>(index[i, right], index[i, right])] += 2;
                        Q[new Tuple<int, int>(index[i, left], index[i, right])] -= 2;
                        Q[new Tuple<int, int>(index[i, right], index[i, left])] -= 2;
                    }
                }
            }

            List<int> li = new List<int>();
            List<int> lj = new List<int>();
            List<double> lv = new List<double>();
            foreach (KeyValuePair<Tuple<int, int>, double> pair in Q)
            {
                if (pair.Key.Item1 >= pair.Key.Item2 && pair.Value != 0)
                {
                    li.Add(pair.Key.Item1);
                    lj.Add(pair.Key.Item2);
                    lv.Add((double)pair.Value);
                }
            }

            int[] qsubi = li.ToArray();
            int[] qsubj = lj.ToArray();
            double[] qval = lv.ToArray();


            List<Tuple<int, int>> listInner = new List<Tuple<int, int>>();
            List<Tuple<int, int>> listOuter = new List<Tuple<int, int>>();
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                List<Tuple<int, double>> l = new List<Tuple<int, double>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        l.Add(new Tuple<int, double>(i, position[i, frame]));
                    }
                }
                l.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                for (int k = 0; k < l.Count - 1; ++k)
                {
                    int x = l[k].Item1;
                    int y = l[k + 1].Item1;
                    if (story.SessionTable[x, frame] == story.SessionTable[y, frame])
                    {
                        if (!listInner.Contains(new Tuple<int, int>(index[x, frame], index[y, frame])))
                            listInner.Add(new Tuple<int, int>(index[x, frame], index[y, frame]));
                    }
                    else
                    {
                        if (!listOuter.Contains(new Tuple<int, int>(index[x, frame], index[y, frame])))
                            listOuter.Add(new Tuple<int, int>(index[x, frame], index[y, frame]));
                    }
                }
            }

            Debug.Assert(!ExistingCircle(listInner, X.Count));
            Debug.Assert(!ExistingCircle(listOuter, X.Count));

            foreach (Tuple<int, int> tuple in listInner)
            {
                Debug.Write(tuple.Item1.ToString() + "->" + tuple.Item2.ToString() + ", ");
            }

            const double infinity = 0;
            int NumConstraint = listInner.Count + listOuter.Count;
            int NumVariable = X.Count;


            double[] blx = new double[NumVariable];
            double[] bux = new double[NumVariable];
            mosek.boundkey[] bkx = new mosek.boundkey[NumVariable];

            for (int i = 0; i < NumVariable; ++i)
            {
                bkx[i] = mosek.boundkey.ra;
                blx[i] = -12000;
                bux[i] = 12000;
            }
            bkx[0] = mosek.boundkey.fx;
            bkx[0] = 0;
            bkx[0] = 0;


            int[][] asub = new int[NumVariable][];
            double[][] aval = new double[NumVariable][];


            List<int>[] asubList = new List<int>[NumVariable];
            List<double>[] avalList = new List<double>[NumVariable];
            for (int i = 0; i < NumVariable; ++i)
            {
                asubList[i] = new List<int>();
                avalList[i] = new List<double>();
            }
            for (int i = 0; i < listInner.Count; ++i)
            {
                Tuple<int, int> pair = listInner[i];
                int x = pair.Item1;
                int y = pair.Item2;

                asubList[x].Add(i);
                avalList[x].Add(-1);

                asubList[y].Add(i);
                avalList[y].Add(1);
            }
            for (int i = 0; i < listOuter.Count; ++i)
            {
                int j = i + listInner.Count;
                Tuple<int, int> pair = listOuter[i];
                int x = pair.Item1;
                int y = pair.Item2;
                asubList[x].Add(j);
                avalList[x].Add(-1);

                asubList[y].Add(j);
                avalList[y].Add(1);
            }
            for (int i = 0; i < NumVariable; ++i)
            {
                asub[i] = asubList[i].ToArray();
                aval[i] = avalList[i].ToArray();
            }
            mosek.boundkey[] bkc = new mosek.boundkey[NumConstraint];
            double[] blc = new double[NumConstraint];
            double[] buc = new double[NumConstraint];
            for (int i = 0; i < listInner.Count; ++i)
            {
                bkc[i] = mosek.boundkey.fx;
                //blc[i] = 28;
                //buc[i] = 28;
                blc[i] = _app.Status.Config.Style.DefaultInnerGap;
                buc[i] = _app.Status.Config.Style.DefaultInnerGap;
            }
            for (int i = listInner.Count; i < listInner.Count + listOuter.Count; ++i)
            {
                bkc[i] = mosek.boundkey.lo;
                //blc[i] = 84;
                blc[i] = _app.Status.Config.Style.OuterGap;
                buc[i] = 1000;
            }

            mosek.Task task = null;
            mosek.Env env = null;
            double[] xx = new double[NumVariable];
            DateTime start = DateTime.Now;
            try
            {
                env = new mosek.Env();
                env.set_Stream(mosek.streamtype.log, new msgclass(""));
                env.init();

                task = new mosek.Task(env, 0, 0);
                task.set_Stream(mosek.streamtype.log, new msgclass(""));
                task.putmaxnumvar(NumVariable);
                task.putmaxnumcon(NumConstraint);

                //task.putdouparam(mosek.dparam.intpnt_nl_tol_pfeas, 1.0e-1);
                //task.putdouparam(mosek.dparam.intpnt_tol_dfeas, 1.0e-1);
                //task.putdouparam(mosek.dparam.intpnt_nl_tol_rel_gap, 1.0e-1);
                //task.putdouparam(mosek.dparam.intpnt_co_tol_infeas, 1.0e-13);
                //task.putdouparam(mosek.dparam.intpnt_nl_tol_mu_red, 1.0e-13);


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
                    NumVariable,
                    xx);

                switch (solsta)
                {
                    case mosek.solsta.optimal:
                    case mosek.solsta.near_optimal:
                        Console.WriteLine("Optimal primal solution\n");
                        //for (int j = 0; j < NumVariable; ++j)
                        //    Console.WriteLine("x[{0}]:", xx[j]);
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
            finally
            {
                if (task != null) task.Dispose();
                if (env != null) env.Dispose();
            }

            Console.WriteLine("///{0}", DateTime.Now - start);

            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        position[i, frame] = xx[index[i, frame]];
                    }
                }
            }


        }

        static bool ExistingCircle(List<Tuple<int, int>> list, int N)
        {
            int[] dist = new int[N];
            for (int i = 0; i < N; ++i)
            {
                foreach (Tuple<int, int> tuple in list)
                {
                    int x = tuple.Item1;
                    int y = tuple.Item2;
                    if (dist[x] - 1 < dist[y])
                        dist[y] = dist[x] - 1;
                }
            }
            foreach (Tuple<int, int> tuple in list)
            {
                int x = tuple.Item1;
                int y = tuple.Item2;
                if (dist[x] - 1 < dist[y])
                    return true;
            }
            return false;
        }
    }

    class msgclass : mosek.Stream
    {
        string prefix;
        public msgclass(string prfx)
        {
            prefix = prfx;
        }

        public override void streamCB(string msg)
        {
            Console.Write("{0}{1}", prefix, msg);
        }
    }
}