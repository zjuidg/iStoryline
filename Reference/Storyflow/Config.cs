using System;
using System.Collections.Generic;
using Structure;
using StorylineBackend.upload;

/// <summary>
/// Summary description for Config
/// </summary>
/// 
namespace Storyline
{
    public class Config
    {
        // style
        public Style Style = new CompactStyle();
        // public  Style Style = new ModernStyle();
        
        public List<Pair<int, double>> sessionInnerGaps = new List<Pair<int, double>>();
        // Pair<Pair<s1, s2>, Pair<min, max>>
        // -1 -> Infinity
        public List<Pair<Pair<int, int>, Pair<int, int>>> sessionOuterGaps = new List<Pair<Pair<int, int>, Pair<int, int>>>();

        public List<CharacterYConstraint> CharacterYConstraints = new List<CharacterYConstraint>();

        public List<Tuple<int, int>> Orders = new List<Tuple<int, int>>();
        public List<Tuple<int, List<int>>> OrderTable = new List<Tuple<int, List<int>>>();
        public List<int> GroupIds = new List<int>();
        public List<int> SelectedSessions = new List<int>();
        public List<SessionBreak> SessionBreaks = new List<SessionBreak>();

        public List<Pair<int, List<int>>> MajorCharacters = new List<Pair<int, List<int>>>();

        // parameters
        public double PaddingLeft = 100;
        public double PaddingTop = 100;

        public bool Relaxing = false;

        public double RelaxingGradient = 3;

        //public  double MinimumRelaxedLength = 5;
        public double bundlePow = 0.4;

        //display
        public bool ShowStoryline = true;
        public bool ShowTimestamp = false;
        public bool ShowBubble = true;
        public bool ShowSkeleton = true;

        //display option
        public bool ShowBlend = false;
        public bool Bundling = false;
        public bool NewThining = true;
        public bool BalancingSplit = false;
        public bool isInitDetail = false;
        public bool LODAnim = true;
        public int LODMaxLine = 800;

        //interaction
        public bool EnableDrag = false;
        public bool EnableFocus = false;
        public bool EnableMajorSelection = false;
        public bool EnableDoubleClick = false;

        //Demo use
        public bool DemoMode = false;
        public bool DoReorder = true;
    }

    public class DemoParam
    {
        public string fileName;
        public bool relaxing;
        public double relaxingGradient;
    }
}