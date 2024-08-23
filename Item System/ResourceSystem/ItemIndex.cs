using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemIndex : MonoBehaviour
{
    // Singleton instance
    private static ItemIndex instance;
    public static ItemIndex Instance { get { return instance; } }

    // Dictionary to store items with their IDs
    [SerializeField] private List<Item> itemDictionary = new List<Item>();

    private void Awake()
    {
        // Ensure only one instance of ItemIndex exists
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        // Unsubscribe from unity update callbacks.
        enabled = false;
    }


    // Method to retrieve a resource by its ID
    public Item GetItemByID(int id)
    {
        if (id > itemDictionary.Count || id < 0)
        {
            Debug.Log("invald ID requested from itemIndex : ID of : " + id);
            return null;
        }
        if (itemDictionary[id] != null)
        {
            return itemDictionary[id];
        }
        else
        {
            Debug.LogWarning("Resource with ID " + id + " not found in the item index.");
            return null;
        }
    }
}

public static class SpriteSheetSampler
{
    public static Sprite SampleSpriteRegion(Texture2D texture, int x, int y, int width, int height)
    {
        // Create a new texture for the sampled region
        Texture2D sampledTexture = new Texture2D(width, height);

        // Copy pixels from the sprite sheet texture to the sampled texture
        Color[] pixels = texture.GetPixels(x, y, width, height);
        sampledTexture.SetPixels(pixels);
        sampledTexture.Apply();

        // Create a new sprite from the sampled texture
        Sprite sampledSprite = Sprite.Create(sampledTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        return sampledSprite;
    }
}
