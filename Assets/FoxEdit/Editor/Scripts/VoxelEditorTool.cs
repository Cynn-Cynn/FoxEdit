using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace FoxEdit
{
    [EditorTool("Voxel Editor", typeof(VoxelRenderer))]
    internal class VoxelEditorTool : EditorTool
    {
        private VoxelEditor _voxelEditor;
        private GUIContent _icon;
        private bool _isMouseOnVoxel;
        private Vector3 _cubePosition;
        private Vector3 _worldNormal;
        private bool _repaint = false;
        private bool _previousShowGizmos = false;

        #region Initialize
        private void OnEnable()
        {
            _icon = new GUIContent(EditorGUIUtility.Load("d_Prefab On Icon") as Texture2D, "Voxel Editor Tool");
            FoxEditManager.OnStartEditVoxelObject += OnStartEditVoxelObject;

        }

        private void OnStartEditVoxelObject(VoxelObject obj, VoxelRenderer renderer, VoxelEditor voxelEditor)
        {
            if (ToolManager.activeToolType != typeof(VoxelEditorTool))
                ToolManager.SetActiveTool<VoxelEditorTool>();
        }

        public override void OnActivated()
        {
            if (!TryGetVoxelEditor(out _voxelEditor))
            {
                Debug.Log("Cannot open Voxel Edit Tool on the selected Gameobject");
                Tools.current = Tool.None;
            }
            GizmoUtility.SetGizmoEnabled(typeof(BoxCollider), false);
        }

        void OnDisable()
        {
            GizmoUtility.SetGizmoEnabled(typeof(BoxCollider), true);
        }
        #endregion

        public override void OnToolGUI(EditorWindow window)
        {
            if (_voxelEditor == null)
                return;

            Event e = Event.current;
            if (e.type == EventType.MouseMove)
                OnMouseMove(e);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                OnLeftClick(e);
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 3)
            {

            }

            Handles.color = Color.green;
            Vector3 offsetedCubePosition = _cubePosition + new Vector3(0f, 0.05f, 0f);
            Handles.DrawWireCube(offsetedCubePosition, Vector3.one * 0.1f);
            Handles.DrawLine(offsetedCubePosition, offsetedCubePosition + _worldNormal * 0.1f);
            if (_repaint)
            {
                window.Repaint();
                _repaint = false;
            }
        }

        private void OnMouseMove(Event e)
        {
            if (_voxelEditor.TryGetCubePosition(out Vector3 newCubePosition, out Vector3 newWorldNormal, HandleUtility.GUIPointToWorldRay(e.mousePosition)))
            {
                if (newCubePosition != _cubePosition)
                    _repaint = true;
                if (newWorldNormal != _worldNormal)
                    _repaint = true;
                _cubePosition = newCubePosition;
                _worldNormal = newWorldNormal;
                _isMouseOnVoxel = true;
            }
            else
            {
                _isMouseOnVoxel = false;
            }

        }

        private void OnLeftClick(Event e)
        {
            if (!_isMouseOnVoxel) return;
            OnMouseMove(e);
            _voxelEditor.UseTool(_cubePosition, _worldNormal);
        }

        private void OnMiddleClick()
        {
            //todo set color from voxel under mouse
        }

        private bool TryGetVoxelEditor(out VoxelEditor voxelEditor)
        {
            voxelEditor = null;
            if (FoxEditManager.VoxelEditor == null)
            {
                if (TryGetSelectedVoxelRenderer(out VoxelRenderer voxelRenderer))
                {
                    _voxelEditor = FoxEditManager.StartEditVoxelObject(voxelRenderer);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            voxelEditor = FoxEditManager.VoxelEditor;
            return voxelEditor != null;
        }

        private bool TryGetSelectedVoxelRenderer(out VoxelRenderer voxelRenderer)
        {
            GameObject selectedGameobject = Selection.activeGameObject;
            voxelRenderer = null;

            if (selectedGameobject == null)
                return false;

            voxelRenderer = selectedGameobject.GetComponent<VoxelRenderer>();
            return voxelRenderer != null;
        }

        public override GUIContent toolbarIcon => _icon;
    }
}