using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalGravity : GravitySource
{
    // Singleton instance
    public static GlobalGravity instance;

    // Gravity value
    [SerializeField] private Vector3 globalGravity = new Vector3(0, -9.81f, 0);

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // Ensure there's only one instance of GlobalGravity
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        base.requireGravityUpdates = false;
    }

    protected override Vector3 CalculateGravity(Transform targetObject)
    {
        return globalGravity;
    }
}

