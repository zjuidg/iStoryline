using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
/// <summary>
/// Summary description for locationtreealigner
/// </summary>
/// 
namespace Algorithm.aligner
{
    class LocationTreeAligner : IAligner
    {
        private StorylineApp _app;

        public LocationTreeAligner(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<int> Align(Story story, PositionTable<int> permutation)
        {
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

            List<Tuple<int, double>> pivot = new List<Tuple<int, double>>();
            for (int frame = 0; frame < story.FrameCount - 1; ++frame)
            {
                //if (frame % 20 == 0)
                //    continue;
                int leftFrame = frame;
                int rightFrame = frame + 1;

                List<Tuple<int, List<int>>> leftSessions = GetSessionList(story, permutation, leftFrame);
                List<Tuple<int, List<int>>> rightSessions = GetSessionList(story, permutation, rightFrame);
                double[,] dp = new double[leftSessions.Count + 1, rightSessions.Count + 1];
                Tuple<int, int>[,] path = new Tuple<int, int>[leftSessions.Count + 1, rightSessions.Count + 1];
                for (int i = 0; i < leftSessions.Count; ++i)
                {
                    for (int j = 0; j < rightSessions.Count; ++j)
                    {
                        dp[i + 1, j + 1] = -1;
                        double relativeWeight = GetRelativeWeight(GetPositionFactor(i, leftSessions.Count), GetPositionFactor(j, rightSessions.Count));
                        //if (story.GetLocationId(leftSessions[i].Item1) == story.GetLocationId(rightSessions[j].Item1))
                        {
                            Tuple<int, int, int, double> tuple = GetMaximumMatch(story, leftSessions[i].Item2, rightSessions[j].Item2, leftFrame);
                            const double kReletiveWeight = 0.1;
                            if (dp[i + 1, j + 1] < dp[i, j] + tuple.Item4 + kReletiveWeight * relativeWeight)
                            {
                                dp[i + 1, j + 1] = dp[i, j] + tuple.Item4 + kReletiveWeight * relativeWeight;
                                path[i + 1, j + 1] = new Tuple<int, int>(i, j);
                            }
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
                pivot.Add(new Tuple<int, double>(frame, dp[leftSessions.Count, rightSessions.Count]));
            }
            pivot.Sort((a, b) =>
            {
                return a.Item2.CompareTo(b.Item2);
            });
            bool[] free = new bool[story.FrameCount];
            for (int i = 0; i < pivot.Count; ++i)
            {
                bool covered = false;
                for (int j = 0; j < pivot.Count; ++j)
                {
                    if (free[pivot[j].Item1] && Math.Abs(pivot[i].Item1 - pivot[j].Item1) < 40)
                    {
                        covered = true;
                    }
                }
                if (!covered)
                    free[pivot[i].Item1] = true;
            }

            for (int frame = 0; frame < story.FrameCount - 1; ++frame)
            {
                //if (frame % 20 == 0)
                //    continue;

                if (_app.Status.Config.BalancingSplit && free[frame])
                    continue;

                int leftFrame = frame;
                int rightFrame = frame + 1;

                List<Tuple<int, List<int>>> leftSessions = GetSessionList(story, permutation, leftFrame);
                List<Tuple<int, List<int>>> rightSessions = GetSessionList(story, permutation, rightFrame);
                double[,] dp = new double[leftSessions.Count + 1, rightSessions.Count + 1];
                Tuple<int, int>[,] path = new Tuple<int, int>[leftSessions.Count + 1, rightSessions.Count + 1];
                for (int i = 0; i < leftSessions.Count; ++i)
                {
                    for (int j = 0; j < rightSessions.Count; ++j)
                    {
                        dp[i + 1, j + 1] = -1;
                        double relativeWeight = GetRelativeWeight(GetPositionFactor(i, leftSessions.Count), GetPositionFactor(j, rightSessions.Count));
                        //if (story.GetLocationId(leftSessions[i].Item1) == story.GetLocationId(rightSessions[j].Item1))
                        {
                            Tuple<int, int, int, double> tuple = GetMaximumMatch(story, leftSessions[i].Item2, rightSessions[j].Item2, leftFrame);
                            const double kReletiveWeight = 0.1;
                            if (dp[i + 1, j + 1] < dp[i, j] + tuple.Item4 + kReletiveWeight * relativeWeight)
                            {
                                dp[i + 1, j + 1] = dp[i, j] + tuple.Item4 + kReletiveWeight * relativeWeight;
                                path[i + 1, j + 1] = new Tuple<int, int>(i, j);
                            }
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
                            Tuple<int, int, int, double> tuple = GetMaximumMatch(story, leftSessions[i].Item2, rightSessions[j].Item2, leftFrame);
                            for (int k = 0; k < tuple.Item3; ++k)
                            {
                                segments[rightSessions[j].Item2[tuple.Item2 + k], rightFrame] = segments[leftSessions[i].Item2[tuple.Item1 + k], leftFrame];
                            }
                        }
                        Tuple<int, int> pair = path[i + 1, j + 1];
                        i = pair.Item1 - 1;
                        j = pair.Item2 - 1;
                    }
                }
            }


            return segments;
        }

        private List<Tuple<int, List<int>>> GetSessionList(Story story, PositionTable<int> perm, int frame)
        {
            int count = 0;
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (perm[i, frame] != -1)
                    ++count;
            }
            int[] invertedPerm = new int[count];
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                if (perm[i, frame] != -1)
                    invertedPerm[perm[i, frame]] = i;
            }
            List<Tuple<int, List<int>>> result = new List<Tuple<int, List<int>>>();
            for (int i = 0; i < invertedPerm.Length; ++i)
            {
                if (i == 0 || story.SessionTable[invertedPerm[i], frame] != story.SessionTable[invertedPerm[i - 1], frame])
                {
                    result.Add(new Tuple<int, List<int>>(story.SessionTable[invertedPerm[i], frame], new List<int>()));
                }
                result.Last().Item2.Add(invertedPerm[i]);
            }
            return result;
        }

        // TODO(Enxun):1.add line weight 2.add space effciency volumn 3.transform int to double weight
        // 1, 3 added
        private Tuple<int, int, int, double> GetMaximumMatch(Story story, List<int> a, List<int> b, int timefameOfa)
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
                        var weight = story.Characters[a[i]].Weight;
                        var majorChracters = _app.Status.Config.MajorCharacters;
                        majorChracters.ForEach(pair =>
                        {
                            if (pair.Item1 == a[i] && pair.Item2.Contains(timefameOfa))
                            {
                                weight = 1000;
                            }
                        });
                        
                        dp[i + 1, j + 1] = dp[i, j] + weight;
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

        private double GetRelativeWeight(double factor1, double factor2)
        {
            // diff 0 -> 1
            // diff 1 -> 0
            return 1.0 - Math.Abs(factor1 - factor2);
        }

        private double GetPositionFactor(int index, int count)
        {
            if (count <= 1)
                return 0.5;
            return (double)index / (count - 1);
            //return ((double)index) / count;
        }
    }
}