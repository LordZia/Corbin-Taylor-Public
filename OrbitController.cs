using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(PathFollower))]
public class OrbitController : MonoBehaviour
{
    public Transform centerObject; // The object to orbit around
    public float orbitSpeed = 2f; // Speed of orbit in degrees per second

    private float radius;

    private List<Vector3> orbitPath = new List<Vector3>();

    private void Awake()
    {
        radius = Vector3.Distance(this.transform.position, centerObject.position);
        Quaternion rotation = Quaternion.LookRotation(transform.position - centerObject.position, Vector3.up);

        CalculatePath(centerObject.position, radius, rotation);
        GetComponent<PathFollower>().SetPath(orbitPath);
    }
    private void OnDrawGizmosSelected()
    {
        // Draw a gizmo circle when the object is selected
        if (Application.isPlaying)
        {
            Utility.DrawLoop(orbitPath);
            return;
        }

        // If in scene view calculate a prediction of the path
        Gizmos.color = Color.yellow;
        radius = Vector3.Distance(this.transform.position, centerObject.position);
        Quaternion rotation = Quaternion.LookRotation(transform.position - centerObject.position, Vector3.up);
        DrawOrbitCircle(centerObject.position, radius, rotation);
    }

    private void CalculatePath(Vector3 center, float radius, Quaternion rotation)
    {
        int circleSegments = 100;
        float angleIncrement = 360f / circleSegments;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * angleIncrement;
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            Vector3 pointOnCircle = center + rotation * new Vector3(x, 0f, z);
            Vector3 nextPointOnCircle = center + rotation * new Vector3(Mathf.Cos(Mathf.Deg2Rad * (angle + angleIncrement)) * radius, 0f, Mathf.Sin(Mathf.Deg2Rad * (angle + angleIncrement)) * radius);
            orbitPath.Add(nextPointOnCircle);
        }
    }
    private void DrawOrbitCircle(Vector3 center, float radius, Quaternion rotation)
    {
        int circleSegments = 100;
        float angleIncrement = 360f / circleSegments;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * angleIncrement;
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            Vector3 pointOnCircle = center + rotation * new Vector3(x, 0f, z);
            Vector3 nextPointOnCircle = center + rotation * new Vector3(Mathf.Cos(Mathf.Deg2Rad * (angle + angleIncrement)) * radius, 0f, Mathf.Sin(Mathf.Deg2Rad * (angle + angleIncrement)) * radius);
            Gizmos.DrawLine(pointOnCircle, nextPointOnCircle);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Orbit around the centerObject
        if (centerObject != null)
        {
            Orbit();
        }
    }

    void Orbit()
    {
        // Calculate the orbit rotation
        Quaternion orbitRotation = Quaternion.Euler(0f, orbitSpeed * Time.deltaTime, 0f);

        // Apply the rotation around the centerObject
        transform.RotateAround(centerObject.position, Vector3.up, orbitSpeed * Time.deltaTime);
    }
}



