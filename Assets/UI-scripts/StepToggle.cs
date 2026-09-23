using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StepToggle : MonoBehaviour
{
    [Header("UI References")]
    public Button leftButton;
    public Button rightButton;
    public TMP_Text valueText;

    [Header("Options")]
    public List<string> options = new List<string>() { "OFF", "ON" };
    public int currentIndex = 0;

    void Start()
    {
        leftButton.onClick.AddListener(Previous);
        rightButton.onClick.AddListener(Next);
        ApplyValue();
    }

    public void Next()
    {
        if (currentIndex < options.Count - 1)
        {
            currentIndex++;
            ApplyValue();
        }
    }

    public void Previous()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ApplyValue();
        }
    }

    void ApplyValue()
    {
        if (options.Count == 0) return;

        currentIndex = Mathf.Clamp(currentIndex, 0, options.Count - 1);
        valueText.text = options[currentIndex];

        // Hide the arrows at the ends of the index list
        leftButton.gameObject.SetActive(currentIndex > 0);
        rightButton.gameObject.SetActive(currentIndex < options.Count - 1);
    }
}