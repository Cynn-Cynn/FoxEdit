using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using System;

namespace FoxEdit
{
    [ExecuteAlways]
    public class VoxelSharedData : MonoBehaviour
    {
        [SerializeField] private VoxelGlobalData _globalData = null;

        public static VoxelSharedData Instance { get; private set; } = null;


        #region Buffers

        private GraphicsBuffer _faceVertexBuffer = null;
        private GraphicsBuffer _faceTriangleBuffer = null;
        private GraphicsBuffer _rotationMatricesBuffer = null;
        private List<GraphicsBuffer> _colorsBuffers = null;
        private int _faceTriangleCount = 0;

        internal GraphicsBuffer FaceVertexBuffer { get { return _faceVertexBuffer; } }
        internal GraphicsBuffer FaceTriangleBuffer { get { return _faceTriangleBuffer; } }
        internal GraphicsBuffer RotationMatricesBuffer { get { return _rotationMatricesBuffer; } }
        internal int FaceTriangleCount { get { return _faceTriangleCount; } }

        #endregion Buffers

        #region Vertices

        private Vector3[] _faceVertices =
        {
            new Vector3(0.05f, 0.05f, 0.05f),
            new Vector3(0.05f, 0.05f, -0.05f),
            new Vector3(-0.05f, 0.05f, -0.05f),
            new Vector3(-0.05f, 0.05f, 0.05f)
        };

        private int[] _faceTriangles =
        {
            0, 1, 2,
            0, 2, 3
        };

        private Matrix4x4[] _rotationMatrices = null;

        #endregion Vertices

        private struct ColorData
        {
            private Vector4 Color;
            float Emissive;
            float Metallic;
            float Smoothness;

            public ColorData(Vector4 color, float emissive, float metallic, float smoothness)
            {
                Color = color;
                Emissive = emissive;
                Metallic = metallic;
                Smoothness = smoothness;
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                CreateBuffers();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
                Refresh();
        }
#endif

        private void OnDisable()
        {
            DisposeBuffers();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

#if UNITY_EDITOR
        private void Refresh()
        {
            CreateBuffers();
            VoxelRenderer[] renderers = FindObjectsOfType<VoxelRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].Refresh();
            }
        }
#endif

        private void CreateBuffers()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DisposeBuffers();
#endif
            CreateFacesBuffers();
            CreateColorsBuffers();
        }

        private void DisposeBuffers()
        {
            _faceTriangleBuffer?.Dispose();
            _faceVertexBuffer?.Dispose();

            _rotationMatricesBuffer?.Dispose();

            if (_colorsBuffers != null)
            {
                for (int i = 0; i < _colorsBuffers.Count; i++)
                {
                    if (_colorsBuffers[i] != null)
                        _colorsBuffers[i]?.Dispose();
                }
            }
        }

        #region Faces

        private void CreateFacesBuffers()
        {
            _faceVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceVertices.Length, sizeof(float) * 3);
            _faceVertexBuffer.SetData(_faceVertices);

            _faceTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceTriangles.Length, sizeof(int));
            _faceTriangleBuffer.SetData(_faceTriangles);
            _faceTriangleCount = _faceTriangleBuffer.count;

            SetRotationMatrices();
            _rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
            _rotationMatricesBuffer.SetData(_rotationMatrices);
        }

        private void SetRotationMatrices()
        {
            float halfPi = Mathf.PI / 2.0f;

            _rotationMatrices = new Matrix4x4[6];
            _rotationMatrices[0] = GetRotationMatrixX(0);
            _rotationMatrices[1] = GetRotationMatrixX(halfPi);
            _rotationMatrices[2] = GetRotationMatrixX(halfPi * 2);
            _rotationMatrices[3] = GetRotationMatrixX(halfPi * 3);
            _rotationMatrices[4] = GetRotationMatrixZ(-halfPi);
            _rotationMatrices[5] = GetRotationMatrixZ(halfPi);
        }

        private Matrix4x4 GetRotationMatrixX(float angle)
        {
            float c = Mathf.Cos(angle);
            float s = Mathf.Sin(angle);

            return new Matrix4x4
            (
                new Vector4(1, 0, 0, 0),
                new Vector4(0, c, -s, 0),
                new Vector4(0, s, c, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        private Matrix4x4 GetRotationMatrixZ(float angle)
        {
            float c = Mathf.Cos(angle);
            float s = Mathf.Sin(angle);

            return new Matrix4x4
            (
                new Vector4(c, -s, 0, 0),
                new Vector4(s, c, 0, 0),
                new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1)
            );
        }

        #endregion Faces

        #region Colors

        internal void CreateColorsBuffers()
        {
            _colorsBuffers = new List<GraphicsBuffer>();
            for (int i = 0; i < _globalData.Palettes.Length; i++)
            {
                ColorData[] colors = CreateColorBufferFromPalette(i);
                GraphicsBuffer colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, colors.Length, sizeof(float) * 7);
                colorBuffer.SetData(colors);
                _colorsBuffers.Add(colorBuffer);
            }
        }

        private ColorData[] CreateColorBufferFromPalette(int index)
        {
            return _globalData.Palettes[index].Colors.Select(color =>
            {
                return new ColorData(
                    new Vector4(color.Color.r, color.Color.g, color.Color.b, color.Color.a),
                    color.EmissiveIntensity, color.Metallic, color.Smoothness
                );
            }).ToArray();
        }

        internal GraphicsBuffer GetColorBuffer(int index)
        {
            if (_colorsBuffers == null || index >= _colorsBuffers.Count)
                return null;
            return _colorsBuffers[index];
        }

        #endregion Colors

        #region Palettes

        public int GetPaletteCount()
        {
            return _globalData.Palettes.Length;
        }

        public VoxelPalette GetPalette(int index)
        {
            if (index >= _globalData.Palettes.Length)
                return null;
            return _globalData.Palettes[index];
        }

        public string[] GetPaletteNames()
        {
            return _globalData.Palettes.Select(palette => palette.name).ToArray();
        }

        #endregion Palettes
    }
}
