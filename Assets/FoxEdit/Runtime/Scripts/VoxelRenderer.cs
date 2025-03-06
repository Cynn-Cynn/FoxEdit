using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class VoxelRenderer : MonoBehaviour
{
    [SerializeField] private ComputeShader _computeShader = null;
    [SerializeField] [HideInInspector] private ComputeShader _computeShaderInstance = null;
    [SerializeField] private Material _material = null;

    [SerializeField] private float _frameTime = 0.2f;
    [SerializeField] private VoxelEditor _voxelEditor = null;

    VoxelObject _voxelObject = null;

    private GraphicsBuffer _voxelPositionBuffer = null;

    private GraphicsBuffer _faceIndicesBuffer = null;
    private GraphicsBuffer _transformMatrixBuffer = null;
    private GraphicsBuffer _voxelIndicesBuffer = null;

    private GraphicsBuffer _colorIndicesBuffer = null;
    private GraphicsBuffer _colorsBuffer = null;

    private GraphicsBuffer _faceVertexBuffer = null;
    private GraphicsBuffer _faceTriangleBuffer = null;

    private GraphicsBuffer _rotationMatricesBuffer = null;

    private Bounds _bounds;

    private int _kernel = 0;
    private uint _threadGroupSize = 0;

    private float _timer = 0.0f;
    [SerializeField] private int _frameIndex = 0;

    RenderParams _renderParams;

    private Matrix4x4[] _rotationMatrices = null;

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

    void Start()
    {
        transform.hasChanged = false;
    }

    [Button("Refresh")]
    private void SetBuffers()
    {
        DisposeBuffer();

        _computeShaderInstance = Instantiate(_computeShader);
        _kernel = _computeShaderInstance.FindKernel("VoxelGeneration");
        _voxelObject = _voxelEditor.ConstructVoxelObject();

        //Voxel
        _voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelPositions.Length, sizeof(float) * 3);
        _voxelPositionBuffer.SetData(_voxelObject.VoxelPositions);
        _computeShaderInstance.SetBuffer(_kernel, "_VoxelPositions", _voxelPositionBuffer);

        _faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.FaceIndices.Length, sizeof(int));
        _faceIndicesBuffer.SetData(_voxelObject.FaceIndices);
        _computeShaderInstance.SetBuffer(_kernel, "_FaceIndices", _faceIndicesBuffer);

        _voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelIndices.Length, sizeof(int));
        _voxelIndicesBuffer.SetData(_voxelObject.VoxelIndices);
        _computeShaderInstance.SetBuffer(_kernel, "_VoxelIndices", _voxelIndicesBuffer);

        //Face
        _faceVertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceVertices.Length, sizeof(float) * 3);
        _faceVertexBuffer.SetData(_faceVertices);

        _faceTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _faceTriangles.Length, sizeof(int));
        _faceTriangleBuffer.SetData(_faceTriangles);

        //Transformation matrix
        _transformMatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxInstanceCount, sizeof(float) * 16);
        _computeShaderInstance.SetBuffer(_kernel, "_TransformMatrices", _transformMatrixBuffer);

        //Color
        _colorIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.ColorIndices.Length, sizeof(int));
        _colorIndicesBuffer.SetData(_voxelObject.ColorIndices);

        _colorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Colors.Length, sizeof(float) * 7);
        _colorsBuffer.SetData(_voxelObject.Colors);

        //Rotation matrices
        SetRotationMatrices();
        _rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
        _rotationMatricesBuffer.SetData(_rotationMatrices);
        _computeShaderInstance.SetBuffer(_kernel, "_RotationMatrices", _rotationMatricesBuffer);

        _bounds = _voxelObject.Bounds;
        _bounds.center += transform.position;

        _renderParams = new RenderParams(_material);
        _renderParams.worldBounds = _bounds;
        _renderParams.matProps = new MaterialPropertyBlock();
        _renderParams.matProps.SetBuffer("_VertexPositions", _faceVertexBuffer);
        _renderParams.matProps.SetBuffer("_Colors", _colorsBuffer);
        _renderParams.matProps.SetBuffer("_TransformMatrices", _transformMatrixBuffer);
        _renderParams.matProps.SetBuffer("_ColorIndices", _colorIndicesBuffer);
        _renderParams.matProps.SetBuffer("_VoxelIndices", _voxelIndicesBuffer);
        _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        RunComputeShader();
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

    private void RunComputeShader()
    {
        int instanceStartIndex = _voxelObject.InstanceStartIndices[_frameIndex];
        int instanceCount = _voxelObject.InstanceCount[_frameIndex];
        _computeShaderInstance.SetInt("_InstanceStartIndex", instanceStartIndex);
        _computeShaderInstance.SetInt("_InstanceCount", instanceCount);
        _computeShaderInstance.SetInt("_FrameIndex", _frameIndex);
        _computeShaderInstance.SetMatrix("_VoxelToWorldMatrix", transform.localToWorldMatrix);

        _computeShaderInstance.GetKernelThreadGroupSizes(_kernel, out _threadGroupSize, out _, out _);
        int threadGroups = Mathf.CeilToInt((float)instanceCount / _threadGroupSize);
        _computeShaderInstance.Dispatch(_kernel, threadGroups, 1, 1);

        _renderParams.matProps.SetInteger("_InstanceStartIndex", instanceStartIndex);
        _renderParams.matProps.SetVector("_Scale", transform.localScale);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _frameTime)
        {
            _frameIndex = (_frameIndex + 1) % _voxelObject.FrameCount;
            _timer -= _frameTime;
            RunComputeShader();
        }

        if (transform.hasChanged)
        {
            RunComputeShader();
            transform.hasChanged = false;
        }

        Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, _faceTriangleBuffer, _faceTriangleBuffer.count, instanceCount: _voxelObject.InstanceCount[_frameIndex]);
    }

    private void OnEnable()
    {
        SetBuffers();
    }

    private void OnDisable()
    {
        DisposeBuffer();
    }

    private void DisposeBuffer()
    {
        if (_voxelPositionBuffer == null)
            return;

        _voxelPositionBuffer.Dispose();
        _voxelIndicesBuffer.Dispose();

        _transformMatrixBuffer.Dispose();

        _faceIndicesBuffer.Dispose();
        _colorIndicesBuffer.Dispose();
        _colorsBuffer.Dispose();

        _faceTriangleBuffer.Dispose();
        _faceVertexBuffer.Dispose();

        _rotationMatricesBuffer.Dispose();
    }
}
