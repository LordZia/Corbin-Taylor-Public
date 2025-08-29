using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generics;
using ZiaPlayer;
public class PlayerMovementStateMachine : MonoBehaviour
{
    public StateMachine StateMachine { get; private set; }

    // States
    public IdleState IdleState { get; private set; }
    public WalkState WalkState { get; private set; }
    public SprintState SprintState { get; private set; }
    public CrouchState CrouchState { get; private set; }
    public JumpState JumpState { get; private set; }
    public WallrunState WallrunState { get; private set; }
}

public abstract class PlayerState : State
{
    protected Zia_PlayerMovement player;
    protected Rigidbody rb => player.Rb;
    protected Vector2 input => player.MoveInput;
    public virtual PlayerStateType StateType => PlayerStateType.Unknown;

    public PlayerState(StateMachine sm, Zia_PlayerMovement player) : base(sm)
    {
        this.player = player;
    }

    public virtual float GetSpeedMultiplier() => 1f;

    // Signal-based transition hooks
    public virtual void OnJumpPressed()
    {
        if (player.IsGrounded)
        {
            player.ExecuteJump();
        }
        else
        {
            player.SetJumpBuffer();
            player.CheckForBufferedJump();
        }
    }
    public virtual void OnCrouchPressed() 
    {
        TransitionToCrouchOrSlide();
    }
    public virtual void OnSprintPressed() 
    {
        StateMachine.ChangeState(player.SprintState);
    }
    public virtual void OnFallDetected() 
    {
        StateMachine.ChangeState(player.FallingState);
    }
    public virtual void OnGrounded() 
    {
        if (player.WallRunEnabled)
        {
            player.WallRunHandler.UpdateGroundedStatus(true);
        }

        if (input.magnitude > 0.1f)
        {
            StateMachine.ChangeState(player.WalkState);
        }
        else
        {
            StateMachine.ChangeState(player.IdleState);
        }
    }
    public virtual void OnVaultDetected() 
    {
        StateMachine.ChangeState(player.VaultState);
    }
    public virtual void OnWallDetected() 
    {
        StateMachine.ChangeState(player.WallrunState);
    }

    public virtual void OnMoveInput()
    {

    }

    // Helper methods
    protected void TransitionToIdleOrWalk()
    {
        if (input.magnitude > 0.1f)
            StateMachine.ChangeState(player.WalkState);
        else
            StateMachine.ChangeState(player.IdleState);
    }

    protected void TransitionToCrouchOrSlide()
    {
        if (player.CanSlide())
        {
            StateMachine.ChangeState(player.SlideState);
        }
        else
        {
            StateMachine.ChangeState(player.CrouchState);
        }
    }
}

// -----Movement States-----

public abstract class GroundedState : PlayerState
{
    public GroundedState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
}

// --- IdleState.cs ---
public class IdleState : GroundedState
{
    public IdleState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Idle;
    public override void FixedTick()
    {
        if (input.magnitude > 0.1f)
        {
            StateMachine.ChangeState(player.WalkState);
        }

        Vector3 velocity = player.Rb.velocity;
        if (velocity.magnitude == 0) return;

        Vector2 velocityHorizontal = new Vector2(velocity.x, velocity.z);
        player.ApplyGroundFriction(ref velocityHorizontal);

        player.Rb.velocity = new Vector3(velocityHorizontal.x, velocity.y, velocityHorizontal.y);
    }

    public override void OnMoveInput()
    {
        StateMachine.ChangeState(player.WalkState);
    }
}

// --- WalkState.cs ---
public class WalkState : GroundedState
{
    public WalkState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Walk;
    public override void FixedTick()
    {
        player.ApplyMove();
        if (ExitWalkCheck())
            StateMachine.ChangeState(player.IdleState);
    }

    private bool ExitWalkCheck()
    {
        if (player.MoveInput.magnitude == 0) return true;
        return false;
    }
}

// --- SprintState.cs ---
public class SprintState : GroundedState
{
    public SprintState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Sprint;
    public override float GetSpeedMultiplier() => player.SprintSpeedMultiplier;

