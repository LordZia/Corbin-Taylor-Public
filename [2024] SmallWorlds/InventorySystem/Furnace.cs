using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Furnace : Processor
{
    [SerializeField] private InventorySlotDisplay fuelSlot;
    [SerializeField] private InventorySlotDisplay processingSlot;
    [SerializeField] private InventorySlotDisplay outputSlot;
    [SerializeField] private Image progressBar;

    [SerializeField] private int[] furnaceSpecialSlotPositions;

    [SerializeField] private int currentBurnTime = 0;
    [SerializeField] private int maxProgressBarDisplayValue = 3000; // equivalent to 30 seconds
    [SerializeField] private bool containsExtraFuel = false;

    private void Awake()
    {
        furnaceSpecialSlotPositions = new int[3];
        furnaceSpecialSlotPositions[0] = 0;
        furnaceSpecialSlotPositions[1] = 1;
        furnaceSpecialSlotPositions[2] = 2;
    }
    private void FixedUpdate()
    {
        if (!base.isActive && !containsExtraFuel)
        {
            return;
        }

        if (currentBurnTime <= 0 && containsExtraFuel)
        {
            ConsumeFuel();
        }

        if (currentBurnTime > 0 )
        {
            currentBurnTime -= 1;

            float newFillAmount = Mathf.Clamp01((float)currentBurnTime / maxProgressBarDisplayValue);
            progressBar.fillAmount = newFillAmount;
        }
        else
        {
            currentBurnTime = 0;
            base.isActive = false;
        }
    }
    protected override void SlotUpdateEvent(int slot)
    {
        HandleSlotUpdate(slot);
    }
    protected override void SlotUpdateEvent(List<int> slots)
    {
        foreach (var slot in slots)
        {
            HandleSlotUpdate(slot);
        }
    }
    private void HandleSlotUpdate(int slot)
    {
        string listOfNums = " ";
        foreach (var num in furnaceSpecialSlotPositions)
        {
            listOfNums += num + " : ";
        }
        Debug.Log(furnaceSpecialSlotPositions.Length + " : length : " + listOfNums);

        Debug.Log("Detected item update in furnace : " + slot);

        bool containsValue = Array.Exists(furnaceSpecialSlotPositions, element => element == slot);
        if (!containsValue)
        {
            // updated inventory slot is not in the list of special slot positions, ignore it
            Debug.Log("Detected item update in non special furnace slots : " + slot);
            return;
        }
        if (slot == furnaceSpecialSlotPositions[0])
        {
            Debug.Log("Furnace detected fuel item");
            HandleFuelSlotUpdate();
            return;
        }
        if (slot == furnaceSpecialSlotPositions[1])
        {
            Debug.Log("Furnace detected cooking slot item");
            HandleProcessSlotUpdate();
            return;
        }
        if (slot == furnaceSpecialSlotPositions[2])
        {
            HandleItemOutputSlotUpdate();
        }
    }
    private void HandleFuelSlotUpdate()
    {
        if (!base.InventorySlots.ContainsKey(furnaceSpecialSlotPositions[0]))
        {
            return;
        }

        InventoryItem invItem = base.InventorySlots[furnaceSpecialSlotPositions[0]];
        if (invItem.item == null) return;
        if (invItem.item.ItemBurnTime != 0)
        {
            if (base.isActive)
            {
                Debug.Log("Furnace has extra fuel");
                containsExtraFuel = true;
                return;
            }
            if (!base.isActive)
            {
                Debug.Log("Furnace attempted to use fuel");
                if (invItem.quantity > 1)
                {
                    containsExtraFuel = true;
                }
                else
                {
                    containsExtraFuel = false;
                }
                ConsumeFuel();
            }
        }
        
    }
    private void ConsumeFuel()
    {
        if (!base.InventorySlots.ContainsKey(furnaceSpecialSlotPositions[0]))
        {
            return;
        }

        int targetInventorySlot = furnaceSpecialSlotPositions[0];
        InventoryItem invItem = base.InventorySlots[targetInventorySlot];
        if (invItem.item != null)
        {
            currentBurnTime += invItem.item.ItemBurnTime;
            base.RemoveItemFromSlot(targetInventorySlot, invItem.item, 1);
            isActive = true;
        }
    }
    private void HandleProcessSlotUpdate()
    {

    }

    private void CheckForValidRecipe()
    {

    }
    private void HandleItemOutputSlotUpdate()
    {

    }

    private void HandleProcessing()
    {

    }
}

public class Processor : Chest
{
    [SerializeField] protected bool isActive = false;
}
