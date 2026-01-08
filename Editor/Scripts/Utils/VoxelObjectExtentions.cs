using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FoxEdit
{
    public static class VoxelObjectExtentions
    {
        private const int THUMBNAIL_SIZE = 256;
        private const float isoDistance = 1f;
        private const float camDistanceMultiplier = 1.5f;

        public static async Task<Texture2D> GetPreviewIcon(this VoxelObject obj)
        {
            
            Scene previewScene = EditorSceneManager.NewPreviewScene();

            GameObject voxelRendererGO = new GameObject("Preview voxel");
            VoxelRenderer voxelRenderer = voxelRendererGO.AddComponent<VoxelRenderer>();
            voxelRenderer.SetVoxelObject(obj);
            VoxelRendererEditor.SetupVoxelRenderer(voxelRenderer);
            voxelRenderer.RenderSwap();

            await Task.Delay(1000);


            GameObject cameraGO = new GameObject();
            Camera camera = cameraGO.AddComponent<Camera>();
            camera.scene = previewScene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.orthographic = true;
            camera.orthographicSize = 1;

            SceneManager.MoveGameObjectToScene(voxelRendererGO, previewScene);
            SceneManager.MoveGameObjectToScene(camera.gameObject, previewScene);

            Bounds voxelObjectBounds = CalculateBounds(voxelRendererGO);

            float distance = voxelObjectBounds.extents.magnitude * camDistanceMultiplier;
            camera.transform.position = voxelObjectBounds.center + Vector3.back * distance;
            camera.transform.position += new Vector3(-1, 1, 0) * isoDistance;
            camera.transform.LookAt(voxelObjectBounds.center);

            var rt = new RenderTexture(THUMBNAIL_SIZE, THUMBNAIL_SIZE, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var tex = new Texture2D(256, 256, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            tex.Apply();

            RenderTexture.active = null;

            GameObject.DestroyImmediate(camera);
            GameObject.DestroyImmediate(voxelRendererGO);

            EditorSceneManager.ClosePreviewScene(previewScene);

            return tex;
        }

        static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }
}