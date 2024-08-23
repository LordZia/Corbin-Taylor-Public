using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewEnemyData", menuName = "NPCs/Enemy Data")]
public class EnemyData : NPCData
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public void Initialize(string npcName, int npcID, int maxHealth)
    {
        base.Initialize(npcName, npcID);
        this.maxHealth = maxHealth;
    }
}
