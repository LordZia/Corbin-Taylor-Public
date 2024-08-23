using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using UnityEngine;
using UnityEngine.UI;

public class ItemPickupUIController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private PlayerStats player;
    private Inventory playerInventory;

    [SerializeField] private float displayResetDelay = 2;

    [SerializeField] private RectTransform displaySlider;

    [SerializeField] private List<ItemPickupUIDisplay> pickupDisplays = new List<ItemPickupUIDisplay>();

    private Dictionary<Item, ItemPickupUIDisplay> activePickupDisplays = new Dictionary<Item, ItemPickupUIDisplay>();

    private Coroutine timerCoroutine;
    private bool isTimerPaused = false;

    private bool isDisplayingInventoryUpdates = false;
    void Awake()
    {
        player = FindObjectOfType<PlayerStats>();
        playerInventory = player.gameObject.GetComponent<Inventory>();

        if (playerInventory == null)
        {
            Debug.LogError("Failed to locate player object", this);
        }

        if (displaySlider == null)
        {
            Debug.LogError("No Display slider transform is assigned to ItemPickupUIDisplay, please assign one in the inspector", this);
        }

        playerInventory.OnItemCountChange += HandleItemCountChange;

        // delay item add display to prevent the initial item load from displaying
        Invoke("ToggleDisplay", 0.1f);
    }

    private void ToggleDisplay()
    {
        isDisplayingInventoryUpdates = !isDisplayingInventoryUpdates;
    }

    void HandleItemCountChange(List<InventoryItem> changedItems)
    {
        if (!isDisplayingInventoryUpdates) return;

        for (int i = 0; i < changedItems.Count; i++)
        {
            if (activePickupDisplays.Count == pickupDisplays.Count)
            {
                // Maximum amount of active displays reached
                break;
            }

            if (changedItems[i].item == null) continue;

            if (activePickupDisplays.ContainsKey(changedItems[i].item))
            {
                // an active pickup display is already displaying this item, add the newely added quantity to the display and update it.
                activePickupDisplays[changedItems[i].item].AddToDisplayQuantity(changedItems[i].quantity);
            }
            else
            {
                // no active displays are displaying this item, initialize a new display for it
                ItemPickupUIDisplay lowestInactiveDisplay = GetLowestInactiveObject(pickupDisplays);

                activePickupDisplays[changedItems[i].item] = lowestInactiveDisplay;

                lowestInactiveDisplay.InitializeDisplay(changedItems[i].item, changedItems[i].quantity);
                lowestInactiveDisplay.gameObject.SetActive(true);
            }
        }

        RestartTimer();
    }

    private IEnumerator ResetDisplaysAfterDelay(float duration)
    {
        float timer = duration;

        while (timer > 0f)
        {
            if (!isTimerPaused)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(0.1f); // Small delay while paused
            }
        }

        // Timer has reached zero, execute the target method
        ResetDisplays();
    }


    private void ResetDisplays()
    {
        foreach (var display in pickupDisplays)
        {
            display.gameObject.SetActive(false);
        }

        activePickupDisplays.Clear();
    }


    public void PauseTimer()
    {
        isTimerPaused = true;
    }

    public void ResumeTimer()
    {
        isTimerPaused = false;
    }

    public void RestartTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(ResetDisplaysAfterDelay(displayResetDelay));
    }

    private IEnumerator LerpImageAlpha(List<Image> images, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float alphaValue = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            foreach (Image image in images)
            {
                Color currentColor = image.color;
                image.color = new Color(currentColor.r, currentColor.g, currentColor.b, alphaValue);
            }

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure the final alpha value is exactly 0
        foreach (Image image in images)
        {
            Color currentColor = image.color;
            image.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
        }
    }

    private ItemPickupUIDisplay GetLowestInactiveObject(List<ItemPickupUIDisplay> objects)
    {
        ItemPickupUIDisplay lowestObject = null;
        int lowestIndex = int.MaxValue;

        for (int i = 0; i < objects.Count; i++)
        {
            GameObject obj = objects[i].gameObject;
            if (!obj.activeSelf && i < lowestIndex) // Check if the GameObject is inactive and has a lower index
            {
                lowestIndex = i;
                lowestObject = obj.GetComponent<ItemPickupUIDisplay>();
            }
        }

        return lowestObject;
    }

    public List<T> FindComponentsInChildren<T>(GameObject targetObject) where T : Component
    {
        List<T> foundComponents = new List<T>();

        // Check if the targetObject itself has the component
        T component = targetObject.GetComponent<T>();
        if (component != null)
        {
            foundComponents.Add(component);
        }

        // Recursively search in child objects
        foreach (Transform child in targetObject.transform)
        {
            List<T> childComponents = FindComponentsInChildren<T>(child.gameObject);
            foundComponents.AddRange(childComponents);
        }

        return foundComponents;
    }
}
