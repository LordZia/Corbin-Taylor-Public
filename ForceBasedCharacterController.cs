using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ForceBasedCharacterController : MonoBehaviour
{
    private CharacterController characterController;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 velocity;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float friction = 2f;
    [SerializeField] private float rotationSpeed = 2f;

    public Transform gravityPoint;
    public float gravityAmount = 9.8f;
    private Vector3 gravityDir = Vector3.zero;

    private float pitchClamp = 90f;
    private float xRotation = 0f;


    private int frameCounter = 0;
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector3 gravityDirection = (gravityPoint.position - transform.position).normalized;
        gravityDir = gravityDirection;

        // Rotate the player to align with the gravity direction
        RotatePlayer(gravityDirection);
        // Rotate player based on mouse input
        RotatePlayerWithMouse();

        // Calculate forces
        Vector3 forces = CalculateForces();

        // Apply forces to velocity
        velocity += forces * Time.deltaTime;

        // Apply friction
        if (characterController.isGrounded)
        {
            velocity *= Mathf.Pow(1.0f - friction * Time.deltaTime, 2.0f);
            if (velocity.magnitude <= 0.1f) velocity = Vector3.zero;
        }

        // Apply gravity towards the gravity point
        velocity += (gravityDirection * gravityAmount) * Time.deltaTime;

        // Smoothly rotate the velocity towards the gravity source
        //Vector3 targetVelocity = Vector3.Slerp(velocity, gravityDirection, gravityAmount);
        //velocity = Vector3.Lerp(velocity, targetVelocity, gravityAmount * Time.deltaTime);

        //Vector3 targetVelocity = Vector3.Lerp(velocity, gravityDirection * gravityAmount, gravityAmount * Time.deltaTime);
        //velocity = targetVelocity;

        // Debug lines for visualization
        //Debug.DrawLine(transform.position, transform.position + gravityDirection * 5, Color.yellow);
        Debug.DrawLine(transform.position, transform.position + velocity.normalized * 5, Color.magenta);

        // Apply velocity to the character controller
        characterController.Move(velocity);

        Debug.DrawLine(transform.position, transform.position + characterController.velocity.normalized * 5, Color.blue);

        //debug trail draw
        if (frameCounter <= 10)
        {
            frameCounter++;
        }
        else
        {
            Color color = Color.blue;
            if (characterController.isGrounded) color = Color.red;
            Utility.DrawCube(transform.position, Vector3.one * 0.25f, Quaternion.identity, color, 10);
        }

    }

    Vector3 CalculateForces()
    {
        if (!characterController.isGrounded) return Vector3.zero;
        // Example: Input-based movement
        Vector3 inputForce = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        // Transform input force to world space based on player's rotation
        inputForce = transform.TransformDirection(inputForce) * acceleration;

        // Add any other forces you need (e.g., jumping, external forces)

        return inputForce;
    }


    Vector3 CalculateForcesWithGravity(Vector3 gravityDirection)
    {
        // Example: Input-based movement
        Vector3 inputForce = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        // Transform input force to world space based on player's rotation
        inputForce = transform.TransformDirection(inputForce) * acceleration;

        // Calculate gravitational force component
        Vector3 gravityForce = gravityDirection * gravityAmount;

        // Add any other forces you need (e.g., jumping, external forces)
        Vector3 totalForce = inputForce + gravityForce;

        return totalForce;
    }

    void RotatePlayer(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.FromToRotation(-transform.up, targetDirection) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 5f * Time.deltaTime);
        }
    }

    void RotatePlayerWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(transform.up * mouseX);
    }
}

