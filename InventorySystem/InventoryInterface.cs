using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Inventory))]
public class InventoryInterface : MonoBehaviour
{
    [SerializeField] private bool useDebugLogs = false;
    [SerializeField] private GameObject inventoryDisplayObj;
    [SerializeField] private GameObject crosshair;
    [SerializeField] public Inventory inventory;

    public List<InventorySlotDisplay> itemSlots = new List<InventorySlotDisplay>();

    Dictionary<int, InventoryItem> inventorySlotsToModify = new Dictionary<int, InventoryItem>();

    private InventoryItem heldItem;
    private InventoryItem hoveredItem;
    private int previousHoveredSlot;

    public float heldItemDisplayMoveSpeed = 25f;

    PlayerStateMachine playerState;
    private void ForceUpdateDisplays()
    {
        List<int> slots = new List<int>();
        UpdateDisplay(slots);
    }
    public void OnOpenInterface(Inventory inventory, GameObject openingObject)
    {
        playerState = openingObject.GetComponent<PlayerStateMachine>();

        this.inventory.OnSlotChange += UpdateDisplay;

        this.inventory = inventory;
        UpdateDisplay(1); // display update logic currently does not care about what slots have actually been updated, updates all slot regardless of need.
                          // will fix later; - zia

        //inventory = GetComponent<Inventory>();
        this.inventory.Initialize();
        this.inventory.OnSlotChange += UpdateDisplay;

        heldItem = new InventoryItem(null, 0);

        // inventory displays were displaying as white until the first update call / bandaid fix until the actual problem is found. - zia
        Invoke("ForceUpdateDisplays", 0.1f);

        inventoryDisplayObj.SetActive(true);
        inventory.CallOpenInventory();
    }
    public void OnCloseInterface()
    {
        inventoryDisplayObj.SetActive(false);
        inventory.CallCloseInventory();
    }
    public void UpdateDisplay(int changedSlot)
    {
        List<int> slots = new List<int>();
        slots.Add(changedSlot);
        UpdateDisplay(slots);
    }

    public void UpdateDisplay(List<int> changedSlots)
    {
        inventorySlotsToModify.Clear();

        // Loop through the held items and update the display
        for (int i = 0; i < itemSlots.Count; i++)
        {
            InventoryItem slotItem = new InventoryItem(null, -1);
            if (inventory.InventorySlots.ContainsKey(i))
            {
                if (useDebugLogs)
                {
                    Debug.Log($"{i} : {this.gameObject.name}");
                    Debug.Log($"item found in slot {i} : {inventory.InventorySlots[i].item.ItemName} quantity : {inventory.InventorySlots[i].quantity}");
                }
                slotItem = inventory.InventorySlots[i];
            }

            // Update slot visuals with slot item, if no key was found then an empty item will be passed in and a blank slot will be shown
            if (useDebugLogs && slotItem.item != null)
            {
                Debug.Log($"adding item {slotItem.item.ItemName} to slot {i}");
            }
            itemSlots[i].UpdateSlotVisuals(slotItem);
        }
    }


    #region ItemPreviewTextLogic
    

    private void OnUICursorHoverExit(GameObject exitedUIElement)
    {
        InventorySlotDisplay displaySlot = exitedUIElement.GetComponent<InventorySlotDisplay>();
        if (displaySlot != null)
        {
            displaySlot.OnSlotHoverExit();
        }
    }


    #endregion

    #region ItemMovingLogic

    public void OnUICursorRightClight(GameObject UIClickTarget)
    {
        /*
        int targetSlot = CheckClickTarget(UIClickTarget);
        Debug.Log(targetSlot);
        if (targetSlot == -1)
        {
            return;
        }

        if (heldItem.item != null) // an item is currently in hand, handle assosciated logic.
        {
            InventoryItem singleItem = new InventoryItem(heldItem.item, 1);
            HandleItemPlacing(targetSlot, singleItem);
            itemSlots[targetSlot].UpdateSlotVisuals(inventory.InventorySlots[targetSlot]);
        }
        else // helditem contains no data, it is empty;
        {
            // try pick up half stack
            InventoryItem pickUpItem = PickupItemFromSlot(targetSlot);
            if (pickUpItem.item != null)
            {
                int firstStack;
                int secondStack;
                Utility.SplitInt(pickUpItem.quantity, out firstStack, out secondStack);

                InventoryItem placedItem = new InventoryItem(pickUpItem.item, secondStack);
                HandleItemPlacing(targetSlot, placedItem);

                heldItem = new InventoryItem(pickUpItem.item, firstStack);
                itemSlots[targetSlot].UpdateSlotVisuals(inventory.InventorySlots[targetSlot]);
            }
        }
        */
    }

