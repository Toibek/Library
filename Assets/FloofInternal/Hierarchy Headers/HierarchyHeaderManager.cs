using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class HierarchyHeaderManager
{
    [MenuItem("GameObject/Create Header %h", false, priority = 0)]
    static public void CreateHeader()
    {
        GameObject go = new GameObject("NEW HEADER");
        go.AddComponent<HierarchyHeader>();
    }

    static HierarchyHeaderManager()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= UpdateHierarchy;
        EditorApplication.hierarchyWindowItemOnGUI += UpdateHierarchy;
    }
    public static void UpdateHierarchy(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj != null && obj.GetComponent<HierarchyHeader>())
        {
            HierarchyHeader set = obj.GetComponent<HierarchyHeader>();

            set.BgColor.a = 1;
            EditorGUI.DrawRect(selectionRect, set.BgColor);

            GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"))
            {
                font = set.textFont,
                alignment = set.textAlignment,
                fontStyle = set.textStyle,
                normal = new GUIStyleState()
                {
                    textColor = set.textColor
                }
            };

            EditorGUI.LabelField(selectionRect, obj.name, style);
        }
    }
}