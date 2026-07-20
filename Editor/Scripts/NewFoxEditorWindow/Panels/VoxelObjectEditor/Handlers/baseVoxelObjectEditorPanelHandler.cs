
using System;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal abstract class baseVoxelObjectEditorPanelHandler
    {
        protected VoxelEditor _voxelEditor = null;
        protected VoxelRenderer _voxelRenderer = null;
        protected VoxelObject _voxelObject = null;
        protected VisualElement _root = null;

        public baseVoxelObjectEditorPanelHandler(VisualElement root)
        {
            _root = root;
        }

        public void StartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            _voxelObject = voxelObject;
            _voxelEditor = voxelEditor;
            _voxelRenderer = voxelRenderer;

            OnStartEditVoxelObject(voxelObject, voxelRenderer, voxelEditor);
        }

        public void StopEditVoxelObject()
        {
            OnStopEditVoxelObject();
            _voxelEditor = null;
            _voxelRenderer = null;
            _voxelObject = null;
        }

        protected abstract void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor);
        protected abstract void OnStopEditVoxelObject();
        public abstract void GetElements();
        public abstract void SetupFields();
        public abstract void RegisterCallbacks();
        public abstract void UnregisterCallbacks();

    }
}
