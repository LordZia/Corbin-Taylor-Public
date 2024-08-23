using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using UnityEngine;


public class Octree
{
    public Graph octreeGraph = new Graph();

    private List<Vector3> obstructedNodes = new List<Vector3>(); // stores nodes that are blocked by temporary / moving colliders 
    // stores paths that are current being used
    //paths are stored and used so that AI agents do not all follow the same path, this is intended to create interesting emerging behavior
    //Ai agents may opt to seek new paths when pathfinding by using "ignoreOccupiedPaths" when calculating their path. - zia
    private HashSet<(OctreeNode, OctreeNode)> neighborPairs = new HashSet<(OctreeNode, OctreeNode)>();
    private Dictionary<Vector3, OctreeNode> nodeTable = new Dictionary<Vector3, OctreeNode>();

    public OctreeNode rootNode;
    private List<OctreeNode> nodeIndex;
    private int maxNeighbors = 0;

    public OctreeNode RootNode { get { return rootNode; } }
    
    public List<OctreeNode> NodeIndex
    {
        get { return nodeIndex; }
        set { nodeIndex = value; }
    }
    public List<Vector3> ObstructedNodes { get { return obstructedNodes; } set { obstructedNodes = value; } }
    public HashSet<(OctreeNode, OctreeNode)> NeighborPairs
    {
        get { return neighborPairs; }
    }
    public Octree(Transform _octreeRegion, float _minNodeSize, int _maxNeighbors)
    {
        //calculate region
        float maxSize = Mathf.Max(_octreeRegion.localScale.x, _octreeRegion.localScale.y, _octreeRegion.localScale.z);
        Vector3 sizeVector = new Vector3(maxSize, maxSize, maxSize);
        

        Bounds bounds = new Bounds();
        bounds.size = sizeVector;
        bounds.center = _octreeRegion.position;

        this.maxNeighbors = _maxNeighbors;
        this.obstructedNodes = new List<Vector3>();

        nodeIndex = new List<OctreeNode>();
        rootNode = new OctreeNode(this, bounds, _minNodeSize);
        neighborPairs = new HashSet<(OctreeNode, OctreeNode)>();

        PopulateOctreeGraph();
        UnityEngine.Debug.Log($"Finished populating octree of size: {octreeGraph.adjacencyList.Keys.Count}");
    }

    public void PopulateOctreeGraph()
    {
        neighborPairs.Clear(); // Clear the existing pairs (in case you call this method again)

        List<OctreeNode> nodeIndex = this.NodeIndex; // Cache the node index for performance

        float rootNodeSizeSquared = Mathf.Pow(this.RootNode.NodeBounds.size.y, 2);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); // Start the stopwatch

        // Create an instance of the Graph class to represent the octreeGraph
        // Graph octreeGraph = new Graph(); // You can create the graph outside and pass it as a parameter

