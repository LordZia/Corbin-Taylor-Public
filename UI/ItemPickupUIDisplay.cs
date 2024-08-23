using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupUIDisplay : MonoBehaviour
{

    [Header("Individual display settings")]
    [SerializeField] private Image itemIconDisplay;
    [SerializeField] private TextMeshProUGUI itemNameDisplay;
    [SerializeField] private TextMeshProUGUI itemQuantityDisplay;

    [Header("General display settings")]
    [SerializeField] private Color displayColor;
    [SerializeField] private Image addItemIcon;
    [SerializeField] private Image removeItemIcon;

    int currentlyDisplayedQuantity = 0;

    public void InitializeDisplay(Item item, int quantity)
    {
        if (item != null)
        {
            itemIconDisplay.sprite = item.Sprite;
            itemNameDisplay.text = item.ItemName;

            currentlyDisplayedQuantity = quantity;
            SetQuantityText(quantity);
        }
    }

    private void SetQuantityText(int quantity)
    {
        string quantityPretext = "";
        if (quantity > 0)
        {
            quantityPretext = "+";

        }
        else if (quantity < 0)
        {
            quantityPretext = "-";

        }
        itemQuantityDisplay.text = quantityPretext + quantity.ToString();
    }

    public void AddToDisplayQuantity(int newQuantity)
    {
        currentlyDisplayedQuantity += newQuantity;
        SetQuantityText(currentlyDisplayedQuantity);
    }
}
