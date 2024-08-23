using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static PlayerStateMachine;

[RequireComponent(typeof(PlayerStateMachine))]
public class UICursorController : MonoBehaviour
{
    private Vector3 lastCursorPosition;
    private GameObject currentlySelectedObject;
    private GameObject previouslySelectedObject;

    PlayerStateMachine playerState;

    bool isActive = false;

    public event Action<GameObject> OnElementClick;
    public event Action<GameObject> OnElementRightClick;
    public event Action<GameObject> OnElementHoverEnter;
    public event Action<GameObject> OnElementHoverExit;
    void Awake()
    {
        lastCursorPosition = Input.mousePosition;

        playerState = GetComponent<PlayerStateMachine>();
        playerState.OnStateChange += HandleStateChange;
    }

    private void OnDestroy()
    {
        playerState.OnStateChange -= HandleStateChange;
    }

    private void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        if (subState == PlayerStateMachine.playerSubState.InventoryControl)
        {
            isActive = true;
        }
        else
        {
            isActive = false;
        }
    }
    void Update()
    {
        if (!isActive) { return; }

        // Check if the cursor has moved since the last frame
        if (Input.mousePosition != lastCursorPosition)
        {
            // Use the PointerIsOverUI method from the RaycastUtilities class
            if (RaycastUtilities.PointerIsOverUI(Input.mousePosition))
            {
                // Use the UIRaycast method from the RaycastUtilities class
                previouslySelectedObject = currentlySelectedObject;
                currentlySelectedObject = RaycastUtilities.UIRaycast(ScreenPosToPointerData(Input.mousePosition));

                if (currentlySelectedObject != null && previouslySelectedObject != currentlySelectedObject)
                {
                    OnElementHoverEnter?.Invoke(currentlySelectedObject);

                    if (previouslySelectedObject != null)
                    {
                        //Debug.Log("exited this UI element : " + previouslySelectedObject.name);
                        OnElementHoverExit?.Invoke(previouslySelectedObject);
                    }
                }
            }

            // Update the last cursor position
            lastCursorPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (currentlySelectedObject != null)
            {
                OnElementClick?.Invoke(currentlySelectedObject);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (currentlySelectedObject != null)
            {
                OnElementRightClick?.Invoke(currentlySelectedObject);
            }
        }
    }

    private PointerEventData ScreenPosToPointerData(Vector2 screenPos)
    {
        return new PointerEventData(EventSystem.current) { position = screenPos };
    }
}

public static class RaycastUtilities
{
    public static bool PointerIsOverUI(Vector2 screenPos)
    {
        var hitObject = UIRaycast(ScreenPosToPointerData(screenPos));
        return hitObject != null && hitObject.layer == LayerMask.NameToLayer("UI");
    }

    public static GameObject UIRaycast(PointerEventData pointerData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count < 1 ? null : results[0].gameObject;
    }

    public static PointerEventData ScreenPosToPointerData(Vector2 screenPos)
        => new(EventSystem.current) { position = screenPos };
}

