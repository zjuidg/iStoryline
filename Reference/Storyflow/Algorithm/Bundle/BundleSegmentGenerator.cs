using System;
using System.Collections.Generic;
using Structure;
using Storyline;

/// <summary>
/// Summary description for bundlesegmentgenerator
/// </summary>
/// 
namespace Algorithm.bundle
{
    public class BundleSegmentGenerator
    {
        private StorylineApp _app;

        public BundleSegmentGenerator(StorylineApp app)
        {
            _app = app;
        }

        public List<Tuple<double, double, double>>[] groupPos;
        public List<int>[] groupN;
        public List<int>[] groupBundle;
        public List<Color>[] blendColor;
        public int[,] group;

        public List<Tuple<double, int, double, int, int, int, int, Tuple<Color, Color, Color>>> GetBundleSegment(Story story, Tuple<double, double, double>[][] position)
        {
            List<Tuple<double, int, double, int, int, int, int, Tuple<Color, Color, Color>>> result = new List<Tuple<double, int, double, int, int, int, int, Tuple<Color, Color, Color>>>();
            //List<Tuple<Color, Color, Color>> resultColor = new List<Tuple<Color, Color, Color>>();
            groupPos = new List<Tuple<double, double, double>>[story.FrameCount];
            groupN = new List<int>[story.FrameCount];
            groupBundle = new List<int>[story.FrameCount];
            //By Simon : blend
            blendColor = new List<Color>[story.FrameCount];
            group = new int[story.Characters.Count, story.FrameCount];
            for (int id = 0; id < story.Characters.Count; id++)
            {
                for (int frame = 0; frame < story.FrameCount; frame++)
                {
                    group[id, frame] = -1;
                }
            }
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                groupPos[frame] = new List<Tuple<double, double, double>>();
                groupN[frame] = new List<int>();
                groupBundle[frame] = new List<int>();
                blendColor[frame] = new List<Color>();
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
                        blendColor[frame].Add(story.Characters[tuple.Item1].Color);
                    }
                    else
                    {
                        float groupSize = groupN[frame][groupN[frame].Count - 1];
                        blendColor[frame][groupN[frame].Count - 1] = blendColor[frame][groupN[frame].Count - 1] * (groupSize / (groupSize + 1)) + story.Characters[tuple.Item1].Color * (1 / (groupSize + 1));
                    }
                    group[tuple.Item1, frame] = groupN[frame].Count - 1;
                    groupN[frame][groupN[frame].Count - 1]++;
//                    groupBundle[frame][groupN[frame].Count - 1] = _app.Status.BundleMapping.GetBundleBySegment(tuple.Item1, frame);

                }
                for (int i = 0; i < groupN[frame].Count; ++i)
                {
                    Tuple<Color, Color, Color> bC = new Tuple<Color, Color, Color>(blendColor[frame][i], blendColor[frame][i], blendColor[frame][i]);
                    result.Add(new Tuple<double, int, double, int, int, int, int, Tuple<Color, Color, Color>>(
                        groupPos[frame][i].Item1, groupBundle[frame][i],
                        groupPos[frame][i].Item2, groupBundle[frame][i],
                        groupN[frame][i],
                        groupN[frame][i],
                        groupN[frame][i],
                        bC
                        ));
                }
            }//every FRAME!!!! just for my information

            Dictionary<Tuple<int, int, int, int>, Tuple<int, Color>> connections = new Dictionary<Tuple<int, int, int, int>, Tuple<int, Color>>();
            //Dictionary<Tuple<int, int, int, int>, int> connections = new Dictionary<Tuple<int, int, int, int>, int>();
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
                                connections.Add(key, new Tuple<int, Color>(1, story.Characters[id].Color));
                            }
                            else
                            {
                                //Blend
                                float curveGroupSize = connections[key].Item1;
                                Color curveBlendColor = connections[key].Item2 * (curveGroupSize / (curveGroupSize + 1)) + story.Characters[id].Color * (1 / (curveGroupSize + 1));
                                connections[key] = new Tuple<int, Color>(connections[key].Item1 + 1, curveBlendColor);
                            }
                            last = frame;
                        }
                    }
                }
            }

            foreach (KeyValuePair<Tuple<int, int, int, int>, Tuple<int, Color>> pair in connections)
            {
                if (pair.Key.Item2 == -1 || pair.Key.Item4 == -1)
                    continue;
                result.Add(new Tuple<double, int, double, int, int, int, int, Tuple<Color, Color, Color>>(
                    groupPos[pair.Key.Item1][pair.Key.Item2].Item2, groupBundle[pair.Key.Item1][pair.Key.Item2],
                    groupPos[pair.Key.Item3][pair.Key.Item4].Item1, groupBundle[pair.Key.Item3][pair.Key.Item4],
                    groupN[pair.Key.Item1][pair.Key.Item2],
                    groupN[pair.Key.Item3][pair.Key.Item4],
                    pair.Value.Item1,
                    new Tuple<Color, Color, Color>(blendColor[pair.Key.Item1][pair.Key.Item2], blendColor[pair.Key.Item3][pair.Key.Item4], pair.Value.Item2)//left right, middle
                    ));

            }

            return result;
        }
    }
}