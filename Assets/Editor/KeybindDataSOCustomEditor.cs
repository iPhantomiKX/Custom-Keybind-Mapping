using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KeybindDataSO))]
public class KeybindDataSOCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        KeybindDataSO kbdso = (KeybindDataSO)target;

        GUILayout.Space(15);
        if (GUILayout.Button("Save Keybind Data", GUILayout.Height(30)))
        {
            Debug.Log("Keybind Data has been saved!");
            SaveKeybinds(kbdso);
        }
    }

    private void SaveKeybinds(KeybindDataSO keybindDataSO)
    {
        KeybindSaveData data = new KeybindSaveData();
        Dictionary<string, KeybindData> keybinds = new Dictionary<string, KeybindData>();

        keybinds.Clear();

        foreach (KeybindData kbd in keybindDataSO.bindings)
            keybinds[kbd.actionName] = new KeybindData { actionName = kbd.actionName, keyboardBinding = kbd.keyboardBinding, controllerBinding = kbd.controllerBinding };

        data.bindings.AddRange(keybinds.Values);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "keybinds.json"), JsonUtility.ToJson(data, true));
    }
}
