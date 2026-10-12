using FoxEdit.WindowComponents;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using FoxEdit.WindowPanels.SubPanels;
using System.Collections.Generic;
using FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers;
using UnityEditor;
using UnityEditor.EditorTools;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditorPanel
    {
        private VisualElement root;
        private Button stopButton;
        private VoxelRenderer _voxelRenderer;

        private Dictionary<Type, baseVoxelObjectEditorPanelHandler> handlers = new Dictionary<Type, baseVoxelObjectEditorPanelHandler>();

        public VoxelObjectEditorPanel(VisualElement root)
        {
            this.root = root;
            AddHandlers();
            GetElements();
            RegisterCallbacks();
            SetupFields();
        }

        ~VoxelObjectEditorPanel()
        {
            UnregisterCallbacks();
        }

#region Handlers
        private void AddHandlers()
        {
            AddHandler<ToolsHandler>();
            AddHandler<SaveHandler>();
            AddHandler<ColorsHandler>();
            AddHandler<AnimationsHandler>();
        }

        private void AddHandler<T>() where T : baseVoxelObjectEditorPanelHandler
        {
            handlers.Add(typeof(T), Activator.CreateInstance(typeof(T), root) as baseVoxelObjectEditorPanelHandler);
        }

        private bool TryGetHandler<T>(out T handler) where T : baseVoxelObjectEditorPanelHandler
        {
            if (handlers.TryGetValue(typeof(T), out var h))
            {
                handler = h as T;
                return true;
            }

            handler = null;
            return false;
        }
#endregion

        private void SetupFields()
        {
            foreach (var h in handlers)
                h.Value.SetupFields();
        }

        private void GetElements()
        {
            foreach (var h in handlers)
                h.Value.GetElements();

            stopButton = root.Q<Button>("stop-edit-button");
        }

        #region Callbacks
        private void RegisterCallbacks()
        {
            foreach (var h in handlers)
                h.Value.RegisterCallbacks();
            stopButton.clicked += OnClickStopEdit;

            root.RegisterCallback<ClickEvent>(FocusVoxelRendererIfNotSelected);

            FoxEditManager.OnStartEditVoxelObject += OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject += OnStopEditVoxelObject;
        }


        private void UnregisterCallbacks()
        {
            foreach (var h in handlers)
                h.Value.UnregisterCallbacks();
            stopButton.clicked -= OnClickStopEdit;
            root.UnregisterCallback<ClickEvent>(FocusVoxelRendererIfNotSelected);

            FoxEditManager.OnStartEditVoxelObject -= OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject -= OnStopEditVoxelObject;
        }

        private void OnStopEditVoxelObject()
        {
            foreach (var h in handlers)
                h.Value.StopEditVoxelObject();

            _voxelRenderer = null;
        }

        private void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            foreach (var h in handlers)
                h.Value.StartEditVoxelObject(voxelObject, voxelRenderer, voxelEditor);
            _voxelRenderer = voxelRenderer;
            FocusVoxelRenderer();

            SetupFields();
        }

        private void OnClickStopEdit()
        {
            FoxEditManager.StopEditVoxelObject();
        }

        #endregion

        private void FocusVoxelRendererIfNotSelected(ClickEvent onFocus)
        {
            if (_voxelRenderer == null)
                return;
            if (Selection.activeGameObject != _voxelRenderer.gameObject)
                FocusVoxelRenderer();
        }

        private void FocusVoxelRenderer()
        {
            Selection.activeObject = _voxelRenderer.gameObject;
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.FrameSelected();
        }

        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Undo()
        {
            if (TryGetHandler<ToolsHandler>(out ToolsHandler handler))
                handler.Undo();
        }

        public void Redo()
        {
            if (TryGetHandler<ToolsHandler>(out ToolsHandler handler))
                handler.Redo();
        }

    }
}
