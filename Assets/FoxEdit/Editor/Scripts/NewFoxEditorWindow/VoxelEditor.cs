
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FoxEdit.VoxelTools;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace FoxEdit
{
    internal class VoxelEditor : IDisposable
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
        public event Action<int, Texture2D> OnFramesThumbnailsUpdated;
        public List<Texture2D> FramesThumbnails {get; private set;}

        //Flags
        private bool _edit = false;
        public bool IsDirty {get; private set;} = false;

        //Mesh
        private VoxelRenderer _voxelRenderer = null;
        private Material[][] _editorMaterials = null;

        public event Action<int> OnFrameIndexChanged;
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
                if (_frameList == null || _frameList.Count == 0 || SelectedFrameIndex < 0 || SelectedFrameIndex >= _frameList.Count)
                    return null;
                return _frameList[SelectedFrameIndex];
            }
        }

        //Scene editor voxels
        private MeshRenderer _voxelPrefab = null;
        private Transform _voxelParent = null;
        private List<VoxelEditorFrame> _frameList;

        //Save
        private ComputeShader _computeStaticMesh = null;

        #region Init
        public VoxelEditor(VoxelRenderer voxelRenderer)
        {
            _voxelRenderer = voxelRenderer;
            _computeStaticMesh = FoxEditSettings.GetSettings().staticShader;
            _frameList = new List<VoxelEditorFrame>();
            _voxelPrefab = FoxEditEditorSettings.Instance.VoxelPrefab.Asset;
            VoxelEditor.OnChangePalette += OnPaletteChanged;

            CreateMaterials(); ;

            if (!_edit)
                EnableEditing();
            EditorApplication.delayCall += SetFramesVoxel;
        }

        private void SetFramesVoxel()
        {
            FramesThumbnails = GetFramesVoxels();
            for (int i = 0; i < FramesThumbnails.Count; i++)
            {
                int tmp_i = i;
                OnFramesThumbnailsUpdated?.Invoke(tmp_i, FramesThumbnails[i]);
            }
        }

        private List<Texture2D> GetFramesVoxels()
        {
            List<Texture2D> frameList = new List<Texture2D>();
            List<GameObject> framesGOList = _frameList.Select(f => f.FrameObject.gameObject).ToList();

            return ThumbnailsTaker.GetThumbnails(framesGOList);
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

            if (_frameList == null || SelectedFrameIndex >= _frameList.Count)
                SelectedFrameIndex = 0;

            VoxelObject voxelObject = _voxelRenderer.VoxelObject;
            if (voxelObject != null)
                PaletteIndex = voxelObject.PaletteIndex;
            else
                PaletteIndex = 0;

            if (_voxelParent != null)
            {
                GameObject.DestroyImmediate(_voxelParent.gameObject);
                _frameList.Clear();
            }

            _voxelParent = new GameObject(string.Format("{0} Editor", voxelObject.name)).transform;
            _voxelParent.parent = _voxelRenderer.transform;
            _voxelParent.localPosition = Vector3.zero;

            if (voxelObject != null)
            {
                for (int i = 0; i < voxelObject.EditorVoxelPositions.Length; i++)
                {
                    VoxelEditorFrame frame = new VoxelEditorFrame(_voxelParent, i, _voxelPrefab, this);
                    frame.LoadFromSave(voxelObject.EditorVoxelPositions[i].VoxelPositions, PaletteIndex, voxelObject.EditorVoxelPositions[i].ColorIndices);
                    if (i != SelectedFrameIndex)
                        frame.Hide();
                    frame.FrameObject.name = string.Format("Frame {0}", i);
                    _frameList.Add(frame);
                }
            }
            else
            {
                NewFrame();
            }

            _voxelRenderer.enabled = false;
            _edit = true;
        }

        #region Dispose

        #endregion
        public void UseTool(Vector3 worldPosition, Vector3 worldNormal)
        {
            VoxelEditorFrame currentFrame = _frameList[SelectedFrameIndex];
            Vector3Int gridPosition = currentFrame.WorldToGridPosition(worldPosition);
            Vector3Int direction = currentFrame.NormalToDirection(worldNormal);

            if (Action == vxAction.Paint)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = currentFrame.TryAddVoxelNextTo(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        IsDirty = currentFrame.TryAddLayer(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                }
            }
            else if (Action == vxAction.Erase)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = currentFrame.TryRemoveVoxel(gridPosition);
                        break;
                    case vxTool.Fill:
                        IsDirty = currentFrame.TryRemoveLayer(gridPosition, direction);
                        break;
                }
            }
            else if (Action == vxAction.Color)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        IsDirty = currentFrame.TryColorVoxel(gridPosition, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        IsDirty = currentFrame.TryFillColor(gridPosition, PaletteIndex, ColorIndex);
                        break;
                }
            }

            IsDirty = true;
        }

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
            VoxelEditorFrame newFrame = new VoxelEditorFrame(_voxelParent, _frameList.Count, _voxelPrefab, this);
            newFrame.TryAddVoxelNextTo(Vector3Int.zero, Vector3Int.zero, PaletteIndex, 0);
            _frameList.Add(newFrame);

            if (_frameList.Count != 1)
                ChangeFrame(_frameList.Count - 1);

            IsDirty = true;
        }

        private void OnPaletteChanged(int paletteColor)
        {
            UpdateColors();
        }

        public void DeleteFrame()
        {
            _frameList[SelectedFrameIndex].Destroy();
            _frameList.RemoveAt(SelectedFrameIndex);
            FramesThumbnails.RemoveAt(SelectedFrameIndex);

            if (SelectedFrameIndex > 0)
                SelectedFrameIndex -= 1;
            else
                SelectedFrameIndex = 0;
            _frameList[SelectedFrameIndex].Show();

            IsDirty = true;
        }

        public void DuplicateFrame()
        {
            VoxelEditorFrame newFrame = _frameList[SelectedFrameIndex].GetCopy(_frameList.Count, PaletteIndex);
            FramesThumbnails.Add(FramesThumbnails[SelectedFrameIndex]);
            _frameList.Add(newFrame);

            ChangeFrame(_frameList.Count - 1);
            UpdateFrameThumbnail(SelectedFrameIndex);

            IsDirty = true;
        }

        public void ChangeFrame(int index)
        {
            index = Mathf.Clamp(index, 0, _frameList.Count - 1);
            UpdateFrameThumbnail(SelectedFrameIndex);
            if (SelectedFrameIndex >= 0 || SelectedFrameIndex < _frameList.Count)
                _frameList[SelectedFrameIndex].Hide();
            SelectedFrameIndex = index;
            _frameList[SelectedFrameIndex].Show();
            UpdateColors();
        }

        public void MoveFrame(int oldIndex, int newIndex)
        {
            VoxelEditorFrame movedFrame = _frameList.Move(oldIndex, newIndex);
            for (int i = 0; i < _frameList.Count; i++)
                _frameList[i].FrameObject.name = string.Format("Frame {0}", i);
            movedFrame.FrameObject.SetSiblingIndex(newIndex);
            if (oldIndex == SelectedFrameIndex)
                SelectedFrameIndex = _frameList.IndexOf(movedFrame);
            FramesThumbnails.Move(oldIndex, newIndex);
        }

        #endregion

        #region Frames Thumbnails
        private void UpdateFrameThumbnail(int index)
        {
            if (FramesThumbnails == null)
                return;
            if (index >= 0 && index < FramesThumbnails.Count)
            {
                int tmp_index = index;
                if (FramesThumbnails[index] != null)
                    GameObject.DestroyImmediate(FramesThumbnails[index]);
                VoxelEditorFrame voxelEditorFrame = _frameList[index];
                FramesThumbnails[index] = ThumbnailsTaker.GetThumbnail(voxelEditorFrame.FrameObject.gameObject);
                OnFramesThumbnailsUpdated?.Invoke(index, FramesThumbnails[index]);
            }
        }

        public Texture2D GetFrameThumbnail(int index)
        {
            if (index < 0 || index >= FramesThumbnails.Count)
                return null;
            return FramesThumbnails[index];
        }
        #endregion
        private void UpdateColors()
        {
            if (_frameList == null || SelectedFrameIndex < 0 || SelectedFrameIndex >= _frameList.Count)
                return;
            _frameList[SelectedFrameIndex].UpdatePalette(PaletteIndex);
        }

        private void DisableEditing(bool isFromReload)
        {
            _edit = false;
            DestroyEditorFrame(isFromReload);
        }

        private void DestroyEditorFrame(bool isFromReload)
        {
            if (_voxelParent != null)
            {
                GameObject.DestroyImmediate(_voxelParent.gameObject);
                _voxelParent = null;
                _frameList.Clear();
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
            VoxelSaveSystem.Save(savePath, _voxelRenderer, CurrentPalette, PaletteIndex, _frameList, _computeStaticMesh);
            IsDirty = false;
        }
    }

}