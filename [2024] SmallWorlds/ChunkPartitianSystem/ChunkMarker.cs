using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMarker : MonoBehaviour , IChunkPartitianed
{
    [SerializeField] private Vector3Int chunk;

    private void Awake()
    {
        chunk = ChunkPartitionManager.Instance.GetChunkPosition(this.transform.position);

        ChunkPartitionManager.Instance.AddItem(this.gameObject);
    }

    public Vector3Int GetChunk()
    {
        return chunk;
    }
}
