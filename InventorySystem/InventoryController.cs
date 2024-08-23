using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerStateMachine;
using static UnityEngine.Rendering.DebugUI;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private Color hoverColor = Color.green;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private GameObject menuUIContainer;
    [SerializeField] private GameObject crosshair;

    [SerializeField] private GameObject EscapeMenuContainer;

    [SerializeField][Tooltip("Stores a reference to the item drop container, there is a proper prefab to use in the item system folder.")]
    private GameObject droppedItemLootContainer;

    [SerializeField] private List<InventoryInterface> inventoryInterfaceList = new List<InventoryInterface>();
    [SerializeField] private List<InventoryInterface> activeInventoryInterfaces = new List<InventoryInterface>();

    [SerializeField] private float heldItemDisplayMoveSpeed = 25f;
    [SerializeField] private Image heldItemDisplay;
    [SerializeField] private Image itemPreviewWindowDisplay;

    [SerializeField] private GameObject playerHotBarObj;
    private HotBarController playerHotbar;

    [SerializeField] private InventoryItem heldItem;
    private InventoryItem hoveredItem;
    TargetInventorySlot hoveredSlot = new TargetInventorySlot(-1, -1);

    PlayerStateMachine playerState;
    playerMainState playerMainState = playerMainState.Alive;
    playerSubState currentPlayerState = playerSubState.CharacterControl;

    UICursorController cursorController;

    [SerializeField] private List<KeyCode> inventoryOpenKeys = new List<KeyCode>();
    [SerializeField] private List<KeyCode> inventoryCloseKeys = new List<KeyCode>();

    // Start is called before the first frame update
    private struct TargetInventorySlot
    {
        public int inventory;
        public int slot;

        public TargetInventorySlot(int inventory, int slot)
        {
            this.inventory = inventory;
            this.slot = slot;
        }
    }

    private void Awake()
    {
        playerState = this.gameObject.GetComponent<PlayerStateMachine>();
        cursorController = this.GetComponent<UICursorController>();

        playerState.OnStateChange += HandleStateChange;

        playerHotbar = this.GetComponent<HotBarController>();
        //playerHotbar.Initialize();

        cursorController = this.GetComponent<UICursorController>();

        cursorController.OnElementClick += OnUICursorClick;
        cursorController.OnElementHoverEnter += OnUICursorHover;
        cursorController.OnElementHoverExit += OnUICursorHoverExit;
        cursorController.OnElementRightClick += OnUICursorRightClight;

        heldItemDisplay.gameObject.SetActive(false);

        heldItem = new InventoryItem(null, 0);

        playerState.SetState(PlayerStateMachine.playerSubState.InventoryControl);
        playerState.SetState(PlayerStateMachine.playerSubState.CharacterControl);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Debug.Log("escape");
            if (playerMainState != playerMainState.Paused)
            {
                playerState.SetState(PlayerStateMachine.playerMainState.Paused);
            }
            else
            {
                playerState.SetState(PlayerStateMachine.playerMainState.Alive);
                playerState.SetState(PlayerStateMachine.playerSubState.CharacterControl);
            }
        }

        if (currentPlayerState == playerSubState.InventoryControl)
        {
            UpdateImageElementPosition(heldItemDisplay);
        }

        HandleItemDropping();

        if (currentPlayerState != playerSubState.InventoryControl)
        // player is not in inventory, check for opening inputs
        {
            foreach (KeyCode keyCode in inventoryOpenKeys)
            {
                // Check if the key corresponding to the current keycode is pressed if so open inventory
                if (Input.GetKeyDown(keyCode))
                {
                    if (currentPlayerState != playerSubState.InventoryControl)
                    {
                        playerState.SetState(PlayerStateMachine.playerSubState.InventoryControl);
                    }
                }
            }
        }
        else
        // player is in inventory, check for closing inputs
        {
            foreach (KeyCode keyCode in inventoryCloseKeys)
            {
                // Check if the key corresponding to the current keycode is pressed if so open inventory
                if (Input.GetKeyDown(keyCode) || Input.GetKeyDown(KeyCode.Escape))
                {
                    if (currentPlayerState == playerSubState.InventoryControl)
                    {
                        playerState.SetState(PlayerStateMachine.playerSubState.CharacterControl);
                    }
                }
            }
        }
        /*
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (currentPlayerState == playerSubState.InventoryControl)
            {
                playerState.SetState(PlayerStateMachine.playerSubState.CharacterControl);
            }
            else
            {
                playerState.SetState(PlayerStateMachine.playerSubState.InventoryControl);
            }
        }
        */
    }
    private void OnDestroy()
    {
        cursorController.OnElementClick -= OnUICursorClick;
        cursorController.OnElementHoverEnter -= OnUICursorHover;
        cursorController.OnElementHoverExit -= OnUICursorHoverExit;
        cursorController.OnElementRightClick -= OnUICursorRightClight;
    }

    public void OpenInventory(Inventory openedInventory)
    {
        switch (openedInventory.Type)
        {
            case Inventory.inventoryType.None:
                break;
            case Inventory.inventoryType.Inventory:
                OpenInventoryInterface(inventoryInterfaceList[0], openedInventory);
                break;
            case Inventory.inventoryType.Chest:
                OpenInventoryInterface(inventoryInterfaceList[1], openedInventory);
                break;
            case Inventory.inventoryType.Processor:
                OpenInventoryInterface(inventoryInterfaceList[2], openedInventory);
                break;
            default:
                break;
        }
    }
    public void CloseInventory(Inventory closedInventory)
    {
        switch (closedInventory.Type)
        {
            case Inventory.inventoryType.None:
                break;
            case Inventory.inventoryType.Inventory:
                CloseInventoryInterface(inventoryInterfaceList[0], closedInventory);
                break;
            case Inventory.inventoryType.Chest:
                CloseInventoryInterface(inventoryInterfaceList[1], closedInventory);
                break;
            case Inventory.inventoryType.Processor:
                CloseInventoryInterface(inventoryInterfaceList[2], closedInventory);
                break;
            default:
                break;
        }
    }

    private void CloseAllInventoryMenus()
    {
        menuUIContainer.SetActive(false);
    }

    private void HandleStateChange(playerMainState mainState, playerSubState subState)
    {
        if (mainState == PlayerStateMachine.playerMainState.Alive)
        {
            EscapeMenuContainer.SetActive(false);
            currentPlayerState = subState;

            if (subState == PlayerStateMachine.playerSubState.InventoryControl)
            {
                OpenInventory(playerInventory);
                menuUIContainer.SetActive(true);
                crosshair.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (heldItem.item != null)
                {
                    DropHeldItem();
                }

                CloseInventory(playerInventory);
                menuUIContainer.SetActive(false);
                crosshair.SetActive(true);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                for (int i = 0; i < activeInventoryInterfaces.Count; i++)
                {
                    Debug.Log(i);
                    CloseInventoryInterface(activeInventoryInterfaces[i], activeInventoryInterfaces[i].inventory);
                }
            }
        }
        else if (mainState == PlayerStateMachine.playerMainState.Paused)
        {
            menuUIContainer.SetActive(true);
            EscapeMenuContainer.SetActive(true);
            CloseAllInventoryMenus();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OpenInventoryInterface(InventoryInterface inventoryInterface, Inventory newInventory)
    {
        inventoryInterface.inventory = newInventory;
        inventoryInterface.OnOpenInterface(newInventory, this.gameObject);
        AddInventoryInterface(inventoryInterface);
    }  
    
    private void CloseInventoryInterface(InventoryInterface inventoryInterface, Inventory inventory)
    {
        if (inventoryInterface != playerInventory)
        {
            //inventoryInterface.inventory = null;
        }
        RemoveInventoryInterface(inventoryInterface);
        inventoryInterface.OnCloseInterface();
    }
    public void AddInventoryInterface(InventoryInterface newInventoryMenu)
    {
        activeInventoryInterfaces.Add(newInventoryMenu);
    }
    public void RemoveInventoryInterface(InventoryInterface newInventoryMenu)
    {
        activeInventoryInterfaces.Remove(newInventoryMenu);
    }
    public void OnUICursorHover(GameObject UIHoverTarget)
    {
        TargetInventorySlot targetInventorySlot = CheckClickTarget(UIHoverTarget);
        if (targetInventorySlot.inventory == -1 || targetInventorySlot.slot == -1)
        {
            return;
        }

        InventorySlotDisplay displaySlot = UIHoverTarget.GetComponent<InventorySlotDisplay>();
        if (displaySlot != null)
        {
            displaySlot.OnSlotHoverEnter(hoverColor);
        }

        Inventory targetInventory = activeInventoryInterfaces[targetInventorySlot.inventory].inventory;
        int targetSlot = targetInventorySlot.slot;
        if (targetInventory.InventorySlots.ContainsKey(targetSlot)) // found an item in slot, draw preview display
        {
            hoveredItem = targetInventory.InventorySlots[targetSlot];
            hoveredSlot = targetInventorySlot;


            itemPreviewWindowDisplay.enabled = true;

            itemPreviewWindowDisplay.GetComponentInChildren<TextMeshProUGUI>().text = hoveredItem.item.ItemName;
        }
        else if (heldItem.item != null) // is not currently hovering over an item, draw preview display to show held item info instead
        {
            itemPreviewWindowDisplay.GetComponentInChildren<TextMeshProUGUI>().text = heldItem.item.ItemName;
            itemPreviewWindowDisplay.enabled = true;
        }
        else // no item is held and no item is being hovered over; clear the display
        {
            hoveredItem = new InventoryItem(null, 0);
            itemPreviewWindowDisplay.enabled = false;

            itemPreviewWindowDisplay.GetComponentInChildren<TextMeshProUGUI>().text = "";
        }
    }
    private void OnUICursorHoverExit(GameObject exitedUIElement)
    {
        InventorySlotDisplay displaySlot = exitedUIElement.GetComponent<InventorySlotDisplay>();
        if (displaySlot != null)
        {
            displaySlot.OnSlotHoverExit();
        }
    }

    #region ItemMovingLogic

    public void OnUICursorRightClight(GameObject UIClickTarget)
    {
        TargetInventorySlot targetInventorySlot = CheckClickTarget(UIClickTarget);
        if (targetInventorySlot.inventory == -1 || targetInventorySlot.slot == -1)
        {
            return;
        }

        InventoryInterface targetInventoryInterface = activeInventoryInterfaces[targetInventorySlot.inventory];
        int targetSlot = targetInventorySlot.slot;

        if (heldItem.item != null) // an item is currently in hand, handle assosciated logic.
        {
            Debug.Log("I already have a held item, picking up something");
            InventoryItem singleItem = new InventoryItem(heldItem.item, 1);
            targetInventoryInterface.HandleItemPlacing(targetSlot, singleItem, ref heldItem);
        }
        else // helditem contains no data, it is empty;
        {
            // try pick up half stack
            InventoryItem pickUpItem = targetInventoryInterface.PickupItemFromSlot(targetSlot);
            if (pickUpItem.item != null)
            {
                int firstStack;
                int secondStack;
                Utility.SplitInt(pickUpItem.quantity, out firstStack, out secondStack);

                Debug.Log($"first stack = {firstStack} second stack = {secondStack}");
                InventoryItem placedItem = new InventoryItem(pickUpItem.item, secondStack);
                targetInventoryInterface.HandleItemPlacing(targetSlot, placedItem, ref heldItem);

                heldItem = new InventoryItem(pickUpItem.item, firstStack);
            }
        }

        UpdateHeldItemVisuals();
    }

    public void HandleItemDropping()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (heldItem.item != null)
            {
                DropHeldItem();
                return;
            }

            if (hoveredItem.item != null)
            {
                Debug.Log(hoveredItem.item.ItemName);
                DropHoveredItem();
            }
            
        }
    }

    private void DropHeldItem()
    {
        DropItem(heldItem);

        heldItem = new InventoryItem(null, 0);
        UpdateHeldItemVisuals();
    }
    private void DropHoveredItem()
    {
        DropItem(hoveredItem);
        InventoryInterface hoveredInventoryInterface = activeInventoryInterfaces[hoveredSlot.inventory];
        hoveredInventoryInterface.inventory.InventorySlots.Remove(hoveredSlot.slot);

        hoveredInventoryInterface.UpdateDisplay(hoveredSlot.slot);
    }
    private void DropItem(InventoryItem droppedItem)
    {
        GameObject spawnedObject = Instantiate(droppedItemLootContainer, this.transform.position, Quaternion.identity);
        spawnedObject.GetComponent<Rigidbody>().AddForce(this.transform.forward * 3, ForceMode.Force);
        ItemContainer itemContainer = spawnedObject.GetComponent<ItemContainer>();

        itemContainer.AddToContainer(droppedItem);
    }

    public void OnUICursorClick(GameObject UIClickTarget)
    {
        TargetInventorySlot targetInventorySlot = CheckClickTarget(UIClickTarget);
        //Debug.Log($" clicked on inv slot = {targetInventorySlot.inventory} : {targetInventorySlot.slot}");
        if (targetInventorySlot.inventory == -1 || targetInventorySlot.slot == -1)
        {
            return;
        }

        InventoryInterface targetInventoryInterface = activeInventoryInterfaces[targetInventorySlot.inventory];
        int targetSlot = targetInventorySlot.slot;

        if (heldItem.item != null) // an item is currently in hand, handle assosciated logic.
        {
            targetInventoryInterface.HandleItemPlacing(targetSlot, heldItem, ref heldItem);
        }
        else // helditem contains no data, it is empty;
        {
            heldItem = targetInventoryInterface.PickupItemFromSlot(targetSlot);
        }

        UpdateHeldItemVisuals();

        List<int> updatedSlots = new List<int>();
        updatedSlots.Add(targetSlot);
    }

    private TargetInventorySlot CheckClickTarget(GameObject UIClickTarget)
    {
        int inventory = -1;
        int slot = -1;

        for (int i = 0; i < activeInventoryInterfaces.Count; i++)
        {
            for (int j = 0; j < activeInventoryInterfaces[i].itemSlots.Count; j++)
            {
                if (activeInventoryInterfaces[i].itemSlots[j].gameObject == UIClickTarget)
                {
                    slot = j;
                    break;
                }

                if (activeInventoryInterfaces[i].itemSlots[j].GetComponentInChildren<RectTransform>().position == UIClickTarget.GetComponent<RectTransform>().position)
                {
                    slot = j;
                    break;
                }
            }

            if (slot != -1)
            {
                // found a valid slot in an inventory, unnecassary to check the other inventories
                inventory = i;
                break;
            }

        }

        if (inventory == -1 || slot == -1)
        {
            //Debug.Log("Click event was not over a UI Slot");
        }

        return new TargetInventorySlot(inventory, slot);
    }
    private void UpdateHeldItemVisuals()
    {
        if (heldItem.item == null)
        {
            heldItemDisplay.gameObject.SetActive(false);
            return;
        }
        heldItemDisplay.gameObject.SetActive(true);

        // Get the item texture from the item index
        InventoryItem inventoryItem = heldItem;
        Sprite itemSprite = inventoryItem.item.Sprite;

        heldItemDisplay.sprite = itemSprite;
        heldItemDisplay.enabled = true; // Show the image

        string textDisplay = "";
        if (heldItem.quantity > 1)
        {
            textDisplay = heldItem.quantity.ToString();
        }
        heldItemDisplay.GetComponentInChildren<TextMeshProUGUI>().text = textDisplay;

        //Vector2 canvasPosition = FindCursorCanvasPos();
        //heldItemDisplay.rectTransform.anchoredPosition = canvasPosition;
    }
    private void UpdateImageElementPosition(Image targetImage)
    {
        // Get the current position of the cursor in screen space
        Vector2 canvasPosition = FindCursorCanvasPos();

        // Smoothly move the image element to the cursor position
        Vector2 targetPosition = canvasPosition;
        Vector2 smoothedPosition = Vector2.Lerp(heldItemDisplay.rectTransform.anchoredPosition, targetPosition, heldItemDisplayMoveSpeed * Time.deltaTime);
        targetImage.rectTransform.anchoredPosition = smoothedPosition;

    }
    private Vector2 FindCursorCanvasPos()
    {
        Vector2 cursorPosition = Input.mousePosition;

        // Convert the screen position to canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            heldItemDisplay.rectTransform.parent as RectTransform, cursorPosition, null, out Vector2 canvasPosition);
        return canvasPosition;
    }
    #endregion
}

