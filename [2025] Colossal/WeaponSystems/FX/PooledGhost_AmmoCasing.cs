using System.Collections;
using UnityEngine;

public class PooledGhost_AmmoCasing : MonoBehaviour, IPoolable
{
    [SerializeField] private Transform visualRoot; // mesh holder (or leave null to use self)

    private BaseObjectPool<PooledGhost_AmmoCasing> _pool;
    private Vector3 _vel;           // m/s
    private Vector3 _angVelRad;     // rad/s
    private float _gravity = 9.81f;
    private float _despawnAt;

    public string PoolKey => this.gameObject.name;

    public void SetOwningPool(BaseObjectPool<PooledGhost_AmmoCasing> pool) => _pool = pool;

    public void OnWarm() { }

    public void OnSpawn()
    {
        (visualRoot ? visualRoot : transform).gameObject.SetActive(true);
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Sim());
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        (visualRoot ? visualRoot : transform).gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ApplySpawn(Vector3 pos, Quaternion rot, Vector3 linearVel, Vector3 angularVelRad,
                           float lifetime, float gravity = 9.81f)
    {
        transform.SetPositionAndRotation(pos, rot);
        _vel = linearVel;
        _angVelRad = angularVelRad;
        _gravity = gravity * 1.0f; // particle “gravity modifier” style if you want
        _despawnAt = Time.time + lifetime;
    }

    private IEnumerator Sim()
    {
        var t = transform;
        while (Time.time < _despawnAt)
        {
            float dt = Time.deltaTime;
            _vel += Vector3.down * _gravity * dt;
            t.position += _vel * dt;

            // integrate angular velocity (world space)
            if (_angVelRad.sqrMagnitude > 0f)
            {
                var dq = Quaternion.Euler(_angVelRad * Mathf.Rad2Deg * dt);
                t.rotation = dq * t.rotation;
            }

            // optional: cheap ground kill
            // if (t.position.y < someFloorY) break;

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
