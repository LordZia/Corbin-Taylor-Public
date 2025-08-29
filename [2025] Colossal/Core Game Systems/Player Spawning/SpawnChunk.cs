using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public Vector2Int coords;
    public List<Transform> spawnPoints = new();
    public List<GameObject> enemies = new();
    public List<GameObject> allies = new();
    public int recentDeaths = 0;
    public float score = 0f;
    public float lastDeathTime;

    public float nativeScore;
    public float finalScore;

    private Dictionary<PlayerMain, float> recentlySpawnedPlayers = new();

    // Tweakable value: how long a player counts as recently spawned
    private const float recentSpawnDuration = 6f;

    public Chunk(Vector2Int coords)
    {
        this.coords = coords;
    }

    public void ClearDynamicData()
    {
        enemies.Clear();
        allies.Clear();
        recentDeaths = 0;
        score = 0f;
    }

    public void RegisterPlayerSpawn(PlayerMain player)
    {
        recentlySpawnedPlayers[player] = Time.time;
    }

    public void UpdateScore()
    {
        // Clean up expired recently spawned players
        List<PlayerMain> toRemove = new();
        foreach (var entry in recentlySpawnedPlayers)
        {
            if (Time.time - entry.Value > recentSpawnDuration)
                toRemove.Add(entry.Key);
        }
        foreach (var player in toRemove)
            recentlySpawnedPlayers.Remove(player);

        // Decay old death values
        if (Time.time - lastDeathTime > 7f)
            recentDeaths = Mathf.Max(0, recentDeaths - 1);

        // Final score calculation
        score = 0;
        score -= recentDeaths * 3;
        score -= enemies.Count * 2;
        score += allies.Count;
        score -= recentlySpawnedPlayers.Count * 2; // Penalize cluster spawns
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
}
