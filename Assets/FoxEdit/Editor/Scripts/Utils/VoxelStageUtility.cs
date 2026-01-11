using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.SceneManagement;

namespace FoxEdit.EditorUtils
{
    public class VoxelStage : PreviewSceneStage
    {
        private VoxelObject voxelObject;
        public VoxelRenderer VoxelRenderer {get; private set;}


        public void SetVoxelObject(VoxelObject voxelObject)
        {
            this.voxelObject = voxelObject;
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Voxel Editor");
        }

        protected override bool OnOpenStage()
        {
            bool success = base.OnOpenStage();

            Light light = new GameObject("Light").AddComponent<Light>();
            light.type = UnityEngine.LightType.Directional;
            light.gameObject.hideFlags = HideFlags.NotEditable;
            light.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            light.transform.position = new Vector3(0f, 10f, 0f);

            GameObject voxelGO = new GameObject(voxelObject.name, typeof(MeshFilter), typeof(MeshRenderer));
            VoxelRenderer voxelRenderer = voxelGO.AddComponent<VoxelRenderer>();
            voxelRenderer.SetVoxelObject(voxelObject);
            voxelRenderer.Setup();
            voxelRenderer.RenderSwap();


            SceneManager.MoveGameObjectToScene(light.gameObject, scene);
            SceneManager.MoveGameObjectToScene(voxelGO, scene);

            SceneView sceneView = SceneView.lastActiveSceneView;

            sceneView.camera.clearFlags = CameraClearFlags.SolidColor;
            sceneView.camera.backgroundColor = Color.red;

            sceneView.Repaint();
            this.VoxelRenderer = voxelRenderer;

            return success;
        }

        protected override void OnCloseStage()
        {
            FoxEditManager.StopEditVoxelObject();
            base.OnCloseStage();
        }
    }

    public static class VoxelStageUtility
    {
        public const string TMP_PREFAB_PATH = "Assets/__TempPrefab.prefab";

        public static VoxelStage OpenVoxelStage(VoxelObject target)
        {
            VoxelStage voxelStage = ScriptableObject.CreateInstance<VoxelStage>();

            voxelStage.SetVoxelObject(target);
            StageUtility.GoToStage(voxelStage, true);

            return voxelStage;
        }

        public static void CloseVoxelStage()
        {
            if (StageUtility.GetCurrentStage() is VoxelStage)
                StageUtility.GetMainStage();
        }
    }
}