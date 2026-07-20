using FoxEdit.WindowComponents;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using FoxEdit.WindowPanels.SubPanels;
using System.Collections.Generic;
using FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditorPanel
    {
        private VisualElement root;
        private Button stopButton;

        private List<baseVoxelObjectEditorPanelHandler> handlers = new List<baseVoxelObjectEditorPanelHandler>();

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
            handlers.Add(Activator.CreateInstance(typeof(T), root) as baseVoxelObjectEditorPanelHandler);
        }
#endregion

        private void SetupFields()
        {
            handlers.ForEach(h => h.SetupFields());
        }

        private void GetElements()
        {
            handlers.ForEach(h => h.GetElements());

            stopButton = root.Q<Button>("stop-edit-button");
        }

        #region Callbacks
        private void RegisterCallbacks()
        {
            handlers.ForEach(h => h.RegisterCallbacks());
            stopButton.clicked += OnClickStopEdit;

            FoxEditManager.OnStartEditVoxelObject += OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject += OnStopEditVoxelObject;
        }

        private void UnregisterCallbacks()
        {
            handlers.ForEach(h => h.UnregisterCallbacks());
            stopButton.clicked -= OnClickStopEdit;

            FoxEditManager.OnStartEditVoxelObject -= OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject -= OnStopEditVoxelObject;
        }

        private void OnStopEditVoxelObject()
        {
            handlers.ForEach(h => h.StopEditVoxelObject());
        }

        private void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            handlers.ForEach(h => h.StartEditVoxelObject(voxelObject, voxelRenderer, voxelEditor));

            SetupFields();
        }

        private void OnClickStopEdit()
        {
            FoxEditManager.StopEditVoxelObject();
        }

        #endregion


        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

    }
}
