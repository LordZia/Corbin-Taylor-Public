using UnityEngine;
public class SoundPool : MonoBehaviour
{
    [SerializeField] private SoundEmitter prefab;
    [SerializeField] private int poolSize = 32;

    private BaseObjectPool<SoundEmitter> _pool;

    private void Awake()
    {
        // Pre-warm the pool under this.transform
        _pool = new BaseObjectPool<SoundEmitter>(prefab, poolSize, transform);
    }

    /// <summary>
    /// Spawns a SoundEmitter at worldPosition.  
    /// </summary>
    public SoundEmitter Spawn(Vector3 worldPosition)
    {
        var emitter = _pool.Spawn(worldPosition, Quaternion.identity);
        emitter.Initialize(this);
        return emitter;
    }


    /// <summary>
    /// Despawns the emitter, resets it under the pool root, and notifies SoundManager.
    /// </summary>
    public void Despawn(SoundEmitter emitter)
    {
        _pool.Despawn(emitter);
    }
}

