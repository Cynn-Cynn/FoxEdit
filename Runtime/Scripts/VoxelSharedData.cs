using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if HAS_VFX_GRAPH
using UnityEngine.VFX;
#endif
namespace FoxEdit
{
    public static class VoxelSharedData
    {
#if HAS_VFX_GRAPH
        [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
#endif
        private struct ColorData
        {
            public Vector4 Color;
            public float Emissive;
            public float Metallic;
            public float Smoothness;

            public ColorData(Vector4 color, float emissive, float metallic, float smoothness)
            {
                Color = color;
                Emissive = emissive;
                Metallic = metallic;
                Smoothness = smoothness;
            }
        }

        private static bool _isInitialized = false;

        private static FoxEditSettings _settings = null;

        #region Buffers

        private static GraphicsBuffer _faceVertexBuffer = null;
        private static GraphicsBuffer _faceTriangleBuffer = null;
        private static List<GraphicsBuffer> _colorsBuffers = null;

        internal static GraphicsBuffer FaceVertexBuffer { get { return _faceVertexBuffer; } }
        internal static GraphicsBuffer FaceTriangleBuffer { get { return _faceTriangleBuffer; } }

        #endregion Buffers

        #region Vertices

        private static Vector3[] _faceVertices =
        {
            new Vector3(0.05f, 0.05f, 0.05f),
            new Vector3(0.05f, 0.05f, -0.05f),
            new Vector3(-0.05f, 0.05f, -0.05f),
            new Vector3(-0.05f, 0.05f, 0.05f)
        };

        private static int[] _faceTriangles =
        {
            0, 2, 1,
            1, 2, 3
        };

        #endregion Vertices

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _settings = Resources.Load<FoxEditSettings>("FoxEditSettings");

            CreateBuffers();
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += Unload;
            EditorApplication.playModeStateChanged += StateChange;
#endif
        }

#if UNITY_EDITOR
        private static void StateChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Unload();
                Initialize();
            }
        }
#endif

        public static void Unload()
        {
            if (!_isInitialized)
                return;

            DisposeBuffers();
        }

        private static void Refresh()
        {
            VoxelRenderer[] renderers = Object.FindObjectsByType<VoxelRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].Setup();
            }
        }

        private static void CreateBuffers()
        {
            CreateFacesBuffers();
            CreateColorsBuffers();
            Refresh();
            _isInitialized = true;
        }

        private static void DisposeBuffers()
        {
            _faceTriangleBuffer?.Dispose();
            _faceVertexBuffer?.Dispose();

            DisposeColorsBuffers();

            _isInitialized = false;
        }

        #region Faces

        private static void CreateFacesBuffers()
        {
            _faceVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceVertices.Length, sizeof(float) * 3);
            _faceVertexBuffer.SetData(_faceVertices);

            _faceTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceTriangles.Length, sizeof(int));
            _faceTriangleBuffer.SetData(_faceTriangles);
        }

        #endregion Faces

        #region Colors

        internal static void CreateColorsBuffers()
        {
            DisposeColorsBuffers();

            _colorsBuffers = new List<GraphicsBuffer>();
            VoxelPalette[] palettes = _settings.Palettes;
            for (int i = 0; i < palettes.Length; i++)
            {
                if (palettes[i] == null)
                {
                    _colorsBuffers.Add(null);
                    continue;
                }

                ColorData[] colors = CreateColorBufferFromPalette(palettes[i]);
                GraphicsBuffer colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, colors.Length, sizeof(float) * 7);
                colorBuffer.SetData(colors);
                _colorsBuffers.Add(colorBuffer);
            }
        }

        internal static void RefreshColorBuffer(int index)
        {
            if (index >= _colorsBuffers.Count)
                return;

            _colorsBuffers[index]?.Dispose();

            VoxelPalette[] palettes = _settings.Palettes;
            ColorData[] colors = CreateColorBufferFromPalette(palettes[index]);
            GraphicsBuffer colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, colors.Length, sizeof(float) * 7);
            colorBuffer.SetData(colors);
            _colorsBuffers[index] = colorBuffer;
        }

        private static void DisposeColorsBuffers()
        {
            if (_colorsBuffers == null)
                return;

            for (int i = 0; i < _colorsBuffers.Count; i++)
            {
                _colorsBuffers[i]?.Dispose();
            }
        }

        private static ColorData[] CreateColorBufferFromPalette(VoxelPalette palette)
        {
            return palette.Colors.Select(color =>
            {
                return new ColorData(
                    new Vector4(color.Color.r, color.Color.g, color.Color.b, color.Color.a),
                    color.EmissiveIntensity, color.Metallic, color.Smoothness
                );
            }).ToArray();
        }

        internal static GraphicsBuffer GetColorBuffer(int index)
        {
            if (_colorsBuffers == null || index >= _colorsBuffers.Count)
                return null;
            return _colorsBuffers[index];
        }

        public static void SetupMaterialVoxelData(Material material, int paletteIndex)
        {
            GraphicsBuffer colorBuffer = VoxelSharedData.GetColorBuffer(paletteIndex);
            if (colorBuffer == null)
                return;

            material.SetBuffer("_Colors", colorBuffer);
            material.SetInt("_ColorCount", colorBuffer.count);
        }

        public static void SetupMeshRendererVoxelData(MeshRenderer meshRenderer, int paletteIndex)
        {
            GraphicsBuffer colorBuffer = VoxelSharedData.GetColorBuffer(paletteIndex);
            if (colorBuffer == null)
                return;

            foreach (Material material in meshRenderer.sharedMaterials)
            {
                material.SetBuffer("_Colors", colorBuffer);
                material.SetInt("_ColorCount", colorBuffer.count);
            }
        }

        public static void SetupParticleSystemVoxelData(ParticleSystem particleSystem, int paletteIndex)
        {
            GraphicsBuffer colorBuffer = VoxelSharedData.GetColorBuffer(paletteIndex);
            if (colorBuffer == null)
                return;
            foreach (ParticleSystemRenderer system in particleSystem.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                foreach (Material material in system.sharedMaterials)
                {
                    material.SetBuffer("_Colors", colorBuffer);
                    material.SetInt("_ColorCount", colorBuffer.count);
                }
            }
        }

#if HAS_VFX_GRAPH
        public static void SetupVisualEffectVoxelData(VisualEffect visualEffect, int paletteIndex)
        {
            GraphicsBuffer colorBuffer = VoxelSharedData.GetColorBuffer(paletteIndex);
            if (colorBuffer == null)
                return;

            visualEffect.SetGraphicsBuffer("_Colors", colorBuffer);
            visualEffect.SetInt("_ColorCount", colorBuffer.count);
        }
#endif

        #endregion Colors

        #region Palettes

        public static int GetPaletteCount()
        {
            return _settings.Palettes.Length;
        }

        public static VoxelPalette GetPalette(int index)
        {
            if (index >= _settings.Palettes.Length)
                return null;
            return _settings.Palettes[index];
        }

        public static string[] GetPaletteNames()
        {
            return _settings.Palettes.Select(palette => palette.name).ToArray();
        }

        #endregion Palettes
    }
}
