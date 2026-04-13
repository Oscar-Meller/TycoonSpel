using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(BuyPadActivator))]
public class BuyPadActivatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Inspector Helpers", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select one or more GameObjects in the Hierarchy and use the buttons below to manage targetsToEnable.", MessageType.Info);

        var targetComp = (BuyPadActivator)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selection to Targets"))
        {
            AddSelectionToTargets(targetComp);
        }

        if (GUILayout.Button("Set Selection as Only Target"))
        {
            SetSelectionAsOnlyTarget(targetComp);
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Targets"))
        {
            ClearTargets(targetComp);
        }
    }

    private void AddSelectionToTargets(BuyPadActivator targetComp)
    {
        var selections = Selection.gameObjects;
        if (selections == null || selections.Length == 0)
        {
            Debug.LogWarning("BuyPadActivator: No GameObjects selected in Hierarchy to add.");
            return;
        }

        var list = new List<GameObject>();
        if (targetComp.targetsToEnable != null)
            list.AddRange(targetComp.targetsToEnable);

        bool changed = false;
        foreach (var sel in selections)
        {
            if (sel == null) continue;
            if (!list.Contains(sel))
            {
                list.Add(sel);
                changed = true;
            }
        }

        if (changed)
        {
            Undo.RecordObject(targetComp, "Add Selection to targetsToEnable");
            targetComp.targetsToEnable = list.ToArray();
            EditorUtility.SetDirty(targetComp);
            Debug.Log($"BuyPadActivator: Added {selections.Length} selection(s) to targetsToEnable.");
        }
        else
        {
            Debug.Log("BuyPadActivator: Selection already present in targetsToEnable.");
        }
    }

    private void SetSelectionAsOnlyTarget(BuyPadActivator targetComp)
    {
        var sel = Selection.activeGameObject;
        if (sel == null)
        {
            Debug.LogWarning("BuyPadActivator: No active GameObject selected in Hierarchy to set as target.");
            return;
        }

        Undo.RecordObject(targetComp, "Set Selection as only target");
        targetComp.targetsToEnable = new GameObject[] { sel };
        EditorUtility.SetDirty(targetComp);
        Debug.Log($"BuyPadActivator: Set '{sel.name}' as the only target.");
    }

    private void ClearTargets(BuyPadActivator targetComp)
    {
        if (targetComp.targetsToEnable == null || targetComp.targetsToEnable.Length == 0)
        {
            Debug.Log("BuyPadActivator: targetsToEnable already empty.");
            return;
        }

        Undo.RecordObject(targetComp, "Clear targetsToEnable");
        targetComp.targetsToEnable = new GameObject[0];
        EditorUtility.SetDirty(targetComp);
        Debug.Log("BuyPadActivator: Cleared targetsToEnable.");
    }
}