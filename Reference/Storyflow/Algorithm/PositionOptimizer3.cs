﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
/// <summary>
/// Summary description for PositionOptimizer3
/// </summary>
/// 
namespace Algorithm
{
    class PositionOptimizer3
    {
        public PositionTable<double> Optimize(Story story, PositionTable<double> position, double weight1, double weight2, double weight3, double weight4)
        {
            // gradient descent optimization
            //PositionTable<double> position = positionTable.Clone<double>();

            double eps = 0.05;
            double terminal = 0.005;
            double maxShift = 100.0;
            int limit = 0;

            while (maxShift > terminal && ++limit < 200)
            {
                eps *= 0.95;
                PositionTable<double> old = position.Clone<double>();
                maxShift = 0.0;
                for (int i = 0; i < story.Characters.Count; ++i)
                {
                    for (int frame_start = 0; frame_start < story.TimeStamps.Length - 1; ++frame_start)
                    {
                        if (story.SessionTable[i, frame_start] == -1)
                            continue;
                        int frame_end;
                        for (frame_end = frame_start + 1;
                            frame_end < story.TimeStamps.Length - 1 &&
                            story.SessionTable[i, frame_end] != -1 &&
                            Math.Abs(old[i, frame_end] - old[i, frame_start]) < 0.001;
                            ++frame_end)
                        {
                        }
                        double w = 0;
                        for (int frame = frame_start; frame < frame_end; ++frame)
                        {
                            w += old[i, frame];
                        }
                        w /= (frame_end - frame_start);
                        for (int frame = frame_start; frame < frame_end; ++frame)
                        {
                            old[i, frame] = w;
                        }


                        double shift = 0.0;
                        for (int frame = frame_start; frame < frame_end; ++frame)
                        {
                            // 1.Inside session
                            {
                                double w1 = weight1;
                                int n = Ultities.GetGroupCount(story, i, frame);
                                for (int j = 0; j < story.Characters.Count; ++j)
                                {
                                    if (i != j && story.SessionTable[i, frame] == story.SessionTable[j, frame])
                                    {
                                        double d = der1(old[i, frame], old[j, frame], n, 1.0);
                                        shift += w1 * eps * d;
                                    }
                                }
                            }

                            // 2.Between sessions
                            {
                                double w2 = weight1;
                                for (int j = 0; j < story.Characters.Count; ++j)
                                {
                                    if (i != j && story.SessionTable[j, frame] != -1 && story.SessionTable[i, frame] != story.SessionTable[j, frame])
                                    {
                                        double d = der2(old[i, frame], old[j, frame], 5.0);
                                        shift += w2 * eps * d;
                                    }
                                }
                            }

                            // 3.Wiggles
                            {
                                double w3 = weight2;
                                if (frame != 0 && story.SessionTable[i, frame - 1] != -1)
                                {
                                    //if (Ultities.GetGroupCount(story, i, frame) == Ultities.GetGroupCount(story, i, frame - 1))
                                    {
                                        double d = der3(old[i, frame], old[i, frame - 1]);
                                        shift += w3 * eps * d;
                                    }
                                }
                                if (frame != story.TimeStamps.Length - 2 && story.SessionTable[i, frame + 1] != -1)
                                {
                                    //if (Ultities.GetGroupCount(story, i, frame) == Ultities.GetGroupCount(story, i, frame + 1))
                                    {
                                        double d = der3(old[i, frame], old[i, frame + 1]);
                                        shift += w3 * eps * d;
                                    }
                                }
                            }

                            // 4.Smooth
                            {
                                double w4 = weight3;
                                if (frame != 0 && frame != story.TimeStamps.Length - 2 &&
                                    story.SessionTable[i, frame - 1] != -1 && story.SessionTable[i, frame + 1] != -1)
                                {
                                    double d = der3(old[i, frame], (old[i, frame - 1] + old[i, frame + 1]) / 2);
                                    shift += w4 * eps * d;
                                }
                            }

                            // 5.Parellel
                            {
                                double w5 = weight4;
                                for (int j = 0; j < story.Characters.Count; ++j)
                                {
                                    if (i != j && frame != 0 &&
                                        story.SessionTable[i, frame] != -1 && story.SessionTable[j, frame] != -1 &&
                                        story.SessionTable[i, frame - 1] != -1 && story.SessionTable[j, frame - 1] != -1 &&
                                        story.SessionTable[i, frame] == story.SessionTable[j, frame] &&
                                        story.SessionTable[i, frame - 1] == story.SessionTable[j, frame - 1])
                                    {
                                        double x = old[i, frame] + old[j, frame - 1] - old[i, frame - 1];
                                        double d = der3(old[j, frame], x);
                                        shift += w5 * eps * d;
                                    }
                                }
                            }
                        }

                        if (maxShift < shift)
                            maxShift = shift;
                        for (int frame = frame_start; frame < frame_end; ++frame)
                        {
                            position[i, frame] += shift;
                        }

                        frame_start = frame_end - 1;
                    }
                }
            }

            return position;
        }

        private double der3(double x, double y)
        {
            if (Math.Abs(x - y) < 0.001)
                return 0;
            if (x < y)
                return 1.0 / (2.0 * Math.Sqrt(Math.Abs(x - y)));
            else
                return -1.0 / (2.0 * Math.Sqrt(Math.Abs(x - y)));
            return 2.0 * (y - x);
        }

        private double der2(double x, double y, double d)
        {
            double abs = Math.Abs(x - y);
            if (abs >= d)
            {
                return 0;
            }
            else
            {
                if (x > y)
                {
                    return 2.0 * (d - abs);
                }
                else
                {
                    return 2.0 * (abs - d);
                }
            }
        }

        private double der1(double x, double y, int n, double d)
        {
            double abs = Math.Abs(x - y);
            if (abs < d)
            {
                if (x > y)
                {
                    return 2.0 * (d - abs);
                }
                else
                {
                    return 2.0 * (abs - d);
                }
            }
            else if (abs > (n - 1) * d)
            {
                if (x > y)
                {
                    return 2.0 * ((n - 1) * d - abs);
                }
                else
                {
                    return 2.0 * (abs - (n - 1) * d);
                }
            }
            else
            {
                return 0;
            }
        }
    }
}