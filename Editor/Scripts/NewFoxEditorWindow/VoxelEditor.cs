
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FoxEdit.VoxelTools;
using System.Linq;
using System;

namespace FoxEdit
{
    internal class VoxelEditor : IDisposable
    {
        //Flags
        private static bool _isOpen = false;
        private bool _edit = false;
        private bool _canClick = true;
        private bool _needToSave = false;
        private bool _selection = false;
        private bool _drag = false;

        //Mesh
        private string _meshName = null;
        private string _saveDirectory = "Meshes";
        private VoxelRenderer _voxelRenderer = null;
        private Material[][] _editorMaterials = null;

        //Edit
        public vxTool SelectedTool = vxTool.Brush;
        public vxAction SelectedAction = vxAction.Paint;

        private int _selectedPalette = 0;
        private string[] _paletteNames = null;

        private int _selectedColor = 0;
        private VoxelPalette _palette = null;
        private Color[] _colors = null;

        private int _selectedFrame = 0;
        private string[] _frameIndices = null;

        //Scene editor voxels
        private MeshRenderer _voxelPrefab = null;
        private Transform _voxelParent = null;
        private List<VoxelEditorFrame> _frameList;
        private Transform _selectedVoxel = null;

        //Save
        private ComputeShader _computeStaticMesh = null;
        private FoxEditSettings _foxEditSettings = null;

        #region Init
        public VoxelEditor(vxTool tool, vxAction action, VoxelRenderer voxelRenderer)
        {
            _foxEditSettings = FoxEditSettings.GetSettings();
            _voxelRenderer = voxelRenderer;
            _voxelPrefab = _foxEditSettings.voxelPrefab;
            _computeStaticMesh = _foxEditSettings.staticVoxelComputeShader;
            _frameList = new List<VoxelEditorFrame>();

            _isOpen = true;

            CreateMaterials(); ;

            if (!_edit)
                EnableEditing();
        }

        ~VoxelEditor()
        {
            DisableEditing(false);
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
                _selectedPalette = voxelObject.PaletteIndex;
            else
                _selectedPalette = 0;
            LoadColors();

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
                    frame.LoadFromSave(voxelObject.EditorVoxelPositions[i].VoxelPositions, _selectedPalette, voxelObject.EditorVoxelPositions[i].ColorIndices);
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
        #region InputHandling
        private void Click()
        {
            _canClick = false;
            Debug.Log("click");

        }

        public void UseTool(Vector3 worldPosition, Vector3 worldNormal)
        {
            VoxelEditorFrame currentFrame = _frameList[_selectedFrame];
            Vector3Int gridPosition = currentFrame.WorldToGridPosition(worldPosition);
            Vector3Int direction = currentFrame.NormalToDirection(worldNormal);

            if (SelectedAction == vxAction.Paint)
            {
                switch (SelectedTool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryAddVoxelNextTo(gridPosition, direction, _selectedPalette, _selectedColor);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryAddLayer(gridPosition, direction, _selectedPalette, _selectedColor);
                        break;
                }
            }
            else if (SelectedAction == vxAction.Erase)
            {
                switch (SelectedTool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryRemoveVoxel(gridPosition);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryRemoveLayer(gridPosition, direction);
                        break;
                }
            }
            else if (SelectedAction == vxAction.Color)
            {
                switch (SelectedTool)
                {
                    case vxTool.Brush:
                        _needToSave = currentFrame.TryColorVoxel(gridPosition, _selectedPalette, _selectedColor);
                        break;
                    case vxTool.Fill:
                        _needToSave = currentFrame.TryFillColor(gridPosition, _selectedPalette, _selectedColor);
                        break;
                }
            }
        }
        #endregion


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
            newFrame.TryAddVoxelNextTo(Vector3Int.zero, Vector3Int.zero, _selectedPalette, 0);
            _frameList.Add(newFrame);

            if (_frameList.Count != 1)
                ChangeFrame(_frameList.Count - 1);

            CreateFrameIndices(_frameList.Count);

            _needToSave = true;
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
            VoxelEditorFrame newFrame = _frameList[_selectedFrame].GetCopy(_frameList.Count, _selectedPalette);
            _frameList.Add(newFrame);

            ChangeFrame(_frameList.Count - 1);
            CreateFrameIndices(_frameList.Count);

            _needToSave = true;
        }

        public void ChangeFrame(int index)
        {
            _frameList[_selectedFrame].Hide();
            _selectedFrame = index;
            _frameList[_selectedFrame].Show();
        }
        #endregion

        private void LoadColors()
        {
            _palette = VoxelSharedData.GetPalette(_selectedPalette);
            _paletteNames = VoxelSharedData.GetPaletteNames();
            _colors = _palette.Colors.Select(color => color.Color).ToArray();

            _selectedColor = 0;
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
        }
    }

}