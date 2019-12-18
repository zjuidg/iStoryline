using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using Algorithm.aligner;
using Algorithm.PermutationCalculator;
using Algorithm.PositionOptimizer;
using Storyline;

/// <summary>
/// Summary description for LocationSensitiveSolver
/// </summary>
/// 
namespace Algorithm
{
    class LocationSensitiveSolver : ISolver
    {
        private StorylineApp _app;

        public LocationSensitiveSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> oldPosition)
        {
            return oldPosition;
        }

        public PositionTable<double> Solve(Story story)
        {
            IPermutationCalculator permCalculator = new LocationSensitiveCalculator();
            PositionTable<int> perm = permCalculator.Calculate(story);

            //for (int i = 0; i < story.Characters.Count; ++i)
            //{
            //    for (int j = 0; j < story.FrameCount; ++j)
            //        Console.Write(perm[i, j] + ", ");
            //    Console.WriteLine();
            //}

            IAligner aligner = new LocationSensitiveAligner();
            PositionTable<int> segments = aligner.Align(story, perm);

            PositionTable<double> position = perm.Clone<double>();
            LongLineConstrainedOptimizer optimizer = new LongLineConstrainedOptimizer(_app);
            optimizer.Optimize(story, position, segments);


            // move to positive
            double min = int.MaxValue;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1 && min > position[id, frame])
                        min = position[id, frame];

            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        position[id, frame] -= min;
                        position[id, frame] *= 1;
                    }

            Console.WriteLine("Location Sensitive Crossing:{0}", Crossing.Count(story, perm));

            return position;
        }
    }
}