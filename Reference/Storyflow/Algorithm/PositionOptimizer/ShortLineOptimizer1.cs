using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for shortlineoptimizer1
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    public class ShortLineOptimizer1 : IPositionOptimizer
    {
        public static double InnerDistanceConstraintWeight { get; set; }
        public static double OutterDistanceConstraintWeight { get; set; }
        public static double LineStraightWeight { get; set; }
        public static int MaxIterationCount { get; set; }
        public static int MaxInnerIteration { get; set; }
        public static double ConvergentShift { get; set; }
        public static int LineStraightFunction { get; set; }


        static ShortLineOptimizer1()
        {
            InnerDistanceConstraintWeight = 1;
            OutterDistanceConstraintWeight = 1;
            LineStraightWeight = 0.4;
            MaxIterationCount = 100;
            ConvergentShift = 0.01;
        }

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

            double f = (x - y) * (z - x);
            double g = -2.0 * x + (z - y);
            if (x < min)
            {
                return new Tuple<double, double>(-f, -g);
            }
            else if (x < max)
            {
                return new Tuple<double, double>(f, g);
            }
            else
            {
                return new Tuple<double, double>(-f, -g);
            }
        }

        private void EnergyGrad(double[] x, ref double func, double[] grad, object obj)
        {
            Tuple<Story, PositionTable<double>, int, int> tuple = obj as Tuple<Story, PositionTable<double>, int, int>;
            Story story = tuple.Item1;
            PositionTable<double> position = tuple.Item2;
            int frame = tuple.Item3;
            int i = tuple.Item4;

            func = 0;
            grad[0] = 0;

            for (int j = 0; j < story.Characters.Count; ++j)
            {
                if (i != j && story.SessionTable[j, frame] != -1)
                {
                    if (story.SessionTable[i, frame] == story.SessionTable[j, frame])
                    {
                        var t = InnerEnergy(x[0], position[j, frame], 1.0, 1.0 * (Ultities.GetGroupCount(story, i, frame) - 1));
                        func += InnerDistanceConstraintWeight * t.Item1;
                        grad[0] += InnerDistanceConstraintWeight * t.Item2;
                    }
                    else
                    {
                        var t = OuterEnergy(x[0], position[j, frame], 5.0);
                        func += OutterDistanceConstraintWeight * t.Item1;
                        grad[0] += OutterDistanceConstraintWeight * t.Item2;
                    }


                }
            }

            if (LineStraightFunction == 1 && frame > 0 && frame < story.FrameCount - 1 && story.SessionTable[i, frame - 1] != -1 && story.SessionTable[i, frame + 1] != -1)
            {
                var t = StraightEnergy(x[0], position[i, frame - 1], position[i, frame + 1]);
                func += LineStraightWeight * t.Item1;
                grad[0] += LineStraightWeight * t.Item2;
            }
            else
            {
                if (frame > 0 && story.SessionTable[i, frame - 1] != -1)
                {
                    var t = StraightEnergy(x[0], position[i, frame - 1]);
                    func += LineStraightWeight * t.Item1;
                    grad[0] += LineStraightWeight * t.Item2;
                }
                else if (frame < story.FrameCount - 1 && story.SessionTable[i, frame + 1] != -1)
                {
                    var t = StraightEnergy(x[0], position[i, frame + 1]);
                    func += LineStraightWeight * t.Item1;
                    grad[0] += LineStraightWeight * t.Item2;
                }
                else
                { }
            }
        }
        private double CalculateNewPosition(Story story, PositionTable<double> position, int frame, int i)
        {
            double[] x = new double[] { position[i, frame] };

            double epsg = 0.0001;
            double epsf = 0.01;
            double epsx = 0.01;
            double stpmax = 0.5;
            int maxits = 100;
            alglib.mincgstate state;
            alglib.mincgreport rep;

            alglib.mincgcreate(x, out state);
            alglib.mincgsetcond(state, epsg, epsf, epsx, maxits);
            alglib.mincgsetstpmax(state, stpmax);
            Tuple<Story, PositionTable<double>, int, int> param = new Tuple<Story, PositionTable<double>, int, int>(story, position, frame, i);
            alglib.mincgoptimize(state, EnergyGrad, null, param);
            alglib.mincgresults(state, out x, out rep);

            System.Console.WriteLine("{0}", rep.terminationtype); // EXPECTED: 4
            System.Console.WriteLine("{0}", alglib.ap.format(x, 2)); // EXPECTED: [-3,3]
            System.Console.ReadLine();

            return x[0];
        }

        public void Optimize(Story story, PositionTable<double> position)
        {
            for (int round = 0; round < MaxIterationCount; ++round)
            {
                double maxShift = 0.0;
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    for (int i = 0; i < story.Characters.Count; ++i)
                    {
                        if (story.SessionTable[i, frame] == -1)
                            continue;

                        double newPosition = CalculateNewPosition(story, position, frame, i);
                        if (maxShift < Math.Abs(position[i, frame] - newPosition))
                            maxShift = Math.Abs(position[i, frame] - newPosition);
                        position[i, frame] = newPosition;
                    }
                }
                if (maxShift < ConvergentShift)
                    break;
            }
        }
    }
}