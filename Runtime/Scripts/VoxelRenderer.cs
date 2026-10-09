using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static FoxEdit.VoxelRenderer;


#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

//TODO: fix light dans shader
namespace FoxEdit
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Animator))]
    public class VoxelRenderer : MonoBehaviour
    {
        internal class AnimatedMeshData
        {
            public GraphicsBuffer OpaqueVertices = null;
            public GraphicsBuffer TransparentVertices = null;
            public GraphicsBuffer OpaqueQuads = null;
            public GraphicsBuffer TransparentQuads = null;
            public GraphicsBuffer Matrices = null;
            public Dictionary<VoxelRenderer, Matrix4x4> ObjectToWorldMatrices = null;
            public bool UpdateMatricesBuffer = false;
            public VoxelRenderer Drawer = null;
            public int UseCount = 0;
            public float AnimationTimer = 0.0f;
            public int FrameIndex = 0;
        }

        [System.Serializable]
        internal class AnimationEvent
        {
            public int AnimationIndex;
            public int FrameIndex;
            public UnityEvent Events;
        }

        //User editable
        [SerializeField] private VoxelObject _voxelObject = null;
        [SerializeField] private int _paletteIndexOverride = -1;
        [SerializeField] private bool _staticRender = false;
        [SerializeField] private bool _areBuffersBatched = true;
        [SerializeField] private List<AnimationEvent> _animationEvents = null;

        //Setup
        [SerializeField] private MeshFilter _meshFilter = null;
        [SerializeField] private MeshRenderer _meshRenderer = null;
        [SerializeField] private Animator _animator = null;

        public VoxelObject VoxelObject { get { return _voxelObject; } set { SetVoxelObject(value); } }
        public Animator VoxelAnimator { get { return _animator; } }
        private string _currentAnimationName { get { return $"{_voxelObject.name}_{_voxelObject.Animations[_animationIndex].AnimName}_{GetPaletteIndex()}"; } }

        private static Dictionary<string, AnimatedMeshData> _batchedAnimatedMeshData = null;
        private AnimatedMeshData _animatedMeshData = null;

        private int _animationIndex = 0;

        private VoxelRenderParams _renderParams = null;

        #region Initialization

        private void InitializeAnimatedRenderer()
        {
            _renderParams.CreateAnimatedParams();
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
            else
            {
                Material[] materials = _meshRenderer.sharedMaterials;
                _renderParams.SetStaticMaterials(materials.FirstOrDefault(m => m.name.Contains("Opaque")), materials.FirstOrDefault(m => m.name.Contains("Transparent")));
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(gameObject);
#endif
        }

#if UNITY_EDITOR
        private void CheckDuplication()
        {
            PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            SerializedObject serializedObject = new SerializedObject(this);
            inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
            SerializedProperty localIdProp = serializedObject.FindProperty("m_LocalIdentfierInFile");
            int localId = localIdProp.intValue;

            if (localId == 0)
                _meshRenderer.sharedMaterial = null;
        }
#endif

        void Awake()
        {
            if (_areBuffersBatched && _batchedAnimatedMeshData == null)
                _batchedAnimatedMeshData = new Dictionary<string, AnimatedMeshData>();

            _renderParams = new VoxelRenderParams();
            GetUsedComponents();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                CheckDuplication();
#endif

            if (_voxelObject == null)
                return;
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                InitializeAnimatedRenderer();
            Setup();
            StaticRender();
        }

        #endregion Initialization

        #region UserEditable

        public void SetVoxelObject(VoxelObject voxelObject)
        {
            if (voxelObject == _voxelObject)
                return;

            _voxelObject = voxelObject;
            _animationIndex = 0;

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
                    DisposeBuffers(out _);
                else
                    SetBufferData();
#if UNITY_EDITOR
            }
#endif
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
                    _paletteIndexOverride = -1;
                else if (index != -1)
                    _paletteIndexOverride = index;
            }

            if (_areBuffersBatched)
            {
                int frameIndex = -1;
                DisposeBuffers(out frameIndex);
                SetBufferData(frameIndex);
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
            if (_renderParams == null)
                _renderParams = new VoxelRenderParams();

            bool opaque = _voxelObject.Animations[0].HasOpaqueFaces;
            bool transparent = _voxelObject.Animations[0].HasTransparentFaces;

            Material[] materials = _meshRenderer.sharedMaterials;
            if (materials != null && materials.Length > 0 && materials[0] != null)
                _renderParams.SetStaticMaterials(materials.FirstOrDefault(m => m.name.Contains("Opaque")), materials.FirstOrDefault(m => m.name.Contains("Transparent")));
            else
                _renderParams.CreateStaticMaterial(opaque, transparent);

            if (opaque && transparent)
                _meshRenderer.SetMaterials(new List<Material> { _renderParams.StaticOpaqueMaterial, _renderParams.StaticTransparentMaterial });
            else if (opaque)
                _meshRenderer.SetMaterials(new List<Material> { _renderParams.StaticOpaqueMaterial });
            else if (transparent)
                _meshRenderer.SetMaterials(new List<Material> { _renderParams.StaticTransparentMaterial });
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
            DisposeBuffers(out _);

            _animationIndex = animationIndex;
            SetWorldBounds();

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
                DisposeBuffers(out _);
        }

        private void CreateVoxelBuffers()
        {
            _animatedMeshData = new AnimatedMeshData();
            _animatedMeshData.ObjectToWorldMatrices = new Dictionary<VoxelRenderer, Matrix4x4>();
            _animatedMeshData.Drawer = this;

            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
            {
                _animatedMeshData.OpaqueVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxOpaqueVerticesCount, sizeof(float) * 3);
                _animatedMeshData.OpaqueQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxOpaqueQuadsCount, sizeof(int));
            }
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
            {
                _animatedMeshData.TransparentVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxTransparentVerticesCount, sizeof(float) * 3);
                _animatedMeshData.TransparentQuads = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.MaxTransparentQuadsCount, sizeof(int));
            }

            if (_areBuffersBatched)
                _batchedAnimatedMeshData.Add(_currentAnimationName, _animatedMeshData);
        }

        private void SetMatricesBuffer()
        {
            if (_animatedMeshData.UpdateMatricesBuffer)
            {
                if (_animatedMeshData.Matrices == null || _animatedMeshData.Matrices.count != _animatedMeshData.ObjectToWorldMatrices.Count)
                {
                    _animatedMeshData.Matrices?.Dispose();
                    _animatedMeshData.Matrices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _animatedMeshData.ObjectToWorldMatrices.Count, sizeof(float) * 16);
                }
                _animatedMeshData.Matrices.SetData(_animatedMeshData.ObjectToWorldMatrices.Values.ToArray());
                _animatedMeshData.UpdateMatricesBuffer = false;
            }
        }

        private void AddMatrixToBatch()
        {
            _animatedMeshData.ObjectToWorldMatrices.Add(this, transform.localToWorldMatrix);
            _animatedMeshData.UpdateMatricesBuffer = true;
        }

        private void RemoveMatrixFromBatch()
        {
            _animatedMeshData.ObjectToWorldMatrices.Remove(this);
            _animatedMeshData.UpdateMatricesBuffer = true;
        }

        private bool GetActiveBuffers()
        {
            if (_areBuffersBatched)
                return _batchedAnimatedMeshData.TryGetValue(_currentAnimationName, out _animatedMeshData);

            if (_animatedMeshData == null)
                return false;
            return true;
        }

        private void SetBufferData(int startFrameIndex = -1)
        {
            if (!GetActiveBuffers())
            {
                CreateVoxelBuffers();

                if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                {
                    _animatedMeshData.OpaqueVertices.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Vertices);
                    _animatedMeshData.OpaqueQuads.SetData(_voxelObject.Animations[_animationIndex].OpaqueMesh.Quads);
                }
                if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                {
                    _animatedMeshData.TransparentVertices.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Vertices);
                    _animatedMeshData.TransparentQuads.SetData(_voxelObject.Animations[_animationIndex].TransparentMesh.Quads);
                }

                if (startFrameIndex != -1)
                    _animatedMeshData.FrameIndex = startFrameIndex;
            }

            _animatedMeshData.UseCount += 1;
            AddMatrixToBatch();

            _renderParams.SetBuffers(_voxelObject.Animations[_animationIndex], _animatedMeshData);
            _renderParams.SetInstancesData(_voxelObject.Animations[_animationIndex], _animatedMeshData.FrameIndex);

            CheckForFrameEvents(_animationIndex, _animatedMeshData.FrameIndex);
        }

        private void DisposeBuffers(out int frameIndex)
        {
            if (_animatedMeshData == null)
            {
                frameIndex = -1;
                return;
            }

            frameIndex = _animatedMeshData.FrameIndex;

            if (_areBuffersBatched)
            {
                if (_animatedMeshData.UseCount > 1)
                {
                    _animatedMeshData.UseCount -= 1;
                    RemoveMatrixFromBatch();
                    if (_animatedMeshData.Drawer == this)
                        _animatedMeshData.Drawer = null;

                    _animatedMeshData = null;
                    return;
                }
                _batchedAnimatedMeshData.Remove(_currentAnimationName);
            }

            _animatedMeshData.OpaqueVertices?.Dispose();
            _animatedMeshData.OpaqueQuads?.Dispose();
            _animatedMeshData.TransparentVertices?.Dispose();
            _animatedMeshData.TransparentQuads?.Dispose();
            _animatedMeshData.Matrices?.Dispose();
            _animatedMeshData = null;
        }

        internal void SwitchBatchMode()
        {
            _areBuffersBatched = !_areBuffersBatched;
        }

        #endregion Buffers

        #region Events

        private void CheckForFrameEvents(int animationIndex, int frameIndex)
        {
            foreach (AnimationEvent animationEvent in _animationEvents)
            {
                if (animationEvent.AnimationIndex == animationIndex && animationEvent.FrameIndex == frameIndex)
                    animationEvent.Events?.Invoke();
            }
        }

        #endregion Events

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

            _renderParams.UpdateStaticMaterials(colorBuffer); ;
        }

        private void ManageAnimation()
        {
            _animatedMeshData.AnimationTimer += Time.deltaTime;

            if (_animatedMeshData.AnimationTimer >= _voxelObject.Animations[_animationIndex].FrameDuration)
            {
                _animatedMeshData.FrameIndex = (_animatedMeshData.FrameIndex + 1) % _voxelObject.Animations[_animationIndex].FrameCount;
                _animatedMeshData.AnimationTimer -= _voxelObject.Animations[_animationIndex].FrameDuration;
                _renderParams.SetInstancesData(_voxelObject.Animations[_animationIndex], _animatedMeshData.FrameIndex);

                VoxelRenderer[] batchedRenderers = _animatedMeshData.ObjectToWorldMatrices.Keys.ToArray();
                for (int i = 0; i < batchedRenderers.Length; i++)
                {
                    batchedRenderers[i].CheckForFrameEvents(_animationIndex, _animatedMeshData.FrameIndex);
                }
            }
        }

        private void ManageTransformUpdate()
        {
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                SetWorldBounds();

                if (_animatedMeshData != null)
                {
                    RemoveMatrixFromBatch();
                    AddMatrixToBatch();
                }
                _renderParams.SetObjectToWorldMatrix(transform.localToWorldMatrix);
            }
        }

        private void AnimationRender()
        {
            if (_animatedMeshData == null)
                return;

            ManageTransformUpdate();

            if (_areBuffersBatched)
            {
                if (_animatedMeshData.Drawer == null)
                    _animatedMeshData.Drawer = this;

                if (_animatedMeshData.Drawer != this)
                    return;
            }

            ManageAnimation();

#if UNITY_EDITOR
            if (VoxelSharedData.FaceTriangleBuffer == null)
                return;
#endif
            SetMatricesBuffer();
            _renderParams.SetBuffers(_voxelObject.Animations[_animationIndex], _animatedMeshData);

            if (_voxelObject.Animations[_animationIndex].HasOpaqueFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.OpaqueRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].OpaqueMesh.InstanceCount[_animatedMeshData.FrameIndex] * _animatedMeshData.UseCount);
            if (_voxelObject.Animations[_animationIndex].HasTransparentFaces)
                Graphics.RenderPrimitivesIndexed(_renderParams.TransparentRenderParams, MeshTopology.Triangles, VoxelSharedData.FaceTriangleBuffer, 6 /* 2 triangles */, instanceCount: _voxelObject.Animations[_animationIndex].TransparentMesh.InstanceCount[_animatedMeshData.FrameIndex] * _animatedMeshData.UseCount);
        }

        #endregion Rendering
    }
}
