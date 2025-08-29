using System.Collections.Generic;
using UnityEngine;

public class RespawnChunkManager : MonoBehaviour
{
    public static RespawnChunkManager Instance;

    [Header("Chunk Settings")]
    public Vector2 chunkSize = new Vector2(25f, 25f);
    public LayerMask spawnPointMask;

    [Header("Debug")]
    public bool debugDrawChunks;

    private Dictionary<Vector2Int, Chunk> chunks = new();
    private HashSet<Vector2Int> spawnableChunkCoords = new();
    private List<Transform> allSpawnPoints = new();

    private static readonly Vector2Int[] neighborOffsets = new Vector2Int[]
    {
        new Vector2Int(-1,  0), new Vector2Int(1,  0),
        new Vector2Int(0,  -1), new Vector2Int(0,  1),
        new Vector2Int(-1, -1), new Vector2Int(1, -1),
        new Vector2Int(-1,  1), new Vector2Int(1,  1),
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        Initialize();
    }

    public void Initialize()
    {
        chunks.Clear();
        spawnableChunkCoords.Clear();
        allSpawnPoints.Clear();

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (var sp in spawnPoints)
        {
            Vector2Int coords = WorldToChunkCoords(sp.transform.position);
            if (!chunks.ContainsKey(coords))
                chunks[coords] = new Chunk(coords);

            chunks[coords].spawnPoints.Add(sp.transform);
            allSpawnPoints.Add(sp.transform);
        }

        foreach (var kvp in chunks)
        {
            if (kvp.Value.spawnPoints.Count > 0)
                spawnableChunkCoords.Add(kvp.Key);
        }

        Debug.Log($"[RespawnChunkManager] Spawnable chunks: {spawnableChunkCoords.Count} / Total: {chunks.Count}");
    }

    public void ReportPlayerDeath(Vector3 position)
    {
        Vector2Int chunkCoord = WorldToChunkCoords(position);
        if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            chunk.recentDeaths++;
            chunk.lastDeathTime = Time.time;
        }
    }

    public Transform GetRespawnPoint(PlayerMain requester)
    {
        UpdateChunkScoresFor(requester);

        Chunk bestChunk = null;
        float bestAdjustedScore = float.MinValue;

        List<Chunk> candidates = new();

        foreach (Vector2Int coord in spawnableChunkCoords)
        {
            Chunk chunk = chunks[coord];
            if (chunk.enemies.Count > 0)
                continue;

            chunk.nativeScore = EvaluateChunkScore(chunk);
            candidates.Add(chunk);
        }

        foreach (Chunk chunk in candidates)
        {
            float total = chunk.nativeScore;
            int count = 1;

            foreach (var offset in neighborOffsets)
            {
                Vector2Int neighborCoord = chunk.coords + offset;
                if (chunks.TryGetValue(neighborCoord, out Chunk neighbor))
                {
                    if (neighbor.enemies.Count == 0)
                    {
                        total += EvaluateChunkScore(neighbor);
                        count++;
                    }
                }
            }

            chunk.finalScore = total / count;

            if (chunk.finalScore > bestAdjustedScore)
            {
                bestAdjustedScore = chunk.finalScore;
                bestChunk = chunk;
            }
        }

        if (bestChunk != null)
        {
            Transform spawnPoint = bestChunk.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                bestChunk.RegisterPlayerSpawn(requester);
                return spawnPoint;
            }
        }

        return GetRandomFallbackSpawn();
    }


    private void UpdateChunkScoresFor(PlayerMain requester)
    {
        foreach (var chunk in chunks.Values)
        {
            chunk.allies.Clear();
            chunk.enemies.Clear();
        }

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("PlayerCharacter");

        foreach (GameObject playerGO in allPlayers)
        {
            if (!playerGO.activeInHierarchy)
                continue;

            PlayerMain other = playerGO.GetComponent<PlayerMain>();
            if (other == null || other == requester)
                continue;

            Vector2Int chunkCoord = WorldToChunkCoords(playerGO.transform.position);
            if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
            {
                if (requester.IsOnSameTeam(other))
                    chunk.allies.Add(playerGO);
                else
                    chunk.enemies.Add(playerGO);
            }
        }

        foreach (var chunk in chunks.Values)
            chunk.UpdateScore();
    }

    private float EvaluateChunkScore(Chunk chunk)
    {
        float score = 0f;
        score -= chunk.enemies.Count * 2f;
        score += chunk.allies.Count * 1.5f;
        score -= chunk.recentDeaths * 3f;
        return score;
    }

    private Vector2Int WorldToChunkCoords(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / chunkSize.x);
        int z = Mathf.FloorToInt(worldPos.z / chunkSize.y);
        return new Vector2Int(x, z);
    }

    private Transform GetRandomFallbackSpawn()
    {
        if (allSpawnPoints.Count == 0) return null;
        return allSpawnPoints[Random.Range(0, allSpawnPoints.Count)];
    }

    public Dictionary<Vector2Int, Chunk> GetAllChunks()
    {
        return chunks;
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawChunks || chunks == null) return;

        Gizmos.color = Color.green;
        foreach (var chunk in chunks.Values)
        {
            Vector3 center = new Vector3(chunk.coords.x * chunkSize.x + chunkSize.x / 2f, 30, chunk.coords.y * chunkSize.y + chunkSize.y / 2f);
            Vector3 size = new Vector3(chunkSize.x, 0f, chunkSize.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
