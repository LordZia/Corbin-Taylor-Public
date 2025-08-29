using System.Collections;
using UnityEngine;
using Photon.Pun;
using Items;

// Ensure you have DebugDrawExtensions in your project
// with a method like DrawCube(center, size, orientation, color, duration)
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour, IPoolable
{
    [Header("Collision Settings")]
    [SerializeField] private float boxHalfExtents = 0.05f;
    [SerializeField] private LayerMask hitLayer;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;
    [SerializeField] private Color debugBoxHitColor = Color.red;
    [SerializeField] private Color debugBoxMissColor = Color.green;
    [SerializeField] private Color debugRayColor = Color.red;

    private static int _nextId = 1;
    private int _projectileId;

    // Configured on spawn
    private Vector3 _logicFirePosition;
    private Quaternion _logicFireRotation;
    private Vector3 _visualFirePosition;
    private Quaternion _visualFireRotation;

    private Transform _logicFirePoint;
    private Transform _visualFirePoint;

    private Vector3 _initialVel;
    private int _damage;
    private float _lifetime;
    private float _gravityStrength;
    private int _ownerViewID;
    private int _weaponId;
    private ProjectilePool _pool;
    private bool _isOwner;
    private int _fleshImpactEffectID;
    private int _structureImpactEffectID;

    private SoundDefinition _fleshImpactSfx;
    private SoundDefinition _structureImpactSfx;

    // Runtime state
    private Rigidbody _rb;
    private Coroutine _lifetimeRoutine;
    private const string damageType = "Projectile";

    /*
    public void Initialize(
        Transform logicFirePoint,
        Transform visualFirePoint,
        Vector3 initialVelocity,
        int damage,
        float lifetime,
        float gravityStrength,
        ProjectilePool pool,
        int ownerViewID,
        int fleshImpactEffectID,
        int structureImpactEffectID)
    {
        _projectileId = _nextId++;
        _logicFirePoint = logicFirePoint;
        _visualFirePoint = visualFirePoint;
        _initialVel = initialVelocity;
        _damage = damage;
        _lifetime = lifetime;
        _gravityStrength = gravityStrength;
        _pool = pool;
        _ownerViewID = ownerViewID;
        _fleshImpactEffectID = fleshImpactEffectID;
        _structureImpactEffectID = structureImpactEffectID;
        _isOwner = PhotonNetwork.LocalPlayer.ActorNumber == ownerViewID;

        LaunchProjectile();
    }
    */


    public void Initialize(WeaponContext ctx)
    {
        var def = ctx.Definition;

        // Derive logic/visual spawn transforms the same way your spawner does now
        Quaternion logicRot = ctx.LogicFirePoint.localRotation * ctx.FireRotationOffset;
        Quaternion visualRot = ctx.FirePoint.rotation * ctx.FireRotationOffset;

        // Compute initial velocity (adds forward component of owner rb velocity)
        Vector3 dir = ctx.FireRotationOffset * ctx.LogicFirePoint.forward;
        float forwardSpeed = 0f;
        if (ctx.OwnerRigidbody != null)
            forwardSpeed = Mathf.Max(0f, Vector3.Dot(ctx.OwnerRigidbody.velocity, dir));
        Vector3 initialVel = dir * (def.projectileSpeed + forwardSpeed);

        // Use the global pool instance here; adjust if you keep a different reference
        Initialize(
            logicFirePosition: ctx.LogicFirePoint.position,
            logicFireRotation: logicRot,
            visualFirePosition: ctx.FirePoint.position,
            visualFireRotation: visualRot,
            initialVelocity: initialVel,
            damage: def.damage,
            lifetime: def.projectileLifetime,
            gravityStrength: def.projectileDropAcceleration,
            pool: ProjectilePool.Instance,
            ownerViewID: ctx.PhotonView.ViewID,
            weaponId: def.weaponID,
            fleshImpactEffectID: def.fleshImpactEffect.effectID,
            structureImpactEffectID: def.structureImpactEffect.effectID,
            fleshImpactSoundDef: def.fleshImpactSoundDef,
            structureImpactSoundDef: def.structureImpactSoundDef
        );
    }

    public void Initialize(
    Vector3 logicFirePosition,
    Quaternion logicFireRotation,
    Vector3 visualFirePosition,
    Quaternion visualFireRotation,
    Vector3 initialVelocity,
    int damage,
    float lifetime,
    float gravityStrength,
    ProjectilePool pool,
    int ownerViewID,
    int weaponId,
    int fleshImpactEffectID,
    int structureImpactEffectID,
    SoundDefinition fleshImpactSoundDef,   
    SoundDefinition structureImpactSoundDef)   
    {
        // assign id
        _projectileId = _nextId++;

        // store spawn transforms as data
        _logicFirePosition = logicFirePosition;
        _logicFireRotation = logicFireRotation;
        _visualFirePosition = visualFirePosition;
        _visualFireRotation = visualFireRotation;

        // other data
        _initialVel = initialVelocity;
        _damage = damage;
        _lifetime = lifetime;
        _gravityStrength = gravityStrength;
        _pool = pool;
        _ownerViewID = ownerViewID;
        _weaponId = weaponId;

        _fleshImpactEffectID = fleshImpactEffectID;
        _structureImpactEffectID = structureImpactEffectID;

        _fleshImpactSfx = fleshImpactSoundDef;
        _structureImpactSfx = structureImpactSoundDef;

        _isOwner = PhotonNetwork.LocalPlayer.ActorNumber == ownerViewID;

        LaunchProjectile();
    }

    public void OnWarm()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.velocity = Vector3.zero;
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
        _lifetimeRoutine = null;

        _rb.velocity = Vector3.zero;
        _initialVel = Vector3.zero;
    }

    private void LaunchProjectile()
    {
        // 1) Zero + apply velocity to the REAL projectile (already in your code)
        _rb.velocity = Vector3.zero;
        _rb.AddForce(_initialVel, ForceMode.VelocityChange);

        // 2) Track current position etc...
        currentPosition = _logicFirePosition;

        // 3) Initial collision check
        if (PerformCollisionChecks(_initialVel))
            return;

        // 4) Despawn timer
        _lifetimeRoutine = StartCoroutine(DespawnAfterDelay());

        // --- NEW: build composite id so different shooters never collide ---
        int compositeId = GhostProjectilePool.MakeCompositeId(_ownerViewID, _projectileId);

        // visual (muzzle)
        Vector3 visualDir = _visualFireRotation * Vector3.forward;
        Vector3 visualVel = visualDir * _initialVel.magnitude;

        // logic (camera)
        Vector3 logicOrigin = _logicFirePosition;
        Vector3 logicVel = _initialVel;

        // network time
        double t0 = PhotonNetwork.Time;

        // dial these per-weapon; ADS can pass smaller bend
        const float bendOverMeters = 6f; // hipfire baseline for rifles
        const float lockDistance = 0.1f;

        GhostProjectilePool.Instance.SpawnGhostRPC(
            id: compositeId,
            visualOrigin: _visualFirePosition,
            visualVel: visualVel,
            logicOrigin: logicOrigin,
            logicVel: logicVel,
            gravity: _gravityStrength,
            startTime: t0,
            lifetime: _lifetime,
            bendOverMeters: bendOverMeters,
            lockDistance: lockDistance
        );
    }


    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(_lifetime);
        _pool.Despawn(this);
    }

    Vector3 currentPosition;
    private void FixedUpdate()
    {
        // Apply gravity
        if (_gravityStrength != 0f)
            _rb.AddForce(Vector3.down * _gravityStrength, ForceMode.Acceleration);

        // Align orientation with flight direction
        Vector3 vel = _rb.velocity;
        if (vel.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);

        PerformCollisionChecks(vel);
    }

    private bool PerformCollisionChecks(Vector3 vel)
    {
        // Compute current and next positions
        currentPosition = transform.position;
        Vector3 nextPosition = currentPosition + vel * Time.fixedDeltaTime;

        // Check collisions between current and predicted next
        if (TrySphereHit(currentPosition, nextPosition, out RaycastHit hit))
        {
            if (debugDraw)
                Debug.DrawLine(currentPosition, hit.point, debugRayColor, Time.fixedDeltaTime);

            ProcessHit(hit);
            return true;
        }

        return false;
    }

    [SerializeField][Tooltip("Extends the end position of each box cast, prevents the projectile from clipping through thin walls in rare cases")] private float endPosExtension;
    private Color debugRayHitColor = Color.red;
    private Color debugRayMissColor = Color.green;

    [SerializeField, Tooltip("Must match the pool entry key in NetworkedPrefabPool")]
    private string poolKey = "Projectile";
    public string PoolKey => poolKey;

    /// <summary>
    /// Alternative collision detection using a SphereCast between start and end.
    /// Returns true if any collider in hitLayer is hit; hit info is from the SphereCast.
    /// </summary>
    private bool TrySphereHit(Vector3 start, Vector3 end, out RaycastHit hit)
    {
        hit = default;
        Vector3 delta = end - start;
        float dist = delta.magnitude + endPosExtension;
        if (dist <= 0f) return false;
        Vector3 dir = delta.normalized;

        // Perform the SphereCast as a fat ray
        bool didHit = Physics.SphereCast(
            start,
            boxHalfExtents,
            dir,
            out RaycastHit sphereHit,
            dist,
            hitLayer,
            QueryTriggerInteraction.Collide
        );

        if (debugDraw)
        {
            Vector3 rayEnd = start + dir * dist;
            Color lineColor = didHit ? debugRayHitColor : debugRayMissColor;
            Debug.DrawLine(start, rayEnd, lineColor, Time.fixedDeltaTime);

            // Draw bounding box approximating the swept sphere volume
            Vector3 boxCenter = start + dir * (dist * 0.5f);
            Vector3 boxSize = new Vector3(
                boxHalfExtents * 2f,
                boxHalfExtents * 2f,
                dist + boxHalfExtents * 2f
            );
            DebugExtensions.DebugDrawExtensions.DrawRotatedCube(
                position: boxCenter,
                rotation: Quaternion.LookRotation(dir),
                size: boxSize,
                color: lineColor,
                duration: Time.fixedDeltaTime
            );
        }

        if (didHit)
        {
            hit = sphereHit;
            return true;
        }
        return false;
    }


    private void ProcessHit(RaycastHit hit)
    {
        if (hit.collider.GetComponent<Projectile>() != null)
        {
            return;
        }

        int targetPV = 0;

        var damageable = hit.collider.GetComponent<IDamageable>();
        SurfaceType surface = SurfaceType.None;
        if (damageable != null)
        {
            surface = damageable.GetSurfaceType();
        }
        
        if (damageable == null)
        {
            Debug.LogWarning($"[Projectile] No IDamageable found on {hit.collider.name}. Checking fallback components...");

            // Optional: explore deeper if needed
            var hitZone = hit.collider.GetComponent<HitZone>();
            var composite = hit.collider.GetComponentInParent<CompositeHitbox>();
            var playerMain = hit.collider.GetComponentInParent<PlayerMain>();

            if (hitZone != null) Debug.Log($"[Projectile] HitZone component found on: {hitZone.name}");
            if (composite != null) Debug.Log($"[Projectile] CompositeHitbox found on: {composite.name}");
            if (playerMain != null) Debug.Log($"[Projectile] PlayerMain found on: {playerMain.name}");
        }
        else
        {
            targetPV = damageable.GetViewID();
        }

        if (damageable != null)
        {
            DamageManager.Instance.ReportDamage(
                attackerID: _ownerViewID,
                targetID: targetPV,
                damageAmount: _damage,
                damageSourceType: damageType,
                weaponId: _weaponId
            );
        }
        else
        {
            Debug.LogWarning($"[Projectile] Skipped damage report: no valid IDamageable found.");
        }

        // 1) Map surface type to both effectID and SFX SO
        int effectID;
        SoundDefinition impactSfx = null;

        switch (surface)
        {
            case SurfaceType.Flesh:
                effectID = _fleshImpactEffectID;
                impactSfx = _fleshImpactSfx;
                break;

            default:
                effectID = _structureImpactEffectID;
                impactSfx = _structureImpactSfx;
                break;
        }

        // 2) Attempt to play the SFX
        if (impactSfx == null)
        {
            Debug.LogWarning("[Projectile] No impactSfx assigned for this surface, skipping sound.");
        }
        else
        {
            SoundManager.Instance.Play(impactSfx.soundId, hit.point, _isOwner);
        }

        DamageManager.Instance.ReportImpact(
            impactEffectID: effectID,
            position: hit.point,
            normal: hit.normal
        );

        GhostProjectilePool.Instance.DestroyGhostRPC(
            GhostProjectilePool.MakeCompositeId(_ownerViewID, _projectileId)
        );

        _pool.Despawn(this);
    }


    public void RegisterToPool()
    {
    }
}
