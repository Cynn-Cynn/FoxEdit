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
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Animator))]
    public class VoxelRenderer : MonoBehaviour
    {
        internal class VoxelBuffers
        {
            public GraphicsBuffer OpaqueVertices = null;
            public GraphicsBuffer TransparentVertices = null;
            public GraphicsBuffer OpaqueQuads = null;
            public GraphicsBuffer TransparentQuads = null;
            public int UseCount = 0;
        }

        //User editable
        [SerializeField] private VoxelObject _voxelObject = null;
        [SerializeField] private int _paletteIndexOverride = -1;
        [SerializeField] private bool _staticRender = false;

        //Setup
        [SerializeField] private MeshFilter _meshFilter = null;
        [SerializeField] private MeshRenderer _meshRenderer = null;
        [SerializeField] private Animator _animator = null;

        public VoxelObject VoxelObject { get { return _voxelObject; } set { SetVoxelObject(value); } }
        public Animator VoxelAnimator { get { return _animator; } }
        private string _currentAnimationName { get { return _voxelObject.name + "_" + _voxelObject.Animations[_animationIndex].AnimName; } }

        private static Dictionary<string, VoxelBuffers> _buffers = null;
        private Material _staticOpaqueMaterialInstance = null;
        private Material _staticTransparentMaterialInstance = null;

        private float _animationTimer = 0.0f;
        private int _animationIndex = 0;
        private int _frameIndex = 0;

        private VoxelRenderParams _renderParams = null;

        #region Initialization

        private void InitializeAnimatedRenderer()
        {
            _renderParams = new VoxelRenderParams();
            SetWorldBounds();
        }

        internal void GetUsedComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
                _meshRenderer = GetComponent<MeshRenderer>();
                _animator = GetComponent<Animator>();
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(gameObject);
#endif
        }

        private void Initialize()
        {
            GetUsedComponents();

            if (_voxelObject == null)
                return;
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                InitializeAnimatedRenderer();
            Setup();
            StaticRender();
        }

        void Awake()
        {
            if (_buffers == null)
                _buffers = new Dictionary<string, VoxelBuffers>();
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

            if (_voxelObject.StaticMesh != null)
                Setup();

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

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                _meshRenderer.enabled = _staticRender;
                if (_staticRender)
                    DisposeBuffers();
                else
                    SetBufferData();
#if UNITY_EDITOR
            }
