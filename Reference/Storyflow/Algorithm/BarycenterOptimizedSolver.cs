using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;
using System.Diagnostics;
using Algorithm.PermutationCalculator;
using Algorithm.PositionOptimizer;

/// <summary>
/// Summary description for BarycenterOptimizedSolver
/// </summary>
/// 
namespace Algorithm
{
    class BarycenterOptimizedSolver : ISolver
    {
        public PositionTable<double> Solve(Story story, PositionTable<double> oldPosition)
        {
            PositionTable<double> position = oldPosition.Clone<double>();

            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        position[id, frame] /= 5.0;
                    }

            ShortLineConstrainedOptimizer opt = new ShortLineConstrainedOptimizer();
            opt.Optimize(story, position);

            // move to positive
            double min = 0;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1 && min > position[id, frame])
                        min = position[id, frame];

            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        position[id, frame] -= min;
                        position[id, frame] *= 5;
                    }

            return position;
        }

        public PositionTable<double> Solve(Story story)
        {
            //IPermutationCalculator permCalculator = new EfficientBarycenterCalculator();
            IPermutationCalculator permCalculator = new CrossingMinimizedCalculator();
            PositionTable<int> perm = permCalculator.Calculate(story);
            PositionTable<double> position = new PositionTable<double>(story.Characters.Count, story.TimeStamps.Length - 1);

            // transfer perm to position
            position = perm.Clone<double>();

            // new potimize
            ShortLineConstrainedOptimizer2 opt = new ShortLineConstrainedOptimizer2();
            opt.Optimize(story, position);

            // optimize
            Debug.WriteLine("Before opt, Crossing:{0}", Crossing.Count(story, position.Clone<int>()));

            PositionOptimizer3 optimizer2 = new PositionOptimizer3();
            PositionOptimizer1 optimizer1 = new PositionOptimizer1();
            PositionOptimizer2 optimizer = new PositionOptimizer2();



            int x = 0;
            while (x-- > 0)
            {
                //position = optimizer.Optimize(story, position, 0.6, 0.2, 0.0, 0.0);
                position = optimizer2.Optimize(story, position, 1.0, 0.5, 0.0, 0.0);
                position = optimizer1.Optimize(story, position, 1.0, 0.0, 0.0, 0.0);
            }
            Debug.WriteLine("After opt, Crossing:{0}", Crossing.Count(story, position.Clone<int>()));

            // move to positive
            double min = 0;
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