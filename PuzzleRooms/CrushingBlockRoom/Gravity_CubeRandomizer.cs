using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Gravity_CubeRandomizer : GravitySource
{
    public float timerInterval = 5f; // Time in seconds between direction picks
    private Vector3 currentDirection;
    private BoxCollider boxCollider;
    private List<Vector3> directions;
    private Vector3 currentDir = Vector3.zero;
    private Vector3 lastDir = Vector3.zero;

    private Coroutine directionPickerCoroutine;

    public delegate void DirectionChangeHandler(Vector3 newDirection);
    public event DirectionChangeHandler OnDirectionChange;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.enabled = true;
        boxCollider.isTrigger = true;

        base.requireGravityUpdates = true;

        // Initialize the possible directions
        directions = new List<Vector3>
        {
            transform.up,
            -transform.up,
            transform.right,
            -transform.right,
            transform.forward,
            -transform.forward
        };

        // Start the timer coroutine
        StartCoroutine(PickRandomDirection());

        // Assign initial gravity dir value;
        SetGravityDir(-this.transform.up);
    }

    private void Update()
    {
        // Any additional logic you might need in Update
    }

    IEnumerator PickRandomDirection()
    {
        while (true)
        {
            // Wait for the specified interval
            yield return new WaitForSeconds(timerInterval);

            // Filter out the current direction from the list of possible directions
            List<Vector3> availableDirections = new List<Vector3>(directions);
            availableDirections.Remove(currentDirection);

            // Pick a random direction from the filtered list
            Vector3 newDirection = availableDirections[Random.Range(0, availableDirections.Count)];

            SetGravityDir(newDirection);
        }
    }

    private void SetGravityDir(Vector3 newDirection)
    {
        // Update the current direction and invoke the event
        currentDirection = newDirection;

        // Log the selected direction for debugging
        Debug.Log("Selected Direction: " + currentDirection);

        // Invoke the event to notify listeners of the direction change
        OnDirectionChange?.Invoke(currentDirection);
    }

    protected override Vector3 CalculateGravity(Transform targetObject)
    {
        return currentDirection * gravityStrength;
    }

    protected override Quaternion CalculateRotation(Transform targetObject)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(targetObject.up, -currentDirection.normalized) * targetObject.rotation;
        return targetRotation;
    }

    public void StartDirectionPicking()
    {
        if (directionPickerCoroutine == null)
        {
            directionPickerCoroutine = StartCoroutine(PickRandomDirection());
        }
    }

    public void PauseDirectionPicking()
    {
        if (directionPickerCoroutine != null)
        {
            StopCoroutine(directionPickerCoroutine);
            directionPickerCoroutine = null;
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Draw lines to represent the gravity directions
        foreach (var direction in directions)
        {
            Vector3 dir = direction * 10; // Scale for visibility
            Debug.DrawLine(sourceObject.position, sourceObject.position + dir, Color.blue);
        }
    }
}
