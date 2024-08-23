using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Items/Consumable")]
public class ConsumableItem : Item
{
    [SerializeField] private InventoryItem itemToReturnWhenEmpty = new InventoryItem(null, -1);
    public Stats statChanges;
    public int maxUses;
    public int remainingUses;

    // Empty constructor required for ScriptableObjects
    public ConsumableItem()
    {
    }

    public void Initialize(Stats stats)
    {
        statChanges = stats;
        remainingUses = maxUses;
    }
    public virtual Stats OnConsume(int useCost, out int remainingUses, out InventoryItem invItem)
    {
        this.remainingUses -= 1;
        remainingUses = this.remainingUses;
        invItem = itemToReturnWhenEmpty;
        return statChanges;
    }
}
