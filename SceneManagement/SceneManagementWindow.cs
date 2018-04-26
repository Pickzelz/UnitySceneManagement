using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

public class SceneManagementWindow : EditorWindow {

    [SerializeField] TreeViewState m_TreeViewState;
    SceneView m_SimpleTreeView;

    private void OnEnable()
    {
        if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState();

        m_SimpleTreeView = new SceneView(m_TreeViewState);
    }

    void OnGUI()
    {

        m_SimpleTreeView.OnGUI(new Rect(0, 0, position.width, position.height));
    }

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Scene Management")]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        var window = GetWindow<SceneManagementWindow>();
        window.titleContent = new GUIContent("Scenes");
        window.Show();
    }
}
