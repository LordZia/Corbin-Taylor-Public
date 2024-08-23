using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class PathSmoother
{
    public static List<Vector3> SmoothPath(List<Vector3> positions)
    {
        List<Vector3> waypoints = new List<Vector3>();
        List<Vector3> path = new List<Vector3>();

        if (positions.Count < 4)
        {
            UnityEngine.Debug.LogError("Smooth path calculator requires at least 4 waypoints.");
            return positions;
        }

        waypoints = new List<Vector3>(positions); // Copy the list of positions to waypoints

        path = new List<Vector3>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[i + 1];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)];

            for (float t = 0; t < 1; t += 0.1f)
            {
                Vector3 position = GetCatmullRomPoint(p0, p1, p2, p3, t);
                path.Add(position);
            }
        }

        path.Add(waypoints[waypoints.Count - 2]);

        return path;
    }

    private static Vector3 GetCatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Catmull-Rom spline interpolation formula
        return 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
    }
}
    /*
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float deceleration = 4f;
    [SerializeField] private Transform transformToMove;
    [SerializeField] private Rigidbody rigidbody;

    [SerializeField] private CreateOctree octree;
    [SerializeField] private bool DEBUG_SHOW_PATH;

    private LineRenderer lineRenderer;
    private List<Vector3> waypoints = new List<Vector3>(); // Initialize an empty list

    private List<Vector3> path = new List<Vector3>();
    private int currentWaypoint = 0;
    private float distanceThreshold = 2.5f;

    private float currentSpeed = 0f; // Start with zero speed
    
    private bool reachedFullSpeed = false;


    private void Awake()
    {
        if (!DEBUG_SHOW_PATH) 
        {
            return;
        }

        rigidbody = this.GetComponent<Rigidbody>(); 
        if (rigidbody == null)
        {
            UnityEngine.Debug.LogError("Smooth path follower requires a rigid body component");
            return;
        }

        octree = GameObject.Find("OctreeNavMeshRegion").GetComponent<CreateOctree>();
        if (octree == null)
        {
            UnityEngine.Debug.LogError("CreateOctree component not found on OctreeNavMeshRegion GameObject.");
        }

        // Initialize the LineRenderer component.
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    public void SetWaypoints(List<Vector3> positions)
    {
        if (positions.Count < 4)
        {
            UnityEngine.Debug.LogError("Smooth path calculator requires at least 4 waypoints.");
            return;
        }

        waypoints = new List<Vector3>(positions); // Copy the list of positions to waypoints

        path = new List<Vector3>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 p0 = waypoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = waypoints[i];
            Vector3 p2 = waypoints[i + 1];
            Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)];

            for (float t = 0; t < 1; t += 0.1f)
            {
                Vector3 position = GetCatmullRomPoint(p0, p1, p2, p3, t);
                path.Add(position);
            }
        }

        path.Add(waypoints[waypoints.Count - 2]);
        

        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }

    private void Update()
    {
        if (octree != null)
        {
            if (octree.ShortestPathNodes.Count != waypoints.Count)
            {
                currentWaypoint = 0;
                SetWaypoints(octree.ShortestPathNodes);
            }
        }

        if (currentWaypoint >= path.Count)
        {
            return; // Reached the end of the path.
        }

        Vector3 targetPosition = path[currentWaypoint];
        float distanceToWaypoint = Vector3.Distance(transformToMove.position, targetPosition);

        // Check if the object is close enough to the waypoint

        if (distanceToWaypoint < distanceThreshold)
        {
            if (currentWaypoint != path.Count)
            {
                currentWaypoint++; // Move to the next point.
            }
            else
            {
                UnityEngine.Debug.Log("reached end of path");
                rigidbody.velocity = Vector3.zero;
            }
            return;
        } 
       

        // Calculate a desired velocity vector
        Vector3 desiredVelocity = (targetPosition - transformToMove.position).normalized * speed;
        UnityEngine.Debug.DrawLine(transformToMove.position, transformToMove.position + desiredVelocity, Color.red);

        // Calculate a steering force to reach the desired velocity
        Vector3 steeringForce = desiredVelocity - rigidbody.velocity;

        // Apply the steering force to the rigidbody
        rigidbody.AddForce(steeringForce, ForceMode.Acceleration);
    }

    private Vector3 GetCatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Catmull-Rom spline interpolation formula
        return 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
    }
}
    */