using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

public class FISceneManagementWindow : EditorWindow {

    [SerializeField] TreeViewState m_TreeViewState;
    FISceneView m_SimpleTreeView;
    int rows = 0;

    private void OnEnable()
    {
        if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState();

        m_SimpleTreeView = new FISceneView(m_TreeViewState);
        rows = EditorBuildSettings.scenes.Length;
    }

    void OnGUI()
    {
        if (rows != EditorBuildSettings.scenes.Length)
        {
            m_SimpleTreeView.Reload();
            rows = EditorBuildSettings.scenes.Length;
        }
        m_SimpleTreeView.OnGUI(new Rect(0, 0, position.width, position.height));
    }

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Scene Management")]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        var window = GetWindow<FISceneManagementWindow>();
        window.titleContent = new GUIContent("Scenes");
        window.Show();
    }

}
