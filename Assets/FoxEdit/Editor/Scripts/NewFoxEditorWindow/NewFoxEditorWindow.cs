using System;
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
    public static NewFoxEditorWindow currentWindow {get; private set;}

    [MenuItem("FoxEdit/NewFoxEditorWindow")]
    public static void Open()
    {
        currentWindow = GetWindow<NewFoxEditorWindow>();
        currentWindow.titleContent = new GUIContent("FoxEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        m_VisualTreeAsset.CloneTree(root);
        objectEditor = new VoxelObjectEditor(root.Q("voxel-object-editor"));

        voxelRendererSelectorContainer = root.Q("voxel-renderer-selector");
        voxelRendererSelectorElement = voxelRendererSelectorContainer.Q<VoxelRendererSelectorElement>();
        voxelRendererSelectorElement.onSelectVoxelObject += OnSelectVoxelObject;

        ShowVoxelObjectSelector();
    }

    private void OnEnable()
    {
        RegisterCallbacks();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
        FoxEditManager.StopEditVoxelObject();
    }

    private void RegisterCallbacks()
    {
        FoxEditManager.OnStartEditVoxelObject += ShowVoxelEditor;
        FoxEditManager.OnStopEditVoxelObject += ShowVoxelObjectSelector;
    }

    private void UnregisterCallbacks()
    {
        FoxEditManager.OnStartEditVoxelObject -= ShowVoxelEditor;
        FoxEditManager.OnStopEditVoxelObject -= ShowVoxelObjectSelector;
        voxelRendererSelectorElement.onSelectVoxelObject += OnSelectVoxelObject;
    }

    private void OnSelectVoxelObject(VoxelObject voxelObject)
    {
        Debug.Log("Start edit");
        FoxEditManager.StartEditVoxelObject(voxelObject);
    }

    public void ShowVoxelObjectSelector()
    {
        SetActivePanel(EPanel.Selector);

        List<VoxelObject> voxelObjects = AssetDatabaseUtility.FindAssetsByType<VoxelObject>();
        voxelRendererSelectorElement.UpdateVoxelObjectList(voxelObjects);
    }

    public void ShowVoxelEditor(VoxelObject voxelObject, VoxelRenderer voxelRenderer)
    {
        SetActivePanel(EPanel.Editor);
    }

    private void SetActivePanel(EPanel newPanel)
    {
        if (newPanel == currentPanel)
            return;
        voxelRendererSelectorContainer.style.display = newPanel == EPanel.Selector ? DisplayStyle.Flex : DisplayStyle.None;
        objectEditor.SetVisibility(newPanel == EPanel.Editor);
    }
}
