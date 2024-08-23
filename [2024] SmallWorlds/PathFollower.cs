using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public float speed = 2.0f;
    public List<Vector3> pathPoints = new List<Vector3>();
    public bool loop = true; // Set to true for continuous looping

    private int currentPointIndex = 0;
    public void SetPath(List<Vector3> path)
    {
        pathPoints = path;
        if (pathPoints.Count < 2)
        {
            Debug.LogError("Path must have at least two points.");
            enabled = false;
            return;
        }

        transform.position = pathPoints[0];
        StartCoroutine(MoveAlongPath());
    }

    IEnumerator MoveAlongPath()
    {
        while (true)
        {
            Vector3 targetPosition = pathPoints[currentPointIndex];
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Move towards the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            // Check if the distance is small enough to consider reaching the target
            if (distance <= 0.01f)
            {
                if (loop)
                {
                    // Move to the next point in the path or reset to the beginning if looping
                    currentPointIndex = (currentPointIndex + 1) % pathPoints.Count;
                }
                else
                {
                    // Move to the next point in the path or stop if reached the end
                    currentPointIndex++;

                    if (currentPointIndex >= pathPoints.Count)
                    {
                        break; // exit the loop if reached the end and not looping
                    }
                }
            }

            yield return null;
        }
    }

}
