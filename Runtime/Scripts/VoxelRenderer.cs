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
        private enum OpacityType
        {
            Both,
            Opaque,
            Transparent
        }

        //User editable
        [SerializeField] private VoxelObject _voxelObject = null;
        [SerializeField] private int _paletteIndexOverride = -1;
        [SerializeField] private bool _staticRender = false;
        [SerializeField] private float _frameDuration = 0.2f;

        //Setup
        [SerializeField] private MeshFilter _meshFilter = null;
        [SerializeField] private MeshRenderer _meshRenderer = null;

        public VoxelObject VoxelObject { get { return _voxelObject; } set { SetVoxelObject(value); } }

        private GraphicsBuffer _opaqueVerticesBuffer = null;
        private GraphicsBuffer _transparentVerticesBuffer = null;
        private GraphicsBuffer _opaqueQuadsBuffer = null;
        private GraphicsBuffer _transparentQuadsBuffer = null;
        private Material _staticOpaqueMaterialInstance = null;
        private Material _staticTransparentMaterialInstance = null;

        private float _timer = 0.0f;
        private int _animationIndex = 0;
        private int _frameIndex = 0;

        RenderParams _opaqueRenderParams;
        RenderParams _transparentRenderParams;

        #region Initialization

        private void InitializeRenderParams()
        {
            _opaqueRenderParams = new RenderParams(_voxelObject.AnimatedOpaqueMaterial);
            _opaqueRenderParams.matProps = new MaterialPropertyBlock();
            _opaqueRenderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _opaqueRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);

            _transparentRenderParams = new RenderParams(_voxelObject.AnimatedTransparentMaterial);
            _transparentRenderParams.matProps = new MaterialPropertyBlock();
            _transparentRenderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _transparentRenderParams.matProps.SetBuffer("_VertexPositions", VoxelSharedData.FaceVertexBuffer);

            SetWorldBounds();
        }

        private void InitializeStaticRenderer()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            if (_paletteIndexOverride != _voxelObject.PaletteIndex)
                CreateStaticMaterialInstances();

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
                    DisposeBuffers(OpacityType.Both);
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

        //public bool IsStaticRender => _staticRender;

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
                    SetColorBufferParam(colorsBuffer);
#endif

                if (_paletteIndexOverride == -1 && index != -1)
                {
                    CreateStaticMaterialInstances();
                    _paletteIndexOverride = index;
                }
                else if (index == _voxelObject.PaletteIndex)
                {
                    _meshRenderer.SetMaterials(new List<Material>{ _voxelObject.StaticOpaqueMaterial, _voxelObject.StaticTransparentMaterial });
                    _staticOpaqueMaterialInstance = null;
                    _staticTransparentMaterialInstance = null;
                    _paletteIndexOverride = -1;
                }
            }

#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(gameObject);
                AssetDatabase.SaveAssets();
            }
#endif
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

            _animationIndex = animationIndex;
            SetVoxelBuffers();
            SetWorldBounds();
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
                    SetColorBufferParam(colorsBuffer);
                if (!_staticRender)
                    SetVoxelBuffers();
#if UNITY_EDITOR
            }
