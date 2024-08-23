using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static CraftingRecipe;

public class CraftingMenuController : MonoBehaviour
{
    public GameObject craftingMenuContainer; // Parent container for crafting displays

    //private GameObject[] craftingDisplays; // Array to hold crafting displays

    [SerializeField] private CraftingIconDisplay[] craftingIconDisplays;
    private List<CraftingIconDisplay> activeCraftingDisplays = new List<CraftingIconDisplay>();

    private List<CraftingRecipe> currentlyCraftableRecipes = new List<CraftingRecipe>();
    private List<CraftingRecipe> currentlyDisplayedRecipes = new List<CraftingRecipe>();

    [SerializeField] private GameObject craftingRecipePreviewDisplay;
    [SerializeField] private Color craftableColor = Color.blue;
    [SerializeField] private Color nonCraftableColor = Color.gray;

    private Inventory inventory;
    private UICursorController cursorController;

    private int lastUIHoverTarget = -1;
    private void Awake()
    {
        // Get all children of the craftingMenuContainer
        int childCount = craftingMenuContainer.transform.childCount;
        //craftingDisplays = new GameObject[childCount];

        if (craftingRecipePreviewDisplay == null)
        {
            Debug.Log("Crafting menu requires a preview display object as referance, check player UI for it and assign it");
        }
        foreach (CraftingIconDisplay display in craftingIconDisplays)
        {
            display.gameObject.SetActive(false);
        }

        inventory = this.GetComponent<Inventory>();

        inventory.OnItemCountChange += OnInventoryUpdate;

        cursorController = this.GetComponent<UICursorController>();

        cursorController.OnElementClick += OnUICursorClick;
        cursorController.OnElementHoverEnter += OnUICursorHover;

        Invoke("UpdateRecipeMenu", 0.1f);
    }

    private void OnDestroy()
    {
        inventory.OnItemCountChange -= OnInventoryUpdate;

        cursorController.OnElementClick -= OnUICursorClick;
        cursorController.OnElementHoverEnter -= OnUICursorHover;
    }

    private void OnInventoryUpdate(List<InventoryItem> items)
    {
        UpdateRecipeMenu();
    }
    private void UpdateRecipeMenu()
    {
        List<CraftingRecipe> craftingRecipes = CraftingIndex.Instance.GetRecipesWithItems(inventory.HeldItemsCounter);

        /*
        string heldItems = "";
        foreach (KeyValuePair<Item, int> item in inventory.HeldItemsCounter)
        {
            heldItems += " : " + item.Key.ItemName + " " + item.Value.ToString();
        }
        Debug.Log("currently held items are : " + heldItems);
        */

        currentlyCraftableRecipes.Clear();
        currentlyDisplayedRecipes.Clear();

        Queue<CraftingRecipe> currentlyCraftable = new Queue<CraftingRecipe>();
        Queue<CraftingRecipe> craftingPreview = new Queue<CraftingRecipe>();

        for (int i = 0; i < craftingRecipes.Count; i++)
        {
            // Check if this recipe is currently craftable or not
            CraftingRecipe recipe = craftingRecipes[i];
            bool isCraftable = CraftingIndex.Instance.CheckRequiredMaterials(recipe, inventory.HeldItemsCounter);
            if (isCraftable)
            {
                currentlyCraftable.Enqueue(recipe);
                currentlyCraftableRecipes.Add(recipe);
            }
            else
            {
                craftingPreview.Enqueue(recipe);
            }
        }

        for (int i = 0; i < craftingIconDisplays.Length; i++)
        {
            activeCraftingDisplays.Clear();
            // This stack of if and else if statements ensures that craftable recipes are displayed first in the menu with preview windows shown after.
            // This is an easy way to sort the displayed recipes.
            if (currentlyCraftable.Count > 0)
            {
                // Configure craftable recipe display
                craftingIconDisplays[i].gameObject.SetActive(true);
                currentlyDisplayedRecipes.Add(currentlyCraftable.Peek());
                craftingIconDisplays[i].ConfigureCraftingDisplay(currentlyCraftable.Dequeue(), craftableColor);
                activeCraftingDisplays.Add(craftingIconDisplays[i]);
            }
            else if (craftingPreview.Count > 0)
            {
                // Configure non-craftable recipe display
                craftingIconDisplays[i].gameObject.SetActive(true);
                currentlyDisplayedRecipes.Add(craftingPreview.Peek());
                craftingIconDisplays[i].ConfigureCraftingDisplay(craftingPreview.Dequeue(), nonCraftableColor);
                activeCraftingDisplays.Add(craftingIconDisplays[i]);
            }
            else
            {
                craftingIconDisplays[i].gameObject.SetActive(false);
                craftingIconDisplays[i].Clear();
            }
        }
    }

