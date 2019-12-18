using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using Storyline;
/// <summary>
/// Summary description for ModifiedOgawasolver
/// </summary>
/// 
namespace Algorithm
{
    class ModifiedOgawaSolver : ISolver
    {
        private StorylineApp _app;

        public ModifiedOgawaSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> position)
        {
            return position;
        }

        public PositionTable<double> Solve(Story story)
        {
            int outerGap = (int)(_app.Status.Config.Style.OuterGap / _app.Status.Config.Style.DefaultInnerGap);
            PositionTable<int> positionTable = new PositionTable<int>(story.Characters.Count, story.TimeStamps.Length - 1);

            // 1.put first frame
            List<Tuple<int, List<int>>> list = Ultities.GetGroups(story, 0);
            //List<Tuple<int, List<int>>> list = Ultities.GetRandomList<Tuple<int, List<int>>>(Ultities.GetGroups(story, 0));

            int yBaseline = 0;
            foreach (Tuple<int, List<int>> tuple in list)
            {
                foreach (int id in tuple.Item2)
                {
                    positionTable[id, 0] = yBaseline;
                    yBaseline += 1;
                }
                yBaseline += outerGap;
            }

            // 2.calculate group average for other frames
            for (int frame = 1; frame < story.TimeStamps.Length - 1; ++frame)
            {
                list = Ultities.GetGroups(story, frame);
                list.Sort((a, b) => -a.Item2.Count.CompareTo(b.Item2.Count));

                List<int> occupied = new List<int>();
                foreach (Tuple<int, List<int>> tuple in list)
                {
                    // sort by previous position
                    tuple.Item2.Sort((a, b) => positionTable[a, frame - 1].CompareTo(positionTable[b, frame - 1]));
                    // calculate weighted average position
                    int weight = 0;
                    int value = 0;
                    int sub = 0;
                    bool allNew = true;
                    foreach (int id in tuple.Item2)
                    {
                        if (story.SessionTable[id, frame - 1] != -1)
                        {
                            allNew = false;
                            break;
                        }
                    }
                    int top;
                    if (allNew)
                    {
                        top = 0 - tuple.Item2.Count / 2;
                    }
                    else
                    {
                        for (int i = 0; i < tuple.Item2.Count; ++i)
                        {
                            int id = tuple.Item2[i];
                            int w = Ultities.GetHistoryLength(story, id, frame);
                            value += w * positionTable[id, frame - 1];
                            weight += w;
                            sub += w * i;
                        }
                        double bestCenter = (double)value / weight;
                        top = (int)Math.Round((bestCenter * weight - sub) / weight);
                    }
                    // find a place to put it
                    for (int shift = 0; true; ++shift)
                    {
                        int shiftedTop1 = top - shift;
                        int shiftedTop2 = top + shift;
                        bool available = false;
                        int pos = 0;
                        if (IsAvailable(occupied, shiftedTop1 - outerGap, shiftedTop1 + tuple.Item2.Count - 1 + outerGap))
                        {
                            pos = shiftedTop1;
                            available = true;
                        }
                        else if (IsAvailable(occupied, shiftedTop2 - outerGap, shiftedTop2 + tuple.Item2.Count - 1 + outerGap))
                        {
                            pos = shiftedTop2;
                            available = true;
                        }
                        if (available)
                        {
                            for (int i = 0; i < tuple.Item2.Count; ++i)
                            {
                                positionTable[tuple.Item2[i], frame] = pos + i;
                                occupied.Add(pos + i);
                            }
                            break;
                        }
                    }
                }
            }

            for (int t = 0; t < 10; ++t)
            {
                // shift lines to new positions
                for (int frame = 1; frame < story.TimeStamps.Length - 2; ++frame)
                {
                    HashSet<int> deltas = new HashSet<int>();
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1)
                        {
                            int delta = positionTable[id, frame] - positionTable[id, frame + 1];
                            if (!deltas.Contains(delta))
                                deltas.Add(delta);
                        }
                    }
                    int minCost = int.MaxValue;
                    int minDelta = 0;
                    foreach (int delta in deltas)
                    {
                        int cost = 0;
                        for (int id = 0; id < story.Characters.Count; ++id)
                        {
                            if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1)
                            {
                                cost += Math.Abs(positionTable[id, frame + 1] + delta - positionTable[id, frame]);
                                if (positionTable[id, frame + 1] + delta == positionTable[id, frame])
                                {
                                    cost -= 100;
                                }
                            }
                        }
                        if (minCost > cost)
                        {
                            minCost = cost;
                            minDelta = delta;
                        }
                    }
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        positionTable[id, frame + 1] += minDelta;
                    }
                }

                for (int frame = 1; frame < story.TimeStamps.Length - 2; ++frame)
                {
                    for (int id = 0; id < story.Characters.Count; ++id)
                    {
                        if (Ultities.GetGroupCount(story, id, frame) == 1 && Ultities.GetGroupCount(story, id, frame + 1) > 1)
                        {
                            bool isHead = true;
                            int f;
                            for (f = frame - 1; f >= 0; --f)
                            {
                                if (Ultities.GetGroupCount(story, id, f) > 1)
                                {
                                    isHead = false;
                                    break;
                                }
                                else if (story.SessionTable[id, f] == -1)
                                {
                                    break;
                                }
                            }

                            HashSet<int> tubes = new HashSet<int>();
                            for (int ii = f + 1; ii <= frame; ++ii)
                            {
                                for (int ch = 0; ch < story.Characters.Count; ++ch)
                                {
                                    if (ch != id && story.SessionTable[ch, ii] != -1)
                                    {
                                        if (!tubes.Contains(positionTable[ch, ii]))
                                            tubes.Add(positionTable[ch, ii]);
                                    }
                                }
                            }
                            var occupied = tubes.ToList<int>();
                            int p = positionTable[id, frame + 1];
                            // find a place to put it
                            for (int shift = 0; true; ++shift)
                            {
                                int shiftedTop1 = p - shift;
                                int shiftedTop2 = p + shift;
                                bool available = false;
                                int pp = 0;
                                if (IsAvailable(occupied, shiftedTop1 - outerGap, shiftedTop1 + outerGap))
                                {
                                    pp = shiftedTop1;
                                    available = true;
                                }
                                else if (IsAvailable(occupied, shiftedTop2 - outerGap, shiftedTop2 + outerGap))
                                {
                                    pp = shiftedTop2;
                                    available = true;
                                }
                                if (available)
                                {
                                    for (int ii = f + 1; ii <= frame; ++ii)
                                    {
                                        //if (Math.Abs(positionTable[id, ii] - pp) > 0)
                                        {
                                            positionTable[id, ii] = pp;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                //
            }

            // move to positive
            int min = 0;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1 && min > positionTable[id, frame])
                        min = positionTable[id, frame];

            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        positionTable[id, frame] -= min;
                        positionTable[id, frame] *= 5;
                    }

            return positionTable.Clone<double>();
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