using System;
using System.Collections;
using System.Collections.Generic;
using FoxEdit.EditorUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace FoxEdit
{
    public static class FoxEditManager
    {
        public static event Action<VoxelObject, VoxelRenderer> OnStartEditVoxelObject;
        public static event Action OnStopEditVoxelObject;

        private static VoxelStage voxelStage = null;
        private static VoxelRenderer voxelRenderer = null;
        private static VoxelObject voxelObject = null;

        public static void StartEditVoxelObject(VoxelRenderer voxelRenderer) => StartEditVoxelObject(voxelRenderer.VoxelObject, voxelRenderer);

        public static void StartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer = null)
        {
            EnsureFoxEditWindowIsOpen();
            if (voxelObject == null)
            {
                Debug.LogError("Cannot edit null voxelObject");
                return;
            }
            if (voxelRenderer == null)
            {
                voxelStage = VoxelStageUtility.OpenVoxelStage(voxelObject);
                FoxEditManager.voxelRenderer = voxelStage.VoxelRenderer;
            }
            else
            {
                FoxEditManager.voxelRenderer = voxelRenderer;
            }

            FoxEditManager.voxelObject = voxelObject;
            FocusGameObject(FoxEditManager.voxelRenderer.gameObject);
            OnStartEditVoxelObject?.Invoke(voxelObject, voxelRenderer);
        }

        private static void EnsureFoxEditWindowIsOpen()
        {
            NewFoxEditorWindow.Open();
        }

        public static void StopEditVoxelObject()
        {
            if (voxelStage != null)
                StageUtility.GoToMainStage();
            voxelObject = null;
            voxelRenderer = null;
            voxelStage = null;
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
                    Selection.activeGameObject = voxelGO;
                    sceneView.FrameSelected();
                    sceneView.Repaint();
                }
            };
        }
    }
}