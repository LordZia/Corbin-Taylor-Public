using System.Collections.Generic;
using UnityEngine;
using Items;

[RequireComponent(typeof(ParticleSystem))]
public class CasingParticleEjector : MonoBehaviour
{
    // Legacy Particle Based Casing Ejector; might use in the future
    /*
    [Header("Wiring")]
    [SerializeField] private PlayerEventBus eventBus;
    [SerializeField] private Transform defaultEjectPoint; // optional fallback

    private ParticleSystem _ps;
    private ParticleSystemRenderer _psr;

    private bool _configured;
    private Mesh[] _meshes;
    private Material _material;
    private Vector3 _spawnRotationOffsetEuler; 
    private float _lifetime;
    private float _gravity;
    private Vector2 _speedRange;
    private Vector2 _spinRange;
    private float _jitter;
    private Transform _ejectPoint;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
        _psr = GetComponent<ParticleSystemRenderer>();

        var emission = _ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        _psr.renderMode = ParticleSystemRenderMode.Mesh;
    }

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

    public void ConfigureFromDefinition(CasingSettings settings, Transform ejectPointOverride = null)
    {
        // Must be enabled and have a valid prefab with mesh + material
        if (!settings.enabled || settings.prefab == null ||
            !TryExtractFromPrefab(settings.prefab, out var meshes, out var material))
        {
            DisableCasings();
            return;
        }

        _meshes = meshes;
        _material = material;
        _spawnRotationOffsetEuler = settings.prefabRotationOffsetEuler;
        _lifetime = Mathf.Max(0f, settings.lifetime);
        _gravity = Mathf.Max(0f, settings.gravity);
        _speedRange = settings.ejectSpeed;
        _spinRange = settings.spinDegPerSec;
        _jitter = Mathf.Max(0f, settings.jitter);

        _ejectPoint = ejectPointOverride != null ? ejectPointOverride : defaultEjectPoint;

        _psr.material = _material;
        _psr.SetMeshes(_meshes);

        var main = _ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = _lifetime;
        main.gravityModifier = _gravity;
        main.startRotation3D = true;   // IMPORTANT: allow X/Y/Z rotation at spawn

        main.stopAction = ParticleSystemStopAction.None; // prevents accidental GO disable


        // Ensure the renderer uses 3D rotation in its vertex streams
        var streams = new List<ParticleSystemVertexStream>();
        _psr.GetActiveVertexStreams(streams);

        // If "Rotation" (Z only) is present, remove it
        streams.Remove(ParticleSystemVertexStream.Rotation);

        streams = new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Normal,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.Rotation3D,
            ParticleSystemVertexStream.Center
        };

        _psr.SetActiveVertexStreams(streams);

        _configured = true;
    }

    public void DisableCasings()
    {
        _configured = false;
        _meshes = null;
        _material = null;
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _ps.Pause(true);
        _ps.Clear(true);
    }

    private void OnWeaponSwapped(WeaponSwappedEvent evt)
    {
        // Example: ConfigureFromDefinition(evt.NewWeaponDef.casings, evt.NewWeaponEjectPoint);
        _ps.Clear();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static Vector3 ToEulerRad(Quaternion q)
    {
        var e = q.eulerAngles;
        return new Vector3(e.x * Mathf.Deg2Rad, e.y * Mathf.Deg2Rad, e.z * Mathf.Deg2Rad);
    }

    private void OnWeaponFired(WeaponFiredEvent evt)
    {
        if (!_configured) ConfigureFromDefinition(evt.Context.Definition.casings, null);
        if (!_configured) return;

        Transform eject = _ejectPoint ? _ejectPoint : transform;

        var ep = new ParticleSystem.EmitParams();
        ep.position = eject.position;

        float speed = Random.Range(_speedRange.x, _speedRange.y);
        ep.velocity = eject.right * speed + Random.insideUnitSphere * _jitter;

        // Compose rotation properly, then feed radians:
        Quaternion q = eject.rotation * Quaternion.Euler(_spawnRotationOffsetEuler);
        ep.rotation3D = ToEulerRad(q);

        // Temporarily disable spin to verify initial orientation visibly changes:
        // ep.angularVelocity3D = Vector3.zero;
        float spinDeg = Random.Range(_spinRange.x, _spinRange.y);
        ep.angularVelocity3D = Random.onUnitSphere * (spinDeg * Mathf.Deg2Rad);

        ep.applyShapeToPosition = false;
        _ps.Emit(ep, 1);
    }



    // Pull meshes and a material from the prefab without instantiating it.
    private static bool TryExtractFromPrefab(GameObject prefab, out Mesh[] meshes, out Material material)
    {
        meshes = null;
        material = null;
        if (prefab == null) return false;

        // Collect unique meshes from MeshFilters
        var mfs = prefab.GetComponentsInChildren<MeshFilter>(true);
        var meshList = new List<Mesh>(mfs.Length);
        foreach (var mf in mfs)
        {
            if (mf != null && mf.sharedMesh != null && !meshList.Contains(mf.sharedMesh))
                meshList.Add(mf.sharedMesh);
        }
        if (meshList.Count == 0) return false;

        meshes = meshList.ToArray();

        // Grab the first available material (assumes single-material casing)
        var mr = prefab.GetComponentInChildren<MeshRenderer>(true);
        if (mr != null && mr.sharedMaterial != null)
        {
            material = mr.sharedMaterial;
            return true;
        }

        // Fallback to any renderer's material if needed
        var anyRenderer = prefab.GetComponentInChildren<Renderer>(true);
        if (anyRenderer != null && anyRenderer.sharedMaterial != null)
        {
            material = anyRenderer.sharedMaterial;
            return true;
        }

        // No material found -> invalid
        meshes = null;
        return false;
    }
    */
}
