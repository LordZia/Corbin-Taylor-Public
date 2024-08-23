using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject droppedItemLootContainer;
    private Dictionary<Item, int> heldItemsCounter = new Dictionary<Item, int>(); 
    // Tracks just the quantity of each item.

    private Dictionary<int, InventoryItem> inventorySlots = new Dictionary<int, InventoryItem>();
    // Keeps track of each seperate slot in the inventory, their item ID and it's quantity

    private ItemIndex itemIndex;
    // Method to add a specified amount of a resource to the inventory

    [SerializeField] private List<StartingItem> startingItems = new List<StartingItem>();

    private List<InventoryItem> changedItems = new List<InventoryItem>();
    private List<int> changedSlots = new List<int>();

    [SerializeField] private int maxSlots = 10;

    private bool hasBeenInitialized = false;
    public enum inventoryType
    {
        None,
        Inventory,
        Chest,
        ChestLarge,
        Processor
    }

    [System.Serializable]
    private struct StartingItem
    {
        public int slot;
        public InventoryItem inventoryItem;
    }

    [SerializeField] protected inventoryType type = inventoryType.None;
    public Dictionary<Item, int> HeldItemsCounter { get { return heldItemsCounter; } }
    public Dictionary<int , InventoryItem> InventorySlots { get { return inventorySlots; } }

    public inventoryType Type { get { return type; } }

    public int MaxSlots { get { return maxSlots; } }

    public event Action<List<InventoryItem>> OnItemCountChange;
    public event Action<List<int>> OnSlotChange;

    private void Awake()
    {
        if (type == inventoryType.Inventory)
        {
            Initialize(); // initialize on awake for inventory types
        }
    }
    public void Initialize()
    {
        if (!hasBeenInitialized)
        {
            OnFirstLoad();
            CallSlotUpdateEvent(1);
            hasBeenInitialized = true;
        }
    }
    private void OnFirstLoad()
    {
        foreach (StartingItem startingItem in startingItems)
        {
            if (startingItem.inventoryItem.item == null)
            {
                Debug.Log("tried to place an item in " + startingItem.slot + " does not have an item");
                continue;
            }
            Debug.Log(startingItem.slot + " " + startingItem.inventoryItem.item.ItemName);
            AddItemToSlot(startingItem.slot, startingItem.inventoryItem);
        }
    }
    public void AddItem(Item item, int amount)
    {
        changedItems.Clear();

        // Check if the resource is already in the inventory
        if (item == null)
        {
            return;
        }
        if (heldItemsCounter.ContainsKey(item))
        {
            // If yes, add the amount to the existing amount
            heldItemsCounter[item] += amount;
            changedItems.Add(new InventoryItem(item, amount));
        }
        else
        {
            // If not, add the resource to the inventory with the specified amount
            heldItemsCounter.Add(item, amount);
        }
        AddOrRemoveFromSlots(new InventoryItem(item, amount));
        //OnSlotChange?.Invoke(new List<int>());
    }
    private void AddOrRemoveFromSlots(InventoryItem inventoryItem)
    {
        int remainingQuantityToAdd = inventoryItem.quantity;
        int quantityToRemove = -inventoryItem.quantity;

        // If quantity is negative, it indicates removal of items
        bool isRemoval = inventoryItem.quantity < 0;

        List<int> updatedSlots = new List<int>();

        for (int i = 0; i < maxSlots; i++)
        {
            
            // Check each inventory slot to see if it is currently occupied or not.
            if (!inventorySlots.ContainsKey(i))
            {
                // Inventory slot is free to use, add the new item to the slot
                if (!isRemoval)
                {
                    //inventorySlots.Add(i, new InventoryItem(inventoryItem.item, remainingQuantityToAdd));
                    int newStackQuantity;
                    int overflowAmount;

                    CalculateStackAmount(0, remainingQuantityToAdd, inventoryItem.item.ItemStackLimit , out newStackQuantity, out overflowAmount);
                    inventorySlots[i] = new InventoryItem(inventoryItem.item, newStackQuantity);
                    updatedSlots.Add(i);
                    remainingQuantityToAdd = overflowAmount;

                    if (overflowAmount <= 0)
                    {
                        remainingQuantityToAdd = 0;
                        break;
                    }
                }
                else
                {
                    continue;
                }
            }

            // Target inventory slot is occupied currently, check if it contains the same type of item as the new item.
            InventoryItem occupiedSlotItem = inventorySlots[i];
            if (occupiedSlotItem.item != inventoryItem.item)
            {
                continue; // occupied slot does not contain the input item to add/remove : move to check next slot in inventory 
            }
            Item sameItem = inventoryItem.item;

            if (!isRemoval)
            {
                int newStackQuantity;
                int overflowAmount;

                CalculateStackAmount(occupiedSlotItem.quantity, remainingQuantityToAdd, sameItem.ItemStackLimit, out newStackQuantity, out overflowAmount);

                // Set the new quantity of target inventory slot
                inventorySlots[i] = new InventoryItem(sameItem, newStackQuantity);
  
                remainingQuantityToAdd = overflowAmount;
            }
            else
            {
                int slotStackSizeAfterRemoval = occupiedSlotItem.quantity - quantityToRemove;
                if (slotStackSizeAfterRemoval < 0)
                {
                    inventorySlots[i] = new InventoryItem(null, 0);
                    quantityToRemove = slotStackSizeAfterRemoval * -1;
                }
                else
                {
                    inventorySlots[i] = new InventoryItem(sameItem, slotStackSizeAfterRemoval);
                    quantityToRemove = 0;
                }
            }

            // Check if the slot is empty after removal
            if (inventorySlots[i].quantity <= 0)
            {
                inventorySlots.Remove(i);
            }

            updatedSlots.Add(i);

            // If there are no more items to add or remove, exit the loop
            if ((isRemoval && quantityToRemove <= 0) || (!isRemoval && remainingQuantityToAdd <= 0))
            {
                // add auto item drop functionality here later
                break;
            }
        }

        if (remainingQuantityToAdd > 0)
        {
            DropItem(inventoryItem);
        }

        OnItemCountChange?.Invoke(changedItems);
        OnSlotChange?.Invoke(updatedSlots);
    }

    public static void CalculateStackAmount(int currentQuantity, int quantityToAdd, int maxStackSize, out int newStackAmount, out int overflowAmount)
    {
        // Calculate the total quantity after adding the quantity to add
        int totalQuantity = currentQuantity + quantityToAdd;

        // Check if the total quantity exceeds the maximum stack size
        if (totalQuantity > maxStackSize)
        {
            // The stack size will reach maximum
            newStackAmount = maxStackSize;

            // Calculate the overflow amount
            overflowAmount = totalQuantity - maxStackSize;
        }
        else
        {
            // The stack size does not exceed the maximum stack size
            newStackAmount = totalQuantity;
            overflowAmount = 0;
        }
    }

    public void RemoveItem(Item item, int amount)
    {
        changedItems.Clear();
        // Check if the resource is in the inventory
        if (heldItemsCounter.ContainsKey(item))
        {
            if (amount > 0)
            {
                // removal logic expects a negative value.
                amount *= -1;
            }

            // Subtract the specified amount from the existing amount
            int newAmount = heldItemsCounter[item] + amount;

            heldItemsCounter[item] += amount;
            AddOrRemoveFromSlots(new InventoryItem(item, amount));

            // Check if the remaining amount is zero or negative
            if (newAmount <= 0)
            {
                heldItemsCounter.Remove(item);
                changedItems.Add(new InventoryItem(item, amount));
            }
        }
        else
        {
            Debug.LogWarning("Trying to remove a item that is not in the inventory: " + item.ItemName);
        }

        OnSlotChange?.Invoke(new List<int>());
    }

    public void CallSlotUpdateEvent(List<int> slots)
    {
        OnSlotChange?.Invoke(slots);
        SlotUpdateEvent(slots);
    }
    public void CallSlotUpdateEvent(int slot)
    {
        List<int> ints = new List<int>();
        ints.Add(slot);
        OnSlotChange?.Invoke(ints);
        SlotUpdateEvent(slot);
    }
    protected virtual void SlotUpdateEvent(int slot)
    {
    }
    protected virtual void SlotUpdateEvent(List<int> slots)
    {
    }
    protected virtual void ItemCountUpdateEvent(InventoryItem updatedItem)
    {
    }
    protected virtual void ItemCountUpdateEvent(List<InventoryItem> updatedItems)
    {
    }
    public void CallItemCountUpdateEvent(InventoryItem updatedItem)
    {
        List<InventoryItem> updatedItems = new List<InventoryItem>();
        updatedItems.Add(updatedItem);
        OnItemCountChange?.Invoke(updatedItems);
    }
    public void CallItemCountUpdateEvent(List<InventoryItem> updatedItems)
    {
        OnItemCountChange?.Invoke(updatedItems);
    }
    public void AddItemToSlot(int slotID, InventoryItem itemToAdd)
    {
        if (inventorySlots.ContainsKey(slotID))
        {
            Debug.LogError($"Slot already contains key of {slotID}");
            return;
        }
        if (itemToAdd.quantity == 0)
        {
            Debug.LogError("A request to add an item to an inventory was made where the item has a quantity of 0, ignoring the request.", this.gameObject);
            return;
        }
        inventorySlots.Add(slotID, itemToAdd);

        if (heldItemsCounter.ContainsKey(itemToAdd.item))
        {
            heldItemsCounter[itemToAdd.item] += itemToAdd.quantity;
        }
        else
        {
            heldItemsCounter.Add(itemToAdd.item, itemToAdd.quantity);
        }

        CallItemCountUpdateEvent(itemToAdd);
        CallSlotUpdateEvent(slotID);
    }
    public void RemoveItemFromSlot(int slotID, Item itemToRemove, int quantity)
    {
        if (inventorySlots.ContainsKey(slotID))
        {
            InventoryItem oldItem = inventorySlots[slotID];

            int newQuantity = oldItem.quantity - quantity;
            if (newQuantity <= 0) 
            {
                Debug.Log(newQuantity + "  new quantity");
                // Item reached a quantity of 0 remove it from the dictionary and call updates
                newQuantity = 0;
                inventorySlots.Remove(slotID);

                InventoryItem item = new InventoryItem(itemToRemove, 0);
                CallItemCountUpdateEvent(item);
                CallSlotUpdateEvent(slotID);
                return;
            }
            InventoryItem newItem = new InventoryItem(itemToRemove, newQuantity);

            inventorySlots[slotID] = newItem;

            if (heldItemsCounter.ContainsKey(itemToRemove))
            {
                heldItemsCounter[itemToRemove] -= quantity;
            }

            CallItemCountUpdateEvent(newItem);
            CallSlotUpdateEvent(slotID);
        }
        else
        {
            Debug.Log($"recieved a call to remove {itemToRemove} : but {itemToRemove} does not exist in inventory slot {slotID} on {this.gameObject.name}'s inventory");
        }
    }
    public void RemoveItemFromSlot(int slotID, Item removedItem)
    {
        if (inventorySlots.ContainsKey(slotID))
        {
            inventorySlots.Remove(slotID);

            InventoryItem item = new InventoryItem(removedItem, 0);
            CallItemCountUpdateEvent(item);
            CallSlotUpdateEvent(slotID);
        }
        else
        {
            Debug.Log($"recieved a call to remove {removedItem} : but {removedItem} does not exist in inventory slot {slotID} on {this.gameObject.name}'s inventory");
        }
    }

    // Method to get the amount of a specific item in the inventory
    public int GetResourceAmount(Item itemD)
    {
        // Check if the item is in the inventory
        if (heldItemsCounter.ContainsKey(itemD))
        {
            // Return the amount of the specified item
            return heldItemsCounter[itemD];
        }
        else
        {
            // If the item is not in the inventory, return zero or handle the case accordingly
            return 0;
        }
    }

    private void RefreshTotalItemCount()
    {
        foreach (KeyValuePair<int, InventoryItem> invItem in inventorySlots)
        {
            if (inventorySlots.ContainsKey(invItem.Key))
            {
                if (invItem.Value.item != null)
                {
                    heldItemsCounter[invItem.Value.item] += invItem.Value.quantity;
                }
            }    
        }
    }

    // Method to print the contents of the inventory (for testing/debugging)
    public void PrintInventory()
    {
        if (heldItemsCounter.Count == 0) 
        {
            Debug.Log("Inventory Is Empty");
        }

        foreach (var slot in inventorySlots)
        {
            InventoryItem inventoryItem = slot.Value;
            Debug.Log("Item: " + inventoryItem.item.ItemName + ", Amount: " + inventoryItem.quantity);
        }
    }
    private void DropItem(InventoryItem droppedItem)
    {
        GameObject spawnedObject = Instantiate(droppedItemLootContainer, this.transform.position, Quaternion.identity);
        spawnedObject.GetComponent<Rigidbody>().AddForce(this.transform.forward * 3, ForceMode.Force);
        ItemContainer itemContainer = spawnedObject.GetComponent<ItemContainer>();

        itemContainer.AddToContainer(droppedItem);
    }

    public void CallOpenInventory()
    {
        OnInventoryOpen();
    }
    public void CallCloseInventory()
    {
        OnInventoryClose();
    }
    protected virtual void OnInventoryOpen()
    {
    }
    protected virtual void OnInventoryClose()
    {
    }
}

[System.Serializable]
public struct InventoryItem
{
    public Item item;
    public int quantity;

    // Default constructor to represent no item
    public InventoryItem(Item item, int quantity = 0)
    {
        this.item = item;
        this.quantity = quantity;
    }
}
