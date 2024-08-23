using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.SocialPlatforms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Vector3 = UnityEngine.Vector3;

public class CreateOctree : MonoBehaviour
{
    //Debug Vars
    [SerializeField] private Color debug_drawNodesColor = Color.white;
    [SerializeField] private Color debug_drawConnectionsColor = Color.white;

    [SerializeField][Range(0.1f, 1.0f)] private float debug_drawNodesScale = 0.5f;

    [SerializeField] private bool debug_drawTreeNodes = false;
    [SerializeField] private bool debug_drawTreeConnections = false;
    [SerializeField] private bool debug_drawOccupiedPaths = false;


    [SerializeField] private int targetIndex = 0;
    [SerializeField] private int maxNeighbors = 4;
    [SerializeField] public int nodeMinSize = 5;

   
    private GameObject octreeRegion;
    private Dictionary<int, Octree> activeOctrees = new Dictionary<int, Octree>();

    private OctreePathfindingManager pathfindingManager;

    public List<OctreeNode> CurrentNodePath = new List<OctreeNode>();
    public Dictionary<int, Octree> ActiveOctrees { get { return activeOctrees; } }
    public int TargetIndex { get { return targetIndex; } set { targetIndex = value; } }


    // Start is called before the first frame update
    void Start()
    {
        //GenerateOctree();
        if (octreeRegion == null)
        {
            octreeRegion = this.gameObject;
        }

        pathfindingManager = FindObjectOfType<OctreePathfindingManager>();

    }

    void OnDrawGizmos()
    {
        if (activeOctrees.Count == 0) return;

        if (debug_drawTreeNodes) activeOctrees[targetIndex].rootNode.Draw(debug_drawNodesColor, debug_drawNodesScale); // draw nodes

        if (activeOctrees.ContainsKey(targetIndex)) DrawOccupiedPaths();

        if (activeOctrees.ContainsKey(targetIndex)) DrawLines(targetIndex);
    }

    public void GenerateOctree()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); // Start the stopwatch

        Octree octree = new Octree(octreeRegion.transform, nodeMinSize, maxNeighbors);
        activeOctrees.Add(activeOctrees.Count, octree);

        
        List<object> nodeTable = new List<object>();
        List<object> connectionsTable = new List<object>();

        foreach (KeyValuePair<Vector3, List<Vector3>> kvp in octree.octreeGraph.adjacencyList)
        {
            // Unpack octreeNodes into saveable data types

            OctreeNode octreeNode = octree.GetNode(kvp.Key);

            DataVector3 pos = new DataVector3(kvp.Key);
            DataGraphNode node = new DataGraphNode(octreeNode.nodeID, pos);

            nodeTable.Add(node);


            // fill data into connection table
            List<int> children = new List<int>();
            foreach (var item in octreeNode.ChildNodes)
            {
                if (item != null)
                {
                    children.Add(item.nodeID);
                }    
            }

            List<int> neighbors = new List<int>();
            List<Vector3> neighborPositions = new List<Vector3>();
            if (octree.octreeGraph.adjacencyList.TryGetValue(kvp.Key, out neighborPositions))
            {
                foreach (Vector3 position in neighborPositions)
                {
                    OctreeNode neighborNode = octree.GetNode(position);
                    if (neighborNode != null)
                    {
                        neighbors.Add(neighborNode.nodeID);
                    }
                }
            }

            DataGraphNodeConnections connections = new DataGraphNodeConnections(octreeNode.nodeID, neighbors, children);
            connectionsTable.Add(connections);

        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/OctreeData.dat");
        formatter.Serialize(file, nodeTable);
        file.Close();

        string persistentDataPath = Application.persistentDataPath + "/OctreeData.dat";
        

        stopwatch.Stop(); // Stop the stopwatch

        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds; // Get the elapsed time in milliseconds

        UnityEngine.Debug.Log($"Octree Generation took {elapsedMilliseconds / 1000} seconds to execute");

    }

    /*
        IEnumerator GenerateOctreeCoroutine()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start(); // Start the stopwatch

        // Clear the prior generated octree if it exists
        if (octree != null)
        {
            DestroyOctree();
        }

        octree = new Octree(octreeRegion, nodeMinSize, maxNeighbors);
        nodeIndex = octree.NodeIndex;
        octreeGenerated = true;

        stopwatch.Stop(); // Stop the stopwatch

        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds; // Get the elapsed time in milliseconds

        UnityEngine.Debug.Log($"Octree Generation took {elapsedMilliseconds / 1000} seconds to execute");

        yield return null; // Yield to ensure this frame is updated

        // Here you can perform any other post-generation tasks or continue with your logic.
    }

    private void DestroyOctree()
    {
        // Implement your octree destruction logic here
    }
    */

    public void LoadOctreeFromMemory()
    {

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream file2 = File.Open(Application.persistentDataPath + "/playerData.dat", FileMode.Open);
        PlayerData loadedPlayer = (PlayerData)formatter.Deserialize(file2);
        file2.Close();
    }

    public void DestroyOctree(int _targetIndex)
    {
        if (activeOctrees.ContainsKey(_targetIndex))
        {
            activeOctrees[_targetIndex].NodeIndex.Clear();
            activeOctrees[_targetIndex].octreeGraph.Clear();
            activeOctrees.Remove(_targetIndex);
        }
    }

    // Debug Gizmos Methods
    public void DrawLines(int _targetIndex)
    {
        if (!debug_drawTreeConnections) return;

        Gizmos.color = debug_drawConnectionsColor;

        foreach (var nodePosition in activeOctrees[_targetIndex].octreeGraph.adjacencyList.Keys)
        {   
            foreach (var neighborPosition in activeOctrees[_targetIndex].octreeGraph.GetNeighbors(nodePosition))
            {
                Gizmos.DrawLine(nodePosition, neighborPosition);
            }
        }
    }
    private void DrawOccupiedPaths()
    {
        if (!debug_drawOccupiedPaths) return;


        float debug_drawScale = 1.0f;

        foreach (var pathEntry in pathfindingManager.OccupiedPaths)
        {
            foreach (Vector3 nodePosition in pathEntry.Value)
            {
                OctreeNode node = ActiveOctrees[targetIndex].FindClosestOctreeNode(nodePosition);
                if (node != null)
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
                    Gizmos.DrawWireCube(node.NodeBounds.center, node.NodeBounds.size * debug_drawScale);
                }

            }
        }
    }
}
