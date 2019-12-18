using System.Collections.Generic;

namespace Structure
{
    public class Style
    {
        // Gap and factor
        public double DefaultInnerGap;
        public SortedDictionary<int, double> SessionGapDictionary;
        public double OuterGap;
        public double XFactor;
        public double XGap;

        // line style
        public double LineWidth;
        public double StartDotRadius;
        public double EndDotRadius;

        // sheath style
        public double SheathWidth;
        public double StartSheathWidth;
        public double EndSheathWidth;
        public bool StartSheathTopmost;
        public bool EndSheathTopmost;
        public Color SheathColor;

        // dash line style
        public double DashWidth;

        // thinning
        public bool Thinning;
        public double ThinningGradient;
        public double ThinningGap;

        //Bubble
        public double BasicBubbleSize;
        public double BubbleIncrease;

        public double TextSize;
        public double TimeLineWidth;
    }
    
    public class CompactStyle : Style
    {
        public CompactStyle()
        {
            DefaultInnerGap = 18;//0.09;//5.0;//0.18;//50.0/10;
            SessionGapDictionary = new SortedDictionary<int, double>();
            OuterGap = 54;//0.27;//10.0;//0.54;//100.0/10;
            XFactor = 10;//1.0;//1.5;//10;//15/10;//15;
            XGap = 70;//5.0;//7.8;//50;//90/10;//78;

            LineWidth = 6;//0.03;// 1.0;//0.06;//18.0/10;
            StartDotRadius = 5;//10;//0;//5;//0;
            EndDotRadius = 4;// 0;// 1.0;//24.0/10;

            SheathWidth = 6;
            StartSheathWidth = 4;
            EndSheathWidth = 5;
            StartSheathTopmost = false;
            EndSheathTopmost = true;
            SheathColor = Color.FromRgb(219, 219, 219);

            DashWidth = 3;// 0; //0.25;//0.3;
            
            Thinning = false;
            ThinningGradient = 0.92;
            ThinningGap = 2 * OuterGap;

            BasicBubbleSize = 20;//15;
            BubbleIncrease = 8;
            TextSize = 12;
            TimeLineWidth = 2;
        }
    }

    class ModernStyle : Style
    {
        public ModernStyle()
        {
            DefaultInnerGap = 28;
            OuterGap = 84;
            XFactor = 10;
            XGap = 50;

            LineWidth = 6;
            StartDotRadius = 7;
            EndDotRadius = 11;

            SheathWidth = 0;
            StartSheathWidth = 0;
            EndSheathWidth = 2;
            StartSheathTopmost = false;
            EndSheathTopmost = false;
            SheathColor = Color.FromRgb(255, 255, 255);

            DashWidth = 2;

            Thinning = false;
            ThinningGradient = 1.5;
            ThinningGap = 2 * OuterGap;

            TextSize = 12;
        }
    }
}