using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;
using static PlayerStateMachine;

[RequireComponent(typeof(Inventory))]
public class HotBarController : MonoBehaviour
{
    public int currentSlot = 1; // Initial slot
    public int totalSlots = 0; // Total number of slots

    [SerializeField] private GameObject menuUIContainer;
    [SerializeField] private Inventory playerInventory = new Inventory();

    [SerializeField] private Color selectedSlotColor = Color.white;
    [SerializeField] private Color unselectedSlotColor = Color.white;

    [SerializeField] List<int> inventoryHotBarSlotIDs = new List<int>();

    [SerializeField] private List<GameObject> slotHolsters = new List<GameObject>();
    [SerializeField] private Dictionary<int, IUseable> useableSlots = new Dictionary<int, IUseable>();
    [SerializeField] private Dictionary<int, ConsumableItem> consumableSlots = new Dictionary<int, ConsumableItem>();

    [SerializeField] private InventorySlotDisplay[] hotbarSlots = new InventorySlotDisplay[0];
    [SerializeField] private InventorySlotDisplay[] inventorySlotDisplays = new InventorySlotDisplay[0];

    [SerializeField][Tooltip("compare the difference between the inventory slot number to the related hotbar slot number to get this value")]
    private int inventorySlotToHotbarSlotConversionAmt = 24;

    [SerializeField] private Transform rightHandReferenceTransform = null;

    PlayerStateMachine playerState;

    bool isInventory = false;

    public event Action<GameObject> OnElementClick;
    public event Action<GameObject> OnElementHover;

    private void Awake()
    {
        totalSlots = hotbarSlots.Length;
        unselectedSlotColor = hotbarSlots[1].backgroundImage.color;

        //UpdateSlotsUsability(inventoryHotBarSlotIDs);

        playerInventory = this.GetComponent<Inventory>();

        SwitchSlot(1);

        playerState = GetComponent<PlayerStateMachine>();
        playerState.OnStateChange += HandleStateChange;

        playerInventory.OnSlotChange += OnInventoryUpdate;

        foreach (var display in inventorySlotDisplays)
        {
            display.Clear();
        }

        foreach (var holster in slotHolsters)
        {
            holster.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        playerState.OnStateChange -= HandleStateChange;
        playerInventory.OnSlotChange -= OnInventoryUpdate;
    }
    void Update()
    {
        if (isInventory) return;
        HandleInput();
    }
    private void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        if (subState == PlayerStateMachine.playerSubState.InventoryControl)
        {
            isInventory = true;
            menuUIContainer.SetActive(false);
        }
        else
        {
            isInventory = false;
            menuUIContainer.SetActive(true);
        }
    }

    private void UpdateSlotsUsability(List<int> updatedSlots)
    {
        foreach (int slot in updatedSlots)
        {
            int hotBarSlot = slot - inventorySlotToHotbarSlotConversionAmt;
            if (!inventoryHotBarSlotIDs.Contains(slot))
            {
                continue; // ignore all updated inventory slots that are not hotbar slots;
            }


            //Item item = hotbarSlots[hotBarSlot].InventoryItem.item;
            InventoryItem invItem;
            playerInventory.InventorySlots.TryGetValue(slot, out invItem);
            Item item = invItem.item;

            if (item == null) // remove item from slot if empty
            {
                Debug.Log(hotBarSlot + " was updated");
                foreach (Transform child in slotHolsters[hotBarSlot].transform)
                {
                    // Destroy the child GameObject
                    Destroy(child.gameObject);
                }

                if (consumableSlots.ContainsKey(hotBarSlot))
                {
                    consumableSlots.Remove(hotBarSlot);
                }

                if (useableSlots.ContainsKey(hotBarSlot))
                {
                    useableSlots.Remove(hotBarSlot);
                }

                continue;
            }
            
            if (useableSlots.ContainsKey(slot))
            {
                continue;
            }

            // Use type checking to determine if the item is a Tool
            if (item is Tool)
            {
                Tool tool = (Tool)item; // Type casting to access Tool-specific properties or methods

                GameObject toolWorldObject = Instantiate(tool.WorldObjectPrefab, this.transform);
                IUseable usableComponent = toolWorldObject.GetComponent<IUseable>();

                if (useableSlots.ContainsKey(hotBarSlot))
                {
                    useableSlots.Remove(hotBarSlot);
                }
                useableSlots.Add(hotBarSlot, usableComponent);

                toolWorldObject.transform.SetParent(slotHolsters[hotBarSlot].transform);
                toolWorldObject.transform.localPosition = Vector3.zero;


                if (usableComponent is Usable usable)
                {
                    usable.UsableItem = item;
                }

                toolWorldObject.SetActive(true);
                usableComponent.ConfigureReferences();
            }
            else if (item is ConsumableItem)
            {
                ConsumableItem consumable = (ConsumableItem)item;
                consumableSlots.Add(hotBarSlot, consumable);
            }
            else
            {
                if (consumableSlots.ContainsKey(hotBarSlot))
                {
                    consumableSlots.Remove(hotBarSlot);
                }

                if (useableSlots.ContainsKey(hotBarSlot))
                {
                    consumableSlots.Remove(hotBarSlot);
                }
            }
        }
    }

