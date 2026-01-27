using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;

namespace FoxEdit
{
    internal static class ThumbnailsTaker
    {
        private const int THUMBNAIL_SIZE = 256;
        private const float isoDistance = 1f;
        private const float camDistanceMultiplier = 1.5f;

        private static Scene? _previewScene = null;
        private static Camera _camera;

        public static List<Texture2D> GetThumbnails(List<GameObject> gameObjects)
        {
            List<Texture2D> thumbnails = new List<Texture2D>();

            foreach (GameObject gameObject in gameObjects)
                thumbnails.Add(GetThumbnail(gameObject));

            return thumbnails;
        }

        public static Texture2D GetThumbnail(GameObject gameObject)
        {
            if (!_previewScene.HasValue)
                _previewScene = CreatePreviewScene();
            GameObject gameobjectCopy = GameObject.Instantiate(gameObject);
            gameobjectCopy.transform.position = Vector3.zero;
            gameobjectCopy.SetActive(true);

            SceneManager.MoveGameObjectToScene(gameobjectCopy, _previewScene.Value);
            Bounds objectBounds = CalculateBounds(gameobjectCopy);
            SetCameraPosition(objectBounds);
            Texture2D texture = GetTexture();

            GameObject.DestroyImmediate(gameobjectCopy);

            return texture;
        }

        private static Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static Scene CreatePreviewScene()
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();

            _camera = CreateCamera(previewScene);
            SceneManager.MoveGameObjectToScene(_camera.gameObject, previewScene);
            _camera.scene = previewScene;

            CreateDirectionalLight(previewScene);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;


            return previewScene;
        }

        private static Light CreateDirectionalLight(Scene scene)
        {
            GameObject lightGO = new GameObject("PreviewLight");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = Color.white;

            SceneManager.MoveGameObjectToScene(lightGO, scene);
            return light;
        }

        private static Texture2D GetTexture()
        {
            // Create RenderTexture
            RenderTexture rt = new RenderTexture(THUMBNAIL_SIZE, THUMBNAIL_SIZE, 24, RenderTextureFormat.ARGB32);
            rt.Create();

            // Force camera render
            _camera.targetTexture = rt;
            _camera.Render();

            // Save previous RT
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            // Read pixels
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // Cleanup
            RenderTexture.active = previous;
            _camera.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);

            return tex;
        }

        private static void SetCameraPosition(Bounds bounds)
        {
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            _camera.orthographicSize = maxExtent * camDistanceMultiplier;

            Vector3 offset = new Vector3(-1, 1, -1).normalized * maxExtent * camDistanceMultiplier * 2f; // scale 2x to be safe
            _camera.transform.position = bounds.center + offset;

            _camera.transform.LookAt(bounds.center);
        }

        private static Camera CreateCamera(Scene scene)
        {
            GameObject cameraGO = new GameObject();
            Camera camera = cameraGO.AddComponent<Camera>();

            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.cullingMask = ~0;
            camera.orthographic = true;

            return camera;
        }
    }
}