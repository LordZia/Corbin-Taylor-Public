using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Gravity_Cube : GravitySource
{
    private SphereCollider sphereCollider;
    List<Quaternion> rotationList = new List<Quaternion>();

    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        
    }
    private void Awake()
    {
        sphereCollider = this.GetComponent<SphereCollider>();
        sphereCollider.enabled = true;
        sphereCollider.isTrigger = true;

        base.requireGravityUpdates = true;

        // Add default directions to the rotation list
        UpdateRotationList();

    }
    protected override Vector3 CalculateGravity(Transform targetObject)
    {
        Vector3 direction = (targetObject.position - sourceObject.transform.position).normalized;
        Debug.DrawLine(sourceObject.transform.position, targetObject.transform.position + direction * 1.5f, Color.yellow);

        Quaternion inputRotation = Quaternion.LookRotation(direction);

        Quaternion closestRotation = Utility.FindClosestRotation(inputRotation, rotationList);

        Vector3 gravityDir = closestRotation * Vector3.forward * -1;
        Vector3 customGravity = gravityDir * base.gravityStrength * -1f;

        return customGravity;
    }
    
    protected void UpdateRotationList()
    {
        rotationList.Clear();
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.forward)); // Forward
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.forward * -1)); // Backward
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.right)); // Right
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.right * -1)); // Left
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.up)); // Up
        rotationList.Add(Quaternion.LookRotation(base.sourceObject.transform.up * -1)); // Down
    }

    private void OnDrawGizmosSelected()
    {
        //Debug.DrawLine
        foreach (var rotation in rotationList)
        {
            Vector3 dir = rotation * Vector3.forward * -1;
            Debug.DrawLine(sourceObject.transform.position, sourceObject.transform.position + dir * 35, Color.blue);
        }
    }
}
