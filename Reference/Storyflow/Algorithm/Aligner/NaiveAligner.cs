using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using Storyline;
/// <summary>
/// Summary description for naivealigner
/// </summary>
/// 
namespace Algorithm.aligner
{
    class NaiveAligner : IAligner
    {
        private StorylineApp _app;

        public NaiveAligner(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<int> Align(Story story, PositionTable<int> permutation)
        {
            // some tricky codes
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                int max = -1;
                for (int id = 0; id < story.Characters.Count; ++id)
                {
                    if (permutation[id, frame] > max)
                        max = permutation[id, frame];
                }
                for (int id = 0; id < story.Characters.Count; ++id)
                {
                    if (permutation[id, frame] == -1)
                        permutation[id, frame] = ++max;
                }
            }


            // calculate positions
            PositionTable<int> posY = new PositionTable<int>(story.Characters.Count, story.FrameCount);

            List<int[]>[] intervals = new List<int[]>[story.FrameCount];
            List<int>[] prev = new List<int>[story.FrameCount];
            List<int>[] next = new List<int>[story.FrameCount];
            List<Tuple<int, int, int>>[] lcss = new List<Tuple<int, int, int>>[story.FrameCount];

            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                // find intervals at this frame
                int[] t = new int[story.Characters.Count];
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    t[permutation[i, frame]] = i;
                }
                List<int> p = new List<int>();
                for (int i = 0; i < t.Length; ++i)
                {
                    if (story.SessionTable[t[i], frame] != -1)
                    {
                        p.Add(t[i]);
                    }
                }
                intervals[frame] = new List<int[]>();
                prev[frame] = new List<int>();
                next[frame] = new List<int>();
                lcss[frame] = new List<Tuple<int, int, int>>();
                int last = -1;
                for (int i = 0; i < p.Count; ++i)
                {
                    if (i == p.Count - 1 || story.SessionTable[p[i], frame] != story.SessionTable[p[i + 1], frame])
                    {
                        intervals[frame].Add(p.GetRange(last + 1, i - last).ToArray<int>());
                        last = i;
                        prev[frame].Add(-1);
                        next[frame].Add(-1);
                        lcss[frame].Add(new Tuple<int, int, int>(-1, -1, -1));
                    }
                }

                // calculate the connection with previous frame
                Random rand = new Random(DateTime.Now.Millisecond);
                if (frame > 0)
                {
                    Tuple<int, int, int>[,] lcs = new Tuple<int, int, int>[intervals[frame - 1].Count, intervals[frame].Count];
                    for (int i = 0; i < intervals[frame - 1].Count; ++i)
                    {
                        for (int j = 0; j < intervals[frame].Count; ++j)
                        {
                            lcs[i, j] = GetLcs(intervals[frame - 1][i], intervals[frame][j]);
                        }
                    }
                    int[,] dp = new int[intervals[frame - 1].Count + 1, intervals[frame].Count + 1];
                    Tuple<int, int>[,] path = new Tuple<int, int>[intervals[frame - 1].Count + 1, intervals[frame].Count + 1];
                    for (int i = 0; i < intervals[frame - 1].Count; ++i)
                    {
                        for (int j = 0; j < intervals[frame].Count; ++j)
                        {
                            // modified for random
                            dp[i + 1, j + 1] = -1;

                            if (dp[i + 1, j + 1] < dp[i, j] + lcs[i, j].Item3)
                            {
                                dp[i + 1, j + 1] = dp[i, j] + lcs[i, j].Item3;
                                path[i + 1, j + 1] = new Tuple<int, int>(i, j);
                            }

                            if (rand.Next(2) == 0)
                            {
                                if (dp[i + 1, j + 1] < dp[i + 1, j])
                                {
                                    dp[i + 1, j + 1] = dp[i + 1, j];
                                    path[i + 1, j + 1] = new Tuple<int, int>(i + 1, j);
                                }
                                if (dp[i + 1, j + 1] < dp[i, j + 1])
                                {
                                    dp[i + 1, j + 1] = dp[i, j + 1];
                                    path[i + 1, j + 1] = new Tuple<int, int>(i, j + 1);
                                }
                            }
                            else
                            {
                                if (dp[i + 1, j + 1] < dp[i, j + 1])
                                {
                                    dp[i + 1, j + 1] = dp[i, j + 1];
                                    path[i + 1, j + 1] = new Tuple<int, int>(i, j + 1);
                                }
                                if (dp[i + 1, j + 1] < dp[i + 1, j])
                                {
                                    dp[i + 1, j + 1] = dp[i + 1, j];
                                    path[i + 1, j + 1] = new Tuple<int, int>(i + 1, j);
                                }
                            }
                        }
                    }
                    {
                        int i = intervals[frame - 1].Count - 1;
                        int j = intervals[frame].Count - 1;
                        while (i > -1 && j > -1)
                        {
                            if (path[i + 1, j + 1].Item1 == i && path[i + 1, j + 1].Item2 == j)
                            {
                                prev[frame][j] = i;
                                next[frame - 1][i] = j;
                                lcss[frame][j] = lcs[i, j];
                            }
                            Tuple<int, int> pair = path[i + 1, j + 1];
                            i = pair.Item1 - 1;
                            j = pair.Item2 - 1;
                        }
                    }
                }
            }