    public void OnUICursorClick(GameObject UIClickTarget)
    {
        /*
        int targetSlot = CheckClickTarget(UIClickTarget);
        Debug.Log(targetSlot);
        if (targetSlot == -1)
        {
            return;
        }

        if (heldItem.item != null) // an item is currently in hand, handle assosciated logic.
        {
            HandleItemPlacing(targetSlot, heldItem);
        }
        else // helditem contains no data, it is empty;
        {
            heldItem = PickupItemFromSlot(targetSlot);
        }
        

        List<int> updatedSlots = new List<int>();
        updatedSlots.Add(targetSlot);
        UpdateDisplay(updatedSlots);
        */
    }

    private int CheckClickTarget(GameObject UIClickTarget)
    {
        int targetSlot = -1;
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i].gameObject == UIClickTarget)
            {
                targetSlot = i;
                break;
            }

            if (itemSlots[i].GetComponentInChildren<RectTransform>().position == UIClickTarget.GetComponent<RectTransform>().position)
            {
                targetSlot = i;
                break;
            }
        }

        return targetSlot;
    }

    public InventoryItem PickupItemFromSlot(int targetSlot)
    {
        InventoryItem item = new InventoryItem(null, 0);
        if (inventory.InventorySlots.ContainsKey(targetSlot))
        {
            item = inventory.InventorySlots[targetSlot];
            inventory.InventorySlots.Remove(targetSlot);
        }


        inventory.CallSlotUpdateEvent(targetSlot);
        return item;
    }

    public void HandleItemPlacing(int targetSlot, InventoryItem inventoryItem, ref InventoryItem heldItem)
    {
        if (!inventory.InventorySlots.ContainsKey(targetSlot))
        {
            //inventory.InventorySlots.Add(targetSlot, inventoryItem);
            inventory.AddItemToSlot(targetSlot, inventoryItem);
            heldItem.quantity -= inventoryItem.quantity;

            if (heldItem.quantity <= 0)
            {
                heldItem = new InventoryItem(null, 0);
            }

            inventory.CallSlotUpdateEvent(targetSlot);
            return;
        }
        
        // Target inventory slot is occupied currently, check if it contains the same type of item as the new held item.
        InventoryItem occupiedSlotItem = inventory.InventorySlots[targetSlot];
        if (occupiedSlotItem.item == inventoryItem.item) 
        {
            heldItem.quantity -= inventoryItem.quantity;
            Item itemTypeData = inventoryItem.item;
            // slot contains the same item as held item, handle stacking
            int newStackQuantity;
            int overflowAmount;

            Inventory.CalculateStackAmount(occupiedSlotItem.quantity, inventoryItem.quantity, itemTypeData.ItemStackLimit, out newStackQuantity, out overflowAmount);

            // Set the new quantity of target inventory slot
            //inventory.InventorySlots[targetSlot] = new InventoryItem(inventoryItem.item, newStackQuantity);
            inventory.AddItemToSlot(targetSlot, new InventoryItem(inventoryItem.item, newStackQuantity));
            heldItem.quantity += overflowAmount;
        }
        else
        {
            // slot contains an item with a different itemID
            // swap target slot item with held item
            InventoryItem tempItemReferance = inventory.InventorySlots[targetSlot];
            //inventory.InventorySlots.Remove(targetSlot);

            inventory.RemoveItemFromSlot(targetSlot, tempItemReferance.item, tempItemReferance.quantity);
            inventory.AddItemToSlot(targetSlot, heldItem);
            //inventory.InventorySlots.Add(targetSlot, heldItem);
            heldItem = tempItemReferance;
        }

        if (heldItem.quantity <= 0)
        {
            heldItem = new InventoryItem(null, 0);
        }

        inventory.CallSlotUpdateEvent(targetSlot);
    }
    #endregion
}
