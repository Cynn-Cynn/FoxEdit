using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.SceneManagement;

namespace FoxEdit.EditorUtils
{
    public class VoxelStage : PreviewSceneStage
    {
        private FoxEditEditorSettings settings => FoxEditEditorSettings.Instance;
        private VoxelObject voxelObject;
        public VoxelRenderer VoxelRenderer { get; private set; }


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

            Light bottomLight = CreateLight("Bottom Light", new Vector3(-90, 0, 0));
            Light sideLight = CreateLight("Side light", new Vector3(50, -30, 0));
            Light otherSideLight = CreateLight("Side light light", new Vector3(50, -120, 0));

            string objectName = null;

            if (voxelObject == null)
                objectName = "New voxel object";
            else
                objectName = voxelObject.name;
            

            GameObject voxelGO = new GameObject(string.Format("{0} (Preview)", objectName), typeof(MeshFilter), typeof(MeshRenderer));
            VoxelRenderer = voxelGO.AddComponent<VoxelRenderer>();
            VoxelRenderer.SetVoxelObject(voxelObject);
            VoxelRenderer.RenderSwap();

            GameObject backgroundGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            MeshRenderer backgroundMeshRenderer = backgroundGO.GetComponent<MeshRenderer>();
            Material material = Instantiate(settings.VoxelStageBackgroundMaterial.Asset);
            material.color = settings.StageBackgroundColor.Value;
            backgroundMeshRenderer.material = material;
            backgroundGO.transform.localScale = Vector3.one * settings.StageBackgroundSphereSize.Value;
            DestroyImmediate(backgroundGO.GetComponent<Collider>());
            backgroundGO.hideFlags = HideFlags.NotEditable;


            SceneManager.MoveGameObjectToScene(bottomLight.gameObject, scene);
            SceneManager.MoveGameObjectToScene(sideLight.gameObject, scene);
            SceneManager.MoveGameObjectToScene(otherSideLight.gameObject, scene);
            SceneManager.MoveGameObjectToScene(voxelGO, scene);
            SceneManager.MoveGameObjectToScene(backgroundGO, scene);

            return true;
        }

        private Light CreateLight(string lightName, Vector3 rotation)
        {
            Light light = new GameObject(lightName).AddComponent<Light>();
            light.type = UnityEngine.LightType.Directional;
            light.color = settings.StageLightColor.Value;
            light.intensity = settings.StageLightIntensity.Value;
            light.bounceIntensity = settings.StageLightIndirectMultiplier.Value;
            light.transform.position = new Vector3(0f, 3f, 0f);
            light.color = settings.StageLightColor.Value;
            light.gameObject.hideFlags = HideFlags.NotEditable;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(rotation);

            return light;
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