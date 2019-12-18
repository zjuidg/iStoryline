using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Structure;
using System.Diagnostics;
using Storyline;

/// <summary>
/// Based on the result of locationtreecalculator, we change the permutation of characters according to a list of constraints. 
/// </summary>
/// 

namespace Algorithm.PermutationCalculator
{
    class ConstraintCalculator
    {
        private StorylineApp _app;
        private int frameCount;
        public ConstraintCalculator(StorylineApp app)
        {
            _app = app;
        }

        public PositionTable<int> Reorder(Story story, PositionTable<int> perm)
        {
            // initialize new ordering
            PositionTable<int> _perm = new PositionTable<int>(story.Characters.Count, story.FrameCount);
            frameCount = story.FrameCount;
            for (int i = 0; i < story.Characters.Count; ++i)
                for (int j = 0; j < story.FrameCount; ++j)
                    _perm[i, j] = perm[i, j];
            OrderContainer orderContainer = new OrderContainer(frameCount, _app.Status.Config.Orders, _app.Status.Config.OrderTable, story);
            List<Tuple<int, int>> constraints = orderContainer.OrderTable;
            // max iteration times
            int maxItr = 1;
            for (int i=0; i < maxItr; i++)
            {
                // forward sweep
                for (int frame = 0; frame < story.FrameCount; frame++)
                {
                    // NOTICE!!! constraints will be changed during reordering.
                    List<Node> sortedList = ReorderFrame(story, _perm, constraints, frame, frame - 1);
                    // except rabbit case
                    int rabbitId = story.rabbitId;
                    // new ordering
                    for (int j=0; j<sortedList.Count; j++)
                    {
                        int characterId = sortedList[j].ID;
                        if (characterId > -1)
                            _perm[characterId, frame] = j;
                    }
                    List<int> sortedOrder = orderContainer.SortedOrderTable[frame];
                    if (sortedOrder.Count > 0)
                    {
                        for (int k = 0; k < story.Characters.Count; k++)
                        {
                            if (_perm[k, frame] > -1)
                            {
                                _perm[k, frame] = sortedOrder[k];
                            }
                        }
                    }

                    // force rabbit to the first place
                    int rabbitPos = _perm[rabbitId, frame];
                    for (int k = 0; k < story.Characters.Count; k++)
                    {
                        int pos = _perm[k, frame];
                        if (pos >= 0 && pos < rabbitPos)
                        {
                            _perm[k, frame] += 1;
                        }
                    }

                    _perm[rabbitId, frame] = 0;
                }
            }
            return _perm;
        }

        public List<Node> ReorderFrame(Story story, PositionTable<int> perm, List<Tuple<int, int>> orders, int frame, int reference)
        {
            // initialize nodes
            Dictionary<int, Node> nodeDict = Init(story, perm, frame, reference);
            // initialize constraints
            Constrainer ctr = new Constrainer(orders, nodeDict, perm, frame);
            // initialize constrainted and unconstrained nodes
            List<Node> cNodeList = new List<Node>();
            List<Node> uNodeList = new List<Node>();
            foreach (var node in nodeDict.Values)
            {
                if (ctr.cNodes.Contains(node.ID))
                    cNodeList.Add(node);
                else
                    uNodeList.Add(node);
            }
            Tuple<int, int> constraint = ctr.FindViolatedConstraint();
            int nodeCount = story.Characters.Count + 100;
            while (constraint.Item1 != constraint.Item2)
            {
                int id = nodeCount++;
                Node cNode = new Node(id, -1);
                nodeDict.Add(id, cNode);
                Node sNode = nodeDict[constraint.Item1];
                Node tNode = nodeDict[constraint.Item2];
                cNode.Indeg = sNode.Indeg + tNode.Indeg;
                // cNode.BaryCenter = (sNode.BaryCenter * sNode.Indeg + tNode.BaryCenter * tNode.Indeg) / cNode.Indeg;
                cNode.L = sNode.L.Concat(tNode.L).ToList();
                // cNode.I = sNode.I.Concat(tNode.I).ToList();
                cNode.GetBaryCenter2();
                ctr.RemoveSelfLoops(cNode.ID, sNode.ID, tNode.ID);
                // if cNode has no incident constraints
                if (!ctr.cNodes.Contains(cNode.ID))
                    uNodeList.Add(cNode);
                // update constrained nodes
                cNodeList = UpdateConstranedNodes(cNodeList, ctr);
                constraint = ctr.FindViolatedConstraint();
            }
            // union constrained and unconstrained nodes
            List<Node> unionNodeList = cNodeList.Concat(uNodeList).ToList();
            // sort nodes according to their barycenters
            List<Node> sortedUnionNodeList = BaryCenterSort(unionNodeList);
            List<Node> finalNodeList = new List<Node>();
            foreach (var node in sortedUnionNodeList)
                finalNodeList = finalNodeList.Concat(node.L).ToList();
            return finalNodeList;
        }

