using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPositionTracker : MonoBehaviour , IChunkPartitianed
{
    [SerializeField]
    [Tooltip("Number of fixed update calls (60 are called per second) between position updates")]
    private int updateFrequency = 60;
    private int currentCounter = 0;

    private Vector3Int currentChunkPos = new Vector3Int(0, 0, 0);

    // Start is called before the first frame update
    void Awake()
    {
        UpdateChunkPosition();
        ResourceManager.Instance.AddChunkObstruction(this.gameObject);
    }
    private void OnDestroy()
    {
        ResourceManager.Instance.RemoveChunkObstruction(this.gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        currentCounter++;

        if (currentCounter >= updateFrequency)
        {
            currentCounter = 0;
            UpdateChunkPosition();
        }
    }

    private void UpdateChunkPosition()
    {
        Vector3Int newPos = ChunkPartitionManager.Instance.GetChunkPosition(this.transform.position);

        if (currentChunkPos == newPos)
        {
            return;
        }

        // Position has changed update the partitian manager
        currentChunkPos = newPos;

        ChunkPartitionManager.Instance.RemoveItem(this.gameObject);
        ChunkPartitionManager.Instance.AddItem(this.gameObject);
        ResourceManager.Instance.UpdateChunkObstruction(this.gameObject);
    }

    public Vector3Int GetChunk()
    {
        return currentChunkPos;
    }
}
