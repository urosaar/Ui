using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class UIKeyBindTester : MonoBehaviour
{
    [SerializeField] private Button rebindButton;
    [SerializeField] private TMP_Text buttonText;

    private bool isWaitingForInput = false;
    private string previousKey; // Removed the hardcoded "I"

    // Variables for the blinking cursor
    private float blinkTimer = 0f;
    private float blinkSpeed = 0.5f;
    private bool showCursor = true;

    private void Start()
    {
        // Automatically grab whatever text is set in the Inspector on startup
        previousKey = buttonText.text;

        rebindButton.onClick.AddListener(StartRebindMode);
    }

    private void StartRebindMode()
    {
        if (isWaitingForInput) return;

        isWaitingForInput = true;
        previousKey = buttonText.text; // Save current key before changing in case of cancel
        rebindButton.interactable = false;

        // Reset blinker
        showCursor = true;
        blinkTimer = 0f;
        buttonText.text = "|";
    }

    private void Update()
    {
        if (!isWaitingForInput) return;

        // 1. Control the Blinking Cursor Effect
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkSpeed)
        {
            showCursor = !showCursor;
            buttonText.text = showCursor ? "|" : " ";
            blinkTimer = 0f;
        }

        // 2. Control and capture the Keyboard Input
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (KeyControl key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    // If player presses Escape, cancel and restore the previous key
                    if (key.name == "escape")
                    {
                        buttonText.text = previousKey;
                    }
                    else
                    {
                        // Otherwise, assign the new key
                        buttonText.text = key.displayName.ToUpper();
                    }

                    // End the input phase
                    isWaitingForInput = false;
                    rebindButton.interactable = true;
                    break;
                }
            }
        }
    }
}