using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        internal static event Action<VoxelObject, VoxelRenderer, VoxelEditor> OnStartEditVoxelObject;
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
            VoxelEditor = new VoxelEditor(_voxelRenderer);
            OnStartEditVoxelObject?.Invoke(voxelObject, _voxelRenderer, VoxelEditor);
            return VoxelEditor;
        }

        private static void EnsureFoxEditWindowIsOpen()
        {
            NewFoxEditorWindow.Open();
        }

        public static void StopEditVoxelObject()
        {
            if (VoxelEditor == null)
                return;
            if (VoxelEditor.IsDirty)
            {
                if (!DisplaySaveDialog())
                    return;
            }
            _voxelObject = null;
            _voxelRenderer = null;
            if (_voxelStage != null)
                StageUtility.GoToMainStage();
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

        public static bool DisplaySaveDialog()
        {
            int dialogue = EditorUtility.DisplayDialogComplex(
                "Hold on, little voxel!",
                "You have unsaved changes. Do you want to save your creation before leaving?",
                "Save",
                "Save as...",
                "Cancel");

            switch (dialogue)
            {
                case 0:
                    return Save();
                case 1:
                    return SaveAs();
            }

            return false;
        }

        private static bool Save(string savePath)
        {
            Debug.LogFormat("Save at {0}", savePath);
            return true;
        }

        private static bool Save()
        {
            string assetPath = AssetDatabase.GetAssetPath(_voxelObject);

            if (string.IsNullOrEmpty(assetPath))
                return SaveAs();
            Save(ProjectRelativeToAbsolute(assetPath));
            return true;
        }

        private static bool SaveAs()
        {
            string defaultPath = FoxEditEditorSettings.Instance.DefaultSavePath.Value;
            if (!Directory.Exists(defaultPath))
                Directory.CreateDirectory(defaultPath);

            string path = EditorUtility.SaveFilePanelInProject(
                string.Format("Save {0} voxel object", _voxelObject.name),
                _voxelObject.name,
                "asset",
                string.Format("Save {0} voxel object", _voxelObject.name),
                defaultPath);

            if (path == null)
                return false;
            return Save(path);
        }

        private static string ProjectRelativeToAbsolute(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                return null;

            if (!projectPath.StartsWith("Assets/") && !projectPath.StartsWith("Assets\\"))
            {
                Debug.LogError("Path must be relative to project: " + projectPath);
                return null;
            }

            string relativeToAssets = projectPath.Substring("Assets".Length); // remove "Assets"
            string absolutePath = Path.Combine(Application.dataPath, relativeToAssets);

            return Path.Join(Application.dataPath, absolutePath);
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