        public Dictionary<int, Node> Init(Story story, PositionTable<int> perm, int frame, int reference)
        {
            Dictionary<int, Node> nodeDict = new Dictionary<int, Node>();
            SessionTable sessionTable = story.SessionTable;
            Dictionary<int, List<int>> sessionMap = new Dictionary<int, List<int>>();
            for (int character = 0; character < story.Characters.Count; character++)
            {
                int sessionId = sessionTable[character, frame];
                if (sessionId > -1 && !sessionMap.ContainsKey(sessionId))
                    sessionMap.Add(sessionId, new List<int>());
            }
            for (int character = 0; character < story.Characters.Count; character++)
            {
                int sessionId = sessionTable[character, frame];
                if (sessionId > -1)
                {
                    List<int> session = sessionMap[sessionId];
                    session.Add(character);
                }
            }
            int id = -1;
            int pos = -1;
            int nodeCount = story.Characters.Count;
            foreach (var session in sessionMap.Values)
            {
                if (session.Count == 1)
                {
                    id = session[0];
                    pos = perm[id, frame];
                    if (pos > -1)
                    {
                        Node tempNode = new Node(id, pos);
                        tempNode.GetBaryCenterRef(perm, id, frame, reference, frameCount);
                        tempNode.L = new List<Node>();
                        tempNode.L.Add(tempNode);
                        nodeDict.Add(id, tempNode);
                    }
                }
                else
                {
                    List<int> characters = SortCharacters(session, perm, frame);
                    id = nodeCount++;
                    Node sessionNode = new Node(id, -1);
                    foreach (var nodeID in characters)
                    {
                        pos = perm[nodeID, frame];
                        Node tempNode = new Node(nodeID, pos);
                        tempNode.GetBaryCenterRef(perm, nodeID, frame, reference, frameCount);
                        tempNode.L = new List<Node>();
                        tempNode.L.Add(tempNode);
                        sessionNode.L.Add(tempNode);
                    }
                    // sort nodes within session
                    sessionNode.L = BaryCenterSort(sessionNode.L);
                    sessionNode.GetBaryCenter2();
                    nodeDict.Add(id, sessionNode);
                }
            }
            return nodeDict;
        }
        public List<int> SortCharacters(List<int> characters, PositionTable<int> perm, int frame)
        {
            List<int> sortedCharacters = new List<int>();
            sortedCharacters.Add(characters[0]);
            for (int i = 1; i < characters.Count; i++)
            {
                bool isInserted = false;
                for (int j = 0; j < sortedCharacters.Count; j++)
                {
                    if (perm[characters[i], frame] < perm[sortedCharacters[j], frame] && perm[characters[i], frame] > -1)
                    {
                        sortedCharacters.Insert(j, characters[i]);
                        isInserted = true;
                        break;
                    }
                }
                if (!isInserted)
                {
                    sortedCharacters.Add(characters[i]);
                }
            }
            return sortedCharacters;
        }
        public List<Node> UpdateConstranedNodes(List<Node> cNodes, Constrainer ctr)
        {
            List<Node> newCNodes = new List<Node>();
            foreach (var cNode in cNodes)
                if (ctr.cNodes.Contains(cNode.ID))
                    newCNodes.Add(cNode);
            return newCNodes;
        }
        public List<Node> BaryCenterSort(List<Node> nodes)
        {
            return nodes.OrderBy(node => node.BaryCenter).ToList();
        }
    }

    public class Node
    {
        private int id = -1;
        public int ID
        {
            get
            {
                return id;
            }
        }
        private int pos = -1;
        public int Pos
        {
            get
            {
                return pos;
            }
        }
        // incoming constraints.
        public List<Node> I = new List<Node>();
        // contained nodes
        public List<Node> L = new List<Node>();
        public double BaryCenter = 0;
        public int Indeg = 0;
        public Node(int nodeID, int position)
        {
            id = nodeID;
            pos = position;
        }
        public void GetBaryCenter()
        {
            BaryCenter = pos;
        }
        public void GetBaryCenter2()
        {
            double sum = 0;
            foreach (var node in L)
            {
                sum += node.BaryCenter;
            }
            BaryCenter = sum / L.Count;
        }
        public void GetBaryCenterRef(PositionTable<int> perm, int node, int frame, int reference, int frameCount)
        {
            if (reference == -1 || reference == frameCount)
            {
                BaryCenter = perm[node, frame];
            }
            else if (perm[node, reference] == -1)
            {
                BaryCenter = perm[node, frame];
            }
            else
            {
                BaryCenter = perm[node, reference];
            }

        }
    }

