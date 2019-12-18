using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for sessiontable
/// </summary>
/// 
namespace Structure
{
    public class SessionTable
    {
        private int[,] _table;

        public int this[int id, int frame]
        {
            get
            {
                return _table[id, frame];
            }
            set
            {
                _table[id, frame] = value;
            }
        }

        public SessionTable(int characterCount, int frameCount)
        {
            _table = new int[characterCount, frameCount];
            for (int id = 0; id < characterCount; ++id)
                for (int frame = 0; frame < frameCount; ++frame)
                    _table[id, frame] = -1;
        }
    }
}