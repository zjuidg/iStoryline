using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for segmentread
/// </summary>
/// 
namespace Structure
{
    public class segmentread
    {
        public List<int> Prev;
        public int Now;
        public List<int> Next;
        public double Weight;
        public int Start;
        public int End;

        public segmentread(int start, int end, List<int> prev, int now, List<int> next, double weight)
        {
            Start = start;
            End = end;
            Prev = prev;
            Now = now;
            Next = next;
            Weight = weight;
        }
    }
}