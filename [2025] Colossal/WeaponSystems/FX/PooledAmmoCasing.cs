using System.Collections;
using UnityEngine;

public class PooledAmmoCasing : MonoBehaviour, IPoolable
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Renderer rend; // optional for quick enable/disable
    
    private BaseObjectPool<PooledAmmoCasing> _pool;
    private float _despawnAt;
    private float _sleepDespawnDelay = 0.75f;
    private float _sleepSince = -1f;

    public string PoolKey => this.gameObject.name;

    public void SetOwningPool(BaseObjectPool<PooledAmmoCasing> pool) => _pool = pool;

    public void OnWarm() { }

    public void OnSpawn()
    {
        _sleepSince = -1f;
        if (rb) { rb.WakeUp(); rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (rend) rend.enabled = true;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(CheckSleepAndLifetime());
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        if (rend) rend.enabled = false;
        gameObject.SetActive(false);
    }

    public void ApplySpawn(
        Vector3 position, Quaternion rotation,
        Vector3 linearVelocity, Vector3 angularVelocityRad, float lifetime)
    {
        transform.SetPositionAndRotation(position, rotation);
        if (rb)
        {
            rb.velocity = linearVelocity;
            rb.angularVelocity = angularVelocityRad; // world-space rad/s
        }
        _despawnAt = Time.time + lifetime;
    }

    private IEnumerator CheckSleepAndLifetime()
    {
        while (true)
        {
            if (Time.time >= _despawnAt) break;

            if (rb != null)
            {
                if (rb.IsSleeping())
                {
                    if (_sleepSince < 0f) _sleepSince = Time.time;
                    if (Time.time - _sleepSince >= _sleepDespawnDelay) break;
                }
                else _sleepSince = -1f;
            }
            yield return null;
        }
        if (_pool != null) _pool.Despawn(this);
        else gameObject.SetActive(false);
    }

    public void RegisterToPool()
    {
        throw new System.NotImplementedException();
    }
}
