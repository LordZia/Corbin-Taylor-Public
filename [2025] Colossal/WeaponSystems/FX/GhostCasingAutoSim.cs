using System.Collections;
using UnityEngine;

public class GhostCasingAutoSim : MonoBehaviour, IPoolable
{
    [Header("Mode")]
    [Tooltip("If a Rigidbody is present, use it with collisions disabled. Otherwise fall back to script integration.")]
    [SerializeField] private bool useRigidbodyIfAvailable = true;

    [Header("Optional refs")]
    [SerializeField] private Rigidbody rb;              // optional; auto-fetched
    [SerializeField] private Transform visualRoot;      // optional; toggled on spawn/despawn

    [Header("RB defaults (ghosts)")]
    [SerializeField] private RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
    [SerializeField] private CollisionDetectionMode collisionMode = CollisionDetectionMode.Discrete;

    // runtime state
    private bool _useRB;
    private Vector3 _vel;        // m/s
    private Vector3 _angVelRad;  // rad/s
    private float _gravity = 9.81f;
    private float _despawnAt = -1f;

    private Coroutine _simCo;

    public string PoolKey => this.gameObject.name;

    // --- External API ---

    /// Call after spawn to set initial motion and (optionally) per-instance gravity/lifetime.
    public void OnSpawn()
    {
        if (!rb && useRigidbodyIfAvailable) rb = GetComponent<Rigidbody>();
        _useRB = useRigidbodyIfAvailable && rb != null;

        if (_useRB)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = false;
            rb.collisionDetectionMode = collisionMode;
            rb.interpolation = interpolation;
            rb.WakeUp(); // <- ensure awake on reuse
        }

        if (visualRoot) visualRoot.gameObject.SetActive(true);
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        if (_simCo != null) { StopCoroutine(_simCo); _simCo = null; }

        _despawnAt = -1f;
        _vel = Vector3.zero;
        _angVelRad = Vector3.zero;

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = false;
            rb.WakeUp();
        }

        if (visualRoot) visualRoot.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Arm(Vector3 linearVelocity, Vector3 angularVelocityRad, float lifetimeSeconds, float gravity)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        _vel = linearVelocity;
        _angVelRad = angularVelocityRad;
        _gravity = gravity > 0f ? gravity : 9.81f;
        _despawnAt = (lifetimeSeconds > 0f) ? Time.time + lifetimeSeconds : -1f;

        if (_useRB && rb != null)
        {
            rb.WakeUp(); // <- belt & suspenders
            rb.velocity = _vel;
            rb.angularVelocity = _angVelRad; // rad/s
                                             // (detectCollisions=false etc. already set in OnSpawn)
        }
        else
        {
            if (_simCo != null) StopCoroutine(_simCo);
            _simCo = StartCoroutine(Simulate());
        }
    }


    // --- Internal helpers ---

    private void Setup()
    {
        if (!rb && useRigidbodyIfAvailable) rb = GetComponent<Rigidbody>();
        _useRB = useRigidbodyIfAvailable && rb != null;

        if (_useRB)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = false; // ghosts never collide
            rb.collisionDetectionMode = collisionMode;
            rb.interpolation = interpolation;
        }
    }

    private IEnumerator Simulate()
    {
        var t = transform;
        while (true)
        {
            float dt = Time.deltaTime;

            // optional lifetime stop (manager will also despawn via its own timer)
            if (_despawnAt > 0f && Time.time >= _despawnAt) yield break;

            _vel += Vector3.down * _gravity * dt;
            t.position += _vel * dt;

            if (_angVelRad.sqrMagnitude > 0f)
            {
                var dq = Quaternion.Euler(_angVelRad * Mathf.Rad2Deg * dt);
                t.rotation = dq * t.rotation;
            }

            yield return null;
        }
    }

    public void RegisterToPool()
    {
        throw new System.NotImplementedException();
    }

    public void OnWarm()
    {
        Setup();
    }
}
