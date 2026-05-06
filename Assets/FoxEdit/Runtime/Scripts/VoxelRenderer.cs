using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

//TODO: fix light dans shader
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
        [SerializeField] private MeshFilter _meshFilter = null;
        [SerializeField] private MeshRenderer _meshRenderer = null;

        public VoxelObject VoxelObject { get { return _voxelObject; } set { SetVoxelObject(value); } }

        private GraphicsBuffer _verticesBuffer = null;
        private Material _staticMaterialInstance = null;

        private float _timer = 0.0f;
        private int _animationIndex = 0;
        private int _frameIndex = 0;

        RenderParams _renderParams;

        #region Initialization

        private void InitializeRenderParams()
        {
            _renderParams = new RenderParams(_voxelObject.AnimatedMaterial);
            _renderParams.matProps = new MaterialPropertyBlock();
            _renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _renderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);
            SetWorldBounds();
        }

        private void InitializeStaticRenderer()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _staticMaterialInstance = new Material(_voxelObject.StaticMaterial);
            _meshRenderer.material = _staticMaterialInstance;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        internal void Initialize(bool setupForRender = false)
        {
            InitializeStaticRenderer();
#if UNITY_EDITOR
            if (Application.isPlaying || setupForRender)
            {
#endif
                InitializeRenderParams();
                Setup();
#if UNITY_EDITOR
            }
#endif
        }

        void Start()
        {
            Initialize();
        }

        #endregion Initialization

        #region UserEditable

        public void SetVoxelObject(VoxelObject voxelObject)
        {
            if (voxelObject == _voxelObject)
                return;

            _voxelObject = voxelObject;
            _animationIndex = 0;
            _frameIndex = 0;

#if UNITY_EDITOR

            if (Application.isPlaying && _voxelObject.StaticMesh != null)
            {
                Setup();
            }

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#else
            if (_voxelObject.StaticMesh != null)
                Refresh();
#endif
        }

        public void RenderSwap()
        {
            _staticRender = !_staticRender;

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                _meshRenderer.enabled = _staticRender;
                if (_staticRender)
                    DisposeBuffers();
                else
                    SetVoxelBuffers();
#endif
#if UNITY_EDITOR
            }
#endif
            _timer = 0.0f;
        }

        //public void SetAnimatedRender()
        //{
        //    _staticRender = false;
        //    _meshRenderer.enabled = false;
        //    _timer = 0.0f;
        //}

        //public void SetStaticRender()
        //{
        //    _staticRender = true;
        //    _meshRenderer.enabled = true;
        //}

        public bool IsStaticRender => _staticRender;

        public int GetPaletteIndex()
        {
            if (_paletteIndexOverride == -1)
                return _voxelObject == null ? -1 : _voxelObject.PaletteIndex;
            return _paletteIndexOverride;
        }

        public void SetPalette(int index)
        {
            GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(index);

            if (colorsBuffer != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
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

        internal void Setup()
        {
            if (_voxelObject == null)
                return;

            _meshFilter.mesh = _voxelObject.StaticMesh;
#if UNITY_EDITOR
            _meshRenderer.enabled = _staticRender || !Application.isPlaying;
#else
            _meshRenderer.enabled = _staticRender;
#endif
            SetWorldBounds();
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(GetPaletteIndex());
                if (colorsBuffer != null)
                    _renderParams.matProps.SetBuffer("_Colors", colorsBuffer);
                if (!_staticRender)
                    SetVoxelBuffers();
#if UNITY_EDITOR
            }
#endif
        }

        private void SetVoxelBuffers()
        {
            if (_verticesBuffer != null && _verticesBuffer.count != _voxelObject.Vertices.Length)
            {
                _verticesBuffer.Dispose();
                _verticesBuffer = null;
            }
            if (_verticesBuffer == null)
                _verticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Vertices.Length, sizeof(float) * 4);
            _verticesBuffer.SetData(_voxelObject.Vertices);
            _renderParams.matProps.SetBuffer("_Vertices", _verticesBuffer);
        }

        private void SetWorldBounds()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                Bounds bounds = _voxelObject.Bounds;
                bounds.center += transform.position;
                _renderParams.worldBounds = bounds;
#if UNITY_EDITOR
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                DisposeBuffers();
        }

        private void DisposeBuffers()
        {
            _verticesBuffer?.Dispose();
            _verticesBuffer = null;
        }

        #endregion Buffers

        #region Rendering

        void Update()
        {
            if (_voxelObject == null)
                return;

#if UNITY_EDITOR
            if (_staticRender || !Application.isPlaying)
                StaticRender();
            else
                AnimationRender();
#else
            if (_staticRender)
                StaticRender();
            else
                AnimationRender();
#endif
        }

        private void StaticRender()
        {
            _staticMaterialInstance.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(GetPaletteIndex()));
        }

        private void AnimationRender()
        {
            _timer += Time.deltaTime;
            int linearFrameIndex = _voxelObject.AnimationIndices[_animationIndex].StartIndex + _frameIndex;

            if (_timer >= _frameDuration)
            {
                _frameIndex = (_frameIndex + 1) % _voxelObject.AnimationIndices[_animationIndex].FrameCount;
                _timer -= _frameDuration;
                _renderParams.matProps.SetInteger("_InstanceStartIndex", _voxelObject.InstanceStartIndices[linearFrameIndex]);
            }

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                SetWorldBounds();
                _renderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0.0f, 180.0f, 0.0f)));
            }

            Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, VoxelSharedData.FaceTriangleCount, instanceCount: _voxelObject.InstanceCount[linearFrameIndex]);
        }

        #endregion Rendering
    }
}
