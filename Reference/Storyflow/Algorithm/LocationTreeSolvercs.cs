using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
using Algorithm.bundle;
using Algorithm.aligner;
using Algorithm.PermutationCalculator;
using Algorithm.PositionOptimizer;

/// <summary>
/// Summary description for LocationTreeSolvercs
/// </summary>
/// 
namespace Algorithm
{
    class LocationTreeSolver : ISolver
    {
        private StorylineApp _app;

        public LocationTreeSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> oldPosition)
        {
            return oldPosition;
        }

        public PositionTable<double> Solve(Story story)
        {
            DateTime time = DateTime.Now;

            //IPermutationCalculator permCalculator = new LocationSensitiveCalculator();
            IPermutationCalculator permCalculator = new LocationTreeCalculator();
            PositionTable<int> perm = permCalculator.Calculate(story);

            Console.WriteLine(">>>Perm Time Consuming:{0}", DateTime.Now - time);
            time = DateTime.Now;

            IAligner aligner = new LocationTreeAligner(_app);
            PositionTable<int> segments = aligner.Align(story, perm);

            Console.WriteLine(">>>Align Time Consuming:{0}", DateTime.Now - time);
            time = DateTime.Now;

            _app.Status.Optimizer = new GreedyOptimizer(_app.Status.Config, story, perm, segments);
            PositionTable<double> position = _app.Status.Optimizer.Optimize();

            Console.WriteLine(">>>Optimize Time Consuming:{0}", DateTime.Now - time);
            time = DateTime.Now;



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

            Console.WriteLine("Location Tree Crossing:{0}", Crossing.Count(story, perm));

            return position;
        }
    }
}