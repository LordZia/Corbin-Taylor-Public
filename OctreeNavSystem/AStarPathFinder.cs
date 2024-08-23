using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public static class AStarPathfinder
{
    public static List<Vector3> ShortestPath(Graph _graph, float _agentSize, float _occupiedNodesAvoidanceMultiplier, List<Vector3> _occupiedNodes, Vector3 _startPos, Vector3 _targetPos)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); 

        List<Vector3> shortestPathNodes = FindPath(_graph, _agentSize, _occupiedNodesAvoidanceMultiplier, _occupiedNodes, _startPos, _targetPos);

        if (shortestPathNodes != null && shortestPathNodes.Count > 0)
        {
            shortestPathNodes.Insert(0, _startPos); // ensures that the first node in the path is the same as startPos
            shortestPathNodes.Add(_targetPos); // ensures that the last node in the path is the same as targetPos
        }
        else
        {
            // Handle the case where no path was found.
            return new List<Vector3>();
        }

        stopwatch.Stop(); 
        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds; // Get the elapsed time in milliseconds
        //UnityEngine.Debug.Log($"A*Pathfinder took {elapsedMilliseconds} ms to calculate a valid path");

        return shortestPathNodes;
    }
    private static List<Vector3> FindPath(Graph _graph, float _agentSize, float _occupiedNodesAvoidanceMultiplier, List<Vector3> _occupiedNodes, Vector3 startPosition, Vector3 targetPosition)
    {
        Vector3 startNode = Vector3.zero;
        Vector3 targetNode = Vector3.zero;

        // pathfinding agents will generally pass in requests with the proper start and target position, the else statements are a backup.
        if (_graph.adjacencyList.ContainsKey(startPosition))
        {
            startNode = startPosition;
        }
        else { startNode = FindClosestNode(startPosition, _graph); }

        if (_graph.adjacencyList.ContainsKey(targetPosition))
        {
            targetNode = targetPosition;
        }
        else { targetNode = FindClosestNode(targetPosition, _graph); }

        if (startNode == Vector3.zero || targetNode == Vector3.zero)
        {
            UnityEngine.Debug.LogError("Start or target node not found.");
            if (startNode == Vector3.zero)
            {
                startNode = FindClosestNode(startPosition, _graph);
            }
            if (targetNode == Vector3.zero)
            {
                targetNode = FindClosestNode(targetPosition, _graph);
            }
        }

        SortedDictionary<float, Vector3> openSet = new SortedDictionary<float, Vector3>();
        // stores a list of nodes that are candidates for exploration and will be evaluated against eachother to find 
        // potential paths through the graph

        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        // log of the path being explored, used to set the path if a valid route is found.

        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float>();
        // gScore represents the cost of the shortest path found so far from the start node.
        // It is essentially the cumulative cost of reaching a node from the start node. During the execution of the
        // A* algorithm, the gScore is updated as the algorithm explores and discovers shorter paths.

        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float>();
        // fScore is the sum of the gScore and a heuristic estimate (hScore) of the cost from the current node to the goal node.
        // The fScore is used to prioritize nodes for exploration. The A* algorithm selects nodes with the lowest fScore to explore
        // next, with the goal of minimizing the total cost of the path.

        openSet.Add(0, startNode);
        gScore[startNode] = 0;
        fScore[startNode] = HeuristicCostEstimate(startNode, targetNode);

        Vector3 closestNode = startNode;

        while (openSet.Count > 0)
        {
            var current = openSet.First();
            openSet.Remove(current.Key);

            GraphNode node = _graph.GetNode(current.Value);
            //DebugUtility.DrawCube(node.Position, Vector3.one * node.Size, Quaternion.identity, Color.white, 5f);

            if (current.Value == targetNode) // pathfinder has found the targetNode, returning the path it found
            {
                return ReconstructPath(cameFrom, current.Value);
            }

            foreach (var neighbor in _graph.GetNeighbors(current.Value))
            {
                float neighborSize = _graph.GetNodeSize(neighbor);

      
 
                //if (_agentSize >= neighborSize) // skip nodes that are too small for the agent
                //{
                //    UnityEngine.Debug.Log("skipping small node");
                //    continue;
                //}

                float cost = Vector3.Distance(current.Value, neighbor);

                if (_agentSize >= neighborSize)
                {
                    GraphNode skipNode = _graph.GetNode(neighbor);
                    //DebugUtility.DrawCube(skipNode.Position, Vector3.one * skipNode.Size, Quaternion.identity, Color.magenta, 5f);
                    cost += 9999;
                }

                if (_occupiedNodes.Contains(neighbor)) // check occupied nodes list and adjust the cost accordingly
                {
                    cost *= _occupiedNodesAvoidanceMultiplier;
                }

                var tentativeGScore = gScore[current.Value] + cost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.Value;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, targetNode);
                    
                    openSet[fScore[neighbor]] = neighbor;

                    if (HeuristicCostEstimate(neighbor, targetNode) < HeuristicCostEstimate(closestNode, targetNode))
                    {
                        closestNode = neighbor;
                    }
                }
            }
        }

        if (startNode != Vector3.zero)
        {
            cameFrom[targetNode] = closestNode;
            return ReconstructPath(cameFrom, targetNode);


        }

        return null; // No path found
    }
    /*
    private static List<Vector3> FindPath(Graph _graph, float _agentSize, float _occupiedNodesAvoidanceMultiplier, List<Vector3> _occupiedNodes, Vector3 startPosition, Vector3 targetPosition)
    {
        Vector3 startNode = Vector3.zero;
        Vector3 targetNode = Vector3.zero;

        if (_graph.adjacencyList.ContainsKey(startPosition))
        {
            startNode = startPosition;
        }
        else { startNode = FindClosestNode(startPosition, _graph); }

        if (_graph.adjacencyList.ContainsKey(targetPosition))
        {
            targetNode = targetPosition;
        }
        else { targetNode = FindClosestNode(targetPosition, _graph); }


        if (startNode == Vector3.zero || targetNode == Vector3.zero)
        {
            UnityEngine.Debug.LogError("Start or target node not found.");
            return null;
        }

        SortedDictionary<float, Vector3> openSet = new SortedDictionary<float, Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float>();
        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float>();

        openSet.Add(0, startNode);
        gScore[startNode] = 0;
        fScore[startNode] = HeuristicCostEstimate(startNode, targetNode);

        while (openSet.Count > 0)
        {
            var current = openSet.First();
            openSet.Remove(current.Key);

            if (current.Value == targetNode)
            {
                return ReconstructPath(cameFrom, current.Value);
            }

            foreach (var neighbor in _graph.GetNeighbors(current.Value))
            {
                /*
                if (_obstructedNodes.Contains(neighbor)) // Skip obstructed nodes
                {
                    continue;
                }
                
                float neighborSize = _graph.GetNodeSize(neighbor);

                if (neighborSize <= _agentSize)
                {
                    continue;
                }

                // Calculate the cost of moving to the neighbor
                float cost = Vector3.Distance(current.Value, neighbor);

                if (_occupiedNodes.Contains(neighbor))
                {
                    cost *= _occupiedNodesAvoidanceMultiplier;
                }

                var tentativeGScore = gScore[current.Value] + cost;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.Value;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, targetNode);

                    openSet[fScore[neighbor]] = neighbor;
                }
            }
        }

        return null; // No path found
    }
    */

    private static Vector3 FindClosestNode(Vector3 position, Graph _graph)
    {
        if (_graph == null)
        {
            //UnityEngine.Debug.Log("Find Closest Node Requires A Valid Graph");
            return Vector3.zero;
        }

        float minDistance = float.MaxValue;
        Vector3 closestNode = Vector3.zero;

        foreach (var nodePosition in _graph.adjacencyList.Keys)
        {
            float distance = Vector3.Distance(position, nodePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = nodePosition;
            }
        }

        return closestNode;
    }

    private static float HeuristicCostEstimate(Vector3 node, Vector3 target)
    {
        Vector3 delta = target - node;
        return Vector3.Dot(delta, delta); // Squared Euclidean distance
    }

    private static List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new List<Vector3>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }
}
