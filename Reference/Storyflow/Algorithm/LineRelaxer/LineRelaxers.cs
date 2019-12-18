using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;

/// <summary>
/// Summary description for linerelaxers
/// </summary>
/// 
namespace Algorithm.linerelaxer
{
    class LineRelaxerS
    {
        private StorylineApp _app;

        public LineRelaxerS(StorylineApp app)
        {
            _app = app;
        }

        public Tuple<double, double, double>[][] Relax(Story story, PositionTable<double> position)
        {
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

            // build up original table
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

            // relax
            List<double> pinRight = new List<double>();
            for (int id = 0; id < story.Characters.Count; id++)
            {
                pinRight.Add(0);
            }
            for (int frame = 0; frame < story.FrameCount - 1; frame++)
            {
                int frameLeft = frame;
                int frameRight = frame + 1;
                //List<List<int>> chNeedRelax = new List<List<int>>();
                Dictionary<Tuple<int, int>, List<int>> needRelax = new Dictionary<Tuple<int, int>, List<int>>();
                //find the group to relax
                for (int id = 0; id < story.Characters.Count; id++)
                {
                    double k = Math.Abs((result[id][frameRight].Item3 - result[id][frameLeft].Item3) / (result[id][frameRight].Item1 - result[id][frameLeft].Item2));
                    if (story.SessionTable[id, frameLeft] != -1 && story.SessionTable[id, frameRight] != -1 && position[id, frameLeft] != position[id, frameRight] && k > 1)
                    {
                        Tuple<int, int> key = new Tuple<int, int>(story.SessionTable[id, frameLeft], story.SessionTable[id, frameRight]);
                        if (needRelax.ContainsKey(key))
                        {
                            needRelax[key].Add(id);
                        }
                        else
                        {
                            needRelax.Add(key, new List<int>());
                            needRelax[key].Add(id);
                        }
                    }
                }
                //relax each group
                foreach (Tuple<int, int> key in needRelax.Keys)
                {
                    List<int> idList = needRelax[key];
                    List<double> leftPos = new List<double>();
                    List<double> rightPos = new List<double>();
                    foreach (int id in idList)
                    {
                        leftPos.Add(Math.Max(pinRight[id], GetLeftPos(id, frameLeft, frameRight)));
                        rightPos.Add(GetRightPos(id, frameRight, frameRight));
                    }
                    double leftResult = leftPos.Max();
                    double rightResult = rightPos.Max();
                    if (Math.Abs(leftResult - rightResult) > Math.Abs(result[idList[0]][frameRight].Item3 - result[idList[0]][frameLeft].Item3))//can relax
                    {

                    }
                }
            }

            return result;
        }

        double GetLeftPos(int id, int frameLeft, int frameRight)
        {
            return 0;
        }

        double GetRightPos(int id, int frameLeft, int frameRight)
        {
            return 0;
        }

        List<int> GetGroup(int id, int frame, Story story)
        {
            List<int> result = new List<int>();
            if (story.SessionTable[id, frame] != -1)
            {
                for (int i = 0; i < story.Characters.Count; i++)
                {
                    if (story.SessionTable[id, frame] == story.SessionTable[i, frame])
                    {
                        result.Add(i);
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