using System;
using System.Collections.Generic;
using UnityEngine;
using RaycastExtensions;
using DebugExtensions;
using MathExt;
using ZiaPlayer;
using CameraSystem;

[RequireComponent(typeof(Rigidbody))]
public class test_WallRunHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Zia_PlayerMovement pMovement;
    [SerializeField] private CameraRotationManager cameraRotationManager;
    [SerializeField] private LayerMask wallrunableLayers;
    [SerializeField] private bool debugMode = true;

    [Header("Movement Settings")]
    [SerializeField] private float wallPullForce = 8f;
    [SerializeField] private float wallRunSpeedAcceleration = 5f;
    [SerializeField] private float wallInputSpeedCap = 8f;
    [SerializeField] private float wallFriction = 2f;
    [SerializeField] private float wallRunDistance = 0.75f;

    [SerializeField] private float wallRunExitPassiveForce = 2f;
    [SerializeField] private float wallRunExitActiveForce = 5f;
    [SerializeField][Range(0,1)] private float wallDetachInputThreshold = 0.2f;

    [Header("Vertical Damping & Exit Accel")]
    [Tooltip("How fast to remove existing vertical speed (units per second)")]
    [SerializeField] private float verticalVelocityDampeningSpeed = 20f;
    [Tooltip("Max downward acceleration to apply as you near the end of your run (units/sec)")]
    [SerializeField] private float maxDownwardAcceleration = 30f;
    [Tooltip("Origin height of the top of wall checks; prevents the player from going above the top of the wall during a wallrun")]
    [SerializeField] private float topOfWallCheckHeight = 1f;
    [Tooltip("How many units per second are added as downward velocity during the duration of the wallrun")]
    [SerializeField] private float wallSlipAccelerationRate = 5f;
    [SerializeField] private AnimationCurve slipOverTimeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float maxSlipAcceleration = 30f;

    private float cachedInitialVerticalVelocity;

    [Header("WallRun Duration Settings")]
    [Tooltip("Slowest possible wall-run (at min speed)")]
    [SerializeField] private float minWallRunDuration = 0.5f;
    [Tooltip("Fastest possible wall-run (at max speed)")]
    [SerializeField] private float maxWallRunDuration = 3f; // seconds you can stay on the wall
    [Tooltip("Speed at which you get the full max duration")]
    [SerializeField] private float speedForMaxDuration = 10f;
    private float wallRunTimeRemaining = 0f;
    private float _wallRunProgress;

    [Header("Wall-Run Enter Timing")]
    [SerializeField] private float wallGrabDuration = 0.25f;  // seconds to “stick” before full run 
    private float grabTimer = 0f;

    [Header("Wall-Run Enter Boost Settings")]
    [SerializeField] private float maxEnterBoost = 5f;   // how big a speed boost at max momentum
    [SerializeField] private float momentumForFullEnterBoost = 10f;  // horizontal speed needed to get the full boost
    [SerializeField] private float minVerticalSpeedForEnterBoost = 1f;   // y-velocity threshold
    [SerializeField] private float verticalEnterBoost = 2f;   // extra upward kick

    [Header("Detection Settings")]
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private float wallGravSphereCastRadius = 0.25f; // for more forgiving hit
    [SerializeField] private float maxWallEnterDistance = 1.5f;
    [SerializeField] private float lookAheadDistance = 0.2f;
    [SerializeField] private float squareCheckScale = 1;

    [Header("Curve Settings")]
    [SerializeField, Range(-1f, 1f)] private float minIntersectionAlignment = 0.3f;
    [SerializeField, Range(0.5f, 1f)] private float parallelNormalsDotThreshold = 0.99f;
    [SerializeField] private float intersectionPriorityDistanceThreshold = 1f;
    [SerializeField] private float intersectionProximityThreshold = 0.2f;
    [SerializeField] private float curveInputSpeedCap = 12;

    private float currentInputSpeedCap = 8;

    [Header("Input & Cooldown")]
    [SerializeField] private float minWallRunVelocity = 0.025f;
    [SerializeField][Tooltip("Delay (in seconds) before the player can grab the same wall normal as the wall they last exited wallrun from")] private float reGrabDelay = 0.8f;
    [SerializeField] private float maxWallRuntime = 3.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float normalAngleThreshold = 5f;

    [Header("Corner Turn Tuning")]
    [SerializeField, Tooltip("Max yaw speed when forced around a corner (deg/sec)")]
    private float maxCornerTurnSpeed = 120f;
    [SerializeField, Tooltip("How fast we ramp our yaw speed toward the target (deg/sec²)")]
    private float cornerTurnAcceleration = 600f;

    // 2) Cached state
    private float currentCornerTurnSpeed;    // our running angular velocity (deg/sec)


    [Header("Camera Tilt Settings")]
    [SerializeField] private float endTiltTarget = 15f;
    [SerializeField] private float startTiltTarget = 5f;
    [SerializeField] private float tiltSpeed = 90f;   // degrees per second to ramp in/out
    [SerializeField] private float tiltRampUpTime = 0.2f;
    [SerializeField] private Transform cameraTransform; // your actual Camera
    [SerializeField] private float tiltResetSpeed = 180f; // degrees per second
    [SerializeField] private float tiltBlendSpeed = 60f;

    private bool _resettingTilt = false;
    private float currentTiltAngle = 0f;

    Quaternion _camBaseLocalRot;

    // Events
    public event Action OnWallRunStart;
    public event Action OnWallRunStop;
    public event Action OnWallRunEnter;
    public event Action OnWallRunExit;

    public bool IsWallrunning => isWallrunning;

    // State
    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 inputDir;
    private Vector2 velocityHorizontal;

    private Vector3 currentWallNormal;
    private Vector3 lastDetectedWallNormal = Vector3.zero;
    private Vector3 detectedWallPos;

    private Vector3 nextWallNormal;
    private Vector3 nextWallPos;
    private Vector3 runDir;

    private bool isStartingWallrun;
    private bool isWallrunning;
    [SerializeField] private bool isReGrabCooldownActive;
    [SerializeField] private float reGrabTimer;

    private Vector3 currentWallTangent; // Measures the line running perpendicular to the current wall normal

    public float currentVelMag = 0;

    private bool isCornering;
    [SerializeField] private float cornerTurnRate;   // degrees per meter along the tangent
    [SerializeField] private float momentumRedirectionOnActiveWallExit = 0;

    private bool _wallRunCancelledThisFrame = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (pMovement == null) pMovement = GetComponent<Zia_PlayerMovement>();
    }

    void FixedUpdate()
    {
        UpdateCooldown();

        if (isGrounded)
        {
            return;
        }

        velocityHorizontal = new Vector2(rb.velocity.x, rb.velocity.z);
        currentVelMag = velocityHorizontal.magnitude;


        if (isStartingWallrun)
        {
            HandleWallRunEntering();

            //ContinuedDetection();
            //if (isWallrunning)
            //  HandleWallRunContinuous();
        }
        else if (isWallrunning)
        {
            
            // Drain wall run timer
            wallRunTimeRemaining -= Time.fixedDeltaTime;
            if (wallRunTimeRemaining <= 0f)
            {
                // out of time, passively exit
                PassiveWallExit();
                return;
            }
            
            /*
            // measure horizontal speed
            Vector3 horiz = rb.velocity;
            horiz.y = 0f;
            float speed = horiz.magnitude;

            // remap speed -> [0…1]
            float speedT = Mathf.InverseLerp(0f, speedForMaxDuration, speed);

            // pick the “effective” duration this frame
            float desiredDuration = Mathf.Lerp(
                minWallRunDuration,
                maxWallRunDuration,
                speedT
            );

            // advance progress by the fraction of that duration
            _wallRunProgress += Time.deltaTime / desiredDuration;

            if (_wallRunProgress >= 1f)
            {
                PassiveWallExit();
                return;
            }
            */

            ContinuedDetection();

            // This is silly but ContinuedDetection() can cancel wallrunning; Will remove this garbage with a state machine based refactor - zia
            if (isWallrunning)
            {
                HandleWallRunContinuous();
            }
        }
        else
        {
            PreliminaryDetection();
        }

        HandleWallRunCancelling();
        WallRunFailsafeCheck();
    }

    [Header("Curve Follow Settings")]
    [SerializeField] private float followSmoothSpeed = 5f;  // higher = tighter follow

    private Vector3 _lastWallTangent;
    private bool _justStartedCorner;
    private float _currentCornerTurnSpeed;
    private bool _justStartedWallrun = true;

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (isCornering && currentWallTangent.sqrMagnitude > 0f)
        {
            // 1) How fast are we moving along the wall?
            Vector3 flatVel = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
            float forwardSpeed = Vector3.Dot(flatVel, currentWallTangent.normalized);

            // 2) cornerTurnRate (deg per meter) × forwardSpeed (m/s) desired yaw rate (deg/s)
            float desiredYawRate = cornerTurnRate * forwardSpeed;

            // 3) Ramp toward that yaw-rate, respecting angular accel
            _currentCornerTurnSpeed = Mathf.MoveTowards(
                _currentCornerTurnSpeed,
                desiredYawRate,
                cornerTurnAcceleration * dt
            );

            // 4) Clamp to your max turn speed
            _currentCornerTurnSpeed = Mathf.Clamp(
                _currentCornerTurnSpeed,
               -maxCornerTurnSpeed,
                maxCornerTurnSpeed
            );

            // 5) Convert speed (deg/s)  delta for this frame
            float yawDelta = _currentCornerTurnSpeed * dt;

            // 6) Apply it
            transform.Rotate(0f, yawDelta, 0f, Space.World);
        }
        /*
        // a) determine desired yaw rate in deg/sec
        Vector3 flatVel = rb.velocity; flatVel.y = 0f;
        float forwardSpeed = Vector3.Dot(flatVel, currentWallTangent);
        float desiredYawRate = cornerTurnRate * forwardSpeed;

        // b) ramp toward that target rate, capped by our angular acceleration
        currentCornerTurnSpeed = Mathf.MoveTowards(
            currentCornerTurnSpeed,
            desiredYawRate,
            cornerTurnAcceleration * Time.deltaTime
        );

        // c) clamp to our max turn speed
        currentCornerTurnSpeed = Mathf.Clamp(
            currentCornerTurnSpeed,
           -maxCornerTurnSpeed,
            maxCornerTurnSpeed
        );

        // d) rotate by that speed * deltaTime (deg this frame)
        float yawDelta = currentCornerTurnSpeed * Time.deltaTime;
        transform.Rotate(0f, yawDelta, 0f, Space.World);

        // 5) make a rotation around that axis by the *scaled* tilt angle
        //Quaternion tiltQuat = Quaternion.AngleAxis(currentTiltAngle, currentWallTangent);

        // 6) apply it on top of the camera’s existing world rotation
        //cameraTransform.rotation = tiltQuat * cameraTransform.rotation;
        //currentInputSpeedCap = curveInputSpeedCap;
        */
        else
        {
            // if we're not cornering, ease back to zero turn speed
            currentCornerTurnSpeed = Mathf.MoveTowards(
                currentCornerTurnSpeed,
                0f,
                cornerTurnAcceleration * Time.deltaTime
            );

            currentInputSpeedCap = wallInputSpeedCap;
        }

        if (isStartingWallrun)
        {
            
            float t = wallGrabDuration - grabTimer;
            float progress = Mathf.Clamp01(t);

            //TiltCameraOverProgress(progress, startTiltTarget, tiltSpeed);
        }
        if (IsWallrunning || isStartingWallrun && !isCornering)
        {
            float progress = 1f - (wallRunTimeRemaining / maxWallRunDuration);
            //float progress = 1 - _wallRunProgress;
            TiltCameraOverProgress(progress, endTiltTarget, tiltSpeed);
            ApplyCameraTilt();
        }

        if (_resettingTilt)
        {
            //HandleCameraTiltResetting();
        }
    }

    #region Camera Tilt
    private void HandleCameraTiltResetting()
    {
        // Ease Z back to zero
        Vector3 eulers = cameraTransform.localEulerAngles;
        float pitch = eulers.x;
        float yaw = eulers.y;
        float current = (eulers.z > 180f) ? eulers.z - 360f : eulers.z;
        float nextRoll = Mathf.MoveTowardsAngle(current, 0f, tiltResetSpeed * Time.deltaTime);

        cameraTransform.localEulerAngles = new Vector3(
            pitch,
            yaw,
            nextRoll
        );

        if (Mathf.Abs(nextRoll) < 0.01f)
            _resettingTilt = false;
    }
    private void ApplyCameraTilt()
    {
        // how far through the run are we? (0 at start, 1 at end)
        float t = 1f - (wallRunTimeRemaining / maxWallRunDuration);
        t = Mathf.Clamp01(t);
        float currentTiltAngle = Mathf.Lerp(startTiltTarget, endTiltTarget, t);

        // 1) flatten the wall normal so it’s purely horizontal
        Vector3 flatWallNormal = new Vector3(
            currentWallNormal.x,
            0f,
            currentWallNormal.z
        ).normalized;

        // 2) define the outward direction from the wall
        Vector3 wallOut = -flatWallNormal;

        // 3) pick your camera’s current up vector
        Vector3 camUp = cameraTransform.up;

        // 4) the hinge axis is perpendicular to camUp and wallOut
        Vector3 axis = Vector3.Cross(camUp, wallOut).normalized;
        if (axis.sqrMagnitude < 0.0001f)
        {
            // fallback to a safe axis if degenerate
            axis = Vector3.up;
        }

        // 5) make a rotation around that axis by the *scaled* tilt angle
        Quaternion tiltQuat = Quaternion.AngleAxis(currentTiltAngle, axis);

        // 6) apply it on top of the camera’s existing world rotation
        //cameraTransform.rotation = tiltQuat * cameraTransform.rotation;


        cameraRotationManager.AddRotateRequest(RotateSource.Wallrun, new RotateRequest
        {
            targetRotation = tiltQuat,
            blendSpeed = tiltBlendSpeed,
            duration = 0f
        });
    }

    /// <summary>
    /// Smoothly tilts the camera based on a normalized progress [0..1],
    /// stepping the internal tilt toward the target at up to tiltSpeed deg/sec.
    /// </summary>
    /// <param name="progress">
    ///   How far along the tilt you are, from 0 (no tilt) to 1 (full tilt).
    /// </param>
    /// <param name="maxTiltAngle">
    ///   The tilt angle in degrees when progress == 1.
    /// </param>
    /// <param name="tiltSpeed">
    ///   The maximum change in tilt per second (deg/sec).
    /// </param>
    private void TiltCameraOverProgress(
        float progress,
        float maxTiltAngle,
        float tiltSpeed
    )
    {
        // 1) clamp progress to [0,1]
        float t = Mathf.Clamp01(progress);

        // 2) compute the desired tilt angle this frame
        float targetAngle = Mathf.Lerp(0f, maxTiltAngle, t);

        // 3) step our stored tilt toward that target
        float maxDelta = tiltSpeed * Time.deltaTime;
        float oldAngle = currentTiltAngle;
        float newAngle = Mathf.MoveTowards(oldAngle, targetAngle, maxDelta);
        float deltaAngle = newAngle - oldAngle;
        currentTiltAngle = newAngle;

        // 4) if the step is too small, bail out
        if (Mathf.Abs(deltaAngle) < Mathf.Epsilon)
            return;

        // 5) flatten the wall normal horizontally
        Vector3 flatWall = new Vector3(
            currentWallNormal.x,
            0f,
            currentWallNormal.z
        ).normalized;
        if (flatWall.sqrMagnitude < 0.0001f)
            flatWall = Vector3.forward;

        // 6) compute hinge axis = cameraUp x (away from wall)
        Vector3 wallOut = -flatWall;
        Vector3 camUp = cameraTransform.up;
        Vector3 axis = Vector3.Cross(camUp, wallOut).normalized;
        if (axis.sqrMagnitude < 0.0001f)
            axis = Vector3.up;

        // 7) apply only the delta roll around that axis
        Quaternion deltaQ = Quaternion.AngleAxis(deltaAngle, axis);
        cameraTransform.rotation = deltaQ * cameraTransform.rotation;
    }
    #endregion

    /// ==== Public API ====
    public void UpdatePlayerInputDir(Vector2 dir) => inputDir = dir;
    public void UpdateGroundedStatus(bool grounded)
    {
        isGrounded = grounded;

        if (isGrounded)
        {
            lastDetectedWallNormal = Vector3.zero;
            ResetCooldown();
            StopWallRun();
        }
    }

    public void ActiveWallExit()
    {
        OnWallRunExit?.Invoke();
        StopWallRun();

        Vector3 worldIn = new Vector3(inputDir.x, 0f, inputDir.y);
        RedirectMomentum(worldIn, momentumRedirectionOnActiveWallExit);
        ApplyExitForce(wallRunExitActiveForce);
    }

    // ==== Core State Transitions ====

    private void PreliminaryDetection()
    {
        Vector3 origin = transform.position;
        float maxDist = wallCheckDistance;

        // build your two candidate directions
        Vector3 velDir = velocityHorizontal.magnitude >= minWallRunVelocity
            ? new Vector3(velocityHorizontal.x, 0, velocityHorizontal.y).normalized
            : Vector3.zero;
        Vector3 inpDir = inputDir.sqrMagnitude > 0
            ? new Vector3(inputDir.x, 0, inputDir.y).normalized
            : Vector3.zero;

        // attempt a spherecast along input first
        RaycastHit hitInp = default;
        bool inputHit = inpDir != Vector3.zero
            && Physics.SphereCast(origin, wallGravSphereCastRadius, inpDir, out hitInp, maxDist, wallrunableLayers);
        if (debugMode && inpDir != Vector3.zero)
            Debug.DrawLine(origin, origin + inpDir * maxDist, Color.cyan);

        // if that missed, try along velocity
        RaycastHit hitVel = default;
        bool velHit = !inputHit
            && velDir != Vector3.zero
            && Physics.SphereCast(origin, wallGravSphereCastRadius, velDir, out hitVel, maxDist, wallrunableLayers);
        if (debugMode && velDir != Vector3.zero)
            Debug.DrawLine(origin, origin + velDir * maxDist, Color.magenta);

        if (inputHit || velHit)
        {
            var hit = inputHit ? hitInp : hitVel;
            if (CanRegrab(hit.normal))
            {
                detectedWallPos = hit.point;
                currentWallNormal = hit.normal;

                HandleWallRunEnter();
                OnWallRunStart?.Invoke();
            }
        }
        else
        {
            // no valid hit
            detectedWallPos = Vector3.zero;
            currentWallNormal = Vector3.zero;
        }
    }

    /// <summary>
    /// Called once after preliminary detection finds a wall
    /// </summary>
    private void HandleWallRunEnter()
    {
        if (debugMode)
            Debug.Log("recieved call to start wallrun enter");
        // 1) enter state
        isStartingWallrun = true;
        _resettingTilt = false;
        reachedTopOfWall = false;

        _camBaseLocalRot = cameraTransform.localRotation;
        pMovement.ToggleCustomGravity(false);

        // 2) redirect horizontal momentum onto wall tangent
        Vector3 origVel = rb.velocity;
        Vector3 horizVel = new Vector3(origVel.x, 0f, origVel.z);

        Vector3 tangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(horizVel, tangent) < 0f)
            tangent = -tangent;

        float alignment = horizVel.sqrMagnitude > 0.0001f
            ? Mathf.Clamp01(Vector3.Dot(horizVel.normalized, tangent))
            : 0f;

        RedirectMomentum(tangent, alignment);

        // 3) vertical boost if you were already ascending


        // 4) reset grab timer
        grabTimer = 0f;
    }

    /// <summary>
    /// Called on a fixed update while approaching the wall
    /// </summary>
    private void HandleWallRunEntering()
    {
        if (!isStartingWallrun)
            return;
        
        Vector3 origin = transform.position;
        Vector3 inward = -currentWallNormal.normalized;
        float maxCheck = wallCheckDistance;

        // debug the inward cast
        if (debugMode)
            Debug.DrawLine(origin, origin + inward * maxCheck, Color.red, Time.fixedDeltaTime);

        // sphere-cast into the wall
        if (Physics.SphereCast(origin, wallGravSphereCastRadius, inward, out RaycastHit hit, maxCheck, wallrunableLayers))
        {
            // still on the wall -> advance the grab timer
            grabTimer += Time.fixedDeltaTime;

            // pull toward wall
            rb.AddForce(inward * wallPullForce, ForceMode.Force);

            // Dampen any downward velocity during entering period
            //UpdateVerticalVelocityWithSlip(verticalVelocityDampeningSpeed);
            CheckAndClampVelAtTopOfWall();

            // once we've "grabbed" long enough, start the run
            if (grabTimer >= wallGrabDuration)
            {
                grabTimer = 0f;
                //rb.velocity = new Vector3(vel.x, 0, vel.z);
                StartWallRun();
            }
        }
        else
        {
            // lost the wall -> cancel wallgrab
            grabTimer = 0f;
            PassiveWallExit();
        }
    }

    /// <summary>
    /// Called once when we reach the wall
    /// </summary>
    private void StartWallRun()
    {
        isWallrunning = true;
        isStartingWallrun = false;
        wallRunElapsedTime = 0f;

        wallRunTimeRemaining = maxWallRunDuration;
        pMovement.SetCustomGravityScale(0f);

        // horizontal boost only
        Vector3 postVel = rb.velocity;
        Vector3 newHoriz = new Vector3(postVel.x, 0f, postVel.z);
        float speed = newHoriz.magnitude;

        if (speed > 0.01f)
        {
            float frac = Mathf.Clamp01(speed / momentumForFullEnterBoost);
            float boost = frac * maxEnterBoost;
            newHoriz += newHoriz.normalized * boost;
        }

        rb.velocity = new Vector3(newHoriz.x, postVel.y, newHoriz.z);

        bool allowBoost = HasWallAbove(currentWallNormal);

        if (allowBoost)
        {
            if (debugMode)
                Debug.Log("applying vertical velocity boost");

            Vector3 postVertVel = rb.velocity;
            postVertVel.y += verticalEnterBoost;
            rb.velocity = postVertVel;
        }

        cachedInitialVerticalVelocity = rb.velocity.y;

        // normalize speed into [0..1]
        float t = Mathf.InverseLerp(0f, speedForMaxDuration, speed);

        // pick the duration
        wallRunTimeRemaining = Mathf.Lerp(minWallRunDuration, maxWallRunDuration, t);

        OnWallRunStart?.Invoke();
    }

    [SerializeField] private float minLookAheadDistance = 0.75f;     // always look at least this far
    [SerializeField] private float maxLookAheadDistance = 2f;     // never look farther than this

    private void ContinuedDetection()
    {
        // 1) Build horizontal velocity & input direction
        Vector3 flatVel = new Vector3(velocityHorizontal.x, 0f, velocityHorizontal.y);
        Vector3 moveDir = flatVel.sqrMagnitude > 0.01f
            ? flatVel.normalized
            : Vector3.zero;
        Vector3 inDir = inputDir.sqrMagnitude > 0f
            ? new Vector3(inputDir.x, 0f, inputDir.y).normalized
            : Vector3.zero;

        // 2) Determine detDir via existing wall normal or input/velocity
        Vector3 detDir;
        if (currentWallNormal.sqrMagnitude > 0.001f)
        {
            Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
            Vector3 alignRef = moveDir != Vector3.zero ? moveDir : inDir;
            if (alignRef.sqrMagnitude > 0.01f &&
                Vector3.Dot(wallTangent, alignRef) < 0f)
                wallTangent = -wallTangent;
            detDir = wallTangent;
        }
        else
        {
            detDir = moveDir != Vector3.zero
                ? moveDir
                : inDir != Vector3.zero
                ? inDir
                : Vector3.zero;
        }

        if (debugMode)
            Debug.Log("Direction-based wallrun handling, detDir = " + detDir);

        // 4) Sample the current wall
        (detectedWallPos, currentWallNormal) =
            DetectPerpendicular(detDir, transform.position);
        if (currentWallNormal == Vector3.zero)
        {
            if (debugMode)
                Debug.Log("Lost the wall entirely, ending wallrun early");
            StopWallRun();
            return;
        }

        // 5) Compute raw look-ahead distance using v0 * t + 0.5 * a * t^2
        float v0 = Vector3.Dot(flatVel, detDir);      // m/s along wall
        float t = lookAheadTime;                     // seconds to look ahead
        float rawLookDist = v0 * t + 0.5f * wallRunSpeedAcceleration * t * t;

        // 6) Clamp to [min, max]
        float lookDist = Mathf.Clamp(rawLookDist, minLookAheadDistance, maxLookAheadDistance);

        // 7) Sample the next wall at that future position
        Vector3 lookOrigin = transform.position + detDir * lookDist;
        (nextWallPos, nextWallNormal) =
            DetectPerpendicular(detDir, lookOrigin);

        if (debugMode)
        {
            Debug.DrawRay(transform.position, detDir * lookDist, Color.yellow);
            Debug.DrawRay(detectedWallPos, currentWallNormal * 2f, Color.cyan);
            Debug.DrawRay(nextWallPos, nextWallNormal * 2f, Color.magenta);
        }

        // 8) Compute run direction from wall geometry
        Vector3 currT = detectedWallPos + currentWallNormal * wallRunDistance;
        runDir = nextWallNormal == Vector3.zero
            ? ComputeSingleWallDir(currT)
            : ComputeDualWallDir(
                currT,
                nextWallPos + nextWallNormal * wallRunDistance
            );

        if (debugMode)
            Debug.DrawRay(transform.position, runDir.normalized * 2f, Color.green);

        // 9) Flatten and store the tangent
        runDir = new Vector3(runDir.x, 0f, runDir.z);
        currentWallTangent = runDir;

        // 10) Continue with movement logic
        UpdateVerticalVelocityWithSlip(verticalVelocityDampeningSpeed);
        CheckAndClampVelAtTopOfWall();
        ApplyAcceleration(runDir);
    }

    /*

    /// <summary>
    /// Called on a fixed update while on the wall
    /// </summary>
    private void ContinuedDetection()
    {
        // Build flat-XZ velocity and normalized input vector
        Vector3 flatVel = new Vector3(velocityHorizontal.x, 0f, velocityHorizontal.y);
        Vector3 moveDir = flatVel.sqrMagnitude > 0.01f
            ? flatVel.normalized
            : Vector3.zero;
        Vector3 inDir = inputDir.sqrMagnitude > 0f
            ? new Vector3(inputDir.x, 0f, inputDir.y).normalized
            : Vector3.zero;

        // Pick our detection dir: prefer movement, then input, else sticky
        Vector3 detDir = moveDir != Vector3.zero ? moveDir
                       : inDir != Vector3.zero ? inDir
                       : Vector3.zero;

        if (detDir == Vector3.zero)
        {
            if (debugMode)
                Debug.Log("Stick handling");

            // STICK: player isn’t moving or inputting, so just confirm we still hit the same wall
            Vector3 origin = transform.position;
            Vector3 inward = -currentWallNormal;
            if (Physics.Raycast(origin, inward, out var hit, wallCheckDistance, wallrunableLayers))
            {
                // still on the wall, keep the same normal/pos
                currentWallTangent = Vector3.Cross(Vector3.up, currentWallNormal).normalized;
                // nextWallNormal == currentWallNormal so curves skip
                nextWallNormal = currentWallNormal;
                nextWallPos = detectedWallPos;
            }
            else
            {
                if (debugMode)
                    Debug.Log("Stick handling: lost the wall");

                StopWallRun();
                //PassiveWallExit();
            }
            return;
        }

        // NORMAL DETECTION: -we have a direction-

        if (debugMode)
            Debug.Log("normal direction based wall run handling");

        (detectedWallPos, currentWallNormal) = DetectPerpendicular(detDir, transform.position);

        if (currentWallNormal != Vector3.zero)
        {
            Vector3 lookAheadPos = transform.position + detDir * lookAheadDistance;
            (nextWallPos, nextWallNormal) = DetectPerpendicular(detDir, lookAheadPos);
            lastDetectedWallNormal = currentWallNormal;
        }
        else
        {
            if (debugMode)
                Debug.Log("Lost the wall entirely, ending wallrun early");
            StopWallRun();
        }
    }
    */
    private void HandleWallRunContinuous()
    {
        wallRunElapsedTime += Time.fixedDeltaTime;

        // 1) Sample current wall at detectedWallPos
        Vector3 currT = detectedWallPos + currentWallNormal * wallRunDistance;
        DrawWallDebug(currT, currentWallNormal, Color.blue);

        // 2) Draw the current normal in cyan
        if (debugMode && currentWallNormal != Vector3.zero)
        {
            Debug.DrawRay(
                detectedWallPos,
                currentWallNormal * 5f,
                Color.cyan
            );
        }

        // 3) Sample runDir (and implicitly nextWallNormal inside ComputeDualWallDir)
        runDir = (nextWallNormal == Vector3.zero)
            ? ComputeSingleWallDir(currT)
            : ComputeDualWallDir(
                currT,
                nextWallPos + nextWallNormal * wallRunDistance
            );

        // 4) Draw the next normal in magenta
        if (debugMode && nextWallNormal != Vector3.zero)
        {
            Debug.DrawRay(
                nextWallPos,
                nextWallNormal * 5f,
                Color.magenta
            );
        }

        // 5) (Optional) draw your runDir to see the direction
        if (debugMode)
        {
            Debug.DrawRay(
                transform.position,
                runDir.normalized * 5f,
                Color.yellow
            );
        }

        // flatten runDir to ensure no vertical component
        runDir = new Vector3(runDir.x, 0f, runDir.z);
        currentWallTangent = runDir;

        UpdateVerticalVelocityWithSlip(verticalVelocityDampeningSpeed);
        CheckAndClampVelAtTopOfWall();
        ApplyAcceleration(runDir);

        //if (nextWallNormal != Vector3.zero)
         //   currentWallNormal = nextWallNormal;
    }

    private void PassiveWallExit()
    {
        OnWallRunExit?.Invoke();
        StopWallRun();
        ApplyExitForce(wallRunExitPassiveForce);
    }

    private void StopWallRun()
    {
        _wallRunProgress = 0;

        // remember which face we left
        if (debugMode)
            Debug.Log("Setting last wall normal to : " + currentWallNormal);

        if (currentWallNormal != Vector3.zero)
            lastDetectedWallNormal = currentWallNormal;

        // re-enable gravity
        pMovement.ToggleCustomGravity(true);
        pMovement.SetCustomGravityScale(1f);
        OnWallRunStop?.Invoke();

        // clear state
        currentWallNormal = Vector3.zero;
        isWallrunning = false;
        isStartingWallrun = false;
        isCornering = false;
        currentWallTangent = Vector3.zero;
        runDir = Vector3.zero;

        // kick off cooldown
        if (debugMode)
            Debug.Log("Setting regrab timer to true");
        isReGrabCooldownActive = true;
        reGrabTimer = reGrabDelay;

        // reset camera
        _resettingTilt = true;
        //cameraTransform.localRotation = Quaternion.Euler(0, cameraTransform.localEulerAngles.y, 0);

        cameraRotationManager.CancelRequest(RotateSource.Wallrun);
    }

    // ==== Helpers ====

    private Vector3 ComputeSingleWallDir(Vector3 currT)
    {
        Vector3 tangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        DrawDebugLine(tangent, 2f, Color.yellow, 0f);
        isCornering = false;
        return ChooseTangentSign(tangent);
    }

    private Vector3 ComputeDualWallDir(Vector3 currT, Vector3 nextT)
    {
        DrawWallDebug(nextT, nextWallNormal, Color.green);

        Debug.Log("[Wallrun Handler] is computing ComputeDualWallDir");

        float dist = Vector3.Distance(transform.position, currT);
        if (dist > intersectionPriorityDistanceThreshold)
            return (nextT - transform.position).normalized;

        Vector3 flatCurr = Flatten(currentWallNormal);
        Vector3 flatNext = Flatten(nextWallNormal);

        isCornering = false;

        if (Vector3.Dot(flatCurr, flatNext) > parallelNormalsDotThreshold)
        {
            Debug.Log("[Wallrun Handler] ComputeDualWallDir : parallelNormalsDotThreshold exceeded returning next");
            return (nextT - transform.position).normalized;
        }


        return ComputeCurveDir(currT, nextT);
    }

    private Vector3 ComputeCurveDir(Vector3 currT, Vector3 nextT)
    {

        Debug.Log("[Wallrun Handler] is computing curve dir");
        Vector3 curTan = Vector3.Cross(currentWallNormal, Vector3.up);
        Vector3 nexTan = Vector3.Cross(nextWallNormal, Vector3.up);

        MathExtensions.DebugDrawLineIntersection(
            currT, curTan,
            nextT, nexTan,
            Color.green, Color.magenta
        );

        Vector3? interPoint = MathExtensions.FindLineIntersection(currT, curTan, nextT, nexTan);
        Vector3 dir;


        if (interPoint.HasValue)
        {
            float distToInter = Vector3.Distance(transform.position, interPoint.Value);
            if (distToInter >= intersectionProximityThreshold)
            {
                // raw 3D dir
                Vector3 rawDir = (interPoint.Value - transform.position).normalized;

                // DEBUG: draw raw vs flat
                Debug.DrawRay(transform.position, rawDir * 2f, Color.red);

                // flatten out the vertical
                Vector3 flatDir = new Vector3(rawDir.x, 0f, rawDir.z).normalized;
                Debug.DrawRay(transform.position, flatDir * 2f, Color.green);

                dir = flatDir;

                // redirect momentum only along the flat direction
                RedirectMomentum(flatDir, 1);
            }
            else
            {
                Vector3 rawDir = (nextT - transform.position).normalized;
                dir = new Vector3(rawDir.x, 0f, rawDir.z).normalized;
            }
        }
        else
        {
            Vector3 rawDir = (nextT - transform.position).normalized;
            dir = new Vector3(rawDir.x, 0f, rawDir.z).normalized;
        }


        // === Calculate Curve Rotation ===

        Vector3 flatCurr = new Vector3(currentWallNormal.x, 0f, currentWallNormal.z).normalized;
        Vector3 flatNext = new Vector3(nextWallNormal.x, 0f, nextWallNormal.z).normalized;
        float cornerAngle = Vector3.SignedAngle(flatCurr, flatNext, Vector3.up);

        float chordLength = Vector3.Distance(
            detectedWallPos + currentWallNormal * wallRunDistance,
            nextWallPos + nextWallNormal * wallRunDistance
        );

        cornerTurnRate = (chordLength > 0f)
            ? cornerAngle / chordLength
            : 0f;

        Debug.Log("[Wallrun Handler] Setting cornering to true");
        isCornering = true;
        return dir;
    }

    private Vector3 ChooseTangentSign(Vector3 tangent)
    {
        Vector3 drive = new Vector3(inputDir.x, 0, inputDir.y);
        if (drive.sqrMagnitude > 0)
            return Vector3.Dot(tangent, drive) >= 0 ? tangent : -tangent;

        Vector3 flatVel = rb.velocity; flatVel.y = 0;
        return Vector3.Dot(tangent, flatVel) >= 0 ? tangent : -tangent;
    }

    private (Vector3 hitPos, Vector3 hitNormal) DetectPerpendicular(Vector3 dir, Vector3 origin)
    {
        // cast either side of dir
        Vector3 left = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 right = -left;

        var (normL, ptsL) = PerformWallCheck(origin, left, wallCheckDistance);
        var (normR, ptsR) = PerformWallCheck(origin, right, wallCheckDistance);

        if (ptsL.Count > ptsR.Count)
            return (GetAveragePosition(ptsL), normL);
        if (ptsR.Count > 0)
            return (GetAveragePosition(ptsR), normR);

        return (Vector3.zero, Vector3.zero);
    }

    private (Vector3, List<Vector3>) PerformWallCheck(Vector3 pos, Vector3 dir, float dist)
    {
        var hits = CustomRaycast.SquareRaycast(pos, dir, dist, squareCheckScale, wallrunableLayers, true, Color.yellow, Color.red, 0);
        Vector3 total = Vector3.zero;
        var pts = new List<Vector3>();
        foreach (var h in hits)
        {
            if (h.collider != null)
            {
                total += h.normal;
                pts.Add(h.point);
            }
        }
        return (total.normalized, pts);
    }

    [SerializeField]
    [Tooltip("0 = snap instantly to runDir, 1 = keep all sideways momentum")]
    private float accelVelocityRedirectBlend = 0.5f;

    private void ApplyAcceleration(Vector3 runDir)
    {
        // 1) split vertical / horizontal
        Vector3 vel = rb.velocity;
        Vector3 vert = Vector3.Project(vel, Vector3.up);
        Vector3 horiz = vel - vert;

        // 2) signed speed along runDir
        float speed = Vector3.Dot(horiz, runDir);

        // 3) friction
        float f = wallFriction * Time.fixedDeltaTime;
        if (Mathf.Abs(speed) > f) speed -= Mathf.Sign(speed) * f;
        else speed = 0f;

        // 4) input
        Vector3 worldIn = new Vector3(inputDir.x, 0f, inputDir.y);
        float inAlong = Vector3.Dot(worldIn, runDir);
        float a = wallRunSpeedAcceleration * Time.fixedDeltaTime * inAlong;

        if (inAlong > 0f)
        {
            if (speed < currentInputSpeedCap)
                speed = Mathf.Min(speed + a, currentInputSpeedCap);
        }
        else
        {
            speed += a;
        }

        // 5) build the “target” horizontal velocity along runDir
        Vector3 targetHoriz = runDir * speed;

        // 6) blend your old sideways velocity toward that target
        Vector3 newHoriz = Vector3.Lerp(horiz, targetHoriz, 1f - accelVelocityRedirectBlend);

        // 7) reassemble final velocity
        rb.velocity = newHoriz + vert;
    }
    /*
    private void ApplyAcceleration(Vector3 runDir)
    {
        // 1) split vertical/horizontal
        Vector3 vel = rb.velocity;
        Vector3 vert = Vector3.Project(vel, Vector3.up);
        Vector3 horiz = vel - vert;

        // 2) signed speed along runDir
        float speed = Vector3.Dot(horiz, runDir);

        // 3) friction
        float f = wallFriction * Time.fixedDeltaTime;
        if (Mathf.Abs(speed) > f) speed -= Mathf.Sign(speed) * f;
        else speed = 0f;

        // 4) input
        Vector3 worldIn = new Vector3(inputDir.x, 0f, inputDir.y);
        if (debugMode)
            Debug.DrawLine(transform.position, transform.position + worldIn * 2f, Color.yellow, Time.fixedDeltaTime);

        float inAlong = Vector3.Dot(worldIn, runDir);

        // 5) accelerate (only push *up* to cap, never pull down if above)
        float a = wallRunSpeedAcceleration * Time.fixedDeltaTime * inAlong;
        if (inAlong > 0f)
        {
            if (speed < currentInputSpeedCap)
                speed = Mathf.Min(speed + a, currentInputSpeedCap);
        }
        else
        {
            speed += a;
        }

        // 6) apply
        if (Mathf.Abs(runDir.y) > 0.01f)
            Debug.LogWarning($"[WallRun] runDir has Y component: {runDir}");

        rb.velocity = runDir * speed + vert;
        //HandleVerticalVelocity();
    }
    */
    /// <summary>
    /// Redirects the player’s existing horizontal momentum toward a new direction,
    /// blending between the old and new directions by a given scale.
    /// </summary>
    /// <param name="newDir">
    /// The target direction to redirect horizontal momentum toward.
    /// </param>
    /// <param name="redirectMomentumScale">
    /// A blend factor between 0 and 1:
    /// 0 = keep the original momentum direction entirely,
    /// 1 = fully redirect to <paramref name="newDir"/>.
    /// </param>

    private void RedirectMomentum(Vector3 newDir, float redirectMomentumScale)
    {
        // 1) split vertical vs horizontal
        Vector3 vel = rb.velocity;
        Vector3 vertical = Vector3.Project(vel, Vector3.up);
        Vector3 horizontal = vel - vertical;
        float mag = horizontal.magnitude;
        if (mag < 0.0001f)
            return;  // no horizontal motion to redirect

        // 2) compute old vs target unit directions
        Vector3 oldDir = horizontal.normalized;
        Vector3 targDir = newDir.normalized;

        // 3) blend them
        Vector3 blendedDir = Vector3.Lerp(oldDir, targDir, redirectMomentumScale).normalized;

        // 4) reapply same magnitude along blended direction
        rb.velocity = blendedDir * mag + vertical;
    }

    /// <summary>
    /// 1) Quickly damps any existing vertical velocity toward zero  
    /// 2) Applies extra downward acceleration scaled by wall-run progress
    /// </summary>
    private void HandleVerticalVelocity()
    {
        Vector3 vel = rb.velocity;

        // 1) remove existing Y-speed toward zero at up to dampSpeed units/sec
        float dampedY = Mathf.MoveTowards(
            vel.y,
            0f,
            verticalVelocityDampeningSpeed * Time.deltaTime
        );

        // 2) add downward accel based on how far through the run we are
        //    (progress 0 at start -> 1 at end)
        float downwardAccel = maxDownwardAcceleration * _wallRunProgress;
        float newY = dampedY - downwardAccel * Time.deltaTime;

        // 3) write it back
        rb.velocity = new Vector3(vel.x, newY, vel.z);
    }
    private void DampenDownwardVelocityOverTime(float dampingSpeed)
    {
        Vector3 vel = rb.velocity;

        if (vel.y < 0f)
        {
            float dampedY = Mathf.MoveTowards(vel.y, 0f, dampingSpeed * Time.fixedDeltaTime);
            rb.velocity = new Vector3(vel.x, dampedY, vel.z);
        }
    }

    private float wallRunElapsedTime;
    private void UpdateVerticalVelocityWithSlip(float dampingSpeed)
    {
        Vector3 vel = rb.velocity;

        // 1) Dampen the starting vertical velocity toward 0
        float dampedY = Mathf.MoveTowards(
            cachedInitialVerticalVelocity,
            0f,
            dampingSpeed * wallRunElapsedTime
        );

        // 2) Evaluate slip curve
        float t = Mathf.Clamp01(wallRunElapsedTime / maxWallRunDuration);
        float slipT = slipOverTimeCurve.Evaluate(t);
        float slipY = -maxSlipAcceleration * slipT;

        // 3) Combine
        float finalY = dampedY + slipY;

        // 4) Clamp to not exceed current Y if we’re applying downward force
        // This ensures we don’t “overwrite” natural downward motion
        if (finalY < vel.y)
            vel.y = finalY;

        rb.velocity = new Vector3(vel.x, vel.y, vel.z);
    }

    private void UpdateCooldown()
    {
        if (!isReGrabCooldownActive) return;
        reGrabTimer -= Time.fixedDeltaTime;
        if (reGrabTimer <= 0)
        {
            reGrabTimer = reGrabDelay;
            isReGrabCooldownActive = false;
        }

    }
    private void ResetCooldown()
    {
        isReGrabCooldownActive = false;
        reGrabTimer = reGrabDelay;
    }

    private bool CanRegrab(Vector3 newNormal)
    {
        // bandaid fix until mantling detection is added. Mantling/ Vaulting detection will bail out of wallrun and we wont need this check
        // without this check the player gets stuck on ledges while they try to jump over them
        // this is caused by the wall raycasts hitting the top of the ledge and returning upwards values.
        // which means these casts are always different enough from the face of the wall on the ledge
        // and always allowing the wallrun to start.  - zia 
        if (newNormal.y > 0.1f) return false;

        // If we're not on cooldown, always allow
        if (!isReGrabCooldownActive)
        {
            if (debugMode)
                Debug.Log("CanRegrab -> regrab cooldown inactive: allowing immediately");
            return true;
        }

        // Otherwise compute the similarity
        float dot = Vector3.Dot(lastDetectedWallNormal, newNormal);
        bool canRegrab = dot < parallelNormalsDotThreshold;

        if (debugMode)
            Debug.Log(
            $"CanRegrab -> cooldown ACTIVE\n" +
            $"  lastNormal: {lastDetectedWallNormal}\n" +
            $"  newNormal:  {newNormal}\n" +
            $"  dot:        {dot:F3}\n" +
            $"  threshold:  {parallelNormalsDotThreshold:F3}\n" +
            $"  result:     {(canRegrab ? "ALLOW" : "BLOCK")}"
        );

        return canRegrab;
    }

    private void ApplyExitForce(float force)
    {
        if (lastDetectedWallNormal != Vector3.zero)
            rb.AddForce(lastDetectedWallNormal * force, ForceMode.Impulse);
    }

    private Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0, v.z).normalized;

    public Vector3 GetAveragePosition(List<Vector3> positions)
    {
        if (positions.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var p in positions) sum += p;
        return sum / positions.Count;
    }

    private void WallRunFailsafeCheck()
    {
        return;
        // Define the number of rays (8 directions in a circle around the player)
        int numRays = 8;
        float angleStep = 360f / numRays; // Each ray is separated by an equal angle in the circle

        // Check each direction in the circle (flat plane)
        bool wallDetected = false;

        for (int i = 0; i < numRays; i++)
        {
            // Calculate the angle for the current ray
            float angle = i * angleStep;

            // Calculate the direction for the current ray (in the XZ plane, y=0)
            Vector3 direction = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle), 0, Mathf.Sin(Mathf.Deg2Rad * angle));

            // Cast the ray from the player's position in the current direction
            RaycastHit hit;
            if (Physics.Raycast(this.transform.position, direction, out hit, wallCheckDistance * 1.25f, wallrunableLayers))
            {
                // If the ray hits a valid wall, we consider it detected
                wallDetected = true;
                break; // Exit the loop early since we found a valid wall
            }
        }

        // If no wall was detected, stop the wallrun
        if (!wallDetected)
        {
            StopWallRun();
            Debug.Log("No wall detected, stopping wallrun.");
        }
    }

    private bool reachedTopOfWall = false;
    [SerializeField] private float lookAheadTime;

    /// <summary>
    /// Prevents the player from going above the top of the wall.
    /// </summary>
    private void CheckAndClampVelAtTopOfWall()
    {
        if (reachedTopOfWall)
            return;

        if (!HasWallAbove(currentWallNormal))
        {
            reachedTopOfWall = true;

            Vector3 vel = rb.velocity;
            if (vel.y > 0f)
                rb.velocity = new Vector3(vel.x, 0f, vel.z); // Stop upward momentum
            

            if (debugMode)
                Debug.Log("Reached top of wall — upward velocity clamped.");
            
        }
    }
    private bool HasWallAbove(Vector3 wallNormal)
    {
        Vector3 inward = -wallNormal.normalized;
        Vector3 headCenter = transform.position + Vector3.up * topOfWallCheckHeight * 0.9f;

        // Determine square offsets to apply around the head position
        Vector3 right = Vector3.Cross(Vector3.up, inward).normalized;
        if (right == Vector3.zero) right = Vector3.Cross(Vector3.right, inward).normalized;
        Vector3 up = Vector3.Cross(inward, right).normalized;

        Vector3 offset1 = (right + up).normalized * squareCheckScale;
        Vector3 offset2 = (-right + up).normalized * squareCheckScale;

        Vector3 origin1 = headCenter + offset1;
        Vector3 origin2 = headCenter + offset2;

        bool cast1 = Physics.Raycast(origin1, inward, out RaycastHit hit1, wallCheckDistance * 1.3f, wallrunableLayers);
        bool cast2 = Physics.Raycast(origin2, inward, out RaycastHit hit2, wallCheckDistance * 1.3f, wallrunableLayers);

        if (debugMode)
        {
            Debug.DrawRay(origin1, inward * wallCheckDistance, cast1 ? Color.green : Color.red, 0.5f);
            Debug.DrawRay(origin2, inward * wallCheckDistance, cast2 ? Color.green : Color.red, 0.5f);

            string log = $"[WallTopCheck]\n" +
                         $"- Head Center: {headCenter:F3}\n" +
                         $"- Wall Normal: {wallNormal.normalized:F3}\n" +
                         $"- Inward Dir:  {inward:F3}\n" +
                         $"- Offset1:     {offset1:F3} | Origin1: {origin1:F3} | Hit1: {(cast1 ? "yes hit" : "no hit")} {(cast1 ? $"-> {hit1.collider.name}" : "")}\n" +
                         $"- Offset2:     {offset2:F3} | Origin2: {origin2:F3} | Hit2: {(cast2 ? "yes hit" : "no hit")} {(cast2 ? $"-> {hit2.collider.name}" : "")}\n" +
                         $"- Result:      {(cast1 || cast2 ? "Wall Above Detected" : "No Wall Detected Above")}\n";

            //Debug.Log(log);
        }

        return cast1 || cast2;
    }

    private void HandleWallRunCancelling()
    {
        if (inputDir != Vector2.zero)
        {
            Vector3 worldIn = pMovement.InputDir3D;

            // Check if there's a valid detected wall normal
            if (currentWallNormal != Vector3.zero)
            {
                // Compare input direction with the detected wall normal
                float dotProduct = Vector3.Dot(worldIn.normalized, currentWallNormal);

                // If the dot product is too low, it means the player is pulling away from the wall (opposite direction)
                if (dotProduct > wallDetachInputThreshold) // adjust this threshold based on how sensitive you want the pull-off to be
                {
                    PassiveWallExit();  // Stop the wallrun if the input direction is too different from the wall normal
                    Debug.Log("Input direction is too different from wall normal, wallrun canceled");
                }
            }
        }
        if (_wallRunCancelledThisFrame)
        {
            PassiveWallExit();
            _wallRunCancelledThisFrame = false;
        }
    }
    /// <summary>
    /// Forces a passive wallrun exit at the end of the current frame
    /// </summary>
    public void ForceCancelWallRun()
    {
        _wallRunCancelledThisFrame = true;
    }

    private void DrawWallDebug(Vector3 pos, Vector3 normal, Color color)
    {
        DebugDrawExtensions.DrawRotatedCube(pos, Quaternion.LookRotation(normal, Vector3.up), Vector3.one * 0.5f, color, 0f);
        DebugDrawExtensions.DrawGridPlane(pos, normal, Vector2.one * 4f, color, 3, 0f);
    }

    private void DrawDebugLine(Vector3 dir, float len, Color col, float dur = 0f)
    {
        if (!debugMode) return;
        Debug.DrawLine(transform.position, transform.position + dir.normalized * len, col, dur);
    }
}
