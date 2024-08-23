using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private playerMainState baseMainState = playerMainState.Alive;
    [SerializeField] private playerSubState baseSubState = playerSubState.CharacterControl;

    public event Action<playerMainState, playerSubState> OnStateChange;

    public enum playerMainState
    {
        Alive,
        Dead,
        Respawning,
        Loading,
        Paused
    }

    public enum playerSubState
    {
        CharacterControl,
        InventoryControl
    }

    public enum playerActiveState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Meleeing,
        Shooting,
        Reloading
    }

    private playerMainState currentMainState;
    private playerSubState currentSubState;

    public void SetState(playerMainState mainState)
    {
        currentMainState = mainState;
        OnStateChange?.Invoke(currentMainState, currentSubState);
    }

    public void SetState(playerSubState subState)
    {
        currentSubState = subState;
        OnStateChange?.Invoke(currentMainState, currentSubState);
    }
    public void SetState(playerMainState mainState, playerSubState subState)
    {
        currentMainState = mainState;
        currentSubState = subState;
        OnStateChange?.Invoke(currentMainState, currentSubState);
    }

    private void Awake()
    {
        Invoke("SendInitialState", 0.1f);
    }

    private void SendInitialState()
    {
        SetState(baseMainState, baseSubState);
    }
}