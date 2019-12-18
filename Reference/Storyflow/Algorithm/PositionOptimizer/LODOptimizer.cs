using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Storyline;
using Structure;
using mosek;

/// <summary>
/// Summary description for LODOptimizer
/// </summary>
/// 
namespace Algorithm.PositionOptimizer
{
    class LODOptimizer
    {
        private StorylineApp _app;
        private List<Node>[] nodePool;
        private mosek.Task task;
        private mosek.Env env;
        private int count;
        List<Tuple<int, int>> inner = new List<Tuple<int, int>>();
        List<Tuple<int, int>> outer = new List<Tuple<int, int>>();
        Dictionary<Node, int> index = new Dictionary<Node, int>();

        public LODOptimizer(StorylineApp app, List<Node>[] _nodePool)
        {
            _app = app;
            nodePool = _nodePool;
            count = 0;
            for (int frame = 0; frame < nodePool.Count(); frame++)
            {
                foreach (Node node in nodePool[frame])
                {
                    if (frame > 0 && node.alignPrev != null)
                        index.Add(node, index[node.alignPrev]);
                    else
                        index.Add(node, count++);
                }
            }
            int NumVariable = index.Count;

            #region Matrix
            var matrix = new Dictionary<Tuple<int, int>, double>();
            for (int i = 0; i < index.Count; ++i)
            {
                matrix.Add(new Tuple<int, int>(i, i), 0.01);//simon
            }
            for (int frame = 0; frame < nodePool.Count() - 1; frame++)
            {
                foreach (Node left in nodePool[frame])
                {
                    foreach (Node right in nodePool[frame + 1])
                    {
                        if (left.alignNext != right)//这样才有可能算进(yi-yj)^2里
                        {
                            int weight = left.segments.Intersect(right.segments).Count();
                            Tuple<int, int> ll = new Tuple<int, int>(index[left], index[left]);
                            Tuple<int, int> lr = new Tuple<int, int>(index[left], index[right]);
                            Tuple<int, int> rl = new Tuple<int, int>(index[right], index[left]);
                            Tuple<int, int> rr = new Tuple<int, int>(index[right], index[right]);
                            if (!matrix.ContainsKey(ll))
                                matrix.Add(ll, 0);
                            if (!matrix.ContainsKey(lr))
                                matrix.Add(lr, 0);
                            if (!matrix.ContainsKey(rl))
                                matrix.Add(rl, 0);
                            if (!matrix.ContainsKey(rr))
                                matrix.Add(rr, 0);
                            matrix[ll] += weight;
                            matrix[rr] += weight;
                            matrix[lr] -= weight;
                            matrix[rl] -= weight;
                        }
                    }
                }
            }
            List<int> li = new List<int>();
            List<int> lj = new List<int>();
            List<double> lv = new List<double>();
            foreach (KeyValuePair<Tuple<int, int>, double> pair in matrix)
            {
                if (pair.Key.Item1 >= pair.Key.Item2 && pair.Value != 0)
                {
                    li.Add(pair.Key.Item1);
                    lj.Add(pair.Key.Item2);
                    lv.Add(pair.Value);
                }
            }
            int[] qsubi = li.ToArray();
            int[] qsubj = lj.ToArray();
            double[] qval = lv.ToArray();
            #endregion

            #region Constraints
            for (int frame = 0; frame < nodePool.Count(); frame++)
            {
                for (int i = 0; i < nodePool[frame].Count - 1; i++)
                {
                    var nodeUp = nodePool[frame][i];
                    var nodeDown = nodePool[frame][i + 1];
                    if (nodeUp.type == NodeType.Segment && nodeDown.type == NodeType.Segment &&
                        nodeUp.parent == nodeDown.parent)
                    {
                        inner.Add(new Tuple<int, int>(index[nodeUp], index[nodeDown]));
                    }
                    else
                    {
                        outer.Add(new Tuple<int, int>(index[nodeUp], index[nodeDown]));
                    }
                }
            }
            int NumConstraint = inner.Count + outer.Count;
            int[][] asub = new int[NumVariable][];
            double[][] aval = new double[NumVariable][];
            List<int>[] asubList = new List<int>[NumVariable];
            List<double>[] avalList = new List<double>[NumVariable];
            for (int i = 0; i < NumVariable; ++i)
            {
                asubList[i] = new List<int>();
                avalList[i] = new List<double>();
            }
            for (int i = 0; i < inner.Count; i++)
            {
                Tuple<int, int> pair = inner[i];
                int x = pair.Item1;
                int y = pair.Item2;
                asubList[x].Add(i);
                avalList[x].Add(-1);
                asubList[y].Add(i);
                avalList[y].Add(1);
            }
            for (int i = 0; i < outer.Count; i++)
            {
                int j = i + inner.Count;
                Tuple<int, int> pair = outer[i];
                int x = pair.Item1;
                int y = pair.Item2;
                asubList[x].Add(j);
                avalList[x].Add(-1);
                asubList[y].Add(j);
                avalList[y].Add(1);
            }
            for (int i = 0; i < NumVariable; ++i)
            {
                asub[i] = asubList[i].ToArray();
                aval[i] = avalList[i].ToArray();
            }
            mosek.boundkey[] bkc = new mosek.boundkey[NumConstraint];
            double[] blc = new double[NumConstraint];
            double[] buc = new double[NumConstraint];
            for (int i = 0; i < inner.Count; ++i)
            {
                bkc[i] = mosek.boundkey.fx;
                blc[i] = _app.Status.Config.Style.DefaultInnerGap;
                buc[i] = _app.Status.Config.Style.DefaultInnerGap;
            }
            for (int i = inner.Count; i < inner.Count + outer.Count; ++i)
            {
                bkc[i] = mosek.boundkey.lo;
                blc[i] = _app.Status.Config.Style.OuterGap;
                buc[i] = 1000;
            }
            double[] blx = new double[NumVariable];
            double[] bux = new double[NumVariable];
            mosek.boundkey[] bkx = new mosek.boundkey[NumVariable];
            for (int i = 0; i < NumVariable; ++i)
            {
                bkx[i] = mosek.boundkey.ra;
                blx[i] = -12000;
                bux[i] = 12000;
            }
            #endregion

            #region Mosek
            try
            {
                env = new mosek.Env();
                env.init();

                task = new mosek.Task(env, 0, 0);
                task.putmaxnumvar(NumVariable);
                task.putmaxnumcon(NumConstraint);

                task.append(mosek.accmode.con, NumConstraint);
                task.append(mosek.accmode.var, NumVariable);

                task.putcfix(0.0);

                for (int j = 0; j < NumVariable; ++j)
                {
                    task.putcj(j, 0);
                    task.putbound(mosek.accmode.var, j, bkx[j], blx[j], bux[j]);

                    task.putavec(mosek.accmode.var, j, asub[j], aval[j]);
                }

                for (int i = 0; i < NumConstraint; ++i)
                {
                    task.putbound(mosek.accmode.con, i, bkc[i], blc[i], buc[i]);
                }

                task.putobjsense(mosek.objsense.minimize);
                task.putqobj(qsubi, qsubj, qval);
            }
            catch (mosek.Exception e)
            {
                Console.WriteLine(e.Code);
                Console.WriteLine(e);
            }
            #endregion
        }

