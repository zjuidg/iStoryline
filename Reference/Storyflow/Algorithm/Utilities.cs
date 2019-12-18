using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
/// <summary>
/// Summary description for Utilities
/// </summary>
/// 
namespace Algorithm
{
    public static class Ultities
    {
        public static void ColorChange(Story story)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < story.Locations.Count; i++)
            {
                Color randomColor = Color.FromRgb((byte)random.Next(10, 245), (byte)random.Next(10, 245), (byte)random.Next(10, 245));
                //story.Locations[i].Color = ChangeSatuation(randomColor, 0.3);
            }
            for (int id = 0; id < story.Characters.Count; id++)
            {
                Dictionary<int, int> locationTimeDic = new Dictionary<int, int>();
                for (int frame = 0; frame < story.FrameCount; frame++)
                {
                    if (story.SessionTable[id, frame] != -1)
                    {
                        if (!locationTimeDic.ContainsKey(story._sessionToLocation[story.SessionTable[id, frame]]))
                            locationTimeDic.Add(story._sessionToLocation[story.SessionTable[id, frame]], 0);
                        locationTimeDic[story._sessionToLocation[story.SessionTable[id, frame]]]++;
                    }
                }
                var locationTimeList = locationTimeDic.ToList();
                locationTimeList.Sort((a, b) => { return b.Value.CompareTo(a.Value); });
                int majorLoc;
                if (locationTimeList.Count > 0)
                {
                    majorLoc = locationTimeList[0].Key;
                    story.Characters[id].Color = ChangeSatuation(story.Locations[majorLoc].Color, 1.0);
                }
                else
                {
                    story.Characters[id].Color = Colors.Gray;
                }
                if (story.Characters[id].Color == Colors.Black)
                {
                    story.Characters[id].Color = Color.FromRgb(40, 40, 40);
                }
            }
        }

        public static Color ChangeSatuation(Color color, double p)
        {
            RGB rgb = new RGB(color.R, color.G, color.B);
            HSI hsi = ColorspaceHelper.RGB2HSI(rgb);
            hsi.Saturation *= p;
            rgb = ColorspaceHelper.HSI2RGB(hsi);
            return Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue);
        }

        public static int[] GetRandomFeasiblePermutation(Story story, int frame)
        {
            int[] permutation = new int[story.Characters.Count];
            for (int i = 0; i < permutation.Length; ++i)
                permutation[i] = -1;
            int count = 0;
            List<int> list = new List<int>();
            for (int i = 0; i < story.Characters.Count; ++i)
                list.Add(i);
            list = GetRandomList<int>(list);
            foreach (int i in list)
            {
                if (permutation[i] == -1 && story.SessionTable[i, frame] != -1)
                {
                    permutation[i] = count++;
                    foreach (int j in list)
                    {
                        if (i != j && story.SessionTable[i, frame] == story.SessionTable[j, frame])
                        {
                            permutation[j] = count++;
                        }
                    }
                }
            }
            return permutation;
        }

        public static int[] GetFeasiblePermutation(Story story, int frame)
        {
            int[] permutation = new int[story.Characters.Count];
            for (int i = 0; i < permutation.Length; ++i)
                permutation[i] = -1;
            int count = 0;
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (permutation[i] == -1 && story.SessionTable[i, frame] != -1)
                {
                    permutation[i] = count++;
                    for (int j = 0; j < story.Characters.Count; ++j)
                    {
                        if (i != j && story.SessionTable[i, frame] == story.SessionTable[j, frame])
                        {
                            permutation[j] = count++;
                        }
                    }
                }
            }
            return permutation;
        }
        // old ones
        public static int GetGroupCount(Story story, int id, int frame)
        {
            if (id >= story.Characters.Count || frame >= story.FrameCount || id < 0 || frame < 0)
                return 0;
            if (story.SessionTable[id, frame] == -1)
                return 0;
            int s = 0;
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] == story.SessionTable[id, frame])
                    ++s;
            }
            return s;
        }

        public static List<int> GetSingleGroup(Story story, int id, int frame)
        {
            if (id >= story.Characters.Count || frame >= story.FrameCount || id < 0 || frame < 0)
                return new List<int>();
            if (story.SessionTable[id, frame] == -1)
                return new List<int>();
            List<int> result = new List<int>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (story.SessionTable[i, frame] == story.SessionTable[id, frame])
                    result.Add(i);
            }
            return result;
        }


        public static int GetHistoryLength(Story story, int id, int currentFrame)
        {
            int length = 0;
            for (int frame = 0; frame < currentFrame; ++frame)
            {
                if (story.SessionTable[id, frame] != -1)
                {
                    length += story.TimeStamps[frame + 1] - story.TimeStamps[frame];
                }
            }
            return length;
        }

        public static List<Tuple<int, List<int>>> GetGroups(Story story, int frame)
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                int group = story.SessionTable[i, frame];
                if (group != -1)
                {
                    if (!dict.ContainsKey(group))
                    {
                        dict.Add(group, new List<int>());
                    }
                    dict[group].Add(i);
                    //dict[group] = GetRandomList(dict[group]);
                }
            }
            List<Tuple<int, List<int>>> result = new List<Tuple<int, List<int>>>();
            foreach (KeyValuePair<int, List<int>> pair in dict)
            {
                result.Add(new Tuple<int, List<int>>(pair.Key, pair.Value));
            }
            return result;
            //return GetRandomList(result);
        }

        public static List<T> GetRandomList<T>(List<T> inputList)
        {
            //Copy to a array
            T[] copyArray = new T[inputList.Count];
            inputList.CopyTo(copyArray);

            //Add range
            List<T> copyList = new List<T>();
            copyList.AddRange(copyArray);

            //Set outputList and random
            List<T> outputList = new List<T>();
            Random rd = new Random(DateTime.Now.Millisecond);

            while (copyList.Count > 0)
            {
                //Select an index and item
                int rdIndex = rd.Next(0, copyList.Count);
                T remove = copyList[rdIndex];

                //remove it from copyList and add it to output
                copyList.Remove(remove);
                outputList.Add(remove);
            }
            return outputList;
        }
    }
}