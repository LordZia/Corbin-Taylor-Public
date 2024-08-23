using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventorySlotExtender : InventorySlotDisplay
{
    [SerializeField][Tooltip("Reference any inventory slots that you'd like to extend this display too. For example hotbar slots should be extensions from the inventory display")]
    private List<InventorySlotDisplay> inventorySlotDisplays = new List<InventorySlotDisplay>();

    protected override void OnSlotUpdate()
    {
        foreach (var extendedDisplay in inventorySlotDisplays)
        {
            extendedDisplay.UpdateSlotVisuals(base.currentItem);
        }
    }
}