        public void Optimize()
        {
            //DateTime start = DateTime.Now;
            double[] x = new double[count];
            try
            {
                task.optimize();
                task.solutionsummary(mosek.streamtype.msg);

                mosek.solsta solsta;
                mosek.prosta prosta;

                task.getsolutionstatus(mosek.soltype.itr,
                    out prosta,
                    out solsta);
                task.getsolutionslice(mosek.soltype.itr,
                    mosek.solitem.xx,
                    0,
                    count,
                    x);

                switch (solsta)
                {
                    case mosek.solsta.optimal:
                    case mosek.solsta.near_optimal:
                        //Console.WriteLine("Optimal primal solution\n");
                        break;
                    case mosek.solsta.dual_infeas_cer:
                    case mosek.solsta.prim_infeas_cer:
                    case mosek.solsta.near_dual_infeas_cer:
                    case mosek.solsta.near_prim_infeas_cer:
                        //Console.WriteLine("Primal or dual infeasibility.\n");
                        break;
                    case mosek.solsta.unknown:
                        //Console.WriteLine("Unknown solution status.\n");
                        break;
                    default:
                        //Console.WriteLine("Other solution status");
                        break;
                }
            }
            catch (mosek.Exception e)
            {
                Console.WriteLine(e.Code);
                Console.WriteLine(e);
            }

            //Console.WriteLine("{0}", DateTime.Now - start);

            for (int frame = 0; frame < nodePool.Length; frame++)
            {
                for (int i = 0; i < nodePool[frame].Count; i++)
                {
                    if (_app.Status.Config.LODAnim)
                        nodePool[frame][i].position.Push(x[index[nodePool[frame][i]]]);
                    else
                        nodePool[frame][i].position.Set(x[index[nodePool[frame][i]]]);
                }
            }

            double min = x.Min();
            for (int frame = 0; frame < nodePool.Length; frame++)
            {
                for (int i = 0; i < nodePool[frame].Count; i++)
                {
                    if (_app.Status.Config.LODAnim)
                        nodePool[frame][i].position.New -= min;
                    else
                        nodePool[frame][i].position.Set(nodePool[frame][i].position.New - min);
                }
            }
        }

    }
}