    private void CraftItem(int targetSlot)
    {
        if (currentlyCraftableRecipes.Count <= 0) return;

        // craft target
        if (targetSlot >= 0 && targetSlot < currentlyCraftableRecipes.Count)
        {
            CraftingRecipe targetRecipe = currentlyCraftableRecipes[targetSlot];

            Ingredient[] ingredients = targetRecipe.Ingredients;

            foreach (Ingredient ingredient in ingredients)
            {
                inventory.RemoveItem(ingredient.item, ingredient.amountRequired);
            }

            inventory.AddItem(targetRecipe.CraftedItem, targetRecipe.CraftedAmount);
        }
    }

    #region MenuInteractionHandling

    public void OnButtonPress(GameObject UIElement)
    {
        Debug.Log("Pressed On Object" + UIElement.name);
    }
    public void OnUICursorClick(GameObject targetUIObject)
    {
        int targetSlot = CheckClickTarget(targetUIObject);

        CraftItem(targetSlot);
    }

    public void OnUICursorHover(GameObject targetUIObject)
    {
        int targetSlot = CheckClickTarget(targetUIObject);

        /*
        if (targetSlot == -1)
        {
            // dont display preview window when no crafting menu element is being hovered
            craftingRecipePreviewDisplay.SetActive(false);
            return;
        }
        */

        if (targetSlot == lastUIHoverTarget || targetSlot == -1)
        {
            // current target is the same as displayed element, no need change the display or update it 
            return;
        }

        craftingRecipePreviewDisplay.SetActive(true);
        lastUIHoverTarget = targetSlot;
        CraftingRecipe hoveredOverRecipe = currentlyDisplayedRecipes[targetSlot];

        StringBuilder previewTextBuilder = new StringBuilder();

        previewTextBuilder.AppendLine($"<color=orange>{hoveredOverRecipe.CraftedItem.ItemName}</color> {hoveredOverRecipe.CraftedItem.ItemDescription}");

        // Add crafting requirements for each ingredient
        if (currentlyDisplayedRecipes.Contains(hoveredOverRecipe))
        {
            foreach (var ingredient in hoveredOverRecipe.Ingredients)
            {
                // set color of ingredients text based on if current ingredient requirements are met or not
                string colorTag = CraftingIndex.Instance.CheckIfInventoryHasIngredient(inventory, ingredient) ? "green" : "red";
                previewTextBuilder.AppendLine($"Requires: <color={colorTag}>{ingredient.amountRequired} : {ingredient.item.ItemName} </color>");
            }
        }

        string previewText = previewTextBuilder.ToString();

        craftingRecipePreviewDisplay.GetComponentInChildren<TextMeshProUGUI>().text = previewText;
    }

    private int CheckClickTarget(GameObject UIClickTarget)
    {
        int targetSlot = -1;

        
        for (int i = 0; i < craftingIconDisplays.Length; i++)
        {
            GameObject parentObject = UIClickTarget.transform.parent.gameObject;
            if (craftingIconDisplays[i].gameObject == parentObject)
            {
                targetSlot = i;
                break;
            }
        }

        return targetSlot;
    }

    #endregion
}
