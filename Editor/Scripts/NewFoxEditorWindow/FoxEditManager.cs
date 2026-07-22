using System;
using System.IO;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoxEdit
{
    public static class FoxEditManager
    {
        internal static event Action<VoxelObject, VoxelRenderer, VoxelEditor> OnStartEditVoxelObject;
        public static event Action OnStopEditVoxelObject;
        internal static VoxelEditor VoxelEditor { get; private set; }


        private static VoxelRenderer _voxelRenderer = null;
        private static VoxelObject _voxelObject = null;



        internal static VoxelEditor StartEditVoxelObject(VoxelRenderer voxelRenderer) => StartEditVoxelObject(voxelRenderer.VoxelObject, voxelRenderer);
        internal static VoxelEditor StartEditNewVoxelObject() => StartEditVoxelObject(null, null);
        internal static VoxelEditor StartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer = null)
        {
            EnsureFoxEditWindowIsOpen();
            if (voxelRenderer == null)
            {
                FoxEditManager._voxelRenderer = VoxelEditorScene.Open(voxelObject);
            }
            else
            {
                FoxEditManager._voxelRenderer = voxelRenderer;
            }

            FoxEditManager._voxelObject = voxelObject;
            Selection.activeGameObject = _voxelRenderer.gameObject;
            VoxelEditor = new VoxelEditor(_voxelRenderer);
            OnStartEditVoxelObject?.Invoke(voxelObject, _voxelRenderer, VoxelEditor);
            FocusGameObject(_voxelRenderer.gameObject);
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
            if (VoxelEditorScene.IsOpen)
                VoxelEditorScene.Close();

            _voxelObject = null;
            _voxelRenderer = null;
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
                "Cancel",
                "Exit without saving");

            if (dialogue == 0)
            {
                int saveDialogue = EditorUtility.DisplayDialogComplex(
                    "Save Changes",
                    "How would you like to save your changes?",
                    "Save",
                    "Cancel",
                    "SaveAs");
                
                if (saveDialogue == 0)
                    return Save();
                else if (saveDialogue == 2)
                    return SaveAs();
            }
            else if (dialogue == 1)
            {
                return false;
            }
            else if (dialogue == 2)
            {
                return true;
            }

            return false;
        }

        private static bool Save(string savePath)
        {
            VoxelEditor.Save(savePath);
            return true;
        }

        public static bool Save()
        {
            string assetPath = AssetDatabase.GetAssetPath(_voxelObject);

            if (string.IsNullOrEmpty(assetPath))
                return SaveAs();
            Save(assetPath);
            return true;
        }

        public static bool SaveAs()
        {
            string defaultPath = FoxEditEditorSettings.Instance.DefaultSavePath.Value;
            if (!Directory.Exists(defaultPath))
                Directory.CreateDirectory(defaultPath);
            
            string defaultFileName = "New Voxel object";

            if (_voxelObject != null)
                defaultFileName = _voxelObject.name;

            string path = EditorUtility.SaveFilePanelInProject(
                string.Format("Save {0} voxel object", defaultFileName),
                defaultFileName,
                "asset",
                string.Format("Save {0} voxel object", defaultFileName),
                defaultPath);

            if (string.IsNullOrEmpty(path))
                return false;
            return Save(path);
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