using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System;

namespace Items
{
    /// <summary>
    /// Manages dynamic equipping and firing based on WeaponDefinition.
    /// Equip new definitions via public Equip(...).
    /// </summary>
    [RequireComponent(typeof(PhotonView), typeof(Rigidbody), (typeof(EquippedWeaponDisplayManager)))]
    public class WeaponController : MonoBehaviour
    {
        public bool FireRequested { get; set; }

        [Header("References")]
        [SerializeField] private Transform firePoint;

        private PlayerEventBus playerEventBus;
        private WeaponContext _context;
        private WeaponStateMachine _stateMachine;
        private EquippedWeapon _activeWeapon;
        private WeaponDefinition _activeWeaponDef;

        private EquippedWeaponDisplayManager _displayManager;
        public EquippedWeaponDisplayManager Display => _displayManager;

        // Cache for each inventory slot: slotIndex -> EquippedWeapon
        private Dictionary<int, EquippedWeapon> _slotCache = new Dictionary<int, EquippedWeapon>();

        private Coroutine _burstCoroutine;
        private Coroutine _chargeCoroutine;

        private bool _isFireHeld = false;
        public bool FireHeld => _isFireHeld;

        private bool _altHeld;
        private float curInaccuracyScale;

        public bool AltHeld => _altHeld;

        
        private void SetCurrentInaccuracyScale(float scale)
        {
            // ignore negative input
            if (scale < 0)
            {
                Debug.LogError("[WeaponController][SetCurrentAccuracyScale] Recieved a negative accuracy scale input. Ignoring");
                return;
            }

            curInaccuracyScale = scale;
            _activeWeapon.Context.CurrentInaccuracyModifier = scale;
        }
        void Awake()
        {
            // 1) Build weapon context (Definition will be set on first EquipSlot)
            _context = new WeaponContext
            {
                Definition = null,
                Owner = this.gameObject,
                PlayerEventBus = GetComponent<PlayerMain>().PlayerEventBus,
                FirePoint = firePoint,
                PhotonView = GetComponent<PhotonView>(),
                OwnerRigidbody = GetComponent<Rigidbody>(),
                WeaponController = this
            };

            _displayManager = GetComponent<EquippedWeaponDisplayManager>();
            playerEventBus = GetComponent<PlayerEventBus>();

            // NO state-machine construction here—it needs a non-null Definition
        }

        public void Enable()
        {
            playerEventBus.Subscribe<PlayerStateChangedEvent>(OnStateChanged);
            playerEventBus.Subscribe<PlayerStateStayEvent>(OnStateStay);
        }

        public void Disable()
        {
            playerEventBus.Unsubscribe<PlayerStateChangedEvent>(OnStateChanged);
            playerEventBus.Unsubscribe<PlayerStateStayEvent>(OnStateStay);

            _altHeld = false;
            curInaccuracyScale = 1;

            CancelBurst();
            CancelCharge();
        }

        private void OnStateChanged(PlayerStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case AirborneState :
                    OnAirborneEntered();
                    break;

                case GroundedState :
                    OnGroundedEntered();
                    break;

                default:
                    // other states
                    break;
            }
        }

        private void OnGroundedEntered()
        {
            SetCurrentInaccuracyScale(_activeWeaponDef.groundedAccuracyScale);
        }

        private void OnAirborneEntered()
        {
            SetCurrentInaccuracyScale(_activeWeaponDef.airborneAccuracyScale);
        }

        private void OnStateStay(PlayerStateStayEvent evt) { }

        void Update()
        {
            // 1) Raise FireRequested either on input-start or when holding an automatic weapon
            if (_isFireHeld && _activeWeaponDef.isAutomatic)
            {
                FireRequested = true;
            }

            // 2) Decide once whether we should fire this frame
            bool shouldFire = FireRequested;
            if (shouldFire)
            {
                _activeWeapon.RequestFire();
            }

            // 3) Let the state machine see that flag in its Idle.Tick
            _activeWeapon.Update();

            if (_altHeld) _activeWeapon._altBehavior?.Tick(_activeWeapon.Context, Time.deltaTime);

            // 4) Clear it so we only fire again on the next input or auto-hold
            FireRequested = false;
        }

        void FixedUpdate()
        {
            _activeWeapon?.FixedUpdate();
        }

