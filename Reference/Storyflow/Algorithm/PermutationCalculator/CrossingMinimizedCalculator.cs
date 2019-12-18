using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Algorithm.aligner;

/// <summary>
/// Summary description for crossingminimizedcalculator
/// </summary>
/// 
namespace Algorithm.PermutationCalculator
{
    class CrossingMinimizedCalculator : IPermutationCalculator
    {
        public PositionTable<int> Calculate(Story story)
        {
            PositionTable<int> ans = null;

            int[] permutation = Ultities.GetFeasiblePermutation(story, 0);
            PositionTable<int> perm = new PositionTable<int>(story.Characters.Count, story.TimeStamps.Length - 1);
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int j = 0; j < story.TimeStamps.Length - 1; ++j)
                    perm[i, j] = -1;

            for (int id = 0; id < story.Characters.Count; ++id)
            {
                if (story.SessionTable[id, 0] != -1)
                    perm[id, 0] = permutation[id];
            }

            Sweep(story, ref ans, perm, true, true);

            EfficientBarycenterCalculator calc1 = new EfficientBarycenterCalculator();
            perm = calc1.Calculate(story);
            Update(story, ref ans, perm);

            Sweep(story, ref ans, perm, true, true);

            Sweep(story, ref ans, perm, false, true);

            return ans;
        }

        private void Sweep(Story story, ref PositionTable<int> ans, PositionTable<int> perm, bool forward, bool backward)
        {
            int frameCount = story.TimeStamps.Length - 1;
            int characterCount = story.Characters.Count;

            PositionTable<int> backup1 = null;
            PositionTable<int> backup2 = null;
            bool change = false;
            int loop = 0;
            do
            {
                change = false;

                if (forward)
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
                                    sum += perm[x, frame - 1];
                                    ++count;
                                }
                                else
                                {

                                }
                                sum_cur += perm[x, frame];
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
                            items.Sort((a, b) => perm[a, frame - 1].CompareTo(perm[b, frame - 1]));
                            foreach (int x in items)
                            {
                                perm[x, frame] = baseline++;
                            }
                        }
                    }
                    Update(story, ref ans, perm);
                    if (backup1 == null || !backup1.Equals(perm))
                    {
                        backup1 = perm.Clone<int>();
                        change = true;
                    }
                }

                if (backward)
                {
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
                                    sum += perm[x, frame + 1];
                                    ++count;
                                }
                                else
                                {

                                }
                                sum_cur += perm[x, frame];
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
                            items.Sort((a, b) => perm[a, frame + 1].CompareTo(perm[b, frame + 1]));
                            foreach (int x in items)
                            {
                                perm[x, frame] = baseline++;
                            }
                        }
                    }
                    Update(story, ref ans, perm);

                    if (backup2 == null || !backup2.Equals(perm))
                    {
                        backup2 = perm.Clone<int>();
                        change = true;
                    }
                }
            }
            while (change && loop++ < 100);
        }

        private void Update(Story story, ref PositionTable<int> ans, PositionTable<int> perm)
        {
            if (ans == null)
            {
                ans = perm.Clone<int>();
            }
            else if (Crossing.Count(story, ans) > Crossing.Count(story, perm))
            {
                ans = perm.Clone<int>();
            }
        }
    }
}