using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class MovingObject : MonoBehaviour
{
    [SerializeField] public Vector3 velocity = Vector3.zero;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void Update()
    {
        LerpPosition();
    }
    // Method to get the current velocity of the object
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    // Method to get the current rotation speed of the object
    public float GetRotationSpeed()
    {
        // For simplicity, we are using angular velocity magnitude as rotation speed
        return rb.angularVelocity.magnitude;
    }

    // Method to perform position lerp based on initial velocity
    private void LerpPosition()
    {
        // Calculate the new position based on the initial velocity
        Vector3 newPosition = transform.position + velocity;

        // Lerp the position
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime);
    }
}
