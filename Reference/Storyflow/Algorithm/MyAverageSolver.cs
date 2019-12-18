using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using System.Diagnostics;
using Storyline;
using Algorithm.aligner;

/// <summary>
/// Summary description for MyAverageSolver
/// </summary>
/// 
namespace Algorithm
{
    public class MyAverageSolver : ISolver
    {
        private StorylineApp _app;

        public MyAverageSolver(StorylineApp app)
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



            int[] orderedSequence = new int[characterCount];
            for (int i = 0; i < characterCount; ++i)
            {
                orderedSequence[i] = i;
            }

            int[] permutation = Ultities.GetFeasiblePermutation(story, 0);

            PositionTable<int> permutationTable = new PositionTable<int>(characterCount, frameCount);
            for (int i = 0; i < characterCount; ++i)
                for (int j = 0; j < frameCount; ++j)
                    permutationTable[i, j] = -1;

            for (int id = 0; id < characterCount; ++id)
            {
                if (story.SessionTable[id, 0] != -1)
                    permutationTable[id, 0] = permutation[id];
            }

            permutationTable = (new EfficientBarycenterCalculator()).Calculate(story);

            for (int xx = 0; xx < 10; ++xx)
            {
                // forward
                for (int frame = 1; frame < frameCount; ++frame)
                {
                    Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                    for (int id = 0; id < characterCount; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1)
                        {
                            if (!dict.ContainsKey(story.SessionTable[id, frame]))
                            {
                                dict.Add(story.SessionTable[id, frame], new List<int>());
                            }
                            dict[story.SessionTable[id, frame]].Add(id);
                        }
                    }
                    List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                    foreach (KeyValuePair<int, List<int>> pair in dict)
                    {
                        int sum = 0;
                        int count = 0;
                        int sum_cur = 0;
                        foreach (int x in pair.Value)
                        {
                            if (story.SessionTable[x, frame - 1] != -1)
                            {
                                sum += permutationTable[x, frame - 1];
                                ++count;
                            }
                            else
                            {

                            }
                            sum_cur += permutationTable[x, frame];
                        }
                        double average = (double)sum_cur / pair.Value.Count;
                        if (count > 0)
                            average = (double)sum / count;
                        list.Add(new Tuple<int, double>(pair.Key, average));
                    }
                    //list = Ultities.GetRandomList<Tuple<int, double>>(list);
                    list.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                    int baseline = 0;
                    foreach (Tuple<int, double> tuple in list)
                    {
                        int group = tuple.Item1;
                        List<int> items = dict[group];
                        //items = Ultities.GetRandomList<int>(items);
                        items.Sort((a, b) => permutationTable[a, frame - 1].CompareTo(permutationTable[b, frame - 1]));
                        foreach (int x in items)
                        {
                            permutationTable[x, frame] = baseline++;
                        }
                    }
                }

                Debug.WriteLine("Forward:{0}", Crossing.Count(story, permutationTable));

