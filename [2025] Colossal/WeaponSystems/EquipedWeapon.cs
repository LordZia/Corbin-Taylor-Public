using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using Generics;
using VFX;

namespace Items
{
    static class Stance
    {
        public static float Select(bool isADS, float hipfire, float ads)
            => isADS ? ads : hipfire;
    }

    public class EquippedWeapon
    {
        private readonly WeaponStateMachine _machine;
        public readonly WeaponContext Context;

        public readonly IAlternateUseBehavior _altBehavior;

        public EquippedWeapon(WeaponDefinition def, GameObject owner, Transform firePoint)
        {
            Context = new WeaponContext
            {
                Owner = owner,
                PlayerEventBus = owner.GetComponent<PlayerEventBus>(),
                FirePoint = firePoint,
                LogicFirePoint = owner.GetComponent<PlayerMain>().CameraRigController.mainCameraTransform,
                PhotonView = owner.GetComponent<PhotonView>(),
                OwnerRigidbody = owner.GetComponent<Rigidbody>(),
                Definition = def,
                CurrentAmmo = def.maxAmmo,
                CurrentReserveAmmo = def.startingReserveAmmo,
                LastFireTimestamp = 0f,
                WeaponController = owner.GetComponent<WeaponController>(),
                Recoil = owner.GetComponent<RecoilDriver>()
            };

            _machine = new WeaponStateMachine(Context);

            Debug.Log($"[Equiped Weapon] Created a new weapon with name {def.weaponName}");
            _machine.Initialize();

            _altBehavior = WeaponBehaviorFactory.CreateAlternateUse(def, Context);

        }

        public void Update() => _machine.Tick();
        public void FixedUpdate() => _machine.FixedTick();
        public void RequestFire() => Context.Owner.GetComponent<WeaponController>().FireRequested = true;
        public void RequestReload()
        {
            _machine.ChangeState(_machine.Reload);
        }
    }

    public static class WeaponBehaviorFactory
    {
        /// <summary>
        /// Creates a stack of IWeaponFireBehaviors : each behavior that is created wraps the last created behavior.
        /// On weapon fire - the stack is walked through starting from the last assigned behavior and ending on the first behavior.
        /// </summary>
        /// <param name="def"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IWeaponFireBehavior Create(WeaponDefinition def, WeaponContext ctx)
        {
            // Assign base firee behavior
            IWeaponFireBehavior behavior = def.fireType switch
            {
                WeaponFireType.Projectile when def.pelletAngleOffsets?.Count > 0
                    => new PatternProjectileBehavior(
                           def.pelletAngleOffsets,
                           WrapInaccuracyIfNeeded(new ProjectileSpawnerBehavior(), def)
                       ),
                WeaponFireType.Projectile
                    => WrapInaccuracyIfNeeded(new ProjectileSpawnerBehavior(), def),
                WeaponFireType.Hitscan
                    => WrapInaccuracyIfNeeded(new HitscanDamageBehavior(), def),
                WeaponFireType.Melee
                    => new MeleeDamageBehavior(),
                _ => throw new ArgumentOutOfRangeException()
            };

            // charge behavior
            if (def.isChargeable)
                behavior = new ChargedBehavior(behavior, ctx.WeaponController, def.chargeTime);

            // recoil behaviors (they sample progress/stance)
            if (def.useRecoilPath && def.recoilPath != null)
                behavior = new RecoilPath2DBehavior(behavior, def);
            if (def.useRecoilCurves)
                behavior = new RecoilCurveBehavior(behavior, def);

            // per-shot progress (advances SprayProgress01 once per bullet)
            behavior = new SprayProgressBehavior(behavior, def);

            // per-bullet FX and ammo: ADD ONLY WHAT EXISTS
            if (!def.bottomlessClip)
                behavior = new ConsumeAmmoBehavior(behavior, def);

            if (def.muzzleFlashDef != null)
                behavior = new MuzzleFlashVfxBehavior(behavior, def);

            if (def.muzzleFlashLightDef != null)
                behavior = new MuzzleFlashLightBehavior(behavior, def);

            // called after burst behavior to ensure the event is called per shot not per burst
            behavior = new RaiseWeaponFiredEventBehavior(behavior);

            // burst at the OUTSIDE so the inner chain runs once per bullet
            if (def.burstCount > 1)
                behavior = new BurstBehavior(behavior, ctx.WeaponController, def.burstCount, def.singleFireDelay);


            return behavior;
        }