    private void OnInventoryUpdate(List<int> updatedSlots)
    {
        //UpdateSlotUsability();
        UpdateSlotsUsability(updatedSlots);

        return;
        string logMessage = "Int List: [";
        for (int i = 0; i < updatedSlots.Count; i++)
        {
            logMessage += updatedSlots[i].ToString();
            if (i < updatedSlots.Count - 1)
            {
                logMessage += ", ";
            }
        }
        logMessage += "]";
        Debug.Log(logMessage);

        return;
    }

    private void DrawHotBarSlot(int slot)
    {

    }

    void HandleInput()
    {
        bool useDefaultRightClickFunction = false;
        bool useDefaultLeftClickFunction = false;

        if (Input.GetMouseButtonDown(0))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                useableSlots[currentSlot].HandleLeftClick();
            }
            else
            {
                useDefaultLeftClickFunction = true;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                useableSlots[currentSlot].HandleLeftClickHold();
            }
            else
            {
                useDefaultLeftClickFunction = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                useableSlots[currentSlot].HandleLeftClickUp();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                int inventorySlot = currentSlot + inventorySlotToHotbarSlotConversionAmt;
                InventoryItem invItem = playerInventory.InventorySlots[inventorySlot];

                if (invItem.item is Tool tool)
                {

                    int newDur = 1;
                    if (newDur > 0)
                    {
                        // item has durability, handle use.
                        useableSlots[currentSlot].HandleRightClick();
                    }
                    else
                    {
                        // item is out of durability, remove from inventory.
                        playerInventory.RemoveItemFromSlot(inventorySlot, invItem.item, 1);
                        if (!playerInventory.InventorySlots.ContainsKey(inventorySlot))
                        {
                            useableSlots.Remove(currentSlot);
                        }
                    }
                }

            }
            else if (consumableSlots.ContainsKey(currentSlot))
            {
                HandleSlotConsume(currentSlot);
            }
            else
            {
                useDefaultRightClickFunction = true;
            }
        }
        if (Input.GetMouseButton(1))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                useableSlots[currentSlot].HandleRightClickHold();
            }
        }
        if (Input.GetMouseButtonUp(1))
        {
            if (useableSlots.ContainsKey(currentSlot))
            {
                useableSlots[currentSlot].HandleRightClickUp();
            }
        }

        if (useDefaultRightClickFunction == true)
        {
            // Input detected on non useable hotbar slot, use default right click functions
        }

        if (useDefaultLeftClickFunction == true)
        {
            // Input detected on non useable hotbar slot, use default left click functions
        }


        // Hot Bar Selection Inputs: 

        // Check for hotkey input to switch between hotbar slots
        for (int i = 1; i <= totalSlots; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                SwitchSlot(i);
                break; // Exit the loop after processing the input
            }
        }

        // Check for scroll wheel input to switch between hotbar slots
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int direction = scroll > 0 ? -1 : 1 ;
            int newSlot = currentSlot + direction;
            if (newSlot >= 1 && newSlot <= totalSlots)
            {
                SwitchSlot(newSlot);
            }
        }
    }

    private void HandleSlotConsume(int slotHolster)
    {
        int remainingUses;
        int inventorySlot = slotHolster + inventorySlotToHotbarSlotConversionAmt;
        ConsumableItem consumable = consumableSlots[currentSlot];
        InventoryItem postConsumeReturnedItem;
        Stats statChange = consumable.OnConsume(1, out remainingUses, out postConsumeReturnedItem);
        this.GetComponent<PlayerStats>().ApplyStatChange(statChange);

        Debug.Log($"attempting use {consumable.ItemName} in slot {inventorySlot}");
        if (remainingUses <= 0)
        {
            playerInventory.RemoveItemFromSlot(inventorySlot, consumable);
            if (consumableSlots.ContainsKey(currentSlot))
            {
                consumableSlots.Remove(currentSlot);
            }
        }
        if (postConsumeReturnedItem.item != null)
        {
            playerInventory.AddItemToSlot(inventorySlot, postConsumeReturnedItem);
        }
    }

    void SwitchSlot(int slot)
    {
        if (slot >= 1 && slot <= totalSlots - 1)
        {
            // deactivate previously selected slot
            hotbarSlots[currentSlot].backgroundImage.color = unselectedSlotColor;
            slotHolsters[currentSlot].gameObject.SetActive(false);

            if (useableSlots.ContainsKey(currentSlot))
            {
                if (useableSlots[currentSlot] is Usable usable)
                {
                    usable.UnEquip();
                }
            }

            // set up newely selected slot
            currentSlot = slot;
            hotbarSlots[slot].backgroundImage.color = selectedSlotColor;
            slotHolsters[slot].gameObject.SetActive(true);

            if (useableSlots.ContainsKey(slot))
            {
                if (useableSlots[slot] is Usable usable)
                {
                    usable.Equip();
                }
            }
        }
    }
}