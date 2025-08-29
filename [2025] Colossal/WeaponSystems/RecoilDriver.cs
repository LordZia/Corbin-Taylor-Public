using UnityEngine;

public class RecoilDriver : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform target;

    [Header("Motion")]
    [SerializeField] private float snapSpeed = 50f;     // SmoothDamp smoothTime = 1/snapSpeed
    [SerializeField] private float returnSpeed = 15f;   // deg/sec toward target/zero
    [SerializeField] private float maxYawDeg = 30f;
    [SerializeField] private float maxPitchDeg = 30f;

    [Header("Kick Shaping")]
    [SerializeField] private float kickRiseTime = 0.06f;

    [Header("Return Hold")]
    [Tooltip("Minimum time to hold the target (no decay) after each ApplyKick.")]
    [SerializeField] private float baseReturnHold = 0.10f; // seconds

    private Quaternion _baseLocalRot;
    private Vector2 _current;
    private Vector2 _target;
    private Vector2 _vel;

    // kick rise state
    private Vector2 _targetStart;
    private Vector2 _targetEnd;
    private float _kickT;
    private bool _kicking;

    // hold/partial-return state
    private float _holdUntil;                   // absolute time until which decay is disabled
    private bool _applyAnchorAtHoldEnd;         // set when a partial return has been requested
    private float _pendingReturnFraction;       // 0..1, how far toward zero to go immediately after hold
    private float _pendingBleedDuration;        // seconds, optional slow bleed to zero after reaching anchor

    private bool _hasReturnAnchor;
    private Vector2 _returnAnchor;              // where we decay to after hold (partial)
    private bool _bleeding;                     // true while doing time-based bleed-to-zero
    private Vector2 _bleedStart;
    private float _bleedStartTime;
    private float _bleedDuration;

    void Awake()
    {
        if (target == null) target = transform;
        _baseLocalRot = target.localRotation;
    }

    /// <summary>
    /// Request a new yaw/pitch kick in degrees. This lerps the target from current to new end over kickRiseTime.
    /// </summary>
    public void ApplyKick(Vector2 deltaDeg)
    {
        var end = _target + deltaDeg;
        end.x = Mathf.Clamp(end.x, -maxYawDeg, maxYawDeg);
        end.y = Mathf.Clamp(end.y, -maxPitchDeg, maxPitchDeg);

        _targetStart = _target;
        _targetEnd = end;
        _kickT = 0f;
        _kicking = true;

        // Any new kick cancels anchor/bleed plans; we will reschedule on the next HoldFor call.
        _hasReturnAnchor = false;
        _bleeding = false;

        HoldFor(baseReturnHold); // keep your existing default
    }

    /// <summary>
    /// Prevent return-to-zero for at least 'seconds' from now (legacy behavior).
    /// </summary>
    public void HoldFor(float seconds)
    {
        float until = Time.time + Mathf.Max(0f, seconds);
        if (until > _holdUntil) _holdUntil = until;

        // No partial return scheduled: after hold, decay to zero using returnSpeed
        _applyAnchorAtHoldEnd = false;
        _pendingReturnFraction = 1f;
        _pendingBleedDuration = 0f;
    }

    /// <summary>
    /// Prevent return for 'seconds', then decay toward a partial amount of the current offset.
    /// returnFraction = 1 means full return to zero (legacy).
    /// returnFraction = 0 means hold the current offset (no immediate return).
    /// postHoldBleedTime > 0 will slowly bleed from the partial anchor down to zero over that many seconds.
    /// </summary>
    public void HoldFor(float seconds, float returnFraction, float postHoldBleedTime)
    {
        float until = Time.time + Mathf.Max(0f, seconds);
        if (until > _holdUntil) _holdUntil = until;

        _pendingReturnFraction = Mathf.Clamp01(returnFraction);
        _pendingBleedDuration = Mathf.Max(0f, postHoldBleedTime);
        _applyAnchorAtHoldEnd = true;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) Advance kick rise
        if (_kicking)
        {
            float rise = Mathf.Max(0.0001f, kickRiseTime);
            _kickT += dt / rise;

            if (_kickT >= 1f)
            {
                _kicking = false;
                _target = _targetEnd;
            }
            else
            {
                _target = Vector2.Lerp(_targetStart, _targetEnd, _kickT);
            }
        }
        else
        {
            // 2) Handle hold / partial return / bleed
            if (Time.time < _holdUntil)
            {
                // still holding: do nothing
            }
            else
            {
                // First frame after hold expires: compute anchor if requested
                if (_applyAnchorAtHoldEnd)
                {
                    // Anchor is a point between current offset and zero.
                    // Example: returnFraction = 0.3 -> go 30% of the way back toward zero.
                    _returnAnchor = Vector2.Lerp(_target, Vector2.zero, _pendingReturnFraction);
                    _hasReturnAnchor = true;
                    _applyAnchorAtHoldEnd = false;

                    // Reset bleeding for this cycle
                    _bleeding = false;
                    _bleedDuration = _pendingBleedDuration;
                }

                if (_hasReturnAnchor)
                {
                    // Move toward anchor using returnSpeed
                    _target = Vector2.MoveTowards(_target, _returnAnchor, returnSpeed * dt);

                    // Arrived at anchor: optionally start a timed bleed to zero
                    if ((_target - _returnAnchor).sqrMagnitude <= 1e-6f)
                    {
                        _hasReturnAnchor = false;

                        if (_bleedDuration > 0f)
                        {
                            _bleeding = true;
                            _bleedStart = _target;          // equals anchor now
                            _bleedStartTime = Time.time;
                        }
                    }
                }
                else if (_bleeding)
                {
                    float t = Mathf.Clamp01((Time.time - _bleedStartTime) / _bleedDuration);
                    _target = Vector2.Lerp(_bleedStart, Vector2.zero, t);
                    if (t >= 1f) _bleeding = false;
                }
                else
                {
                    // Legacy decay straight to zero
                    if (_target.sqrMagnitude > 0f)
                    {
                        _target = Vector2.MoveTowards(_target, Vector2.zero, returnSpeed * dt);
                    }
                }
            }
        }

        // 3) Smooth applied value toward target
        _current = Vector2.SmoothDamp(
            _current, _target, ref _vel,
            1f / Mathf.Max(0.0001f, snapSpeed),
            Mathf.Infinity, dt
        );

        // 4) Apply yaw/pitch
        var yaw = Quaternion.AngleAxis(_current.x, Vector3.up);
        var pitch = Quaternion.AngleAxis(-_current.y, Vector3.right);
        target.localRotation = _baseLocalRot * yaw * pitch;
    }

    public void ResetAll()
    {
        _current = Vector2.zero;
        _target = Vector2.zero;
        _vel = Vector2.zero;
        _kicking = false;
        _kickT = 0f;

        _holdUntil = 0f;
        _applyAnchorAtHoldEnd = false;
        _pendingReturnFraction = 1f;
        _pendingBleedDuration = 0f;

        _hasReturnAnchor = false;
        _bleeding = false;

        target.localRotation = _baseLocalRot;
    }

    public void SetBaseLocalRotation(Quaternion rot) { _baseLocalRot = rot; }
    public void SetMaxClamp(float yawDeg, float pitchDeg) { maxYawDeg = yawDeg; maxPitchDeg = pitchDeg; }
    public void SetReturnHold(float seconds) { baseReturnHold = Mathf.Max(0f, seconds); }
}
