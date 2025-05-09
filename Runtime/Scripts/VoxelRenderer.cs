using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelRenderer : MonoBehaviour
    {
        //User editable
        [SerializeField] private VoxelObject _voxelObject = null;
        [SerializeField] private int _paletteIndexOverride = 0;
        [SerializeField] private bool _staticRender = false;
        [SerializeField] private float _frameDuration = 0.2f;

        //Setup
        [SerializeField] private bool _isSetup = false;
        [SerializeField] private MeshFilter _meshFilter = null;
        [SerializeField] private MeshRenderer _meshRenderer = null;
        [SerializeField] private ComputeShader _computeShader = null;
        [SerializeField] private Material _material = null;
        [SerializeField] private Material _staticMaterial = null;

        public VoxelObject VoxelObject { get { return _voxelObject; } set { SetVoxelObject(value); } }

        private GraphicsBuffer _voxelPositionBuffer = null;
        private GraphicsBuffer _faceIndicesBuffer = null;
        private GraphicsBuffer _transformMatrixBuffer = null;
        private GraphicsBuffer _voxelIndicesBuffer = null;
        private GraphicsBuffer _colorIndicesBuffer = null;

        private Bounds _bounds;
        private Bounds _baseBounds;

        private int _kernel = 0;
        private uint _threadGroupSize = 0;

        private float _timer = 0.0f;
        private int _frameIndex = 0;

        RenderParams _renderParams;

#if UNITY_EDITOR
        [SerializeField] private VoxelSharedData _sharedData = null;
#endif

        private void Awake()
        {
            _isSetup = !_isSetup;
            _isSetup = !_isSetup;
        }

        void Start()
        {
            transform.hasChanged = true;
        }

        #region UserEditable

        private void SetVoxelObject(VoxelObject voxelObject)
        {
            if (voxelObject == _voxelObject)
                return;

            _voxelObject = voxelObject;
            _frameIndex = 0;

            if (_voxelObject.StaticMesh != null)
            {
                _meshFilter.mesh = voxelObject.StaticMesh;
                Refresh();
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        public void RenderSwap()
        {
            _staticRender = !_staticRender;
            _meshRenderer.enabled = _staticRender;
            _timer = 0.0f;
        }

        public void SetPalette(int index)
        {
            VoxelSharedData sharedData = GetSharedData();
            if (sharedData == null)
                return;

            GraphicsBuffer colorsBuffer = sharedData.GetColorBuffer(index);
            if (colorsBuffer != null)
            {
                _renderParams.matProps.SetBuffer("_Colors", colorsBuffer);
                _paletteIndexOverride = index;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        #endregion UserEditable

        #region Buffers

        private void OnEnable()
        {
            SetBuffers();
        }

        private void OnDisable()
        {
            DisposeBuffers();
        }

        internal void Refresh()
        {
            SetBuffers();
            _meshFilter.mesh = _voxelObject?.StaticMesh;
        }

        internal void RefreshColors()
        {
            SetPalette(_voxelObject.PaletteIndex);
            RunComputeShader();
        }

        private void SetBuffers()
        {
            if (_voxelObject == null)
                return;

            _kernel = _computeShader.FindKernel("VoxelGeneration");

            SetVoxelBuffers();
            SetColorBuffer();
            SetMatrixBuffer();
            SetRenderParams();

            _bounds = _voxelObject.Bounds;
            _baseBounds = _voxelObject.Bounds;
            _bounds.center += transform.position;

            RunComputeShader();
        }

        private void SetVoxelBuffers()
        {
            if (_voxelPositionBuffer != null && _voxelPositionBuffer.count != _voxelObject.VoxelPositions.Length)
            {
                _voxelPositionBuffer.Dispose();
                _voxelPositionBuffer = null;
            }
            if (_voxelPositionBuffer == null)
                _voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelPositions.Length, sizeof(float) * 3);
            _voxelPositionBuffer.SetData(_voxelObject.VoxelPositions);
            _computeShader.SetBuffer(_kernel, "_VoxelPositions", _voxelPositionBuffer);

            if (_faceIndicesBuffer != null && _faceIndicesBuffer.count != _voxelObject.FaceIndices.Length)
            {
                _faceIndicesBuffer.Dispose();
                _faceIndicesBuffer = null;
            }
            if (_faceIndicesBuffer == null)
                _faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.FaceIndices.Length, sizeof(int));
            _faceIndicesBuffer.SetData(_voxelObject.FaceIndices);
            _computeShader.SetBuffer(_kernel, "_FaceIndices", _faceIndicesBuffer);

            if (_voxelIndicesBuffer != null && _voxelIndicesBuffer.count != _voxelObject.VoxelIndices.Length)
            {
                _voxelIndicesBuffer.Dispose();
                _voxelIndicesBuffer = null;
            }
            if (_voxelIndicesBuffer == null)
                _voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelIndices.Length, sizeof(int));
            _voxelIndicesBuffer.SetData(_voxelObject.VoxelIndices);
            _computeShader.SetBuffer(_kernel, "_VoxelIndices", _voxelIndicesBuffer);
        }

        private void SetMatrixBuffer()
        {
            if (_transformMatrixBuffer != null && _transformMatrixBuffer.count != _voxelObject.MaxInstanceCount)
            {
                _transformMatrixBuffer.Dispose();
                _transformMatrixBuffer = null;
            }
            if (_transformMatrixBuffer == null)
                _transformMatrixBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxInstanceCount, sizeof(float) * 16);
            _computeShader.SetBuffer(_kernel, "_TransformMatrices", _transformMatrixBuffer);
        }

        private void SetColorBuffer()
        {
            if (_colorIndicesBuffer != null && _colorIndicesBuffer.count != _voxelObject.ColorIndices.Length)
            {
                _colorIndicesBuffer.Dispose();
                _colorIndicesBuffer = null;
            }
            if (_colorIndicesBuffer == null)
                _colorIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.ColorIndices.Length, sizeof(int));
            _colorIndicesBuffer.SetData(_voxelObject.ColorIndices);
        }

        private void SetRenderParams()
        {
            _renderParams = new RenderParams(_material);
            _renderParams.worldBounds = _bounds;
            _renderParams.matProps = new MaterialPropertyBlock();

            _renderParams.matProps.SetBuffer("_TransformMatrices", _transformMatrixBuffer);
            _renderParams.matProps.SetBuffer("_ColorIndices", _colorIndicesBuffer);
            _renderParams.matProps.SetBuffer("_VoxelIndices", _voxelIndicesBuffer);
            _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            VoxelSharedData sharedData = GetSharedData();
            if (sharedData != null && sharedData.RotationMatricesBuffer != null)
            {
                _renderParams.matProps.SetBuffer("_VertexPositions", sharedData.FaceVertexBuffer);
                _computeShader.SetBuffer(_kernel, "_RotationMatrices", sharedData.RotationMatricesBuffer);
            }

            SetPalette(_voxelObject.PaletteIndex);
        }

        private void DisposeBuffers()
        {
            _voxelPositionBuffer?.Dispose();
            _voxelPositionBuffer = null;
            _voxelIndicesBuffer?.Dispose();
            _voxelIndicesBuffer = null;
            _faceIndicesBuffer?.Dispose();
            _faceIndicesBuffer = null;

            _transformMatrixBuffer?.Dispose();
            _transformMatrixBuffer = null;

            _colorIndicesBuffer?.Dispose();
            _colorIndicesBuffer = null;
        }

        #endregion Buffers

        void Update()
        {
            if (_voxelObject == null)
                return;

            if (_staticRender)
                StaticRender();
            else
                AnimationRender();
        }

        private void StaticRender()
        {
            VoxelSharedData sharedData = GetSharedData();
            if (sharedData != null)
                _staticMaterial.SetBuffer("_Colors", sharedData.GetColorBuffer(_voxelObject.PaletteIndex));
        }

        private void AnimationRender()
        {
            _timer += Time.deltaTime;
            if (_timer >= _frameDuration)
            {
                _frameIndex = (_frameIndex + 1) % _voxelObject.FrameCount;
                _timer -= _frameDuration;
                RunComputeShader();
            }

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                _bounds = _baseBounds;
                _bounds.center += transform.position;
                _renderParams.worldBounds = _bounds;
                RunComputeShader();
            }

            VoxelSharedData sharedData = GetSharedData();
            if (sharedData != null)
                Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, sharedData.FaceTriangleBuffer, sharedData.FaceTriangleCount, instanceCount: _voxelObject.InstanceCount[_frameIndex]);
        }

        private void RunComputeShader()
        {
            int instanceStartIndex = _voxelObject.InstanceStartIndices[_frameIndex];
            int instanceCount = _voxelObject.InstanceCount[_frameIndex];
            _computeShader.SetInt("_InstanceStartIndex", instanceStartIndex);
            _computeShader.SetInt("_InstanceCount", instanceCount);
            _computeShader.SetInt("_FrameIndex", _frameIndex);
            _computeShader.SetMatrix("_VoxelToWorldMatrix", transform.localToWorldMatrix);

            _computeShader.GetKernelThreadGroupSizes(_kernel, out _threadGroupSize, out _, out _);
            int threadGroups = Mathf.CeilToInt((float)instanceCount / _threadGroupSize);
            _computeShader.Dispatch(_kernel, threadGroups, 1, 1);

            _renderParams.matProps.SetInteger("_InstanceStartIndex", instanceStartIndex);
            _renderParams.matProps.SetVector("_Scale", transform.localScale);
        }

        private VoxelSharedData GetSharedData()
        {
            VoxelSharedData sharedData = VoxelSharedData.Instance;
#if UNITY_EDITOR
            if (!Application.isPlaying && (_sharedData != null || TryGetSharedData()))
                sharedData = _sharedData;
#endif
            return sharedData;
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
    }
}
