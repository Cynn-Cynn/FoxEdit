using System;
using System.Collections;
using System.Collections.Generic;
using FoxEdit.EditorUtils;
using FoxEdit.VoxelTools;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace FoxEdit
{
    public static class FoxEditManager
    {
        public static event Action<VoxelObject, VoxelRenderer> OnStartEditVoxelObject;
        public static event Action OnStopEditVoxelObject;
        internal static VoxelEditor VoxelEditor { get; private set; }


        private static VoxelStage _voxelStage = null;
        private static VoxelRenderer _voxelRenderer = null;
        private static VoxelObject _voxelObject = null;



        internal static VoxelEditor StartEditVoxelObject(VoxelRenderer voxelRenderer) => StartEditVoxelObject(voxelRenderer.VoxelObject, voxelRenderer);

        internal static VoxelEditor StartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer = null)
        {
            EnsureFoxEditWindowIsOpen();
            if (voxelObject == null)
            {
                Debug.LogError("Cannot edit null voxelObject");
                return null;
            }
            if (voxelRenderer == null)
            {
                _voxelStage = VoxelStageUtility.OpenVoxelStage(voxelObject);
                FoxEditManager._voxelRenderer = _voxelStage.VoxelRenderer;
            }
            else
            {
                FoxEditManager._voxelRenderer = voxelRenderer;
            }

            FoxEditManager._voxelObject = voxelObject;
            Selection.activeGameObject = _voxelRenderer.gameObject;
            FocusGameObject(FoxEditManager._voxelRenderer.gameObject);
            VoxelEditor = new VoxelEditor(voxelRenderer);
            OnStartEditVoxelObject?.Invoke(voxelObject, voxelRenderer);
            return VoxelEditor;
        }

        private static void EnsureFoxEditWindowIsOpen()
        {
            NewFoxEditorWindow.Open();
        }

        public static void StopEditVoxelObject()
        {
            if (_voxelStage != null)
                StageUtility.GoToMainStage();
            _voxelObject = null;
            _voxelRenderer = null;
            _voxelStage = null;
            if (VoxelEditor != null)
                VoxelEditor.Dispose();
            if (ToolManager.activeToolType == typeof(VoxelEditorTool))
            {
                Tools.current = UnityEditor.Tool.None;
            }
            VoxelEditor = null;
            OnStopEditVoxelObject?.Invoke();
        }

        public static void Save(string path)
        {

        }

        private static void FocusGameObject(GameObject voxelGO)
        {
            EditorApplication.delayCall += () =>
            {
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.camera.clearFlags = CameraClearFlags.SolidColor;
                    sceneView.camera.backgroundColor = Color.red;
                    sceneView.FrameSelected();
                    sceneView.Repaint();
                }
            };
        }
    }
}