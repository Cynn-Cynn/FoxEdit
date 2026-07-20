

using FoxEdit.VoxelTools;
using FoxEdit.WindowComponents;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class ToolsHandler : baseVoxelObjectEditorPanelHandler
    {
        private ToolbarElement _toolToolbar;
        private ToolbarElement _actionToolbar;

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
            _toolToolbar.OnToolSelected += OnToolSelected;
            _actionToolbar.OnToolSelected += OnActionSelected;
        }

        public override void UnregisterCallbacks()
        {
            VoxelEditor.OnChangeAction -= OnChangeAction;
            VoxelEditor.OnChangeTool -= OnChangeTool;
            _toolToolbar.OnToolSelected -= OnToolSelected;
            _actionToolbar.OnToolSelected -= OnActionSelected;
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