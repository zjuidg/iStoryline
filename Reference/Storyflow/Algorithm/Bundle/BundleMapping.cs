using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for bundlemapping
/// </summary>
/// 
namespace Algorithm.bundle
{
    public class BundleMapping
    {
        public int[,] bundleMap;
        public List<List<Tuple<int, int>>> bundleList;
        public List<List<int>> bundleSessionList;
        public BundleMapping(Story story, PositionTable<int> perm, PositionTable<int> segments)
        {
            bundleMap = new int[story.Characters.Count, story.FrameCount];
            bundleList = new List<List<Tuple<int, int>>>();
            bundleSessionList = new List<List<int>>();

            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                List<Tuple<int, int>> list = new List<Tuple<int, int>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        list.Add(new Tuple<int, int>(i, perm[i, frame]));
                    }
                }
                list.Sort((a, b) => a.Item2.CompareTo(b.Item2));

                List<List<int>> groups = new List<List<int>>();
                int last = -2;
                foreach (Tuple<int, int> tuple in list)
                {
                    if (story.SessionTable[tuple.Item1, frame] != last)
                    {
                        groups.Add(new List<int>());
                    }
                    groups.Last().Add(tuple.Item1);
                    last = story.SessionTable[tuple.Item1, frame];
                }
                foreach (List<int> group in groups)
                {
                    int bundleId = -1;
                    if (frame > 0)
                    {
                        foreach (int i in group)
                        {
                            if (segments[i, frame] == segments[i, frame - 1])
                            {
                                bundleId = bundleMap[i, frame - 1];
                                break;
                            }
                        }
                    }
                    if (bundleId == -1)
                    {
                        bundleSessionList.Add(new List<int>());
                        bundleList.Add(new List<Tuple<int, int>>());
                        bundleId = bundleList.Count - 1;
                    }
                    foreach (int i in group)
                    {
                        bundleMap[i, frame] = bundleId;
                        bundleList[bundleId].Add(new Tuple<int, int>(i, frame));
                        if (story.SessionTable[i, frame] == 23)
                            bundleId *= 1;
                        if (!bundleSessionList[bundleId].Contains(story.SessionTable[i, frame]))
                            bundleSessionList[bundleId].Add(story.SessionTable[i, frame]);
                    }
                }
            }
        }

        public int GetBundleBySegment(int id, int frame)
        {
            return bundleMap[id, frame];
        }

        public List<Tuple<int, int>> GetSegmentsByBundle(int bundle)
        {
            return bundleList[bundle];
        }

        public int BundleCount
        {
            get
            {
                return bundleList.Count;
            }
        }

        public double[] GetBundlePosition(Story story, PositionTable<double> position)
        {
            double[] result = new double[BundleCount];
            for (int i = 0; i < result.Length; ++i)
            {
                Tuple<int, int> tuple = GetSegmentsByBundle(i).First();
                //result[i] = position[tuple.Item1][tuple.Item2].Item3;
                result[i] = position[tuple.Item1, tuple.Item2];
            }
            return result;
        }
    }
}