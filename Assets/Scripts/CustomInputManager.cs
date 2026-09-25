using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomInputManager : MonoBehaviour
{
    public static CustomInputManager Instance;

    [SerializeField]
    private KeybindDataSO m_KeybindDataSO;

    // Internal lookup dictionary
    private Dictionary<string, KeybindData> keybinds = new Dictionary<string, KeybindData>();
    public Dictionary<string, KeybindData> GetAllBindings() => keybinds;
    private string savePath;

    public enum InputDeviceType { KeyboardMouse, Controller }
    public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;

    // Track the last selected UI object to restore selection when switching back to controller
    private GameObject lastSelectedUIObject;

    [Header("Axis Settings")]
    public float sensitivity = 3f;
    public float gravity = 3f;

    private float horizontalValue = 0f;
    private float verticalValue = 0f;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        savePath = Path.Combine(Application.persistentDataPath, "keybinds.json");
        LoadKeybinds();
    }

    public void SetDefaultKeybinds()
    {
        keybinds.Clear();

        foreach(KeybindData kbd in m_KeybindDataSO.bindings)
            keybinds[kbd.actionName] = new KeybindData { actionName = kbd.actionName, keyboardBinding = kbd.keyboardBinding, controllerBinding = kbd.controllerBinding };
    }

    private void Update()
    {
        // Smooth out the keyboard input simulation for axes
        //horizontalValue = CalculateKeyboardAxis("Horizontal", horizontalValue);
        //verticalValue = CalculateKeyboardAxis("Vertical", verticalValue);

        //if(horizontalValue != 0 || verticalValue != 0)
        //    Debug.LogError("H: " +  horizontalValue + ", V: " + verticalValue);

        //if (GetButtonDown("LightAttack"))
        //    Debug.LogError("LightAttack Button: is pressed");
        DetectActiveDevice();
    }

    public IEnumerator WaitAndRebind(string actionName, bool isControllerSlot, System.Action<string> onComplete)
    {
        yield return null; // Prevent UI click overlap

        // Check if the current action is an axis action (Horizontal or Vertical)
        bool isAxisAction = (actionName == "Horizontal" || actionName == "Vertical");
        string finalBindingString = "";

        if (isControllerSlot)
        {
            // --- CONTROLLER REBIND ---
            bool inputFound = false;
            while (!inputFound)
            {
                // 1. Check Analog Stick Axes (1 to 10)
                for (int axisNum = 1; axisNum <= 10; axisNum++)
                {
                    float value = Input.GetAxisRaw("JoystickAxis" + axisNum);
                    if (Mathf.Abs(value) > 0.7f) // Pushed hard in a direction
                    {
                        // Convert hardware axis indexes to your desired JSON names
                        if (axisNum == 1) finalBindingString = "LeftStickX";
                        else if (axisNum == 2) finalBindingString = "LeftStickY";
                        else finalBindingString = "JoystickAxis" + axisNum;

                        inputFound = true;
                        break;
                    }
                }

                // 2. Check Standard Buttons (Fallback)
                if (!inputFound && Input.anyKeyDown)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        KeyCode joyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
                        if (Input.GetKeyDown(joyCode))
                        {
                            finalBindingString = joyCode.ToString();
                            inputFound = true;
                            break;
                        }
                    }
                }
                yield return null;
            }
        }
        else
        {
            // --- KEYBOARD/MOUSE REBIND ---
            if (isAxisAction)
            {
                // Axis requires TWO steps: Negative (Left/Down) then Positive (Right/Up)
                onComplete?.Invoke("Press NEGATIVE Key...");
                KeyCode negativeKey = KeyCode.None;
                yield return StartCoroutine(ListenForSingleKeyboardKey(k => negativeKey = k));

                yield return new WaitForSeconds(0.2f); // Quick breathing room step

                onComplete?.Invoke("Press POSITIVE Key...");
                KeyCode positiveKey = KeyCode.None;
                yield return StartCoroutine(ListenForSingleKeyboardKey(k => positiveKey = k));

                finalBindingString = negativeKey.ToString() + "/" + positiveKey.ToString();
            }
            else
            {
                // Standard single button action
                KeyCode singleKey = KeyCode.None;
                yield return StartCoroutine(ListenForSingleKeyboardKey(k => singleKey = k));
                finalBindingString = singleKey.ToString();
            }
        }

        // Save to active profiles and dump to JSON
        if (keybinds.ContainsKey(actionName))
        {
            if (isControllerSlot) keybinds[actionName].controllerBinding = finalBindingString;
            else keybinds[actionName].keyboardBinding = finalBindingString;
            SaveKeybinds();
        }

        onComplete?.Invoke(finalBindingString);
    }

    private IEnumerator ListenForSingleKeyboardKey(System.Action<KeyCode> callback)
    {
        bool keyFound = false;
        while (!keyFound)
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if ((int)kcode >= (int)KeyCode.Mouse0 && (int)kcode <= (int)KeyCode.Mouse6) continue;
                    if (Input.GetKeyDown(kcode)) { callback?.Invoke(kcode); keyFound = true; break; }
                }
            }
            // Check mouse buttons manually
            if (!keyFound)
            {
                for (int m = 0; m <= 6; m++)
                {
                    if (Input.GetMouseButtonDown(m)) { callback?.Invoke((KeyCode)System.Enum.Parse(typeof(KeyCode), "Mouse" + m)); keyFound = true; break; }
                }
            }
            yield return null;
        }
    }

    private float CalculateKeyboardAxis(string actionName, float currentValue)
    {
        if (!keybinds.ContainsKey(actionName)) return 0f;

        string[] split = keybinds[actionName].keyboardBinding.Split('/');
        if (split.Length != 2) return 0f;

        // Parse strings back to executable KeyCodes
        KeyCode negativeKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), split[0]);
        KeyCode positiveKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), split[1]);

        float target = 0f;
        if (Input.GetKey(positiveKey)) target += 1f;
        if (Input.GetKey(negativeKey)) target -= 1f;

        float changeRate = (target != 0f) ? sensitivity : gravity;
        return Mathf.MoveTowards(currentValue, target, changeRate * Time.deltaTime);
    }

    public float GetCustomNavigationAxis(string axisName)
    {
        if (!keybinds.ContainsKey(axisName)) return 0f;
        var bind = keybinds[axisName];

        // 1. Check Controller Stick Names
        if (bind.controllerBinding == "LeftStickX") return Input.GetAxisRaw("JoystickAxis1");
        if (bind.controllerBinding == "LeftStickY") return Input.GetAxisRaw("JoystickAxis2");
        if (bind.controllerBinding.StartsWith("JoystickAxis")) return Input.GetAxisRaw(bind.controllerBinding);

        // 2. Check Keyboard / split formats (e.g., "A/D")
        string[] split = bind.keyboardBinding.Split('/');
        if (split.Length == 2)
        {
            if (System.Enum.TryParse(split[0], out KeyCode negKey) && System.Enum.TryParse(split[1], out KeyCode posKey))
            {
                float val = 0f;
                if (Input.GetKey(posKey)) val += 1f;
                if (Input.GetKey(negKey)) val -= 1f;
                return val;
            }
        }
        return 0f;
    }

    // --- CONTROLLER DETECTION FUNCTIONS ---

    private void DetectActiveDevice()
    {
        // 1. Detect Controller Activity
        if (DetectControllerInput())
        {
            if (CurrentDevice != InputDeviceType.Controller)
            {
                CurrentDevice = InputDeviceType.Controller;
                OnDeviceChanged(InputDeviceType.Controller);
            }
            return;
        }

        // 2. Detect Keyboard / Mouse Activity
        if (Input.anyKeyDown || Input.mousePresent && (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0))
        {
            if (CurrentDevice != InputDeviceType.KeyboardMouse)
            {
                CurrentDevice = InputDeviceType.KeyboardMouse;
                OnDeviceChanged(InputDeviceType.KeyboardMouse);
            }
        }
    }

    private bool DetectControllerInput()
    {
        // Check standard joystick buttons (0 to 19)
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i))) return true;
        }

        // Check standard joystick axes (1 to 10 for thumbsticks and triggers)
        for (int axisNum = 1; axisNum <= 10; axisNum++)
        {
            if (Mathf.Abs(Input.GetAxisRaw("JoystickAxis" + axisNum)) > 0.5f) return true;
        }

        return false;
    }

    private void OnDeviceChanged(InputDeviceType newDevice)
    {
        if (EventSystem.current == null) return;

        if (newDevice == InputDeviceType.KeyboardMouse)
        {
            // Cache what the player was looking at before deselecting
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                lastSelectedUIObject = EventSystem.current.currentSelectedGameObject;
            }

            // Clear current selection so mouse hovers cleanly without blue selection frames stuck behind
            EventSystem.current.SetSelectedGameObject(null);
        }
        else if (newDevice == InputDeviceType.Controller)
        {
            // Restore selection to the last highlighted item, or fall back to a default button if null
            if (lastSelectedUIObject != null && lastSelectedUIObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedUIObject);
            }
            else
            {
                // Fallback: Look for the first active button in the menu layout if needed
                Button firstButton = FindObjectOfType<Button>();
                if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }
    }

    /// <summary>
    /// Public helper for scripts to manually register what UI item was clicked/navigated to.
    /// </summary>
    public void UpdateLastSelectedUI(GameObject uiObject)
    {
        lastSelectedUIObject = uiObject;
    }

    // --- PUBLIC RUNTIME API ---

    public bool GetButton(string actionName)
    {
        if (!keybinds.ContainsKey(actionName)) return false;
        var bind = keybinds[actionName];

        // 1. Check Keyboard/Mouse Profile (Continuous check)
        if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey))
        {
            if (Input.GetKey(kbKey)) return true;
        }

        // 2. Check Controller Profile (Handles continuous analog inputs or button holds)
        if (bind.controllerBinding.StartsWith("JoystickAxis"))
        {
            // Triggers register as an analog spectrum (0.0 to 1.0)
            float axisValue = Input.GetAxisRaw(bind.controllerBinding);
            return axisValue > 0.5f; // Active if squeezed past 50% pressure
        }
        else if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey))
        {
            if (Input.GetKey(joyKey)) return true;
        }

        return false;
    }

    public bool GetButtonDown(string actionName)
    {
        if (!keybinds.ContainsKey(actionName)) return false;

        var bind = keybinds[actionName];

        // Check Keyboard/Mouse Slot
        if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey) && Input.GetKeyDown(kbKey)) return true;
        // Check Controller Slot
        if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey) && Input.GetKeyDown(joyKey)) return true;

        return false;
    }

    public float GetAxis(string axisName)
    {
        // 1. Try checking standard controller hardware maps first
        // Note: "Horizontal" and "Vertical" inside project settings are bound to thumbsticks by default
        float controllerInput = Input.GetAxisRaw(axisName);
        if (Mathf.Abs(controllerInput) > 0.19f) return controllerInput;

        // 2. Fall back to smoothed Keyboard structural variables
        if (axisName == "Horizontal") return horizontalValue;
        if (axisName == "Vertical") return verticalValue;

        return 0f;
    }

    ///// <summary>
    ///// Checks if a structural modifier binding is currently being held down.
    ///// </summary>
    //public bool IsModifierHeld(string modifierActionName, bool checkingController)
    //{
    //    if (!keybinds.ContainsKey(modifierActionName)) return false;

    //    var bind = keybinds[modifierActionName];

    //    if (checkingController)
    //    {
    //        // If the controller modifier is bound to a continuous Axis (like JoystickAxis10)
    //        if (bind.controllerBinding.StartsWith("JoystickAxis"))
    //        {
    //            float axisValue = Input.GetAxisRaw(bind.controllerBinding);
    //            return axisValue > 0.5f; // Held down if pulled past 50% pressure
    //        }
    //        // Fallback for standard buttons used as modifiers (e.g., LB/L1 via JoystickButton4)
    //        if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey))
    //        {
    //            return Input.GetKey(joyKey);
    //        }
    //    }
    //    else
    //    {
    //        // Check Keyboard/Mouse Profile Modifier
    //        if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey))
    //        {
    //            return Input.GetKey(kbKey);
    //        }
    //    }

    //    return false;
    //}

    ///// <summary>
    ///// Enhanced Button check that pairs a core action with a separate dedicated modifier mapping.
    ///// </summary>
    //public bool GetButtonDownWithModifier(string actionName, string modifierActionName)
    //{
    //    if (!keybinds.ContainsKey(actionName)) return false;
    //    var bind = keybinds[actionName];

    //    // 1. CHECK KEYBOARD/MOUSE PROFILE
    //    if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey))
    //    {
    //        if (Input.GetKeyDown(kbKey) && IsModifierHeld(modifierActionName, false))
    //        {
    //            return true;
    //        }
    //    }

    //    // 2. CHECK CONTROLLER PROFILE
    //    if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey))
    //    {
    //        if (Input.GetKeyDown(joyKey) && IsModifierHeld(modifierActionName, true))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    // --- JSON SYSTEM ---

    public void SaveKeybinds()
    {
        KeybindSaveData data = new KeybindSaveData();
        data.bindings.AddRange(keybinds.Values);
        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
    }

    public void LoadKeybinds()
    {
        SetDefaultKeybinds();
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            KeybindSaveData data = JsonUtility.FromJson<KeybindSaveData>(json);
            keybinds.Clear();
            foreach (var bind in data.bindings) keybinds[bind.actionName] = bind;
        }
        else
        {
            SaveKeybinds();
        }
    }
}

