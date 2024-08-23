using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CraftingIndex : MonoBehaviour
{
    // Singleton instance
    private static CraftingIndex instance;

    // List to store crafting recipes
    [SerializeField] private List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

    // Property to access the singleton instance
    public static CraftingIndex Instance
    {
        get
        {
            // If the instance doesn't exist, find it or create a new one
            if (instance == null)
            {
                instance = FindObjectOfType<CraftingIndex>();

                // If no instance exists in the scene, create a new GameObject and add the CraftingIndex script to it
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("CraftingIndex");
                    instance = singletonObject.AddComponent<CraftingIndex>();
                }
            }
            return instance;
        }
    }

    // Method to add a crafting recipe to the index
    public void AddRecipe(CraftingRecipe recipe)
    {
        if (!craftingRecipes.Contains(recipe))
        {
            craftingRecipes.Add(recipe);
        }
    }

    // Method to remove a crafting recipe from the index
    public void RemoveRecipe(CraftingRecipe recipe)
    {
        if (craftingRecipes.Contains(recipe))
        {
            craftingRecipes.Remove(recipe);
        }
    }

    // Method to retrieve all crafting recipes from the index
    public List<CraftingRecipe> GetAllRecipes()
    {
        return craftingRecipes;
    }

    // Method to retrieve recipes containing any items from an input list of items

    public List<CraftingRecipe> GetRecipesWithItems(Dictionary<Item, int> items)
    {
        List<CraftingRecipe> recipesWithItems = new List<CraftingRecipe>();
        List<Item> itemsList= new List<Item>();
        foreach (KeyValuePair<Item, int> item in items)
        {
            itemsList.Add(item.Key);
        }

        return GetRecipesWithItems(itemsList);
    }
    public List<CraftingRecipe> GetRecipesWithItems(List<InventoryItem> inventoryItems)
    {
        List<CraftingRecipe> recipesWithItems = new List<CraftingRecipe>();

        List<Item> heldItems = new List<Item>();
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            heldItems.Add(inventoryItems[i].item);
        }

        return GetRecipesWithItems(heldItems);
    }
    public List<CraftingRecipe> GetRecipesWithItems(List<Item> items)
    {
        // Create a list to store recipes containing any items from the input list
        List<CraftingRecipe> recipesWithItems = new List<CraftingRecipe>();

        // Iterate through each recipe in the index
        foreach (CraftingRecipe recipe in craftingRecipes)
        {
            // Check if any of the recipe's ingredients match the items in the input list
            if (recipe.Ingredients.Any(ingredient => items.Contains(ingredient.item)))
            {
                recipesWithItems.Add(recipe);
            }
        }

        return recipesWithItems;
    }

    // Method to check if a recipe has the required crafting materials
    public bool CheckRequiredMaterials(CraftingRecipe recipe, Dictionary<Item, int> inventoryItems)
    {
        List<InventoryItem> items = new List<InventoryItem>();

        foreach (KeyValuePair<Item, int> item in inventoryItems)
        {
            items.Add(new InventoryItem(item.Key, item.Value));
        }
        return CheckRequiredMaterials(recipe, items);
    }

    public bool CheckRequiredMaterials(CraftingRecipe recipe, List<InventoryItem> inventoryItems)
    {
        // Iterate through each ingredient in the recipe
        foreach (CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
        {
            InventoryItem inventoryItem = new InventoryItem(null, 0);
            foreach (InventoryItem item in inventoryItems)
            {
                if (item.item == ingredient.item)
                {
                    inventoryItem = item;
                    break; // Exit the loop once the item is found
                }
            }
            //Debug.Log($"checking crafting for {recipe.CraftedItem.ItemName} : requires : {ingredient.amountRequired} : {ingredient.item.ItemName}");
            // If the inventory item is null or the quantity is less than required, return false
            if (inventoryItem.item == null || inventoryItem.quantity < ingredient.amountRequired)
            {
                return false;
            }
        }

        // All required materials are present
        return true;
    }

    public bool CheckIfInventoryHasIngredient(Inventory inventory, CraftingRecipe.Ingredient neededIngredient)
    {
        InventoryItem inventoryItem = new InventoryItem(null, 0);
        foreach (KeyValuePair<int , InventoryItem> slot in inventory.InventorySlots)
        {
            if (slot.Value.item == neededIngredient.item)
            {
                inventoryItem = slot.Value;
                break; // Exit the loop once the item is found
            }
        }

        // If the inventory item is null or the quantity is less than required, return false
        if (inventoryItem.item == null || inventoryItem.quantity < neededIngredient.amountRequired)
        {
            return false;
        }
            
        // All required materials are present
        return true;
    }
}
