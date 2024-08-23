using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [System.Serializable]
    public class Ingredient
    {
        public Item item;
        public int amountRequired;
    }

    [SerializeField] private Ingredient[] ingredients;
    [SerializeField] private Item craftedItem;
    [SerializeField] private int craftedAmount = 1;

    public Ingredient[] Ingredients => ingredients;
    public Item CraftedItem => craftedItem;
    public int CraftedAmount => craftedAmount;
}


[CreateAssetMenu(fileName = "NewProcessingRecipe", menuName = "Crafting/ProcessingRecipe")]
public class ItemProcessingRecipe : CraftingRecipe
{
    [SerializeField] private ItemProcessorType processorType;
    [SerializeField] private float processingTime;

    // Getter methods for the new fields
    public ItemProcessorType ProcessorType => processorType;
    public float ProcessingTime => processingTime;
}

public enum ItemProcessorType
{
    Boiler,
    Electrolyser,
    Furnace
}
