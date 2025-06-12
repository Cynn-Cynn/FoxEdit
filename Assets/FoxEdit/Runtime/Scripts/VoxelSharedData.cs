using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VoxelSharedData
{
    private static bool _isInitialized = false;

    private static FoxEditSettings _settings = null;

    #region Buffers

    private static GraphicsBuffer _faceVertexBuffer = null;
    private static GraphicsBuffer _faceTriangleBuffer = null;
    private static GraphicsBuffer _rotationMatricesBuffer = null;
    private static List<GraphicsBuffer> _colorsBuffers = null;
    private static int _faceTriangleCount = 0;

    internal static GraphicsBuffer FaceVertexBuffer { get { return _faceVertexBuffer; } }
    internal static GraphicsBuffer FaceTriangleBuffer { get { return _faceTriangleBuffer; } }
    internal static GraphicsBuffer RotationMatricesBuffer { get { return _rotationMatricesBuffer; } }
    internal static int FaceTriangleCount { get { return _faceTriangleCount; } }

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
            0, 1, 2,
            0, 2, 3
        };

    private static Matrix4x4[] _rotationMatrices = null;

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
        VoxelRenderer[] renderers = GameObject.FindObjectsOfType<VoxelRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].Refresh();
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

        _rotationMatricesBuffer?.Dispose();

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
        _faceTriangleCount = _faceTriangleBuffer.count;

        SetRotationMatrices();
        _rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
        _rotationMatricesBuffer.SetData(_rotationMatrices);
    }

    private static void SetRotationMatrices()
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

    private static Matrix4x4 GetRotationMatrixX(float angle)
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

    private static Matrix4x4 GetRotationMatrixZ(float angle)
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
