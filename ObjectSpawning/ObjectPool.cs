using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    // Dictionary to store lists of inactive objects for each object type
    private Dictionary<GameObject, List<GameObject>> inactiveObjectMap = new Dictionary<GameObject, List<GameObject>>();

    [SerializeField] private bool dynamicallyScalePoolSizes = false;
    // Initialize the object pool
    public void InitializePool(GameObject[] objectPrefabs, int[] poolSizes)
    {
        Debug.Log("pool fill");
        for (int i = 0; i < objectPrefabs.Length; i++)
        {
            GameObject prefab = objectPrefabs[i];
            int poolSize = poolSizes[i];
            InitializeObjectPool(prefab, poolSize);
        }
    }

    // Initialize an object pool for a specific prefab
    private void InitializeObjectPool(GameObject objectPrefab, int poolSize)
    {
        List<GameObject> inactiveObjects = new List<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            AddObjectToPool(objectPrefab, inactiveObjects);
        }

        inactiveObjectMap[objectPrefab] = inactiveObjects;
    }

    private void AddObjectToPool(GameObject objectPrefab, List<GameObject> inactiveObjects)
    {
        GameObject newObj = Instantiate(objectPrefab, transform);
        newObj.transform.SetParent(this.transform);
        newObj.SetActive(false);
        inactiveObjects.Add(newObj);
    }

    // Activate an object from the pool
    public GameObject ActivateObject(GameObject objectPrefab, Vector3 position, Quaternion rotation)
    {
        if (!inactiveObjectMap.ContainsKey(objectPrefab))
        {
            Debug.LogError("Object pool for prefab " + objectPrefab.name + " not initialized.");
            return null;
        }

        List<GameObject> inactiveObjects = inactiveObjectMap[objectPrefab];

        if (inactiveObjects.Count == 0)
        {
            if (dynamicallyScalePoolSizes)
            {
                Debug.Log("adding new " + objectPrefab.name + " to pool");
                // Create and add a new object to the pool if no inactive objects are available
                AddObjectToPool(objectPrefab, inactiveObjects);
            }
        }

        if (inactiveObjects.Count == 0)
        {
            return objectPrefab;
        }
        GameObject objToActivate = inactiveObjects[inactiveObjects.Count - 1];
        inactiveObjects.RemoveAt(inactiveObjects.Count - 1);

        objToActivate.transform.position = position;
        objToActivate.transform.rotation = rotation;
        objToActivate.SetActive(true);

        return objToActivate;
    }

    // Deactivate an object and return it to the pool
    public void DeactivateObject(GameObject obj)
    {
        obj.SetActive(false);

        foreach (var pair in inactiveObjectMap)
        {
            if (pair.Value.Contains(obj))
            {
                pair.Value.Add(obj);
                return;
            }
        }

        Debug.LogWarning("Object " + obj.name + " does not belong to any object pool.");
    }

    // Deactivate an object and return it to the pool after a delay
    public void DeactivateObjectDelayed(GameObject obj, float delay)
    {
        StartCoroutine(DeactivateObjectCoroutine(obj, delay));
    }

    private IEnumerator DeactivateObjectCoroutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        DeactivateObject(obj);
    }
}
