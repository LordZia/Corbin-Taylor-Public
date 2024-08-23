using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPartitionManager : MonoBehaviour
{
    [SerializeField] private int chunkSize = 10; // Size of each chunk
    public int ChunkSize => chunkSize;

    private Dictionary<Vector3Int, List<GameObject>> chunkedItems = new Dictionary<Vector3Int, List<GameObject>>();

    private Dictionary<GameObject, Vector3Int> itemChunk = new Dictionary<GameObject, Vector3Int>();

    private static ChunkPartitionManager instance;
    public static ChunkPartitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ChunkPartitionManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ChunkPartitionManager");
                    instance = obj.AddComponent<ChunkPartitionManager>();
                }
            }
            return instance;
        }
    }

    private Vector3Int[] adjacentChunkOffsets = new Vector3Int[]
        {
            // list of offsets directly touching a chunks faces.
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)

            /* list of all offsets including the connecting corners.
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(-1, 0, -1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, 1, -1),
            new Vector3Int(0, -1, 1),
            new Vector3Int(0, -1, -1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(1, 1, -1),
            new Vector3Int(1, -1, 1),
            new Vector3Int(1, -1, -1),
            new Vector3Int(-1, 1, 1),
            new Vector3Int(-1, 1, -1),
            new Vector3Int(-1, -1, 1),
            new Vector3Int(-1, -1, -1)
            */
        };

    private void Update()
    {
        return;
        Vector3 offsetCorrection = new Vector3(0.5f, 0.5f, 0.5f) * chunkSize;
        foreach (var item in itemChunk)
        {
            Debug.Log("drawing a chunk");
            Utility.DrawCube((item.Value * chunkSize) + offsetCorrection, chunkSize * Vector3.one, Quaternion.identity, Color.blue, 0);

            List<Vector3Int> adjacentChunks = new List<Vector3Int>();
            adjacentChunks = GetAdjacentChunks(item.Value);


            foreach (var adjacentChunk in adjacentChunks)
            {
                Utility.DrawCube((adjacentChunk * chunkSize) + offsetCorrection, chunkSize * Vector3.one * 0.3f, Quaternion.identity, Color.green, 0);
            }
        }
    }

    // Method to add an item to the appropriate chunk
    public void AddItem(GameObject item)
    {
        Vector3Int chunkPosition = GetChunkPosition(item.transform.position);

        if (!chunkedItems.ContainsKey(chunkPosition))
        {
            chunkedItems[chunkPosition] = new List<GameObject>();
        }
        if (!itemChunk.ContainsKey(item))
        {
            itemChunk[item] = chunkPosition;
        }

        chunkedItems[chunkPosition].Add(item);
    }

    // Method to remove an item from the appropriate chunk
    public void RemoveItem(GameObject item)
    {
        Vector3Int chunkPosition = GetChunkPosition(item.transform.position);

        if (chunkedItems.ContainsKey(chunkPosition))
        {
            chunkedItems[chunkPosition].Remove(item);
        }
        if (itemChunk.ContainsKey(item))
        {
            itemChunk.Remove(item);
        }
    }

    // Method to get the chunk position for a given world position
    public Vector3Int GetChunkPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int y = Mathf.FloorToInt(worldPosition.y / chunkSize);
        int z = Mathf.FloorToInt(worldPosition.z / chunkSize);

        return new Vector3Int(x, y, z);
    }

    // Method to get all items in a specific chunk
    public List<GameObject> GetItemsInChunk(Vector3Int chunkPosition)
    {
        if (chunkedItems.ContainsKey(chunkPosition))
        {
            return chunkedItems[chunkPosition];
        }
        else
        {
            return new List<GameObject>();
        }
    }

    public List<Vector3Int> GetAdjacentChunks(Vector3Int targetChunk)
    {
        List<Vector3Int> adjacentChunks = new List<Vector3Int>();

        // Calculate the positions of adjacent chunks using the offsets
        foreach (var offset in adjacentChunkOffsets)
        {
            Vector3Int adjacentChunkPos = targetChunk + offset;
            adjacentChunks.Add(adjacentChunkPos);
        }

        return adjacentChunks;
    }

    public Vector3Int GetChunk(GameObject referenceObject)
    {
        if (itemChunk.ContainsKey(referenceObject))
        {
            return itemChunk[referenceObject];
        }
        return new Vector3Int(0,0,0);
    }

    public bool CheckProximity(Vector3 referencePos, GameObject targetObject)
    {
        Vector3Int referenceChunk = GetChunkPosition(referencePos);
        List<Vector3Int> adjacentChunks = GetAdjacentChunks(referenceChunk);

        if (itemChunk.ContainsKey(targetObject))
        {
            if (itemChunk[targetObject] == referenceChunk)
            {
                return true;
            }
        }

        foreach (var adjacentChunk in adjacentChunks)
        {
            if (itemChunk[targetObject] == adjacentChunk)
            {
                return true;
            }
        }

        return false;
    }

    // Method to clear all items from the manager
    public void Clear()
    {
        chunkedItems.Clear();
    }
}
