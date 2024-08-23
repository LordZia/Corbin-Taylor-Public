using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ObjectPool))]
public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private List<SpawnableObject> objectsToSpawn = new List<SpawnableObject>();
    [SerializeField] private float spawnDelay = 5.0f;

    protected ObjectPool objectPool;
    private Coroutine spawnCoroutine;

    [System.Serializable]
    private struct SpawnableObject
    {
        public Vector3 randomRotMin;
        public Vector3 randomRotMax;
        public GameObject obj;
        public float weight;
        public int initialPoolSize;
        public SpawnableObject(GameObject gameObject) : this(gameObject, 10f, Vector3.zero, Vector3.zero, 5)
        {
        }

        public SpawnableObject(GameObject gameObject, float weight, Vector3 rotMin, Vector3 rotMax, int initialPoolSize)
        {
            this.obj = gameObject;
            this.weight = weight;
            this.randomRotMin = rotMin;
            this.randomRotMax = rotMax;
            this.initialPoolSize = initialPoolSize;
        }
    }

    private void Awake()
    {
        OnAwake();
    }
    protected void OnAwake()
    {
        objectPool = GetComponent<ObjectPool>();
        // Initialize object pools for each prefab

        GameObject[] gameObjectArray = new GameObject[objectsToSpawn.Count];
        int[] initialPoolSizes = new int[objectsToSpawn.Count];
        for (int i = 0; i < objectsToSpawn.Count; i++)
        {
            gameObjectArray[i] = objectsToSpawn[i].obj;
            initialPoolSizes[i] = objectsToSpawn[i].initialPoolSize;
        }

        objectPool.InitializePool(gameObjectArray, initialPoolSizes); // Adjust the initial size as needed

        // Start spawning objects
        spawnCoroutine = StartCoroutine(SpawnObjects());
    }

    private void OnDestroy()
    {
        // Stop the spawning coroutine if the object is destroyed
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }

    private IEnumerator SpawnObjects()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);

            // Randomly select an object to spawn
            Quaternion spawnRotation;
            GameObject objectToSpawn = GetRandomObjectToSpawn(out spawnRotation);

            if (objectToSpawn != null)
            {
                GameObject spawnedObject = objectPool.ActivateObject(objectToSpawn, transform.position, spawnRotation);
                if (spawnedObject != objectToSpawn) // wierd, but works - zia
                {
                    OnObjectSpawn(spawnedObject);
                }
            }
        }
    }

    protected virtual void OnObjectSpawn(GameObject _spawnedObject)
    { }

    private GameObject GetRandomObjectToSpawn(out Quaternion spawnRotation)
    {
        // Calculate total weight of all objects for random selection
        float totalWeight = 0;
        foreach (SpawnableObject spawnableObject in objectsToSpawn)
        {
            totalWeight += spawnableObject.weight;
        }

        // Generate a random value between 0 and totalWeight
        float randomValue = Random.Range(0f, totalWeight);

        // Iterate through objects and find the one to spawn based on randomValue
        float cumulativeWeight = 0;
        foreach (SpawnableObject spawnableObject in objectsToSpawn)
        {
            float weight = spawnableObject.weight;
            cumulativeWeight += weight;
            if (randomValue <= cumulativeWeight)
            {
                spawnRotation = Utility.GetRandomRotation(spawnableObject.randomRotMin, spawnableObject.randomRotMax);
              
                return spawnableObject.obj;
            }
        }

        spawnRotation = Quaternion.identity;
        return null;
    }
}