        private static IWeaponFireBehavior WrapInaccuracyIfNeeded(IWeaponFireBehavior inner, WeaponDefinition def)
        {
            bool apply = (def.inaccuracyMinDeg > 0f) || (def.inaccuracyMaxDeg > 0f);
            if (!apply) return inner;

            // Choose weighted vs simple based on the radial weight curve
            if (ShouldUseWeighted(def.inaccuracyRadialWeight))
                return new ApplyWeightedInaccuracyBehavior(inner);  // uses def.InaccuracyCurve internally via ctx

            return new ApplyInaccuracyBehavior(inner);
        }

        private static bool ShouldUseWeighted(AnimationCurve radialWeight)
        {
            if (radialWeight == null) return false;
            return !IsApproximatelyConstant(radialWeight, tolerance: 0.02f, samples: 33);
        }

        /// <summary>
        /// Treats the curve as "flat" if its value variation over [0,1] is tiny.
        /// Flat => uniform-in-area already; no need for the weighted sampler.
        /// </summary>
        private static bool IsApproximatelyConstant(AnimationCurve curve, float tolerance, int samples)
        {
            if (curve == null) return true;

            float minV = float.PositiveInfinity;
            float maxV = float.NegativeInfinity;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float v = curve.Evaluate(t);
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }
            return (maxV - minV) <= tolerance;
        }

        // OPTIONAL: if we *really* want to treat any straight line as "no special bias" (not recommended),
        // use this instead of IsApproximatelyConstant in ShouldUseWeighted. A linear slope still biases shots.
        // Keeping it here for reference.
        /*
        private static bool IsApproximatelyLinear(AnimationCurve curve, float tolerance, int samples)
        {
            if (curve == null) return true;
            // Line through endpoints
            float y0 = curve.Evaluate(0f);
            float y1 = curve.Evaluate(1f);

            float maxErr = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float yLine = Mathf.Lerp(y0, y1, t);
                float y = curve.Evaluate(t);
                maxErr = Mathf.Max(maxErr, Mathf.Abs(y - yLine));
            }
            return maxErr <= tolerance;
        }
        */

