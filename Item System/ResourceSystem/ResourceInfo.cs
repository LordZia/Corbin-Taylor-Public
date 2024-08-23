using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceInfo", menuName = "Custom/ResourceInfo", order = 1)]
public class ResourceInfo : ScriptableObject
{
    public string sourceName;
    public Item item;
    public int minAmountPerHit = 5;
    public int maxAmountPerHit = 10;
}