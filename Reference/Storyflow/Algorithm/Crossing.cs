using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
/// <summary>
/// Summary description for Crossing
/// </summary>
/// 
namespace Algorithm
{
    class Crossing
    {
        public static int CountCrossing(Story story, PositionTable<int> permutation)
        {
            int count = 0;
            for (int frame = 0; frame < story.TimeStamps.Length - 2; ++frame)
            {
                int left = frame;
                int right = frame + 1;
                for (int i = 0; i < story.Characters.Count; ++i)
                    for (int j = i + 1; j < story.Characters.Count; ++j)
                    {
                        if (story.SessionTable[i, left] != -1 && story.SessionTable[j, left] != -1 &&
                            story.SessionTable[i, right] != -1 && story.SessionTable[j, right] != -1)
                        {
                            // Console.WriteLine(permutation);
                            if ((permutation[i, left] - permutation[j, left]) * (permutation[i, right] - permutation[j, right]) < 0)
                            {
                                ++count;
                            }
                        }
                    }
            }
            return count;
        }

        public static int Count(Story story, PositionTable<int> permutation)
        {
            int count = 0;
            for (int frame = 0; frame < story.TimeStamps.Length - 2; ++frame)
            {
                int left = frame;
                int right = frame + 1;
                for (int i = 0; i < story.Characters.Count; ++i)
                    for (int j = i + 1; j < story.Characters.Count; ++j)
                    {
                        if (story.SessionTable[i, left] != -1 && story.SessionTable[j, left] != -1 &&
                            story.SessionTable[i, right] != -1 && story.SessionTable[j, right] != -1)
                        {
                            if ((permutation[i, left] - permutation[j, left]) * (permutation[i, right] - permutation[j, right]) < 0)
                            {
                                ++count;
                            }
                        }
                    }
            }
            return count;
        }

        public static int CountWiggle(Story story, PositionTable<int> segments)
        {
            int count = 0;
            for (int id = 0; id < story.Characters.Count; id++)
            {
                for (int frame = 0; frame < story.FrameCount - 1; frame++)
                {
                    if (story.SessionTable[id, frame] != -1 && story.SessionTable[id, frame + 1] != -1)
                    {
                        if (segments[id, frame] != segments[id, frame + 1])
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

    }
}