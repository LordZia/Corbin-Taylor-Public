using Items;
using UnityEngine;
using ZiaAnim;

public class FirstPersonAnimDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerEventBus eventBus;
    [SerializeField] FpAnimProfile profile;
    [SerializeField] WeaponController weaponController;

    [SerializeField] Transform weaponDisplayRoot;   // the object you want to wobble (local space)
    [SerializeField] Transform cameraTransform;     // main camera (for look)
    [SerializeField] Rigidbody playerRb;            // or CharacterController; just expose a speed provider

    [Header("Tuning")]
    [SerializeField] float idleSpeedThreshold = 0.1f; // m/s
    [SerializeField] float posLerpSpeed = 12f;
    [SerializeField] float rotLerpSpeed = 14f;

    // runtime
    private float _adsBlend;                // 0 hipfire -> 1 ADS (smoothed)
    private float _swayPhase, _bobPhase, _idlePhase;
    private Quaternion _prevCamRot;
    private Vector3 _recoilRotSpring, _recoilRotVel; // euler spring
    private float _recoilPosSpring, _recoilPosVel;   // forward pos spring

    // base snapshot (pre-offset)
    private bool _hasBase;
    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot;

    void Awake()
    {
        if (!weaponDisplayRoot) weaponDisplayRoot = transform;
        if (!cameraTransform) cameraTransform = Camera.main ? Camera.main.transform : null;

        SnapshotBase(); // capture anchor's initial local pose

        _prevCamRot = cameraTransform ? cameraTransform.rotation : Quaternion.identity;

        // Optional: subscribe to your recoil driver if it exposes an event
        // RecoilDriver.OnKick += OnRecoilKick;
    }

    // Call this if you ever swap the weaponDisplayRoot at runtime
    public void SnapshotBase()
    {
        if (!weaponDisplayRoot) return;
        _baseLocalPos = weaponDisplayRoot.localPosition;
        _baseLocalRot = weaponDisplayRoot.localRotation;
        _hasBase = true;
    }

    public void OnRecoilKick(Vector2 kickYawPitchDeg)
    {
        // Map camera kick to weapon tilt and slight pushback
        var amp = profile.recoilRotAmplitude * Mathf.Lerp(profile.hipfireRecoilVisMul, profile.adsRecoilVisMul, _adsBlend);
        _recoilRotSpring += new Vector3(-kickYawPitchDeg.y, kickYawPitchDeg.x, -kickYawPitchDeg.x).Multiply(amp);

        float posAmp = profile.recoilPosAmplitude * Mathf.Lerp(profile.hipfireRecoilVisMul, profile.adsRecoilVisMul, _adsBlend);
        _recoilPosSpring += posAmp; // small push back
    }

    void Update()
    {
        if (!profile || !weaponDisplayRoot || !cameraTransform) return;
        if (!_hasBase) SnapshotBase();

        // --- ADS blend ---
        bool isADS = false; // or read from context/events
        float target = isADS ? 1f : 0f;
        _adsBlend = Mathf.Lerp(_adsBlend, target, 1f - Mathf.Exp(-profile.adsEnterSpeed * Time.deltaTime));

        // --- speed and phases ---
        float speed = playerRb ? playerRb.velocity.magnitude : 0f;

        float swayHz = profile.swaySpeedToPhase.Evaluate(speed);
        _swayPhase += swayHz * Time.deltaTime;

        float bobHz = Mathf.Lerp(profile.bobAmplitude > 0 ? swayHz : 0f, swayHz, 1f);
        _bobPhase += bobHz * Time.deltaTime;

        _idlePhase += profile.idleHz * Time.deltaTime;

        // --- look lag ---
        Quaternion camRot = cameraTransform.rotation;
        Quaternion delta = camRot * Quaternion.Inverse(_prevCamRot);
        delta.ToAngleAxis(out float deltaAngle, out Vector3 deltaAxis);
        Vector3 eulerDelta = deltaAngle > 0.001f ? deltaAxis.normalized * (deltaAngle) : Vector3.zero;

        Vector3 localDelta = cameraTransform.InverseTransformDirection(eulerDelta);
        float desiredYawLag = Mathf.Clamp(-localDelta.y, -profile.lookLagMaxYawDeg, profile.lookLagMaxYawDeg);
        float desiredPitchLag = Mathf.Clamp(localDelta.x, -profile.lookLagMaxPitchDeg, profile.lookLagMaxPitchDeg);

        float lagMul = Mathf.Lerp(profile.hipfireLookLagMul, profile.adsLookLagMul, _adsBlend);
        desiredYawLag *= lagMul;
        desiredPitchLag *= lagMul;

        Vector2 currentLag = Vector2.zero;
        currentLag.x = Mathf.LerpAngle(0f, desiredYawLag, 1f - Mathf.Exp(-profile.lookLagFollowSpeed * Time.deltaTime));
        currentLag.y = Mathf.LerpAngle(0f, desiredPitchLag, 1f - Mathf.Exp(-profile.lookLagFollowSpeed * Time.deltaTime));

        _prevCamRot = camRot;

        // --- movement sway ---
        float swayMul = Mathf.Lerp(profile.hipfireSwayMul, profile.adsSwayMul, _adsBlend);
        float swayX = profile.swayX.Evaluate(_swayPhase) * profile.swayAmplitude * swayMul;
        float swayY = profile.swayY.Evaluate(_swayPhase + 0.25f) * profile.swayAmplitude * swayMul;
        float swayRollDeg = profile.swayRotAmplitude * swayMul * -swayX * 20f;

        // --- bob ---
        float bobMul = Mathf.Lerp(profile.hipfireBobMul, profile.adsBobMul, _adsBlend);
        float bobX = profile.bobX.Evaluate(_bobPhase) * profile.bobAmplitude * bobMul;
        float bobY = profile.bobY.Evaluate(_bobPhase + 0.5f) * profile.bobAmplitude * bobMul;

        // --- idle ---
        float idleMul = (speed < idleSpeedThreshold) ? Mathf.Lerp(profile.hipfireIdleMul, profile.adsIdleMul, _adsBlend) : 0f;
        float idleY = profile.idleY.Evaluate(_idlePhase) * profile.idleAmplitude * idleMul;

        // --- visual recoil spring ---
        float d = profile.springDamping, w = profile.springFrequency;
        SpringDamped(ref _recoilRotSpring, ref _recoilRotVel, Vector3.zero, d, w);
        SpringDamped(ref _recoilPosSpring, ref _recoilPosVel, 0f, d, w);

        // --- compose offsets relative to base snapshot ---
        Vector3 offsetPos = new Vector3(swayX + bobX, swayY + bobY + idleY, -_recoilPosSpring);
        Quaternion deltaRot =
            Quaternion.AngleAxis(currentLag.x, Vector3.up) *
            Quaternion.AngleAxis(currentLag.y, Vector3.right) *
            Quaternion.AngleAxis(swayRollDeg, Vector3.forward) *
            Quaternion.Euler(_recoilRotSpring);

        Vector3 targetPos = _baseLocalPos + offsetPos;
        Quaternion targetRot = _baseLocalRot * deltaRot;

        // apply with smoothing
        weaponDisplayRoot.localPosition = Vector3.Lerp(weaponDisplayRoot.localPosition, targetPos, posLerpSpeed * Time.deltaTime);
        weaponDisplayRoot.localRotation = Quaternion.Slerp(weaponDisplayRoot.localRotation, targetRot, rotLerpSpeed * Time.deltaTime);
    }

    // --- utilities ---
    static void SpringDamped(ref Vector3 x, ref Vector3 v, Vector3 target, float damping, float freq)
    {
        float dt = Time.deltaTime;
        float k = freq * freq;
        float c = 2f * damping;
        Vector3 a = (target - x) * k - v * c;
        v += a * dt;
        x += v * dt;
    }
    static void SpringDamped(ref float x, ref float v, float target, float damping, float freq)
    {
        float dt = Time.deltaTime;
        float k = freq * freq;
        float c = 2f * damping;
        float a = (target - x) * k - v * c;
        v += a * dt;
        x += v * dt;
    }
}

static class VecExt
{
    public static Vector3 Multiply(this Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
}
