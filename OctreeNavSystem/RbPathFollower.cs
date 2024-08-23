using System.Collections.Generic;
using UnityEngine;

public class RbPathFollower : MonoBehaviour
{
    private OctreePathfindingManager pathfindingManager;
    [SerializeField] private int agentId = -1; // need to add functionality to automatically assign agentIds on spawn - zia
                                               // for now these need to be manually assigned
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private Vector3 DefaultPosition;
    [SerializeField] public Transform targetTransform;
    [SerializeField] private float loseAggroDelay = 10f;

    [SerializeField] private Vector3 targetOffset = Vector3.zero;
    [SerializeField] private float targetMoveUpdatePathThreshold = 1.0f;
    [SerializeField] private int targetOctreeIndex = 0;
    [SerializeField] private float occupiedNodesAvoidanceMultiplier = 0;

    [SerializeField] private bool DEBUG_DRAW_PATH_LINE = false;
    [SerializeField] private bool DEBUG_DRAW_PATH_NODES = false;
    [SerializeField] private Color DEBUG_DRAW_COLOR = Color.white;

    private List<Vector3> path = new List<Vector3>();
    [SerializeField] private int currentPathId = -1;

    private CreateOctree OctreeManager;
    private Octree octree;
    private List<OctreeNode> pathNodes = new List<OctreeNode>();

    private Rigidbody rigidbody;

    private int currentPathIndex = 0;
    private int currentWaypoint = 0;
    private float distanceThreshold = 2.5f;
    private bool disablePathing = false;

    private Vector3 previousTargetPos = Vector3.zero;
    private Graph navMeshGraph = null;

    private LineRenderer lineRenderer;

    private int checkTargetPosDelay = 120; // fixed updates between distance checks. 
    private int checkTargetPosCounter = 0;

    public bool DiablePathing { set { disablePathing = value; } }
    public int TargetOctreeIndex 
    { 
        get { return targetOctreeIndex; }
        set { targetOctreeIndex = value; }
    }
    public int AgentId
    {
        get { return agentId; }
        set { agentId = value; }
    }

    public void Awake()
    {
        rigidbody = this.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            UnityEngine.Debug.LogError("Smooth path follower requires a rigid body component");
            return;
        }

        OctreeManager = GameObject.Find("OctreeNavMeshRegion").GetComponent<CreateOctree>();
        if (OctreeManager == null)
        {
            UnityEngine.Debug.LogError("CreateOctree component not found on OctreeNavMeshRegion GameObject.");
        }

        if (rigidbody == null)
        {
            rigidbody = this.gameObject.AddComponent<Rigidbody>();
        }

        pathfindingManager = FindObjectOfType<OctreePathfindingManager>();
        

        if (pathfindingManager == null)
        {
            Debug.LogError("failed to find pathfindingManager");
        }

        targetTransform = FindObjectOfType<_Player>().GetComponent<Transform>();

        SetTargetOctree(targetOctreeIndex);

        if (octree != null)
        {
            RequestPathfinding(targetTransform.position + targetOffset);
        }

