using Generics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZiaPlayer
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]

    public class Zia_PlayerMovement : MonoBehaviour
    {
        #region Variables
        [SerializeField] private bool debugMode = false;

        [Header("References")]
        [SerializeField] private PlayerMain _player;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform cameraContainer;
        [SerializeField] private CapsuleCollider capsule;
        [SerializeField] private test_WallRunHandler wallRunHandler;
        public test_WallRunHandler WallRunHandler => wallRunHandler;
        public StateMachine _StateMachine { get; private set; }
        public Rigidbody Rb { get { return rb; } }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float airControlPercent = 0.2f;
        [SerializeField] private float groundFriction = 8f;
        private float currentMoveSpeed;

        [Header("Sprint Settings")]
        [SerializeField] private bool useToggleSprint = false;
        [SerializeField][Range(0, 1)] private float sprintSpeedThreshold = 0.1f;
        [SerializeField] private float sprintSpeedMultiplier = 1.35f;
        [SerializeField] private float sprintThreshold = 0.2f;
        [SerializeField] private bool sprintPressed;
        [SerializeField] private bool isSprintHeld = false;
        [SerializeField] private bool isSprinting = false;
        public bool IsSprintHeld => isSprintHeld;
        public float SprintSpeedMultiplier => sprintSpeedMultiplier;
        public float SprintSpeedThreshold => sprintSpeedThreshold;

        [Header("Camera Settings")]
        [SerializeField] private float lookSensitivity = 0.4f;
        [SerializeField] private float verticalLookLimit = 80f;
        [SerializeField] private float cameraHeightOffset = 0.1f;
        [SerializeField] private float crouchTransitionSmoothTime = 0.1f;
        private float heightVelocity;
        private Vector3 centerVelocity;
        private float cameraYVelocity;
        private float speedVelocity;
        private float pitch = 0f;

        [Header("Ground Check")]
        public Transform groundCheckPosition;
        public float groundCheckRadius = 0.3f;
        public LayerMask groundLayer;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField][Tooltip("Multiplier to control the effect of movement direction on the jump")] private float jumpDirectionMultiplier = 0.05f;
        [SerializeField][Tooltip("Multiplier to control the effect of movement direction on the jump")] private float minDirectionalJumpBoost = 1f;
        //[SerializeField, Range(0f, 1f)][Tooltip("How effective is jump boosting in the same direction as current velocity?: 1 = full boost, 0 = no boost")]
        //private float directionalInfluenceAlignmentWeight = 0.5f;
        [SerializeField][Tooltip("Time after leaving the ground where jump is still possible")] private float coyoteTime = 0.15f;
        [SerializeField][Tooltip("Time window to buffer the jump input")] private float jumpBufferTime = 0.1f;
        [SerializeField][Tooltip("Max number of jump charges a player can have")] private int maxJumpCharges = 2;
        [SerializeField][Tooltip("Time it takes to refill a jump charge when grounded")] private float jumpRechargeTime = 0.1f;
        [SerializeField][Tooltip("Length of time before transitioning to falling state")] private float jumpDuration = 0.35f;
        public float JumpDuration => jumpDuration;
        private float lastGroundedTime;
        private float jumpBufferedTime;
        private bool jumpBuffered;
        private int jumpCharges;
        private float lastJumpTime;
        public Vector3 InputDirectionWorld =>
        (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        public float JumpForce => jumpForce;
        public float JumpDirectionMultiplier => jumpDirectionMultiplier;
        public float MinDirectionalJumpBoost => minDirectionalJumpBoost;

        [Header("Crouch Settings")]
        [SerializeField][Tooltip("Height of the character collider when standing.")] private float standingHeight = 2f;
        [SerializeField][Tooltip("Height of the character collider when crouching.")] private float crouchingHeight = 1f;
        [SerializeField][Tooltip("Speed multiplier applied while crouching.")] private float crouchSpeedMultiplier = 0.5f;
        [SerializeField][Tooltip("Speed of the camera transition when crouching.")] private float crouchTransitionSpeed = 8f;
        [SerializeField][Tooltip("Local Y position of the camera when standing.")] private float standingCameraHeight = 0.9f;
        [SerializeField][Tooltip("Local Y position of the camera when crouching.")] private float crouchingCameraHeight = 0.45f;
        [SerializeField][Tooltip("How fast the character transitions between speeds")] private float crouchSpeedChangeTransitionRate = 0.2f;

        private bool isCrouching = false;
        public bool IsCrouching => isCrouching;
        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;

        [Header("Slide Settings")]
        [SerializeField, Tooltip("Boost applied to slide velocity on start.")] float slideStartBoost = 4f;
        [SerializeField, Tooltip("Maximum allowed slide speed at the start.")] float maxSlideStartSpeed = 14f;
        [SerializeField, Tooltip("Minimum speed required to keep sliding. Slide ends when velocity drops below this threshold.")] float slideEndSpeedThreshold = 1f;
        [SerializeField, Tooltip("Base friction applied while sliding on flat ground.")] float slideFrictionFlat = 8f;
        [SerializeField, Tooltip("Multiplier applied to friction based on slope steepness. Higher values increase slowdown when sliding uphill.")] float slideFrictionSlopeMultiplier = 1.5f;
        [SerializeField, Tooltip("Minimum speed required to initiate a slide (used in CanSlide check). Prevents micro-slides at low speeds.")] float minSlideSpeed = 6f;
        [SerializeField, Tooltip("Maximum slope angle allowed to initiate a slide. Prevents slides from triggering on steep walls or cliffs.")] float maxSlideSlopeAngle = 70f;
        [SerializeField, Tooltip("Additional friction applied when the player holds backward input during a slide.")] float extraBackFriction = 10f;
        [SerializeField, Tooltip("How strongly the player can curve their slide left or right using sideways input.")] float slideSteeringStrength = 4f;
        [SerializeField, Tooltip("Minimum allowed friction value. Prevents friction from being reduced too much on steep downslopes.")] float minSlideFriction = 1f;
        [SerializeField, Tooltip("Strength of the downhill acceleration force applied based on slope steepness.")] float slopeAccelerationMultiplier = 1.2f;
        [SerializeField, Tooltip("What percentage of falling velocity is redirected into the slope's direction when landing into a slide.")] float fallRedirectInfluence = 0.75f;
        [SerializeField, Tooltip("Horizontal boost applied when sliding off an edge")] private float slideEdgeBoostForce = 5f;
        [SerializeField, Tooltip("Vertical boost applied when sliding off an edge")] private float verticalEdgeBoostAmount = 1.5f;
        [SerializeField, Tooltip("Minimum horizontal speed required to recieve edge boost")] private float slideEdgeBoostSpeedThreshold = 3f;

        [Header("Vault Settings")]
        [SerializeField][Tooltip("Vertical offset added above the surface of the vaultable obstacle before starting the clearance check:")] private float vaultClearanceOffset = 0.1f;
        [SerializeField][Tooltip("Maximum distance forward to detect a vaultable obstacle")] private float vaultRange = 1f;
        [SerializeField][Tooltip("Height above the vault point to check for space to complete the vault")] private float vaultHeight = 1.2f;
        [SerializeField][Tooltip("Duration of the vaulting movement")] private float vaultDuration = 0.3f;
        [SerializeField, Range(0f, 1f)][Tooltip("Percentage of capsule height to use for chest-level raycast origin (e.g. 0.6 = 60% up)")] private float vaultChestHeightScale = 0.6f;
        [SerializeField, Range(0f, 1f)][Tooltip("Percentage of capsule height to use for head-level raycast origin (e.g. 0.9 = 90% up)")] private float vaultHeadHeightScale = 0.9f; 
        [SerializeField] private LayerMask vaultLayerMask;
        public float VaultRange => vaultRange;
        public float VaultHeight => vaultHeight;
        public float VaultDuration => vaultDuration;
        public LayerMask VaultLayerMask => vaultLayerMask;

        [Header("Gravity Settings")]
        [SerializeField][Tooltip("Whether to apply custom gravity manually")] private bool useCustomGravity = true;
        [SerializeField][Tooltip("Custom gravity strength applied per second (negative to pull down)")] private float gravityStrength = -10f;
        [SerializeField][Tooltip("Maximum downward speed allowed")] private float maxFallSpeed = -40f;
        private float currentGravityScale = 1;

        [Header("WallRun Settings")]
        [SerializeField] private bool wallRunEnabled = false;

        [Header("Movement Costs")]
        [SerializeField] private List<EnergyCostEntry> _costs;
        private Dictionary<MovementAction, EnergyCostEntry> _lookup;
        public bool WallRunEnabled => wallRunEnabled;

        private Vector2 moveInput;
        private Vector2 lookInput;
        public Vector2 MoveInput { get { return moveInput; } }

        // Movement States
        private PlayerState CurrentPlayerState => _StateMachine.CurrentState as PlayerState;
        public IdleState IdleState { get; private set; }
        public WalkState WalkState { get; private set; }
        public SprintState SprintState { get; private set; }
        public CrouchState CrouchState { get; private set; }
        public JumpState JumpState { get; private set; }
        public SlideState SlideState { get; private set; }
        public WallrunState WallrunState { get; private set; }
        public VaultState VaultState { get; private set; }
        public FallingState FallingState { get; private set; }

        // Bools
        private bool isGrounded;
        private bool wasGrounded;
        private bool isWallRunning;
        private bool isMovementEnabled = true;
        private bool _zeroMomentumThisFrame = false;
        public bool IsGrounded => isGrounded;
        public bool IsWallRunning => isWallRunning;

        #endregion

        #region MonoBehavior Methods
        public void Initialize()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (rb == null) rb = GetComponent<Rigidbody>();
            if (capsule == null) capsule = GetComponent<CapsuleCollider>();

            if (wallRunEnabled)
            {
                if (wallRunHandler == null) wallRunHandler = GetComponent<test_WallRunHandler>();
                if (wallRunHandler != null)
                {
                    wallRunHandler.enabled = true;
                    wallRunHandler.OnWallRunStart += HandleWallRunStart;
                    wallRunHandler.OnWallRunStop += HandleWallRunStop;
                }
            }

            if (useCustomGravity) rb.useGravity = false;
            else rb.useGravity = true;

            float loadedSensitivity = SettingsStorageController.Instance.LoadSliderValue("LookSensitivity");
            if (loadedSensitivity != 0f)
            {
                lookSensitivity = loadedSensitivity;
            }

            _lookup = _costs.ToDictionary(e => e.action);

            // Initialize State Machine

            _StateMachine = new StateMachine();
            IdleState = new IdleState(_StateMachine, this);
            WalkState = new WalkState(_StateMachine, this);
            SprintState = new SprintState(_StateMachine, this);
            CrouchState = new CrouchState(_StateMachine, this);
            JumpState = new JumpState(_StateMachine, this);
            WallrunState = new WallrunState(_StateMachine, this);
            VaultState = new VaultState(_StateMachine, this);
            SlideState = new SlideState(_StateMachine, this);
            FallingState = new FallingState(_StateMachine, this);

            // start in Idle
            _StateMachine.Initialize(IdleState);

            InitializeCrouchVisuals();
        }
        void OnDisable()
        {
            if (wallRunHandler != null)
            {
                wallRunHandler.OnWallRunStart -= HandleWallRunStart;
                wallRunHandler.OnWallRunStop -= HandleWallRunStop;
            }
        }

        void FixedUpdate()
        {
            ApplyCustomGravity();
            GroundCheck();
            HandleGroundedTransition();
            //CheckForVaultableObstacle();
            MaintainAirGap();

            _StateMachine.FixedTick();

            if (debugMode)
                Debug.Log($"player's current state : {CurrentPlayerState.Name}");

            /*
            if (wallRunEnabled)
            {
                wallRunHandler.UpdatePlayerInputDir(GetFlatInputDir());
                wallRunHandler.UpdateGroundedStatus(IsGrounded);
            }
            */

            if (_zeroMomentumThisFrame)
            {
                rb.velocity = Vector3.zero;
                //Debug.Log($"Momentum reset to zero, velocity: {rb.velocity}");
                _zeroMomentumThisFrame = false;
            }

            /*
            Debug.DrawLine(this.transform.position, this.transform.position + rb.velocity, Color.cyan);

            ApplyCustomGravity();
            GroundCheck();

            if (!isWallRunning)
            {
                ApplyMove();
            }

            JumpingLogic();
            SprintLogic();
            HandleCrouchTransition();

            if (wallRunEnabled)
            {
                //if (TryTickContinuous(MovementAction.WallRun, Time.fixedDeltaTime))
                wallRunHandler.UpdatePlayerInputDir(GetFlatInputDir());
                wallRunHandler.UpdateGroundedStatus(isGrounded);
            }

            if (_zeroMomentumThisFrame)
            {
                rb.velocity = Vector3.zero;
                Debug.Log($"pMovement recieved a call to reset momentum to zero, velocity is now : {rb.velocity}");
                _zeroMomentumThisFrame = false;
            }

            switch (_StateMachine.CurrentState)
            {
                default:
                    break;
            }
            */
        }

        private void Update()
        {
            ApplyLook();
            HandleCrouchVisuals();
            _StateMachine.Tick();
        }
        #endregion

        #region Update Methods
        void ApplyLook()
        {
            transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -verticalLookLimit, verticalLookLimit);
            cameraContainer.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        #endregion

        #region FixedUpdate Methods
        private void ApplyCustomGravity()
        {
            if (!useCustomGravity || isGrounded) return;

            // Apply gravity as acceleration (ForceMode.Acceleration makes it frame-rate independent)
            rb.AddForce(Vector3.up * gravityStrength * currentGravityScale, ForceMode.Acceleration);

            if (rb.velocity.y < maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, maxFallSpeed, rb.velocity.z);
            }
        }

        [SerializeField] float groundHoverDistance = 0.05f;
        [SerializeField] float hoverSnapSpeed = 20f;
        [SerializeField] float raycastLength = 1f;
        [SerializeField] float hoverRaycastSpacing = 0.25f;

        private readonly Vector3[] rayOffsets = new Vector3[]
        {
            new Vector3( 1, 0,  1),
            new Vector3(-1, 0,  1),
            new Vector3( 1, 0, -1),
            new Vector3(-1, 0, -1),
            Vector3.zero // center
        };

        /// <summary>
        /// Hovers the player character slightly above the ground to smooth motion over bumps and bad floor geometry.
        /// </summary>
        private void MaintainAirGap()
        {
            // Step 1: Calculate base world position of the capsule
            Vector3 capsuleCenterWorld = transform.TransformPoint(capsule.center);
            float capsuleBottomY = capsuleCenterWorld.y - (capsule.height / 2f) + capsule.radius;

            // Step 2: Calculate base ray origin position
            Vector3 baseRayOrigin = new Vector3(transform.position.x, capsuleBottomY + 0.05f, transform.position.z);

            Vector3 totalHitPoint = Vector3.zero;
            int hitCount = 0;

            foreach (var offset in rayOffsets)
            {
                Vector3 rayOffset = transform.TransformDirection(offset.normalized * hoverRaycastSpacing);
                Vector3 rayOrigin = baseRayOrigin + rayOffset;

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastLength, groundLayer))
                {
                    totalHitPoint += hit.point;
                    hitCount++;

                    if (debugMode)
                    Debug.DrawLine(rayOrigin, hit.point, Color.green);
                }
                else if(debugMode)
                {
                    Debug.DrawRay(rayOrigin, Vector3.down * raycastLength, Color.red);
                }
            }

            if (hitCount > 0)
            {
                Vector3 averageHitPoint = totalHitPoint / hitCount;
                float desiredY = averageHitPoint.y + groundHoverDistance;

                Vector3 position = transform.position;
                float smoothedY = Mathf.Lerp(position.y, desiredY, hoverSnapSpeed * Time.fixedDeltaTime);
                transform.position = new Vector3(position.x, smoothedY, position.z);
            }
        }

        private void HandleGroundedTransition()
        {
            if (!wasGrounded && isGrounded)
            {
                CurrentPlayerState?.OnGrounded();
            }
            else if (wasGrounded && !isGrounded)
            {
                CurrentPlayerState?.OnFallDetected();
            }

            wasGrounded = isGrounded;
        }

        #endregion

        #region Public Methods

        public void EnableMovement()
        {
            isMovementEnabled = true;
        }
        public void DisableMovement()
        {
            SetMomentumToZero();
        }
        public void SetMomentumToZero()
        {
            _zeroMomentumThisFrame = true;
        }
        public void ToggleCustomGravity(bool isActive)
        {
            useCustomGravity = isActive;
        }
        public void SetCustomGravityScale(float scale)
        {
            currentGravityScale = scale;
        }
        public void ResetCustomGravityScale()
        {
            currentGravityScale = 1;
        }

        #endregion

        #region Input Actions
        public void Move(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();

            if (moveInput != null )
            {
                CurrentPlayerState?.OnMoveInput();
            }
        }
        public void Look(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
                CurrentPlayerState?.OnJumpPressed();

            /*
            if (context.performed)
            {
                jumpBuffered = true; // Store the jump input when it's pressed
                jumpBufferedTime = Time.time; // Mark the time the jump was pressed
            }
            */
        }
        public void Crouch(InputAction.CallbackContext context)
        {
            if (context.performed)
                CurrentPlayerState?.OnCrouchPressed();

            /*
            if (!context.performed) return;
            
            if (_StateMachine.CurrentState == CrouchState)
            {
                if (LeaveCrouchCheck())
                {
                    _StateMachine.ChangeState(WalkState);
                    isCrouching = false;
                }
            }
            else
            {
                isCrouching = true;
                _StateMachine.ChangeState(CrouchState);
                // crouch logic
            }

            if (isWallRunning)
            {
                wallRunHandler.ForceCancelWallRun();
            }
            */
        }
        public void Sprint(InputAction.CallbackContext context)
        {
            if (context.performed && moveInput.y > sprintSpeedThreshold)
                CurrentPlayerState?.OnSprintPressed();
            
            /*
            if (!isGrounded) return;

            float forwardInput = moveInput.y;
            
            if (useToggleSprint)
            {
                if (context.performed && forwardInput > sprintSpeedThreshold)
                {
                    isSprinting = !isSprinting;
                }
            }
            else
            {
                if (context.started && forwardInput > sprintSpeedThreshold)
                    isSprintHeld = true;
                else if (context.canceled)
                    isSprintHeld = false;
            }

            bool shouldSprint = (useToggleSprint && isSprinting) || (!useToggleSprint && isSprintHeld && forwardInput > sprintSpeedThreshold);

            if (shouldSprint)
            {
                _StateMachine.ChangeState(SprintState);
            }
            else
            {
                _StateMachine.ChangeState(WalkState);
            }
            */
        }
        #endregion

        #region Action Executions

        #region Jump Logic
        public void SetJumpBuffer()
        {
            jumpBuffered = true; // Store the jump input when it's pressed
            jumpBufferedTime = Time.time; // Mark the time the jump was pressed
        }
        public void ConsumeJumpBuffer() => jumpBuffered = false;
        public void DecrementJumpCharge()
        {
            jumpCharges--;
            lastJumpTime = Time.time;
        }
        public void AttemptJump()
        {
            // This method gives players a slight window to queue a jump input before hitting the ground
            // and allows players to still jump very briefly after leaving the grounded state

            // Check if jump is buffered and the coyote time or jump buffered window is valid
            if (jumpBuffered && Time.time - lastGroundedTime <= coyoteTime && jumpCharges > 0)
            {
                ExecuteJump();
            }
            // Check if we can jump outside of coyote time, this allows a double jump in the air
            else if (jumpBuffered && jumpCharges > 0)
            {
                // Allow the player to jump even outside of coyote time if they have available jump charges
                ExecuteJump();
            }
            // Clear the jump buffer if time runs out
            else if (jumpBuffered && Time.time - jumpBufferedTime > jumpBufferTime)
            {
                jumpBuffered = false;
            }
        }
        public bool HasBufferedJump => jumpBuffered && Time.time - jumpBufferedTime <= jumpBufferTime;
        public void CheckForBufferedJump()
        {
            bool isGroundedNow = isGrounded;
            bool isWithinCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;
            bool isJumpStillBuffered = Time.time - jumpBufferedTime <= jumpBufferTime;

            // Valid jump: jump was buffered, still valid, and either grounded or in coyote time
            if (jumpBuffered && isJumpStillBuffered && (isGroundedNow || isWithinCoyoteTime))
            {
                ExecuteJump();

                // Only consume a jump charge if jumping from air (coyote time implies not grounded)
                if (!isGroundedNow && !isWithinCoyoteTime)
                {
                    jumpCharges--;
                }

                jumpBuffered = false;
            }
            // Allow jump if airborne with available jump charges (e.g., a double jump)
            else if (jumpBuffered && isJumpStillBuffered && jumpCharges > 0)
            {
                ExecuteJump();
                jumpCharges--; // Always consume charge for true midair jump
                jumpBuffered = false;
            }
            // Expired buffer
            else if (jumpBuffered && !isJumpStillBuffered)
            {
                jumpBuffered = false;
            }

            /*
            // This method gives players a slight window to queue a jump input before hitting the ground
            // and allows players to still jump very briefly after leaving the grounded state

            // Check if jump is buffered and the coyote time or jump buffered window is valid
            if (jumpBuffered && Time.time - lastGroundedTime <= coyoteTime && jumpCharges > 0)
            {
                ExecuteJump();
            }
            // Check if we can jump outside of coyote time, this allows a double jump in the air
            else if (jumpBuffered && jumpCharges > 0)
            {
                // Allow the player to jump even outside of coyote time if they have available jump charges
                ExecuteJump();
            }
            // Clear the jump buffer if time runs out
            else if (jumpBuffered && Time.time - jumpBufferedTime > jumpBufferTime)
            {
                jumpBuffered = false;
            }
            */
        }
        public void ExecuteJump()
        {
            Debug.Log("execute jump called");
            _StateMachine.ChangeState(JumpState);

            Vector3 jumpDirection = Vector3.up; // Default jump direction
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector3 horizontalInputDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            if (moveInput.magnitude > 0.1f)
            {
                // Check input alignment with current velocity
                float alignment = Vector3.Dot(horizontalVelocity.normalized, horizontalInputDir); // -1 to 1

                // Scale influence based on misalignment. 0 influence if aligned, full influence if perpendicular/opposite
                float redirectionFactor = 1f - Mathf.Clamp01(alignment); // 1 when opposite, 0 when aligned

                // Add scaled directional influence to jump
                Vector3 minJumpBoost = horizontalInputDir * minDirectionalJumpBoost;
                Vector3 directionalJump = (horizontalInputDir * redirectionFactor * jumpDirectionMultiplier) + minJumpBoost;
                jumpDirection += directionalJump;
            }

            // Reset vertical velocity before jump
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // Apply jump force
            rb.AddForce(jumpDirection.normalized * jumpForce, ForceMode.VelocityChange);

            if (wallRunHandler.IsWallrunning)
            {
                wallRunHandler.ActiveWallExit();
            }

            // Decrease the jump charges and update last jump time
            jumpCharges--;
            lastJumpTime = Time.time;

            jumpBuffered = false; // Clear the buffered jump after it's applied
        }
        #endregion

        #region Move Logic
        public Vector3 InputDir3D => (transform.right * moveInput.x + transform.forward* moveInput.y).normalized;
        public void ApplyMove()
        {
            Vector3 inputDir3D = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            Vector2 inputDir2D = new Vector2(inputDir3D.x, inputDir3D.z);

            Vector3 velocity = rb.velocity;
            Vector2 velocityHorizontal = new Vector2(velocity.x, velocity.z);

            float baseSpeed = moveSpeed;
            float multiplier = CurrentPlayerState?.GetSpeedMultiplier() ?? 1f;
            float alignment = 1f;

            if (isSprinting && velocityHorizontal.sqrMagnitude > 0.01f && inputDir2D.sqrMagnitude > 0.01f)
            {
                Vector2 velDir = velocityHorizontal.normalized;
                Vector2 inputDir = inputDir2D.normalized;
                alignment = Mathf.Clamp01(Vector2.Dot(velDir, inputDir));
            }

            float targetMaxSpeed = baseSpeed * multiplier * moveInput.magnitude * alignment;



            float acceleration = targetMaxSpeed * 10f;

            // Apply friction only when grounded and not in crouch/slide/etc.
            if (isGrounded && ShouldApplyGroundFriction())
            {
                ApplyGroundFriction(ref velocityHorizontal);
            }

            float currentSpeedInDir = Vector2.Dot(velocityHorizontal, inputDir2D);
            float remainingSpeed = Mathf.Clamp(targetMaxSpeed - currentSpeedInDir, 0f, acceleration * Time.deltaTime);
            Vector2 newVelocityHorizontal = velocityHorizontal + inputDir2D * remainingSpeed;

            float controlMultiplier = isGrounded ? 1f : airControlPercent;
            newVelocityHorizontal = Vector2.Lerp(velocityHorizontal, newVelocityHorizontal, controlMultiplier);

            rb.velocity = new Vector3(newVelocityHorizontal.x, velocity.y, newVelocityHorizontal.y);

            /*
            // Convert input to world direction and flatten it
            inputDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;

            // Get horizontal velocity
            Vector3 velocity = rb.velocity;
            Vector2 velocityHorizontal = new Vector2(velocity.x, velocity.z);

            // Change this garbage later - zia
            float curSpeed = moveSpeed;
            if (_StateMachine.CurrentState == SprintState)
            {
                curSpeed = moveSpeed * sprintSpeedMultiplier;
            }
            else if (_StateMachine.CurrentState == CrouchState)
            {
                curSpeed = moveSpeed * crouchSpeedMultiplier;
            }

            // Set target max speed based on input magnitude
            float targetMaxSpeed = curSpeed * moveInput.magnitude; // Scale speed based on how far the stick is pressed
            float acceleration = targetMaxSpeed * 10f;

            // Friction (only on ground) - Bootleg temporary slide 
            if (isGrounded && _StateMachine.CurrentState != CrouchState)
            {
                if (velocityHorizontal.magnitude > 0.1f)
                {
                    Vector2 frictionForce = -velocityHorizontal.normalized * groundFriction * Time.deltaTime;
                    velocityHorizontal += frictionForce;

                    // Clamp to zero if small enough
                    if (velocityHorizontal.magnitude < 0.5f)
                        velocityHorizontal = Vector2.zero;
                }
            }

            // Acceleration toward input direction
            Vector2 inputDirHorizontal = new Vector2(inputDir.x, inputDir.z);
            float currentSpeedInDir = Vector2.Dot(velocityHorizontal, inputDirHorizontal);
            float remainingSpeed = Mathf.Clamp(targetMaxSpeed - currentSpeedInDir, 0f, acceleration * Time.deltaTime);
            Vector2 newVelocityHorizontal = velocityHorizontal + inputDirHorizontal * remainingSpeed;

            // Reduce control in air
            float controlMultiplier = isGrounded ? 1f : airControlPercent;
            newVelocityHorizontal = Vector2.Lerp(velocityHorizontal, newVelocityHorizontal, controlMultiplier);

            // Apply new velocity to Rigidbody
            rb.velocity = new Vector3(newVelocityHorizontal.x, velocity.y, newVelocityHorizontal.y);
            */
        }
        private bool ShouldApplyGroundFriction()
        {
            return _StateMachine.CurrentState != SlideState;
        }
        public void SetSprinting(bool isSprinting)
        {
            this.isSprinting = isSprinting;
        }
        public void ApplyGroundFriction(ref Vector2 velocityHorizontal)
        {
            if (velocityHorizontal.magnitude > 0.1f)
            {
                Vector2 frictionForce = -velocityHorizontal.normalized * groundFriction * Time.deltaTime;
                velocityHorizontal += frictionForce;

                if (velocityHorizontal.magnitude < 0.5f)
                    velocityHorizontal = Vector2.zero;
            }
        }

        #endregion

        #region Crouch Logic

        private bool shouldBeCrouching = false;
        private bool isAtCrouchTarget = true;
        private enum CrouchAnchor { Feet, Head }
        private CrouchAnchor currentCrouchAnchor = CrouchAnchor.Feet;

        private void InitializeCrouchVisuals()
        {
            Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * capsule.height / 2f;
            groundCheckPosition.position = capsuleBottom;
        }
        public void SetCrouchVisuals(bool crouching, bool airborne = false)
        {
            shouldBeCrouching = crouching;
            isAtCrouchTarget = false;
            currentCrouchAnchor = airborne ? CrouchAnchor.Head : CrouchAnchor.Feet;
        }

        private void HandleCrouchVisuals()
        {
            // Handle edge cases where the player gets stuck in a crouched state.
            if (!shouldBeCrouching && capsule.height < standingHeight * 0.8f)
            {
                Debug.LogWarning("[CrouchVisuals] Forcing uncrouch recovery.");
                isAtCrouchTarget = false; // forcibly re-run uncrouch visuals
            }

            if (isAtCrouchTarget) return;

            float desiredHeight = shouldBeCrouching ? crouchingHeight : standingHeight;

            Vector3 desiredCenter;
            if (currentCrouchAnchor == CrouchAnchor.Feet)
            {
                // Top comes down
                desiredCenter = Vector3.down * (standingHeight - desiredHeight) / 2f;
            }
            else
            {
                // Bottom comes up
                desiredCenter = Vector3.up * (standingHeight - desiredHeight) / 2f;
            }

            capsule.height = Mathf.SmoothDamp(capsule.height, desiredHeight, ref heightVelocity, crouchTransitionSmoothTime, Mathf.Infinity, Time.deltaTime);
            capsule.center = Vector3.SmoothDamp(capsule.center, desiredCenter, ref centerVelocity, crouchTransitionSmoothTime, Mathf.Infinity, Time.deltaTime);

            // Smooth camera height
            float cameraTargetY = capsule.center.y + capsule.height / 2f - cameraHeightOffset;
            Vector3 localPos = cameraContainer.localPosition;
            localPos.y = Mathf.SmoothDamp(localPos.y, cameraTargetY, ref cameraYVelocity, crouchTransitionSmoothTime, Mathf.Infinity, Time.deltaTime);
            cameraContainer.localPosition = localPos;

            // Smooth movement speed
            float targetSpeed = shouldBeCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            currentMoveSpeed = Mathf.SmoothDamp(currentMoveSpeed, targetSpeed, ref speedVelocity, crouchTransitionSmoothTime, Mathf.Infinity, Time.deltaTime);

            // Done?
            const float threshold = 0.01f;
            bool heightDone = Mathf.Abs(capsule.height - desiredHeight) < threshold;
            bool centerDone = Vector3.Distance(capsule.center, desiredCenter) < threshold;
            bool camDone = Mathf.Abs(localPos.y - cameraTargetY) < threshold;

            if (heightDone && centerDone && camDone)
            {
                isAtCrouchTarget = true;
            }

            // Always update feet position
            Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * capsule.height / 2f;
            groundCheckPosition.position = capsuleBottom;
        }
        private void HandleCrouchTransition()
        {
            // Target capsule height and center
            float desiredHeight = _StateMachine.CurrentState == CrouchState ? crouchingHeight : standingHeight;
            Vector3 desiredCenter = new Vector3(0f, desiredHeight / 2f, 0f);

            // Smooth capsule height and center
            capsule.height = Mathf.SmoothDamp(capsule.height, desiredHeight, ref heightVelocity, crouchTransitionSmoothTime * Time.fixedDeltaTime);
            capsule.center = Vector3.SmoothDamp(capsule.center, desiredCenter, ref centerVelocity, crouchTransitionSmoothTime * Time.fixedDeltaTime);

            // Camera target Y position
            float cameraTargetY = capsule.center.y + capsule.height / 2f - cameraHeightOffset;
            Vector3 localPos = cameraContainer.transform.localPosition;
            localPos.y = Mathf.SmoothDamp(localPos.y, cameraTargetY, ref cameraYVelocity, crouchTransitionSmoothTime * Time.fixedDeltaTime);
            cameraContainer.transform.localPosition = localPos;

            // Smooth movement speed transition
            float targetSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;
            currentMoveSpeed = Mathf.SmoothDamp(currentMoveSpeed, targetSpeed, ref speedVelocity, crouchTransitionSmoothTime * Time.fixedDeltaTime);
        }

        #endregion

        #region Slide Logic

        [SerializeField] private float minRampSlideAngle = 15f; // tweakable
        public bool CanSlide()
        {
            if (!isGrounded)
                return false;

            Vector3 velocity = rb.velocity;
            Vector3 groundNormal = GetGroundNormal();
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            if (slopeAngle > maxSlideSlopeAngle)
                return false; // Prevent sliding on walls/cliffs

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;

            // Case 1: Standard slide while moving
            if (horizontalSpeed >= minSlideSpeed)
                return true;

            // Case 2: Allow slide if recently landed on a steep slope
            bool recentlyJumpedOrFell = !wasGrounded || velocity.y < -0.1f;
            bool steepRamp = slopeAngle >= minRampSlideAngle;

            if (recentlyJumpedOrFell && steepRamp)
                return true;

            return false;
        }

        private Vector3 slideVelocity;

        public void BeginSlide()
        {
            Vector3 currentVelocity = rb.velocity;
            Vector3 groundNormal = GetGroundNormal();
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            // Horizontal velocity and direction
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            Vector3 slideDir = horizontalVelocity.normalized;
            if (slideDir.sqrMagnitude < 0.01f)
                slideDir = transform.forward;

            // Align to slope
            slideDir = Vector3.ProjectOnPlane(slideDir, groundNormal).normalized;

            // -- New logic: only redirect fall if moving with slope or standing still --
            Vector3 slopeDownDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
            float alignment = Vector3.Dot(horizontalVelocity.normalized, slopeDownDir); // -1 to 1

            bool allowFallRedirect = currentSpeed < 0.1f || alignment > 0.25f;

            if (allowFallRedirect)
            {
                Vector3 redirectedFall = Vector3.Project(currentVelocity, -groundNormal);
                if (redirectedFall.sqrMagnitude > 0.01f)
                {
                    Vector3 fallDir = Vector3.ProjectOnPlane(redirectedFall.normalized, groundNormal).normalized;
                    slideDir = (slideDir + fallDir * fallRedirectInfluence).normalized;
                }
            }

            // Calculate boost factor
            float speedFactor = Mathf.InverseLerp(0f, minSlideSpeed, currentSpeed);
            float slopeFactor = Mathf.InverseLerp(0f, 45f, slopeAngle);
            float boostMultiplier = Mathf.Max(speedFactor, slopeFactor);

            float scaledBoost = slideStartBoost * boostMultiplier;

            float maxAllowedBoost = Mathf.Max(0f, maxSlideStartSpeed - currentSpeed);
            float actualBoost = Mathf.Min(scaledBoost, maxAllowedBoost);

            float finalSpeed = currentSpeed + actualBoost;
            slideVelocity = slideDir * finalSpeed;

            Debug.Log($"Slide started | Speed: {currentSpeed:F2}, Boost: {actualBoost:F2}, Final: {finalSpeed:F2}, Slope: {slopeAngle:F2}, Alignment: {alignment:F2}");
        }
        public void ApplySlideMovement()
        {
            Debug.Log("Applying Slide Movement");
            // -- 1. Calculate slope-influenced friction --
            Vector3 groundNormal = GetGroundNormal();
            bool hasValidGround = groundNormal != Vector3.zero;

            if (!hasValidGround)
            {
                // Treat as flat ground if no floor is found
                groundNormal = Vector3.up;
            }

            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);

            // Slope direction (down the ramp)
            Vector3 slopeDownDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;

            // Dot between slide direction and downhill direction
            float alignmentWithSlope = Vector3.Dot(slideVelocity.normalized, slopeDownDir) * -1; // 1 = downhill, -1 = uphill

            // Apply slope influence with correct sign
            float slopeInfluence = alignmentWithSlope * slopeAngle;

            // Final friction value
            float friction = slideFrictionFlat + (slopeInfluence * slideFrictionSlopeMultiplier);
            friction = Mathf.Max(minSlideFriction, friction); // optional clamp

            // -- 2. Sample input and project it relative to slide direction --
            Vector3 inputWorld = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            Vector3 slideDir = slideVelocity.normalized;

            // Forward/back influence
            float alignment = Vector3.Dot(inputWorld, slideDir); // -1 = back, 0 = perpendicular, 1 = forward

            // Side-steering influence
            Vector3 steeringDir = Vector3.ProjectOnPlane(inputWorld, Vector3.up);
            Vector3 perpendicularToSlide = Vector3.Cross(Vector3.up, slideDir);
            float steerAmount = Vector3.Dot(steeringDir, perpendicularToSlide); // -1 to 1

            if (alignment < -0.2f) // Pressing mostly backwards
            {
                float backFrictionBonus = Mathf.Lerp(0f, extraBackFriction, -alignment); // stronger if pressing directly back
                friction += backFrictionBonus;
            }

            // -- 4. Apply sideways steering --
            if (Mathf.Abs(steerAmount) > 0.1f)
            {
                Vector3 sideAdjust = perpendicularToSlide * steerAmount * slideSteeringStrength * Time.fixedDeltaTime;
                slideVelocity += sideAdjust;
            }

            // -- 5. Apply friction --
            if (hasValidGround)
            {
                float frictionAmount = friction * Time.fixedDeltaTime;
                Vector3 decel = slideVelocity.normalized * frictionAmount;
                if (decel.magnitude > slideVelocity.magnitude)
                    decel = slideVelocity;
                slideVelocity -= decel;
            }
            else
            {
                slideVelocity.y += (gravityStrength * currentGravityScale);
            }

            // -- 5b. Apply slope boost (downhill force) --
            if (hasValidGround && slopeAngle > 0.1f) // only boost when actually on slope
            {
                Vector3 slopeBoostDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;

                float downDot = Vector3.Dot(slopeBoostDir, slideVelocity.normalized);
                if (downDot > 0f)
                {
                    float boostStrength = slopeAngle * slopeAccelerationMultiplier;
                    Vector3 slopeForce = slopeBoostDir * boostStrength * Time.fixedDeltaTime;
                    slideVelocity += slopeForce;
                }
            }

            // -- 6. Check for slide exit --
            if (slideVelocity.magnitude < slideEndSpeedThreshold)
            {
                if (CurrentPlayerState is SlideState slideState)
                {
                    slideState.ExitSlide();
                }
                return;
            }

            // -- 7. Apply final velocity --
            Vector3 slopeAlignedVelocity = hasValidGround
                ? Vector3.ProjectOnPlane(slideVelocity, groundNormal)
                : slideVelocity; // midair: keep natural velocity

            if (hasValidGround)
            {
                slopeAlignedVelocity.y = Mathf.Min(slopeAlignedVelocity.y, 0f);
            }

            rb.velocity = slopeAlignedVelocity;
        }

        /// <summary>
        /// Applies a forward boost of momentum when the player slides off a ledge
        /// </summary>
        public void ApplySlideEdgeBoost()
        {
            if (slideVelocity.magnitude < slideEdgeBoostSpeedThreshold)
                return;

            Vector3 boostDir = slideVelocity.normalized;
            slideVelocity += boostDir * slideEdgeBoostForce;

            // Optional upward nudge to make it feel snappier
            slideVelocity.y += verticalEdgeBoostAmount;

            rb.velocity = slideVelocity;
            Debug.Log("Applied slide edge boost!");
        }

        #endregion

        #endregion

        #region Physics Checks
        void GroundCheck()
        {
            isGrounded = Physics.CheckSphere(groundCheckPosition.position, groundCheckRadius, groundLayer);

            // Replenish jump charges when grounded
            if (isGrounded)
            {
                lastGroundedTime = Time.time;

                // Replenish jump charges if the cooldown has passed
                if (Time.time - lastJumpTime >= jumpRechargeTime && jumpCharges < maxJumpCharges)
                {
                    jumpCharges = maxJumpCharges;
                }
            }
        }

        public Vector3 GetGroundNormal()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, capsule.bounds.extents.y + 5, groundLayer))
                return hit.normal;
            return Vector3.up; // fallback
        }

        public bool LeaveCrouchCheck()
        {
            float headClearance = standingHeight - crouchingHeight;
            float radius = capsule.radius * Mathf.Max(transform.localScale.x, transform.localScale.z); // account for scaling
            Vector3 origin = transform.position + Vector3.up * (crouchingHeight + radius); // start slightly above crouch height

            return !Physics.SphereCast(origin, radius, Vector3.up, out _, headClearance, groundLayer, QueryTriggerInteraction.Ignore);
        }

        public void CheckForVaultableObstacle()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;

            float height = capsule.height * transform.localScale.y;

            float chestOffset = height * vaultChestHeightScale;
            float headOffset = height * vaultHeadHeightScale;

            Vector3 chestRayOrigin = origin + Vector3.up * chestOffset;
            Vector3 headRayOrigin = origin + Vector3.up * headOffset;

            bool chestHit = Physics.Raycast(chestRayOrigin, direction, out RaycastHit chestHitInfo, vaultRange, vaultLayerMask, QueryTriggerInteraction.Ignore);
            bool headHit = Physics.Raycast(headRayOrigin, direction, vaultRange, vaultLayerMask, QueryTriggerInteraction.Ignore);

            if (chestHit && !headHit)
            {
                // Obstacle is low enough to vault
                Vector3 clearanceCheckStart = chestHitInfo.point + Vector3.up * vaultClearanceOffset;

                Vector3 clearanceTop = clearanceCheckStart + Vector3.up * vaultHeight;

                float capsuleRadius = capsule.radius * 0.9f;
                bool hasClearance = !Physics.CheckCapsule(clearanceCheckStart, clearanceTop, capsuleRadius, vaultLayerMask, QueryTriggerInteraction.Ignore);

                // Prepare and enter vault state
                if (hasClearance)
                {
                    Vector3 vaultTarget = chestHitInfo.point + Vector3.up * vaultHeight;
                    VaultState.BeginVault(vaultTarget);
                    CurrentPlayerState?.OnVaultDetected();
                }
            }
        }

        private void HandleWallRunStart()
        {
            Debug.Log("Pmovement detected: started wall running!");
            isWallRunning = true;
            //_StateMachine.ChangeState(WallrunState);
            CurrentPlayerState?.OnWallDetected();
            jumpCharges = maxJumpCharges;
        }

        private void HandleWallRunStop()
        {
            Debug.Log("Pmovement detected: stopped wall running.");
            isWallRunning = false;
        }

        public Vector2 GetFlatInputDir()
        {
            // Raw stick
            Vector2 raw = moveInput;
            if (raw.sqrMagnitude < 0.01f)
                return Vector2.zero;

            // Camera axes, flattened
            Vector3 f = cameraContainer.transform.forward;
            Vector3 r = cameraContainer.transform.right;
            f.y = 0; f.Normalize();
            r.y = 0; r.Normalize();

            // Build a world-space 3D vector
            Vector3 world = (r * raw.x + f * raw.y).normalized;

            // Store only XZ into your handler
            return new Vector2(world.x, world.z);
        }

        #endregion

        #region Movement Energy Handling

        /// <summary>
        /// One-off cost (e.g. double-jump, dash). Returns true if you have enough energy.
        /// </summary>
        public bool TryUse(MovementAction action)
        {
            if (!_lookup.TryGetValue(action, out var entry))
                return false;

            _player.Energy.Modify(entry.costOnUse);
            return true;
        }

        /// <summary>
        /// Continuous cost (e.g. wall-run, sprint). Returns false when you run out.
        /// </summary>
        public bool TryTickContinuous(MovementAction action, float dt)
        {
            if (!_lookup.TryGetValue(action, out var entry))
                return false;

            float costThisFrame = entry.costPerSecond * dt;
            _player.Energy.Modify(costThisFrame);

            return true;
        }
        #endregion

        #region Gizmos
        void OnDrawGizmosSelected()
        {
            if (groundCheckPosition != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPosition.position, groundCheckRadius);
            }
        }
        #endregion
    }

    [System.Serializable]
    public struct EnergyCostEntry
    {
        public MovementAction action;
        [Tooltip("One-off cost when you trigger the action.")]
        public float costOnUse;
        [Tooltip("Continuous cost per second while active.")]
        public float costPerSecond;
    }

    public enum MovementAction
    {
        DoubleJump,
        WallRun,
        Dash,
        Grapple
        // Add any movement actions which have an an energy cost here.
    }
}
