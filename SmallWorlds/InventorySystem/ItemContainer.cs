using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ItemContainer : MonoBehaviour , IInteractable
{
    [SerializeField] protected List<InventoryItem> items = new List<InventoryItem>();
    [SerializeField] protected string interactionDescription = "Press E To Interact";
    private bool hasBeenInteractedWith = false;

    private void Awake()
    {
        if (interactionDescription == string.Empty)
        {
            if (items.Count > 0)
            {
                interactionDescription = "Press E To Pickup " + items[0].item.ItemName;
            }
        }    
    }
    public ItemContainer(List<InventoryItem> inventoryItems)
    {
        items = inventoryItems;
    }
    public ItemContainer(InventoryItem inventoryItem)
    {
        items = new List<InventoryItem>();
        items.Add(inventoryItem);
    }
    public void OnInteract(GameObject interactingObject)
    {
        if (hasBeenInteractedWith) return; // ensure cannot recieve multiple interaction calls without a reset call

        Inventory inventory = interactingObject.GetComponent<Inventory>();
        if (inventory == null) { return; } // Can only be interacted with by objects with an inventory

        foreach (var item in items)
        {
            inventory.AddItem(item.item, item.quantity);
        }

        VFXManager.Instance.PlayEffect(VFXType.Dust, this.transform.position, this.transform.rotation, 1.5f);
        AudioManager.Instance.PlaySound(AudioType.Pickup, this.transform.position);

        hasBeenInteractedWith = true;
        Destroy(this.gameObject);
    }

    public void AddToContainer(InventoryItem inventoryItem)
    {
        items.Add(inventoryItem);
    }

    public void SelfDestruct()
    {
        Destroy(this.gameObject);
    }

    public virtual string InteractDescription()
    {
        return interactionDescription;
    }
}