using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBlock : MonoBehaviour
{
    public Color activeColor = Color.red; // Color when the object is active
    public Color inactiveColor = Color.blue; // Color when the object is inactive

    [SerializeField] private Renderer objectRenderer; // Renderer component of the object
    private bool isActive = false; // Current state of the object

    void Start()
    {
        // Get the Renderer component of the object
        objectRenderer = this.transform.parent.GetComponent<Renderer>();

        // Check if the object has a Renderer component
        if (objectRenderer == null)
        {
            Debug.LogError("Object does not have a Renderer component!");
        }

        // Set the initial color based on the initial state
        objectRenderer.material.color = inactiveColor;
    }

    // OnTriggerEnter is called when the Collider other enters the trigger
    void OnTriggerEnter(Collider other)
    {
        // Toggle the state between active and inactive
        isActive = !isActive;

        // Change the color based on the current state
        objectRenderer.material.color = isActive ? activeColor : inactiveColor;
    }
}
