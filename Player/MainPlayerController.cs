using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(RBCharacterController))]
[RequireComponent(typeof(InventoryInterface))]
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(UICursorController))]
[RequireComponent(typeof(PlayerStateMachine))]

public class MainPlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine playerState;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private InventoryInterface inventoryInterface;
    [SerializeField] private Inventory inventory;
    [SerializeField] private UICursorController cursorController;
    [SerializeField] private RBCharacterController characterController;
        

    // Start is called before the first frame update
    void Awake()
    {
        playerState = this.GetComponent<PlayerStateMachine>();
        playerStats = this.GetComponent<PlayerStats>();
        inventoryInterface = this.GetComponent<InventoryInterface>();
        inventory = this.GetComponent<Inventory>();

        cursorController = this.GetComponent<UICursorController>();
        characterController = this.GetComponent<RBCharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
}
