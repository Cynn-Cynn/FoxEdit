

using System;
using System.Diagnostics;
using FoxEdit.VoxelTools;
using FoxEdit.WindowComponents;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class ToolsHandler : baseVoxelObjectEditorPanelHandler
    {
        private ToolbarElement _toolToolbar;
        private ToolbarElement _actionToolbar;
        private Button _undoButton;
        private Button _redoButton;

        public ToolsHandler(VisualElement root) : base(root)
        {
        }

        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
        }

        public override void GetElements()
        {
            _toolToolbar = _root.Q<ToolbarElement>("tools");
            _actionToolbar = _root.Q<ToolbarElement>("actions");
            _undoButton = _root.Q<Button>("undo-button");
            _redoButton = _root.Q<Button>("redo-button");
        }


        public override void SetupFields()
        {
            _toolToolbar.SelectTool((int)VoxelEditor.Tool, false);
            _actionToolbar.SelectTool((int)VoxelEditor.Action, false);
        }

        public override void RegisterCallbacks()
        {
            VoxelEditor.OnChangeAction += OnChangeAction;
            VoxelEditor.OnChangeTool += OnChangeTool;
            VoxelEditor.CanRedoChanged += OnCanRedoChanged;
            VoxelEditor.CanUndoChanged += OnCanUndoChanged;
            _toolToolbar.OnToolSelected += OnToolSelected;
            _actionToolbar.OnToolSelected += OnActionSelected;
            _undoButton.clickable.clicked += Undo;
            _redoButton.clickable.clicked += Redo;
        }


        public override void UnregisterCallbacks()
        {
            VoxelEditor.OnChangeAction -= OnChangeAction;
            VoxelEditor.CanUndoChanged -= OnCanUndoChanged;
            VoxelEditor.CanRedoChanged -= OnCanRedoChanged;
            VoxelEditor.OnChangeTool -= OnChangeTool;
            _toolToolbar.OnToolSelected -= OnToolSelected;
            _actionToolbar.OnToolSelected -= OnActionSelected;
            _undoButton.clickable.clicked -= Undo;
            _redoButton.clickable.clicked -= Redo;
        }
        private void OnCanUndoChanged(bool canUndo)
        {
            UnityEngine.Debug.Log("Undo: " + canUndo);
            _undoButton.SetEnabled(canUndo);
        }

        private void OnCanRedoChanged(bool canRedo)
        {
            UnityEngine.Debug.Log("Redo: " + canRedo);
            _redoButton.SetEnabled(canRedo);
        }

        private void Undo()
        {
            _voxelEditor.Undo();
        }

        private void Redo()
        {
            _voxelEditor.Redo();
        }

        private void OnChangeTool(vxTool tool)
        {
            _toolToolbar.SelectTool((int)tool, false);
        }

        private void OnChangeAction(vxAction action)
        {
            _actionToolbar.SelectTool((int)action, false);
        }

        private void OnActionSelected(int toolIndex)
        {
            VoxelEditor.Action = (VoxelTools.vxAction)toolIndex;
        }

        private void OnToolSelected(int toolIndex)
        {
            VoxelEditor.Tool = (VoxelTools.vxTool)toolIndex;
        }

        protected override void OnStopEditVoxelObject()
        {
        }
    }
}