using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Formatters;
using Storyline;
using StorylineBackend.upload;
using Structure;

namespace Algorithm.PositionOptimizer
{
    public class GreedyOptimizer: IOptimizer
    {
        private Config _config;

        private Story story;

        // order in a single timeframe
        private PositionTable<int> perm;
        private PositionTable<int> segment;

        private int[,] index;

        private double defaultInnerGap;
        private double defaultOuterGap;

        private List<Pair<int, double>> sessionInnerGaps;

        // Pair<Pair<s1, s2>, Pair<min, max>>
        // -1 -> Infinity
        private List<Pair<Pair<int, int>, Pair<int, int>>> sessionOuterGaps;

        public GreedyOptimizer(Config config, Story story, PositionTable<int> perm, PositionTable<int> segment)
        {
            _config = config;
            this.story = story;
            this.perm = perm;
            this.segment = segment;

            // initialize
            // index for Q at character i, timeframe j 
            index = new int[story.Characters.Count, story.FrameCount];
            for (int i = 0; i < story.Characters.Count; ++i)
            for (int frame = 0; frame < story.FrameCount; ++frame)
                index[i, frame] = -1; // invalid value       

            sessionInnerGaps = config.sessionInnerGaps;
            sessionOuterGaps = config.sessionOuterGaps;
            defaultInnerGap = config.Style.DefaultInnerGap;
            defaultOuterGap = config.Style.OuterGap;
        }

        public PositionTable<double> Optimize()
        {
            PositionTable<double> result = new PositionTable<double>(story.Characters.Count, story.FrameCount);

            int count = -1;
            // if indices are the same, they are aligned
            for (int i = 0; i < story.Characters.Count; ++i)
            {
                for (int frame = 0; frame < story.FrameCount; ++frame)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        if (frame > 0 && story.SessionTable[i, frame - 1] != -1 &&
                            segment[i, frame] == segment[i, frame - 1])
                        {
                            index[i, frame] = count;
                        }
                        else
                        {
                            index[i, frame] = ++count;
                        }
                    }
                }
            }

            int NumVariable = ++count;
            List<List<Tuple<int, int>>> timeCharacterPermList = new List<List<Tuple<int, int>>>();
            for (int frame = 0; frame < story.FrameCount; ++frame)
            {
                // charaters at timeframe
                List<Tuple<int, int>> l = new List<Tuple<int, int>>();
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    if (story.SessionTable[i, frame] != -1)
                    {
                        l.Add(new Tuple<int, int>(i, perm[i, frame]));
                    }
                }

                // get character order in current frame
                // apply result in location tree sort
                l.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                timeCharacterPermList.Add(l);

