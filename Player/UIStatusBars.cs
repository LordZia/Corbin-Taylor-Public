using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIStatusBars : MonoBehaviour
{
    // Health bar is intended to be a mirrored horizontal display in the top of the player's screen.
    [SerializeField] private Image healthLeft;
    [SerializeField] private Image healthRight;

    [SerializeField] private List<Image> statusBars = new List<Image>();
    [SerializeField] private Image waterBar;
    [SerializeField] private Image foodBar;
    [SerializeField] private Image oxygenBar;

    [SerializeField] private Color maxStatusBarColor = Color.white;
    [SerializeField] private Color minStatusBarColor = Color.white;

    [SerializeField] private Color maxHealthColor = Color.white;
    [SerializeField] private Color minHealthColor = Color.white;

    [SerializeField] private Color emptyStatusFlashColor = Color.white;
    [SerializeField] private Color emptyStatusColor = Color.white;

    [SerializeField] private float emptyStatusFlashDur = 0.5f;

    // delay of 60 = 1 second
    [SerializeField] private int flashDelay = 30;
    private int fixedUpdateCounter = 0;
    private void Awake()
    {
        var playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChange += UpdateHealthBar;
            playerStats.OnWaterChange += UpdateWaterBar;
            playerStats.OnFoodChange += UpdateFoodBar;
            playerStats.OnOxygenChange += UpdateOxygenBar;
        }
        else
        {
            Debug.LogError("UIStatusBars: Unable to locate PlayerStats class");
        }

        statusBars.Clear();
        statusBars.Add(oxygenBar);
        statusBars.Add(waterBar);
        statusBars.Add(foodBar);
    }

    private void FixedUpdate()
    {
        if (fixedUpdateCounter <= flashDelay)
        {
            fixedUpdateCounter++;
        }
        else
        {
            fixedUpdateCounter = 0;

            foreach (Image image in statusBars)
            {
                if (image.fillAmount == 0)
                {
                    LerpColors(image, image.color, emptyStatusFlashColor, emptyStatusFlashDur);
                }
            }
        }
    }

    private void ChangeFillAmount(Image image, int currentValue, int maxValue, Color colorMin, Color colorMax)
    {
        image.fillAmount = (float)currentValue / (float)maxValue;

        image.color = Color.Lerp(colorMin, colorMax, image.fillAmount);
    }

    private void OnDestroy()
    {
        var playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChange -= UpdateHealthBar;
            playerStats.OnWaterChange -= UpdateWaterBar;
            playerStats.OnFoodChange -= UpdateFoodBar;
            playerStats.OnOxygenChange -= UpdateOxygenBar;
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        ChangeFillAmount(healthLeft, currentHealth, maxHealth, minHealthColor, maxHealthColor);
        ChangeFillAmount(healthRight, currentHealth, maxHealth, minHealthColor, maxHealthColor);
    }

    private void UpdateWaterBar(int currentHealth, int maxHealth)
    {
        ChangeFillAmount(waterBar, currentHealth, maxHealth, minStatusBarColor, maxStatusBarColor);
    }

    private void UpdateFoodBar(int currentHealth, int maxHealth)
    {
        ChangeFillAmount(foodBar, currentHealth, maxHealth, minStatusBarColor, maxStatusBarColor);
    }

    private void UpdateOxygenBar(int currentHealth, int maxHealth)
    {
        ChangeFillAmount(oxygenBar, currentHealth, maxHealth, minStatusBarColor, maxStatusBarColor);
    }

    IEnumerator LerpColors(Image image, Color startColor, Color endColor, float lerpDuration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < lerpDuration)
        {
            image.color = Color.Lerp(startColor, endColor, elapsedTime / lerpDuration);
            image.fillAmount = 1;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Swap start and end colors
        Color temp = startColor;
        startColor = endColor;
        endColor = temp;

        // Reverse the lerp
        elapsedTime = 0f;
        while (elapsedTime < lerpDuration)
        {
            image.color = Color.Lerp(startColor, endColor, elapsedTime / lerpDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
