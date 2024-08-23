using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : NPC , IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private GameObject spawnObjectOnDeath;

    // Define the delegate for the OnDamage event
    public delegate void OnDamageHandler(int remainingHealth);

    // Declare the OnDamage event
    public event OnDamageHandler OnDamage;

    protected override void OnAwake()
    {
        if (base.npcData is not EnemyData)
        {
            Debug.LogError($"{this.gameObject.name} is supposed to hold an EnemyData as an NPCData type, assign one in the inspector");
        }
    }
    protected override void OnUnpackData(NPCData _npcData)
    {
        EnemyData enemyData = _npcData as EnemyData;
        if (enemyData == null)
            return;

        this.maxHealth = enemyData.MaxHealth;
        this.currentHealth= enemyData.CurrentHealth;
    }

    public void ApplyDamage(float damage)
    {
        int newDamage = Mathf.RoundToInt(damage);
        int newCurHealth = currentHealth - newDamage;
        int clampedHealth = Mathf.Clamp(newCurHealth, 0, maxHealth);
        this.currentHealth = clampedHealth;

        // Raise the OnDamage event, if there are any subscribers
        OnDamage?.Invoke(this.currentHealth);

        if (this.currentHealth <= 0)
        {
            this.currentHealth = this.maxHealth;
            KillEntity();
        }

    }

    private void KillEntity()
    {
        if (spawnObjectOnDeath != null)
        {
            GameObject spawnedObject = Instantiate(spawnObjectOnDeath, this.transform.position, this.transform.rotation);
        }

        Destroy(this.gameObject);
    }
}
