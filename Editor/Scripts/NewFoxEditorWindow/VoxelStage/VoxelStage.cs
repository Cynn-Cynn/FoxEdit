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
            base.OnOpenStage();

            Light light = new GameObject("Light").AddComponent<Light>();
            light.type = UnityEngine.LightType.Directional;
            //light.gameObject.hideFlags = HideFlags.NotEditable;
            light.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            light.transform.position = new Vector3(0f, 10f, 0f);

            GameObject voxelGO = new GameObject(string.Format("{0} (Preview)", voxelObject.name), typeof(MeshFilter), typeof(MeshRenderer));
            VoxelRenderer = voxelGO.AddComponent<VoxelRenderer>();
            VoxelRenderer.SetVoxelObject(voxelObject);
            VoxelRenderer.Setup();
            VoxelRenderer.RenderSwap();

            GameObject backgroundGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            MeshRenderer backgroundMeshRenderer = backgroundGO.GetComponent<MeshRenderer>();
            Material material = Instantiate(FoxEditEditorSettings.Instance.VoxelStageBackgroundMaterial.Asset);
            material.color = FoxEditEditorSettings.Instance.BackgroundColor.Value;
            backgroundMeshRenderer.material = material;
            backgroundGO.transform.localScale = Vector3.one * FoxEditEditorSettings.Instance.BackgroundSphereSize.Value;



            SceneManager.MoveGameObjectToScene(light.gameObject, scene);
            SceneManager.MoveGameObjectToScene(voxelGO, scene);
            SceneManager.MoveGameObjectToScene(backgroundGO, scene);

            return true;
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