using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Windows;
using Storyline;

/// <summary>
/// Summary description for bundleconverter
/// </summary>
/// 
namespace Algorithm.bundle
{
    class BundleConverter
    {
        private StorylineApp _app;

        public BundleConverter(StorylineApp app)
        {
            _app = app;
        }

        public List<Tuple<Point, Point, int, int, int, int, int>> GetBundle(Story story, Tuple<double, double, double>[][] position)
        {
            List<Tuple<Point, Point, int, int, int, int, int>> result = new List<Tuple<Point, Point, int, int, int, int, int>>();

            List<Tuple<double, double, double>>[] groupPos = new List<Tuple<double, double, double>>[story.FrameCount];
            List<int>[] groupN = new List<int>[story.FrameCount];
            List<int>[] groupBundle = new List<int>[story.FrameCount];
            int[,] group = new int[story.Characters.Count, story.FrameCount];

            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                groupPos[frame] = new List<Tuple<double, double, double>>();
                groupN[frame] = new List<int>();
                groupBundle[frame] = new List<int>();
                List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1 && position[i][frame].Item1 <= position[i][frame].Item2)
                    {
                        list.Add(new Tuple<int, double>(i, position[i][frame].Item3));
                    }
                }
                list.Sort((a, b) =>
                {
                    return a.Item2.CompareTo(b.Item2);
                });
                double last = -double.MaxValue;
                foreach (Tuple<int, double> tuple in list)
                {
                    if (Math.Abs(tuple.Item2 - last) > 1e-5)
                    {
                        groupPos[frame].Add(position[tuple.Item1][frame]);
                        groupN[frame].Add(0);
                        groupBundle[frame].Add(0);
                        last = tuple.Item2;
                    }
                    group[tuple.Item1, frame] = groupN[frame].Count - 1;
                    groupN[frame][groupN[frame].Count - 1]++;
//                    groupBundle[frame][groupN[frame].Count - 1] = _app.Status.BundleMapping.GetBundleBySegment(tuple.Item1, frame);
                }
                for (int i = 0; i < groupN[frame].Count; ++i)
                {
                    result.Add(new Tuple<Point, Point, int, int, int, int, int>(
                        new Point(groupPos[frame][i].Item1, groupPos[frame][i].Item3),
                        new Point(groupPos[frame][i].Item2, groupPos[frame][i].Item3),
                        groupN[frame][i],
                        groupN[frame][i],
                        groupN[frame][i],
                        groupBundle[frame][i],
                        groupBundle[frame][i]));
                }
            }

            Dictionary<Tuple<int, int, int, int>, int> connections = new Dictionary<Tuple<int, int, int, int>, int>();
            for (int id = 0; id < story.Characters.Count; ++id)
            {
                int last = -1;
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[id, frame] != -1)
                    {
                        if (position[id][frame].Item1 > position[id][frame].Item2 && frame != 0 && story.SessionTable[id, frame - 1] != -1 && frame < story.FrameCount - 1 && story.SessionTable[id, frame + 1] != -1) // pass this frame
                            continue;
                        if (frame == 0 || story.SessionTable[id, frame - 1] == -1) // new segment starting
                        {
                            last = frame;
                        }
                        else // follow previous segment
                        {
                            // from last to here

                            Tuple<int, int, int, int> key = new Tuple<int, int, int, int>(last, group[id, last], frame, group[id, frame]);
                            if (!connections.ContainsKey(key))
                            {
                                connections.Add(key, 0);
                            }
                            connections[key]++;

                            last = frame;
                        }
                    }
                }
            }

            foreach (KeyValuePair<Tuple<int, int, int, int>, int> pair in connections)
            {
                result.Add(new Tuple<Point, Point, int, int, int, int, int>(
                    new Point(groupPos[pair.Key.Item1][pair.Key.Item2].Item2, groupPos[pair.Key.Item1][pair.Key.Item2].Item3),
                    new Point(groupPos[pair.Key.Item3][pair.Key.Item4].Item1, groupPos[pair.Key.Item3][pair.Key.Item4].Item3),
                    groupN[pair.Key.Item1][pair.Key.Item2],
                    groupN[pair.Key.Item3][pair.Key.Item4],
                    pair.Value,
                    groupBundle[pair.Key.Item1][pair.Key.Item2],
                    groupBundle[pair.Key.Item3][pair.Key.Item4]));
            }

            return result;
        }
    }
}