    public override void Enter()
    {
        base.Enter();
        player.SetSprinting(true);
    }

    public override void Exit()
    {
        base.Exit();
        player.SetSprinting(false);
    }
    public override void FixedTick()
    {
        player.ApplyMove();
        ExitSprintCheck();
    }

    private void ExitSprintCheck()
    {
        if (player.MoveInput.y < player.SprintSpeedThreshold)
        {
            StateMachine.ChangeState(player.WalkState);
        }
    }
}

// --- CrouchState.cs ---
public class CrouchState : GroundedState
{
    public CrouchState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Crouch;
    public override float GetSpeedMultiplier() => player.CrouchSpeedMultiplier;

    public override void Enter()
    {
        base.Enter();
        player.SetCrouchVisuals(true, !player.IsGrounded);
    }

    public override void Exit()
    {
        base.Exit();
        player.SetCrouchVisuals(false, !player.IsGrounded);
    }
    public override void FixedTick()
    {
        player.ApplyMove();
    }

    public override void OnCrouchPressed()
    {
        if (player.LeaveCrouchCheck())
        {
            TransitionToIdleOrWalk();
        }
    }

    public override void OnJumpPressed()
    {
        if (player.LeaveCrouchCheck())
        {
            base.OnJumpPressed();
        }
    }
}

public class CrouchedWalkState : CrouchState
{
    public CrouchedWalkState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.CrouchWalk;
}

// --- SlideState.cs ---
public class SlideState : GroundedState
{
    private float lastGroundedTime;
    private const float slideFallGraceTime = 0.15f;
    public SlideState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Slide;
    public void ExitSlide()
    {
        if (player.LeaveCrouchCheck())
        {
            TransitionToIdleOrWalk();
        }
        else
        {
            StateMachine.ChangeState(player.CrouchState);
        }
    }

    public override void Enter()
    {
        base.Enter();

        lastGroundedTime = Time.fixedTime;
        player.SetCrouchVisuals(true, false);
        player.BeginSlide();
    }

    public override void Exit()
    {
        player.SetCrouchVisuals(false, false);
        base.Exit();
    }
    public override void FixedTick()
    {
        player.ApplySlideMovement();
    }

    public override void OnCrouchPressed()
    {
        if (player.LeaveCrouchCheck())
        {
            TransitionToIdleOrWalk();
        }
    }

    public override void OnJumpPressed()
    {
        if (player.LeaveCrouchCheck())
        {
            base.OnJumpPressed();
        }
    }

    public override void OnSprintPressed()
    {
        if (player.LeaveCrouchCheck())
        {
            base.OnSprintPressed();
        }
    }
    public override void OnGrounded()
    {
        //base.OnGrounded();
        lastGroundedTime = Time.fixedTime;
        Debug.Log($"[SlideState] Grounded at {lastGroundedTime}");
    }
    public override void OnFallDetected()
    {
        float timeSinceGrounded = Time.fixedTime - lastGroundedTime;
        Debug.Log($"[SlideState] Fall detected. Time since grounded: {timeSinceGrounded}");

        if (timeSinceGrounded < slideFallGraceTime)
        {
            Debug.Log("[SlideState] Within grace period. Ignoring fall.");
            return;
        }

        Debug.Log("[SlideState] Grace period expired. Triggering fall.");
        player.ApplySlideEdgeBoost();
        StateMachine.ChangeState(player.FallingState);
    }
}

// --- VaultState.cs ---
public class VaultState : GroundedState
{
    private Vector3 vaultStartPos;
    private Vector3 vaultTarget;
    private float vaultStartTime;
    public VaultState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Vault;
    public void BeginVault(Vector3 targetPosition)
    {
        vaultStartTime = Time.time;
        vaultStartPos = player.transform.position;
        vaultTarget = targetPosition;
    }

