using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityHandler))]
public class PullableObject : MonoBehaviour
{
    private Rigidbody rb;
    private GravityHandler gravityHandler;

    [SerializeField] private Vector3 startingVelocity = Vector3.zero;
    public bool HasBeenPulled { get; private set; } = false;

    void Awake()
    {
        // Get the Rigidbody component attached to the object
        rb = GetComponent<Rigidbody>();

        // Get the GravityHandler component attached to the object
        gravityHandler = GetComponent<GravityHandler>();

        // Disable gravity and the GravityHandler by default
        gravityHandler.enabled = false;

        // Set the initial velocity
        rb.velocity = startingVelocity;
    }

    // Method to be called when the object is pulled
    public void OnPull()
    {
        // Enable the GravityHandler if it's disabled
        if (gravityHandler != null && !gravityHandler.enabled)
        {
            gravityHandler.enabled = true;
        }

        // Set the object as pulled
        HasBeenPulled = true;
    }

    public void AddPullForce(Vector3 pullForce)
    {
        rb.AddForce(pullForce, ForceMode.Force);
    }
}