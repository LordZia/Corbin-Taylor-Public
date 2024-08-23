using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceSource : MonoBehaviour , IRespawnable , IHarvestable
{
    [SerializeField] private ResourceInfo[] resources;

    [SerializeField] private float maxHealth = 200;
    [SerializeField] private float remainingHealth = 200;

    [SerializeField] private bool addToRespawnQueueOnBreak = true;

    [SerializeField] private AudioType audioTypeOnHit = AudioType.Impact;
    [SerializeField] private AudioType audioTypeOnBreak = AudioType.RocksBreaking;

    public void Awake()
    {
        remainingHealth = maxHealth;
    }
    public void OnHit(Inventory inventory, float gainModifier, float damage)
    {
        AudioManager.Instance.PlaySound(audioTypeOnHit, this.transform.position);
        remainingHealth -= damage;

        foreach (var resourceInfo in resources)
        {
            int amountToAdd = Random.Range(resourceInfo.minAmountPerHit, resourceInfo.maxAmountPerHit + 1);
            inventory.AddItem(resourceInfo.item, amountToAdd);
        }

        // Destroy the resource source if it runs out of health
        if (remainingHealth <= 0)
        {
            Break();
        }
    }

    public virtual void Break()
    {
        OnBreak();

        if (addToRespawnQueueOnBreak)
        {
            ResourceManager.Instance.AddBrokenRespawnableToSpawnList(this);
            this.gameObject.SetActive(false);
        }
        else
        {
            Destroy(this);
        }
    }

    protected virtual void OnBreak()
    {

    }

    protected virtual void HandleBreakEffects()
    {
        AudioManager.Instance.PlaySound(audioTypeOnBreak, this.transform.position);
    }

    public void Respawn()
    {
        this.gameObject.SetActive(true);
        remainingHealth = maxHealth;

        OnRespawn();
    }

    public GameObject GetGameObject()
    {
        return this.gameObject;
    }

    protected virtual void OnRespawn()
    {

    }
}
