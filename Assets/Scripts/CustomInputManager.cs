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
        horizontalValue = CalculateKeyboardAxis("Horizontal", horizontalValue);
        verticalValue = CalculateKeyboardAxis("Vertical", verticalValue);
        
        if(horizontalValue != 0 || verticalValue != 0)
            Debug.LogError("H: " +  horizontalValue + ", V: " + verticalValue);

        if (GetButtonDown("LightAttack"))
            Debug.LogError("LightAttack Button: is pressed");
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

