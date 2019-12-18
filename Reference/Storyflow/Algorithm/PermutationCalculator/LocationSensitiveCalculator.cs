using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;

/// <summary>
/// Summary description for locationsensitivecalculator
/// </summary>
/// 
namespace Algorithm.PermutationCalculator
{
    class LocationSensitiveCalculator : IPermutationCalculator
    {
        public PositionTable<int> Calculate(Story story)
        {
            // location tree => location constraint list

            List<int> locationList = new List<int>();
            BuildLocationList(story, story.LocationRoot, locationList);
            int[] invertedLocationList = new int[locationList.Count];
            for (int i = 0; i < locationList.Count; ++i)
            {
                invertedLocationList[locationList[i]] = i;
            }

            PositionTable<int> perm = new PositionTable<int>(story.Characters.Count, story.FrameCount);
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int j = 0; j < story.FrameCount; ++j)
                    perm[i, j] = -1;

            CalculateInitialFrame(story, perm, invertedLocationList, 0);

            // sweep forward
            for (int frame = 1; frame < story.FrameCount; ++frame)
            {
                RecalculateFrame(story, perm, invertedLocationList, frame, frame - 1);
            }
            for (int i = 0; i < 10; ++i)
            {
                for (int frame = story.FrameCount - 2; frame >= 0; --frame)
                {
                    RecalculateFrameFreeSideRemembered(story, perm, invertedLocationList, frame, frame + 1);
                }
                for (int frame = 1; frame < story.FrameCount; ++frame)
                {
                    RecalculateFrameFreeSideRemembered(story, perm, invertedLocationList, frame, frame - 1);
                }
            }
            for (int frame = story.FrameCount - 2; frame >= 0; --frame)
            {
                RecalculateFrameFreeSideRemembered(story, perm, invertedLocationList, frame, frame + 1);
            }



