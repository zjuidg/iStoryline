using Structure;

namespace Algorithm.PositionOptimizer
{
    public interface IOptimizer
    {
        PositionTable<double> Optimize();
    }
}