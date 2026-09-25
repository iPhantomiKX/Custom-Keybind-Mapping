using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybindMenuUI : MonoBehaviour
{
    public Transform container;     // Grid or Vertical Layout container
    public GameObject rowPrefab;    // UI row element prefab containing the KeybindRowUI script
    private Dictionary<string, KeybindData> activeBinds = new Dictionary<string, KeybindData>();

    private void Update()
    {
        if(activeBinds.Count == 0)
            GenerateMenuVisuals();
    }

    //private void OnEnable()
    //{
    //    GenerateMenuVisuals();
    //}

    public void GenerateMenuVisuals()
    {
        // Wipe old temporary instances
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Construct new display rows
        activeBinds = CustomInputManager.Instance.GetAllBindings();
        foreach (var kvp in activeBinds)
        {
            GameObject instantiatedRow = Instantiate(rowPrefab, container);
            KeybindRowUI rowScript = instantiatedRow.GetComponent<KeybindRowUI>();

            rowScript.SetupRow(
                kvp.Value.actionName,
                kvp.Value.keyboardBinding,
                kvp.Value.controllerBinding
            );
        }
    }
}
