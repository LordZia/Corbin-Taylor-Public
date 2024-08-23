using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_GravitySourceAdjust : MonoBehaviour
{
    [SerializeField] private TimedGravityInverter gravityInverter;

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.CompareTag("Player"))
        {
            gravityInverter.SetNotActive();
            gravityInverter.ForceGravityInversionState(false);
        }
    }
}
