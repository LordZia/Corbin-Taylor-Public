using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; // The target object to follow

    [SerializeField] private Transform[] objectsToMove; // List of transforms to move
    private Vector3[] initialOffsets; // Initial offset of each object relative to the target

    private Vector3 lastFramePos;

    void Start()
    {
        if (target == null)
        {
            target = this.transform;
            return;
        }

        // Get the initial offset for each object
        GetInitialOffsets();

        lastFramePos = target.position;
    }

    void Update()
    {
        if (target == null || objectsToMove == null || initialOffsets == null)
        {
            Debug.LogError("Target or objects to move not assigned!");
            return;
        }

        // Move each object to follow the target with its initial offset
        if (target.position == lastFramePos) return;
        for (int i = 0; i < objectsToMove.Length; i++)
        {
            MoveObjectToTarget(objectsToMove[i], initialOffsets[i]);
        }
    }

    void GetInitialOffsets()
    {
        initialOffsets = new Vector3[objectsToMove.Length];
        for (int i = 0; i < objectsToMove.Length; i++)
        {
            initialOffsets[i] = objectsToMove[i].position - target.position;
        }
    }

    void MoveObjectToTarget(Transform objTransform, Vector3 initialOffset)
    {
        // Calculate the target position based on the initial offset
        Vector3 targetPosition = target.position + initialOffset;

        objTransform.position = targetPosition;
    }
}
