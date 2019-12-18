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
/// Summary description for alignmentoptimizedsolver
/// </summary>
/// 
namespace Algorithm
{
    class AlignmentOptimizedSolver : ISolver
    {
        private StorylineApp _app;

        public AlignmentOptimizedSolver(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<double> Solve(Story story, PositionTable<double> oldPosition)
        {
            return oldPosition;
        }

        public PositionTable<double> Solve(Story story)
        {
            IPermutationCalculator permCalculator = new CrossingMinimizedCalculator();
            PositionTable<int> perm = permCalculator.Calculate(story);

            IAligner aligner = new NaiveAligner(_app);
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

            return position;
        }
    }
}