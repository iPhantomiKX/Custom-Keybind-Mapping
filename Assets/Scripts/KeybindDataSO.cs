using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KeybindDataSO", menuName = "ScriptableObjects/new Keybind Data", order = 1)]
public class KeybindDataSO : ScriptableObject
{
    public List<KeybindData> bindings = new List<KeybindData>();
}
