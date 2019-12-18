using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
using System.Diagnostics;
/// <summary>
/// Summary description for GlobalOptimizedSolver
/// </summary>
/// 
namespace Algorithm
{
    public class GlobalOptimizedSolver : ISolver
    {
        private StorylineApp _app;

        public GlobalOptimizedSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> position)
        {
            return position;
        }

        public PositionTable<double> Solve(Story story)
        {
            int frameCount = story.TimeStamps.Length - 1;
            int characterCount = story.Characters.Count;
            int[,] permutaion = new int[characterCount, frameCount];
            {
                List<int[]>[] states = new List<int[]>[frameCount];
                List<int>[] dp = new List<int>[frameCount];
                List<int>[] path = new List<int>[frameCount];

                int[] orderedSequence = new int[characterCount];
                for (int i = 0; i < characterCount; ++i)
                {
                    orderedSequence[i] = i;
                }

                for (int frame = 0; frame < frameCount; ++frame)
                {
                    states[frame] = new List<int[]>();
                    dp[frame] = new List<int>();
                    path[frame] = new List<int>();

                    int[] permutation = (int[])orderedSequence.Clone();
                    do
                    {
                        if (IsValidPermutation(permutation, story.SessionTable, frame))
                        {
                            states[frame].Add((int[])permutation.Clone());
                        }
                    }
                    while (NextPermutation<int>(permutation));

                    for (int i = 0; i < states[frame].Count; ++i)
                    {
                        if (frame == 0)
                        {
                            dp[frame].Add(0);
                            path[frame].Add(0);
                        }
                        else
                        {
                            int min = int.MaxValue;
                            int from = -1;
                            for (int j = 0; j < states[frame - 1].Count; ++j)
                            {
                                int d = dp[frame - 1][j] + GetCost(states[frame - 1][j], states[frame][i], story.SessionTable, frame);
                                if (d < min)
                                {
                                    min = d;
                                    from = j;
                                }
                            }
                            dp[frame].Add(min);
                            path[frame].Add(from);
                        }
                    }
                }

                // 
                int cost = int.MaxValue;
                int index = -1;
                int count = 0;
                for (int i = 0; i < dp[frameCount - 1].Count; ++i)
                {
                    if (cost > dp[frameCount - 1][i])
                    {
                        cost = dp[frameCount - 1][i];
                        index = i;
                        count = 1;
                    }
                    else if (cost == dp[frameCount - 1][i])
                    {
                        ++count;
                    }
                }

                for (int frame = frameCount - 1; frame > -1; --frame)
                {
                    for (int id = 0; id < characterCount; ++id)
                    {
                        permutaion[id, frame] = states[frame][index][id];
                    }
                    index = path[frame][index];
                }
            }


            // calculate positions
            PositionTable<int> posY = new PositionTable<int>(characterCount, frameCount);
            {
                List<int[]>[] intervals = new List<int[]>[frameCount];
                List<int>[] prev = new List<int>[frameCount];
                List<int>[] next = new List<int>[frameCount];
                List<Tuple<int, int, int>>[] lcss = new List<Tuple<int, int, int>>[frameCount];

                for (int frame = 0; frame < frameCount; ++frame)
                {
                    // find intervals at this frame
                    int[] t = new int[characterCount];
                    for (int i = 0; i < characterCount; ++i)
                    {
                        t[permutaion[i, frame]] = i;
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

                int[] top = new int[frameCount];
                List<List<Tuple<int, int>>> layers = new List<List<Tuple<int, int>>>();
                while (true)
                {
                    bool[] isAtTop = new bool[frameCount];
                    bool exist = false;
                    for (int frame = 0; frame < frameCount; ++frame)
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
                    for (int frame = 0; frame < frameCount; ++frame)
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
                int baseline = 0;
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
                            posY[character, frame] = baseline + (topY[j] + ii) * (int)_app.Status.Config.Style.DefaultInnerGap;
                            //positions[frame, character] = new Point(frame * styles.TimeScaleFactor, baseline + (topY[j] + ii) * styles.InnerDistance);
                            if (maxY < posY[character, frame])
                                maxY = (int)posY[character, frame];
                        }
                    }
                    baseline = maxY + (int)_app.Status.Config.Style.OuterGap;
                }

                // new added, move down the hanging lines from bottom lay to top
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
                                            if (f < frameCount - 1)
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
                                                posY[character, f] += delta;
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

            Debug.WriteLine("Crossing:{0}", Crossing.Count(story, posY));

            return posY.Clone<double>();
        }

        private int GetCost(int[] a, int[] b, SessionTable sessionTable, int frame)
        {
            Debug.Assert(a.Length == b.Length);
            int reversal = 0;
            for (int i = 0; i < a.Length - 1; ++i)
                for (int j = i + 1; j < a.Length; ++j)
                {
                    if (sessionTable[i, frame] == -1 || sessionTable[i, frame - 1] == -1 ||
                        sessionTable[j, frame] == -1 || sessionTable[j, frame - 1] == -1)
                    {
                        continue;
                    }
                    if ((a[i] > a[j] && b[i] < b[j]) || (a[i] < a[j] && b[i] > b[j]))
                    {
                        ++reversal;
                    }
                }

            int wiggle = 0;


            int[] tA = new int[a.Length];
            int[] tB = new int[a.Length];
            for (int i = 0; i < a.Length; ++i)
            {
                tA[a[i]] = i;
                tB[b[i]] = i;
            }
            List<int> pA = new List<int>();
            List<int> pB = new List<int>();
            for (int i = 0; i < tA.Length; ++i)
            {
                if (sessionTable[tA[i], frame - 1] > 0)
                {
                    pA.Add(tA[i]);
                }
                if (sessionTable[tB[i], frame] > 0)
                {
                    pB.Add(tB[i]);
                }
            }

            List<Tuple<int, int>> intervalA = new List<Tuple<int, int>>();
            List<Tuple<int, int>> intervalB = new List<Tuple<int, int>>();
            int last = -1;
            for (int i = 0; i < pA.Count; ++i)
            {
                if (i == pA.Count - 1 || sessionTable[pA[i], frame - 1] != sessionTable[pA[i + 1], frame - 1])
                {
                    // this is the last element
                    intervalA.Add(new Tuple<int, int>(last + 1, i));
                    last = i;
                }
            }
            last = -1;
            for (int i = 0; i < pB.Count; ++i)
            {
                if (i == pB.Count - 1 || sessionTable[pB[i], frame] != sessionTable[pB[i + 1], frame])
                {
                    intervalB.Add(new Tuple<int, int>(last + 1, i));
                    last = i;
                }
            }

            // calculate the longest common subsequence
            int[,] lcs = new int[intervalA.Count, intervalB.Count];
            List<int>[] subA = new List<int>[intervalA.Count];
            List<int>[] subB = new List<int>[intervalB.Count];
            for (int i = 0; i < intervalA.Count; ++i)
            {
                subA[i] = pA.GetRange(intervalA[i].Item1, intervalA[i].Item2 - intervalA[i].Item1 + 1);
            }
            for (int i = 0; i < intervalB.Count; ++i)
            {
                subB[i] = pB.GetRange(intervalB[i].Item1, intervalB[i].Item2 - intervalB[i].Item1 + 1);
            }
            for (int i = 0; i < intervalA.Count; ++i)
            {
                for (int j = 0; j < intervalB.Count; ++j)
                {
                    lcs[i, j] = GetLcsLength(subA[i], subB[j]);
                }
            }

            // 
            int max = 0;
            int[,] dp = new int[intervalA.Count + 1, intervalB.Count + 1];
            for (int i = 0; i < intervalA.Count; ++i)
            {
                for (int j = 0; j < intervalB.Count; ++j)
                {
                    dp[i + 1, j + 1] = Math.Max(Math.Max(
                        dp[i, j] + lcs[i, j],
                        dp[i + 1, j]),
                        dp[i, j + 1]
                        );
                    if (max < dp[i + 1, j + 1])
                        max = dp[i + 1, j + 1];
                }
            }

            return reversal - max;
            //return -max;

        }

        private int GetLcsLength(List<int> a, List<int> b)
        {
            int[,] dp = new int[a.Count + 1, b.Count + 1];
            int res = 0;
            for (int i = 0; i < a.Count; ++i)
            {
                for (int j = 0; j < b.Count; ++j)
                {
                    if (a[i] == b[j])
                    {
                        dp[i + 1, j + 1] = dp[i, j] + 1;
                        if (res < dp[i + 1, j + 1])
                            res = dp[i + 1, j + 1];
                    }
                }
            }
            return res;
        }

        private bool IsValidPermutation(int[] permutation, SessionTable sessionTable, int frame)
        {
            int[] t = new int[permutation.Length];
            for (int i = 0; i < permutation.Length; ++i)
            {
                t[permutation[i]] = sessionTable[i, frame];
            }
            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < t.Length; ++i)
            {
                if (t[i] == -1)
                {
                    continue;
                }
                if (i == 0)
                {
                    used.Add(t[i]);
                    continue;
                }
                if (t[i] == t[i - 1])
                {
                    continue;
                }
                else
                {
                    if (used.Contains(t[i]))
                        return false;
                    used.Add(t[i]);
                }
            }
            return true;
        }

        private bool NextPermutation<T>(T[] t) where T : IComparable
        {
            for (int i = t.Length - 2; i >= 0; i--)
            {
                if (t[i].CompareTo(t[i + 1]) < 0)
                {
                    Array.Reverse(t, i + 1, t.Length - i - 1);
                    for (int j = i + 1; j < t.Length; j++)
                    {
                        if (t[i].CompareTo(t[j]) < 0)
                        {
                            T temp = t[i]; t[i] = t[j]; t[j] = temp;
                            return true;
                        }
                    }
                }
            }
            return false;
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