using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GravityHandler))]
public class RBMoveTowards : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float lookRotationSpeed = 5.0f;
    [SerializeField] private Transform target;
    [SerializeField] private float changeDirThreshold = 0.5f;

    private GravityHandler gravityHandler;

    private Rigidbody rb;
    // Start is called before the first frame update
    void Awake()
    {
        rb = this.GetComponent<Rigidbody>();
        gravityHandler= this.GetComponent<GravityHandler>();
    }

    void Update()
    {
        if (target == null)
        {
            Debug.LogError("Target transform is not assigned.");
            return;
        }
        Vector3 directionToTarget = target.position - transform.position;

        // Project directionToTarget onto the plane perpendicular to gravityDir
        Vector3 projectedDirection = Vector3.ProjectOnPlane(directionToTarget, gravityHandler.GravityDir.normalized).normalized;


        UpdateMoveDir(projectedDirection);

        Quaternion targetRot = Quaternion.LookRotation(this.transform.forward, this.transform.up);
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRot, Time.deltaTime);
    }

    private void UpdateMoveDir(Vector3 direction)
    {
        direction = direction.normalized;
        float angleToTarget = Vector3.Dot(transform.forward, direction);

        Vector3 desiredVelocity = direction * moveSpeed;
        rb.velocity = desiredVelocity + gravityHandler.GravityDir;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
