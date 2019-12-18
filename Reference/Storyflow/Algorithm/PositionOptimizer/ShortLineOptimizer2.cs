using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for shortlineoptimizer2
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    class ShortLineOptimizer2 : IPositionOptimizer
    {
        public static double InnerDistanceConstraintWeight { get; set; }
        public static double OutterDistanceConstraintWeight { get; set; }
        public static double LineStraightWeight { get; set; }
        public static int MaxIterationCount { get; set; }
        //public static int MaxInnerIteration { get; set; }
        public static double ConvergentShift { get; set; }
        public static int LineStraightFunction { get; set; }


        private Tuple<double, double> InnerEnergy(double x, double y, double mind, double maxd)
        {
            double c1 = y - maxd;
            double c2 = y - mind;
            double c3 = y;
            double c4 = y + mind;
            double c5 = y + maxd;

            if (x < c1)
            {
                double f = Math.Pow(x - c1, 2.0);
                double g = 2.0 * (x - c1);
                return new Tuple<double, double>(f, g);
            }
            else if (x < c2)
            {
                double f = 0;
                double g = 0;
                return new Tuple<double, double>(f, g);
            }
            else if (x < c3)
            {
                double f = Math.Pow(x - c2, 2.0);
                double g = 2.0 * (x - c2);
                return new Tuple<double, double>(f, g);
            }
            else if (x < c4)
            {
                double f = Math.Pow(x - c4, 2.0);
                double g = 2.0 * (x - c4);
                return new Tuple<double, double>(f, g);
            }
            else if (x < c5)
            {
                double f = 0;
                double g = 0;
                return new Tuple<double, double>(f, g);
            }
            else
            {
                double f = Math.Pow(x - c5, 2.0);
                double g = 2.0 * (x - c5);
                return new Tuple<double, double>(f, g);
            }
        }

        private Tuple<double, double> OuterEnergy(double x, double y, double d)
        {
            if (Math.Abs(x - y) > d)
            {
                return new Tuple<double, double>(0, 0);
            }
            else if (x < y)
            {
                double f = Math.Pow(x - y + d, 2.0);
                double g = 2.0 * (x - y + d);
                return new Tuple<double, double>(f, g);
            }
            else
            {
                double f = Math.Pow(x - y - d, 2.0);
                double g = 2.0 * (x - y - d);
                return new Tuple<double, double>(f, g);
            }
        }

        private Tuple<double, double> StraightEnergy(double x, double y)
        {
            double f = Math.Pow(x - y, 2.0);
            double g = 2.0 * (x - y);
            return new Tuple<double, double>(f, g);
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


        private void EnergyGrad(double[] x, ref double func, double[] grad, object obj)
        {
            Tuple<Story, PositionTable<double>, Dictionary<Tuple<int, int>, int>> tuple = obj as Tuple<Story, PositionTable<double>, Dictionary<Tuple<int, int>, int>>;
            Story story = tuple.Item1;
            PositionTable<double> position = tuple.Item2;
            Dictionary<Tuple<int, int>, int> dict = tuple.Item3;


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
                        int index = dict[new Tuple<int, int>(i, frame)];
                        grad[index] = 0;

                        for (int j = 0; j < story.Characters.Count; ++j)
                        {
                            if (i != j && story.SessionTable[j, frame] != -1)
                            {
                                if (story.SessionTable[i, frame] == story.SessionTable[j, frame])
                                {
                                    var t = InnerEnergy(x[index], x[dict[new Tuple<int, int>(j, frame)]], 1.0, 1.0 * (Ultities.GetGroupCount(story, i, frame) - 1));
                                    func += InnerDistanceConstraintWeight * t.Item1;
                                    grad[index] += InnerDistanceConstraintWeight * t.Item2;
                                }
                                else
                                {
                                    var t = OuterEnergy(x[index], x[dict[new Tuple<int, int>(j, frame)]], 5.0);
                                    func += OutterDistanceConstraintWeight * t.Item1;
                                    grad[index] += OutterDistanceConstraintWeight * t.Item2;
                                }
                            }
                        }

                        if (LineStraightFunction == 1 && frame > 0 && frame < story.FrameCount - 1 && story.SessionTable[i, frame - 1] != -1 && story.SessionTable[i, frame + 1] != -1)
                        {
                            var t = StraightEnergy(x[index], x[dict[new Tuple<int, int>(i, frame - 1)]], x[dict[new Tuple<int, int>(i, frame + 1)]]);
                            func += LineStraightWeight * t.Item1;
                            grad[index] += LineStraightWeight * t.Item2;
                        }
                        else
                        {
                            if (frame > 0 && story.SessionTable[i, frame - 1] != -1)
                            {
                                var t = StraightEnergy(x[index], x[dict[new Tuple<int, int>(i, frame - 1)]]);
                                func += LineStraightWeight * t.Item1;
                                grad[index] += LineStraightWeight * t.Item2;
                            }
                            else if (frame < story.FrameCount - 1 && story.SessionTable[i, frame + 1] != -1)
                            {
                                var t = StraightEnergy(x[index], x[dict[new Tuple<int, int>(i, frame + 1)]]);
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

        public void Optimize(Story story, PositionTable<double> position)
        {
            int count = 0;
            List<double> list = new List<double>();
            Dictionary<Tuple<int, int>, int> dict = new Dictionary<Tuple<int, int>, int>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        dict.Add(new Tuple<int, int>(i, frame), count++);
                        list.Add(position[i, frame]);
                    }
                }
            }

            double[] x = list.ToArray<double>();

            double epsg = 0.0000001;
            double epsf = 0.0001;
            double epsx = 0.0001;
            double stpmax = 1.0;
            int maxits = MaxIterationCount;
            alglib.mincgstate state;
            alglib.mincgreport rep;

            alglib.mincgcreate(x, out state);
            alglib.mincgsetcond(state, epsg, epsf, epsx, maxits);
            alglib.mincgsetstpmax(state, stpmax);
            Tuple<Story, PositionTable<double>, Dictionary<Tuple<int, int>, int>> param = new Tuple<Story, PositionTable<double>, Dictionary<Tuple<int, int>, int>>(story, position, dict);
            alglib.mincgoptimize(state, EnergyGrad, null, param);
            alglib.mincgresults(state, out x, out rep);

            foreach (KeyValuePair<Tuple<int, int>, int> pair in dict)
            {
                position[pair.Key.Item1, pair.Key.Item2] = x[dict[new Tuple<int, int>(pair.Key.Item1, pair.Key.Item2)]];
            }
        }
    }
}