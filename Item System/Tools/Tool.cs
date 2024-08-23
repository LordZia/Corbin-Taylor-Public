using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "Items/Tool")]
public class Tool : Item
{
    [SerializeField] private int _maxDurability = 100;
    [SerializeField] private GameObject _worldObjectPrefab;
    [SerializeField] private int durabilityDrainPerUse = 1;

    // Additional getter methods for Tool-specific fields
    public int MaxDurability => _maxDurability;
    public GameObject WorldObjectPrefab => _worldObjectPrefab;

    // Empty constructor required for ScriptableObjects
    public Tool()
    {
    }

    // Method to initialize the tool
    public void Initialize(string name, int id, float weight, int stackLimit, int maxDurability, Sprite sprite)
    {
        base.Initialize(name, id, weight, stackLimit, sprite); // Call the InitializeItem method from the base class
        _maxDurability = maxDurability;
    }
}