    public override void FixedTick()
    {
        float t = (Time.time - vaultStartTime) / player.VaultDuration;
        t = Mathf.Clamp01(t);

        Vector3 pos = Vector3.Lerp(vaultStartPos, vaultTarget, Mathf.SmoothStep(0f, 1f, t));
        player.Rb.MovePosition(pos);

        // If we've reached or nearly reached the target position:
        if (t >= 1f || Vector3.Distance(player.transform.position, vaultTarget) < 0.1f)
        {
            TransitionToCrouchOrSlide();
        }
    }
}

/// <summary>
/// Holds shared functionality between airborne states.
/// </summary>
public abstract class AirborneState : PlayerState
{
    protected AirborneState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    private bool crouchedWhileFalling = false;
    public override void Enter()
    {
        base.Enter();
        crouchedWhileFalling = false;
    }
    public override void FixedTick()
    {
        player.ApplyMove();

        if (player.WallRunEnabled)
            player.WallRunHandler.UpdateGroundedStatus(false);

        player.CheckForBufferedJump();
    }

    public override void OnMoveInput()
    {
        player.WallRunHandler.UpdatePlayerInputDir(player.GetFlatInputDir());
    }
    public override void OnCrouchPressed()
    {
        crouchedWhileFalling = true;
        player.SetCrouchVisuals(true, true);
    }
    public override void OnGrounded()
    {
        player.CheckForBufferedJump();

        base.OnGrounded();
        if (crouchedWhileFalling)
        {
            Debug.Log("grounded detected while in airborne while crouched state");
            TransitionToCrouchOrSlide();
        }
        else
        {
            TransitionToIdleOrWalk();
        }
    }
    public override void OnSprintPressed()
    {
        // Sprint does nothing while airborne
    }
}
public class JumpState : AirborneState
{
    private float jumpStartTime;
    public JumpState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Jump;
    public override void Enter()
    {
        base.Enter();
        jumpStartTime = Time.time;
    }
    public override void FixedTick()
    {
        base.FixedTick();

        // Automatically transition to FallingState after jump animation time
        if (Time.time - jumpStartTime > player.JumpDuration)
            StateMachine.ChangeState(player.FallingState);
    }
    public override void OnJumpPressed()
    {
        // Intentionally left blank — jumping does nothing here
        // Prevents spamming all jump charges during the same jump state.
    }
}

// --- FallingState.cs ---
public class FallingState : AirborneState
{
    public FallingState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.Fall;
    public override void OnJumpPressed()
    {
        player.SetJumpBuffer();
        player.CheckForBufferedJump();
    }
}

public class CrouchedFallingState : FallingState
{
    public CrouchedFallingState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.CrouchAirborne;
}

public class WallrunState : PlayerState
{
    public WallrunState(StateMachine sm, Zia_PlayerMovement player) : base(sm, player) { }
    public override PlayerStateType StateType => PlayerStateType.WallRun;
    public override void FixedTick()
    {
        player.WallRunHandler.UpdatePlayerInputDir(player.GetFlatInputDir());

        if (!player.WallRunHandler.IsWallrunning)
            StateMachine.ChangeState(player.FallingState);
    }
    public override void Exit()
    {
        base.Exit();
        player.SetCrouchVisuals(false, !player.IsGrounded);
    }

    public override void OnJumpPressed()
    {
        player.WallRunHandler.ActiveWallExit();
        player.ExecuteJump();
    }
    public override void OnCrouchPressed()
    {
        player.WallRunHandler.ForceCancelWallRun();
        StateMachine.ChangeState(player.FallingState);
    }
    public override void OnGrounded()
    {
        TransitionToIdleOrWalk();
        player.WallRunHandler.ForceCancelWallRun();
    }

    public override void OnSprintPressed()
    {
        // Intentionally left blank — sprinting does nothing here
    }
}

[System.Serializable]
public enum PlayerStateType
{
    Unknown,
    Idle,
    Walk,
    Sprint,
    Slide,
    Jump,
    Fall,
    Crouch,
    Vault,
    WallRun,
    CrouchWalk,
    CrouchAirborne
    // Add others as needed
}
