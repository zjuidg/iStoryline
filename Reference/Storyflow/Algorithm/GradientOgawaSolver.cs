using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using System.Diagnostics;
using Storyline;
/// <summary>
/// Summary description for GradientOgawaSolver
/// </summary>
/// 
namespace Algorithm
{
    class GradientOgawaSolver : ISolver
    {
        private StorylineApp _app;

        public GradientOgawaSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> position)
        {
            return position;
        }

        public PositionTable<double> Solve(Story story)
        {
            int outerGap = (int)(_app.Status.Config.Style.OuterGap / _app.Status.Config.Style.DefaultInnerGap);
            PositionTable<int> positionTable = new PositionTable<int>(story.Characters.Count, story.TimeStamps.Length - 1);

            // 1.put first frame
            List<Tuple<int, List<int>>> list = Ultities.GetGroups(story, 0);
            //List<Tuple<int, List<int>>> list = Ultities.GetRandomList<Tuple<int, List<int>>>(Ultities.GetGroups(story, 0));

            int yBaseline = 0;
            foreach (Tuple<int, List<int>> tuple in list)
            {
                foreach (int id in tuple.Item2)
                {
                    positionTable[id, 0] = yBaseline;
                    yBaseline += 1;
                }
                yBaseline += outerGap;
            }

            // 2.calculate group average for other frames
            for (int frame = 1; frame < story.TimeStamps.Length - 1; ++frame)
            {
                list = Ultities.GetGroups(story, frame);
                list.Sort((a, b) => -a.Item2.Count.CompareTo(b.Item2.Count));

                List<int> occupied = new List<int>();
                foreach (Tuple<int, List<int>> tuple in list)
                {
                    // sort by previous position
                    tuple.Item2.Sort((a, b) => positionTable[a, frame - 1].CompareTo(positionTable[b, frame - 1]));
                    // calculate weighted average position
                    int weight = 0;
                    int value = 0;
                    int sub = 0;
                    bool allNew = true;
                    foreach (int id in tuple.Item2)
                    {
                        if (story.SessionTable[id, frame - 1] != -1)
                        {
                            allNew = false;
                            break;
                        }
                    }
                    int top;
                    if (allNew)
                    {
                        top = 0 - tuple.Item2.Count / 2;
                    }
                    else
                    {
                        for (int i = 0; i < tuple.Item2.Count; ++i)
                        {
                            int id = tuple.Item2[i];
                            int w = Ultities.GetHistoryLength(story, id, frame);
                            value += w * positionTable[id, frame - 1];
                            weight += w;
                            sub += w * i;
                        }
                        double bestCenter = (double)value / weight;
                        top = (int)Math.Round((bestCenter * weight - sub) / weight);
                    }
                    // find a place to put it
                    for (int shift = 0; true; ++shift)
                    {
                        int shiftedTop1 = top - shift;
                        int shiftedTop2 = top + shift;
                        bool available = false;
                        int pos = 0;
                        if (IsAvailable(occupied, shiftedTop1 - outerGap, shiftedTop1 + tuple.Item2.Count - 1 + outerGap))
                        {
                            pos = shiftedTop1;
                            available = true;
                        }
                        else if (IsAvailable(occupied, shiftedTop2 - outerGap, shiftedTop2 + tuple.Item2.Count - 1 + outerGap))
                        {
                            pos = shiftedTop2;
                            available = true;
                        }
                        if (available)
                        {
                            for (int i = 0; i < tuple.Item2.Count; ++i)
                            {
                                positionTable[tuple.Item2[i], frame] = pos + i;
                                occupied.Add(pos + i);
                            }
                            break;
                        }
                    }
                }
            }

            for (int t = 0; t < 10; ++t)
            {
                // shift lines to new positions
                for (int frame = 1; frame < story.TimeStamps.Length - 2; ++frame)
                {
                    HashSet<int> deltas = new HashSet<int>();
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1)
                        {
                            int delta = positionTable[id, frame] - positionTable[id, frame + 1];
                            if (!deltas.Contains(delta))
                                deltas.Add(delta);
                        }
                    }
                    int minCost = int.MaxValue;
                    int minDelta = 0;
                    foreach (int delta in deltas)
                    {
                        int cost = 0;
                        for (int id = 0; id < story.Characters.Count; ++id)
                        {
                            if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1)
                            {
                                cost += Math.Abs(positionTable[id, frame + 1] + delta - positionTable[id, frame]);
                                if (positionTable[id, frame + 1] + delta == positionTable[id, frame])
                                {
                                    cost -= 100;
                                }
                            }
                        }
                        if (minCost > cost)
                        {
                            minCost = cost;
                            minDelta = delta;
                        }
                    }
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        positionTable[id, frame + 1] += minDelta;
                    }
                }

                for (int frame = 1; frame < story.TimeStamps.Length - 2; ++frame)
                {
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        if (Ultities.GetGroupCount(story, id, frame) == 1 && Ultities.GetGroupCount(story, id, frame + 1) > 1)
                        {
                            bool isHead = true;
                            int f;
                            for (f = frame - 1; f >= 0; --f)
                            {
                                if (Ultities.GetGroupCount(story, id, f) > 1)
                                {
                                    isHead = false;
                                    break;
                                }
                                else if (story.SessionTable[id, f] == -1)
                                {
                                    break;
                                }
                            }

                            //if (!isHead)
                            //    continue;

                            //positionTable[id, frame] = -100;
                            //continue;
                            //
                            HashSet<int> tubes = new HashSet<int>();
                            for (int ii = f + 1; ii <= frame; ++ii)
                            {
                                for (int ch = 0; ch < story.Characters.Count; ++ch)
                                {
                                    if (ch != id && story.SessionTable[ch, ii] != -1)
                                    {
                                        if (!tubes.Contains(positionTable[ch, ii]))
                                            tubes.Add(positionTable[ch, ii]);
                                    }
                                }
                            }
                            var occupied = tubes.ToList<int>();
                            int p = positionTable[id, frame + 1];
                            // find a place to put it
                            for (int shift = 0; true; ++shift)
                            {
                                int shiftedTop1 = p - shift;
                                int shiftedTop2 = p + shift;
                                bool available = false;
                                int pp = 0;
                                if (IsAvailable(occupied, shiftedTop1 - outerGap, shiftedTop1 + outerGap))
                                {
                                    pp = shiftedTop1;
                                    available = true;
                                }
                                else if (IsAvailable(occupied, shiftedTop2 - outerGap, shiftedTop2 + outerGap))
                                {
                                    pp = shiftedTop2;
                                    available = true;
                                }
                                if (available)
                                {
                                    for (int ii = f + 1; ii <= frame; ++ii)
                                    {
                                        //if (Math.Abs(positionTable[id, ii] - pp) > 0)
                                        {
                                            positionTable[id, ii] = pp;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                //
            }

            PositionTable<double> position = positionTable.Clone<double>();

            Debug.WriteLine("Before, Crossing:{0}", Crossing.Count(story, position.Clone<int>()));

            PositionOptimizer3 optimizer2 = new PositionOptimizer3();
            position = optimizer2.Optimize(story, position, 0.4, 0.9, 0.0, 0.0);
            position = optimizer2.Optimize(story, position, 0.9, 0.9, 0.0, 0.0);

            PositionOptimizer2 optimizer = new PositionOptimizer2();
            position = optimizer.Optimize(story, position, 0.4, 0.5, 0.0, 0.0);
            position = optimizer2.Optimize(story, position, 0.4, 0.9, 0.0, 0.0);


            //position = optimizer2.Optimize(story, position, 1.0, 0.1, 0.0, 0.0);

            //position = optimizer.Optimize(story, position, 1.0, 0.1, 0.0, 0.0);

            PositionOptimizer1 optimizer1 = new PositionOptimizer1();
            //position = optimizer1.Optimize(story, position);

            int x = 4;
            while (x-- > 0)
            {
                //position = optimizer.Optimize(story, position, 0.6, 0.2, 0.0, 0.0);
                position = optimizer2.Optimize(story, position, 1.0, 0.5, 0.0, 0.0);
                position = optimizer1.Optimize(story, position);
            }

            // move to positive
            double min = 0;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1 && min > position[id, frame])
                        min = position[id, frame];

            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        position[id, frame] -= min;
                        position[id, frame] *= 5;
                    }

            Debug.WriteLine("Crossing:{0}", Crossing.Count(story, position.Clone<int>()));


            return position;



            positionTable = position.Clone<int>();



            int[,] permutationTable = new int[story.Characters.Count, story.TimeStamps.Length - 1];

            {
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                {
                    List<Tuple<int, int>> l = new List<Tuple<int, int>>();
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1)
                        {
                            l.Add(new Tuple<int, int>(id, positionTable[id, frame]));
                        }
                        else
                        {
                            l.Add(new Tuple<int, int>(id, 100000));
                        }
                    }
                    l.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                    for (int i = 0; i < l.Count; ++i)
                    {
                        permutationTable[l[i].Item1, frame] = i;
                    }
                }

                // calculate positions
                PositionTable<int> posY = new PositionTable<int>(story.Characters.Count, story.TimeStamps.Length - 1);

                List<int[]>[] intervals = new List<int[]>[story.TimeStamps.Length - 1];
                List<int>[] prev = new List<int>[story.TimeStamps.Length - 1];
                List<int>[] next = new List<int>[story.TimeStamps.Length - 1];
                List<Tuple<int, int, int>>[] lcss = new List<Tuple<int, int, int>>[story.TimeStamps.Length - 1];

                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                {
                    // find intervals at this frame
                    int[] t = new int[story.Characters.Count];
                    for (int i = 0; i < story.Characters.Count; ++i)
                    {
                        t[permutationTable[i, frame]] = i;
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
                                dp[i + 1, j + 1] = -1;
                                if (dp[i + 1, j + 1] < dp[i, j] + lcs[i, j].Item3)
                                {
                                    dp[i + 1, j + 1] = dp[i, j] + lcs[i, j].Item3;
                                    path[i + 1, j + 1] = new Tuple<int, int>(i, j);
                                }
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

                int[] top = new int[story.TimeStamps.Length - 1];
                List<List<Tuple<int, int>>> layers = new List<List<Tuple<int, int>>>();
                while (true)
                {
                    bool[] isAtTop = new bool[story.TimeStamps.Length - 1];
                    bool exist = false;
                    for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
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
                    for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    {
                        if (isAtTop[frame])
                        {
                            layer.Add(new Tuple<int, int>(frame, top[frame]));
                            ++top[frame];
                        }
                    }
                }

                //Point[,] positions = new Point[permutation.FrameCount, permutation.CharacterCount];
                //double[,] posY = new double[frameCount, characterCount];
                int yBaseline1 = 0;
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
                            posY[character, frame] = yBaseline1 + (topY[j] + ii) * (int)_app.Status.Config.Style.DefaultInnerGap;
                            //positions[frame, character] = new Point(frame * styles.TimeScaleFactor, baseline + (topY[j] + ii) * styles.InnerDistance);
                            if (maxY < posY[character, frame])
                                maxY = (int)posY[character, frame];
                        }
                    }
                    yBaseline1 = maxY + (int)_app.Status.Config.Style.OuterGap;
                }

                {
                    // new added, move down the hanging lines from bottom lay to top
                    bool[,] rearranged = new bool[story.Characters.Count, story.TimeStamps.Length - 1];
                    int[] topOfFrame = new int[story.TimeStamps.Length - 1];
                    for (int i = 0; i < topOfFrame.Length; ++i)
                        topOfFrame[i] = maxY;

                    for (int i = layers.Count - 1; i >= 0; --i)
                    {
                        List<Tuple<int, int>> layer = layers[i];
                        if (i != layers.Count - 1) // this is not the bottom layer
                        {
                            int minDepth = int.MaxValue;
                            int BottomOfPiece = 0;
                            int TopOfPieceGround = 0;
                            List<Tuple<int, int>> stack = new List<Tuple<int, int>>();
                            for (int j = 0; j < layer.Count; ++j)
                            {
                                int frame = layer[j].Item1;
                                int k = layer[j].Item2;

                                int max = int.MinValue;
                                for (int ii = 0; ii < intervals[frame][k].Length; ++ii)
                                {
                                    int character = intervals[frame][k][ii];
                                    if (max < posY[character, frame])
                                        max = posY[character, frame];
                                }

                                if (prev[frame][k] == -1)
                                {
                                    BottomOfPiece = max;
                                    TopOfPieceGround = topOfFrame[frame];
                                    stack.Clear();
                                    stack.Add(new Tuple<int, int>(frame, k));

                                    if (intervals[frame][k].Length == 1 &&
                                        (frame > 0 && story.SessionTable[intervals[frame][k][0], frame - 1] == -1))
                                    {
                                        int pos = posY[intervals[frame][k][0], frame];
                                        int f = frame;
                                        do
                                        {
                                            ++f;
                                        }
                                        while (f < story.TimeStamps.Length - 1 && posY[intervals[frame][k][0], f] == pos);
                                        if (f != story.TimeStamps.Length - 1)
                                        {
                                            HashSet<int> tubes = new HashSet<int>();
                                            for (int ii = frame; ii < f; ++ii)
                                            {
                                                for (int ch = 0; ch < story.Characters.Count; ++ch)
                                                {
                                                    if (ch != intervals[frame][k][0])
                                                    {
                                                        if (!tubes.Contains(posY[ch, ii]))
                                                            tubes.Add(posY[ch, ii]);
                                                    }
                                                }
                                            }
                                            var occupied = tubes.ToList<int>();
                                            int p = posY[intervals[frame][k][0], f];
                                            // find a place to put it
                                            for (int shift = 0; true; ++shift)
                                            {
                                                int shiftedTop1 = p - shift;
                                                int shiftedTop2 = p + shift;
                                                bool available = false;
                                                int pp = 0;
                                                if (IsAvailable(occupied, shiftedTop1 - (int)_app.Status.Config.Style.OuterGap, shiftedTop1 + (int)_app.Status.Config.Style.OuterGap))
                                                {
                                                    pp = shiftedTop1;
                                                    available = true;
                                                }
                                                else if (IsAvailable(occupied, shiftedTop2 - (int)_app.Status.Config.Style.OuterGap, shiftedTop2 + (int)_app.Status.Config.Style.OuterGap))
                                                {
                                                    pp = shiftedTop2;
                                                    available = true;
                                                }
                                                if (available)
                                                {
                                                    for (int ii = frame; ii < f; ++ii)
                                                    {
                                                        if (Math.Abs(posY[intervals[frame][k][0], ii] - pp) > 0)
                                                        {
                                                            posY[intervals[frame][k][0], ii] = pp;
                                                            rearranged[intervals[frame][k][0], ii] = true;
                                                        }
                                                    }
                                                    break;
                                                }
                                            }

                                        }
                                    }
                                }
                                else
                                {
                                    if (BottomOfPiece < max)
                                        BottomOfPiece = max;
                                    if (TopOfPieceGround > topOfFrame[frame])
                                        TopOfPieceGround = topOfFrame[frame];
                                    stack.Add(new Tuple<int, int>(frame, k));
                                }
                                if (next[frame][k] == -1)
                                {
                                    if (TopOfPieceGround - BottomOfPiece > (int)_app.Status.Config.Style.OuterGap * 2 - 1)
                                    {
                                        // a large gap detected
                                        int delta = TopOfPieceGround - BottomOfPiece - (int)_app.Status.Config.Style.OuterGap;

                                        int up = 0;
                                        int down = 0;
                                        foreach (Tuple<int, int> tuple in stack)
                                        {
                                            int f = tuple.Item1;
                                            int kk = tuple.Item2;
                                            for (int jj = 0; jj < intervals[f][kk].Length; ++jj)
                                            {
                                                int ch = intervals[f][kk][jj];
                                                if (f < story.TimeStamps.Length - 1 - 1)
                                                {
                                                    if (story.SessionTable[ch, f + 1] != -1)
                                                    {
                                                        if (posY[ch, f] > posY[ch, f + 1])
                                                            ++up;
                                                        else if (posY[ch, f] < posY[ch, f + 1])
                                                            ++down;
                                                    }
                                                }
                                                if (f > 0)
                                                {
                                                    if (story.SessionTable[ch, f - 1] != -1)
                                                    {
                                                        if (posY[ch, f] > posY[ch, f - 1])
                                                            ++up;
                                                        else if (posY[ch, f] < posY[ch, f - 1])
                                                            ++down;
                                                    }
                                                }
                                            }
                                        }

                                        if (down >= up)
                                        {
                                            foreach (Tuple<int, int> tuple in stack)
                                            {
                                                int f = tuple.Item1;
                                                int kk = tuple.Item2;
                                                for (int ii = 0; ii < intervals[f][kk].Length; ++ii)
                                                {
                                                    int character = intervals[f][kk][ii];
                                                    if (!rearranged[character, f])
                                                    {
                                                        posY[character, f] += delta;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        for (int j = 0; j < layer.Count; ++j)
                        {
                            int frame = layer[j].Item1;
                            int k = layer[j].Item2;
                            for (int ii = 0; ii < intervals[frame][k].Length; ++ii)
                            {
                                int character = intervals[frame][k][ii];
                                if (topOfFrame[frame] > posY[character, frame])
                                    topOfFrame[frame] = posY[character, frame];
                            }
                        }
                    }
                    // over
                }

                return posY.Clone<double>();
            }
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


        private bool IsAvailable(List<int> occupied, int top, int bottom)
        {
            foreach (int pos in occupied)
            {
                if (pos >= top && pos <= bottom)
                    return false;
            }
            return true;
        }
    }
}