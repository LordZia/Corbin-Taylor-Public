using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OxygenSource : MonoBehaviour
{
    [Tooltip("The amount of oxygen to add to the player per interval")]
    [SerializeField] private int oxygenToAdd = 1;

    [Tooltip("Number of FixedUpdate calls to wait before adding oxygen again")]
    [SerializeField] private int addOxygenDelay = 10;

    private List<PlayerStats> playersInTrigger = new List<PlayerStats>();
    private int updateCounter = 0;

    private void Awake()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object has the PlayerStats component
        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            Debug.Log("detected player");
            // Add the player to the list of players within the trigger
            if (!playersInTrigger.Contains(playerStats))
            {
                playersInTrigger.Add(playerStats);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the colliding object has the PlayerStats component
        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // Remove the player from the list of players within the trigger
            if (playersInTrigger.Contains(playerStats))
            {
                playersInTrigger.Remove(playerStats);
            }
        }
    }

    private void FixedUpdate()
    {
        // Increment the update counter
        updateCounter++;

        // Check if the counter has reached the specified interval
        if (updateCounter >= addOxygenDelay)
        {
            // Reset the counter
            updateCounter = 0;

            // Add oxygen to each player within the trigger
            foreach (PlayerStats playerStats in playersInTrigger)
            {
                Debug.Log("adding oxygen");
                playerStats.AddOxygen(oxygenToAdd);
            }
        }
    }
}

