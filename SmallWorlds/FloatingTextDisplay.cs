using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FloatingTextDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private string displayText;
    public FloatingTextDisplay(string displayText)
    {
        this.displayText = displayText;
        textMeshProUGUI = this.AddComponent<TextMeshProUGUI>();
        textMeshProUGUI.text = displayText;
    }

    public void UpdateDisplay(string newText)
    {
        textMeshProUGUI.text = newText;
    }
    public void UpdateDisplayTransform(Transform newPos)
    {
        this.transform.position = newPos.position;
        this.transform.rotation = newPos.rotation;
    }
}
