using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseObjectPool<T> where T : Component, IPoolable
{
    readonly Queue<T> _pool = new();
    readonly T _prefab;
    readonly Transform _parent;

    public BaseObjectPool(T prefab, int initialSize = 10, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        // Pre-warm the initial batch
        for (int i = 0; i < initialSize; i++)
        {
            var inst = CreateNewInstance();
            inst.gameObject.SetActive(false);
            _pool.Enqueue(inst);
        }
    }

    private T CreateNewInstance()
    {
        // parent it AND copy prefab’s localPosition/localRotation exactly
        var inst = Object.Instantiate(_prefab, _parent, false);
        inst.OnWarm();
        return inst;
    }

    public T Spawn(Vector3 pos, Quaternion rot)
    {
        T inst;
        if (_pool.Count > 0)
        {
            inst = _pool.Dequeue();
        }
        else
        {
            // pool exhausted: create & warm a brand-new one
            inst = CreateNewInstance();
        }

        inst.transform.SetPositionAndRotation(pos, rot);
        inst.gameObject.SetActive(true);
        inst.OnSpawn();  
        return inst;
    }

    public void Despawn(T inst)
    {
        inst.OnDespawn();   // every-time cleanup
        inst.gameObject.SetActive(false);
        _pool.Enqueue(inst);
    }
}

public interface IPoolable
{
    /// <summary>
    /// A unique identifier for this prefab, matching your pool’s entry key.
    /// </summary>
    string PoolKey { get; }

    /// <summary>Call this on start to register to the desired pool </summary>
    void RegisterToPool();

    /// <summary>Called exactly once, right after this instance is first instantiated.</summary>
    void OnWarm();

    /// <summary>Called each time this instance is spawned from the pool.</summary>
    void OnSpawn();

    /// <summary>Called each time this instance is returned to the pool.</summary>
    void OnDespawn();
}