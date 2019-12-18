using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Algorithm;
/// <summary>
/// Summary description for linerelaxernew
/// </summary>
/// 
namespace Algorithm.linerelaxer
{
    class LineRelaxerNew
    {
        public Dictionary<Tuple<int, int>, Tuple<double, double>> Relax(Story story, PositionTable<int> position)
        {
            // calculate a side free dictionary
            Dictionary<Tuple<int, int>, Tuple<bool, bool>> free = new Dictionary<Tuple<int, int>, Tuple<bool, bool>>();
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                List<Tuple<int, List<int>>> list = Ultities.GetGroups(story, frame);
                List<Tuple<int, List<int>>> list1 = frame == 0 ? null : Ultities.GetGroups(story, frame - 1);
                List<Tuple<int, List<int>>> list2 = frame == story.FrameCount - 1 ? null : Ultities.GetGroups(story, frame + 1);
                foreach (Tuple<int, List<int>> tuple in list)
                {
                    bool leftFree = false;
                    if (frame != 0)
                    {
                        foreach (Tuple<int, List<int>> tuple1 in list1)
                        {
                            List<int> intersection = tuple.Item2.Intersect(tuple1.Item2).ToList();
                            if (intersection.Count == tuple.Item2.Count)
                            {
                                leftFree = true;
                                break;
                            }
                        }
                    }
                    bool rightFree = false;
                    if (frame != story.FrameCount - 1)
                    {
                        foreach (Tuple<int, List<int>> tuple2 in list2)
                        {
                            List<int> intersection = tuple.Item2.Intersect(tuple2.Item2).ToList();
                            if (intersection.Count == tuple.Item2.Count)
                            {
                                rightFree = true;
                                break;
                            }
                        }
                    }
                    free.Add(new Tuple<int, int>(frame, tuple.Item1), new Tuple<bool, bool>(leftFree, rightFree));
                }
            }

            Dictionary<Tuple<int, int>, double> leftIndent = new Dictionary<Tuple<int, int>, double>();
            Dictionary<Tuple<int, int>, double> rightIndent = new Dictionary<Tuple<int, int>, double>();
            for (int id = 0; id < story.Characters.Count; ++id)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    int session = story.SessionTable[id, frame];
                    if (session != -1)
                    {

                    }
                }
            }


            // <frame, session_id> -> <left, right>
            Dictionary<Tuple<int, int>, Tuple<double, double>> result = new Dictionary<Tuple<int, int>, Tuple<double, double>>();
            return result;
        }
    }
}