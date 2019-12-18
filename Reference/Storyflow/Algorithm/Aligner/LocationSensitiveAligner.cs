using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for locationsensitivealigner
/// </summary>
/// 
namespace Algorithm.aligner
{
    class LocationSensitiveAligner : IAligner
    {
        public PositionTable<int> Align(Story story, PositionTable<int> permutation)
        {
            // location tree => location constraint list

            List<int> locationList = new List<int>();
            BuildLocationList(story, story.LocationRoot, locationList);
            int[] invertedLocationList = new int[locationList.Count];
            for (int i = 0; i < locationList.Count; ++i)
            {
                invertedLocationList[locationList[i]] = i;
            }

            // initialize segments to -1 or individual value
            PositionTable<int> segments = new PositionTable<int>(story.Characters.Count, story.FrameCount);
            int x = 0;
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                        segments[i, frame] = x++;
                    else
                        segments[i, frame] = -1;
                }
            }

            //for (int i = 0; i < story.Characters.Count; ++i)
            //{
            //    for (int j = 0; j < story.FrameCount; ++j)
            //        Console.Write(segments[i, j] + ", ");
            //    Console.WriteLine();
            //}

            // calculate alignmnet
            for (int frame = 0; frame < story.FrameCount - 1; ++frame)
            {
                int leftFrame = frame;
                int rightFrame = frame + 1;
                // iterate from different locations
                for (int li = 0; li < locationList.Count; ++li)
                {
                    int locationId = locationList[li];
                    List<List<int>> leftSessions = BuildSessionList(story, permutation, leftFrame, locationId);
                    List<List<int>> rightSessions = BuildSessionList(story, permutation, rightFrame, locationId);
                    double[,] dp = new double[leftSessions.Count + 1, rightSessions.Count + 1];
                    Tuple<int, int>[,] path = new Tuple<int, int>[leftSessions.Count + 1, rightSessions.Count + 1];
                    for (int i = 0; i < leftSessions.Count; ++i)
                    {
                        for (int j = 0; j < rightSessions.Count; ++j)
                        {
                            dp[i + 1, j + 1] = -1;
                            Tuple<int, int, int, double> tuple = GetMaximumMatch(story, leftSessions[i], rightSessions[j]);
                            if (dp[i + 1, j + 1] < dp[i, j] + tuple.Item4)
                            {
                                dp[i + 1, j + 1] = dp[i, j] + tuple.Item4;
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
                        int i = leftSessions.Count - 1;
                        int j = rightSessions.Count - 1;
                        while (i > -1 && j > -1)
                        {
                            if (path[i + 1, j + 1].Item1 == i && path[i + 1, j + 1].Item2 == j)
                            {
                                //segments[j, rightFrame] = segments[i, leftFrame];
                                Tuple<int, int, int, double> tuple = GetMaximumMatch(story, leftSessions[i], rightSessions[j]);
                                for (int k = 0; k < tuple.Item3; ++k)
                                {
                                    segments[rightSessions[j][tuple.Item2 + k], rightFrame] = segments[leftSessions[i][tuple.Item1 + k], leftFrame];
                                }
                            }
                            Tuple<int, int> pair = path[i + 1, j + 1];
                            i = pair.Item1 - 1;
                            j = pair.Item2 - 1;
                        }
                    }
                }
            }

            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int j = 0; j < story.FrameCount; ++j)
                    Console.Write(segments[i, j] + ", ");
                Console.WriteLine();
            }

            return segments;
        }

        // TODO(Enxun):1.add line weight 2.add space effciency volumn 3.transform int to double weight
        // 1, 3 added
        private Tuple<int, int, int, double> GetMaximumMatch(Story story, List<int> a, List<int> b)
        {
            double[,] dp = new double[a.Count + 1, b.Count + 1];
            double max = 0;
            int ea = -1;
            int eb = -1;
            for (int i = 0; i < a.Count; ++i)
            {
                for (int j = 0; j < b.Count; ++j)
                {
                    if (a[i] == b[j])
                    {
                        //dp[i + 1, j + 1] = dp[i, j] + 1;
                        dp[i + 1, j + 1] = dp[i, j] + story.Characters[a[i]].Weight;
                        if (max < dp[i + 1, j + 1])
                        {
                            max = dp[i + 1, j + 1];
                            ea = i;
                            eb = j;
                        }
                    }
                }
            }
            int len = 0;
            while (ea - len >= 0 && eb - len >= 0 && a[ea - len] == b[eb - len])
                ++len;

            return new Tuple<int, int, int, double>(ea - len + 1, eb - len + 1, len, dp[ea + 1, eb + 1]);
        }

        private List<List<int>> BuildSessionList(Story story, PositionTable<int> permutation, int frame, int location)
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            List<List<int>> list = new List<List<int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] != -1 && story.GetLocationId(story.SessionTable[i, frame]) == location)
                {
                    if (!dict.ContainsKey(story.SessionTable[i, frame]))
                    {
                        list.Add(new List<int>());
                        dict.Add(story.SessionTable[i, frame], list.Count - 1);
                    }
                    int j = dict[story.SessionTable[i, frame]];
                    list[j].Add(i);
                }
            }
            foreach (List<int> l in list)
            {
                l.Sort((a, b) =>
                {
                    return permutation[a, frame].CompareTo(permutation[b, frame]);
                });
            }
            list.Sort((a, b) =>
            {
                return permutation[a[0], frame].CompareTo(permutation[b[0], frame]);
            });
            return list;
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