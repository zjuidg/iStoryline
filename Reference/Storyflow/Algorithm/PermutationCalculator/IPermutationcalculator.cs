using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

/// <summary>
/// Summary description for IPermutationcalculator
/// </summary>
/// 
namespace Algorithm.PermutationCalculator
{
    interface IPermutationCalculator
    {
        PositionTable<int> Calculate(Story story);
    }
}