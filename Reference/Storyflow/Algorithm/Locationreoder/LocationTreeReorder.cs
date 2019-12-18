using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for locationtreereorder
/// </summary>
/// 
namespace Algorithm.locationreoder
{
    class LocationTreeReorderer
    {
        public void Reorder(Story story)
        {
            Dfs(story, story.LocationRoot);
        }

        private void Dfs(Story story, LocationNode root)
        {
            Dictionary<int, int> sessionToLocation = new Dictionary<int, int>();
            List<Tuple<int, int>> sizeList = new List<Tuple<int, int>>();
            foreach (LocationNode child in root.Children)
            {
                Dfs(story, child);
                sizeList.Add(new Tuple<int, int>(child.Id, 0));
            }
            for (int id = 0; id < story.Characters.Count; ++id)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[id, frame] != -1)
                    {
                        for (int i = 0; i < sizeList.Count; ++i)
                        {
                            Tuple<int, int> tuple = sizeList[i];
                            if (UnderLocation(story, story.SessionTable[id, frame], tuple.Item1))
                            {
                                sizeList[i] = new Tuple<int, int>(tuple.Item1, tuple.Item2 + 1);
                                if (!sessionToLocation.ContainsKey(story.SessionTable[id, frame]))
                                {
                                    sessionToLocation.Add(story.SessionTable[id, frame], tuple.Item1);
                                }
                                break;
                            }
                        }

                    }
                }
            }
            sizeList = sizeList.OrderByDescending(a => a.Item2).ToList();

            List<int> locationList = new List<int>();
            for (int i = 0; i < sizeList.Count; ++i)
            {
                int crossing = int.MaxValue;
                List<int> best = null;
                for (int j = 0; j <= locationList.Count; ++j)
                {
                    List<int> temp = new List<int>(locationList);
                    temp.Insert(j, sizeList[i].Item1);
                    int t = GetCrossing(story, sessionToLocation, temp);
                    if (crossing > t)
                    {
                        crossing = t;
                        best = temp;
                    }
                }
                locationList = best;
            }

            Dictionary<int, int> perm = new Dictionary<int, int>();
            int count = 0;
            foreach (int x in locationList)
            {
                perm.Add(x, count++);
            }
            root.Children = root.Children.OrderBy(a => perm[a.Id]).ToList();
        }

        private int GetLocation(int sessionId, Dictionary<int, int> sessionToLocation)
        {
            if (!sessionToLocation.ContainsKey(sessionId))
                return -1;
            return sessionToLocation[sessionId];
        }

        private bool UnderLocation(Story story, int sessionId, int locationId)
        {
            int loc = story.GetLocationId(sessionId);
            while (true)
            {
                if (loc == locationId)
                    return true;
                if (story.Locations[loc].Parent == -1)
                    break;
                loc = story.Locations[loc].Parent;
            }
            return false;
        }

        private int GetCrossing(Story story, Dictionary<int, int> sessionToLocation, List<int> list)
        {
            Dictionary<int, int> perm = new Dictionary<int, int>();
            for (int i = 0; i < list.Count; ++i)
            {
                perm.Add(list[i], i);
            }
            int crossing = 0;
            for (int frame = 0; frame < story.FrameCount - 1; ++frame)
            {
                int left = frame;
                int right = frame + 1;
                for (int i = 0; i < story.Characters.Count; ++i)
                {

                    if (story.SessionTable[i, left] != -1 && story.SessionTable[i, right] != -1)
                    {
                        int loc_il = GetLocation(story.SessionTable[i, left], sessionToLocation);
                        int loc_ir = GetLocation(story.SessionTable[i, right], sessionToLocation);
                        if (loc_il != -1 && loc_ir != -1 && perm.ContainsKey(loc_il) && perm.ContainsKey(loc_ir))
                        {
                            for (int j = i + 1; j < story.Characters.Count; ++j)
                            {
                                if (story.SessionTable[j, left] != -1 && story.SessionTable[j, right] != -1)
                                {
                                    int loc_jl = GetLocation(story.SessionTable[j, left], sessionToLocation);
                                    int loc_jr = GetLocation(story.SessionTable[j, right], sessionToLocation);
                                    if (loc_jl != -1 && loc_jr != -1 && perm.ContainsKey(loc_jl) && perm.ContainsKey(loc_jr))
                                    {
                                        if ((perm[loc_il] - perm[loc_jl]) * (perm[loc_ir] - perm[loc_jr]) < 0)
                                        {
                                            ++crossing;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return crossing;
        }
    }
}