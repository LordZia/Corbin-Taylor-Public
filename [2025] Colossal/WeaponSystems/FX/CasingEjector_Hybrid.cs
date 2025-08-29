using UnityEngine;
using Items;
using VFX;

public class CasingEjector_Hybrid : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerEventBus eventBus;
    [SerializeField] private Transform defaultEjectPoint;

    [Header("Spawn feel")]
    [Tooltip("Tiny randomness added per shot so spacing feels natural.")]
    [SerializeField, Range(0f, 0.2f)] private float fairnessJitter = 0.05f;

    private CasingSettings _settings;
    private Transform _ejectPoint;
    private float _shotsPerSecond = 10f;   // from WeaponDefinition.fireRate
    private float _perShotProb = 0.2f;     // computed from settings.desiredRealPercentPerSecond
    private float _realAcc;                // fairness accumulator

    private EffectDefinition _realEffect;  // from settings.realEffect
    private EffectDefinition _fakeEffect;  // from settings.ghostEffect

    private bool isEnabled = false;

    private void OnEnable()
    {
        eventBus?.Subscribe<WeaponFiredEvent>(OnWeaponFired);
        eventBus?.Subscribe<WeaponSwappedEvent>(OnWeaponSwapped);
    }
    private void OnDisable()
    {
        eventBus?.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
        eventBus?.Unsubscribe<WeaponSwappedEvent>(OnWeaponSwapped);
    }

    public void ConfigureFromWeaponDef(WeaponDefinition def, Transform ejectPointOverride = null)
    {
        if (def == null || def.casings.enabled == false || def.casings.realEffect == null)
        {
            _settings = default;
            _realEffect = _fakeEffect = null;
            _ejectPoint = null;
            _perShotProb = 0f;
            _realAcc = 0f;
            return;
        }

        _settings = def.casings;
        _realEffect = _settings.realEffect;
        _fakeEffect = _settings.ghostEffect;

        _ejectPoint = ejectPointOverride ? ejectPointOverride : defaultEjectPoint;
        _shotsPerSecond = GetShotsPerSecond(def);
        RecomputePerShotProbability();     // from settings.desiredRealPercentPerSecond
        _realAcc = 0f;

        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }

    private void RecomputePerShotProbability()
    {
        _perShotProb = Mathf.Clamp01(_settings.desiredRealPercentPerSecond);
    }

    private static float GetShotsPerSecond(WeaponDefinition def)
    {
        if (def.fireRate > 0f) return def.fireRate; // your API
        return 5f;
    }

    private void OnWeaponSwapped(WeaponSwappedEvent evt)
    {
        // ConfigureFromWeapon(evt.NewWeaponDef, evt.NewWeaponEjectPoint);
        _realAcc = 0f;
    }

    private void OnWeaponFired(WeaponFiredEvent evt)
    {
        Debug.Log("recieved fire event");
        if (!isEnabled) return;

        // Lazy configure if needed
        if (_realEffect == null && evt.Context?.Definition != null)
            ConfigureFromWeaponDef(evt.Context.Definition, null);

        if (_realEffect == null || !_settings.enabled) return;

        Transform eject = _ejectPoint ? _ejectPoint : transform;
        // Kinematics (unchanged)
        float speed = Random.Range(_settings.ejectSpeed.x, _settings.ejectSpeed.y);
        Vector3 baseVel = eject.right * speed + Random.insideUnitSphere * _settings.jitter;
        float spinDeg = Random.Range(_settings.spinDegPerSec.x, _settings.spinDegPerSec.y);
        Vector3 spinAxis = Random.onUnitSphere;
        Vector3 angVelRad = spinAxis * (spinDeg * Mathf.Deg2Rad);

        var rootRb = GetComponentInParent<Rigidbody>();
        if (rootRb) baseVel += rootRb.velocity;

        // NEW: compute the final initial rotation once
        Quaternion spawnRot = GetSpawnRotation();

        bool spawnReal = ShouldSpawnReal();

        if (spawnReal)
        {
            // pass spawnRot (offset from emitter's global rotation)
            var go = EffectPoolManager.Instance.PlayEffect(_realEffect.effectID, eject.position, spawnRot);
            if (!go) return;

            if (go.TryGetComponent<CasingAutoReturn>(out var casing))
                casing.Arm(_realEffect.effectID, baseVel, angVelRad, _settings.lifetime);
            else if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                // Ensure transform has the intended rotation (PlayEffect already set it, this is just belt & suspenders)
                go.transform.rotation = spawnRot;
                rb.velocity = baseVel;
                rb.angularVelocity = angVelRad;
            }
        }
        else if (_fakeEffect != null)
        {
            // same rotation for the ghost casing
            var go = EffectPoolManager.Instance.PlayEffect(_fakeEffect.effectID, eject.position, spawnRot);
            if (go && go.TryGetComponent<GhostCasingAutoSim>(out var ghost))
            {
                float g = 9.81f * Mathf.Max(0.01f, _settings.gravity);
                ghost.Arm(baseVel, angVelRad, _settings.lifetime * 0.5f, g);
            }
        }
    }

    // Fairness-accumulator; keeps the correct average and avoids streaks
    private bool ShouldSpawnReal()
    {
        float p = Mathf.Clamp01(_perShotProb + Random.Range(-fairnessJitter, fairnessJitter));
        _realAcc += p;
        bool spawn = (_realAcc >= 1f) || (Random.value < _realAcc);
        if (spawn) _realAcc -= 1f;
        return spawn;
    }

    //  basis is the ejector's GameObject global rotation, not the eject point
    private Quaternion GetSpawnRotation()
    {
        // spawnRotationOffsetEuler is in DEGREES (from WeaponDefinition.casings)
        return transform.rotation * Quaternion.Euler(_settings.spawnRotationOffsetEuler);
    }


#if UNITY_EDITOR
    [Header("Editor Display Settings")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float gizmoLength = 0.4f;
    [SerializeField] private bool gizmoShowAxes = true;
    [SerializeField] private float gizmoAxisSize = 0.075f;

    private void OnDrawGizmosSelected()
    {
        Transform t = _ejectPoint ? _ejectPoint : (defaultEjectPoint ? defaultEjectPoint : transform);
        if (!t) return;

        Vector3 o = t.position;
        Vector3 d = t.right.normalized;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(o, o + d * gizmoLength);

        Vector3 end = o + d * gizmoLength;
        Vector3 side = Vector3.Cross(d, Vector3.up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(d, Vector3.forward);
        side.Normalize();
        float head = gizmoLength * 0.15f;
        Gizmos.DrawLine(end, end - d * head + side * head * 0.5f);
        Gizmos.DrawLine(end, end - d * head - side * head * 0.5f);

        if (gizmoShowAxes)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
            Gizmos.DrawLine(o, o + t.right * gizmoAxisSize);
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
            Gizmos.DrawLine(o, o + t.up * gizmoAxisSize);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
            Gizmos.DrawLine(o, o + t.forward * gizmoAxisSize);
        }
    }
#endif
}