        // Iterate through all nodes to find neighbor pairs
        for (int i = 0; i < nodeIndex.Count; i++)
        {
            OctreeNode nodeA = nodeIndex[i];
            GraphNode graphNodeA = new GraphNode(nodeA.NodeBounds.center, nodeA.NodeBounds.size.y);

            nodeA.nodeID = i;
            nodeTable.Add(nodeA.NodeBounds.center, nodeA);

            if (nodeA.ContainsCollider)
            {
                continue;
            }

            if (nodeA != null && nodeA.NeighborNodes != null)
            {
                
                octreeGraph.AddNode(graphNodeA);

                foreach (OctreeNode neighbor in nodeA.NeighborNodes)
                {
                    if (neighbor != null)
                    {
                        var nodePair = (nodeA, neighbor);
                        neighborPairs.Add(nodePair);
                        GraphNode neighborNode = new GraphNode(neighbor.NodeBounds.center, neighbor.NodeBounds.size.y);

                        // Add an edge in the octreeGraph between nodeA's position and neighbor's position
                        octreeGraph.AddEdge(graphNodeA, neighborNode);
                    }
                }
            }

            if (octreeGraph.GetNeighbors(nodeA.NodeBounds.center).Count >= maxNeighbors)
            {
                continue;
            }

            // Continue to fill NeighborNodes HashSet based on distance
            for (int j = i + 1; j < nodeIndex.Count; j++)
            {
                OctreeNode nodeB = nodeIndex[j];
                GraphNode graphNodeB = new GraphNode(nodeB.NodeBounds.center, nodeB.NodeBounds.size.y);

                if (nodeB != null && !nodeA.NeighborNodes.Contains(nodeB))
                {
                    if (octreeGraph.GetNeighbors(nodeB.NodeBounds.center).Count >= maxNeighbors)
                    {
                        continue;
                    }

                    if (nodeB.ContainsCollider)
                    {
                        continue;
                    }

                    //Calculates the max distance for pairing based on the depth of both nodes in the comparison
       
                    //establish base vars
                    int depthDifference = Mathf.Abs(nodeA.Depth - nodeB.Depth);
                    
                    
                    // scales the max reach based on sizes of the nodes
                    float nodeASize = nodeA.NodeBounds.size.y ; 
                    float nodeBSize = nodeB.NodeBounds.size.y ;

                    //magic number is an approximation of the effect depth difference has on distance
                    //float depthDifferenceFactor = Mathf.Pow(0.62f, depthDifference); 
                    //float depthFactor = (nodeASize + nodeBSize) * depthDifferenceFactor;

                    float distance = Vector3.Distance(nodeA.NodeBounds.center, nodeB.NodeBounds.center);

                    if (distance <= (nodeASize + nodeBSize) * 0.75f)
                    {
                        Vector3 startPoint = nodeA.NodeBounds.center;
                        Vector3 endPoint = nodeB.NodeBounds.center;

                        if (!Physics.Raycast(startPoint, endPoint - startPoint, Vector3.Distance(startPoint, endPoint)))
                        {
                            UnityEngine.Debug.Log("raycast check hit a collider");
                            nodeA.NeighborNodes.Add(nodeB);
                            nodeB.NeighborNodes.Add(nodeA);

                            var nodePair = (nodeA, nodeB);
                            neighborPairs.Add(nodePair);

                            // Add an edge in the octreeGraph between nodeA's position and nodeB's position
                            octreeGraph.AddEdge(graphNodeA, graphNodeB);
                        }
                    }
                }
            }
        }
        stopwatch.Stop(); // Stop the stopwatch

        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds; // Get the elapsed time in milliseconds

