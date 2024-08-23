using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(BoxCollider))]
public class CylinderTrigger : CustomTrigger
{
    private Dictionary<Collider, ushort> enteredColliders = new Dictionary<Collider, ushort>();

    private void Awake()
    {
        // Assuming both colliders are on the same GameObject
        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        BoxCollider boxCollider = GetComponent<BoxCollider>();

        if (capsuleCollider != null)
            capsuleCollider.isTrigger = true;

        if (boxCollider != null)
            boxCollider.isTrigger = true;
    }

    private void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (!enteredColliders.ContainsKey(other))
        {
            enteredColliders.Add(other, 1);
        }
        else
        {
            enteredColliders[other]++;
        }

        // Check bounds condition when a collider enters
        CheckBounds(other);
    }


    void OnTriggerExit(Collider other)
    {
        if (enteredColliders.ContainsKey(other))
        {
            enteredColliders[other]--;
            if (enteredColliders[other] <= 0)
            {
                enteredColliders.Remove(other);
                ExitBounds(other);
            }
        }
    }

    // Check bounds condition and invoke event
    private void CheckBounds(Collider other)
    {
        if (enteredColliders[other] >= 2)
        {
            base.EnterBounds(other);
        }
    }
}

public class CustomTrigger : MonoBehaviour
{
    public event Action<Collider> OnEnterBounds;
    public event Action<Collider> OnExitBounds;

    protected bool isWithinBounds = false;
    public bool IsWithinBounds { get { return isWithinBounds; } }

    // Called when entering bounds
    protected void EnterBounds(Collider other)
    {
        OnEnterBounds?.Invoke(other);
    }

    protected void ExitBounds(Collider other)
    {
        OnExitBounds?.Invoke(other);
    }
}
