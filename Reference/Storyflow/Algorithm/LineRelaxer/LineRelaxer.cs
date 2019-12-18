using System;
using System.Collections.Generic;
using System.Linq;
using Structure;
using Storyline;
/// <summary>
/// Summary description for linerelaxer
/// </summary>
/// 
namespace Algorithm.linerelaxer
{
    class LineRelaxer
    {
        private StorylineApp _app;

        public LineRelaxer(StorylineApp app)
        {
            _app = app;
        }

        public Tuple<double, double, double>[][] Relax(Story story, PositionTable<double> position)
        {
            Tuple<double, double, double>[][] result = new Tuple<double, double, double>[story.Characters.Count][];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                result[i] = new Tuple<double, double, double>[story.FrameCount];
            }
            double xBaseline = 0;
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                double xEnd = xBaseline + _app.Status.Config.Style.XFactor * (story.TimeStamps[frame + 1] - story.TimeStamps[frame]);
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    result[i][frame] = new Tuple<double, double, double>(xBaseline, xEnd, position[i, frame]);
                }
                xBaseline = xEnd + _app.Status.Config.Style.XGap;
            }

            if (!_app.Status.Config.Relaxing)
            {
                return result;
            }
            // calculate x factor
            //double xfactor;
            {
                double min = int.MaxValue;
                double max = int.MinValue;
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    for (int frame = 0; frame < story.FrameCount; ++frame)
                    {
                        if (min > position[i, frame])
                            min = position[i, frame];
                        if (max < position[i, frame])
                            max = position[i, frame];
                    }
                }
                //xfactor = 3 * (max - min) / story.TimeStamps[story.TimeStamps.Length - 1];
            }

            for (int frame = 0; frame < story.FrameCount - 1; ++frame)
            {
                int left = frame;
                int right = frame + 1;
                List<Tuple<int, List<int>>> list1 = Ultities.GetGroups(story, left);
                List<Tuple<int, List<int>>> list2 = Ultities.GetGroups(story, right);
                foreach (Tuple<int, List<int>> tuple1 in list1)
                {
                    foreach (Tuple<int, List<int>> tuple2 in list2)
                    {
                        List<int> intersection = tuple1.Item2.Intersect(tuple2.Item2).ToList();
                        if (intersection.Count == tuple1.Item2.Count || intersection.Count == tuple2.Item2.Count)
                        {
                            double bc1 = GetBarycenter(position, tuple1.Item2, left);
                            double bc2 = GetBarycenter(position, tuple2.Item2, right);
                            double delta = Math.Abs(bc1 - bc2) - _app.Status.Config.RelaxingGradient * _app.Status.Config.Style.XGap;
                            if (delta > 0)
                            {
                                if (intersection.Count == tuple1.Item2.Count && intersection.Count == tuple2.Item2.Count)
                                {
                                    foreach (int x in intersection)
                                        result[x][left] = new Tuple<double, double, double>(result[x][left].Item1, result[x][left].Item2 - delta / 2, result[x][left].Item3);
                                    foreach (int x in intersection)
                                        result[x][right] = new Tuple<double, double, double>(result[x][right].Item1 + delta / 2, result[x][right].Item2, result[x][right].Item3);
                                }
                                else
                                {
                                    if (intersection.Count == tuple1.Item2.Count)
                                    {
                                        foreach (int x in intersection)
                                            result[x][left] = new Tuple<double, double, double>(result[x][left].Item1, result[x][left].Item2 - delta, result[x][left].Item3);
                                    }
                                    else
                                    {
                                        foreach (int x in intersection)
                                            result[x][right] = new Tuple<double, double, double>(result[x][right].Item1 + delta, result[x][right].Item2, result[x][right].Item3);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        private double GetBarycenter(PositionTable<double> position, List<int> list, int frame)
        {
            if (list.Count == 0)
                return 0;
            double ans = 0;
            foreach (int x in list)
                ans += position[x, frame];
            return ans / list.Count;
        }
    }
}