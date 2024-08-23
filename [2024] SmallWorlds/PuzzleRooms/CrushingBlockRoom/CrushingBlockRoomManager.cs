using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CrushingBlockRoomManager : MonoBehaviour
{
    [SerializeField] private Gravity_CubeRandomizer gravitySource;
    [SerializeField] private List<CrushingBlock> crushingBlocks = new List<CrushingBlock>();
    // Start is called before the first frame update

    [SerializeField] bool isActive = false;
    [SerializeField] bool isInMotion = false;

    private Vector3 directionOfMotion = Vector3.zero;

    void Awake()
    {
        foreach (CrushingBlock crushingBlock in crushingBlocks)
        {
            crushingBlock.InitializeBlock(this);
        }

        gravitySource.OnDirectionChange += OnGravityDirectionChange;
    }

    private void OnDestroy()
    {
        gravitySource.OnDirectionChange -= OnGravityDirectionChange;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInMotion) { return; }

        foreach (CrushingBlock crushingBlock in crushingBlocks)
        {
            crushingBlock.PerformCrushingChecks(directionOfMotion);
        }
    }

    void OnGravityDirectionChange(Vector3 direction)
    {
        directionOfMotion = direction;
    }

    void CallDetectedEntity(GameObject detectedObj, bool isInDirectionOfMotion)
    {
        if (detectedObj.CompareTag("Entity"))
        {
            if (isInDirectionOfMotion)
            {
                // hit object should be crushed
            }
            else
            {
                // check if this entity is also in the direction of motion of any other crushing blocks
            }
        }
    }
}
