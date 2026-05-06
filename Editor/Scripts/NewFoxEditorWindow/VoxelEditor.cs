
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FoxEdit.VoxelTools;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.IO;

namespace FoxEdit
{
    internal partial class VoxelEditor : IDisposable
    {
        #region STATIC_FIELDS
        public static event Action<vxTool> OnChangeTool;
        public static event Action<vxAction> OnChangeAction;
        public static event Action<int> OnChangeColor;
        public static event Action<int> OnChangePalette;

        private static vxAction _action = vxAction.Paint;
        public static vxAction Action
        {
            get => _action;
            set
            {
                if (_action == value)
                    return;
                _action = value;
                OnChangeAction?.Invoke(_action);
            }
        }

        private static vxTool _tool = vxTool.Brush;
        public static vxTool Tool
        {
            get => _tool;
            set
            {
                if (_tool == value)
                    return;
                _tool = value;
                OnChangeTool?.Invoke(_tool);
            }
        }

        private static int _colorIndex;
        public static int ColorIndex
        {
            get => _colorIndex;
            set
            {
                _colorIndex = Mathf.Clamp(value, 0, CurrentPalette.Colors.Length - 1);
                OnChangeColor?.Invoke(_colorIndex);
            }
        }

        private static int _paletteIndex;
        public static int PaletteIndex
        {
            get => _paletteIndex;
            set
            {
                if (_paletteIndex == value)
                    return;
                _paletteIndex = Mathf.Clamp(value, 0, VoxelSharedData.GetPaletteCount() - 1);
                ColorIndex = ColorIndex;
                OnChangePalette?.Invoke(_paletteIndex);
            }
        }

        public static VoxelPalette CurrentPalette => VoxelSharedData.GetPalette(PaletteIndex);

        #endregion
        //Thumbnails
        public event Action<int, int, Texture2D> OnFramesThumbnailsUpdated;
        //Flags
        private bool _edit = false;
        public bool IsDirty { get; private set; } = false;

        //Mesh
        private VoxelRenderer _voxelRenderer = null;
        private Material[][] _editorMaterials = null;

        public event Action<int> OnFrameIndexChanged;
        private VoxelEditorFrame lastVisibleEditorFrame = null;
        private int _selectedFrameIndex = 0;
        public int SelectedFrameIndex
        {
            get => _selectedFrameIndex;
            set
            {
                _selectedFrameIndex = value;
                OnFrameIndexChanged?.Invoke(_selectedFrameIndex);
            }
        }
        public VoxelEditorFrame CurrentFrame
        {
            get
            {
                if (CurrentAnimation == null)
                    return null;
                return CurrentAnimation[SelectedFrameIndex];
            }
        }

        public event Action<int> OnAnimationIndexChanged;
        private int _selectedAnimationIndex = 0;
        public int SelectedAnimationIndex
        {
            get => _selectedAnimationIndex;
            set
            {
                _selectedAnimationIndex = value;
                ChangeFrame(0);
                OnAnimationIndexChanged?.Invoke(_selectedAnimationIndex);
            }
        }

        public VoxelEditorAnimation CurrentAnimation
        {
            get
            {
                if (_animationList == null || _animationList.Count == 0 || SelectedAnimationIndex < 0 || SelectedAnimationIndex >= _animationList.Count)
                    return null;
                return _animationList[_selectedAnimationIndex];
            }
        }

        //Scene editor voxels
        private MeshRenderer _voxelPrefab = null;
        private Transform _voxelParent = null;
        private List<VoxelEditorAnimation> _animationList;
        private bool wasVoxelRendererStatic = false;


        //Save
        private ComputeShader _computeStaticMesh = null;

        #region Init
        public VoxelEditor(VoxelRenderer voxelRenderer)
        {
            _voxelRenderer = voxelRenderer;
            _computeStaticMesh = FoxEditSettings.GetSettings().staticShader;
            _animationList = new List<VoxelEditorAnimation>();
            _voxelPrefab = FoxEditEditorSettings.Instance.VoxelPrefab.Asset;
            VoxelEditor.OnChangePalette += OnPaletteChanged;
            if (voxelRenderer.IsStaticRender)
            {
                wasVoxelRendererStatic = true;
                voxelRenderer.SetAnimatedRender();
            }

            CreateMaterials(); ;

            if (!_edit)
                EnableEditing();
        }

        private void CreateMaterials()
        {
            Material materialPrefab = FoxEditEditorSettings.Instance.VoxelEditorCubeMaterial.Asset;
            int paletteCount = VoxelSharedData.GetPaletteCount();
            _editorMaterials = new Material[paletteCount][];

            for (int paletteIndex = 0; paletteIndex < paletteCount; paletteIndex++)
            {
                VoxelPalette palette = VoxelSharedData.GetPalette(paletteIndex);
                int colorCount = palette.Colors.Length;
                _editorMaterials[paletteIndex] = new Material[colorCount];

                for (int colorIndex = 0; colorIndex < colorCount; colorIndex++)
                {
                    VoxelColor color = palette.Colors[colorIndex];
                    Material newMaterial = new Material(materialPrefab);
                    newMaterial.color = color.Color + color.Color * color.EmissiveIntensity;
                    newMaterial.SetFloat("_Smoothness", color.Smoothness);
                    newMaterial.SetFloat("_Metallic", color.Metallic);
                    _editorMaterials[paletteIndex][colorIndex] = newMaterial;
                }
            }
        }

