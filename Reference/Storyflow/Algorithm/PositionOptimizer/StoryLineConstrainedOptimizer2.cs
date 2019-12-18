using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;

/// <summary>
/// Summary description for storylineconstrainedoptimizer2
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    class ShortLineConstrainedOptimizer2 : IPositionOptimizer
    {
        public static double LineStraightWeight { get; set; }
        public static int MaxIterationCount { get; set; }
        //public static int MaxInnerIteration { get; set; }
        public static double ConvergentShift { get; set; }
        public static int LineStraightFunction { get; set; }

        static ShortLineConstrainedOptimizer2()
        {
            LineStraightWeight = 1;
            MaxIterationCount = 1000;
            LineStraightFunction = 1;
        }

        public void Optimize(Story story, PositionTable<double> position)
        {
            int count = 0;
            List<double> list = new List<double>();
            int[,] index = new int[story.Characters.Count, story.FrameCount];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        index[i, frame] = count++;
                        list.Add(position[i, frame]);
                    }
                }
            }

            double[] X = list.ToArray<double>();

            int[,] Q = new int[X.Length, X.Length];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount - 1; ++frame)
                {
                    int left = frame;
                    int right = frame + 1;
                    if (story.SessionTable[i, left] != -1 && story.SessionTable[i, right] != -1)
                    {
                        Q[index[i, left], index[i, left]] += 2;
                        Q[index[i, right], index[i, right]] += 2;
                        Q[index[i, left], index[i, right]] -= 2;
                        Q[index[i, right], index[i, left]] -= 2;
                    }
                }
            }
            List<int> li = new List<int>();
            List<int> lj = new List<int>();
            List<double> lv = new List<double>();
            for (int i = 0; i < X.Length; ++i)
                for (int j = 0; j <= i; ++j)
                    if (Q[i, j] != 0)
                    {
                        li.Add(i);
                        lj.Add(j);
                        lv.Add((double)Q[i, j]);
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
                        listInner.Add(new Tuple<int, int>(index[x, frame], index[y, frame]));
                    }
                    else
                    {
                        listOuter.Add(new Tuple<int, int>(index[x, frame], index[y, frame]));
                    }
                }
            }


            const double infinity = 0;
            int NumConstraint = listInner.Count + listOuter.Count;
            int NumVariable = X.Length;


            double[] blx = new double[NumVariable];
            double[] bux = new double[NumVariable];
            mosek.boundkey[] bkx = new mosek.boundkey[NumVariable];

            for (int i = 0; i < NumVariable; ++i)
                bkx[i] = mosek.boundkey.fr;


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
                blc[i] = 28;
                buc[i] = 28;
            }
            for (int i = listInner.Count; i < listInner.Count + listOuter.Count; ++i)
            {
                bkc[i] = mosek.boundkey.lo;
                blc[i] = 84;
                buc[i] = infinity;
            }

            mosek.Task task = null;
            mosek.Env env = null;
            double[] xx = new double[NumVariable];
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

                task.optimize();

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

        private Tuple<double, double> StraightEnergy(double x, double y)
        {
            double f = Math.Pow(x - y, 2.0);
            double g = 2.0 * (x - y);
            return new Tuple<double, double>(f, g);
        }

        private void EnergyGrad(double[] x, ref double func, double[] grad, object obj)
        {
            Tuple<Story, PositionTable<double>, int[,]> tuple = obj as Tuple<Story, PositionTable<double>, int[,]>;
            Story story = tuple.Item1;
            PositionTable<double> position = tuple.Item2;
            int[,] dict = tuple.Item3;


            // calculate func
            func = 0;
            Tuple<double, double, double> tupleInnerDistance;
            Tuple<double, double, double> tupleOuterDistance;
            Tuple<double, double, double> tupleLineStraight;

            // calculate grad
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        int index = dict[i, frame];
                        grad[index] = 0;


                        if (LineStraightFunction == 1 && frame > 0 && frame < story.FrameCount - 1 && story.SessionTable[i, frame - 1] != -1 && story.SessionTable[i, frame + 1] != -1)
                        {
                            var t = StraightEnergy(x[index], x[dict[i, frame - 1]], x[dict[i, frame + 1]]);
                            func += LineStraightWeight * t.Item1;
                            grad[index] += LineStraightWeight * t.Item2;
                        }
                        else
                        {
                            if (frame > 0 && story.SessionTable[i, frame - 1] != -1)
                            {
                                var t = StraightEnergy(x[index], x[dict[i, frame - 1]]);
                                func += LineStraightWeight * t.Item1;
                                grad[index] += LineStraightWeight * t.Item2;
                            }
                            else if (frame < story.FrameCount - 1 && story.SessionTable[i, frame + 1] != -1)
                            {
                                var t = StraightEnergy(x[index], x[dict[i, frame + 1]]);
                                func += LineStraightWeight * t.Item1;
                                grad[index] += LineStraightWeight * t.Item2;
                            }
                            else
                            { }
                        }
                    }
                }
            }

        }

        private Tuple<double, double> StraightEnergy(double x, double y, double z)
        {
            double min = Math.Min(y, z);
            double max = Math.Max(y, z);

            double f = Math.Pow((x - y) * (z - x), 2.0);
            //double g = -2.0 * x + (z - y);
            double g = 4.0 * Math.Pow(x, 3.0) - (6 * y + 6 * z) * x * x + (2 * y * y + 8 * y * z + 2 * z * z) * x - 2 * y * z * z - 2 * y * y * z;
            return new Tuple<double, double>(f, g);
        }
    }
}