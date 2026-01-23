
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
        public static event Action<int> OnFrameChanged;

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
                if (_colorIndex == value)
                    return;
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
        private List<Texture2D> _framesThumbnails;

        //Flags
        private bool _edit = false;
        private bool _canClick = true;
        private bool _needToSave = false;
        private bool _selection = false;

        //Mesh
        private string _meshName = null;
        private string _saveDirectory = "Meshes";
        private VoxelRenderer _voxelRenderer = null;
        private Material[][] _editorMaterials = null;

        private int _selectedFrame = 0;
        private string[] _frameIndices = null;

        //Scene editor voxels
        private MeshRenderer _voxelPrefab = null;
        private Transform _voxelParent = null;
        private List<VoxelEditorFrame> _frameList;

        //Save
        private ComputeShader _computeStaticMesh = null;
        private FoxEditSettings _foxEditSettings = null;


        #region Init
        public VoxelEditor(VoxelRenderer voxelRenderer)
        {
            _foxEditSettings = FoxEditSettings.GetSettings();
            _voxelRenderer = voxelRenderer;
            _voxelPrefab = _foxEditSettings.voxelPrefab;
            _computeStaticMesh = _foxEditSettings.staticVoxelComputeShader;
            _frameList = new List<VoxelEditorFrame>();
            VoxelEditor.OnChangePalette += OnPaletteChanged;

            CreateMaterials(); ;

            if (!_edit)
                EnableEditing();
            SetFramesVoxel();
        }

        private void SetFramesVoxel()
        {
            _framesThumbnails = GetFramesVoxels();
            for (int i = 0; i < _framesThumbnails.Count; i++)
            {
                int tmp_i = i;
                OnFramesThumbnailsUpdated?.Invoke(tmp_i, _framesThumbnails[i]);
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
            Material materialPrefab = _foxEditSettings.Materials.baseMaterial;
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

            if (_frameList == null || _selectedFrame >= _frameList.Count)
                _selectedFrame = 0;

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

            _voxelParent = new GameObject($"{_meshName}Editor").transform;
            _voxelParent.parent = _voxelRenderer.transform;
            _voxelParent.localPosition = Vector3.zero;

            if (voxelObject != null)
            {
                for (int i = 0; i < voxelObject.EditorVoxelPositions.Length; i++)
                {
                    VoxelEditorFrame frame = new VoxelEditorFrame(_voxelParent, i, _voxelPrefab, this);
                    frame.LoadFromSave(voxelObject.EditorVoxelPositions[i].VoxelPositions, PaletteIndex, voxelObject.EditorVoxelPositions[i].ColorIndices);
                    if (i != _selectedFrame)
                        frame.Hide();
                    _frameList.Add(frame);
                }
            }
            else
            {
                NewFrame();
            }

            CreateFrameIndices(_frameList.Count);

            _voxelRenderer.enabled = false;
            _edit = true;
        }

        #region Dispose

        #endregion
        public void UseTool(Vector3 worldPosition, Vector3 worldNormal)
        {
            VoxelEditorFrame currentFrame = _frameList[_selectedFrame];
            Vector3Int gridPosition = currentFrame.WorldToGridPosition(worldPosition);
            Vector3Int direction = currentFrame.NormalToDirection(worldNormal);

            if (Action == vxAction.Paint)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryAddVoxelNextTo(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryAddLayer(gridPosition, direction, PaletteIndex, ColorIndex);
                        break;
                }
            }
            else if (Action == vxAction.Erase)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryRemoveVoxel(gridPosition);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryRemoveLayer(gridPosition, direction);
                        break;
                }
            }
            else if (Action == vxAction.Color)
            {
                switch (Tool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryColorVoxel(gridPosition, PaletteIndex, ColorIndex);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryFillColor(gridPosition, PaletteIndex, ColorIndex);
                        break;
                }
            }
        }

        #region Helpers

        public bool TryGetCubePosition(out Vector3 cubePosition, out Vector3 worldNormal, Ray ray)
        {
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                cubePosition = hit.transform.position;
                worldNormal = hit.normal;
                return true;
            }

            cubePosition = Vector3.zero;
            worldNormal = Vector3.zero;
            return false;
        }
        #endregion

        #region Frames
        private void CreateFrameIndices(int frameCount)
        {
            _frameIndices = new string[frameCount];
            for (int i = 0; i < _frameIndices.Length; i++)
            {
                _frameIndices[i] = $"{i}";
            }
        }

        private void NewFrame()
        {
            VoxelEditorFrame newFrame = new VoxelEditorFrame(_voxelParent, _frameList.Count, _voxelPrefab, this);
            newFrame.TryAddVoxelNextTo(Vector3Int.zero, Vector3Int.zero, PaletteIndex, 0);
            _frameList.Add(newFrame);

            if (_frameList.Count != 1)
                ChangeFrame(_frameList.Count - 1);

            CreateFrameIndices(_frameList.Count);

            _needToSave = true;
        }

        private void OnPaletteChanged(int paletteColor)
        {
            UpdateColors();
        }

        private void DeleteFrame()
        {
            _frameList[_selectedFrame].Destroy();
            _frameList.RemoveAt(_selectedFrame);

            _selectedFrame -= 1;
            _frameList[_selectedFrame].Show();
            CreateFrameIndices(_frameList.Count);

            _needToSave = true;
        }

        private void DuplicateFrame()
        {
            VoxelEditorFrame newFrame = _frameList[_selectedFrame].GetCopy(_frameList.Count, PaletteIndex);
            _frameList.Add(newFrame);

            ChangeFrame(_frameList.Count - 1);
            CreateFrameIndices(_frameList.Count);

            _needToSave = true;
        }

        public void ChangeFrame(int index)
        {
            index = Mathf.Clamp(index, 0, _frameList.Count - 1);
            if (_selectedFrame >= 0 || _selectedFrame < _frameList.Count)
                _frameList[_selectedFrame].Hide();
            _selectedFrame = index;
            _frameList[_selectedFrame].Show();
            UpdateFrameThumbnail(index);
            UpdateColors();
        }

        #endregion

        #region Frames Thumbnails
        private void UpdateFrameThumbnail(int index)
        {
            if (_framesThumbnails == null)
                return;
            if (index >= 0 && index < _framesThumbnails.Count && _framesThumbnails[index] != null)
            {
                int tmp_index = index;
                GameObject.DestroyImmediate(_framesThumbnails[index]);
                VoxelEditorFrame voxelEditorFrame = _frameList[index];
                _framesThumbnails[index] = ThumbnailsTaker.GetThumbnail(voxelEditorFrame.FrameObject.gameObject);
                OnFramesThumbnailsUpdated?.Invoke(index, _framesThumbnails[index]);
            }
        }

        public List<Texture2D> GetFrameThumbnails() => new List<Texture2D>(_framesThumbnails);

        public Texture2D GetFrameThumbnail(int index)
        {
            if (index < 0 || index >= _framesThumbnails.Count)
                return null;
            return _framesThumbnails[index];
        }
        #endregion
        private void UpdateColors()
        {
            if (_frameList == null || _selectedFrame < 0 || _selectedFrame >= _frameList.Count)
                return;
            _frameList[_selectedFrame].UpdatePalette(PaletteIndex);
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
    }

}