using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CraftingIconDisplay : InventorySlotDisplay
{
    [SerializeField] private TextMeshProUGUI nameDisplay;

    private void Awake()
    {
        // blank awake call overides the base awake method preventing display from clearing itself on awake.
        // wierd but works - zia
    }
    public void ConfigureCraftingDisplay(CraftingRecipe recipe, Color backgroundColor)
    {
        base.backgroundImage.color = backgroundColor;
        base.backgroundImage.enabled = true;

        base.iconImageDisplay.enabled = true; // Show the image

        base.iconImageDisplay.sprite = recipe.CraftedItem.Sprite;
        base.quantityDisplay.text = recipe.CraftedAmount.ToString();

        nameDisplay.text = recipe.CraftedItem.ItemName.ToString();
    }

    protected override void ClearDisplay()
    {
        base.ClearDisplay();
        base.backgroundImage.enabled = false;
        nameDisplay.text = "";
    }
}
