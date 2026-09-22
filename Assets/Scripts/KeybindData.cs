using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class KeybindData
{
    public string actionName;
    public string keyboardBinding; // Store as string for easy JSON serialization
    public string controllerBinding; // E.g., "JoystickButton2" or "JoystickAxis1"
}

[Serializable]
public class KeybindSaveData
{
    public List<KeybindData> bindings = new List<KeybindData>();
}

