using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CylinderTrigger))]
public class Gravity_Cylinder : GravitySource
{
    [SerializeField] Transform cylinderTransform; // Replace with your cylinder's transform

    [SerializeField] private Vector3 topPos = Vector3.zero;
    [SerializeField] private Vector3 bottomPos = Vector3.zero;
    
    void Awake()
    {
        // Ensure the provided transform is not null
        if (cylinderTransform == null)
        {
            Debug.LogError("Cylinder transform is null.");
            topPos = bottomPos = Vector3.zero;
            return;
        }

        GetCylinderPoints(cylinderTransform, out Vector3 topCenter, out Vector3 bottomCenter);
        topPos = topCenter;
        bottomPos = bottomCenter;

        base.requireGravityUpdates = true;

        if (customTrigger != null)
        {
            SubscribeToCustomTrigger();
        }
    }
    private void OnDestroy()
    {
        UnsubscribeToCustomTrigger();
    }

    protected override Vector3 CalculateGravity(Transform targetObject)
    {
        Vector3 closestPos = Utility.ClosestPointOnLine(topPos, bottomPos, targetObject.position);
        Vector3 gravityDir = (closestPos - targetObject.transform.position).normalized;
        Vector3 customGravity = gravityDir * base.gravityStrength * -1f;

        Debug.DrawLine(targetObject.position, closestPos, Color.red);

        return customGravity;
    }
    public static void GetCylinderPoints(Transform cylinderTransform, out Vector3 topCenter, out Vector3 bottomCenter)
    {
        // Get the position, rotation, and scale of the cylinder
        Vector3 position = cylinderTransform.position;
        Quaternion rotation = cylinderTransform.rotation;
        Vector3 scale = cylinderTransform.lossyScale;

        // Calculate the height of the cylinder
        float height = scale.y;

        // Calculate the local positions of the top and bottom center points
        Vector3 localTopCenter = new Vector3(0, height, 0);
        Vector3 localBottomCenter = new Vector3(0, -height, 0);

        // Transform the local positions to world positions
        topCenter = position + rotation * localTopCenter;
        bottomCenter = position + rotation * localBottomCenter;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(topPos, 2);
        Gizmos.DrawWireSphere(bottomPos, 2);
        Gizmos.DrawLine(topPos, bottomPos);
    }
    
}