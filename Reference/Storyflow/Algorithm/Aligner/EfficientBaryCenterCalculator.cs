using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Algorithm.PermutationCalculator;

/// <summary>
/// Summary description for efficientbarycentercalculator
/// </summary>
/// 
namespace Algorithm.aligner
{
    class EfficientBarycenterCalculator : IPermutationCalculator
    {
        public PositionTable<int> Calculate(Story story)
        {
            int frameCount = story.TimeStamps.Length - 1;
            int characterCount = story.Characters.Count;

            int[] permutation = Ultities.GetFeasiblePermutation(story, 0);
            //int[] permutation = Ultities.GetRandomFeasiblePermutation(story, 0);

            PositionTable<int> permutationTable = new PositionTable<int>(characterCount, frameCount);
            for (int i = 0; i < characterCount; ++i)
                for (int j = 0; j < frameCount; ++j)
                    permutationTable[i, j] = -1;

            for (int id = 0; id < characterCount; ++id)
            {
                if (story.SessionTable[id, 0] != -1)
                    permutationTable[id, 0] = permutation[id];
            }

            bool[] available = new bool[frameCount];
            available[0] = true;
            bool change = false;
            do
            {
                change = false;
                for (int frame = 0; frame < frameCount; ++frame)
                {
                    if (frame == 0 && (frameCount == 1 || !available[1]))
                        continue;
                    available[frame] = true;

                    int[] backup = new int[characterCount];
                    for (int i = 0; i < characterCount; ++i)
                        backup[i] = permutationTable[i, frame];

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
                        double barycenter1 = -1;
                        double barycenter2 = -1;
                        if (frame > 0 && available[frame - 1])
                        {
                            double sum = 0;
                            int count = 0;
                            foreach (int x in pair.Value)
                            {
                                if (story.SessionTable[x, frame - 1] != -1)
                                {
                                    sum += permutationTable[x, frame - 1];
                                    ++count;
                                }
                            }
                            if (count > 0)
                                barycenter1 = sum / count;
                        }
                        if (frame < frameCount - 1 && available[frame + 1])
                        {
                            double sum = 0;
                            int count = 0;
                            foreach (int x in pair.Value)
                            {
                                if (story.SessionTable[x, frame + 1] != -1)
                                {
                                    sum += permutationTable[x, frame + 1];
                                    ++count;
                                }
                            }
                            if (count > 0)
                                barycenter2 = sum / count;
                        }
                        double weight = -1;
                        if (barycenter1 == -1)
                            weight = barycenter2;
                        else if (barycenter2 == -1)
                            weight = barycenter1;
                        else
                            weight = 0.5 * barycenter1 + 0.5 * barycenter2;

                        list.Add(new Tuple<int, double>(pair.Key, weight));
                    }
                    list.Sort((a, b) =>
                    {
                        if (a.Item2 == b.Item2)
                            return dict[a.Item1].First().CompareTo(dict[b.Item1].First());
                        return a.Item2.CompareTo(b.Item2);
                    });

                    int baseline = 0;
                    foreach (Tuple<int, double> tuple in list)
                    {
                        int group = tuple.Item1;
                        List<int> items = dict[group];
                        if (frame == 0)
                            items.Sort((a, b) => permutationTable[a, frame + 1].CompareTo(permutationTable[b, frame + 1]));
                        else
                            items.Sort((a, b) => permutationTable[a, frame - 1].CompareTo(permutationTable[b, frame - 1]));
                        foreach (int x in items)
                        {
                            permutationTable[x, frame] = baseline++;
                        }
                    }

                    for (int i = 0; i < characterCount; ++i)
                    {
                        if (backup[i] != permutationTable[i, frame])
                        {
                            change = true;
                        }
                    }
                }
                Console.WriteLine("Crossing:{0}", Crossing.Count(story, permutationTable));
            }
            while (change);

            return permutationTable;
        }
    }
}