using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generics;
using VFX;

namespace Items
{
    public class WeaponStateMachine : StateMachine
    {
        public WeaponContext Context { get; }
        public IdleState Idle { get; private set; }
        public FireState Fire { get; private set; }
        public CooldownState Cooldown { get; private set; }
        public ReloadState Reload { get; private set; }

        public WeaponStateMachine(WeaponContext context)
        {
            Context = context;
        }

        public void Initialize()
        {
            Idle = new IdleState(this);
            Fire = new FireState(this);
            Cooldown = new CooldownState(this);
            Reload = new ReloadState(this);

            base.Initialize(Idle);
            Debug.Log($"[Weapon State Machine] Created a new weapon state machine for a weapon named : {Context.Definition.weaponName}");
        }
    }

    // --- Shared Base WeaponState ------------------------
    public abstract class WeaponState : State
    {
        protected WeaponStateMachine Machine => (WeaponStateMachine)StateMachine;
        protected WeaponContext       Ctx     => Machine.Context;

        protected WeaponState(WeaponStateMachine machine)
            : base(machine)
        {
        }

        protected static class SprayProgressUtil
        {
            // Convert inaccuracyRecoveryPerSec (sec/sec on old SustainedFireTime)
            // into a 0..1 decay rate for SprayProgress01.
            public static void DecaySpray(ref float sprayProgress01, WeaponDefinition def, float dt)
            {
                if (sprayProgress01 <= 0f) return;

                // If ramp time is 0, treat recovery as instant
                if (def.inaccuracyRampTime <= 0f) { sprayProgress01 = 0f; return; }

                // rec (seconds of ramp removed per second) -> fraction of ramp per second
                float rate01 = (def.inaccuracyRecoveryPerSec * dt) / def.inaccuracyRampTime;
                sprayProgress01 = Mathf.Max(0f, sprayProgress01 - rate01);
            }

            public static float ShotInterval(WeaponDefinition def)
            {
                return (def.singleFireDelay > 0f)
                    ? def.singleFireDelay
                    : 1f / Mathf.Max(0.0001f, def.fireRate);
            }
        }
    }

    // --- Idle State -------------------------------------
    public class IdleState : WeaponState
    {
        public IdleState(WeaponStateMachine machine) : base(machine) { }

        public override void Enter()
        {
            base.Enter();
        }    

        public override void Tick()
        {
            var def = Ctx.Definition;

            bool held = Ctx.WeaponController.FireRequested;
            float rec = Ctx.Definition.inaccuracyRecoveryPerSec;

            float sinceLast = Time.time - Ctx.LastFireTimestamp;
            bool withinSprayWindow = sinceLast <= Ctx.Definition.recoilSprayResetDelay;

            if (!held && !withinSprayWindow)
                SprayProgressUtil.DecaySpray(ref Ctx.SprayProgress01, def, Time.deltaTime);

            // Legacy compatibility for any old readers
            Ctx.SustainedFireTime = def.inaccuracyRampTime * Mathf.Clamp01(Ctx.SprayProgress01);

            if (Ctx.SustainedFireTime < 0f)
            {
                Ctx.SustainedFireTime = 0f;
            }

            if (held) StateMachine.ChangeState(Machine.Fire);
        }
}


    // --- Fire State -------------------------------------
    public class FireState : WeaponState
    {
        private readonly IWeaponFireBehavior _behavior;

        public FireState(WeaponStateMachine machine)
            : base(machine)
        {
            _behavior = WeaponBehaviorFactory.Create(Ctx.Definition, machine.Context);
            Debug.Log($"[Weapon State Machine] Creating new IWeaponFireBehavior from weapon named: {Ctx.Definition.weaponName}");

        }

        public override void Enter()
        {
            Debug.Log("[Weapon Fire] - WeaponStateMachine - changed to FireState");
            if (Ctx.CurrentAmmo <= 0)
            {

                StateMachine.ChangeState(Machine.Reload);
                return;
            }

            _behavior.Fire(Ctx);
            //Debug.Log("[Weapon Fire] - WeaponStateMachine - calling _behavior.Fire : remaining ammo = " + Ctx.CurrentAmmo);
            Ctx.LastFireTimestamp = Time.time;
        }

