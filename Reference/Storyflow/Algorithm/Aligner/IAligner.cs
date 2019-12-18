using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for IAligner
/// </summary>
/// 
namespace Algorithm.aligner
{
    interface IAligner
    {
        PositionTable<int> Align(Story story, PositionTable<int> permutation);
    }
}