        public Material GetMaterial(int paletteIndex, int colorIndex)
        {
            if (paletteIndex > _editorMaterials.Length)
                return null;

            if (colorIndex > _editorMaterials[paletteIndex].Length)
                return null;

            return _editorMaterials[paletteIndex][colorIndex];
        }
        #endregion

        private void EnableEditing()
        {
            if (_voxelRenderer == null)
                return;

            if (_animationList == null || SelectedFrameIndex >= _animationList.Count)
                SelectedFrameIndex = 0;

            VoxelObject voxelObject = _voxelRenderer.VoxelObject;
            if (voxelObject != null)
                PaletteIndex = voxelObject.PaletteIndex;
            else
                PaletteIndex = 0;

            if (_voxelParent != null)
            {
                GameObject.DestroyImmediate(_voxelParent.gameObject);
                _animationList.Clear();
            }

            string objectName = null;
            if (voxelObject == null)
                objectName = "New voxel object";
            else
                objectName = voxelObject.name;

            _voxelParent = new GameObject(string.Format("{0} Editor", objectName)).transform;
            _voxelParent.parent = _voxelRenderer.transform;
            _voxelParent.localPosition = Vector3.zero;
            _animationList = new List<VoxelEditorAnimation>();

            if (voxelObject != null)
            {
                for (int animation = 0; animation < voxelObject.AnimationIndices.Length; animation++)
                {
                    _animationList.Add(new VoxelEditorAnimation(voxelObject.AnimationIndices[animation].AnimName));
                    int startIndex = voxelObject.AnimationIndices[animation].StartIndex;
                    for (int i = startIndex; i < startIndex + voxelObject.AnimationIndices[animation].FrameCount; i++)
                    {
                        VoxelEditorFrame frame = new VoxelEditorFrame(_voxelParent, i - startIndex, _voxelPrefab, this);
                        frame.LoadFromSave(voxelObject.EditorVoxelPositions[i].VoxelPositions, PaletteIndex, voxelObject.EditorVoxelPositions[i].ColorIndices);
                        if (i - startIndex != _selectedFrameIndex)
                            frame.Hide();
                        _animationList[animation].AddFrame(frame);
                    }
                }
            }
            else
            {
                NewAnimation("Default");
                NewFrame();
            }

            _voxelRenderer.enabled = false;
            _edit = true;
        }

        public void UseTool(Vector3 worldPosition, Vector3 worldNormal)
        {
            Vector3Int gridPosition = CurrentFrame.WorldToGridPosition(worldPosition);
            Vector3Int direction = CurrentFrame.NormalToDirection(worldNormal);

            if (Action == vxAction.Paint)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = CurrentFrame.TryAddVoxelNextTo(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        IsDirty = CurrentFrame.TryAddLayer(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                }
            }
            else if (Action == vxAction.Erase)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = CurrentFrame.TryRemoveVoxel(gridPosition);
                        break;
                    case vxTool.Fill:
                        IsDirty = CurrentFrame.TryRemoveLayer(gridPosition, direction);
                        break;
                }
            }
            else if (Action == vxAction.Color)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = CurrentFrame.TryColorVoxel(gridPosition, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        IsDirty = CurrentFrame.TryFillColor(gridPosition, PaletteIndex, ColorIndex);
                        break;
                }
            }

            IsDirty = true;
        }

#region Animations
        public VoxelEditorAnimation GetVoxelEditorAnimation(int index)
        {
            return _animationList[index];
        }

        public void DeleteAnimation(int index)
        {
            foreach (VoxelEditorFrame voxelEditorFrame in _animationList[index].frames)
                GameObject.DestroyImmediate(voxelEditorFrame.FrameObject.gameObject);

            if (index <= SelectedAnimationIndex && SelectedAnimationIndex > 0)
                SelectedAnimationIndex--;
            else
                SelectedAnimationIndex = SelectedAnimationIndex;
            
            _animationList.RemoveAt(index);

            IsDirty = true;
        }

        public void NewAnimation(string animationName)
        {
            _animationList.Add(new VoxelEditorAnimation(animationName));
            VoxelEditorFrame newFrame = CurrentAnimation[SelectedFrameIndex].GetCopy(_animationList.Count, PaletteIndex);
            SelectedAnimationIndex = _animationList.Count - 1;
            CurrentAnimation.AddFrame(newFrame);

            ChangeFrame(CurrentAnimation.FramesCount - 1);

            IsDirty = true;
        }

        public List<string> GetAnimationNames()
        {
            return _animationList.Select(a => a.Name).ToList();
        }
