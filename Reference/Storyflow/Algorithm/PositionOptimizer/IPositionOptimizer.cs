using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for IPositionOptimizer
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    interface IPositionOptimizer
    {
        void Optimize(Story story, PositionTable<double> position);
    }
}