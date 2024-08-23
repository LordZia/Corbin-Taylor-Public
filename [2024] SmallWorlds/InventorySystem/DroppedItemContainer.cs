using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;

public class DroppedItemContainer : ItemContainer
{
    [SerializeField] private float despawnTimer = 300; // seconds before despawning item
    [SerializeField] private string itemPickUpText;
    [SerializeField] private bool displayContainedItemsInPreviewWindow = false;
    private float currentDespawnTimer = 0.0f;
    public DroppedItemContainer(List<InventoryItem> inventoryItems) : base(inventoryItems)
    {
    }

    public DroppedItemContainer(InventoryItem inventoryItem) : base(inventoryItem)
    {
    }

    private void Awake()
    {
        currentDespawnTimer = despawnTimer;
        Destroy(this.gameObject, despawnTimer);
    }

    public override string InteractDescription()
    {
        string displayedMessage = base.interactionDescription;
        if (displayContainedItemsInPreviewWindow)
        {
            StringBuilder previewTextBuilder = new StringBuilder();

            previewTextBuilder.AppendLine(base.interactionDescription);

            foreach (var item in base.items)
            {
                previewTextBuilder.Append($"<color=green>{item.item.ItemName}");

                if (item.quantity > 1)
                {
                    previewTextBuilder.Append($": { item.quantity} </color>");
                }

            }

            displayedMessage = previewTextBuilder.ToString();
        }

        return displayedMessage;
    }
}

