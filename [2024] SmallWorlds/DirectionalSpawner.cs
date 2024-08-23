using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DirectionalSpawner : ObjectSpawner
{
    [SerializeField] public Transform spawnPoint;
    [SerializeField] public Transform endPoint;

    [SerializeField] private float initialSpeed;
    [SerializeField] private float randomSpawnRadius = 5;

    private Vector3 direction;
    private Vector3 spawnForce;

    private Vector3 spawnCenterPos;

    private void Awake()
    {
        direction = endPoint.position - spawnPoint.position;
        spawnForce = direction.normalized * initialSpeed;

        spawnCenterPos = spawnPoint.position;

        base.OnAwake();
    }
    protected override void OnObjectSpawn(GameObject _spawnedObject)
    {
        MovingObject movingObj = _spawnedObject.GetComponent<MovingObject>();
        Rigidbody rB = _spawnedObject.GetComponent<Rigidbody>();
        if (movingObj != null)
        {
            movingObj.velocity = spawnForce;
        }
        else if (rB != null)
        {
            rB.AddForce(spawnForce * 60, ForceMode.Force);
            //_spawnedObject.AddComponent<MovingObject>().velocity = spawnForce;
        }

        _spawnedObject.transform.position = AddRandomAmount(spawnCenterPos, randomSpawnRadius * 0.8f);

        float lifetime = CalculateTravelTime(spawnCenterPos, endPoint.position, initialSpeed);

        base.objectPool.DeactivateObjectDelayed(_spawnedObject, lifetime);
    }
    private static float CalculateTravelTime(Vector3 startPoint, Vector3 endPoint, float speed)
    {
        // Calculate the distance between the start and end points
        float distance = Vector3.Distance(startPoint, endPoint);

        // Calculate the time required to travel the distance at the given speed
        float time = distance / speed;

        return time;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint.position, randomSpawnRadius);
        Gizmos.DrawLine(spawnPoint.position, endPoint.position);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPoint.position, randomSpawnRadius);

        Gizmos.color = Color.blue;
        Quaternion lineRotation = CircleCalculator.CalculateLineRotation(spawnPoint.position, endPoint.position);
        Vector3[] linePointsSpawn = CircleCalculator.CalculateCirclePoints(spawnPoint.position, lineRotation, randomSpawnRadius);
        Utility.DrawLoop(linePointsSpawn.ToList());

        Vector3[] linePointsEnd = CircleCalculator.CalculateCirclePoints(endPoint.position, lineRotation, randomSpawnRadius);
        Utility.DrawLoop(linePointsEnd.ToList());

        for (int i = 0; i < linePointsSpawn.Count(); i++)
        {
            Gizmos.DrawLine(linePointsSpawn[i], linePointsEnd[i]);
        }
    }

    // Method to add a random amount to a Vector3 based on an input radius
    public static Vector3 AddRandomAmount(Vector3 vector, float radius)
    {
        // Generate random values for x, y, and z components within the radius
        float randomX = Random.Range(-radius, radius);
        float randomY = Random.Range(-radius, radius);
        float randomZ = Random.Range(-radius, radius);

        // Add the random values to the input vector's components
        Vector3 randomVector = vector + new Vector3(randomX, randomY, randomZ);

        return randomVector;
    }
}

public class CircleCalculator : MonoBehaviour
{
    public static Vector3[] CalculateCirclePoints(Vector3 position, Quaternion rotation, float circleRadius)
    {
        // remap radius to display more line segments with larger radiuses
        float inputMin = 10f;
        float inputMax = 100f;
        int outputMin = 3;
        int outputMax = 8;

        circleRadius = Mathf.Clamp(circleRadius, inputMin, inputMax);

        float t = (circleRadius - inputMin) / (inputMax - inputMin);
        int circleSegmentCount = Mathf.RoundToInt(Mathf.Lerp(outputMin, outputMax, t));

        // Calculate the direction vector based on the rotation
        Vector3 direction = rotation * Vector3.up;

        // Calculate the normal vector to the direction vector
        Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;

        // Define the center of the circle
        Vector3 circleCenter = position;

        // Calculate four points on the circle spaced evenly
        Vector3[] circlePoints = new Vector3[circleSegmentCount];
        float angleIncrement = 2.0f * Mathf.PI / circleSegmentCount; // Divide the circle into four equal parts
        for (int i = 0; i < circleSegmentCount; i++)
        {
            float angle = i * angleIncrement;
            float x = circleCenter.x + circleRadius * Mathf.Cos(angle);
            float y = circleCenter.y + circleRadius * Mathf.Sin(angle);
            Vector3 circlePoint = new Vector3(x, y, circleCenter.z);
            circlePoints[i] = RotatePointAroundPosition(circlePoint, circleCenter, rotation);
        }

        return circlePoints;
    }

    // Calculate the rotation between two positions in 3D space
    public static Quaternion CalculateLineRotation(Vector3 pos1, Vector3 pos2)
    {
        // Calculate the direction vector from pos1 to pos2
        Vector3 direction = pos2 - pos1;

        // Create a rotation that looks from pos1 to pos2
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        return rotation;
    }

    // Rotate a point around a position based on a quaternion rotation
    private static Vector3 RotatePointAroundPosition(Vector3 point, Vector3 position, Quaternion rotation)
    {
        // Translate the point so that the position becomes the origin
        Vector3 translatedPoint = point - position;

        // Apply rotation to the translated point
        Vector3 rotatedPoint = rotation * translatedPoint;

        // Translate the rotated point back to its original position
        Vector3 finalPoint = rotatedPoint + position;

        return finalPoint;
    }
}