        UnityEngine.Debug.Log($"PopulateNeighborPairs took {elapsedMilliseconds} ms to execute");
    }

    public OctreeNode GetNode(Vector3 nodePos)
    {
        OctreeNode node = null;
        if (nodeTable.TryGetValue(nodePos, out node))
        {
            return node;
        }
        else
        {
            return node;
        }
    }
    public OctreeNode FindClosestOctreeNode(Vector3 position) // first pass of find closest node
    {   
        OctreeNode closestNode = null;
        int distanceChecksPerformed = 0;
        closestNode = FindClosestOctreeNodeRecursive(position, this.rootNode, ref distanceChecksPerformed);
        // UnityEngine.Debug.Log($"recursive distance check was called : {distanceChecksPerformed} : times");
        return closestNode;
    }

    private OctreeNode FindClosestOctreeNodeRecursive(Vector3 position, OctreeNode currentNode, ref int checksCount) // searches the tree in a shallow -> deep depth approach by going down a branch
    {
        checksCount++;
        OctreeNode closestNode = currentNode;

        if (currentNode == null)
        {
            UnityEngine.Debug.Log("currentNode was null no rootNode was found");
            return closestNode;
        }

        if (currentNode.ContainsCollider && currentNode.NodeBounds.Contains(position))
        {
            OctreeNode closestChild = null;
            float closestDistanceSqr = float.MaxValue;

            if (currentNode.ChildNodes != null) // Check if ChildNodes array is not null
            {
                foreach (var childNode in currentNode.ChildNodes)
                {
                    if (childNode == null)
                    {
                        continue;
                    }

                    // Calculate squared distance (no need for square root to save on computation)
                    float distanceSqr = (position - childNode.NodeBounds.center).sqrMagnitude;

                    if (distanceSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        closestChild = childNode;
                    }
                }

                if (closestChild != null)
                {
                    // Recursively search in the closest child node to narrow down search.
                    OctreeNode result = FindClosestOctreeNodeRecursive(position, closestChild, ref checksCount);
                    if (result != null)
                    {
                        return result; // Return the valid result.
                    }
                    else if (currentNode != this.rootNode)
                    {
                        return this.rootNode;
                    }
                }
            }

            // If there are no child nodes or all child nodes contain colliders, return the current node.
            return currentNode;
        }

        return closestNode; // If no valid node is found, return the current node.
    }

    // Occupied Paths Methods

    // Add a Path to the Dictionary
 
    /*
    public List<OctreeNode> FindOctreeNodesAtSamePositions(List<Vector3> _positions)
    {
        List<OctreeNode> matchingOctreeNodes = new List<OctreeNode>();

        foreach (Vector3 position in _positions)
        {
            foreach (OctreeNode octreeNode in this.nodeIndex)
            {
                // Check if the positions match (considering a small tolerance for floating-point precision)
                if (Vector3.Distance(position, octreeNode.NodeBounds.center) < 0.001f)
                {
                    matchingOctreeNodes.Add(octreeNode);
                    break; // No need to continue searching for this path node
                }
            }
        }

        return matchingOctreeNodes;
    }
    
    public void PopulateNeighborPairs()
    {
        neighborPairs.Clear(); // Clear the existing pairs (in case you call this method again)

        List<OctreeNode> nodeIndex = this.NodeIndex; // Cache the node index for performance

        float rootSize = this.RootNode.NodeBounds.size.y * 20;

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); // Start the stopwatch

        // Iterate through all nodes to find neighbor pairs
        for (int i = 0; i < nodeIndex.Count; i++)
        {
            OctreeNode nodeA = nodeIndex[i];

            if (nodeA.ContainsCollider)
            {
                continue;
            }

            if (nodeA != null && nodeA.NeighborNodes != null)
            {
                foreach (OctreeNode neighbor in nodeA.NeighborNodes)
                {
                    if (neighbor != null)
                    {
                        var nodePair = (nodeA, neighbor);
                        neighborPairs.Add(nodePair);
                    }
                }
            }

            // Continue to fill NeighborNodes HashSet based on distance
            for (int j = i + 1; j < nodeIndex.Count; j++)
            {
                OctreeNode nodeB = nodeIndex[j];

                if (nodeB != null && !nodeA.NeighborNodes.Contains(nodeB))
                {
                    if (nodeB.ContainsCollider)
                    {
                        continue;
                    }

                    //float depthFactor = Mathf.Pow(0.5f, Mathf.Abs(nodeA.Depth - nodeB.Depth));

                    float depthFactor = 1.0f / Mathf.Pow(2.0f, nodeA.Depth);

                    float adjustedNeighborDistanceSqr = rootSize * depthFactor;


                    float distanceSqr = (nodeA.NodeBounds.center - nodeB.NodeBounds.center).sqrMagnitude;
                    //Debug.Log("nodeDistance = " + adjustedNeighborDistanceSqr);

                    if (distanceSqr <= adjustedNeighborDistanceSqr)
                    {
                        // Add nodes that are within the specified distance but not already neighbors
                        nodeA.NeighborNodes.Add(nodeB);
                        nodeB.NeighborNodes.Add(nodeA);

                        var nodePair = (nodeA, nodeB);
                        neighborPairs.Add(nodePair);
                    }
                }
            }
        }
        stopwatch.Stop(); // Stop the stopwatch

        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds; // Get the elapsed time in milliseconds

        UnityEngine.Debug.Log($"PopulateNeighborPairs took {elapsedMilliseconds} ms to execute");

    }
    */
}