#endif
            _animationTimer = 0.0f;
        }

        internal void HideMesh()
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        internal void ShowMesh()
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = true;
        }

        public int GetPaletteIndex()
        {
            if (_paletteIndexOverride == -1)
                return _voxelObject == null ? -1 : _voxelObject.PaletteIndex;
            return _paletteIndexOverride;
        }

        public void SetPalette(int index)
        {
            if (index == _paletteIndexOverride || (_paletteIndexOverride == -1 && index == _voxelObject.PaletteIndex))
                return;

            index = index == -1 ? _voxelObject.PaletteIndex : index;
            GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(index);
            if (colorsBuffer != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
                    _renderParams.SetColorsBuffer(colorsBuffer);

                if (index == _voxelObject.PaletteIndex)
                {
                    _staticOpaqueMaterialInstance = null;
                    _staticTransparentMaterialInstance = null;
                    _paletteIndexOverride = -1;
                }
                else if (index != -1)
                {
                    _paletteIndexOverride = index;
                }

                SetupMaterials();
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#endif
        }

        private void SetupMaterials()
        {
            if (_paletteIndexOverride != -1 && _paletteIndexOverride != _voxelObject.PaletteIndex)
                CreateStaticMaterialInstances();
            else
                SetDefaultStaticMaterials();
        }

        private void SetDefaultStaticMaterials()
        {
            if (_voxelObject.Animations[0].HasOpaqueFaces && _voxelObject.Animations[0].HasTransparentFaces)
                _meshRenderer.SetMaterials(new List<Material> { _voxelObject.StaticOpaqueMaterial, _voxelObject.StaticTransparentMaterial });
            else if (_voxelObject.Animations[0].HasOpaqueFaces)
                _meshRenderer.SetMaterials(new List<Material> { _voxelObject.StaticOpaqueMaterial });
            else if (_voxelObject.Animations[0].HasTransparentFaces)
                _meshRenderer.SetMaterials(new List<Material> { _voxelObject.StaticTransparentMaterial });
        }

        private void CreateStaticMaterialInstances()
        {
            _staticOpaqueMaterialInstance = new Material(_voxelObject.StaticOpaqueMaterial);
            _staticOpaqueMaterialInstance.name = _voxelObject.StaticOpaqueMaterial.name + "_Instance";

            _staticTransparentMaterialInstance = new Material(_voxelObject.StaticTransparentMaterial);
            _staticTransparentMaterialInstance.name = _voxelObject.StaticTransparentMaterial.name + "_Instance";

            _meshRenderer.SetMaterials(new List<Material> { _staticOpaqueMaterialInstance, _staticTransparentMaterialInstance });
        }

        public void SetAnimation(int animationIndex)
        {
            if (animationIndex == _animationIndex || animationIndex >= _voxelObject.Animations.Length)
                return;

            StartCoroutine(SetAnimationBuffered(animationIndex));
        }

        private IEnumerator SetAnimationBuffered(int animationIndex)
        {
            yield return new WaitForEndOfFrame();

            _animationIndex = animationIndex;
            _animationTimer = 0.0f;
            _frameIndex = 0;
            SetWorldBounds();

            _renderParams.SetInstanceStartIndex(_voxelObject.Animations[_animationIndex], _frameIndex);

            SetBufferData();
        }

        #endregion UserEditable

        #region Buffers

        internal void Setup()
        {
            if (_voxelObject == null)
                return;
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                _meshFilter.mesh = _voxelObject.StaticMesh;
#if UNITY_EDITOR
            else
                _meshFilter.sharedMesh = _voxelObject.StaticMesh;
#endif

            _animator.runtimeAnimatorController = _voxelObject.AnimatorController;
#if UNITY_EDITOR
            _meshRenderer.enabled = _staticRender || !Application.isPlaying;
#else
            _meshRenderer.enabled = _staticRender;
#endif
            SetWorldBounds();
            SetupMaterials();
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                GraphicsBuffer colorsBuffer = VoxelSharedData.GetColorBuffer(GetPaletteIndex());
                if (colorsBuffer != null)
                    _renderParams.SetColorsBuffer(colorsBuffer);
                if (!_staticRender)
                    SetBufferData();
#if UNITY_EDITOR
            }
#endif
        }

        private void SetWorldBounds()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                Bounds bounds = _voxelObject.Animations[_animationIndex].Bounds;
                bounds.center += transform.position;
                _renderParams.SetWorldBounds(bounds);
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

        private void CreateBuffers()
        {
            VoxelBuffers buffers = null;
            string bufferKey = _currentAnimationName;

            if (_buffers.TryGetValue(bufferKey, out buffers))
                return;

            buffers = new VoxelBuffers();
            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
            {
                buffers.OpaqueVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxOpaqueVerticesCount, sizeof(float) * 3);
                buffers.OpaqueQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxOpaqueQuadsCount, sizeof(int));
            }
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
            {
                buffers.TransparentVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxTransparentVerticesCount, sizeof(float) * 3);
                buffers.TransparentQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxTransparentQuadsCount, sizeof(int));
            }
            _buffers.Add(bufferKey, buffers);
        }

        private void SetBufferData()
        {
            VoxelBuffers buffers = null;
            string bufferKey = _currentAnimationName;

            if (!_buffers.TryGetValue(bufferKey, out buffers))
            {
                CreateBuffers();
                buffers = _buffers[bufferKey];

                if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                {
                    buffers.OpaqueVertices.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Vertices);
                    buffers.OpaqueQuads.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Quads);
                }
                if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                {
                    buffers.TransparentVertices.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Vertices);
                    buffers.TransparentQuads.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Quads);
                }
            }

            _renderParams.SetVerticesAndQuads(_voxelObject.Animations[_animationIndex], buffers);
            buffers.UseCount += 1;
        }

        private void DisposeBuffers()
        {
            VoxelBuffers buffers;
            string bufferKey = _currentAnimationName;

            if (!_buffers.TryGetValue(bufferKey, out buffers))
                return;

            if (buffers.UseCount > 1)
            {
                buffers.UseCount -= 1;
                return;
            }

            buffers.OpaqueVertices?.Dispose();
            buffers.OpaqueQuads?.Dispose();
            buffers.TransparentVertices?.Dispose();
            buffers.TransparentQuads?.Dispose();

            _buffers.Remove(bufferKey);
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
            GraphicsBuffer colorBuffer = VoxelSharedData.GetColorBuffer(GetPaletteIndex());
            if (colorBuffer == null)
                return;

            if (_staticOpaqueMaterialInstance != null)
            {
                _staticOpaqueMaterialInstance.SetBuffer("_Colors", colorBuffer);
                _staticOpaqueMaterialInstance.SetInt("_ColorCount", colorBuffer.count);
                _staticTransparentMaterialInstance.SetBuffer("_Colors", colorBuffer);
                _staticTransparentMaterialInstance.SetInt("_ColorCount", colorBuffer.count);
            }
            else
            {
                _voxelObject.StaticOpaqueMaterial.SetBuffer("_Colors", colorBuffer);
                _voxelObject.StaticOpaqueMaterial.SetInt("_ColorCount", colorBuffer.count);
                _voxelObject.StaticTransparentMaterial.SetBuffer("_Colors", colorBuffer);
                _voxelObject.StaticTransparentMaterial.SetInt("_ColorCount", colorBuffer.count);
            }
        }

        private void AnimationRender()
        {
            _animationTimer += Time.deltaTime;

            if (_animationTimer >= _voxelObject.Animations[_animationIndex].FrameDuration)
            {
                _frameIndex = (_frameIndex + 1) % _voxelObject.Animations[_animationIndex].FrameCount;
                _animationTimer -= _voxelObject.Animations[_animationIndex].FrameDuration;
                _renderParams.SetInstanceStartIndex(_voxelObject.Animations[_animationIndex], _frameIndex);
            }

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                SetWorldBounds();
                _renderParams.SetObjectToWorldMatrix(transform.localToWorldMatrix);
            }

#if UNITY_EDITOR
            if (VoxelSharedData.FaceTriangleBuffer == null)
                return;
#endif

            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.OpaqueRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].OpaqueMesh.InstanceCount[_frameIndex]);
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.TransparentRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].TransparentMesh.InstanceCount[_frameIndex]);
        }

        #endregion Rendering
    }
}
