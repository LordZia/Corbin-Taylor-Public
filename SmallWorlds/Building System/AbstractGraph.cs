using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractGraph<T>
{
    private Dictionary<T, List<T>> adjacencyList = new Dictionary<T, List<T>>();

    public Dictionary<T, List<T>> AdjanceyList => adjacencyList;

    // Add a node to the graph.
    public void AddNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<T>();
        }
    }

    // Add an edge (connection) between two nodes.
    public void AddEdge(T fromNode, T toNode)
    {
        if (adjacencyList.ContainsKey(fromNode) && adjacencyList.ContainsKey(toNode))
        {
            // Check if the edge already exists before adding it
            if (!EdgeExists(fromNode, toNode))
            {
                adjacencyList[fromNode].Add(toNode);
                adjacencyList[toNode].Add(fromNode); // For an undirected graph
            }
        }
    }

    // Get neighbors of a node.
    public List<T> GetNeighbors(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            return adjacencyList[node];
        }
        return new List<T>();
    }

    // Remove a node from the graph.
    public void RemoveNode(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            // Collect neighbors in a separate list
            List<T> neighbors = new List<T>(adjacencyList[node]);

            // Remove the node from its neighbors' adjacency lists
            foreach (var neighbor in neighbors)
            {
                adjacencyList[neighbor].Remove(node);
            }

            // Finally, remove the node from the graph
            adjacencyList.Remove(node);
        }
    }
    public void Clear()
    {
        adjacencyList.Clear();
    }

    // Check if an edge exists between two nodes.
    public bool EdgeExists(T fromNode, T toNode)
    {
        return adjacencyList.ContainsKey(fromNode) && adjacencyList[fromNode].Contains(toNode);
    }
}
