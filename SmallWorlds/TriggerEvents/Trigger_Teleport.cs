using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_Teleport : MonoBehaviour
{
    [SerializeField] private Transform targetPosition;

    void Start()
    {
        if (targetPosition == null)
        {
            Debug.LogError("Trigger Telport requires a target position reference, please assign one in he inspector. disabling the script to prevent unexpected behavior.");
            this.gameObject.GetComponent<Collider>().enabled = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            other.gameObject.transform.position = targetPosition.position;
            other.gameObject.transform.rotation = targetPosition.rotation;

            Rigidbody rB = other.gameObject.GetComponent<Rigidbody>();
            if (rB != null)
            {
                rB.velocity = Vector3.zero;
            }

        }
    }
}
