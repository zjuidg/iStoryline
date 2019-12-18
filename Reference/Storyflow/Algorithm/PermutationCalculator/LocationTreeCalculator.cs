using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;

/// <summary>
/// Summary description for locationtreecalculator
/// </summary>
/// 
namespace Algorithm.PermutationCalculator
{
    class LocationTreeCalculator : IPermutationCalculator
    {
        //static Random rand = new Random(DateTime.Now.Millisecond);
        public PositionTable<int> Calculate(Story story)
        {
            PositionTable<int> perm = new PositionTable<int>(story.Characters.Count, story.FrameCount);
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int j = 0; j < story.FrameCount; ++j)
                    perm[i, j] = -1;

            // calculate lines in sessions dictionary to speed up the calculation
            Dictionary<int, List<int>>[] linesInSessions = new Dictionary<int, List<int>>[story.FrameCount];
            Dictionary<int, List<int>>[] sessionsInLocations = new Dictionary<int, List<int>>[story.FrameCount];
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                linesInSessions[frame] = new Dictionary<int, List<int>>();
                sessionsInLocations[frame] = new Dictionary<int, List<int>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        int sessionId = story.SessionTable[i, frame];
                        if (!linesInSessions[frame].ContainsKey(sessionId))
                        {
                            linesInSessions[frame].Add(sessionId, new List<int>());
                            int locationId = story.GetLocationId(sessionId);
                            if (!sessionsInLocations[frame].ContainsKey(locationId))
                            {
                                sessionsInLocations[frame].Add(locationId, new List<int>());
                            }
                            sessionsInLocations[frame][locationId].Add(sessionId);
                        }
                        linesInSessions[frame][sessionId].Add(i);
                    }
                }
            }

            CalculateFrame(story, linesInSessions, sessionsInLocations, perm, 0, -1);
            for (int i = 0; i < 40; ++i)
            {
                for (int frame = 1; frame < story.FrameCount; ++frame)
                {
                    CalculateFrame(story, linesInSessions, sessionsInLocations, perm, frame, frame - 1);
                }
                for (int frame = story.FrameCount - 2; frame >= 0; --frame)
                {
                    CalculateFrame(story, linesInSessions, sessionsInLocations, perm, frame, frame + 1);
                }
            }

            var permBackup = perm;
            perm = new PositionTable<int>(story.Characters.Count, story.FrameCount);
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int j = 0; j < story.FrameCount; ++j)
                    perm[i, j] = -1;

            CalculateFrame(story, linesInSessions, sessionsInLocations, perm, story.FrameCount - 1, story.FrameCount - 2);
            for (int i = 0; i < 40; ++i)
            {
                for (int frame = story.FrameCount - 2; frame >= 0; --frame)
                {
                    CalculateFrame(story, linesInSessions, sessionsInLocations, perm, frame, frame + 1);
                }
                for (int frame = 1; frame < story.FrameCount; ++frame)
                {
                    CalculateFrame(story, linesInSessions, sessionsInLocations, perm, frame, frame - 1);
                }
            }

            int backwardCrossing = Crossing.Count(story, perm);
            int forwardCrossing = Crossing.Count(story, permBackup);
            //Console.WriteLine("Forward:{0},Backward:{1}", forwardCrossing, backwardCrossing);
            if (forwardCrossing < backwardCrossing)
                return permBackup;
            else
                return perm;
        }

        //
        private void CalculateFrame(Story story, Dictionary<int, List<int>>[] linesInSessions, Dictionary<int, List<int>>[] sessionsInLocations, PositionTable<int> perm, int frame, int reference)
        {
            Tuple<double, List<int>> tuple = DfsLocationSubtree(story, linesInSessions, sessionsInLocations, perm, story.LocationRoot, frame, reference);
            List<int> lineList = tuple.Item2;
            for (int i = 0; i < lineList.Count; ++i)
            {
                int x = lineList[i];
                perm[x, frame] = i;
            }
        }

        private Tuple<double, List<int>> DfsLocationSubtree(Story story, Dictionary<int, List<int>>[] linesInSessions, Dictionary<int, List<int>>[] sessionsInLocations, PositionTable<int> perm, LocationNode node, int frame, int reference)
        {
            List<Tuple<double, List<int>, int>> list = new List<Tuple<double, List<int>, int>>();
            Dictionary<int, int> sublocation = new Dictionary<int, int>();
            //List<double> bclist = new List<double>();
            foreach (LocationNode childNode in node.Children)
            {
                Tuple<double, List<int>> subTuple = DfsLocationSubtree(story, linesInSessions, sessionsInLocations, perm, childNode, frame, reference);
                // 每一个结点的排序结果，<barycenterVal, sortedList, locationId>
                Tuple<double, List<int>, int> tuple = new Tuple<double, List<int>, int>(subTuple.Item1, subTuple.Item2, childNode.Id);
                if (tuple.Item2.Count > 0)
                {
                    list.Add(tuple);
                    //bclist.Add(tuple.Item1);
                    sublocation.Add(childNode.Id, sublocation.Count);
                }
            }
            //bclist.Sort();
            //for (int i = 0; i < list.Count; ++i)
            //{
            //    list[i] = new Tuple<double, List<int>, int>(bclist[i], list[i].Item2, list[i].Item3);
            //}

            if (sessionsInLocations[frame].ContainsKey(node.Id))
            {
                foreach (int sessionId in sessionsInLocations[frame][node.Id])
                {
                    Tuple<double, List<int>> sessionTuple = DfsSessionSubtree(story, linesInSessions, perm, sessionId, frame, reference);
                    list.Add(new Tuple<double, List<int>, int>(sessionTuple.Item1, sessionTuple.Item2, -1));
                }
            }
            if (reference == -1)
            {
                list.Sort((a, b) =>
                {
                    if (a.Item3 != -1 && b.Item3 != -1)
                        return sublocation[a.Item3].CompareTo(sublocation[b.Item3]);
                    return a.Item2[0].CompareTo(b.Item2[0]);
                });
            }
            else
            {
                StableSort(list, perm, frame, sublocation);
                #region conment
                #endregion
            }
            List<int> lineList = new List<int>();
            foreach (Tuple<double, List<int>, int> tuple in list)
            {
                lineList.AddRange(tuple.Item2);
            }
            Tuple<double, List<int>> result;
            if (reference != -1)
                result = new Tuple<double, List<int>>(GetBarycenter(perm, lineList, reference), lineList);
            else
                result = new Tuple<double, List<int>>(-1, lineList);
            return result;
        }

        private int Comp(Tuple<double, List<int>, int> a, Tuple<double, List<int>, int> b, PositionTable<int> perm, int frame, Dictionary<int, int> sublocation)
        {
            //return a.Item1.CompareTo(b.Item1);
            Debug.Assert(a.Item2[0] != b.Item2[0]);
            // if both location, fit location requirement first
            if (a.Item3 != -1 && b.Item3 != -1)
            {
                Debug.Assert(a.Item3 != b.Item3);
                return sublocation[a.Item3].CompareTo(sublocation[b.Item3]);
            }
            // compare barycenter if both have
            if (a.Item1 != -1 && b.Item1 != -1 && a.Item1 != b.Item1)
            {
                Debug.Assert(a.Item1 != b.Item1);
                return a.Item1.CompareTo(b.Item1);
            }
            // if either one hasn't, try to compare current frame position
            if (perm[a.Item2[0], frame] != -1 && perm[b.Item2[0], frame] != -1)
            {
                Debug.Assert(perm[a.Item2[0], frame] != perm[b.Item2[0], frame]);
                return perm[a.Item2[0], frame].CompareTo(perm[b.Item2[0], frame]);
            }
            return a.Item2[0].CompareTo(b.Item2[0]);
        }

        private void StableSort(List<Tuple<double, List<int>, int>> list, PositionTable<int> perm, int frame, Dictionary<int, int> sublocation)
        {
            for (int i = list.Count - 2; i >= 0; --i)
                for (int j = 0; j <= i; ++j)
                {
                    if (Comp(list[j], list[j + 1], perm, frame, sublocation) > 0)
                    {
                        var t = list[j];
                        list[j] = list[j + 1];
                        list[j + 1] = t;
                    }
                }
        }

        private Tuple<double, List<int>> DfsSessionSubtree(Story story, Dictionary<int, List<int>>[] linesInSessions, PositionTable<int> perm, int sessionId, int frame, int reference)
        {
            List<int> lineList = new List<int>(linesInSessions[frame][sessionId]);
            double barycenter;
            if (reference == -1)
            {
                lineList.Sort((a, b) => a.CompareTo(b));
                barycenter = -1;
            }
            else
            {
                lineList.Sort((a, b) =>
                {
                    // if reference existing and both two lines existing at reference
                    if (perm[a, reference] != -1 && perm[b, reference] != -1) // -> (story.Session[a, reference] != -1 && story.Session[b, ref] != -1)
                        return perm[a, reference].CompareTo(perm[b, reference]);
                    // either of reference perm = -1, we compare current frame if they have
                    if (perm[a, frame] != -1 && perm[b, frame] != -1)
                        return perm[a, frame].CompareTo(perm[b, frame]);
                    // put new starting one at bottom
                    if (perm[a, reference] != perm[b, reference]) // both not equal to -1
                        return perm[b, reference].CompareTo(perm[a, reference]); // tricky: i swap the pair here so that -1 will dominate
                                                                                 //return perm[a, reference].CompareTo(perm[b, reference]);
                    return a.CompareTo(b);
                });
                barycenter = GetBarycenter(perm, lineList, reference);
            }
            return new Tuple<double, List<int>>(barycenter, lineList);
        }

        private double GetBarycenter(PositionTable<int> perm, List<int> list, int frame)
        {
            double sum = 0;
            int count = 0;
            foreach (int x in list)
            {
                if (perm[x, frame] != -1)
                {
                    sum += perm[x, frame];
                    count++;
                }
            }
            if (count > 0)
                return sum / count;
            return -1;
        }
    }

}