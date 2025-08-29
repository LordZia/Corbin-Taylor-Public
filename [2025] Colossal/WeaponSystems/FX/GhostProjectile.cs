using UnityEngine;
using Photon.Pun;

public class GhostProjectile : MonoBehaviour, IPoolable
{
    [Header("Visuals")]
    [SerializeField] private TrailRenderer tracerTrail;
    [SerializeField] private GameObject particleTrailPrefab;

    [Header("Tuning")]
    [SerializeField] private float maxLifetime = 0.35f;           // safety guard if lifetime missing

    // --- Visual (ghost) kinematics ---
    private Vector3 _ghostVel;        // starts as visualVel (muzzle.forward * speed)
    private float _ghostSpeed;
    private float _remainingBend;   // meters left to finish curving
    private float _lockDistance;    // attach radius
    private bool _attached;

    // --- Target (logic path we steer toward) ---
    private Vector3 _logicOrigin;     // real projectile origin (camera)
    private Vector3 _logicVel;        // real initial velocity (camera.forward * speed)
    private float _gravity;
    private double _startTime;
    private float _lifetime;

    // id / housekeeping
    private int _id;
    private GameObject _trailInstance;

    [SerializeField, Tooltip("Must match the pool entry key in NetworkedPrefabPool")]
    private string poolKey = "GhostProjectile";
    public string PoolKey => poolKey;

    // NEW unified setup with both visual and logic params
    public void Setup(
        int id,

        // visual (ghost starts here)
        Vector3 visualOrigin,
        Vector3 visualVel,

        // logic (target we steer to)
        Vector3 logicOrigin,
        Vector3 logicVel,

        float gravity,
        double startTime,
        float lifetime,
        float bendOverMeters,
        float lockDistance
    )
    {
        _id = id;

        // visual init
        transform.position = visualOrigin;
        _ghostVel = visualVel;
        _ghostSpeed = visualVel.magnitude;
        _remainingBend = Mathf.Max(0.01f, bendOverMeters);
        _lockDistance = Mathf.Max(0.01f, lockDistance);
        _attached = false;

        // target/logic init
        _logicOrigin = logicOrigin;
        _logicVel = logicVel;
        _gravity = gravity;
        _startTime = startTime;
        _lifetime = lifetime;

        // visuals
        if (tracerTrail) tracerTrail.Clear();
        if (particleTrailPrefab)
        {
            _trailInstance = Instantiate(particleTrailPrefab, transform.position, Quaternion.identity, transform);
            var ps = _trailInstance.GetComponent<ParticleSystem>(); if (ps) ps.Play();
        }
    }

    // Tune these:
    [SerializeField] public float leadFactor = 0.8f;            // 0.6..1.0 typically
    [SerializeField] public float aggression = 3.0f;            // higher = more aggressive curve (2..6)
    [SerializeField] public float maxDegPerMeter = 120f;        // clamp turn rate per meter (prevents wobble)

    void Update()
    {

        float tNow = (float)(PhotonNetwork.Time - _startTime);

        // lifetime guard
        if (tNow >= _lifetime || tNow >= maxLifetime)
        {
            GhostProjectilePool.Instance.DestroyGhostRPC(_id);
            return;
        }

        // Real projectile kinematics (target)
        Vector3 targetPos = SimPos(_logicOrigin, _logicVel, _gravity, tNow);
        Vector3 targetVel = _logicVel + Vector3.down * _gravity * tNow;

        if (!_attached)
        {
            // === Aggression & lead tuning ===
            // Lead factor: aim a bit ahead of the target based on time-to-intercept.
            // This avoids chasing & overshoot when ghost speed >> target speed.
            float distToTarget = Mathf.Max(0.0001f, Vector3.Distance(transform.position, targetPos));
            float timeToIntercept = distToTarget / Mathf.Max(0.0001f, _ghostSpeed);


            Vector3 leadPos = targetPos + targetVel * (timeToIntercept * leadFactor);

            // Distance-based steering weight (exponential — very aggressive at start)
            float frameMeters = _ghostSpeed * Time.deltaTime;
            float kExp = 1f - Mathf.Exp(-aggression * (frameMeters / Mathf.Max(0.001f, _remainingBend)));

            // Clamp angular turn per meter to avoid oscillation
            Vector3 curDir = _ghostVel.normalized;
            Vector3 desiredDir = (leadPos - transform.position).normalized;
            float maxRad = Mathf.Deg2Rad * (maxDegPerMeter * (frameMeters / Mathf.Max(0.001f, _remainingBend)));
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(curDir, desiredDir), -1f, 1f));
            float frac = (angle <= maxRad || maxRad <= 0f) ? 1f : (maxRad / angle);

            float k = Mathf.Clamp01(kExp * frac);

            Vector3 newDir = Vector3.Slerp(curDir, desiredDir, k);
            _ghostVel = newDir * _ghostSpeed;

            // Consume bend distance faster (more aggressive)
            _remainingBend = Mathf.Max(0f, _remainingBend - frameMeters * (1f + aggression * 0.5f));

            // Move & orient
            Vector3 prevPos = transform.position;
            transform.position += _ghostVel * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(newDir, Vector3.up);

            // === Early attach guards ===
            // 1) If we're within lock radius, attach
            if ((targetPos - transform.position).sqrMagnitude <= _lockDistance * _lockDistance)
            {
                _attached = true;
            }
            else
            {
                // 2) If we just passed the target (distance increasing and velocity points away), attach
                float dPrev = (targetPos - prevPos).sqrMagnitude;
                float dNow = (targetPos - transform.position).sqrMagnitude;
                bool passed = dNow > dPrev && Vector3.Dot(_ghostVel, (targetPos - transform.position)) < 0f;
                if (passed) _attached = true;
            }
        }
        else
        {
            // Attached: render exactly on the real path
            transform.position = targetPos;

            Vector3 fwd = targetVel.sqrMagnitude > 0.0001f ? targetVel.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }


    private static Vector3 SimPos(Vector3 origin, Vector3 vel, float g, float t)
    {
        return origin + vel * t + 0.5f * Vector3.down * g * t * t;
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        if (_trailInstance)
        {
            _trailInstance.transform.parent = null;
            var ps = _trailInstance.GetComponent<ParticleSystem>(); if (ps) ps.Stop();
            Object.Destroy(_trailInstance, 2f);
            _trailInstance = null;
        }
        if (tracerTrail) tracerTrail.Clear();
    }

    public void OnWarm() { }
    public void OnSpawn() { }
    public void RegisterToPool() { }
}
