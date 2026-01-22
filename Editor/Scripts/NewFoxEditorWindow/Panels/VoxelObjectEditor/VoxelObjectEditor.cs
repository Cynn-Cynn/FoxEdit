using System;
using System.Collections;
using System.Collections.Generic;
using FoxEdit.EditorUtils;
using FoxEdit.WindowComponents;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditor
    {
        private VisualElement root;

        private Button stopButton = null;
        private ToolbarElement toolToolbar;
        private ToolbarElement actionToolbar;


        public VoxelObjectEditor(VisualElement root)
        {
            this.root = root;
            GetElements();
            RegisterCallbacks();

            toolToolbar.SelectTool((int)VoxelEditor.Tool, false);
            actionToolbar.SelectTool((int)VoxelEditor.Action, false);
        }

        ~VoxelObjectEditor()
        {
            UnregisterCallbacks();
        }

        private void GetElements()
        {
            stopButton = root.Q<Button>("stop-edit-button");
            toolToolbar = root.Q<ToolbarElement>("tools");
            actionToolbar = root.Q<ToolbarElement>("actions");
        }

        private void RegisterCallbacks()
        {
            stopButton.clicked += OnClickStopEdit;
            toolToolbar.OnToolSelected += OnToolSelected;
            actionToolbar.OnToolSelected += OnActionSelected;
        }


        private void UnregisterCallbacks()
        {
            stopButton.clicked -= OnClickStopEdit;
            toolToolbar.OnToolSelected += OnToolSelected;
            actionToolbar.OnToolSelected += OnActionSelected;
        }

        private void OnActionSelected(int toolIndex)
        {
            VoxelEditor.Action = (VoxelTools.vxAction)toolIndex;
        }

        private void OnToolSelected(int toolIndex)
        {
            VoxelEditor.Tool = (VoxelTools.vxTool)toolIndex;
        }

        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnClickStopEdit()
        {
            FoxEditManager.StopEditVoxelObject();
        }

    }
}
