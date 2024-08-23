using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private List<PlayerSpawnPoint> playerSpawnPoints = new List<PlayerSpawnPoint>();

    private static PlayerSpawnManager instance;
    public static PlayerSpawnManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(PlayerSpawnManager)) as PlayerSpawnManager;
                if (instance == null)
                {
                    Debug.LogError("There needs to be one active PlayerSpawnManager script on a GameObject in your scene.");
                }
            }
            return instance;
        }
    }

    public void SpawnPlayer(PlayerStats Player)
    {
        if (playerSpawnPoints.Count == 0)
        {
            Debug.LogError("Player Spawn Manager contains no player spawn point references, assign them in the inspector");
            return;
        }

        if (playerSpawnPoints.Count >= 1)
        {
            int targetSpawnPoint = Random.Range(0, playerSpawnPoints.Count);

            Player.gameObject.transform.position = playerSpawnPoints[targetSpawnPoint].transform.position;
            Player.gameObject.transform.rotation = playerSpawnPoints[targetSpawnPoint].transform.rotation;
        }

    }
    
}
