using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class CrushingBlock : MonoBehaviour
{
    [Header("This class relies on CrushingBlockManager, add this object to a manager's list")]
    [SerializeField] private float crushDistance = 5f;
    private Vector3 halfScale;

    private CrushingBlockRoomManager roomManager;

    public void InitializeBlock(CrushingBlockRoomManager roomManager)
    {
        this.roomManager = roomManager;
        Vector3 scale = transform.localScale;
        halfScale = scale * 0.5f;
    }

    /*
    public void PerformCrushingChecks(Vector3 directionOfMotion)
    {   
        // Calculate the center position of each face
        Vector3 frontCenter = transform.position + transform.forward * halfScale.z;
        Vector3 backCenter = transform.position - transform.forward * halfScale.z;
        Vector3 topCenter = transform.position + transform.up * halfScale.y;
        Vector3 bottomCenter = transform.position - transform.up * halfScale.y;
        Vector3 leftCenter = transform.position - transform.right * halfScale.x;
        Vector3 rightCenter = transform.position + transform.right * halfScale.x;

        // Perform raycasting from each face's center and draw debug lines
        RaycastFromCenter(frontCenter, transform.forward, halfScale.z);
        RaycastFromCenter(backCenter, -transform.forward, halfScale.z);
        RaycastFromCenter(topCenter, transform.up, halfScale.y);
        RaycastFromCenter(bottomCenter, -transform.up, halfScale.y);
        RaycastFromCenter(leftCenter, -transform.right, halfScale.x);
        RaycastFromCenter(rightCenter, transform.right, halfScale.x);
    }
    */
    public void PerformCrushingChecks(Vector3 directionOfMotion)
    {
        // Calculate the center position of the face along the direction of motion
        Vector3 centerPositive = transform.position + Vector3.Scale(directionOfMotion, halfScale);
        Vector3 centerNegative = transform.position - Vector3.Scale(directionOfMotion, halfScale);

        // Perform raycasting from each face's center in the direction of motion and its opposite direction
        RaycastInDirection(centerPositive, directionOfMotion);
        RaycastInDirection(centerNegative, -directionOfMotion);
    }
    
    void RaycastInDirection(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 endPos = startPoint + direction * crushDistance;

        if (Physics.Raycast(startPoint, direction, out hit, crushDistance) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Perform a physics cube check
            bool hitEntity = CheckBoxRegion(direction, startPoint, this.transform.lossyScale, halfScale.magnitude, crushDistance);

            if (hitEntity)
            {
                Debug.Log("Entity hit by cube check!");
            }
        }

        // Draw debug line
        Debug.DrawLine(startPoint, endPos, Color.red);
    }

    private bool CheckBoxRegion(Vector3 faceOrientation, Vector3 faceCenter, Vector3 boxDimensions, float faceDistance, float castDistance)
    {
        // Normalize the orientation to always be positive
        Vector3 roundedFaceOrientation = RoundToNearestAxis(faceOrientation);
        Vector3 posFaceOrientation = new Vector3(Mathf.Abs(roundedFaceOrientation.x), Mathf.Abs(roundedFaceOrientation.y), Mathf.Abs(roundedFaceOrientation.z));
        Vector3 checkRegionDimensions;
        Vector3 checkRegionPosition = faceCenter + (faceOrientation * (castDistance /2));

        if (posFaceOrientation == Vector3.up)
        {
            Debug.Log("Detected cast direction: " + posFaceOrientation);
            checkRegionDimensions = new Vector3(boxDimensions.x, castDistance, boxDimensions.z);
        }
        else if (posFaceOrientation == Vector3.right)
        {
            Debug.Log("Detected cast direction: " + posFaceOrientation);
            checkRegionDimensions = new Vector3(castDistance, boxDimensions.y, boxDimensions.z);
        }
        else if (posFaceOrientation == Vector3.forward)
        {
            Debug.Log("Detected cast direction: " + posFaceOrientation);
            checkRegionDimensions = new Vector3(boxDimensions.x, boxDimensions.y, castDistance);
        }
        else
        {
            // Default case handling
            Debug.Log("Did not detect cast direction: " + posFaceOrientation);
            return false;
        }

        Utility.DrawCube(checkRegionPosition, Vector3.one * 1, this.transform.rotation, Color.magenta, 0);

        DrawDebugCube(checkRegionPosition, checkRegionDimensions, transform.rotation);
        return Physics.CheckBox(checkRegionPosition, checkRegionDimensions * 0.5f, transform.rotation, LayerMask.GetMask("Entity"));

    }

    void DrawDebugCube(Vector3 center, Vector3 dimensions, Quaternion rotation)
    {
        Vector3 halfDimensions = dimensions * 0.5f;

        Vector3 frontBottomLeft = center + rotation * new Vector3(-halfDimensions.x, -halfDimensions.y, -halfDimensions.z);
        Vector3 frontBottomRight = center + rotation * new Vector3(halfDimensions.x, -halfDimensions.y, -halfDimensions.z);
        Vector3 frontTopLeft = center + rotation * new Vector3(-halfDimensions.x, halfDimensions.y, -halfDimensions.z);
        Vector3 frontTopRight = center + rotation * new Vector3(halfDimensions.x, halfDimensions.y, -halfDimensions.z);

        Vector3 backBottomLeft = center + rotation * new Vector3(-halfDimensions.x, -halfDimensions.y, halfDimensions.z);
        Vector3 backBottomRight = center + rotation * new Vector3(halfDimensions.x, -halfDimensions.y, halfDimensions.z);
        Vector3 backTopLeft = center + rotation * new Vector3(-halfDimensions.x, halfDimensions.y, halfDimensions.z);
        Vector3 backTopRight = center + rotation * new Vector3(halfDimensions.x, halfDimensions.y, halfDimensions.z);

        // Draw bottom face
        Debug.DrawLine(frontBottomLeft, frontBottomRight, Color.blue);
        Debug.DrawLine(frontBottomRight, backBottomRight, Color.blue);
        Debug.DrawLine(backBottomRight, backBottomLeft, Color.blue);
        Debug.DrawLine(backBottomLeft, frontBottomLeft, Color.blue);

        // Draw top face
        Debug.DrawLine(frontTopLeft, frontTopRight, Color.blue);
        Debug.DrawLine(frontTopRight, backTopRight, Color.blue);
        Debug.DrawLine(backTopRight, backTopLeft, Color.blue);
        Debug.DrawLine(backTopLeft, frontTopLeft, Color.blue);

        // Draw vertical edges
        Debug.DrawLine(frontBottomLeft, frontTopLeft, Color.blue);
        Debug.DrawLine(frontBottomRight, frontTopRight, Color.blue);
        Debug.DrawLine(backBottomLeft, backTopLeft, Color.blue);
        Debug.DrawLine(backBottomRight, backTopRight, Color.blue);
    }

    private Vector3 RoundToNearestAxis(Vector3 vector)
    {
        return new Vector3(
            Mathf.Round(vector.x),
            Mathf.Round(vector.y),
            Mathf.Round(vector.z)
        );
    }
}
