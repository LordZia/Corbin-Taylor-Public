using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    public float damage;
    public MeshRenderer visibility;
    public int equipedWeaponID;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    public Enemy(float _maxHealth, int _weaponID)
    {
        maxHealth = _maxHealth;
        equipedWeaponID = _weaponID;
        currentHealth = maxHealth;
    }

    public void changeHealth(float _amount)
    {
        currentHealth += _amount;

        //make object killable
        if (currentHealth >= 0)
        {
            visibility.enabled = true;
        }
        if (currentHealth <= 0)
        {
            visibility.enabled = false;
        }
    }
}
