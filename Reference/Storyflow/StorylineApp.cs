using System;
using System.Collections.Generic;
using System.Linq;
using Algorithm;
using Algorithm.aligner;
using Algorithm.bundle;
using Algorithm.Group;
using Algorithm.linerelaxer;
using Algorithm.PermutationCalculator;
using Algorithm.PositionOptimizer;
using Algorithm.PathOptimizer;
using Structure;
using StorylineBackend.Reference.Storyflow.Algorithm.Group;
using StorylineBackend.upload;

namespace Storyline
{
    public interface IStorylineSovler
    {
    }

    public class StorylineApp : IStorylineSovler
    {
        public Status Status = new Status();

        public PositionTable<int> _perm;
        public PositionTable<int> _segments;
        public PositionTable<double> _position;
        public PositionTable<double> _bundle;
        public Tuple<double, double, double>[][] _relaxedPos;
        public List<Tuple<double, double>> _framePos;

        public void MoveToPositive(Story story, PositionTable<double> position)
        {
            //PositionTable<double> result = new PositionTable<double>(story.Characters.Count, story.TimeStamps.Length - 1);
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

            //return result;
        }

        public List<Tuple<double, double>> CalculateFramePos(Story story)
        {
            List<Tuple<double, double>> framePos = new List<Tuple<double, double>>();
            double xBaseline = Status.Config.PaddingLeft;
            for (int i = 0; i < story.FrameCount; ++i)
            {
                double left = xBaseline;
                double right = left + Status.Config.Style.XFactor * (story.TimeStamps[i + 1] - story.TimeStamps[i]) +
                               Status.Config.Style.XGap;
                if (i == 0 || i == story.FrameCount - 1)
                    right -= Status.Config.Style.XGap / 2;
                framePos.Add(new Tuple<double, double>(left, right));
                xBaseline = right;
            }

            return framePos;
        }

        public void SolveStory(Story story)
        {
            DateTime start = DateTime.Now;
            DateTime time = DateTime.Now;
            //GotoHellYuzuru(story);
            // Console.WriteLine(Status.isPermDone);
            // StoryVis18: Reset All Status
            
            // add rabbit in session 9999
            RabbitAdder.AddRabbit(story);

            SessionDeleter deleter = new SessionDeleter();
            deleter.delete(story, Status.Config.SelectedSessions);

            Grouper grouper = new Grouper();
            grouper.group(story, Status.Config.GroupIds.ToHashSet());


            IPermutationCalculator permCalculator = new LocationTreeCalculator();
            // IPermutationCalculator permCalculator = new CrossingMinimizedCalculator();
            _perm = permCalculator.Calculate(story);
            ConstraintCalculator ctrCalculator = new ConstraintCalculator(this);
            PositionTable<int> _newPerm = ctrCalculator.Reorder(story, _perm);
            _perm = _newPerm;
            Status.isPermDone = true;

            //MessageBox.Show((DateTime.Now - start).ToString());
            start = DateTime.Now;

            IAligner aligner = new LocationTreeAligner(this);
            _segments = aligner.Align(story, _perm);
            Status.isAlignDone = true;


            SessionBreaker sessionBreak = new SessionBreaker(this, story);
            sessionBreak.BreakAt(Status.Config.SessionBreaks);

            //MessageBox.Show((DateTime.Now - start).ToString());


            start = DateTime.Now;
            IOptimizer persistentOptimizer =
                new PersistentOptimizer(this, story, _perm, _segments, Status.Config.Style.DefaultInnerGap,
                    Status.Config.Style.OuterGap);
            var position = persistentOptimizer.Optimize();
            _position = position;
            time = DateTime.Now;
            Console.WriteLine("persistent time: {0}", time - start);
            Status.isOptimizeDone = true;
            MoveToPositive(story, _position);

            //MessageBox.Show((DateTime.Now - start).ToString());
            start = DateTime.Now;
            time = DateTime.Now;

//            _bundle = Status.Optimizer.Optimize();

            //MessageBox.Show((DateTime.Now - start).ToString());
            //start = DateTime.Now;

            LineRelaxer relaxer = new LineRelaxer(this);
            if (Status.isBundleMode)
            {
                _relaxedPos = relaxer.Relax(story, _bundle);
            }
            else
            {
                _relaxedPos = relaxer.Relax(story, _position);
            }

            Status.isRelaxDone = true;
            //add for LOR

            //MessageBox.Show((DateTime.Now - start).ToString());
            _framePos = CalculateFramePos(story);
            Console.WriteLine(Crossing.CountCrossing(story, _perm));
            //OutputYuzuru();

            // PathOptimizer
            // PathGenerator pathGenerator = new PathGenerator();
            // List<List<Point>> paths = pathGenerator.Generate(_relaxedPos);
            // Console.WriteLine(Crossing.CountWiggle(story, _segments));
        }
    }

    public class Status
    {
        //by Enxun
        //by CTK
        public IOptimizer Optimizer;
        public BundleSegmentGenerator bundleSegmentGenerator;

        public bool[] Expended;

        //By Simon
        public bool isPermDone = false;
        public bool isAlignDone = false;
        public bool isOptimizeDone = false;
        public bool isRelaxDone = false;

        // ctk:
        public bool isGroupDone = false;
        public bool isDeletionDone = false;
        public bool isBreakDone = false;

        public bool isBundleMode = false;
        public bool isSolveDone = false;
        public int focusID = -1;

        public InteractionMode interactionMode = InteractionMode.None;
        public bool isSameStory = true;
        public int LODThresh = -1;

        public Config Config = new Config();

        public void Reset(bool isBM)
        {
            isBundleMode = isBM;
            isPermDone = false;
            isAlignDone = false;
            isOptimizeDone = false;
            isRelaxDone = false;
            isSolveDone = false;
            isGroupDone = false;
            isDeletionDone = false;
//            Config = new Config();
            //Expended = null;
            //Optimizer = null;
            //BundleMapping = null;
            //bundleSegmentGenerator = null;
        }
    }

    public enum InteractionMode
    {
        None,
        DragSession,
        DragLine,
        MajorSelection,
        CharacterSelection,
        LocationSelection,
        Aggregation,
        Expand
    };
}