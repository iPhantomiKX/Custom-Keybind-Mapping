using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        keyboardText.text = currentKb;
        controllerText.text = currentJoy;

        // Setup Button Listeners
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
            keyboardText.text = "Press Key...";
            keyboardButton.interactable = false;
        }

        // Call the Input Manager to listen for input
        StartCoroutine(CustomInputManager.Instance.WaitAndRebind(actionName, isControllerSlot, (newBindingValue) => 
        {
            // Callback executing once key is found
            if (isControllerSlot)
            {
                controllerText.text = newBindingValue;
                controllerButton.interactable = true;
            }
            else
            {
                keyboardText.text = newBindingValue;
                keyboardButton.interactable = true;
            }
        }));
    }
}