        /// <summary>
        /// Equip a weapon into the given slot index (0,1 for primary, 2 for temp).
        /// Reuses the cached EquippedWeapon for that slot if available.
        /// </summary>
        public void EquipSlot(int slotIndex, WeaponDefinition def)
        {
            if (def == null)
            {
                Debug.LogWarning($"WeaponController.EquipSlot called with null definition for slot {slotIndex}");
                return;
            }

            // 1) Cancel any in-flight burst/charge
            CancelBurst();
            CancelCharge();
            curInaccuracyScale = 1;

            // 2) Update the shared context
            _context.Definition = def;

            // 3) Check cache: remove mismatched existing
            _slotCache.TryGetValue(slotIndex, out var existing);
            if (existing != null && existing.Context.Definition != def)
            {
                _slotCache.Remove(slotIndex);
                existing = null;
            }

            // 4) Determine fire point via view manager
            Transform targetFirePoint = firePoint != null ? firePoint : transform;
            var viewGO = _displayManager?.ShowWeapon(def);
            var wv = viewGO?.GetComponent<PoolableViewModel>();
            if (wv != null)
            {
                targetFirePoint = wv.FirePoint;
            }

            // 5) Instantiate or reuse EquippedWeapon
            if (existing == null)
            {
                existing = new EquippedWeapon(def, gameObject, targetFirePoint);
                _slotCache[slotIndex] = existing;
            }
            else
            {
                existing.Context.FirePoint = targetFirePoint;
            }

            // 6) Switch active weapon
            _activeWeapon = existing;
            _activeWeaponDef = def;

            existing.Context.LastFireTimestamp = Time.time;

            // 7) Mark primary/off-hand based on slots
            if (_slotCache.TryGetValue(0, out var primary))
                primary.Context.isPrimary = true;
            if (_slotCache.TryGetValue(1, out var offhand))
                offhand.Context.isPrimary = false;

            // 8) Raise swap event only when both primary and offhand are assigned
            if (_slotCache.TryGetValue(0, out primary) && _slotCache.TryGetValue(1, out offhand))
            {
                playerEventBus.Raise(new WeaponSwappedEvent
                {
                    NewPrimary = primary.Context,
                    NewOffhand = offhand.Context
                });
            }
        }
        #region Burst Logic
        public void StartBurst(IWeaponFireBehavior behavior, WeaponContext ctx, int count, float delay)
        {
            if (_burstCoroutine != null)
                StopCoroutine(_burstCoroutine);
            _burstCoroutine = StartCoroutine(FireBurstRoutine(behavior, ctx, count, delay));
        }

        private IEnumerator FireBurstRoutine(IWeaponFireBehavior behavior, WeaponContext ctx, int count, float delay)
        {
            for (int i = 0; i < count; i++)
            {
                behavior.Fire(ctx);
                yield return new WaitForSeconds(delay);
            }
            ctx.LastFireTimestamp = Time.time;
            _burstCoroutine = null;
        }

        public void CancelBurst()
        {
            if (_burstCoroutine != null)
            {
                StopCoroutine(_burstCoroutine);
                _burstCoroutine = null;
            }
        }
        #endregion

        #region Charge Logic
        public void StartCharge(IWeaponFireBehavior behavior, WeaponContext ctx, float chargeTime)
        {
            if (_chargeCoroutine != null)
                StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = StartCoroutine(ChargeRoutine(behavior, ctx, chargeTime));
        }

        private IEnumerator ChargeRoutine(IWeaponFireBehavior behavior, WeaponContext ctx, float chargeTime)
        {
            yield return new WaitForSeconds(chargeTime);
            behavior.Fire(ctx);
            _chargeCoroutine = null;
        }