                // backward
                for (int frame = frameCount - 2; frame >= 0; --frame)
                {
                    Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                    for (int id = 0; id < characterCount; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1)
                        {
                            if (!dict.ContainsKey(story.SessionTable[id, frame]))
                            {
                                dict.Add(story.SessionTable[id, frame], new List<int>());
                            }
                            dict[story.SessionTable[id, frame]].Add(id);
                        }
                    }
                    List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                    foreach (KeyValuePair<int, List<int>> pair in dict)
                    {
                        int sum = 0;
                        int count = 0;
                        int sum_cur = 0;
                        foreach (int x in pair.Value)
                        {
                            if (story.SessionTable[x, frame + 1] != -1)
                            {
                                sum += permutationTable[x, frame + 1];
                                ++count;
                            }
                            else
                            {

                            }
                            sum_cur += permutationTable[x, frame];
                        }
                        double average = (double)sum_cur / pair.Value.Count;
                        if (count > 0)
                            average = (double)sum / count;
                        list.Add(new Tuple<int, double>(pair.Key, average));
                    }
                    //list = Ultities.GetRandomList<Tuple<int, double>>(list);
                    list.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                    int baseline = 0;
                    foreach (Tuple<int, double> tuple in list)
                    {
                        int group = tuple.Item1;
                        List<int> items = dict[group];
                        //items = Ultities.GetRandomList<int>(items);
                        items.Sort((a, b) => permutationTable[a, frame + 1].CompareTo(permutationTable[b, frame + 1]));
                        foreach (int x in items)
                        {
                            permutationTable[x, frame] = baseline++;
                        }
                    }
                }
                Debug.WriteLine("Backward:{0}", Crossing.Count(story, permutationTable));
            }


            for (int xx = 0; xx < 10; ++xx)
            {
                for (int frame = 0; frame < frameCount; ++frame)
                {
                    Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                    for (int id = 0; id < characterCount; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1)
                        {
                            if (!dict.ContainsKey(story.SessionTable[id, frame]))
                            {
                                dict.Add(story.SessionTable[id, frame], new List<int>());
                            }
                            dict[story.SessionTable[id, frame]].Add(id);
                        }
                    }
                    List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                    foreach (KeyValuePair<int, List<int>> pair in dict)
                    {
                        int sum = 0;
                        int count = 0;
                        int sum_cur = 0;
                        foreach (int x in pair.Value)
                        {
                            if (frame > 0 && story.SessionTable[x, frame - 1] != -1)
                            {
                                sum += permutationTable[x, frame - 1];
                                ++count;
                            }
                            if (frame < frameCount - 1 && story.SessionTable[x, frame + 1] != -1)
                            {
                                sum += permutationTable[x, frame + 1];
                                ++count;
                            }
                            sum_cur += permutationTable[x, frame];
                        }
                        double average = (double)sum_cur / pair.Value.Count;
                        if (count > 0)
                            average = (double)sum / count;
                        list.Add(new Tuple<int, double>(pair.Key, average));
                    }
                    //list = Ultities.GetRandomList<Tuple<int, double>>(list);
                    list.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                    int baseline = 0;
                    foreach (Tuple<int, double> tuple in list)
                    {
                        int group = tuple.Item1;
                        List<int> items = dict[group];
                        //items = Ultities.GetRandomList<int>(items);
                        //items.Sort((a, b) => permutationTable[a, frame].CompareTo(permutationTable[b, frame]));
                        if (frame == 0)
                            items.Sort((a, b) => permutationTable[a, frame + 1].CompareTo(permutationTable[b, frame + 1]));
                        else
                            items.Sort((a, b) => permutationTable[a, frame - 1].CompareTo(permutationTable[b, frame - 1]));
                        foreach (int x in items)
                        {
                            permutationTable[x, frame] = baseline++;
                        }
                    }
                }

                // backward
                continue;
                for (int frame = frameCount - 1; frame >= 0; --frame)
                {
                    Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                    for (int id = 0; id < characterCount; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1)
                        {
                            if (!dict.ContainsKey(story.SessionTable[id, frame]))
                            {
                                dict.Add(story.SessionTable[id, frame], new List<int>());
                            }
                            dict[story.SessionTable[id, frame]].Add(id);
                        }
                    }
                    List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                    foreach (KeyValuePair<int, List<int>> pair in dict)
                    {
                        int sum = 0;
                        int count = 0;
                        int sum_cur = 0;
                        foreach (int x in pair.Value)
                        {
                            if (frame > 0 && story.SessionTable[x, frame - 1] != -1)
                            {
                                sum += permutationTable[x, frame - 1];
                                ++count;
                            }
                            if (frame < frameCount - 1 && story.SessionTable[x, frame + 1] != -1)
                            {
                                sum += permutationTable[x, frame + 1];
                                ++count;
                            }
                            sum_cur += permutationTable[x, frame];
                        }
                        double average = (double)sum_cur / pair.Value.Count;
                        if (count > 0)
                            average = (double)sum / count;
                        list.Add(new Tuple<int, double>(pair.Key, average));
                    }
                    //list = Ultities.GetRandomList<Tuple<int, double>>(list);
                    list.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                    int baseline = 0;
                    foreach (Tuple<int, double> tuple in list)
                    {
                        int group = tuple.Item1;
                        List<int> items = dict[group];
                        //items = Ultities.GetRandomList<int>(items);
                        //items.Sort((a, b) => permutationTable[a, frame].CompareTo(permutationTable[b, frame]));
                        if (frame == 0)
                            items.Sort((a, b) => permutationTable[a, frame + 1].CompareTo(permutationTable[b, frame + 1]));
                        else
                            items.Sort((a, b) => permutationTable[a, frame - 1].CompareTo(permutationTable[b, frame - 1]));
                        foreach (int x in items)
                        {
                            permutationTable[x, frame] = baseline++;
                        }
                    }
                }
            }


            //PositionTable<int> permutationTable = (new EfficientBarycenterCalculator()).Calculate(story);
            permutationTable = (new EfficientBarycenterCalculator()).Calculate(story);
            //permutationTable = (new CrossingMinimizedCalculator()).Calculate(story);

            for (int frame = 0; frame < frameCount; ++frame)
            {
                int max = -1;
                for (int id = 0; id < characterCount; ++id)
                {
                    if (permutationTable[id, frame] > max)
                        max = permutationTable[id, frame];
                }
                for (int id = 0; id < characterCount; ++id)
                {
                    if (permutationTable[id, frame] == -1)
                        permutationTable[id, frame] = ++max;
                }
            }

            Debug.WriteLine("Crossing:{0}", Crossing.Count(story, permutationTable));

            //PositionTable<int> ans = permutationTable.Clone<int>();
            //for (int frame = 0; frame < frameCount; ++frame)
            //{
            //    for (int i = 0; i < story.Characters.Count; ++i)
            //    {
            //        ans[i, frame] *= 5;
            //    }
            //}
            //return ans;

            {
                // calculate positions
                PositionTable<int> posY = new PositionTable<int>(characterCount, frameCount);

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

                return posY.Clone<double>();

                PositionTable<double> position = posY.Clone<double>();
                for (int frame = 0; frame < frameCount; ++frame)
                {
                    for (int i = 0; i < story.Characters.Count; ++i)
                    {
                        position[i, frame] /= 5;
                    }
                }


                PositionOptimizer3 optimizer2 = new PositionOptimizer3();


                PositionOptimizer2 optimizer = new PositionOptimizer2();
                position = optimizer.Optimize(story, position, 1.0, 0.05, 0.0, 0.0);


                PositionOptimizer1 optimizer1 = new PositionOptimizer1();


                int x = 0;
                while (x-- > 0)
                {
    
                    position = optimizer1.Optimize(story, position);
                    position = optimizer.Optimize(story, position, 1.0, 0.03, 0.0, 0.0);

                }

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

                return position;

            }
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