            Debug.WriteLine("Crossing:{0}", Crossing.Count(story, perm));
            return perm;
        }
        private void CalculateInitialFrame(Story story, PositionTable<int> perm, int[] inverted, int frame)
        {
            List<Tuple<int, int>> tempList = new List<Tuple<int, int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1)
                {
                    tempList.Add(new Tuple<int, int>(inverted[story.GetLocationId(story.SessionTable[i, frame])], i));
                }
            }
            tempList.Sort((a, b) =>
            {
                if (a.Item1 != b.Item1)
                    return a.Item1.CompareTo(b.Item1);
                else
                    return a.Item2.CompareTo(b.Item2);
            });
            for (int i = 0; i < tempList.Count; ++i)
            {
                Tuple<int, int> tuple = tempList[i];
                perm[tuple.Item2, frame] = i;
            }
        }

        private void RecalculateFrameFreeSideRemembered(Story story, PositionTable<int> perm, int[] inverted, int frame, int refer)
        {
            // first calculate the barycenter
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1)
                {
                    if (!dict.ContainsKey(story.SessionTable[i, frame]))
                    {
                        dict.Add(story.SessionTable[i, frame], new List<int>());
                    }
                    dict[story.SessionTable[i, frame]].Add(i);
                }
            }
            List<Tuple<int, double>> bclist = new List<Tuple<int, double>>();
            foreach (KeyValuePair<int, List<int>> pair in dict)
            {
                double barycenter = -1;
                double sum = 0;
                int count = 0;
                foreach (int x in pair.Value)
                {
                    if (story.SessionTable[x, refer] != -1)
                    {
                        sum += perm[x, refer];
                        ++count;
                    }
                }
                if (count > 0)
                    barycenter = sum / count;
                else
                    barycenter = 1e6;
                // TODO(Enxun): if count=0 how to handle?
                bclist.Add(new Tuple<int, double>(pair.Key, barycenter));
            }
            bclist.Sort((a, b) =>
            {
                if (a.Item2 != b.Item2)
                    return a.Item2.CompareTo(b.Item2);
                int diff = 0;
                foreach (int x in dict[a.Item1])
                {
                    if (story.SessionTable[x, refer] != -1)
                    {
                        foreach (int y in dict[b.Item1])
                        {
                            if (story.SessionTable[y, refer] != -1)
                            {
                                if (perm[x, refer] > perm[y, refer])
                                    ++diff;
                                else if (perm[x, refer] < perm[y, refer])
                                    --diff;
                            }
                        }
                    }
                }
                if (diff > 0)
                    return 1;
                else if (diff < 0)
                    return -1;
                else
                    return 0;
            });
            Dictionary<int, double> bcDict = new Dictionary<int, double>();//bclist.ToDictionary(p => p.Item1, p => p.Item2);
            for (int i = 0; i < bclist.Count; ++i)
            {
                Tuple<int, double> tuple = bclist[i];
                if (tuple.Item2 == 1e6)
                    bcDict.Add(tuple.Item1, -1);
                else
                    bcDict.Add(tuple.Item1, i);
            }

            // sorting
            List<Tuple<int, double, int, int, int>> tupleList = new List<Tuple<int, double, int, int, int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1)
                {
                    tupleList.Add(new Tuple<int, double, int, int, int>(
                        inverted[story.GetLocationId(story.SessionTable[i, frame])],
                        bcDict[story.SessionTable[i, frame]],
                        perm[i, refer],
                        perm[i, frame],
                        i));
                }
            }
            tupleList.Sort((a, b) =>
            {
                if (a.Item1 != b.Item1)
                    return a.Item1.CompareTo(b.Item1);
                if (a.Item2 != -1 && b.Item2 != -1 && a.Item2 != b.Item2)
                    return a.Item2.CompareTo(b.Item2);
                if (a.Item3 != -1 && b.Item3 != -1 && a.Item3 != b.Item3)
                    return a.Item3.CompareTo(b.Item3);
                if (a.Item4 != -1 && b.Item4 != -1 && a.Item4 != b.Item4)
                    return a.Item4.CompareTo(b.Item4);
                return 0;
                //return a.Item5.CompareTo(b.Item5);
            });
            for (int i = 0; i < tupleList.Count; ++i)
            {
                Tuple<int, double, int, int, int> tuple = tupleList[i];
                perm[tuple.Item5, frame] = i;
            }
        }

        private void RecalculateFrame(Story story, PositionTable<int> perm, int[] inverted, int frame, int refer)
        {
            // first calculate the barycenter
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1)
                {
                    if (!dict.ContainsKey(story.SessionTable[i, frame]))
                    {
                        dict.Add(story.SessionTable[i, frame], new List<int>());
                    }
                    dict[story.SessionTable[i, frame]].Add(i);
                }
            }
            List<Tuple<int, double>> bclist = new List<Tuple<int, double>>();
            foreach (KeyValuePair<int, List<int>> pair in dict)
            {
                double barycenter = -1;
                double sum = 0;
                int count = 0;
                foreach (int x in pair.Value)
                {
                    if (story.SessionTable[x, refer] != -1)
                    {
                        sum += perm[x, refer];
                        ++count;
                    }
                }
                if (count > 0)
                    barycenter = sum / count;
                else
                    barycenter = 1e6;
                // TODO(Enxun): if count=0 how to handle?
                bclist.Add(new Tuple<int, double>(pair.Key, barycenter));
            }
            bclist.Sort((a, b) =>
            {
                if (a.Item2 != b.Item2)
                    return a.Item2.CompareTo(b.Item2);
                int diff = 0;
                foreach (int x in dict[a.Item1])
                {
                    if (story.SessionTable[x, refer] != -1)
                    {
                        foreach (int y in dict[b.Item1])
                        {
                            if (story.SessionTable[y, refer] != -1)
                            {
                                if (perm[x, refer] > perm[y, refer])
                                    ++diff;
                                else if (perm[x, refer] < perm[y, refer])
                                    --diff;
                            }
                        }
                    }
                }
                if (diff > 0)
                    return 1;
                else if (diff < 0)
                    return -1;
                else
                    return 0;
            });
            Dictionary<int, double> bcDict = new Dictionary<int, double>();//bclist.ToDictionary(p => p.Item1, p => p.Item2);
            for (int i = 0; i < bclist.Count; ++i)
            {
                Tuple<int, double> tuple = bclist[i];
                bcDict.Add(tuple.Item1, i);
            }

            // sorting
            List<Tuple<int, double, int, int>> tupleList = new List<Tuple<int, double, int, int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1)
                {
                    tupleList.Add(new Tuple<int, double, int, int>(
                        inverted[story.GetLocationId(story.SessionTable[i, frame])],
                        bcDict[story.SessionTable[i, frame]],
                        perm[i, refer],
                        i));
                }
            }
            tupleList.Sort((a, b) =>
            {
                if (a.Item1 != b.Item1)
                    return a.Item1.CompareTo(b.Item1);
                if (a.Item2 != b.Item2)
                    return a.Item2.CompareTo(b.Item2);
                if (a.Item3 != b.Item3)
                    return a.Item3.CompareTo(b.Item3);
                return a.Item4.CompareTo(b.Item4);
            });
            for (int i = 0; i < tupleList.Count; ++i)
            {
                Tuple<int, double, int, int> tuple = tupleList[i];
                perm[tuple.Item4, frame] = i;
            }
        }

        private void BuildLocationList(Story story, LocationNode node, List<int> locationList)
        {
            if (node.Id != -1)
            {
                locationList.Add(node.Id);
            }
            foreach (LocationNode childNode in node.Children)
            {
                BuildLocationList(story, childNode, locationList);
            }
        }
    }
}