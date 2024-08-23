using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Inventory , IInteractable , IDisplayableMenu
{
    [SerializeField] private string interactionDescription = "Press E To Open";
    [SerializeField] private inventoryType invType = inventoryType.Inventory;
    [SerializeField] private Transform lidPivot;

    [SerializeField] private Vector3 openRotation = Vector3.zero;
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    private bool isOpen = false;

    private void Awake()
    {
        base.type = invType;
    }
    public string InteractDescription()
    {
        return interactionDescription;
    }

    public void OnInteract(GameObject gameObject)
    {
        PlayerStateMachine playerState = gameObject.GetComponent<PlayerStateMachine>();
        if (playerState != null)
        {
            playerState.SetState(PlayerStateMachine.playerSubState.InventoryControl);
        }

        InventoryController inventoryController = gameObject.GetComponent<InventoryController>();
        if (inventoryController != null)
        {
            OpenMenu(inventoryController);
        }
    }

    public void OpenMenu(InventoryController _inventoryController)
    {
        _inventoryController.OpenInventory(this);
    }

    public void CloseMenu(InventoryController _inventoryController)
    {
        _inventoryController.CloseInventory(this);
    }
    protected override void OnInventoryOpen()
    {
        isOpen = true;
        if (lidPivot == null)
        {
            Debug.LogError($"Chest component on object {this.gameObject.name} : does not have a lidPivot transfrom reference assigned, no animation will be played");
            return;
        }
        Debug.Log("Opening chest lid");
        lidPivot.transform.localRotation = Quaternion.Euler(openRotation);

    }
    protected override void OnInventoryClose()
    {
        isOpen = false;
        if (lidPivot == null)
        {
            Debug.LogError($"Chest component on object {this.gameObject.name} : does not have a lidPivot transfrom reference assigned, no animation will be played");
            return;
        }

        Debug.Log("Closing chest lid");
        lidPivot.transform.localRotation = Quaternion.Euler(closedRotation);
    }
}
