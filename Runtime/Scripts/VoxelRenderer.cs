using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[UnityEngine.RequireComponent(typeof(MeshFilter))]
[UnityEngine.RequireComponent(typeof(MeshRenderer))]
public class VoxelRenderer : MonoBehaviour
{
    [SerializeField] VoxelObject _voxelObject = null;

    [SerializeField] private ComputeShader _computeShader = null;
    [SerializeField] private Material _material = null;
    [SerializeField] private Material _staticMaterial = null;

    [SerializeField] [HideInInspector] private bool _staticRender = false;
    [SerializeField] [HideInInspector] private ComputeShader _computeShaderInstance = null;
    [SerializeField] [HideInInspector] private MeshRenderer _meshRenderer = null;
    [SerializeField] [HideInInspector] private Material _instantiatedMaterial = null;

    [SerializeField] private float _frameTime = 0.2f;

    public VoxelObject VoxelObject { get { return _voxelObject; } set { _voxelObject = value; Refresh(); } }

    private GraphicsBuffer _voxelPositionBuffer = null;

    private GraphicsBuffer _faceIndicesBuffer = null;
    private GraphicsBuffer _transformMatrixBuffer = null;
    private GraphicsBuffer _voxelIndicesBuffer = null;

    private GraphicsBuffer _colorIndicesBuffer = null;

    private Bounds _bounds;

    private int _kernel = 0;
    private uint _threadGroupSize = 0;

    private float _timer = 0.0f;
    private int _frameIndex = 0;

    RenderParams _renderParams;

#if UNITY_EDITOR
    [SerializeField][HideInInspector] VoxelSharedData _sharedData = null;
#endif

    void Start()
    {
        transform.hasChanged = false;
        if (Application.isPlaying)
            SetBuffers();
    }

    private void OnEnable()
    {
        if (_meshRenderer == null)
        {
            EditorUtility.SetDirty(gameObject);

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = _voxelObject.StaticMesh;

            _meshRenderer = GetComponent<MeshRenderer>();
            _instantiatedMaterial = Instantiate(_staticMaterial);
            _meshRenderer.material = _instantiatedMaterial;

        }
        if (!_staticRender)
            _meshRenderer.enabled = false;
    }

    private void OnDisable()
    {
        DisposeBuffers();
    }

    [Button]
    private void PaletteSwap()
    {
        if (_voxelObject.PaletteIndex == 0)
            SetPalette(1);
        else
            SetPalette(0);
    }

    [Button]
    private void RenderSwap()
    {
        _staticRender = !_staticRender;
        _meshRenderer.enabled = _staticRender;
        _timer = 0.0f;
    }


#if UNITY_EDITOR
    /// <summary>
    /// /!\ Do not call /!\
    /// </summary>
    [Button("Refresh")]
    public void Refresh()
    {
        if (!Application.isPlaying)
            DisposeBuffers();

        SetBuffers();
    }
#endif

    private void SetBuffers()
    {
        if (_voxelObject == null)
            return;

        _computeShaderInstance = Instantiate(_computeShader);
        _kernel = _computeShaderInstance.FindKernel("VoxelGeneration");

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

        //Transformation matrix
        _transformMatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxInstanceCount, sizeof(float) * 16);
        _computeShaderInstance.SetBuffer(_kernel, "_TransformMatrices", _transformMatrixBuffer);

        //Color
        _colorIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.ColorIndices.Length, sizeof(int));
        _colorIndicesBuffer.SetData(_voxelObject.ColorIndices);

        _bounds = _voxelObject.Bounds;
        _bounds.center += transform.position;

        _renderParams = new RenderParams(_material);
        _renderParams.worldBounds = _bounds;
        _renderParams.matProps = new MaterialPropertyBlock();

        _renderParams.matProps.SetBuffer("_TransformMatrices", _transformMatrixBuffer);
        _renderParams.matProps.SetBuffer("_ColorIndices", _colorIndicesBuffer);
        _renderParams.matProps.SetBuffer("_VoxelIndices", _voxelIndicesBuffer);
        _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        SetPalette(_voxelObject.PaletteIndex);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_sharedData != null || TryGetSharedData())
            {
                _renderParams.matProps.SetBuffer("_VertexPositions", _sharedData.FaceVertexBuffer);
                _computeShaderInstance.SetBuffer(_kernel, "_RotationMatrices", _sharedData.RotationMatricesBuffer);
            }
        }
        else if (VoxelSharedData.Instance != null)
        {
#endif
            _renderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.Instance.FaceVertexBuffer);
            _computeShaderInstance.SetBuffer(_kernel, "_RotationMatrices", VoxelSharedData.Instance.RotationMatricesBuffer);
#if UNITY_EDITOR
        }
#endif

        RunComputeShader();
    }

    public void SetPalette(int index)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_sharedData != null || TryGetSharedData())
            {
                GraphicsBuffer colorsBuffer = _sharedData.GetColorBuffer(index);
                if (colorsBuffer != null)
                {
                    _renderParams.matProps.SetBuffer("_Colors", colorsBuffer);
                    _voxelObject.PaletteIndex = index;
                }
            }
        }
        else if (VoxelSharedData.Instance != null)
        {
#endif
            GraphicsBuffer colorsBuffer = VoxelSharedData.Instance.GetColorBuffer(index);
            if (colorsBuffer != null)
            {
                _renderParams.matProps.SetBuffer("_Colors", colorsBuffer);
                _voxelObject.PaletteIndex = index;
            }
#if UNITY_EDITOR
        }
#endif
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
        if (_voxelObject == null)
            return;

        if (_staticRender)
            StaticRender();
        else
            AnimationRender();
    }

    private void AnimationRender()
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

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_sharedData != null || TryGetSharedData())
                Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, _sharedData.FaceTriangleBuffer, _sharedData.FaceTriangleCount, instanceCount: _voxelObject.InstanceCount[_frameIndex]);
        }
        else if (VoxelSharedData.Instance != null)
#endif
            Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, VoxelSharedData.Instance.FaceTriangleBuffer, VoxelSharedData.Instance.FaceTriangleCount, instanceCount: _voxelObject.InstanceCount[_frameIndex]);
    }

    private void StaticRender()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_sharedData != null || TryGetSharedData())
                _instantiatedMaterial.SetBuffer("_Colors", _sharedData.GetColorBuffer(_voxelObject.PaletteIndex));
        }
        else if (VoxelSharedData.Instance != null)
#endif
            _instantiatedMaterial.SetBuffer("_Colors", VoxelSharedData.Instance.GetColorBuffer(_voxelObject.PaletteIndex));
    }

#if UNITY_EDITOR
    private bool TryGetSharedData()
    {
        _sharedData = FindObjectOfType<VoxelSharedData>();
        if (_sharedData == null)
            return false;
        return true;
    }
#endif

    private void DisposeBuffers()
    {
        _voxelPositionBuffer?.Dispose();
        _voxelIndicesBuffer?.Dispose();

        _transformMatrixBuffer?.Dispose();

        _faceIndicesBuffer?.Dispose();
        _colorIndicesBuffer?.Dispose();
    }
}
