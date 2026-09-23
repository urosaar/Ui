using UnityEngine;
using UnityEngine.UI;
using TMPro; // Use this if using TextMeshPro. Change to UnityEngine.UI if using standard Text.

public class SliderPercentage : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text percentageText;

    private void Start()
    {
        // Add a listener so the method runs every time the slider moves
        volumeSlider.onValueChanged.AddListener(UpdatePercentageText);

        // Call it once at start to set the initial text
        UpdatePercentageText(volumeSlider.value);
    }

    public void UpdatePercentageText(float value)
    {
        // Convert the 0-1 value to 0-100 and round it to a whole number
        float percentage = value * 100f;
        percentageText.text = Mathf.RoundToInt(percentage).ToString() + "%";
    }
}