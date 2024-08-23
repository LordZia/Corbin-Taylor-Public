using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using static PlayerStateMachine;

public class InteractableDetector : MonoBehaviour
{
    [SerializeField] private float raycastRange = 5f;
    [SerializeField] private LayerMask ignoreLayer; // Define the layer for player objects
    [SerializeField] private GameObject interactableDescriptionDisplay;
    [SerializeField] private Camera playerCamera;
    IInteractable interactable;
    IInteractable lastInteractableObject;
    Vector3 interactablePosition;

    List<GameObject> nearbyInteractables = new List<GameObject>();

    [SerializeField] private int interactableDetectionRefreshRate = 20; // fixed update calls per detection check
    private int interactableDetectionCounter = 0;

    PlayerStateMachine playerStateMachine;
    PlayerStateMachine.playerSubState currentPlayerState = playerSubState.CharacterControl;

    TextMeshProUGUI displayField;
    private void Awake()
    {
        if (playerCamera== null)
        {
            Debug.LogError("Player camera reference not assigned for itneractable detector assign one in the inspector");
        }

        playerStateMachine = this.GetComponent<PlayerStateMachine>();
        playerStateMachine.OnStateChange += HandleStateChange;

        displayField = interactableDescriptionDisplay.GetComponent<TextMeshProUGUI>();
    }

    private void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        currentPlayerState = subState;
    }
    private void FixedUpdate()
    {
        if (currentPlayerState != playerSubState.CharacterControl)
        {
            interactable = null;
            return; // only check for interactables if player is alive.
        }

        if (interactableDetectionCounter < interactableDetectionRefreshRate)
        {
            interactableDetectionCounter += 1;
            return;
        }

        interactableDetectionCounter = 0;

        CheckRegionForInteractables();
        GameObject interactableTarget = CheckInteractables(playerCamera.transform.position, playerCamera.transform.forward);
        if (interactableTarget == null)
        {
            interactable = null;
            return;
        }

        interactable = interactableTarget.GetComponent<IInteractable>();

        /*
        LayerMask layerMask = ~ignoreLayer;

        // Perform a raycast forward from the transform's position
        RaycastHit hit;
        if (Physics.Raycast(transform.position, playerCamera.transform.forward, out hit, raycastRange, layerMask))
        {
            // Check if the hit object has the "IInteractable" interface
            interactable = hit.collider.GetComponent<IInteractable>();
            Debug.Log(hit.collider.name);
        }
        
        if (hit.collider == null)
        {
            interactable = null;
        }

        interactablePosition = hit.point;
        */
    }


    private void Update()
    {
        displayField.text = "";
        if (interactable == null) return;


        if (Input.GetKeyDown(KeyCode.E))
        {
            interactable.OnInteract(this.gameObject);
        }

        UpdateInteractDisplay();
        lastInteractableObject = interactable;
    }

    private void UpdateInteractDisplay()
    {
        displayField.text = interactable.InteractDescription();
    }
    private GameObject CheckInteractables(Vector3 pos, Vector3 lookDir)
    {
        float threshold = 0.95f;
        GameObject interactableTarget = null;
        var closest = 0f;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            Vector3 vector1 = lookDir;
            Vector3 vector2 = nearbyInteractables[i].transform.position - pos;

            float lookPercentage = Vector3.Dot(vector1.normalized, vector2.normalized);

            if (lookPercentage > threshold && lookPercentage > closest) 
            {
                closest= lookPercentage;
                interactableTarget = nearbyInteractables[i].transform.gameObject;
            }
        }

        return interactableTarget;
    }

    private void CheckRegionForInteractables()
    {
        nearbyInteractables.Clear();

        // Get colliders within the detection radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, raycastRange);

        // Iterate through the colliders
        foreach (Collider col in colliders)
        {
            // Check if the collider's GameObject has a component that implements the interface
            IInteractable interactableObject = col.gameObject.GetComponent<IInteractable>();

            if (interactableObject != null)
            {
                // Object implements the interface, add it to the list
                nearbyInteractables.Add(col.gameObject);
            }
        }
    }
}
