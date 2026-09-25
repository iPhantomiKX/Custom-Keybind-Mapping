using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeybindRowUI : MonoBehaviour
{
    public TextMeshProUGUI actionNameText;

    [Header("Keyboard/Mouse UI")]
    public Button keyboardButton;
    public TextMeshProUGUI keyboardText;

    [Header("Controller UI")]
    public Button controllerButton;
    public TextMeshProUGUI controllerText;

    private string actionName;

    public void SetupRow(string action, string currentKb, string currentJoy)
    {
        actionName = action;
        actionNameText.text = action;

        // Pass the raw data through the utility formatter for visual display
        keyboardText.text = InputDisplayFormatter.GetPrettyName(currentKb);
        controllerText.text = InputDisplayFormatter.GetPrettyName(currentJoy);

        // Bind layout actions
        keyboardButton.onClick.RemoveAllListeners();
        keyboardButton.onClick.AddListener(() => StartRebinding(isControllerSlot: false));

        controllerButton.onClick.RemoveAllListeners();
        controllerButton.onClick.AddListener(() => StartRebinding(isControllerSlot: true));
    }

    private void StartRebinding(bool isControllerSlot)
    {
        if (isControllerSlot)
        {
            controllerText.text = "Listening...";
            controllerButton.interactable = false;
        }
        else
        {
            // If it's an axis like Horizontal/Vertical, hint the steps contextually
            if (actionName == "Horizontal" || actionName == "Vertical")
                keyboardText.text = "Press Neg Key...";
            else
                keyboardText.text = "Press Key...";

            keyboardButton.interactable = false;
        }

        // Trigger dynamic listener cycle
        StartCoroutine(CustomInputManager.Instance.WaitAndRebind(actionName, isControllerSlot, (newRawValue) =>
        {
            // Re-enable and format cleanly once structural capture resolves safely
            if (isControllerSlot)
            {
                controllerText.text = InputDisplayFormatter.GetPrettyName(newRawValue);
                controllerButton.interactable = true;
                EventSystem.current.SetSelectedGameObject(controllerButton.gameObject);
                CustomInputManager.Instance.UpdateLastSelectedUI(controllerButton.gameObject);
            }
            else
            {
                // Note: The multi-step rebind callback passes "Press POSITIVE Key..." mid-process, 
                // which our formatter safely returns as text because it doesn't match a code block entry.
                keyboardText.text = InputDisplayFormatter.GetPrettyName(newRawValue);

                // Only re-enable the button once a valid key string has finished mapping 
                // (Checking if it still states standard instructions text)
                if (!newRawValue.Contains("Key..."))
                {
                    keyboardButton.interactable = true;
                    CustomInputManager.Instance.UpdateLastSelectedUI(keyboardButton.gameObject);
                }
            }
        }));
    }
}
