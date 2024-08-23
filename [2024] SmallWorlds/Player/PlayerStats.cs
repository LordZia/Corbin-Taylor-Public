using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Import LINQ for the Cast method
using static PlayerStats;
using static PlayerStateMachine;
using UnityEngine.Playables;

public class PlayerStats : MonoBehaviour
{
    [Serializable]
    public class Stat
    {
        public int max;
        public int current;

        public Stat(int max)
        {
            this.max = max;
            this.current = max;
        }
        public Stat(int max, int current)
        {
            this.max = max;
            this.current = current;
        }
    }

    public Stat health = new Stat(100);
    public Stat water = new Stat(100);
    public Stat food = new Stat(100);
    public Stat oxygen = new Stat(100);

    public List<Stat> stats = new List<Stat>();


    //Drain Speed per call based on statDrainDelay: default value of 1 with a delay of 60 is one point drain per 10 seconds;
    private float waterDrainSpeed = 3f;
    private float foodDrainSpeed = 3f;
    private float oxygenDrainSpeed = 3f;

    [SerializeField]
    [Tooltip("Number of fixed update calls between stat drain calculations : 60 calls = 1 second")] private int statDrainDelay = 60;
    private int fixedUpdateCounter = 0;

    public float currentWaterDrainScale = 1f;
    public float currentFoodDrainScale = 1f;
    public float currentOxygenDrainScale = 1f;

    [SerializeField]
    [Tooltip("Damage amount per empty stat per tick")]
    private int emptyStatDamageAmount = -5;

    [SerializeField]
    [Tooltip("Number of fixed update calls between damage ticks")]
    private int damageDelay = 120;
    private int damageDelayCounter = 0;

    public event Action<int, int> OnHealthChange;
    public event Action<int, int> OnWaterChange;
    public event Action<int, int> OnFoodChange;
    public event Action<int, int> OnOxygenChange;

    private void Awake()
    {
        stats.Add(health);
        stats.Add(water);
        stats.Add(food);
        stats.Add(oxygen);

        PlayerInfoManager.Instance.AddPlayerInfo(this);
    }

    private void OnDisable()
    {
        PlayerInfoManager.Instance.RemovePlayerInfo(this);
    }
    private void OnDestroy()
    {
        PlayerInfoManager.Instance.RemovePlayerInfo(this);
    }

    public void CallEvents()
    {
        OnHealthChange?.Invoke(health.current, health.max);
        OnWaterChange?.Invoke(water.current, water.max);
        OnFoodChange?.Invoke(food.current, food.max);
        OnOxygenChange?.Invoke(oxygen.current, oxygen.max);
    }

    private void FixedUpdate()
    {
        fixedUpdateCounter++;
        if (fixedUpdateCounter > statDrainDelay) 
        {
            DrainStat(ref water, waterDrainSpeed * currentWaterDrainScale, OnWaterChange);
            DrainStat(ref food, foodDrainSpeed * currentFoodDrainScale, OnFoodChange);
            DrainStat(ref oxygen, oxygenDrainSpeed * currentOxygenDrainScale, OnOxygenChange);

            fixedUpdateCounter = 0;
        }


        damageDelayCounter++;
        if (damageDelayCounter > damageDelay)
        {
            CalculateDamage(ref water);
            CalculateDamage(ref food);
            CalculateDamage(ref oxygen);

            damageDelayCounter = 0;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ApplyDamage(15);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ApplyDamage(-15);
        }
    }
    private void DrainStat(ref Stat stat, float drainSpeed, Action<int, int> onChange)
    {
        if (stat.current > 0)
        {
            stat.current -= Mathf.RoundToInt(drainSpeed);
            stat.current = Mathf.Clamp(stat.current, 0, stat.max);
            onChange?.Invoke(stat.current, stat.max);
        }
        else
        {
            stat.current = 0;
        }
    }

    private void CalculateDamage(ref Stat stat)
    {
        int damageAmount = 0;
        if (stat.current <= 0)
        {
            damageAmount = emptyStatDamageAmount;
        }

        ApplyDamage(damageAmount);
    }

    public void ApplyDamage(int damageAmount)
    {
        health.current += damageAmount;

        health.current = Mathf.Clamp(health.current, 0, health.max);

        OnHealthChange?.Invoke(health.current, health.max);

        if (health.current == 0) // temp debug logic
        {
            Debug.Log("Player died, debug reviving until death logic is added");
            health.current = 100;
            water.current = 100;
            food.current = 100;
            oxygen.current = 100;

            CallEvents();
        }
    }

    // Methods to retrieve current valuess
    public int GetCurrentHealth()
    {
        return health.current;
    }

    public int GetCurrentWater()
    {
        return water.current;
    }

    public int GetCurrentFood()
    {
        return food.current;
    }

    public int GetCurrentOxygen()
    {
        return oxygen.current;
    }

    public void AddHealth(int addHealth)
    {
        int newValue = this.health.current + addHealth;
        int clampedValue = Mathf.Clamp(newValue, 0, this.health.max);

        this.health.current += addHealth;
        OnHealthChange?.Invoke(health.current, health.max);
    }
    public void AddFood(int addFood)
    {
        int newValue = this.food.current + addFood;
        int clampedValue = Mathf.Clamp(newValue, 0, this.food.max);

        this.food.current += addFood;
        OnFoodChange?.Invoke(food.current, food.max);
    }
    public void AddWater(int addWater)
    {
        int newValue = this.water.current + addWater;
        int clampedValue = Mathf.Clamp(newValue, 0, this.water.max);

        this.water.current += addWater;
        OnWaterChange?.Invoke(water.current, water.max);
    }
    public void AddOxygen(int addOxygen)
    {
        int newValue = this.oxygen.current + addOxygen;
        int clampedValue = Mathf.Clamp(newValue, 0, this.oxygen.max);

        this.oxygen.current = clampedValue;
        OnOxygenChange?.Invoke(oxygen.current, oxygen.max);
    }
    public void ApplyStatChange(Stats statChange)
    {
        AddHealth(statChange.Health);
        AddFood(statChange.Food);
        AddWater(statChange.Water);
        AddOxygen(statChange.Oxygen);

        CallEvents();
    }

    public void SetStats(Stats newStats)
    {
        this.health.current = newStats.Health;
        this.food.current = newStats.Food;
        this.water.current = newStats.Water;
        this.oxygen.current = newStats.Oxygen;

        CallEvents();
    }

    public void SetStats(List<Stat> newStats)
    {
        if (newStats.Count < 3)
        {
            Debug.Log("invalid statcount input value of : " + newStats.Count);
            return;
        }
        this.health.current = newStats[0].current;
        this.food.current = newStats[1].current;
        this.water.current = newStats[2].current;
        this.oxygen.current = newStats[3].current;

        CallEvents();
    }

    public void SetStats(List<int> newCurrentStats)
    {
        if (newCurrentStats.Count < 3)
        {
            Debug.Log("invalid statcount input value of : " + newCurrentStats.Count);
            return;
        }
        this.health.current = newCurrentStats[0];
        this.food.current = newCurrentStats[1];
        this.water.current = newCurrentStats[2];
        this.oxygen.current = newCurrentStats[3];

        CallEvents();
    }
}

[System.Serializable]
public class Stats
{
    public int Food;
    public int Water;
    public int Oxygen;
    public int Health;

    public Stats(int food, int water, int oxygen, int health)
    {
        Food = food;
        Water = water;
        Oxygen = oxygen;
        Health = health;
    }
}
