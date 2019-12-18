using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
using System.Diagnostics;

/// <summary>
/// Summary description for shortlineconsrtainedoptimizer
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    class ShortLineConstrainedOptimizer : IPositionOptimizer
    {
        public static double LineStraightWeight { get; set; }
        public static int MaxIterationCount { get; set; }
        //public static int MaxInnerIteration { get; set; }
        public static double ConvergentShift { get; set; }
        public static int LineStraightFunction { get; set; }

        static ShortLineConstrainedOptimizer()
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
            double[,] C = new double[listInner.Count + listOuter.Count, X.Length + 1];
            int[] CT = new int[listInner.Count + listOuter.Count];

            for (int i = 0; i < listInner.Count; ++i)
            {
                Tuple<int, int> pair = listInner[i];
                int x = pair.Item1;
                int y = pair.Item2;
                C[i, x] = -1;
                C[i, y] = 1;
                C[i, X.Length] = 1.0;
                CT[i] = 0;
            }

            for (int i = 0; i < listOuter.Count; ++i)
            {
                int j = i + listInner.Count;
                Tuple<int, int> pair = listOuter[i];
                int x = pair.Item1;
                int y = pair.Item2;
                C[j, x] = -1;
                C[j, y] = 1;
                C[j, X.Length] = 5.0;
                CT[j] = 1;
            }



            // optimization

            alglib.minbleicstate state;
            alglib.minbleicreport rep;

            double epsg = 0.01;
            double epsf = 0;
            double epsx = 0;
            int maxits = 0;

            alglib.minbleiccreate(X, out state);
            alglib.minbleicsetlc(state, C, CT);
            alglib.minbleicsetcond(state, epsg, epsf, epsx, maxits);
            Tuple<Story, PositionTable<double>, int[,]> param = new Tuple<Story, PositionTable<double>, int[,]>(story, position, index);
            alglib.minbleicoptimize(state, EnergyGrad, null, param);
            alglib.minbleicresults(state, out X, out rep);

            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        position[i, frame] = X[index[i, frame]];
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