            int[] top = new int[story.FrameCount];
            List<List<Tuple<int, int>>> layers = new List<List<Tuple<int, int>>>();
            while (true)
            {
                bool[] isAtTop = new bool[story.FrameCount];
                bool exist = false;
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    // now calculate a bool of isAtTop[frame]
                    if (top[frame] >= intervals[frame].Count) // this frame is already empty
                    {
                        continue;
                    }
                    exist = true;
                    bool flag = true;
                    int current = prev[frame][top[frame]];
                    int currentFrame = frame - 1;
                    while (current > -1)
                    {
                        if (top[currentFrame] != current)
                        {
                            flag = false;
                            break;
                        }
                        current = prev[currentFrame][current];
                        --currentFrame;
                    }
                    current = next[frame][top[frame]];
                    currentFrame = frame + 1;
                    while (current > -1)
                    {
                        if (top[currentFrame] != current)
                        {
                            flag = false;
                            break;
                        }
                        current = next[currentFrame][current];
                        ++currentFrame;
                    }
                    isAtTop[frame] = flag;
                }
                if (!exist)
                {
                    break;
                }
                layers.Add(new List<Tuple<int, int>>());
                var layer = layers.Last<List<Tuple<int, int>>>();
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (isAtTop[frame])
                    {
                        layer.Add(new Tuple<int, int>(frame, top[frame]));
                        ++top[frame];
                    }
                }
            }

            //Point[,] positions = new Point[permutation.story.FrameCount, permutation.story.Characters.Count];
            //double[,] posY = new double[story.FrameCount, story.Characters.Count];
            int yBaseline = 0;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (int i = 0; i < layers.Count; ++i)
            {
                List<Tuple<int, int>> layer = layers[i];
                int[] topY = new int[layer.Count];
                int[] bottomY = new int[layer.Count];
                int lastTopY = -100000;
                minY = int.MaxValue;
                maxY = int.MinValue;
                for (int j = 0; j < layer.Count; ++j)
                {
                    int frame = layer[j].Item1;
                    int k = layer[j].Item2;
                    if (prev[frame][k] == -1)
                    {
                        topY[j] = 0;
                        lastTopY = 0;
                    }
                    else
                    {
                        topY[j] = lastTopY - lcss[frame][k].Item2 + lcss[frame][k].Item1;
                        lastTopY = topY[j];
                    }
                    if (minY > topY[j])
                        minY = topY[j];
                }
                for (int j = 0; j < layer.Count; ++j)
                {
                    int frame = layer[j].Item1;
                    int k = layer[j].Item2;
                    topY[j] -= minY;
                    bottomY[j] = topY[j] + intervals[frame][k].Length;
                    for (int ii = 0; ii < intervals[frame][k].Length; ++ii)
                    {
                        int character = intervals[frame][k][ii];
                        posY[character, frame] = yBaseline + (topY[j] + ii) * (int)_app.Status.Config.Style.DefaultInnerGap;
                        //positions[frame, character] = new Point(frame * styles.TimeScaleFactor, baseline + (topY[j] + ii) * styles.InnerDistance);
                        if (maxY < posY[character, frame])
                            maxY = (int)posY[character, frame];
                    }
                }
                yBaseline = maxY + (int)_app.Status.Config.Style.OuterGap;
            }

            return posY;
        }

        private Tuple<int, int, int> GetLcs(int[] a, int[] b)
        {
            int[,] dp = new int[a.Length + 1, b.Length + 1];
            int len = -1;
            int sa = 0;
            int sb = 0;
            for (int i = 0; i < a.Length; ++i)
            {
                for (int j = 0; j < b.Length; ++j)
                {
                    if (a[i] == b[j])
                    {
                        dp[i + 1, j + 1] = dp[i, j] + 1;
                        if (len < dp[i + 1, j + 1])
                        {
                            len = dp[i + 1, j + 1];
                            sa = i - len + 1;
                            sb = j - len + 1;
                        }
                    }
                }
            }
            return new Tuple<int, int, int>(sa, sb, len);
        }
    }
}