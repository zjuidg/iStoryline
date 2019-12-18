using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for positiontable
/// </summary>
/// 
namespace Structure
{
    public class PositionTable<T>
    {
        public T[,] _table;

        public T this[int id, int frame]
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
        public PositionTable(int characterCount, int frameCount)
        {
            _table = new T[characterCount, frameCount];
            string count = Convert.ToString(characterCount);
            string frames = Convert.ToString(frameCount);
        }

        public PositionTable<T2> Clone<T2>()
        {
            PositionTable<T2> result = new PositionTable<T2>(_table.GetLength(0), _table.GetLength(1));
            for (int i = 0; i < _table.GetLength(0); ++i)
                for (int j = 0; j < _table.GetLength(1); ++j)
                    result[i, j] = (T2)Convert.ChangeType(_table[i, j], typeof(T2));
            return result;
        }

        public List<T2> Select<T2>(int id)
        {
            List<T2> result = new List<T2>();
            for (int j = 0; j < _table.GetLength(1); ++j)
            {
                T2 v;
                try
                {
                    v = (T2)Convert.ChangeType(_table[id, j], typeof(T2));
                }
                catch
                {
                    v = (T2)Convert.ChangeType(0, typeof(T2));
                }
                result.Add(v);
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            PositionTable<T> rhs = (PositionTable<T>)obj;
            if (this._table.GetLength(0) != rhs._table.GetLength(0) || this._table.GetLength(1) != rhs._table.GetLength(1))
                return false;
            for (int i = 0; i < _table.GetLength(0); ++i)
                for (int j = 0; j < _table.GetLength(1); ++j)
                    if (!_table[i, j].Equals(rhs._table[i, j]))
                        return false;
            return true;
        }
    }
}