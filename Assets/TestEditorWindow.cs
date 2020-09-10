using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestEditorWindow : EditorWindow
{
    [MenuItem("Window/WindowTest " +" %t",false,1)]
    static void Init()
    {
        TestEditorWindow window = (TestEditorWindow)GetWindow(typeof(TestEditorWindow));
        window.Show();
    }
}
