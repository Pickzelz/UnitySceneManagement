using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;

public class FISceneManagementWindow : EditorWindow {

    [SerializeField] TreeViewState m_TreeViewState;
    FISceneView m_SimpleTreeView;
    int rows = 0;
    // ReorderableList to mimic Inspector List behaviour
    ReorderableList m_ReorderableList;
    List<EditorBuildSettingsScene> m_ScenesList;
    [SerializeField] bool m_AutoSave = true;
    bool m_IsDirty = false;

    private void OnEnable()
    {
        if (m_TreeViewState == null)
            m_TreeViewState = new TreeViewState();

        m_SimpleTreeView = new FISceneView(m_TreeViewState);
        rows = EditorBuildSettings.scenes.Length;
        
        // Initialize reorderable list from build settings
        m_ScenesList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        m_ReorderableList = new ReorderableList(m_ScenesList, typeof(EditorBuildSettingsScene), true, true, true, true);

        m_ReorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Scenes");
        };

    m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index < 0 || index >= m_ScenesList.Count)
                return;
            var scene = m_ScenesList[index];
            float toggleWidth = 16f;
            var toggleRect = new Rect(rect.x, rect.y + 2, toggleWidth, EditorGUIUtility.singleLineHeight);
            bool newEnabled = EditorGUI.Toggle(toggleRect, scene.enabled);
            if (newEnabled != scene.enabled)
            {
                scene.enabled = newEnabled;
                m_ScenesList[index] = scene; // write back since EditorBuildSettingsScene is a struct
                OnListChanged();
            }

            var labelRect = new Rect(rect.x + toggleWidth + 6f, rect.y + 2, rect.width - toggleWidth - 70f, EditorGUIUtility.singleLineHeight);
            var name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
            EditorGUI.LabelField(labelRect, name);
            // Open on double-click
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(scene.path);
                Event.current.Use();
            }

            var openRect = new Rect(rect.x + rect.width - 120f, rect.y + 2, 56f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(openRect, "Open", EditorStyles.miniButton))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(scene.path);
            }

            var btnRect = new Rect(rect.x + rect.width - 60f, rect.y + 2, 56f, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(btnRect, "Remove", EditorStyles.miniButton))
            {
                m_ScenesList.RemoveAt(index);
                OnListChanged();
            }
        };

        m_ReorderableList.onAddCallback = (ReorderableList list) => { AddScene(); };
        m_ReorderableList.onRemoveCallback = (ReorderableList list) => { OnListChanged(); };
        m_ReorderableList.onReorderCallback = (ReorderableList list) => { OnListChanged(); };
    }

    void OnGUI()
    {
        if (rows != EditorBuildSettings.scenes.Length)
        {
            m_SimpleTreeView.Reload();
            rows = EditorBuildSettings.scenes.Length;
        }
        // Toolbar with Add Scene button
        const float toolbarHeight = 20f;
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Add Scene", EditorStyles.toolbarButton))
        {
            AddScene();
        }

        GUILayout.Space(6);
        // Auto Save checkbox (visible)
        // Use ToggleLeft for a clear checkbox + label
        var newAuto = EditorGUILayout.ToggleLeft("Auto Save", m_AutoSave, GUILayout.Width(120));
        if (newAuto != m_AutoSave)
        {
            m_AutoSave = newAuto;
            if (m_AutoSave && m_IsDirty)
            {
                ApplyScenesListToBuildSettings();
                m_IsDirty = false;
            }
        }

        GUILayout.FlexibleSpace();
        // Save button (only meaningful when AutoSave is off)
        EditorGUI.BeginDisabledGroup(m_AutoSave || !m_IsDirty);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            ApplyScenesListToBuildSettings();
            m_IsDirty = false;
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.EndHorizontal();

        // Draw ReorderableList (Inspector-like) if available
        if (m_ReorderableList != null)
        {
            GUILayout.Space(4);
            m_ReorderableList.DoLayoutList();

            // Handle drag-and-drop from Project window to add scenes
            var lastRect = GUILayoutUtility.GetLastRect();
            var evt = Event.current;
            if (lastRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated)
                {
                    // Accept only .unity assets
                    bool hasScene = false;
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity"))
                        {
                            hasScene = true; break;
                        }
                    }
                    DragAndDrop.visualMode = hasScene ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
                    if (hasScene)
                        evt.Use();
                }
                else if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    bool added = false;
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;
                        var path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity"))
                            continue;
                        if (m_ScenesList == null)
                            m_ScenesList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                        if (!m_ScenesList.Exists(s => s.path == path))
                        {
                            m_ScenesList.Add(new EditorBuildSettingsScene(path, true));
                            added = true;
                        }
                    }
                    if (added)
                        OnListChanged();
                    evt.Use();
                }
            }
        }
        else
        {
            // Fallback to TreeView
            m_SimpleTreeView.OnGUI(new Rect(0, toolbarHeight, position.width, position.height - toolbarHeight));
        }
    }

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Scene Management")]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
    var window = GetWindow<FISceneManagementWindow>();
    window.titleContent = new GUIContent("Scene Management");
        window.Show();
    }

    // Opens a file picker to select a .unity scene and adds it to EditorBuildSettings if not already present
    private void AddScene()
    {
        // Start folder at Assets
        var abs = EditorUtility.OpenFilePanel("Select Scene", Application.dataPath, "unity");
        if (string.IsNullOrEmpty(abs))
            return;

        // Convert absolute path to project-relative path starting with "Assets/"
        if (!abs.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("Invalid Scene", "Please select a scene inside this Unity project (Assets folder).", "OK");
            return;
        }

        var relative = "Assets" + abs.Substring(Application.dataPath.Length).Replace("\\", "/");

        // Check for duplicates
        if (m_ScenesList == null)
            m_ScenesList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (var s in m_ScenesList)
        {
            if (s.path == relative)
            {
                EditorUtility.DisplayDialog("Already Added", $"Scene '{relative}' is already in Build Settings.", "OK");
                return;
            }
        }

        // Add scene (enabled by default)
        m_ScenesList.Add(new EditorBuildSettingsScene(relative, true));
        OnListChanged();
    }

    private void ApplyScenesListToBuildSettings()
    {
        if (m_ScenesList == null)
            m_ScenesList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        EditorBuildSettings.scenes = m_ScenesList.ToArray();
        rows = EditorBuildSettings.scenes.Length;
        if (m_SimpleTreeView != null)
            m_SimpleTreeView.Reload();
        m_IsDirty = false;
    }

    private void OnListChanged()
    {
        if (m_AutoSave)
        {
            ApplyScenesListToBuildSettings();
        }
        else
        {
            m_IsDirty = true;
        }
    }

}
