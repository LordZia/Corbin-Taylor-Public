using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;


public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int initialSize = 20;

    // Internal generic pool
    private BaseObjectPool<Projectile> _pool;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create initial pool; parent all instances under this transform
        _pool = new BaseObjectPool<Projectile>(
            projectilePrefab,
            initialSize,
            transform
        );
    }

    /// <summary>
    /// Spawn at the firePoint’s direction,
    /// merged with the prefab’s base rotation.
    /// </summary>
    public Projectile Spawn(Vector3 pos, Quaternion firePointRot)
    {
        // grab the prefab’s “base” rotation
        Quaternion baseRot = projectilePrefab.transform.localRotation;
        // combine them
        Quaternion spawnRot = firePointRot * baseRot;
        // delegate to the pool
        return _pool.Spawn(pos, spawnRot);
    }

    /// <summary>
    /// Return a projectile to the pool.
    /// </summary>
    public void Despawn(Projectile proj)
    {
        _pool.Despawn(proj);
    }
}


