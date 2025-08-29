using UnityEngine;

namespace ZiaAnim
{
    [CreateAssetMenu(fileName = "FpAnimProfile", menuName = "FPS/FpAnimProfile")]
    public class FpAnimProfile : ScriptableObject
    {
        [Header("General")]
        public float adsEnterSpeed = 12f;       // how quickly to blend to ADS multipliers
        public float springDamping = 14f;       // visual recoil spring damping
        public float springFrequency = 18f;     // visual recoil spring frequency

        [Header("Sway (Movement)")]
        public AnimationCurve swayX = AnimationCurve.EaseInOut(0, 0, 1, 0); // placeholder
        public AnimationCurve swayY = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public float swayAmplitude = 0.03f;     // meters
        public float swayRotAmplitude = 2f;     // deg (roll/yaw tilt)
        public AnimationCurve swaySpeedToPhase = AnimationCurve.Linear(0, 0.8f, 6f, 2.5f); // m/s -> Hz
        public float hipfireSwayMul = 1f, adsSwayMul = 0.25f;

        [Header("Bob (Speed-scaled)")]
        public AnimationCurve bobX = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public AnimationCurve bobY = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public float bobAmplitude = 0.02f;      // meters
        public float hipfireBobMul = 1f, adsBobMul = 0.2f;

        [Header("Look Lag (Inertia while turning)")]
        public float lookLagMaxYawDeg = 4f;     // how far weapon can lag (deg)
        public float lookLagMaxPitchDeg = 3f;
        public float lookLagFollowSpeed = 12f;  // how fast it catches up
        public float hipfireLookLagMul = 1f, adsLookLagMul = 0.35f;

        [Header("Idle (Breathing)")]
        public AnimationCurve idleY = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public float idleAmplitude = 0.006f;    // meters
        public float idleHz = 0.17f;
        public float hipfireIdleMul = 1f, adsIdleMul = 0.6f;

        [Header("Visual Recoil (tilt toward kick)")]
        public float recoilPosAmplitude = 0.005f;  // meters forward/back
        public Vector3 recoilRotAmplitude = new Vector3(1.5f, 1.0f, 2.5f); // pitch,yaw,roll deg
        public float hipfireRecoilVisMul = 1f, adsRecoilVisMul = 0.8f;

        [Header("Jump Impulse")]
        [Tooltip("Normalized 0..1 time -> offset shape")]
        public AnimationCurve jumpPosY = AnimationCurve.EaseInOut(0, 0, 1, 0); // quick dip then return
        public AnimationCurve jumpRotX = AnimationCurve.EaseInOut(0, 0, 1, 0); // slight nose-down then up
        public float jumpDuration = 0.25f;           // seconds
        public float jumpPosAmp = 0.02f;             // meters
        public Vector3 jumpRotAmp = new Vector3(3f, 0f, 1.5f); // pitch,yaw,roll deg
        public float hipfireJumpMul = 1f, adsJumpMul = 0.6f;

        [Header("Land Impulse")]
        public AnimationCurve landPosY = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public AnimationCurve landRotX = AnimationCurve.EaseInOut(0, 0, 1, 0);
        public float landDuration = 0.3f;
        public float landPosAmp = 0.028f;
        public Vector3 landRotAmp = new Vector3(5f, 0f, 2f);
        public float hipfireLandMul = 1f, adsLandMul = 0.6f;

        [Header("Landing Intensity Mapping")]
        [Tooltip("Map |vertical speed| at impact -> 0..1 intensity")]
        public AnimationCurve landSpeedToIntensity = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(4f, 0.4f), new Keyframe(8f, 1f)
        );
    }
}