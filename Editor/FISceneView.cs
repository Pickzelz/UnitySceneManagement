using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor;


public class FISceneView : TreeView{

    public List<SaveItem> allSaveitem;

    public class SaveItem
    {
        public TreeViewItem item;
        public string path;
    }


    public FISceneView(TreeViewState treeViewState)
        : base(treeViewState)
    {
        Reload();
    }

    public void ReloadData()
    {
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        // BuildRoot is called every time Reload is called to ensure that TreeViewItems 
        // are created from data. Here we create a fixed set of items. In a real world example,
        // a data model should be passed into the TreeView and the items created from the model.

        // This section illustrates that IDs should be unique. The root item is required to 
        // have a depth of -1, and the rest of the items increment from that.
    var root = new TreeViewItem { id = 0, depth = -1, displayName = "Scene Management" };

        List<TreeViewItem> allItems = new List<TreeViewItem>();
        allSaveitem = new List<SaveItem>();
        int ids = 1;
        foreach(var scene in EditorBuildSettings.scenes)
        {
            string path = scene.path;
            string[] explode = path.Split('/');
            var name = "";
            if(explode.Length > 0)
            {
                name = explode[explode.Length - 1].Replace(".unity", "");
            }

            SaveItem saveitem = new SaveItem();
            saveitem.item = new TreeViewItem { id = ids, depth = 1, displayName = name };
            saveitem.path = path;

            allItems.Add(saveitem.item);
            allSaveitem.Add(saveitem);
            ids++;
        }
        // Utility method that initializes the TreeViewItem.children and .parent for all items.
        SetupParentsAndChildrenFromDepths(root, allItems);

        // Return root of the tree
        return root;
    }

    protected override void DoubleClickedItem(int id)
    {
        base.DoubleClickedItem(id);

        SaveItem item = allSaveitem.Find(x => x.item.id == id);

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        
        EditorSceneManager.OpenScene(item.path);
        
    }

    // Enable dragging of tree items
    protected override bool CanStartDrag(CanStartDragArgs args)
    {
        return args.draggedItemIDs != null && args.draggedItemIDs.Count > 0;
    }

    protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
    {
        if (args.draggedItemIDs == null || args.draggedItemIDs.Count == 0)
            return;

        var paths = new List<string>();
        foreach (var id in args.draggedItemIDs)
        {
            var si = allSaveitem.Find(x => x.item.id == id);
            if (si != null)
                paths.Add(si.path);
        }

        if (paths.Count == 0)
            return;

        DragAndDrop.PrepareStartDrag();
        DragAndDrop.SetGenericData("ScenePaths", paths);
        // Required to show a dragging icon
        DragAndDrop.objectReferences = new UnityEngine.Object[0];
        DragAndDrop.StartDrag("Dragging Scenes");
    }

    protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
    {
        // Collect dropped paths from either our internal drag data OR from Project window object references
        var droppedPaths = new List<string>();

        var generic = DragAndDrop.GetGenericData("ScenePaths") as List<string>;
        if (generic != null && generic.Count > 0)
            droppedPaths.AddRange(generic);

        if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj == null)
                    continue;
                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".unity"))
                {
                    // Avoid duplicates in the dropped list
                    if (!droppedPaths.Contains(path))
                        droppedPaths.Add(path);
                }
            }
        }

        if (droppedPaths.Count == 0)
            return DragAndDropVisualMode.None;

        // Only allow dropping between root-level items
        if (args.parentItem != null && args.parentItem.id != 0)
            return DragAndDropVisualMode.None;

        if (args.performDrop)
        {
            // Build current ordered list of paths from allSaveitem
            var current = new List<string>();
            foreach (var si in allSaveitem)
                current.Add(si.path);

            // Remove dragged items that already exist in current, preserving relative order
            foreach (var d in droppedPaths)
                current.Remove(d);

            // Determine insert index
            int insertIndex = args.insertAtIndex;
            if (insertIndex < 0 || insertIndex > current.Count)
                insertIndex = current.Count;

            // Insert dropped items at insertIndex preserving their order
            current.InsertRange(insertIndex, droppedPaths);

            // Rebuild EditorBuildSettings.scenes preserving enabled flags when possible
            var oldScenes = EditorBuildSettings.scenes;
            var mapEnabled = new Dictionary<string, bool>();
            foreach (var s in oldScenes)
                mapEnabled[s.path] = s.enabled;

            var newScenes = new List<EditorBuildSettingsScene>();
            foreach (var p in current)
            {
                bool enabled = true;
                if (mapEnabled.ContainsKey(p))
                    enabled = mapEnabled[p];
                newScenes.Add(new EditorBuildSettingsScene(p, enabled));
            }

            EditorBuildSettings.scenes = newScenes.ToArray();

            // Refresh our internal list and TreeView
            Reload();
        }

        return DragAndDropVisualMode.Move;
    }

    // Remove selected items from build settings (with confirmation)
    public void RemoveSelected()
    {
        var selection = GetSelection();
        if (selection == null || selection.Count == 0)
        {
            EditorUtility.DisplayDialog("Remove Scenes", "No scene selected to remove.", "OK");
            return;
        }

        // Map selected ids to scene paths
        var toRemove = new List<string>();
        foreach (var id in selection)
        {
            var si = allSaveitem.Find(x => x.item.id == id);
            if (si != null)
                toRemove.Add(si.path);
        }

        if (toRemove.Count == 0)
        {
            EditorUtility.DisplayDialog("Remove Scenes", "No valid scenes selected to remove.", "OK");
            return;
        }

        string message = toRemove.Count == 1
            ? $"Remove scene '{toRemove[0]}' from Build Settings?"
            : $"Remove {toRemove.Count} selected scenes from Build Settings?";

        if (!EditorUtility.DisplayDialog("Confirm Remove", message, "Remove", "Cancel"))
            return;

        // Build new list excluding removed
        var current = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        current.RemoveAll(s => toRemove.Contains(s.path));

        EditorBuildSettings.scenes = current.ToArray();

        // Refresh internal data and TreeView
        Reload();
    }

    // Draw each row and add a per-row Remove button on the right
    protected override void RowGUI(RowGUIArgs args)
    {
        var rowRect = args.rowRect;
        const float removeWidth = 60f;
        const float arrowWidth = 22f; // for up/down
        var totalButtons = removeWidth + arrowWidth * 2 + 8f; // padding

        // Reserve space for the buttons on the right
        var labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width - totalButtons, rowRect.height);

        // Use base drawing for the label/icon/selection but restrict its rect so it doesn't overlap the buttons
        var newArgs = args;
        newArgs.rowRect = labelRect;
        base.RowGUI(newArgs);

        // Draw Up/Down and Remove buttons on the right
        var x = rowRect.x + rowRect.width - totalButtons + 4f;
        var upRect = new Rect(x, rowRect.y + 2f, arrowWidth, rowRect.height - 4f);
        x += arrowWidth + 2f;
        var downRect = new Rect(x, rowRect.y + 2f, arrowWidth, rowRect.height - 4f);
        x += arrowWidth + 6f;
        var removeRect = new Rect(x, rowRect.y + 2f, removeWidth, rowRect.height - 4f);

        if (GUI.Button(upRect, "▲", EditorStyles.miniButton))
        {
            MoveItemById(args.item.id, -1);
        }

        if (GUI.Button(downRect, "▼", EditorStyles.miniButton))
        {
            MoveItemById(args.item.id, +1);
        }

        if (GUI.Button(removeRect, "Remove", EditorStyles.miniButton))
        {
            RemoveSceneById(args.item.id);
        }
    }

    // Move selected items up by one position (or down)
    public void MoveSelectedUp()
    {
        MoveSelectedByDelta(-1);
    }

    public void MoveSelectedDown()
    {
        MoveSelectedByDelta(+1);
    }

    private void MoveSelectedByDelta(int delta)
    {
        var selection = GetSelection();
        if (selection == null || selection.Count == 0)
            return;

        var selectedPaths = new HashSet<string>();
        foreach (var id in selection)
        {
            var si = allSaveitem.Find(x => x.item.id == id);
            if (si != null)
                selectedPaths.Add(si.path);
        }

        if (selectedPaths.Count == 0)
            return;

        var current = new List<string>();
        foreach (var si in allSaveitem)
            current.Add(si.path);

        // If delta < 0 => move up; delta > 0 => move down
        if (delta < 0)
        {
            // Move up: scan left to right, when a selected block has a non-selected before it, move the block up by one
            int i = 1;
            while (i < current.Count)
            {
                if (selectedPaths.Contains(current[i]) && !selectedPaths.Contains(current[i - 1]))
                {
                    // find block start
                    int start = i;
                    while (start - 1 >= 0 && selectedPaths.Contains(current[start - 1])) start--;
                    // find block end
                    int end = i;
                    while (end + 1 < current.Count && selectedPaths.Contains(current[end + 1])) end++;

                    // move block [start..end] before start-1
                    var block = current.GetRange(start, end - start + 1);
                    var before = current[start - 1];
                    current.RemoveRange(start, end - start + 1);
                    int insertIndex = current.IndexOf(before);
                    current.InsertRange(insertIndex, block);

                    i = insertIndex + block.Count; // continue after moved block
                }
                else
                {
                    i++;
                }
            }
        }
        else
        {
            // Move down: scan right to left
            int i = current.Count - 2;
            while (i >= 0)
            {
                if (selectedPaths.Contains(current[i]) && !selectedPaths.Contains(current[i + 1]))
                {
                    // find block start
                    int start = i;
                    while (start - 1 >= 0 && selectedPaths.Contains(current[start - 1])) start--;
                    // find block end
                    int end = i;
                    while (end + 1 < current.Count && selectedPaths.Contains(current[end + 1])) end++;

                    var block = current.GetRange(start, end - start + 1);
                    var after = current[end + 1];
                    current.RemoveRange(start, end - start + 1);
                    int insertIndex = current.IndexOf(after) + 1;
                    current.InsertRange(insertIndex, block);

                    i = start - 1; // continue left of moved block
                }
                else
                {
                    i--;
                }
            }
        }

        // Apply to EditorBuildSettings (preserve enabled flags)
        var oldScenes = EditorBuildSettings.scenes;
        var mapEnabled = new Dictionary<string, bool>();
        foreach (var s in oldScenes)
            mapEnabled[s.path] = s.enabled;

        var newScenes = new List<EditorBuildSettingsScene>();
        foreach (var p in current)
        {
            bool enabled = true;
            if (mapEnabled.ContainsKey(p))
                enabled = mapEnabled[p];
            newScenes.Add(new EditorBuildSettingsScene(p, enabled));
        }

        EditorBuildSettings.scenes = newScenes.ToArray();

        Reload();
    }

    // Move a single item by id by delta (-1 or +1)
    private void MoveItemById(int id, int delta)
    {
        var si = allSaveitem.Find(x => x.item.id == id);
        if (si == null)
            return;

        var current = new List<string>();
        foreach (var s in allSaveitem)
            current.Add(s.path);

        int idx = current.IndexOf(si.path);
        if (idx < 0)
            return;

        int target = idx + delta;
        if (target < 0 || target >= current.Count)
            return;

        // Swap or move block if consecutive selections? For single move, swap positions idx and target
        var temp = current[idx];
        current.RemoveAt(idx);
        current.Insert(target, temp);

        // Apply to EditorBuildSettings
        var oldScenes = EditorBuildSettings.scenes;
        var mapEnabled = new Dictionary<string, bool>();
        foreach (var s in oldScenes)
            mapEnabled[s.path] = s.enabled;

        var newScenes = new List<EditorBuildSettingsScene>();
        foreach (var p in current)
        {
            bool enabled = true;
            if (mapEnabled.ContainsKey(p))
                enabled = mapEnabled[p];
            newScenes.Add(new EditorBuildSettingsScene(p, enabled));
        }

        EditorBuildSettings.scenes = newScenes.ToArray();

        Reload();
    }

    // Remove a single scene by tree item id
    private void RemoveSceneById(int id)
    {
        var si = allSaveitem.Find(x => x.item.id == id);
        if (si == null)
        {
            EditorUtility.DisplayDialog("Remove Scene", "Could not find the selected scene.", "OK");
            return;
        }

        var path = si.path;
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Remove Scene", "Invalid scene path.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm Remove", $"Remove scene '{path}' from Build Settings?", "Remove", "Cancel"))
            return;

        var current = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        current.RemoveAll(s => s.path == path);
        EditorBuildSettings.scenes = current.ToArray();

        // Refresh internal data and TreeView
        Reload();
    }
}