#endregion
        #region Helpers

        public bool TryGetCubePosition(out Vector3 cubePosition, out Vector3 worldNormal, Ray ray)
        {
            cubePosition = Vector3.zero;
            worldNormal = Vector3.zero;
            if (HandleUtility.RaySnap(ray) is RaycastHit hit)
            {
                if (!IsCubeGameObject(hit.transform.gameObject))
                    return false;
                cubePosition = hit.transform.position;
                worldNormal = hit.normal;
                return true;
            }

            return false;
        }

        private bool IsCubeGameObject(GameObject gameObject)
        {
            return string.Compare("EditorVoxel", gameObject.name) == 0;
        }

        public VoxelEditorObject GetVoxelEditorObject(Vector3Int gridPosition)
        {
            if (CurrentFrame != null)
                return CurrentFrame.GetVoxelEditorObject(gridPosition);
            return null;
        }
        #endregion

        #region Frames
        public void NewFrame()
        {
            VoxelEditorFrame newFrame = new VoxelEditorFrame(_voxelParent, _animationList.Count, _voxelPrefab, this);
            newFrame.TryAddVoxelNextTo(Vector3Int.zero, Vector3Int.zero, PaletteIndex, 0);
            CurrentAnimation.AddFrame(newFrame);
            ChangeFrame(CurrentAnimation.FramesCount - 1);

            IsDirty = true;
        }


        private void OnPaletteChanged(int paletteColor)
        {
            UpdateColors();
        }

        public void DeleteFrame()
        {
            CurrentAnimation[SelectedFrameIndex].Destroy();
            CurrentAnimation.RemoveFrameAt(SelectedFrameIndex);

            if (SelectedFrameIndex > 0)
                SelectedFrameIndex -= 1;
            else
                SelectedFrameIndex = 0;
            CurrentAnimation[SelectedFrameIndex].Show();

            IsDirty = true;
        }

        public void DuplicateFrame()
        {
            VoxelEditorFrame newFrame = CurrentAnimation[SelectedFrameIndex].GetCopy(_animationList.Count, PaletteIndex);
            CurrentAnimation.AddFrame(newFrame);

            ChangeFrame(_animationList.Count - 1);

            IsDirty = true;
        }

        public void ChangeFrame(int index)
        {
            if (CurrentAnimation.FramesCount == 0)
                return;
            index = Mathf.Clamp(index, 0, CurrentAnimation.FramesCount - 1);
            if (lastVisibleEditorFrame != null)
                lastVisibleEditorFrame.Hide();
            SelectedFrameIndex = index;
            lastVisibleEditorFrame = CurrentAnimation[SelectedFrameIndex];
            lastVisibleEditorFrame.Show();
            UpdateColors();
        }

        public void MoveFrame(int oldIndex, int newIndex)
        {
            VoxelEditorFrame movedFrame = CurrentAnimation.Move(oldIndex, newIndex);
            for (int i = 0; i < _animationList.Count; i++)
                CurrentAnimation[i].FrameObject.name = string.Format("Frame {0}", i);
            movedFrame.FrameObject.SetSiblingIndex(newIndex);
            if (oldIndex == SelectedFrameIndex)
                SelectedFrameIndex = _animationList.IndexOf(CurrentAnimation);
        }

        #endregion

        #region Frames Thumbnails
        #endregion
        private void UpdateColors()
        {
            if (CurrentAnimation == null || SelectedFrameIndex < 0 || SelectedFrameIndex >= CurrentAnimation.FramesCount)
                return;
            CurrentAnimation[SelectedFrameIndex].UpdatePalette(PaletteIndex);
        }

        private void DisableEditing(bool isFromReload)
        {
            _edit = false;
            DestroyEditorFrame(isFromReload);

            if (wasVoxelRendererStatic)
                _voxelRenderer.SetStaticRender();
        }

        private void DestroyEditorFrame(bool isFromReload)
        {
            if (_voxelParent != null)
            {
                GameObject.DestroyImmediate(_voxelParent.gameObject);
                _voxelParent = null;
                _animationList.Clear();
            }

            if (_voxelRenderer != null && !isFromReload)
            {
                _voxelRenderer.enabled = true;
                _voxelRenderer.Refresh();
            }
        }

        public void Dispose()
        {
            DisableEditing(false);
            VoxelEditor.OnChangePalette -= OnPaletteChanged;
        }

        public void Save(string savePath)
        {
            string directory = Path.GetDirectoryName(savePath);
            string meshName = Path.GetFileNameWithoutExtension(savePath);
            VoxelSaveSystem.Save(meshName, directory, _voxelRenderer, CurrentPalette, PaletteIndex, _animationList, _computeStaticMesh);
            IsDirty = false;
        }
    }

}