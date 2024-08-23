using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Serializable]
public class Graph
{
    // Dictionary to store neighbors for each GraphNode.
    public Dictionary<Vector3, List<Vector3>> adjacencyList = new Dictionary<Vector3, List<Vector3>>();
    public Dictionary<Vector3, GraphNode> graphNodes = new Dictionary<Vector3, GraphNode>();

    // Add a GraphNode to the graph.
    public void AddNode(GraphNode node)
    {
        if (!graphNodes.ContainsKey(node.Position))
        {
            graphNodes[node.Position] = node;
            adjacencyList[node.Position] = new List<Vector3>();
        }
    }

    // Add an edge (connection) between two GraphNodes.
    public void AddEdge(GraphNode fromNode, GraphNode toNode)
    {
        if (graphNodes.ContainsKey(fromNode.Position) && graphNodes.ContainsKey(toNode.Position))
        {
            adjacencyList[fromNode.Position].Add(toNode.Position);
            adjacencyList[toNode.Position].Add(fromNode.Position); // For an undirected graph
        }
    }

    public GraphNode GetNode(Vector3 position)
    {
        if (graphNodes.ContainsKey(position))
        {
            return graphNodes[position];
        }
        return null; // Return null if the node doesn't exist.
    }

    public int GetNodesCount()
    {
        return graphNodes.Count;
    }

    // Get neighbors of a GraphNode.
    public List<Vector3> GetNeighbors(Vector3 position)
    {
        if (adjacencyList.ContainsKey(position))
        {
            return adjacencyList[position];
        }
        return new List<Vector3>();
    }

    // Get the size of a GraphNode based on its position.
    public float GetNodeSize(Vector3 position)
    {
        if (graphNodes.ContainsKey(position))
        {
            return graphNodes[position].Size;
        }
        return 0; // Return a default size if the node doesn't exist.
    }

    public void Clear()
    {
        adjacencyList.Clear();
        graphNodes.Clear();
    }
}

[Serializable]
public class GraphNode
{
    public Vector3 Position { get; set; }
    public float Size { get; set; }

    public GraphNode(Vector3 position, float size)
    {
        Position = position;
        Size = size;
    }

    public GraphNode(Vector3 position)
    {
        Position = position;
        Size = 0; // You can set a default size here if needed.
    }

    public GraphNode()
    {
        Position = Vector3.zero;
        Size = 0;
    }
}