#endif
        }

        private void SetColorBufferParam(GraphicsBuffer colorsBuffer)
        {
            _opaqueRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
            _transparentRenderParams.matProps.SetBuffer("_Colors", colorsBuffer);
        }

        private void SetVoxelBuffers()
        {
            if (_opaqueVerticesBuffer != null && _opaqueVerticesBuffer.count != _voxelObject.Animations[_animationIndex].OpaqueMesh.Vertices.Length)
                DisposeBuffers(OpacityType.Opaque);
            if (_transparentVerticesBuffer != null && _transparentVerticesBuffer.count != _voxelObject.Animations[_animationIndex].TransparentMesh.Vertices.Length)
                DisposeBuffers(OpacityType.Transparent);

            if (_opaqueVerticesBuffer == null && _voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                CreateBuffers(OpacityType.Opaque);
            if (_transparentVerticesBuffer == null && _voxelObject.Animations[_animationIndex].HasTransparentFaces)
                CreateBuffers(OpacityType.Transparent);

            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                SetBufferData(OpacityType.Opaque);
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                SetBufferData(OpacityType.Transparent);
        }

        private void SetWorldBounds()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                Bounds bounds = _voxelObject.Animations[_animationIndex].Bounds;
                bounds.center += transform.position;
                _opaqueRenderParams.worldBounds = bounds;
                _transparentRenderParams.worldBounds = bounds;
#if UNITY_EDITOR
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                DisposeBuffers(OpacityType.Both);
        }

        private void CreateBuffers(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Animations[_animationIndex].OpaqueMesh.Vertices.Length, sizeof(float) * 3);
                _opaqueQuadsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Animations[_animationIndex].OpaqueMesh.Quads.Length, sizeof(int));
            }
            else if (opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Animations[_animationIndex].TransparentMesh.Vertices.Length, sizeof(float) * 3);
                _transparentQuadsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.Animations[_animationIndex].TransparentMesh.Quads.Length, sizeof(int));
            }
        }

        private void SetBufferData(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Vertices);
                _opaqueQuadsBuffer.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Quads);
                _opaqueRenderParams.matProps.SetBuffer("_Vertices", _opaqueVerticesBuffer);
                _opaqueRenderParams.matProps.SetBuffer("_Quads", _opaqueQuadsBuffer);
            }
            else if (opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Vertices);
                _transparentQuadsBuffer.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Quads);
                _transparentRenderParams.matProps.SetBuffer("_Vertices", _transparentVerticesBuffer);
                _transparentRenderParams.matProps.SetBuffer("_Quads", _transparentQuadsBuffer);
            }
        }

        private void DisposeBuffers(OpacityType opacityType)
        {
            if (opacityType == OpacityType.Both || opacityType == OpacityType.Opaque)
            {
                _opaqueVerticesBuffer?.Dispose();
                _opaqueVerticesBuffer = null;

                _opaqueQuadsBuffer?.Dispose();
                _opaqueQuadsBuffer = null;
            }
            if (opacityType == OpacityType.Both || opacityType == OpacityType.Transparent)
            {
                _transparentVerticesBuffer?.Dispose();
                _transparentVerticesBuffer = null;

                _transparentQuadsBuffer?.Dispose();
                _transparentQuadsBuffer = null;
            }
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
            if (_staticOpaqueMaterialInstance != null)
            {
                _staticOpaqueMaterialInstance.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(GetPaletteIndex()));
                _staticTransparentMaterialInstance.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(GetPaletteIndex()));
            }
            else
            {
                _voxelObject.StaticOpaqueMaterial.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(GetPaletteIndex()));
                _voxelObject.StaticTransparentMaterial.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(GetPaletteIndex()));
            }
        }

        private void AnimationRender()
        {
            _timer += Time.deltaTime;

            if (_timer >= _frameDuration)
            {
                _frameIndex = (_frameIndex + 1) % _voxelObject.Animations[_animationIndex].FrameCount;
                _timer -= _frameDuration;
                if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                    _opaqueRenderParams.matProps.SetInteger("_InstanceStartIndex", _voxelObject.Animations[_animationIndex].OpaqueMesh.InstanceStartIndices[_frameIndex]);
                if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                    _transparentRenderParams.matProps.SetInteger("_InstanceStartIndex", _voxelObject.Animations[_animationIndex].TransparentMesh.InstanceStartIndices[_frameIndex]);
            }

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                SetWorldBounds();
                _opaqueRenderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
                _transparentRenderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            }

            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                Graphics.RenderPrimitivesIndexed(_opaqueRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].OpaqueMesh.InstanceCount[_frameIndex]);
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                Graphics.RenderPrimitivesIndexed(_transparentRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].TransparentMesh.InstanceCount[_frameIndex]);
        }

        #endregion Rendering
    }
}
