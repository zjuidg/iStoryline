using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using Storyline;
using Algorithm.bundle;
using Algorithm.aligner;
using Algorithm.linerelaxer;
using Algorithm.PermutationCalculator;
using Algorithm.PositionOptimizer;

/// <summary>
/// Summary description for SolveManager
/// </summary>
/// 
namespace Algorithm
{
    public class SolveManager
    {
        StorylineApp _app;

        public SolveManager(StorylineApp app)
        {
            _app = app;
        }

        public void SolveStory(Story story)
        {
            if (!_app.Status.isPermDone)
            {
                IPermutationCalculator permCalculator = new LocationTreeCalculator();
                _app._perm = permCalculator.Calculate(story);
                _app.Status.isPermDone = true;
            }
            if (!_app.Status.isAlignDone)
            {
                IAligner aligner = new LocationTreeAligner(_app);
                _app._segments = aligner.Align(story, _app._perm);
                _app.Status.isAlignDone = true;
            }
            if (!_app.Status.isOptimizeDone)
            {
//                _app.Status.BundleMapping = new BundleMapping(story, _app._perm, _app._segments);
//                _app.Status.Expended = new bool[_app.Status.BundleMapping.BundleCount];
                _app.Status.Optimizer = new GreedyOptimizer(_app.Status.Config, story, _app._perm, _app._segments);
                _app._position = _app.Status.Optimizer.Optimize();
                _app.Status.isOptimizeDone = true;
                MoveToPositive(story, _app._position);
            }
            if (_app.Status.isBundleMode)
            {
                _app._bundle = _app.Status.Optimizer.Optimize();
            }
            if (!_app.Status.isRelaxDone)
            {
                LineRelaxer relaxer = new LineRelaxer(_app);
                if (_app.Status.isBundleMode)
                {
                    _app._relaxedPos = relaxer.Relax(story, _app._bundle);
                }
                else
                {
                    _app._relaxedPos = relaxer.Relax(story, _app._position);
                }
                _app.Status.isRelaxDone = true;
            }
            _app._framePos = CalculateFramePos(story);
            _app.Status.isSolveDone = true;
        }

        public void MoveToPositive(Story story, PositionTable<double> position)
        {
            double min = int.MaxValue;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1 && min > position[id, frame])
                        min = position[id, frame];
            min -= 1.0;
            for (int id = 0; id < story.Characters.Count; ++id)
                for (int frame = 0; frame < story.TimeStamps.Length - 1; ++frame)
                    if (story.SessionTable[id, frame] != -1)
                    {
                        position[id, frame] -= min;
                        position[id, frame] *= 1;
                    }
        }

        public List<Tuple<double, double>> CalculateFramePos(Story story)
        {
            List<Tuple<double, double>> framePos = new List<Tuple<double, double>>();
            double xBaseline = _app.Status.Config.PaddingLeft;
            for (int i = 0; i < story.FrameCount; ++i)
            {
                double left = xBaseline;
                double right = left + _app.Status.Config.Style.XFactor * (story.TimeStamps[i + 1] - story.TimeStamps[i]) + _app.Status.Config.Style.XGap;
                if (i == 0 || i == story.FrameCount - 1)
                    right -= _app.Status.Config.Style.XGap / 2;
                framePos.Add(new Tuple<double, double>(left, right));
                xBaseline = right;
            }
            return framePos;
        }
    }
}