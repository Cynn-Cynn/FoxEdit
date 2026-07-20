
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class SaveHandler : baseVoxelObjectEditorPanelHandler
    {
        private Button _saveAsButton;
        private Button _saveButton;

        public SaveHandler(VisualElement root) : base(root)
        {
        }

        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
        }

        public override void GetElements()
        {
            _saveAsButton = _root.Q<Button>("save-as-button");
            _saveButton = _root.Q<Button>("save-button");
        }

        public override void RegisterCallbacks()
        {
            _saveButton.clicked += Save;
            _saveAsButton.clicked += SaveAs;
        }

        public override void UnregisterCallbacks()
        {
            _saveButton.clicked -= Save;
            _saveAsButton.clicked -= SaveAs;
        }

        public override void SetupFields()
        {
        }

        private void Save()
        {
            FoxEditManager.Save();
        }

        private void SaveAs()
        {
            FoxEditManager.SaveAs();
        }

        protected override void OnStopEditVoxelObject()
        {
        }
    }
}
