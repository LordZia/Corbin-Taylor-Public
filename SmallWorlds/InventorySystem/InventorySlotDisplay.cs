using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotDisplay : MonoBehaviour
{
    [SerializeField] public Image backgroundImage;
    [SerializeField] protected Image iconImageDisplay;
    [SerializeField] protected TextMeshProUGUI quantityDisplay;

    [SerializeField] protected InventoryItem currentItem;

    private Color baseColor = Color.gray;

    private float hoverColorResetTimeDelay = 0.25f;
    [SerializeField] private float hoverColorResetTimer = 0;

    private bool isHovered = false;
    public InventoryItem InventoryItem { get { return currentItem; } }

    private void Awake()
    {
        // Ensure that slot displays are always empty on awake
        //ClearDisplay();

        if (quantityDisplay == null)
        {
            quantityDisplay = GetComponentInChildren<TextMeshProUGUI>();
            if (quantityDisplay == null)
            {
                Debug.LogError("Inventory slot display requires either itself or it's own child object to have a text mush pro element, " +
                    "adding one now to avoid errors. Please add one properly in the inspector.");
                quantityDisplay = this.AddComponent<TextMeshProUGUI>();
            }
        }

        baseColor = backgroundImage.color;
        //ClearDisplay();
    }
    public void OnInventoryOpen()
    {

    }
    public void OnInventoryClose()
    {
        backgroundImage.color = baseColor;
    }
    public void OnSlotHoverEnter(Color hoverColor)
    {
        backgroundImage.color = hoverColor;
    }
    public void OnSlotHoverExit()
    {
        backgroundImage.color = baseColor;
    }

    protected virtual void OnSlotUpdate()
    {

    }

    public void UpdateSlotVisuals(InventoryItem inventoryItem)
    {
        if (inventoryItem.item != null)
        {
            currentItem = inventoryItem;
            Sprite itemSprite = inventoryItem.item.Sprite;
            iconImageDisplay.sprite = itemSprite;
            iconImageDisplay.enabled = true; // Show the image

            string textDisplay = "";
            if (inventoryItem.quantity > 1)
            {
                textDisplay = inventoryItem.quantity.ToString();
            }
            quantityDisplay.text = textDisplay;
        }
        else
        {
            currentItem = new InventoryItem(null, -1);
            ClearDisplay();
        }

        OnSlotUpdate();
    }

    public void Clear()
    {
        ClearDisplay();
    }

    protected virtual void ClearDisplay()
    {
        iconImageDisplay.sprite = null; // Clear the sprite
        iconImageDisplay.enabled = false; // Hide the image
        quantityDisplay.text = "";
    }

}
