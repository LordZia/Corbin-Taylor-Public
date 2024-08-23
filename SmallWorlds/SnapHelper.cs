using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if (UNITY_EDITOR) 
public class SnapHelper : MonoBehaviour
{
    [SerializeField] public GravitySource gravitySource;
    [SerializeField] private bool snappingEnabled = false; // Flag to control snapping behavior
    [SerializeField] private SnappingType snappingType = SnappingType.ToNormal;
    [SerializeField] private LayerMask ignoredNormalLayer;
    public void ToggleSnapEnabled()
    {
        snappingEnabled = !snappingEnabled; // Toggle the flag
    }

    public void PointInDirection(Vector3 direction)
    {
        // Get the rotation quaternion that points the object's -transform.up in the specified direction
        Quaternion rotation = Quaternion.FromToRotation(-transform.up, direction);

        // Apply the rotation to the object's rotation
        transform.rotation = rotation * transform.rotation;
    }

    public bool IsSnapEnabled()
    {
        return snappingEnabled;
    }

    public void HandleSnap()
    {
        switch (snappingType)
        {
            case SnappingType.None:
                break;
            case SnappingType.ToGravity:
                SnapToGravity();
                break;
            case SnappingType.ToNormal:
                SnapToNormal();
                break;
            default:
                break;
        }
    }

    private void SnapToGravity()
    {
        if (gravitySource == null)
        {
            Debug.LogError("snap helper recieved a call to snap to gravity, no gravity source is assigned in the inspector, assign one");
            return;
        }
        // Calculate the direction based on the gravity source
        Vector3 downDir = gravitySource.GetGravity(this.transform).normalized;
        PointInDirection(downDir);
    }
    private void SnapToNormal()
    {
        // This works fine as it gets you the correct coordinates
        Vector2 mousePosition = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log(hit.point.x.ToString());
            // Draw a pointing upwards where we clicked. Just to make sure we SEE that the coordinates are correct.
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 5f, Color.cyan, 0.1f);

            this.transform.position = hit.point;
            PointInDirection(-hit.normal);
        }

    }
}
public enum SnappingType
{
    None,
    ToGravity,
    ToNormal
}
#endif