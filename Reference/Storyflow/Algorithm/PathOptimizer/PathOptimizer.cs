using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;

namespace Algorithm.PathOptimizer
{
		class PathGenerator
		{
				public List<List<Point>> Generate(Tuple<double, double, double>[][] relaxedPos)
				{
						List<List<Point>> paths = new List<List<Point>>();
						foreach (var segments in relaxedPos)
						{
								List<Point> path = GenerateSinglePath(segments);
								paths.Add(path);
						}
						return paths;
				}

				public List<Point> GenerateSinglePath(Tuple<double, double, double>[] segments)
				{
						List<Point> path = new List<Point>();
						foreach (var segment in segments)
						{
								double x1 = segment.Item1;
								double x2 = segment.Item2;
								double y = segment.Item3;
								Point leftNode = new Point(x1, y);
								Point rightNode = new Point(x2, y);
								path.Add(leftNode);
								path.Add(rightNode);
						}
						return path;
				}
		}
		// Step1: Calculate Target Position
		//   * Fit Target Path
		//   * Calculate Target Path
		//     * Focus
		//     * Context
		//   * Calculate Context Path
		// Step2: Move into Target Position
		//   * Optimize Target Path
		class BendingOptimizer
		{
				// TODO: using fit library and fit target curve
				public BendingOptimizer(List<Point> targetNodes)
				{
				}

				public void CalculateTargetPath()
				{

				}

				public void OptimizeTargetPath()
				{
					
				}
		}
}