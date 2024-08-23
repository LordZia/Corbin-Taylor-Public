using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Trigger_OnPlayerEnter : MonoBehaviour
{
    // Define a delegate for the event
    public delegate void TriggerEnterhandler(PlayerStats playerData);

    // Define the event using the delegate
    public event TriggerEnterhandler OnPlayerEnter;
    public event TriggerEnterhandler OnPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerStats playerData = Utility.GetComponentFromAnyParent<PlayerStats>(other.gameObject);
            if (playerData != null)
            {
                OnPlayerEnter?.Invoke(playerData);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerStats playerData = Utility.GetComponentFromAnyParent<PlayerStats>(other.gameObject);
            if (playerData != null)
            {
                OnPlayerExit?.Invoke(playerData);
            }
        }
    }
}
