using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomInputManager : MonoBehaviour
{
    public static CustomInputManager Instance;

    [SerializeField]
    private KeybindDataSO m_KeybindDataSO;

    // Internal lookup dictionary
    private Dictionary<string, KeybindData> keybinds = new Dictionary<string, KeybindData>();
    public Dictionary<string, KeybindData> GetAllBindings() => keybinds;
    private string savePath;

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
    }

    public IEnumerator WaitAndRebind(string actionName, bool isControllerSlot, System.Action<string> onComplete)
    {
        // Wait a frame to prevent immediately capturing the mouse click that hit the UI button
        yield return null;

        bool inputFound = false;
        string detectedBinding = "";

        while (!inputFound)
        {
            if (isControllerSlot)
            {
                // 1. Check Controller Analog Axes (Triggers / D-Pad axes mapped to slots 3-10)
                for (int axisNum = 3; axisNum <= 10; axisNum++)
                {
                    string axisName = "JoystickAxis" + axisNum;
                    // Check if an axis is pushed significantly past a deadzone
                    if (Mathf.Abs(Input.GetAxisRaw(axisName)) > 0.6f)
                    {
                        detectedBinding = axisName;
                        inputFound = true;
                        break;
                    }
                }

                // 2. Check Controller Buttons (JoystickButton0 to 19)
                if (!inputFound)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        KeyCode joyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
                        if (Input.GetKeyDown(joyCode))
                        {
                            detectedBinding = joyCode.ToString();
                            inputFound = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                // 3. Check Keyboard Inputs
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        // Exclude mouse values initially to prevent overlap issues
                        if ((int)kcode >= (int)KeyCode.Mouse0 && (int)kcode <= (int)KeyCode.Mouse6) continue;

                        if (Input.GetKeyDown(kcode))
                        {
                            detectedBinding = kcode.ToString();
                            inputFound = true;
                            break;
                        }
                    }
                }

                // 4. Check Mouse Clicks
                if (!inputFound)
                {
                    for (int m = 0; m <= 6; m++)
                    {
                        if (Input.GetMouseButtonDown(m))
                        {
                            detectedBinding = "Mouse" + m;
                            inputFound = true;
                            break;
                        }
                    }
                }
            }

            yield return null;
        }

        // Apply to the active settings profile dictionary
        if (keybinds.ContainsKey(actionName))
        {
            if (isControllerSlot)
                keybinds[actionName].controllerBinding = detectedBinding;
            else
                keybinds[actionName].keyboardBinding = detectedBinding;

            // Instantly write updates down to the persistent JSON file
            SaveKeybinds();
        }

        // Fire UI text refresh update
        onComplete?.Invoke(detectedBinding);
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

    /// <summary>
    /// Checks if a structural modifier binding is currently being held down.
    /// </summary>
    public bool IsModifierHeld(string modifierActionName, bool checkingController)
    {
        if (!keybinds.ContainsKey(modifierActionName)) return false;

        var bind = keybinds[modifierActionName];

        if (checkingController)
        {
            // If the controller modifier is bound to a continuous Axis (like JoystickAxis10)
            if (bind.controllerBinding.StartsWith("JoystickAxis"))
            {
                float axisValue = Input.GetAxisRaw(bind.controllerBinding);
                return axisValue > 0.5f; // Held down if pulled past 50% pressure
            }
            // Fallback for standard buttons used as modifiers (e.g., LB/L1 via JoystickButton4)
            if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey))
            {
                return Input.GetKey(joyKey);
            }
        }
        else
        {
            // Check Keyboard/Mouse Profile Modifier
            if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey))
            {
                return Input.GetKey(kbKey);
            }
        }

        return false;
    }

    /// <summary>
    /// Enhanced Button check that pairs a core action with a separate dedicated modifier mapping.
    /// </summary>
    public bool GetButtonDownWithModifier(string actionName, string modifierActionName)
    {
        if (!keybinds.ContainsKey(actionName)) return false;
        var bind = keybinds[actionName];

        // 1. CHECK KEYBOARD/MOUSE PROFILE
        if (System.Enum.TryParse(bind.keyboardBinding, out KeyCode kbKey))
        {
            if (Input.GetKeyDown(kbKey) && IsModifierHeld(modifierActionName, false))
            {
                return true;
            }
        }

        // 2. CHECK CONTROLLER PROFILE
        if (System.Enum.TryParse(bind.controllerBinding, out KeyCode joyKey))
        {
            if (Input.GetKeyDown(joyKey) && IsModifierHeld(modifierActionName, true))
            {
                return true;
            }
        }

        return false;
    }

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

