using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Structure;

/// <summary>
/// Summary description for ISolver
/// </summary>
/// 
namespace Algorithm
{
    public interface ISolver
    {
        PositionTable<double> Solve(Story story, PositionTable<double> position);
        PositionTable<double> Solve(Story story);
    }
}