        public void CancelCharge()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }
        #endregion

        #region Input Handlers
        public void OnFire(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                FireRequested = true;
                _isFireHeld = true;
            }
            else if (ctx.canceled)
            {
                _isFireHeld = false;
            }
        }

        public void OnAlternateUse(InputAction.CallbackContext ctx)
        {
            if (_activeWeapon == null) return;

            /*
            if (ctx.started)
            {
                if (!_altHeld) { _altHeld = true; _activeWeapon._altBehavior.OnPressed(_activeWeapon.Context); }
                else { _altHeld = false; _activeWeapon._altBehavior.OnReleased(_activeWeapon.Context); }
            }
            */

            if (ctx.started) // button DOWN
            {
                _altHeld = true;
                _activeWeapon._altBehavior.OnPressed(_activeWeapon.Context);
            }
            else if (ctx.canceled) // button UP
            {
                _altHeld = false;
                _activeWeapon._altBehavior.OnReleased(_activeWeapon.Context);
            }
            // You usually don't need ctx.performed for hold-to-aim;
            // reserve it for interactions like Hold(minTime) if you choose to use that.

        }
        public void OnReload(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            _activeWeapon?.RequestReload();
        }
        #endregion
    }
    /* old
    public class WeaponController : MonoBehaviour
    {
        public bool FireRequested { get; set; }

        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponDefinition primaryDef;
        [SerializeField] private WeaponDefinition secondaryDef;

        private WeaponStateMachine _stateMachine;
        private IWeaponFireBehavior _behavior;
        private EquippedWeapon _primary, _secondary, _active;

        private Coroutine _burstCoroutine;

        
        private void Awake()
        {
            // Build your context & machine as before
            var ctx = new WeaponContext
            {
                Definition = primaryDef, // temp, we'll swap right after
                Owner = gameObject,
                FirePoint = firePoint,
                PhotonView = GetComponent<PhotonView>(),
                OwnerRigidbody = GetComponent<Rigidbody>(),
                WeaponController = this
            };

            // Equip both weapons
            _primary = new EquippedWeapon(primaryDef, gameObject, firePoint);
            _secondary = new EquippedWeapon(secondaryDef, gameObject, firePoint);
            _active = _primary;

            // State machine
            _stateMachine = new WeaponStateMachine(ctx);
            _stateMachine.Initialize();
        }


        private void Start()
        {
            _primary = new EquippedWeapon(primaryDef, gameObject, firePoint);
            _secondary = new EquippedWeapon(secondaryDef, gameObject, firePoint);
            _active = _primary;
        }

        private void FixedUpdate()
        {
            _active.FixedUpdate();
        }

        private void Update()
        {
            if (_isFireHeld)
            {
                // fire repeatedly, charge up, whatever
                _active.RequestFire();
            }

            _active.Update();
        }

        private void LateUpdate()
        {
            // Reset after reading
            FireRequested = false;
        }

        // Inputs
        [SerializeField] private bool _isFireHeld;
        public void OnFire(InputAction.CallbackContext ctx)
        {
            // when the button goes down
            if (ctx.phase == InputActionPhase.Started)
                _isFireHeld = true;

            // when the button comes up
            else if (ctx.phase == InputActionPhase.Canceled)
                _isFireHeld = false;
        }

        public void OnReload(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            _active.RequestReload();
        }

        public void OnSwitchPrimary(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            _active = _primary;
        }

        public void OnSwitchSecondary(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            _active = _secondary;
        }

        #region Public Methods
        public void StartBurst(IWeaponFireBehavior behavior, WeaponContext ctx, int count, float delay)
        {
            // cancel any in-progress burst
            if (_burstCoroutine != null)
                StopCoroutine(_burstCoroutine);

            // start a fresh one
            _burstCoroutine = StartCoroutine(FireBurstRoutine(behavior, ctx, count, delay));
        }

        private IEnumerator FireBurstRoutine(
            IWeaponFireBehavior behavior,
            WeaponContext ctx,
            int count,
            float delay
        )
        {
            for (int i = 0; i < count; i++)
            {
                behavior.Fire(ctx);
                yield return new WaitForSeconds(delay);
            }

            ctx.LastFireTimestamp = Time.time;
            _burstCoroutine = null;
        }

        public void CancelBurst()
        {
            if (_burstCoroutine != null)
            {
                StopCoroutine(_burstCoroutine);
                _burstCoroutine = null;
            }
        }


        private Coroutine _chargeCoroutine;

        /// <summary>
        /// Starts (or restarts) a charge that will call `behavior.Fire(ctx)` after delay.
        /// </summary>
        public void StartCharge(
            IWeaponFireBehavior behavior,
            WeaponContext ctx,
            float chargeTime
        )
        {
            // cancel any in-progress charge
            if (_chargeCoroutine != null)
                StopCoroutine(_chargeCoroutine);

            // begin a fresh one
            _chargeCoroutine = StartCoroutine(ChargeRoutine(behavior, ctx, chargeTime));
        }

        /// <summary>
        /// Stops an in-progress charge (so the shot never happens).
        /// </summary>
        public void CancelCharge()
        {
            if (_chargeCoroutine != null)
            {
                StopCoroutine(_chargeCoroutine);
                _chargeCoroutine = null;
            }
        }

        private IEnumerator ChargeRoutine(
            IWeaponFireBehavior behavior,
            WeaponContext ctx,
            float chargeTime
        )
        {
            yield return new WaitForSeconds(chargeTime);

            // fire the wrapped behavior once charge completes
            behavior.Fire(ctx);

            // clear handle
            _chargeCoroutine = null;
        }
        #endregion
    }
    */
}
