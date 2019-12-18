using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for greedylocationreorder
/// </summary>
/// 
namespace Algorithm.locationreoder
{
    class GreedyLocationReorder
    {
        public int[,] locationCrossingTable;
        public Dictionary<int, int> locationHotness;

        public GreedyLocationReorder(Story story)
        {
            locationCrossingTable = new int[story.Locations.Count, story.Locations.Count];
            locationHotness = new Dictionary<int, int>();
            for (int id = 0; id < story.Characters.Count; id++)
            {
                for (int frame = 0; frame < story.FrameCount; frame++)
                {
                    if (story.SessionTable[id, frame] != -1)
                    {
                        if (!locationHotness.ContainsKey(story._sessionToLocation[story.SessionTable[id, frame]]))
                            locationHotness.Add(story._sessionToLocation[story.SessionTable[id, frame]], 0);
                        locationHotness[story._sessionToLocation[story.SessionTable[id, frame]]]++;
                    }
                }
            }
            for (int id = 0; id < story.Characters.Count; id++)
            {
                for (int frame = 0; frame < story.FrameCount - 1; frame++)
                {
                    if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1 && story._sessionToLocation[story.SessionTable[id, frame]] != story._sessionToLocation[story.SessionTable[id, frame + 1]])
                    {
                        locationCrossingTable[story._sessionToLocation[story.SessionTable[id, frame]], story._sessionToLocation[story.SessionTable[id, frame + 1]]]++;
                        locationCrossingTable[story._sessionToLocation[story.SessionTable[id, frame + 1]], story._sessionToLocation[story.SessionTable[id, frame]]]++;
                    }
                }
            }
        }

        public void Reorder(Story story)
        {
            Dfs(story, story.LocationRoot);
        }

        private void Dfs(Story story, LocationNode root)
        {
            foreach (LocationNode child in root.Children)
            {
                Dfs(story, child);
            }
            List<int> locationList = new List<int>();
            for (int i = 0; i < root.Children.Count; i++)
            {


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