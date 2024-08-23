using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OctreePathfindingManager : MonoBehaviour
{
    [SerializeField] private bool Debug_EnableLogs = false;
    private Queue<PathfindingRequest> pathfindingQueue = new Queue<PathfindingRequest>();
    private Dictionary<int, List<Vector3>> occupiedPaths = new Dictionary<int, List<Vector3>>();
    private int pathCount = 0;

    private bool isPathfindingInProgress = false;
    public int PathCount { get { return pathCount; } }
    public Dictionary<int, List<Vector3>> OccupiedPaths { get { return occupiedPaths; } }

    public int RequestPathfinding(PathfindingRequest request)
    {
        pathfindingQueue.Enqueue(request);

        if (!isPathfindingInProgress)
        {
            StartCoroutine(ProcessPathfindingQueue());
        }

        int pathId = GetSmallestAvailablePathId(); // Get the smallest available path ID
        request.PathId = pathId; // Assign the path ID to the request

        return pathId; // Return the path ID to the calling agent
    }

    private IEnumerator ProcessPathfindingQueue()
    {
        isPathfindingInProgress = true;

        while (pathfindingQueue.Count > 0)
        {
            PathfindingRequest request = pathfindingQueue.Dequeue();
            yield return StartCoroutine(PerformPathfinding(request));
        }

        isPathfindingInProgress = false;
    }

    private IEnumerator PerformPathfinding(PathfindingRequest request)
    {
        // Pass PathfindingRequest values into AStarPathfinder to calculate a path
        List<Vector3> path = AStarPathfinder.ShortestPath(
            request.Octree.octreeGraph,
            request.AgentSize,
            request.OccupiedNodesAvoidanceScale,
            request.Octree.ObstructedNodes,
            request.StartPosition,
            request.TargetPosition
        );

        //int pathId = GetSmallestAvailablePathId(); // Get the smallest available path ID
        int pathId = request.AgentId;

        // Add the path to the dictionary with the assigned path ID
        AddPathToOccupiedPaths(request.AgentId, path);
        pathCount = occupiedPaths.Count;

        if (Debug_EnableLogs)
        {
            Debug.Log
            (
                 $"<color=red>agent{request.AgentId}</color> with a size of " +
                 $"<color=green>{request.AgentSize}</color> created a path with id of " +
                 $"<color=blue>{pathId}</color> and added an occupied list of positions to octree with an id of " +
                 $"<color=green>{request.AgentId}</color>"
            );
        }

        // pass the agent the generated path list and id;
        request.Callback(path, pathId);

        // Introduce a delay of 1 second
        yield return new WaitForSeconds(0.1f);
    }

    public void RemovePathingRequestByAgentId(int targetAgentId)
    {
        // Create a temporary queue to store the requests that need to be preserved
        Queue<PathfindingRequest> tempQueue = new Queue<PathfindingRequest>();

        // Search for the target PathId and remove the corresponding request
        while (pathfindingQueue.Count > 0)
        {
            PathfindingRequest request = pathfindingQueue.Dequeue();

            if (request.AgentId != targetAgentId)
            {
                // Preserve requests with PathIds different from the target
                tempQueue.Enqueue(request);
            }
            else
            {
                // Optionally handle the removed request (e.g., notify the caller)
                if (Debug_EnableLogs) Debug.Log($"Canceled request with PathId {targetAgentId}");
            }
        }

        // Update the original queue with the preserved requests
        pathfindingQueue = new Queue<PathfindingRequest>(tempQueue);
    }


    //Occupied Paths Methods
    public void AddPathToOccupiedPaths(int _pathId, List<Vector3> _path)
    {
        occupiedPaths[_pathId] = _path;
        
    }

    // Append a Path to the End of an Existing Path
    public void AppendPathToOccupiedPath(int pathId, List<Vector3> pathToAppend)
    {
        if (occupiedPaths.ContainsKey(pathId))
        {
            List<Vector3> existingPath = occupiedPaths[pathId];
            existingPath.AddRange(pathToAppend);
            occupiedPaths[pathId] = existingPath;
        }
        else
        {
            //if path does not exist add it to the registry
            int smallestAvailableId = 0;

            // Find the smallest available path ID
            while (occupiedPaths.ContainsKey(smallestAvailableId))
            {
                smallestAvailableId++;
            }
            AddPathToOccupiedPaths(smallestAvailableId, pathToAppend);
        }
    }

    // Clear a Path from the Dictionary
    public void ClearPathFromOccupiedPaths(int pathId)
    {
        if (occupiedPaths.ContainsKey(pathId))
        {
            occupiedPaths.Remove(pathId);
        }
        else
        {
            // Handle the case where the specified pathId does not exist in the dictionary.
            // Log a message or handle it as needed.
        }
    }

    //Utility Functions
    private int GetSmallestAvailablePathId()
    {
        // Get a list of used IDs from the dictionary
        List<int> usedIds = new List<int>(occupiedPaths.Keys);

        // Sort the list of used IDs in ascending order
        usedIds.Sort();

        // Find the smallest available ID by checking for gaps in the sorted list
        for (int i = 1; i <= usedIds.Count; i++)
        {
            if (usedIds[i - 1] != i)
            {
                // Found a gap, use the current value of 'i'
                return i;
            }
        }

        // No gaps found, use the next sequential ID
        return usedIds.Count + 1;
    }
}

public struct PathfindingRequest
{
    public int AgentId;
    public Octree Octree;
    public Vector3 StartPosition;
    public Vector3 TargetPosition;
    public float OccupiedNodesAvoidanceScale;
    public float AgentSize;
    public System.Action<List<Vector3>, int> Callback;
    public int PathId; // Add a field to store the path ID

}
