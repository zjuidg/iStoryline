using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
using System.Text;
using mosek;

/// <summary>
/// Summary description for positionbasedbundleoptimizer
/// </summary>
/// 
namespace Algorithm.bundle
{
    class PositionBasedBundleOptimizer
    {
        private StorylineApp _app;

        public PositionBasedBundleOptimizer(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Optimize(Story story, PositionTable<double> position)
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
                        if (frame > 0 && story.SessionTable[i, frame - 1] != -1 && Math.Abs(position[i, frame] - position[i, frame - 1]) < 1e-5)
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
                Q.Add(new Tuple<int, int>(i, i), 2);
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

            const double infinity = 0;
            int NumConstraint = listInner.Count + listOuter.Count;
            int NumVariable = X.Count;


            double[] blx = new double[NumVariable];
            double[] bux = new double[NumVariable];
            mosek.boundkey[] bkx = new mosek.boundkey[NumVariable];

            for (int i = 0; i < NumVariable; ++i)
            {
                bkx[i] = mosek.boundkey.ra;
                blx[i] = -double.MaxValue;
                bux[i] = double.MaxValue;
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
                blc[i] = 0;
                buc[i] = 0;
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
            try
            {
                env = new mosek.Env();
                env.set_Stream(mosek.streamtype.log, new msgclass(""));
                env.init();

                task = new mosek.Task(env, 0, 0);
                task.set_Stream(mosek.streamtype.log, new msgclass(""));
                task.putmaxnumvar(NumVariable);
                task.putmaxnumcon(NumConstraint);


                task.append(mosek.accmode.con, NumConstraint);
                task.append(mosek.accmode.var, NumVariable);

                task.putcfix(0.0);

                for (int j = 0; j < NumVariable; ++j)
                {
                    task.putcj(j, -2 * X[j]);
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

            PositionTable<double> result = new PositionTable<double>(story.Characters.Count, story.FrameCount);
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        result[i, frame] = xx[index[i, frame]];
                    }
                }
            }

            return result;
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