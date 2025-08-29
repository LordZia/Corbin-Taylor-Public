using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using VFX;
using UnityEngine.Serialization;

namespace Items
{
    public enum WeaponFireType { Hitscan, Projectile, Melee, Charged }
    public enum WeaponSlot { Primary, Secondary }

    [CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Weapon Identifiers")]
        public string weaponName;
        public int weaponID;
        public string worldItemPrefabName;
        public string damageType = "weapon";

        [Header("General Stats")]
        public WeaponFireType fireType;
        public int damage;
        //public float damageFallOff
        public float fireRate;          // shots per second
        public float range;             // for hitscan & melee radius
        public int maxAmmo;
        public int startingReserveAmmo = 50;

        public float reloadTime;
        public float recoilStrength;
        public float accuracySpread;
        public bool canADS = false;
        public bool infiniteReserveAmmo = false;
        public bool bottomlessClip = false;

        // WeaponDefinition
        [Header("Spray Progress")]
        [Tooltip("If > 0, advance this much per shot. If 0, will use 1 / maxAmmo.")]
        public float sprayStepPerShot;            // default 0 => 1/maxAmmo
        [Tooltip("Delay before spray begins cooling (seconds).")]
        public float sprayHoldDelay = 0.35f;
        [Tooltip("Seconds to cool from 1 -> 0 while not firing.")]
        public float sprayCooldownTime = 0.6f;
        [Tooltip("Reset SprayProgress01 to 0 when reload finishes.")]
        public bool resetSprayOnReload = true;


        [Header("Inaccuracy")]
        public float inaccuracyMinDeg = 0f;        // minimum random cone
        public float inaccuracyMaxDeg = 5f;        // maximum random cone
        public float inaccuracyRampTime = 1.5f;    // seconds to reach full inaccuracy
        public float inaccuracyRecoveryPerSec = 2f;// how fast it recovers toward 0 when not firing

        public AnimationCurve inaccuracyRadialWeight = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve inaccuracyCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Grounded Status Inaccuracy")]
        [Space(2)]
        [Range(0f, 1f)] public float groundedAccuracyScale = 1f;
        [Range(0f, 1f)] public float airborneAccuracyScale = 0.8f;

        [Header("Stance Based Inaccuracy & Recoil Blending")]
        [Space(2)]

        [Header("(0 = off, 1 = normal, >1 = amplified)")]
        [Range(0f, 2f)] public float hipfireSpreadMultiplier = 1f;
        [Range(0f, 2f)] public float adsSpreadMultiplier = 0f;

        [Range(0f, 2f)] public float hipfireRecoilPathMultiplier = 0f;
        [Range(0f, 2f)] public float adsRecoilPathMultiplier = 1f;

        [Range(0f, 2f)] public float hipfireRecoilCurveMultiplier = 1f;
        [Range(0f, 2f)] public float adsRecoilCurveMultiplier = 0f;

        [Header("Recoil Curves (per shot)")]
        public bool useRecoilCurves = false;              // enable curve-based recoil
        public AnimationCurve recoilYawCurve = AnimationCurve.Linear(0, 0, 1, 0);   // degrees
        public AnimationCurve recoilPitchCurve = AnimationCurve.Linear(0, 0, 1, 2);   // degrees
        public float recoilCurveDuration = 0.6f;             // seconds for 0..1 domain
        public bool recoilCurveLoop = true;             // wrap when time exceeds duration

        // These Vars are used for recoil path2D as well.
        public float recoilCurveScale = 1f;               // global multiplier
        public float recoilNoiseDeg = 0f;               // random jitter radius per shot

        // --- Recoil (Path2D) ---
        [Header("Recoil (Path2D)")]
        public bool useRecoilPath = false;
        public Path2D recoilPath;
        [Tooltip("Map 0..1 over this duration of sustained fire. If loop is off, clamp at 1.")]
        public float recoilPathDuration = 0.5f;
        public bool recoilPathLoop = true;

        [Tooltip("If true, EvaluateByDistance01 is used for even speed along the drawn path.")]
        public bool recoilPathDistanceNormalized = false;

        [Tooltip("Axis scale in DEGREES per Path2D unit. X = yaw, Y = pitch.")]
        public Vector2 recoilPathAxisScaleDeg = new Vector2(1f, 1f);

        // Recoil progression
        [Tooltip("If ON, the path will be traversed in 'recoilPatternShots' shots, regardless of cadence.")]
        public bool useShotsToSpan = false;

        [Min(1)]
        [Tooltip("How many shots should traverse the full Path2D when useShotsToSpan is ON.")]
        public int recoilPatternShots = 30;

        [Header("Fire Mode")]
        [Tooltip("Hold trigger to fire continuously.")]
        public bool isAutomatic = false;

        [Header("Projectile Stats")]
        public float projectileLifetime = 2.5f;
        public float projectileSpeed = 1;
        [Tooltip("Meters per second of downward acceleration")]
        public float projectileDropAcceleration = 0;

        [Header("Burst Settings")]
        [Tooltip("Number of shots per trigger pull (1 = normal)")]
        public int burstCount = 3;
        [Tooltip("Delay between individual shots when not full-auto (e.g., semi or within-burst). Set 0 for full-auto.")]
        [FormerlySerializedAs("burstDelay")]
        public float singleFireDelay = 0.10f;
        [Tooltip("seconds since last shot before we reset pattern")]
        public float recoilSprayResetDelay = 0.25f;
        [Range(0f, 0.25f)]
        [Tooltip("allow small backstep in current spray progress time without treating as reset")]
        public float recoilHoldbackTolerance = 0.03f;

        [Header("Impact Effect")]
        [Tooltip("ID used by the VFX system to play the correct impact effect")]
        public EffectDefinition fleshImpactEffect;
        public EffectDefinition structureImpactEffect;

        [Header("Charge Settings")]
        public bool isChargeable;
        public float chargeTime;        // time before fire

        [Header("Multi-Shot Pattern")]
        [Tooltip("List of (horizontal, vertical) angles in degrees for each pellet")]
        public List<Vector2> pelletAngleOffsets = new List<Vector2>();

        [Header("Melee Stats")]
        public float meleeRadius = 1;

        [Header("References")]
        public GameObject projectilePrefab;  // for projectile weapons
        public GameObject viewPrefab;

        [Header("VFX")]
        public EffectDefinition muzzleFlashDef;
        public EffectDefinition lingeringEffectDef;
        public LightDefinition muzzleFlashLightDef;

        [Header("VFX - Casings")]
        public CasingSettings casings = CasingSettings.Default();

        [Header("Audio")]
        public SoundDefinition fireSound;
        public AnimationCurve soundPitchByAmmo = AnimationCurve.Constant(0, 1, 0f);

        public SoundDefinition reloadSound;
        public SoundDefinition fleshImpactSoundDef;
        public SoundDefinition structureImpactSoundDef;


        [Header("In Game Displays")]
        public string interactMessage = "Weapon";

        [Header("HUD Displays")]
        public Sprite displayIcon;
        public Sprite crosshairIconHipfire;
        public Sprite crosshairIconADS;

        public AnimationCurve crossHairScaleOverSprayDuration = AnimationCurve.Constant(0, 1, 0f);

        public AnimationCurve crosshairScaleOverADSTransition = AnimationCurve.Constant(0, 1, 0f);


#if UNITY_EDITOR
        private void OnValidate()
        {
            casings.lifetime = Mathf.Max(0f, casings.lifetime);
            casings.gravity = Mathf.Max(0f, casings.gravity);
            casings.jitter = Mathf.Max(0f, casings.jitter);

            if (casings.ejectSpeed.y < casings.ejectSpeed.x)
                casings.ejectSpeed.y = casings.ejectSpeed.x;

            if (casings.spinDegPerSec.y < casings.spinDegPerSec.x)
                casings.spinDegPerSec.y = casings.spinDegPerSec.x;

            if (casings.enabled)
            {
                if (casings.realEffect != null)
                {
                    Debug.LogWarning($"[WeaponDefinition:{name}] Casings is enabled but has no real casing effect assigned.", this);
                }

                if (casings.ghostEffect != null)
                {
                    Debug.LogWarning($"[WeaponDefinition:{name}] Casings is enabled but has no ghost casing effect assigned.", this);
                }
            }
        }
#endif
    }