        public static IAlternateUseBehavior CreateAlternateUse(WeaponDefinition def, WeaponContext ctx)
        {
            if (def.canADS)
            {
                Debug.Log($"[WeaponFactory][CreateAltUse] creating new AimDownSightsBehavior behavior");
                return new AimDownSightsBehavior(ctx.WeaponController, ctx.WeaponController.Display);
            }

            return new NoopAlternateBehavior();
        }
    }

    // --- Weapon Behaviors ---
    #region Fire Behaviors
    public interface IWeaponFireBehavior
    {
        void Fire(WeaponContext ctx);
    }

    /// <summary>
    /// Wraps any IWeaponFireBehavior and then plays muzzle-flash & sound exactly once.
    /// </summary>
    public class FireOneShotBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior inner;
        private WeaponDefinition def;
        public FireOneShotBehavior(IWeaponFireBehavior _inner, WeaponDefinition _def) 
        {
            this.inner = _inner;
            def = _def;
        }

        public void Fire(WeaponContext ctx)
        {
            inner.Fire(ctx);

            if (!def.bottomlessClip)
                ctx.CurrentAmmo--;

            LightManager.Instance.Play(
                def: def.muzzleFlashLightDef,
                position: ctx.FirePoint.position,
                followTarget: ctx.FirePoint,
                isLocalOrigin: def.muzzleFlashLightDef.followSpawningObject
            );

            if (def.muzzleFlashDef != null)
            {
                
                NetworkEffectManager.Instance.PlayEffectOnOtherClientsOnly(
                    def.muzzleFlashDef.effectID,
                    ctx.FirePoint.position,
                    ctx.FirePoint.rotation
                );
                

                EffectPoolManager.Instance.PlayEffectFollow(
                    effectID: def.muzzleFlashDef.effectID,
                    target: ctx.FirePoint,
                    offset: Vector3.zero,
                    offsetIsLocal: true,
                    matchRotation: true
                );

            }

            if (ctx.PlayerEventBus != null)
                ctx.PlayerEventBus.Raise(new WeaponFiredEvent { Context = ctx });
        }
    }

    public class ConsumeAmmoBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        public ConsumeAmmoBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        { _inner = inner; _def = def; }

        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);
            if (!_def.bottomlessClip) ctx.CurrentAmmo--;
        }
    }

    public class MuzzleFlashLightBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        public MuzzleFlashLightBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        { _inner = inner; _def = def; }

        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);
            LightManager.Instance.Play(
                def: _def.muzzleFlashLightDef,
                position: ctx.FirePoint.position,
                followTarget: ctx.FirePoint,
                isLocalOrigin: _def.muzzleFlashLightDef.followSpawningObject
            );
        }
    }

    public class MuzzleFlashVfxBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        public MuzzleFlashVfxBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        { _inner = inner; _def = def; }

        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);

            // local VFX
            EffectPoolManager.Instance.PlayEffectFollow(
                effectID: _def.muzzleFlashDef.effectID,
                target: ctx.FirePoint,
                offset: Vector3.zero,
                offsetIsLocal: true,
                matchRotation: true
            );

            // replicate to others
            NetworkEffectManager.Instance.PlayEffectOnOtherClientsOnly(
                _def.muzzleFlashDef.effectID,
                ctx.FirePoint.position,
                ctx.FirePoint.rotation
            );
        }
    }

    public class RaiseWeaponFiredEventBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        public RaiseWeaponFiredEventBehavior(IWeaponFireBehavior inner) { _inner = inner; }
        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);
            ctx.PlayerEventBus?.Raise(new WeaponFiredEvent { Context = ctx });
        }
    }

    public sealed class PerShotProgressBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        private readonly int _ammoPerShot;

        public PerShotProgressBehavior(IWeaponFireBehavior inner, WeaponDefinition def, int ammoPerShot = 1)
        {
            _inner = inner;
            _def = def;
            _ammoPerShot = Mathf.Max(1, ammoPerShot);
        }

        public void Fire(WeaponContext ctx)
        {
            // advance normalized spray progress by fraction of mag consumed
            float denom = Mathf.Max(1, _def.maxAmmo);
            float step = (float)_ammoPerShot / denom;
            ctx.SprayProgress01 = Mathf.Clamp01(ctx.SprayProgress01 + step);

            _inner.Fire(ctx);
        }
    }


    public class ProjectileSpawnerBehavior : IWeaponFireBehavior
    {
        public void Fire(WeaponContext ctx)
        {
            Debug.Log($"[Weapon Fire] - calling Fire on [{ctx.Definition.weaponName}]; remaining ammo = {ctx.CurrentAmmo}");

            Quaternion spawnRot = ctx.LogicFirePoint.localRotation * ctx.FireRotationOffset;
            var proj = ProjectilePool.Instance.Spawn(ctx.LogicFirePoint.position, spawnRot);

            proj.Initialize(ctx);
        }
    }

    public sealed class SprayProgressBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;

        public SprayProgressBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        {
            _inner = inner; _def = def;
        }

        public void Fire(WeaponContext ctx)
        {
            // Advance once per logical shot
            float step = (_def.sprayStepPerShot > 0f)
                ? _def.sprayStepPerShot
                : 1f / Mathf.Max(1, _def.maxAmmo);

            ctx.SprayProgress01 = Mathf.Clamp01(ctx.SprayProgress01 + step);

            _inner.Fire(ctx);
        }
    }


    public class ApplyInaccuracyBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        public ApplyInaccuracyBehavior(IWeaponFireBehavior inner) => _inner = inner;

        public void Fire(WeaponContext ctx)
        {
            var def = ctx.Definition;

            // t from SprayProgressBehavior (0..1 across the mag)
            float t = Mathf.Clamp01(ctx.SprayProgress01);

            // Map via curve
            float k = (def.inaccuracyCurve != null) ? def.inaccuracyCurve.Evaluate(t) : t;

            // Base spread
            float baseSpreadDeg = Mathf.Lerp(def.inaccuracyMinDeg, def.inaccuracyMaxDeg, k);

            // Stance blend (hipfire/ADS) and runtime modifier (airborne/crouch)
            float stanceMul = Stance.Select(ctx.IsAiming, def.hipfireSpreadMultiplier, def.adsSpreadMultiplier);
            float spreadDeg = baseSpreadDeg * Mathf.Max(0f, ctx.CurrentInaccuracyModifier) * stanceMul;

            // UI snapshot
            ctx.CurrentSpreadDeg = spreadDeg;
            {
                float min = def.inaccuracyMinDeg * stanceMul * Mathf.Max(0f, ctx.CurrentInaccuracyModifier);
                float max = def.inaccuracyMaxDeg * stanceMul * Mathf.Max(0f, ctx.CurrentInaccuracyModifier);
                ctx.CurrentAccuracy01 = (max > min + 1e-6f) ? 1f - Mathf.InverseLerp(min, max, Mathf.Clamp(spreadDeg, min, max)) : 1f;
            }

            // Apply spread offset (compose with pellet/pattern)
            var baseOffset = ctx.FireRotationOffset;
            Quaternion spreadRot = Quaternion.identity;

            if (spreadDeg > 0f)
            {
                Vector2 delta = UnityEngine.Random.insideUnitCircle * spreadDeg;
                Transform axis = ctx.LogicFirePoint != null ? ctx.LogicFirePoint : ctx.FirePoint;
                var yaw = Quaternion.AngleAxis(delta.x, axis.up);
                var pitch = Quaternion.AngleAxis(delta.y, axis.right);
                spreadRot = yaw * pitch;
            }

            try { ctx.FireRotationOffset = spreadRot * baseOffset; _inner.Fire(ctx); }
            finally { ctx.FireRotationOffset = baseOffset; }
        }
    }



    public class ApplyWeightedInaccuracyBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private AnimationCurve _cachedCurve;
        private RadialSampler _sampler;

        public ApplyWeightedInaccuracyBehavior(IWeaponFireBehavior inner) => _inner = inner;

        public void Fire(WeaponContext ctx)
        {
            var def = ctx.Definition;

            // 0..1 “shots-through-mag” from SprayProgressBehavior
            float t = Mathf.Clamp01(ctx.SprayProgress01);

            // accuracy-over-spray curve
            float k = (def.inaccuracyCurve != null) ? def.inaccuracyCurve.Evaluate(t) : t;

            // base spread in degrees
            float baseSpreadDeg = Mathf.Lerp(def.inaccuracyMinDeg, def.inaccuracyMaxDeg, k);

            // stance blend: both hipfire and ADS can contribute
            float stanceMul = ctx.IsAiming ? def.adsSpreadMultiplier : def.hipfireSpreadMultiplier;

            // runtime modifier (airborne/crouch/etc.)
            float mod = Mathf.Max(0f, ctx.CurrentInaccuracyModifier);   // 1 = baseline

            float spreadDeg = baseSpreadDeg * stanceMul * mod;

            // --- UI snapshot for crosshair ---
            ctx.CurrentSpreadDeg = spreadDeg;
            {
                float min = def.inaccuracyMinDeg * stanceMul * mod;
                float max = def.inaccuracyMaxDeg * stanceMul * mod;
                ctx.CurrentAccuracy01 = (max > min + 1e-6f)
                    ? 1f - Mathf.InverseLerp(min, max, Mathf.Clamp(spreadDeg, min, max))
                    : 1f;
            }

            // early out if no spread this shot
            if (spreadDeg <= 0f)
            {
                _inner.Fire(ctx);
                return;
            }

            // compose weighted offset with any existing pellet/pattern offset
            var baseOffset = ctx.FireRotationOffset;
            Quaternion spreadRot = Quaternion.identity;

            var weightCurve = def.inaccuracyRadialWeight;
            if (weightCurve != _cachedCurve)
            {
                _cachedCurve = weightCurve;
                _sampler = (weightCurve != null) ? new RadialSampler(weightCurve, 256) : null;
            }

            Vector2 unitDisk = (_sampler != null) ? _sampler.Sample() : UnityEngine.Random.insideUnitCircle;
            Vector2 delta = unitDisk * spreadDeg;

            Transform axis = ctx.LogicFirePoint != null ? ctx.LogicFirePoint : ctx.FirePoint;
            var yaw = Quaternion.AngleAxis(delta.x, axis.up);
            var pitch = Quaternion.AngleAxis(delta.y, axis.right);
            spreadRot = yaw * pitch;

            try
            {
                ctx.FireRotationOffset = spreadRot * baseOffset;
                _inner.Fire(ctx);
            }
            finally
            {
                ctx.FireRotationOffset = baseOffset;
            }
        }
    

    /// <summary>
    /// Samples a 2D offset in the unit disk using an AnimationCurve as radial weight.
    /// The curve is interpreted as weight over area rings, so the radial PDF is w(r) * r.
    /// </summary>
    private sealed class RadialSampler
        {
            private readonly float[] _cdf;    // monotonically increasing 0..1
            private readonly float _dr;
            static float CurveT0(AnimationCurve c) => (c == null || c.length == 0) ? 0f : c.keys[0].time;
            static float CurveT1(AnimationCurve c) => (c == null || c.length == 0) ? 1f : c.keys[c.length - 1].time;

            public RadialSampler(AnimationCurve weight, int samples = 256)
            {
                if (samples < 2) samples = 2;
                _cdf = new float[samples];
                _dr = 1f / (samples - 1);

                if (weight == null || weight.length == 0)
                {
                    float sum = 0f;
                    for (int i = 0; i < samples; i++) { float r = i * _dr; sum += r; _cdf[i] = sum; }
                    for (int i = 0; i < samples; i++) _cdf[i] /= sum;
                    return;
                }

                float t0 = CurveT0(weight), t1 = CurveT1(weight);
                float accum = 0f;
                for (int i = 0; i < samples; i++)
                {
                    float r = i * _dr;
                    float w = Mathf.Max(0f, weight.Evaluate(Mathf.Lerp(t0, t1, r)));
                    float density = w * r; // area ring term
                    accum += density;
                    _cdf[i] = accum;
                }

                if (accum <= 1e-6f)
                {
                    float sum = 0f;
                    for (int i = 0; i < samples; i++) { float r = i * _dr; sum += r; _cdf[i] = sum; }
                    for (int i = 0; i < samples; i++) _cdf[i] /= sum;
                }
                else
                {
                    for (int i = 0; i < samples; i++) _cdf[i] /= accum;
                }
            }


            // Returns a point in the unit disk with the desired radial distribution
            public Vector2 Sample()
            {
                float u = UnityEngine.Random.value;

                // Binary search CDF for u
                int lo = 0;
                int hi = _cdf.Length - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (_cdf[mid] < u) lo = mid + 1;
                    else hi = mid;
                }

                // lo is first index where CDF >= u
                int i1 = Mathf.Clamp(lo, 1, _cdf.Length - 1);
                int i0 = i1 - 1;

                float c0 = _cdf[i0];
                float c1 = _cdf[i1];
                float t = (c1 > c0) ? (u - c0) / (c1 - c0) : 0f;

                float r0 = i0 * _dr;
                float r1 = i1 * _dr;
                float r = Mathf.Lerp(r0, r1, t);

                float theta = UnityEngine.Random.value * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * r;
            }
        }
    }

    public class RecoilCurveBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        private float _prevN = -1f;

        public RecoilCurveBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        { _inner = inner; _def = def; }

        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);
            if (!_def.useRecoilCurves || ctx.Recoil == null) return;

            // n from SprayProgress01 so curves can be “over magazine”
            float duration = Mathf.Max(0.0001f, _def.recoilCurveDuration);
            float nRaw = ctx.SprayProgress01; // or (ctx.SustainedFireTime / duration) if you prefer time-based
            float n = _def.recoilCurveLoop ? (nRaw - Mathf.Floor(nRaw)) : Mathf.Clamp01(nRaw);

            float yawNow = _def.recoilYawCurve.Evaluate(n);
            float pitchNow = _def.recoilPitchCurve.Evaluate(n);

            float yawDelta, pitchDelta;
            if (_prevN < 0f) { yawDelta = yawNow; pitchDelta = pitchNow; }
            else
            {
                float prevN = _prevN;
                if (!_def.recoilCurveLoop && n < prevN) { yawDelta = yawNow; pitchDelta = pitchNow; }
                else
                {
                    float yPrev = _def.recoilYawCurve.Evaluate(prevN);
                    float pPrev = _def.recoilPitchCurve.Evaluate(prevN);
                    yawDelta = yawNow - yPrev;
                    pitchDelta = pitchNow - pPrev;
                }
            }

            // Base mult and stance blend
            float baseMult = _def.recoilCurveScale;
            float stanceMul = Stance.Select(ctx.IsAiming, _def.hipfireRecoilCurveMultiplier, _def.adsRecoilCurveMultiplier);

            Vector2 kick = new Vector2(yawDelta, pitchDelta) * (baseMult * stanceMul);

            if (_def.recoilNoiseDeg > 0f)
                kick += UnityEngine.Random.insideUnitCircle * _def.recoilNoiseDeg;

            ctx.Recoil.ApplyKick(kick);
            _prevN = n;
        }
    }


    /// <summary>
    /// Follows a path defined by Path2D
    /// </summary>
    public class RecoilPath2DBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponDefinition _def;
        private float _prevN = -1f;

        public RecoilPath2DBehavior(IWeaponFireBehavior inner, WeaponDefinition def)
        { _inner = inner; _def = def; }

        public void Fire(WeaponContext ctx)
        {
            _inner.Fire(ctx);
            if (!_def.useRecoilPath || _def.recoilPath == null || ctx.Recoil == null) return;

            float n = Mathf.Clamp01(ctx.SprayProgress01);

            Vector2 now = _def.recoilPathDistanceNormalized
                ? _def.recoilPath.EvaluateByDistance01(n, 128)
                : _def.recoilPath.Evaluate01(n);

            Vector2 delta;
            if (_prevN < 0f || n < _prevN) delta = now;
            else
            {
                Vector2 prev = _def.recoilPathDistanceNormalized
                    ? _def.recoilPath.EvaluateByDistance01(_prevN, 128)
                    : _def.recoilPath.Evaluate01(_prevN);
                delta = now - prev;
            }

            Vector2 axisScale = _def.recoilPathAxisScaleDeg * _def.recoilCurveScale;

            // Stance multiplier (hipfire contributes if >0)
            float stanceMul = Stance.Select(ctx.IsAiming, _def.hipfireRecoilPathMultiplier, _def.adsRecoilPathMultiplier);

            Vector2 kick = new Vector2(delta.x * axisScale.x, delta.y * axisScale.y) * stanceMul;

            // (Optional legacy) If you still want a global ADS scale, multiply here:
            // if (ctx.IsAiming) kick *= _def.recoilAdsMultiplier;

            if (_def.recoilNoiseDeg > 0f) kick += UnityEngine.Random.insideUnitCircle * _def.recoilNoiseDeg;

            ctx.Recoil.ApplyKick(kick);
            ctx.Recoil.HoldFor(ctx.Definition.sprayHoldDelay);

            //ctx.Recoil.HoldFor(ctx.Definition.sprayHoldDelay, returnFraction: 0.25f, postHoldBleedTime: 0.0f);

            _prevN = n;

#if UNITY_EDITOR
            _def.recoilPath.editorPreviewT = n;
            Path2DPreviewBridge.MarkDirty();
#endif
        }
    }




    /// <summary>
    /// Does exactly one hitscan
    /// </summary>
    public class HitscanDamageBehavior : IWeaponFireBehavior
    {
        public void Fire(WeaponContext ctx)
        {
            var origin = ctx.FirePoint.position;
            var dir = ctx.FirePoint.forward;
            if (Physics.Raycast(origin, dir, out var hit, ctx.Definition.range))
            {
                // 1) damage authority
                if (ctx.PhotonView.IsMine)
                {
                    var dmg = hit.collider.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageManager.Instance.ReportDamage(
                            ctx.PhotonView.ViewID,
                            hit.collider.GetComponent<PhotonView>().ViewID,
                            ctx.Definition.damage,
                            ctx.Definition.damageType
                        );
                }

                // 2) impact VFX (flesh vs structure)
                var effect = hit.collider.GetComponent<PlayerMain>() != null
                    ? ctx.Definition.fleshImpactEffect
                    : ctx.Definition.structureImpactEffect;

                DamageManager.Instance.ReportImpact(
                    effect.effectID,
                    hit.point,
                    hit.normal
                );
            }
        }
    }
   /// <summary>
   /// Does exactly one melee swing
   /// </summary>
    public class MeleeDamageBehavior : IWeaponFireBehavior
    {
        public void Fire(WeaponContext ctx)
        {
            // simple example: sphere cast in front
            var hits = Physics.SphereCastAll(
                ctx.FirePoint.position,
                ctx.Definition.meleeRadius,
                ctx.FirePoint.forward,
                ctx.Definition.range
            );
            foreach (var h in hits)
            {

                if (ctx.PhotonView.IsMine)
                {
                    return;
                    var dmg = h.collider.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageManager.Instance.ReportDamage(
                            ctx.PhotonView.ViewID,
                            h.collider.GetComponent<PhotonView>().ViewID,
                            ctx.Definition.damage,
                            ctx.Definition.damageType
                        );
                }

                // impact VFX (flesh vs structure)
                var effect = h.collider.GetComponent<PlayerMain>() != null
                    ? ctx.Definition.fleshImpactEffect
                    : ctx.Definition.structureImpactEffect;

                DamageManager.Instance.ReportImpact(
                    effect.effectID,
                    h.point,
                    h.normal
                );
            }
        }
    }
    public class ChargedBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly WeaponController _controller;
        private readonly float _chargeTime;

        public ChargedBehavior(
            IWeaponFireBehavior inner,
            WeaponController controller,
            float chargeTime
        )
        {
            _inner = inner;
            _controller = controller;
            _chargeTime = chargeTime;
        }

        public void Fire(WeaponContext ctx)
        {
            // kick off the charge on the controller
            _controller.StartCharge(_inner, ctx, _chargeTime);
        }

        /// <summary>
        /// If you need to cancel (e.g. player switched weapons), call:
        /// ctx.WeaponController.CancelCharge();
        /// </summary>
    }
    public class PatternProjectileBehavior : IWeaponFireBehavior
    {
        private readonly List<Vector2> _angleOffsets;
        private readonly IWeaponFireBehavior _spawner;

        public PatternProjectileBehavior(
            List<Vector2> angleOffsets,
            IWeaponFireBehavior spawner
        )
        {
            _angleOffsets = angleOffsets;
            _spawner = spawner;
        }

        public void Fire(WeaponContext ctx)
        {
            // For each pellet, set FireRotationOffset then fire:
            foreach (var offs in _angleOffsets)
            {
                // build a small pitch/yaw offset around the camera's axes
                var pitch = Quaternion.AngleAxis(offs.y, ctx.LogicFirePoint.right);
                var yaw = Quaternion.AngleAxis(offs.x, ctx.LogicFirePoint.up);

                ctx.FireRotationOffset = yaw * pitch;
                _spawner.Fire(ctx);
            }

            // restore to no-offset
            ctx.FireRotationOffset = Quaternion.identity;
        }
    }
    public class BurstBehavior : IWeaponFireBehavior
    {
        private readonly IWeaponFireBehavior _inner;
        private readonly int _burstCount;
        private readonly float _burstDelay;
        private readonly WeaponController _controller;

        public BurstBehavior(
            IWeaponFireBehavior inner,
            WeaponController controller,
            int burstCount,
            float burstDelay
        )
        {
            _inner = inner;
            _controller = controller;
            _burstCount = burstCount;
            _burstDelay = burstDelay;
        }

        public void Fire(WeaponContext ctx)
        {
            // hand off the whole sequence to the controller
            _controller.StartBurst(_inner, ctx, _burstCount, _burstDelay);
        }
    }
    #endregion

    #region Alt Use Behaviors

    /// <summary>
    /// Called by WeaponController when the player uses the alternate input (e.g. right-click or left trigger).
    /// </summary>
    public interface IAlternateUseBehavior
    {
        bool IsActive { get; }
        bool BlocksPrimary { get; }     // e.g., charge attack that suppresses LMB while held

        void OnPressed(WeaponContext ctx);    // button down
        void OnReleased(WeaponContext ctx);   // button up
        void Tick(WeaponContext ctx, float dt);
    }



    public struct ADSStateChangedEvent { public bool IsAiming; }

    public class AimDownSightsBehavior : IAlternateUseBehavior
    {
        private readonly EquippedWeaponDisplayManager _display;
        private readonly WeaponController _controller;
        private bool _active;

        public AimDownSightsBehavior(WeaponController controller, EquippedWeaponDisplayManager display)
        {
            _controller = controller;
            _display = display;
        }

        public bool IsActive => throw new NotImplementedException();

        public bool BlocksPrimary => throw new NotImplementedException();

        // Call this on ALT-USE PRESSED (toggle)
        public void OnAlternateUse(WeaponContext ctx)
        {
            _active = !_active;

            ctx.IsAiming = _active; // drives all the gating above

            // Optional niceties (implement these in your systems as you prefer):
            //_display?.SetADS(_active); // crosshair swap, sway, overlays, etc.
            //_controller?.SetADSPlayerFOV(_active); // FOV zoom / sens scaling on your camera rig
            //ctx.PlayerEventBus?.Raise(new ADSStateChangedEvent { IsAiming = _active });
        }

        public void OnPressed(WeaponContext ctx)
        {
            _active = true;

            ctx.IsAiming = _active; // drives all the gating above

        }

        public void OnReleased(WeaponContext ctx)
        {
            _active = false;

            ctx.IsAiming = _active; // drives all the gating above

        }

        public void Tick(WeaponContext ctx, float dt)
        {
            Debug.Log($"[EquipedWeapon][AimDownSightsBehavior] player is currently aiming down sights");
        }
    }


    public class NoopAlternateBehavior : IAlternateUseBehavior
    {
        public bool IsActive => throw new NotImplementedException();

        public bool BlocksPrimary => throw new NotImplementedException();

        public void OnAlternateUse(WeaponContext ctx) { /* nothing */ }

        public void OnPressed(WeaponContext ctx)
        {
            throw new NotImplementedException();
        }

        public void OnReleased(WeaponContext ctx)
        {
            throw new NotImplementedException();
        }

        public void Tick(WeaponContext ctx, float dt)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