        public override void Tick()
        {
            StateMachine.ChangeState(Machine.Cooldown);
        }
    }

    // --- Cooldown State ---------------------------------
    public class CooldownState : WeaponState
    {
        public CooldownState(WeaponStateMachine machine) : base(machine) { }
        public override void Tick()
        {
            var def = Ctx.Definition;

            bool held = Ctx.WeaponController.FireRequested;

            float sinceLast = Time.time - Ctx.LastFireTimestamp;
            bool withinSprayWindow = sinceLast <= Ctx.Definition.recoilSprayResetDelay;


            if (!held && !withinSprayWindow)
                SprayProgressUtil.DecaySpray(ref Ctx.SprayProgress01, def, Time.deltaTime);

            // Legacy compatibility for any old readers
            Ctx.SustainedFireTime = def.inaccuracyRampTime * Mathf.Clamp01(Ctx.SprayProgress01);


            // lower bound only (do NOT clamp to a duration here)
            if (Ctx.SustainedFireTime < 0f) Ctx.SustainedFireTime = 0f;

            if (Time.time >= Ctx.LastFireTimestamp + 1f / Ctx.Definition.fireRate)
                StateMachine.ChangeState(Machine.Idle);
        }
    }


    // --- Reload State -----------------------------------
    public class ReloadState : WeaponState
    {
        private float _endTime;

        public ReloadState(WeaponStateMachine machine)
            : base(machine)
        {
        }

        public override void Enter()
        {
            AttemptEnter();
        }

        private void AttemptEnter()
        {
            if (Ctx.CurrentAmmo < Ctx.Definition.maxAmmo && Ctx.CurrentReserveAmmo > 0)
            {
                AcceptEnter();
            }
            else
            {
                RejectEnter();
            }
        }

        private void AcceptEnter()
        {
            Debug.Log("[Weapon Fire] - WeaponStateMachine - changed to ReloadState");
            _endTime = Time.time + Ctx.Definition.reloadTime;

            // TODO: trigger reload animation

            /*
            if (Ctx.Definition.reloadSound != null)
                SoundManager.Instance.Play(Ctx.Definition.reloadSound.soundId, Ctx.LogicFirePoint.position, 1, Ctx.LogicFirePoint);
            */

            if (Ctx.PlayerEventBus != null)
                Ctx.PlayerEventBus.Raise(new WeaponReloadStartEvent { Context = Ctx });
        }

        private void RejectEnter()
        {
            // TODO: Play out of ammo fx

            StateMachine.ChangeState(Machine.Idle);
        }

        public override void Tick()
        {
            if (Time.time >= _endTime)
            {
                // Calculate ammo reloaded; Only Drain if infinite ammo is not enabled
                if (!Ctx.Definition.infiniteReserveAmmo)
                {
                    int reloadedAmt = Ctx.Definition.maxAmmo - Ctx.CurrentAmmo;
                    Ctx.CurrentAmmo = Mathf.Min(Ctx.Definition.maxAmmo, Ctx.CurrentReserveAmmo);

                    Ctx.CurrentReserveAmmo -= reloadedAmt;
                }
                else
                {
                    Ctx.CurrentAmmo = Ctx.Definition.maxAmmo;
                }


                // Reset vars
                Ctx.SustainedFireTime = 0;
                Ctx.SprayProgress01 = 0;
                Ctx.CurrentSpreadDeg = 0;
                Ctx.CurrentAccuracy01 = 0;

                Debug.Log($"[WeaponStateMachine] {Ctx.Definition.weaponName} was reloaded! remaining reseve ammo = {Ctx.CurrentReserveAmmo}");
                if (Ctx.PlayerEventBus != null)
                    Ctx.PlayerEventBus.Raise(new WeaponReloadedEvent { Context = Ctx });

                StateMachine.ChangeState(Machine.Idle);
            }
        }
    }
}