    public class WeaponContext
    {
        public WeaponDefinition Definition;
        public GameObject Owner;
        public PlayerEventBus PlayerEventBus;

        /// <summary>
        /// Where visuals originate from (ghost projectiles and vfx)
        /// </summary>
        public Transform FirePoint;

        /// <summary>
        /// Where the actual collision checks are performed from
        /// </summary>
        public Transform LogicFirePoint;

        /// <summary>
        /// Any extra rotation to apply 
        /// to both LogicFirePoint & FirePoint when spawning.
        /// </summary>
        public Quaternion FireRotationOffset { get; set; } = Quaternion.identity;

        public PhotonView PhotonView;
        public Rigidbody OwnerRigidbody;  
        public int CurrentAmmo;
        public int CurrentReserveAmmo;

        public float CurrentInaccuracyModifier = 1;
        public float CurrentSpreadDeg;
        public float CurrentAccuracy01;
        // WeaponContext
        public float SprayProgress01 = 0; // 0..1, advances once per logical shot, resets on reload

        public float LastFireTimestamp;
        public WeaponController WeaponController;
        public RecoilDriver Recoil;
        
        public float SustainedFireTime;           // seconds of continuous firing pressure

        public bool isPrimary;
        public bool IsAiming = false;
    }


    [System.Serializable]
    public struct CasingSettings
    {
        public bool enabled;

        [Header("Effect Pool")]
        [Tooltip("Physics-driven real casing effect (prefab lives in the EffectDefinition). REQUIRED for casings.")]
        public EffectDefinition realEffect;

        [Tooltip("Optional cheap visual ‘ghost’ casing effect.")]
        public EffectDefinition ghostEffect;

        [Header("Mix / Timing")]
        [Range(0f, 1f)]
        [Tooltip("Fraction of shots that should spawn REAL casings on average within ~1s.")]
        public float desiredRealPercentPerSecond;

        [Header("Kinematics")]
        [Min(0f)] public float lifetime;           // seconds
        [Min(0f)] public float gravity;            // used by your ghost sim (9.81 * gravity)
        public Vector2 ejectSpeed;                 // m/s (x=min, y=max)
        public Vector2 spinDegPerSec;              // deg/s (x=min, y=max)
        [Min(0f)] public float jitter;             // random linear velocity magnitude
        public Vector3 spawnRotationOffsetEuler;   // optional orientation offset at spawn

        public static CasingSettings Default()
        {
            return new CasingSettings
            {
                enabled = true,
                realEffect = null,
                ghostEffect = null,
                desiredRealPercentPerSecond = 0.2f,
                lifetime = 3f,
                gravity = 1.5f,
                ejectSpeed = new Vector2(2f, 4f),
                spinDegPerSec = new Vector2(30f, 180f),
                jitter = 0.5f,
                spawnRotationOffsetEuler = Vector3.zero
            };
        }
    }

}