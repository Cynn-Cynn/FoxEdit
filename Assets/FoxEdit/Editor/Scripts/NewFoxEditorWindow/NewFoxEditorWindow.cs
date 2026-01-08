using System.Collections.Generic;
using FoxEdit;
using FoxEdit.WindowPanels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class NewFoxEditorWindow : EditorWindow
{
    private enum EPanel
    {
        None = -1,
        Selector,
        Editor
    }

    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private EPanel currentPanel = EPanel.None;
    private VoxelObjectEditor objectEditor = null;
    private VisualElement voxelRendererSelectorContainer = null;
    private VoxelRendererSelectorElement voxelRendererSelectorElement = null;

    [MenuItem("Window/UI Toolkit/NewFoxEditorWindow")]
    public static void ShowExample()
    {
        NewFoxEditorWindow wnd = GetWindow<NewFoxEditorWindow>();
        wnd.titleContent = new GUIContent("NewFoxEditorWindow");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        m_VisualTreeAsset.CloneTree(root);
        objectEditor = new VoxelObjectEditor(root.Q("voxel-object-editor"));
        voxelRendererSelectorContainer = root.Q("voxel-renderer-selector");
        voxelRendererSelectorElement = voxelRendererSelectorContainer.Q<VoxelRendererSelectorElement>();

        ShowVoxelRendererSelector();
    }

    public void ShowVoxelRendererSelector()
    {
        SetActivePanel(EPanel.Selector);

        List<VoxelObject> voxelObjects = AssetDatabaseUtility.FindAssetsByType<VoxelObject>();
        Debug.Log(voxelObjects.Count);
        voxelRendererSelectorElement.UpdateVoxelRendererList(voxelObjects);
    }

    private void SetActivePanel(EPanel newPanel)
    {
        if (newPanel == currentPanel)
            return;
        voxelRendererSelectorContainer.style.display = newPanel == EPanel.Selector ? DisplayStyle.Flex : DisplayStyle.None;
        Debug.Log(voxelRendererSelectorContainer.style.display);
        objectEditor.SetVisibility(newPanel == EPanel.Editor);
    }
}
