using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Items/Structure")]
public class Structure : Tool
{
    [SerializeField] private GameObject structurePrefab;
    [SerializeField] public GameObject StructurePrefab => structurePrefab;
    [SerializeField] private int requiredStability;
    [SerializeField] private int providedStability;
    
    public int RequiredStability => requiredStability;  
    public int ProvidedStability => providedStability;
}