                if (frame == 0)
                {
                    for (int k = 0; k < l.Count - 1; ++k)
                    {
                        int x = l[k].Item1;
                        int y = l[k + 1].Item1;
                        // inner constraints
                        var sessionX = story.SessionTable[x, frame];
                        var sessionY = story.SessionTable[y, frame];

                        if (k == 0)
                        {
                            // initial lies to origin
                            result[x, frame] = 0;
                        }

                        if (sessionX == sessionY)
                        {
                            var distance = distanceOrInnerGap(sessionX, defaultInnerGap);
                            result[y, frame] = result[x, frame] + distance;
                        }
                        else
                        {
                            var distance = distanceOrOuterGap(sessionX, sessionY, defaultOuterGap);
                            result[y, frame] = result[x, frame] + distance;
                        }
                    }
                }
                else
                {
                    // may trace back 
                    // find first aligned for top, if not found, use first character
                    // and use it as base line 
                    // if any other aligned character is assigned to a coordinate greater than last time frame,
                    // trace back last time frame and adjust it and all other coordinates under it and before that time frame

                    // a := find first aligned character
                    // k := a's index in perm of current time frame
                    // l := permuation of character at current time frame
                    // llist := list of l (in all previous timeframe)
                    // result[a, frame] = result[a, frame - 1]
                    // for j := k to 1
                    //     x := l[j]
                    //     y := l[j - 1]
                    //     result[y, frame] = result[x, frame] - (distance according to session gaps)
                    // for j := k to l.Count - 1
                    //     x := l[j]
                    //     y := l[j + 1]
                    //     result[y, frame] = result[x, frame] + (distance according to session gaps)
                    //     if y should align to last timeframe and result[y, frame] < result[y, frame - 1]
                    //             result[y, frame] = result[y, frame - 1] (align)
                    // for m := last frame to first frame
                    //     lp := llist[m]
                    //     for p in lp
                    //         if (p should align && result[p, m] < result[p, m + 1])
                    //             result[p, m] = result[p, m + 1] // align last to current
                    //             for q := index of p to lp.Count - 1
                    //                 x := l[q]
                    //                 y := l[q + 1]
                    //                 result[y, frame] = result[x, frame] + (distance according to session gaps)
                    int firstAlignedIndex = 0; // use middle to align as default if no alignment is specified
                    int firstAlignedCharacter = l[firstAlignedIndex].Item1;
                    foreach (var tuple in l)
                    {
                        if (index[tuple.Item1, frame] == index[tuple.Item1, frame - 1])
                        {
                            // just tired of keeping index
                            firstAlignedCharacter = tuple.Item1;
                            break;
                        }
                    }
                    
                    result[firstAlignedCharacter, frame] = result[firstAlignedCharacter, frame - 1];

                    for (int j = firstAlignedIndex; j >= 1; j--)
                    {
                        var x = l[j].Item1;
                        var y = l[j - 1].Item1;

                        var sessionX = story.SessionTable[x, frame];
                        var sessionY = story.SessionTable[y, frame];

                        if (sessionX == sessionY)
                        {
                            var distance = distanceOrInnerGap(sessionX, defaultInnerGap);
                            result[y, frame] = result[x, frame] - distance;
                        }
                        else
                        {
                            var distance = distanceOrOuterGap(sessionX, sessionY, defaultOuterGap);
                            result[y, frame] = result[x, frame] - distance;
                        }
                    }

                    for (int j = firstAlignedIndex; j < l.Count - 1; j++)
                    {
                        var x = l[j].Item1;
                        var y = l[j + 1].Item1;

                        var sessionX = story.SessionTable[x, frame];
                        var sessionY = story.SessionTable[y, frame];

                        if (sessionX == sessionY)
                        {
                            var distance = distanceOrInnerGap(sessionX, defaultInnerGap);
                            result[y, frame] = result[x, frame] + distance;
                        }
                        else
                        {
                            var distance = distanceOrOuterGap(sessionX, sessionY, defaultOuterGap);
                            result[y, frame] = result[x, frame] + distance;
                        }

                        if (index[y, frame] == index[y, frame - 1] && result[y, frame] < result[y, frame - 1])
                        {
                            // should align
                            result[y, frame] = result[y, frame - 1];
                        }
                    }

                    for (int m = frame - 1; m >= 0; m--)
                    {
                        var lp = timeCharacterPermList[m];

                        for (int i = 0; i < lp.Count; i++)
                        {
                            var p = lp[i].Item1;
                            if (index[p, m] == index[p, m + 1] && result[p, m] < result[p, m + 1])
                            {
                                result[p, m] = result[p, m + 1];
                            }

                            for (int j = i; j < lp.Count - 1; j++)
                            {
                                var x = lp[j].Item1;
                                var y = lp[j + 1].Item1;
                                
                                var sessionX = story.SessionTable[x, m];
                                var sessionY = story.SessionTable[y, m];

                                if (sessionX == sessionY)
                                {
                                    var distance = distanceOrInnerGap(sessionX, defaultInnerGap);
                                    result[y, m] = result[x, m] + distance;
                                }
                                else
                                {
                                    var distance = distanceOrOuterGap(sessionX, sessionY, defaultOuterGap);
                                    result[y, m] = result[x, m] + distance;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private double distanceOrOuterGap(int sessionX, int sessionY, double distance)
        {
            foreach (var sessionOuterGap in sessionOuterGaps)
            {
                if (sessionOuterGap.Item1.toTuple().Equals(new Tuple<int, int>(sessionX, sessionY)) ||
                    sessionOuterGap.Item1.toTuple().Equals(new Tuple<int, int>(sessionY, sessionX)))
                {
                    var min = sessionOuterGap.Item2.Item1;
                    var max = sessionOuterGap.Item2.Item2;

                    if (min == -1 && max == -1)
                    {
                        continue;
                    }

                    distance = min == -1 ? max : min;
                }
            }

            return distance;
        }

        private double distanceOrInnerGap(int sessionX, double distance)
        {
            foreach (var sessionInnerGap in sessionInnerGaps)
            {
                if (sessionInnerGap.Item1 == sessionX)
                {
                    // reset inner gap according to requirement
                    distance = sessionInnerGap.Item2;
                    break;
                }
            }

            return distance;
        }
    }
}