        DefaultPosition = this.transform.position;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        previousTargetPos = targetTransform.position;
    }
    private void FixedUpdate()
    {
        if (disablePathing)
        {
            return;
        }
        if (octree == null)
        {
            return;
        }
        if (path.Count != 0)
        {
            DrawShortestPath(path);

            if (currentWaypoint >= path.Count)
            {
                if (this.rigidbody != null)
                {
                    this.rigidbody.isKinematic = true;
                }
                currentWaypoint = 0;
                // Reached the end of the path.

                //Clear any previously stored occupied paths and any pending pathfinding requests
                ClearPathfindingData();

                return;
            }

            MoveRbAlongPath();
        }

        if (checkTargetPosCounter < checkTargetPosDelay)
        {
            checkTargetPosCounter++;
            return;
        }
        checkTargetPosCounter = 0;

        Vector3 targetPos = targetTransform.position;
        float sqrDistanceMoved = (targetPos - previousTargetPos).sqrMagnitude;

        if (sqrDistanceMoved >= Mathf.Pow(targetMoveUpdatePathThreshold, 2)) // only redraw the path when the target has moved enough to warrent a new path creation.
        {
            FindPath(targetPos);
        }
    }

    private void FindPath(Vector3 targetPos)
    {
        if (octree.rootNode.NodeBounds.Contains(targetPos)) // prevents pathfinding when target is not within the octree
        {
            this.rigidbody.isKinematic = false;
            RequestPathfinding(targetPos + targetOffset); // submits pathfindingreqest to pathing manager

            if (path.Count == 0) // pathfinder returned a path of length 0, no valid path found reqeust pathing to default position
            {
                ClearPathfindingData();
                ReturnToDefaultPos();
            }
        }
        
    }

    private void ReturnToDefaultPos()
    {
        RequestPathfinding(DefaultPosition); // submits pathfindingreqest to pathing manager
    }

    public void RequestPathfinding(Vector3 targetPos)
    {
        if (octree == null)
        {
            Debug.LogError("No Octree Set For RBPath Follower");
            return;
        }

        if (navMeshGraph == null)
        {
            Debug.LogError("No NavMeshGraph Set For RBPath Follower");
            return;
        }

        if (this.transform == null)
        {
            Debug.LogError("RBPathFollower has no transform component.");
            return;
        }

        if (targetTransform == null)
        {
            Debug.LogError("No TargetTransform Set For RBPath Follower");
            return;
        }

        //handle occupiedNoides

        //List<Vector3> obstructedNodes = new List<Vector3>();

        // Calculate ShortestPath
        // establish vars
        Vector3 closestStartNode = octree.FindClosestOctreeNode(this.transform.position).NodeBounds.center;
        Vector3 closestTargetNode = octree.FindClosestOctreeNode(targetPos).NodeBounds.center;
        float objectMaxSize = Mathf.Max(this.transform.localScale.x, this.transform.localScale.y, this.transform.localScale.z);

        ClearPathfindingData();

        //Send pathing request to pathfinding manager
        pathfindingManager.RequestPathfinding(new PathfindingRequest
        {
            AgentId = agentId,
            Octree = octree,
            StartPosition = transform.position,
            TargetPosition = targetPos,
            AgentSize = objectMaxSize,
            OccupiedNodesAvoidanceScale = occupiedNodesAvoidanceMultiplier,
            Callback = OnPathfindingResult
        });

        // Update the previous position
        previousTargetPos = targetTransform.position;

        if (DEBUG_DRAW_PATH_LINE)
        {
            if (path != null)
            {
                if (path.Count > 0)
                {
                    lineRenderer.positionCount = path.Count;
                    lineRenderer.SetPositions(path.ToArray());
                }
            }
        }
    }

    public void ClearPathfindingData()
    {

        //Clear any previous pathing attempts
        pathfindingManager.RemovePathingRequestByAgentId(agentId);

        //Clear any previously stored occupied paths
        pathfindingManager.ClearPathFromOccupiedPaths(agentId);
    }

    private void OnPathfindingResult(List<Vector3> resultPath, int _pathId)
    {
        // Handle the path result
        this.rigidbody.isKinematic = false;
        path = resultPath;
        currentPathId = _pathId;
        //Debug.Log("Pathing Callback returned path of Length " + path.Count);

        /* Validate the obtained pathNodes
        if (path != null && path.Count > 0)
        {
            pathNodes.Clear();
            foreach (Vector3 node in path)
            {
                OctreeNode octreeNode = octree.FindClosestOctreeNode(node);
                if (octreeNode != null)
                {
                    pathNodes.Add(octreeNode);
                }
                else
                {
                    Debug.LogWarning("A path node has no corresponding OctreeNode.");
                }
            }
        }
        else
        {
            Debug.Log("no valid path found returning to default position");
        }
        */

        // Update the previous position
        previousTargetPos = targetTransform.position;
    }

    private void OnDrawGizmos()
    {
        if (DEBUG_DRAW_PATH_NODES)
        {
            Gizmos.color = DEBUG_DRAW_COLOR;

            if (octree == null) 
            {
                return;
            }

            foreach (OctreeNode node in pathNodes) 
            {
                switch (node.Depth)
                {
                    case 0:
                        Gizmos.color = Color.white;
                        break;
                    case 1:
                        Gizmos.color = Color.blue;
                        break;
                    case 2:
                        Gizmos.color = Color.white;
                        break;
                    case 3:
                        Gizmos.color = Color.yellow;
                        break;
                    case 4:
                        Gizmos.color = Color.red;
                        break;
                    case 5:
                        Gizmos.color = Color.cyan;
                        break;
                    case 6:
                        Gizmos.color = Color.white;
                        break;
                    default:
                        Gizmos.color = Color.magenta;
                        break;
                }

                Gizmos.DrawWireCube(node.NodeBounds.center, node.NodeBounds.size);
            }
            
        }
    }
    private void MoveRbAlongPath()
    {
        if (path == null || path.Count == 0)
        {
            // Handle cases where the path is empty or null.
            return;
        }

        // Ensure that currentWaypoint is within a valid range.
        if (currentWaypoint < 0)
        {
            currentWaypoint = 0;
        }
        else if (currentWaypoint >= path.Count)
        {
            // Handle reaching the end of the path.
            currentWaypoint = path.Count - 1;
            // You might want to add logic to stop or handle the end of the path.
            return;
        }
        Vector3 targetPosition = path[currentWaypoint];
        float distanceToWaypoint = Vector3.Distance(this.transform.position, targetPosition);

        // Check if the object is close enough to the waypoint
        if (distanceToWaypoint < distanceThreshold)
        {
            if (currentWaypoint != path.Count)
            {
                currentWaypoint++; // Move to the next point.
            }
            else
            {
                UnityEngine.Debug.Log("reached the end of the path");
                rigidbody.velocity = Vector3.zero;
            }
            return;
        }

        // Calculate a desired velocity vector
        Vector3 desiredVelocity = (targetPosition - this.transform.position).normalized * speed;

        // Calculate a steering force to reach the desired velocity
        Vector3 steeringForce = desiredVelocity - rigidbody.velocity;

        // Apply the steering force to the rigidbody
        rigidbody.AddForce(steeringForce, ForceMode.Acceleration);
    }

    public void DrawShortestPath(List<Vector3> positions)
    {
        if (positions.Count < 2)
        {
            UnityEngine.Debug.LogWarning("Cannot draw lines with less than 2 points.");
            return;
        }

        for (int i = 0; i < positions.Count - 1; i++)
        {
            UnityEngine.Debug.DrawLine(positions[i], positions[i + 1], Color.magenta);
        }
    }
    public void SetTargetOctree(int _index)
    {
        if (OctreeManager.ActiveOctrees.Count == 0)
        {
            Debug.Log("Rb PathFinder did not find an octree at index: " + _index);
            return;
        }

        if (OctreeManager.ActiveOctrees.ContainsKey(_index))
        {
            octree = OctreeManager.ActiveOctrees[_index];
            navMeshGraph = octree.octreeGraph;
            Debug.Log("set octree successfuly of size " + navMeshGraph.GetNodesCount());
        }
        
    }
}


