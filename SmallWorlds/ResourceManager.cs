using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private bool debug_View = false;
    // Singleton instance
    private static ResourceManager instance;
    public static ResourceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(ResourceManager)) as ResourceManager;
                if (instance == null)
                {
                    Debug.LogError("There needs to be one active ResourceManager script on a GameObject in your scene.");
                }
            }
            return instance;
        }
    }
    

    // List of broken resources
    private List<IRespawnable> currentlyBrokenResources = new List<IRespawnable>();

    // Respawn settings
    public float respawnInterval = 5f; // Respawn every X seconds
    [Range(0f, 1f)]
    [Tooltip("The percentage chance of respawning after every time the respawn interval is reached")]
    public float respawnChance = 0.5f; // Chance of respawn (0-1)

    private bool coroutineRunning = false;

    private Dictionary<GameObject, List<Vector3Int>> chunkObstructions = new Dictionary<GameObject, List<Vector3Int>>();
    private List<Vector3Int> obstructedChunks = new List<Vector3Int>();

    [SerializeField] private bool DEBUG_MODE = false;
    private int chunkSize;
    private Vector3 offsetCorrection = new Vector3(0.5f, 0.5f, 0.5f);

    private void Awake()
    {
        chunkSize = ChunkPartitionManager.Instance.ChunkSize;
        offsetCorrection *= chunkSize;
        StartCoroutine(RespawnResources());
    }

    private void FixedUpdate()
    {
        if (!debug_View)
            return;

        foreach (Vector3Int obstructedChunk in obstructedChunks)
        {
            Utility.DrawCube((obstructedChunk * chunkSize) + offsetCorrection, chunkSize * Vector3.one, Quaternion.identity, Color.blue, 0);
        }
    }
    private IEnumerator RespawnResources()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnInterval);

            // keep track of objects that were respawned during the foreach loop to remove them from the respawnable list after the loop finishes
            List<IRespawnable> respawnedResources = new List<IRespawnable>(); 

            foreach (IRespawnable respawnable in currentlyBrokenResources)
            {
                if (Random.value >= respawnChance)
                {
                    // outside of respawn chance failed spawn move to next object;
                    continue;
                }

                GameObject obj = respawnable.GetGameObject();
                IChunkPartitianed chunkPartitianedObject = obj.GetComponent<IChunkPartitianed>();

                if (chunkPartitianedObject == null)
                {
                    Debug.LogError($"Respawnable objects are required to use the IChunkPartitianed interface : {obj.name} does not have a class that uses the right interface. skipping this object - recource manager", this);
                    continue;
                }

                Vector3Int respawnableObjChunk = chunkPartitianedObject.GetChunk();

                Utility.DrawCube((respawnableObjChunk * chunkSize) + offsetCorrection, chunkSize * Vector3.one * 0.7f, quaternion.identity, Color.red, 5);
                
                bool isObstructed = false;
                foreach (Vector3Int obstructedChunk in obstructedChunks)
                {
                    if (respawnableObjChunk == obstructedChunk)
                    {
                        Utility.DrawCube((obstructedChunk * chunkSize) + offsetCorrection, chunkSize * Vector3.one * 0.5f, Quaternion.identity, Color.blue, 5);

                        // this item's chunk is currently obstructed skip to next respawnable item.
                        isObstructed = true;
                        continue;
                    }
                }

                if (isObstructed)
                {
                    continue;
                }

                // spawn object
                respawnable.Respawn();
                respawnedResources.Add(respawnable);
            }

            // update the list and remove any resources that were successfully spawned
            foreach (IRespawnable resource in respawnedResources)
            {
                currentlyBrokenResources.Remove(resource);
            }
        }
    }

    public void AddChunkObstruction(GameObject obstructingObject)
    {
        if (chunkObstructions.ContainsKey(obstructingObject))
        {
            Debug.Log($"recieved a request to add {obstructingObject.name} to the dictionary of chunk obstructions but it already exists in the dictionary");
            return;
        }

        // convert the object's world position to a chunk position and grab it's neighboring chunks
        Vector3Int chunkLocation = ChunkPartitionManager.Instance.GetChunkPosition(obstructingObject.transform.position);
        List<Vector3Int> newObstructedChunks = ChunkPartitionManager.Instance.GetAdjacentChunks(chunkLocation);

        newObstructedChunks.Add(chunkLocation);

        obstructedChunks.AddRange(newObstructedChunks);
        chunkObstructions.Add(obstructingObject, newObstructedChunks);
    }

    public void UpdateChunkObstruction(GameObject obstructingObject)
    {
        RemoveChunkObstruction(obstructingObject);
        AddChunkObstruction(obstructingObject);
    }

    public void RemoveChunkObstruction(GameObject obstructingObject)
    {
        if (chunkObstructions.ContainsKey(obstructingObject))
        {
            foreach (var obstructedChunk in chunkObstructions[obstructingObject])
            {
                obstructedChunks.Remove(obstructedChunk);
            }

            chunkObstructions.Remove(obstructingObject);
        }
    }

    public void AddBrokenRespawnableToSpawnList(IRespawnable resource)
    {
        Debug.Log("Added resource to respawn list");
        if (!currentlyBrokenResources.Contains(resource))
        {
            currentlyBrokenResources.Add(resource);
            CheckResourceCountAndUpdateCoroutine();
        }
    }

    public void RemoveBrokenRespawnableFromSpawnList(IRespawnable resource)
    {
        if (currentlyBrokenResources.Contains(resource))
        {
            currentlyBrokenResources.Remove(resource);
            CheckResourceCountAndUpdateCoroutine();
        }
    }

    // Method to check resource count and start/stop the coroutine accordingly
    private void CheckResourceCountAndUpdateCoroutine()
    {
        if (currentlyBrokenResources.Count == 0 && coroutineRunning)
        {
            StopCoroutine(RespawnResources());
            coroutineRunning = false;
        }
        else if (currentlyBrokenResources.Count >= 1 && !coroutineRunning)
        {
            StartCoroutine(RespawnResources());
            coroutineRunning = true;
        }
    }
}
