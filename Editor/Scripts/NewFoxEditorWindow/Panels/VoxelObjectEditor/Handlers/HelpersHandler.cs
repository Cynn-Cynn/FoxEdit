using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class HelpersHandler : baseVoxelObjectEditorPanelHandler
    {
        private const float GIZMO_DRAW_TIME = 0.01f;
        private Toggle showGridToggle;
        private ColorField gridColor;
        private double lastGizmoDrawTime;

        public HelpersHandler(VisualElement root) : base(root)
        {
        }

        public override void GetElements()
        {
            showGridToggle = _root.Q<Toggle>("show-grid");
            gridColor = _root.Q<ColorField>("grid-color");
        }

        public override void SetupFields()
        {
        }

        public override void RegisterCallbacks()
        {
        }


        public override void UnregisterCallbacks()
        {
        }


        private void OnCurrentFrameChanged(VoxelEditorFrame oldFrame, VoxelEditorFrame newFrame)
        {
        }


        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            voxelEditor.OnCurrentFrameChanged += OnCurrentFrameChanged;
            EditorApplication.update += Update;
        }


        protected override void OnStopEditVoxelObject()
        {
            EditorApplication.update -= Update;
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup - lastGizmoDrawTime < GIZMO_DRAW_TIME)
                return;
            lastGizmoDrawTime = EditorApplication.timeSinceStartup;

            if (_voxelEditor != null && _voxelEditor.CurrentFrame != null && showGridToggle.value)
                _voxelEditor.CurrentFrame.ShowGrid(color: gridColor.value, duration: GIZMO_DRAW_TIME + 0.01f);
        }
    }
}