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
        var root = new TreeViewItem { id = 0, depth = -1, displayName = "Scenes" };

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
}
