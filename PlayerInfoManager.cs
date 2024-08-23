using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInfoManager : MonoBehaviour
{
    private static PlayerInfoManager instance;
    public static PlayerInfoManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerInfoManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("PlayerInfoManager");
                    instance = obj.AddComponent<PlayerInfoManager>();
                }
            }
            return instance;
        }
    }

    private List<PlayerStats> playerInfos = new List<PlayerStats>();

    public List<PlayerStats> PlayerInfos => playerInfos;


    public Action<PlayerStats> OnPlayerAdd;
    public Action<PlayerStats> OnPlayerRemove;

    public void AddPlayerInfo(PlayerStats playerInfo)
    {
        Debug.Log("added player info");
        playerInfos.Add(playerInfo);
    }

    public void RemovePlayerInfo(PlayerStats playerInfo)
    {
        Debug.Log("removed player info");
        playerInfos.Remove(playerInfo);
    }

    public List<Vector3> GetAllPlayerPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        if (playerInfos.Count == 0)
        {
            return positions;
        }

        for (int i = 1; i < playerInfos.Count; i++)
        {
            Debug.Log(i);
            positions[i] = playerInfos[i].transform.position;
        }
        return positions;
    }

    public Vector3 GetClosestPlayerPosition(Vector3 referencePosition)
    {
        if (playerInfos.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 closestPosition = Utility.GetClosestVector3(referencePosition, GetAllPlayerPositions());
        return closestPosition;
    }

    public GameObject GetClosestPlayerObject(Vector3 referencePosition)
    {
        if (playerInfos.Count == 0)
        {
            Debug.Log("player count is 0 retun null");
            return null;
        }

        int index = -1;
        Vector3 closestPos = Utility.GetClosestVector3(referencePosition, GetAllPlayerPositions(), out index);

        if (index > -1 && index < playerInfos.Count)
        {
            Debug.Log("found closest player index is : " + index);
            return playerInfos[index].gameObject;
        }
        
        return null;
    }
    // Other methods to access player information as needed
}