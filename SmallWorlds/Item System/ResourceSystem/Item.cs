using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;
using Unity.VisualScripting;


[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item")]
public class Item : ScriptableObject
{
    [SerializeField] protected string itemName = "Default Item Name";
    [SerializeField] protected string itemDescription = "Default Item Description";

    [SerializeField] protected int itemId = -1;
    [SerializeField][Tooltip("Weight per maxStack")] protected float itemWeight = 1;
    [SerializeField] protected int itemStackLimit = 100;
    [SerializeField][Tooltip("Fixed updates of power provided from item when used as fuel")] protected int itemBurnTime = 0;
    [SerializeField] protected Sprite sprite;

    // Getter methods using expression-bodied syntax
    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public int ItemId => itemId;
    public float ItemWeight => itemWeight;
    public int ItemStackLimit => itemStackLimit;
    public int ItemBurnTime => itemBurnTime;
    public Sprite Sprite => sprite;

    // Empty constructor required for ScriptableObjects
    public Item()
    {
    }

    public void Initialize(string name, int id, float weight, int stackLimit, Sprite sprite)
    {
        this.itemName = name;
        this.itemId = id;
        this.itemWeight = weight;
        this.itemStackLimit = stackLimit;
        this.sprite = sprite;
    }
}

public class ItemInstance
{
    protected int id;
    public int Id => id;
}
public class ToolInstance : ItemInstance
{
    private int maxDur;
    private int currentDur;
    private int durabilityDrainPerUse;

    public int MaxDur => maxDur;
    public int CurrentDur => currentDur;

    public ToolInstance(int id, int maxDur, int currentDur)
    {
        base.id = id;
        this.maxDur = maxDur;
        this.currentDur = currentDur;
    }

    public void AddDurability(int durabilityChange)
    {
        currentDur += durabilityChange;
    }

    public void SetDurability(int newDurability)
    {
        currentDur = newDurability;
    }

    public int OnUse()
    {
        currentDur -= durabilityDrainPerUse;
        return currentDur;
    }
}

#region Resources
/*
internal class Rock : Item
{
    internal Rock(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Rock properties and methods...
}

internal class Dust : Item
{
    internal Dust(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Dust properties and methods...
}

internal class Ice : Item
{
    internal Ice(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Ice properties and methods...
}

internal class Sludge : Item
{
    internal Sludge(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Sludge properties and methods...
}

internal class Scrap : Item
{
    internal Scrap(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Scrap properties and methods...
}

internal class Metal : Item
{
    internal Metal(string name, int id, float weight, int stackLimit, Sprite sprite) : base(name, id, weight, stackLimit, sprite)
    {

    }
    // Metal properties and methods...
}
*/

#endregion