    public class Constrainer
    {
        public Dictionary<int, Node> nodeDict;
        public List<Tuple<int, int>> constraints;
        public List<int> cNodes = new List<int>();
        public Constrainer(List<Tuple<int, int>> orders, Dictionary<int, Node> nodes, PositionTable<int> perm, int frame)
        {
            nodeDict = nodes;
            InitConstraints(orders, perm, frame);
            UpdateConstranedNodes();
        }
        public void InitConstraints(List<Tuple<int, int>> orders, PositionTable<int> perm, int frame)
        {
            constraints = new List<Tuple<int, int>>();
            foreach (var order in orders)
            {
                int sourcePos = perm[order.Item1, frame];
                int targetPos = perm[order.Item2, frame];
                // if (sourcePos > targetPos && targetPos != -1)
                if (sourcePos != -1 && targetPos != -1)
                    UpdateConstraint(order);
            }
        }
        public void UpdateConstraint(Tuple<int, int> order)
        {
            int sourceID = order.Item1;
            int targetID = order.Item2;
            int newSourceID = -1;
            int newTargetID = -1;
            foreach (var sessionNode in nodeDict.Values)
            {
                foreach (var node in sessionNode.L)
                {
                    if (node.ID == sourceID)
                    {
                        newSourceID = sessionNode.ID;
                    }
                    if (node.ID == targetID)
                    {
                        newTargetID = sessionNode.ID;
                    }
                }
            }
            if (newSourceID == -1)
            {
                newSourceID = sourceID;
            }
            if (newTargetID == -1)
            {
                newTargetID = targetID;
            }
            if (newSourceID != newTargetID)
            {
                Tuple<int, int> constraint = new Tuple<int, int>(newSourceID, newTargetID);
                if (!constraints.Contains(constraint))
                {
                    constraints.Add(constraint);
                }
            }
        }
        public void UpdateConstranedNodes()
        {
            cNodes = new List<int>();
            foreach (var constraint in constraints)
            {
                int sourceID = constraint.Item1;
                int targetID = constraint.Item2;
                if (!cNodes.Contains(sourceID))
                {
                    cNodes.Add(sourceID);
                }
                if (!cNodes.Contains(targetID))
                {
                    cNodes.Add(targetID);
                }
            }
        }
        // choose constraint that source and target nodes are closet
        public Tuple<int, int> FindViolatedConstraint()
        {
            double minBaryCenter = 10000;
            Tuple<int, int> minConstraint = new Tuple<int, int>(-1, -1);
            foreach (var constraint in constraints)
            {
                double tmpBaryCenter = GetBaryCenterForConstraint(constraint);
                if (tmpBaryCenter < minBaryCenter)
                {
                    minConstraint = constraint;
                    minBaryCenter = tmpBaryCenter;
                }
            }
            return minConstraint;
        }
        public double GetBaryCenterForConstraint(Tuple<int, int> constraint)
        {
            int sourceID = constraint.Item1;
            int targetID = constraint.Item2;
            // return nodeDict[sourceID].BaryCenter < nodeDict[targetID].BaryCenter ? nodeDict[sourceID].BaryCenter : nodeDict[targetID].BaryCenter;
            double delta = nodeDict[sourceID].BaryCenter - nodeDict[targetID].BaryCenter;
            return delta < 0 ? -delta : delta;
        }
        public void RemoveSelfLoops(int cNodeId, int sNodeId, int tNodeId)
        {
            List<Tuple<int, int>> _constraints = new List<Tuple<int, int>>();
            for (int i = 0; i < constraints.Count; i++)
            {
                if (constraints[i].Item1 == sNodeId || constraints[i].Item1 == tNodeId)
                    constraints[i] = new Tuple<int, int>(cNodeId, constraints[i].Item2);
                if (constraints[i].Item2 == sNodeId || constraints[i].Item2 == tNodeId)
                    constraints[i] = new Tuple<int, int>(constraints[i].Item1, cNodeId);
            }
            foreach (var constraint in constraints)
                if (constraint.Item1 != constraint.Item2)
                    if (!_constraints.Contains(constraint))
                        _constraints.Add(constraint);
            constraints = _constraints;
            UpdateConstranedNodes();
        }
    }

    public class OrderContainer
    {
        public List<Tuple<int, int>> OrderTable = new List<Tuple<int, int>>();
        public Dictionary<int, List<int>> SortedOrderTable = new Dictionary<int, List<int>>();
        private bool isTestMode = false;
        private Story _story;
        public OrderContainer(int frameCount, List<Tuple<int, int>> orders, List<Tuple<int, List<int>>> sortedOrder, Story story)
        {
            _story = story;
            OrderTable = orders;
            for (int i = 0; i < frameCount; i++)
            {
                List<int> tmpSortedOrder = new List<int>();
                SortedOrderTable.Add(i, tmpSortedOrder);
            }
            sortedOrder.ForEach(pair =>
            {
                SortedOrderTable[pair.Item1] = pair.Item2;
            });
            if (isTestMode)
            {
                Test();
            }
        }
        
        public void Test()
        {
//            OrderTable.Clear();
            for (int i = 0; i < _story.Characters.Count; i++)
            {
                if (i != _story.rabbitId)
                {
                    OrderTable.Add(new Tuple<int, int>(_story.rabbitId, i));
                }
            }
        }

        public List<Node> TREXNoOne(List<Node> nodes)
        {
            List<Node> newNodes = new List<Node>();
            foreach (var node in nodes)
            {
                if (node.ID == 0)
                {
                    newNodes.Add(node);
                }
            }
            foreach (var node in nodes)
            {
                if (node.ID != 0)
                {
                    newNodes.Add(node);
                }
            }
            return newNodes;
        }
    }
}