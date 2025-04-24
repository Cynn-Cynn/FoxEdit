using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Autodesk.Fbx;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace FoxEdit
{
    internal class FoxEditWindow : EditorWindow
    {
        //Flags
        private static bool _isOpen = false;
        private bool _edit = false;
        private bool _canClick = true;
        private bool _needToSave = false;

        //Mesh
        private string _meshName = null;
        private string _saveDirectory = "Meshes";
        private VoxelRenderer _voxelRenderer = null;
        private VoxelSharedData _sharedData = null;

        //Edit
        private int _selectedAction = 0;
        private string[] _actions = { "Paint", "Erase", "Color" };

        private int _selectedPalette = 0;
        private string[] _paletteNames = null;

        private int _selectedColor = 0;
        private VoxelPalette _palette = null;
        private Color[] _colors = null;

        private int _selectedFrame = 0;
        private string[] _frameIndices = null;

        //Scene editor voxels
        private Transform _voxelParent = null;
        private List<VoxelFrame> _frameList;
        private VoxelFrame _voxelFramePrefab;

        //Save
        private ComputeShader _computeStaticMesh = null;

        #region Initialization

        [MenuItem("FoxEdit/Voxel editor", false, 0)]
        public static void ShowExample()
        {
            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.titleContent = new GUIContent("FoxEdit");
        }

        private void OnEnable()
        {
            LoadSharedData();
            string structurePath = AssetDatabase.GUIDToAssetPath("9838c5c1efb562c4ab6018d5b890c7fb");
            _voxelFramePrefab = AssetDatabase.LoadAssetAtPath(structurePath, typeof(VoxelFrame)) as VoxelFrame;

            string computeShaderPath = AssetDatabase.GUIDToAssetPath("2cb62f122b08a144ba5d96639b73bd19");
            _computeStaticMesh = AssetDatabase.LoadAssetAtPath(computeShaderPath, typeof(ComputeShader)) as ComputeShader;
            _frameList = new List<VoxelFrame>();

            _isOpen = true;

            if (!_edit)
                EnableEditing();

            if (_edit)
                DisableEditing();
        }

        private void OnDisable()
        {
            _isOpen = false;
        }

        private void OnBecameVisible()
        {
            if (!_edit)
                EnableEditing();
        }

        private void OnBecameInvisible()
        {
            if (_edit)
                DisableEditing();
        }

        #endregion Initialization

        private void OnGUI()
        {
            EditButton();

            if (!_edit)
                return;

            SharedDataDisplay();
            ObjectDisplay();
            JumpLine();
            EditDisplay();
            JumpLine();
            SaveDisplay();
        }

        private void JumpLine()
        {
            EditorGUILayout.LabelField("");
        }

        private void EditButton()
        {
            Color baseColor = GUI.backgroundColor;
            GUI.backgroundColor = _edit ? Color.red : Color.green;
            if (GUILayout.Button(_edit ? "Stop editing" : "Start editing"))
            {
                _edit = !_edit;
                if (_edit)
                    EnableEditing();
                else
                    DisableEditing();
            }
            GUI.backgroundColor = baseColor;
        }

        #region Shortcuts

        [MenuItem("FoxEdit/Paint &1", false, 1)]
        private static void SelectPaint()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectAction(0);
        }

        [MenuItem("FoxEdit/Erase &2", false, 2)]
        private static void SelectEraseShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectAction(1);
        }

        [MenuItem("FoxEdit/Color &3", false, 3)]
        private static void SelectColorShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectAction(2);
        }

        private void SelectAction(int index)
        {
            _selectedAction = index;
        }

        [MenuItem("FoxEdit/New Frame &N", false, 4)]
        private static void NewFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.NewFrame();
        }

        [MenuItem("FoxEdit/Duplicate Frame &D", false, 5)]
        private static void DuplicateFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.DuplicateFrame();
        }

        [MenuItem("FoxEdit/Delete Frame &X", false, 6)]
        private static void DeleteFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.DeleteFrame();
        }

        #endregion Shortcuts

        #region Data

        private void SharedDataDisplay()
        {
            if (_sharedData == null)
            {
                EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Shared data not loaded");
                if (GUILayout.Button("Load shared data"))
                    LoadSharedData();
                JumpLine();
            }
        }

        private void LoadSharedData()
        {
            _sharedData = FindObjectOfType<VoxelSharedData>();
            if (_sharedData == null)
                return;

            _paletteNames = _sharedData.GetPaletteNames();
            if (_voxelRenderer == null)
                _selectedPalette = 0;
            else
                _selectedPalette = _voxelRenderer.VoxelObject.PaletteIndex;

            LoadColors();
        }

        private void LoadColors()
        {
            _palette = _sharedData.GetPalette(_selectedPalette);
            _colors = _palette.Colors.Select(color => color.Color).ToArray();

            _selectedColor = 0;
        }

        #endregion Data

        #region Object

        private void ObjectDisplay()
        {
            JumpLine();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh name", EditorStyles.boldLabel);
            _meshName = EditorGUILayout.TextField(_meshName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh Renderer", EditorStyles.boldLabel);
            VoxelRenderer voxelRenderer = EditorGUILayout.ObjectField(_voxelRenderer, typeof(VoxelRenderer), true) as VoxelRenderer;
            EditorGUILayout.EndHorizontal();

            if (voxelRenderer != _voxelRenderer)
            {
                _voxelRenderer = voxelRenderer;
                if (_voxelRenderer != null)
                {
                    if (_voxelRenderer.VoxelObject != null)
                        LoadObject();
                    else
                        CreateNewObject();
                }
            }
        }

        private void LoadObject()
        {
            _meshName = _voxelRenderer.VoxelObject.name;
            _saveDirectory = ExtractDirectoryFromPath(AssetDatabase.GetAssetPath(_voxelRenderer.VoxelObject));
            EnableEditing();
        }

        private void CreateNewObject()
        {
            _meshName = "NewVoxelObject";
            EnableEditing();
        }

        private void EnableEditing()
        {
            if (_voxelRenderer == null)
                return;

            if (_selectedFrame >= _frameList.Count)
                _selectedFrame = 0;

            VoxelObject voxelObject = _voxelRenderer.VoxelObject;
            if (voxelObject != null)
                _selectedPalette = voxelObject.PaletteIndex;
            else
                _selectedPalette = 0;
            LoadColors();

            if (_voxelParent != null)
            {
                DestroyImmediate(_voxelParent.gameObject);
                _frameList.Clear();
            }

            _voxelParent = new GameObject($"{_meshName}Editor").transform;
            _voxelParent.parent = _voxelRenderer.transform;
            _voxelParent.localPosition = Vector3.zero;

            if (voxelObject != null)
            {
                for (int i = 0; i < voxelObject.EditorVoxelPositions.Length; i++)
                {
                    VoxelFrame frame = Instantiate(_voxelFramePrefab, _voxelParent);
                    frame.LoadFromMesh(voxelObject.EditorVoxelPositions[i].VoxelPositions, _selectedPalette, voxelObject.EditorVoxelPositions[i].ColorIndices);
                    if (i != _selectedFrame)
                        frame.gameObject.SetActive(false);
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
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void DisableEditing()
        {
            if (_needToSave)
            {
                ConfirmWindow window = CreateWindow<ConfirmWindow>();
                window.titleContent = new GUIContent("Confirm");
                window.ShowModalUtility();
                _needToSave = false;
            }

            DestroyImmediate(_voxelParent.gameObject);
            _voxelParent = null;
            _frameList.Clear();

            _voxelRenderer.enabled = true;
            _voxelRenderer.Refresh();
            _edit = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private string ExtractDirectoryFromPath(string path)
        {
            if (path == null || path == "")
                return null;

            int firstSlash = path.IndexOf('/');
            int lastSlash = path.LastIndexOf('/');

            return path.Substring(firstSlash + 1, lastSlash - firstSlash - 1);
        }

        private void CreateFrameIndices(int frameCount)
        {
            _frameIndices = new string[frameCount];
            for (int i = 0; i < _frameIndices.Length; i++)
            {
                _frameIndices[i] = $"{i}";
            }
        }

        #endregion Object

        #region EditingDisplay

        private void EditDisplay()
        {
            FrameSelection();

            if (_sharedData != null)
            {
                ActionSelection();
                PaletteSelection();
                ColorSelection();
            }
        }

        private void FrameSelection()
        {
            if (_voxelRenderer == null)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Frame", EditorStyles.boldLabel);
            int selectedFrame = EditorGUILayout.Popup(_selectedFrame, _frameIndices);
            EditorGUILayout.EndHorizontal();

            if (selectedFrame != _selectedFrame)
                ChangeFrame(selectedFrame);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Frame (Alt + N)"))
                NewFrame();

            if (_frameList.Count > 0 && GUILayout.Button("Duplicate Frame (Alt + D)"))
                DuplicateFrame();

            if (_frameList.Count > 0 && GUILayout.Button("Delete Frame (Alt + X)"))
                DeleteFrame();
            EditorGUILayout.EndHorizontal();

            JumpLine();
        }

        private void NewFrame()
        {
            _frameList.Add(Instantiate(_voxelFramePrefab, _voxelParent));
            _frameList[_selectedFrame].Initialize(_selectedPalette);
            if (_frameList.Count != 0)
                ChangeFrame(_frameList.Count - 1);
            CreateFrameIndices(_frameList.Count);
            _needToSave = true;
        }

        private void DuplicateFrame()
        {
            _frameList.Add(Instantiate(_frameList[_selectedFrame], _voxelParent));
            _frameList[_selectedFrame].Initialize(_selectedPalette);
            ChangeFrame(_frameList.Count - 1);
            CreateFrameIndices(_frameList.Count);
            _needToSave = true;
        }

        private void DeleteFrame()
        {
            DestroyImmediate(_frameList[_selectedFrame].gameObject);
            _frameList.RemoveAt(_selectedFrame);
            _selectedFrame -= 1;
            _frameList[_selectedFrame].Show();
            CreateFrameIndices(_frameList.Count);
            _needToSave = true;
        }

        private void ChangeFrame(int index)
        {
            _frameList[_selectedFrame].Hide();
            _selectedFrame = index;
            _frameList[_selectedFrame].Show();
        }

        private void ActionSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actions (Alt + 1/2/3)", EditorStyles.boldLabel);
            _selectedAction = EditorGUILayout.Popup(_selectedAction, _actions);
            EditorGUILayout.EndHorizontal();
        }

        private void PaletteSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Palettes", EditorStyles.boldLabel);
            int selectPalette = EditorGUILayout.Popup(_selectedPalette, _paletteNames);
            EditorGUILayout.EndHorizontal();
            if (selectPalette != _selectedPalette)
            {
                _selectedPalette = selectPalette;
                LoadColors();
            }
        }

        private void ColorSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.ColorField(_colors[_selectedColor]);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            Color baseColor = GUI.backgroundColor;

            for (int i = 0; i < _colors.Length; i++)
            {
                GUI.backgroundColor = _colors[i] * 3f;
                if (GUILayout.Button(""))
                    _selectedColor = i;
            }

            GUI.backgroundColor = baseColor;
        }

        private void SaveDisplay()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh path", EditorStyles.boldLabel);
            _saveDirectory = EditorGUILayout.TextField(_saveDirectory);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Save"))
            {
                Save();
            }
        }

        #endregion EditingDisplay

        #region EditingActions

        private void OnSceneGUI(SceneView sceneView)
        {
            if (Application.isPlaying)
                return;

            MouseManagement();
        }

        private void MouseManagement()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Event.current.button == 1 && _canClick)
                Click();
            if (!_canClick && Event.current.button == 1 && Event.current.type == EventType.MouseUp)
                _canClick = true;
#else
        if (Mouse.current.rightButton.IsPressed() && _canClick)
            Click();
        if (!_canClick && !Mouse.current.rightButton.IsPressed())
            _canClick = true;
#endif
        }

        private void Click()
        {
            if (_voxelFramePrefab == null)
                return;

            _canClick = false;

            Ray mouseRay = GetMouseRay();
            Vector3 worldPosition = Vector3.zero;
            Vector3 worldNormal = Vector3.zero;

            if (TryGetCubePosition(out worldPosition, out worldNormal, mouseRay))
            {
                VoxelFrame currentFrame = _frameList[_selectedFrame];
                Vector3Int gridPosition = currentFrame.WorldToGridPosition(worldPosition);
                Vector3Int direction = currentFrame.NormalToDirection(worldNormal);
                if (_selectedAction == 0)
                    currentFrame.TryAddCubeNextTo(gridPosition, direction, _selectedPalette, _selectedColor);
                else if (_selectedAction == 1)
                    currentFrame.TryRemoveCube(gridPosition);
                else if (_selectedAction == 2)
                    currentFrame.TryColorCube(gridPosition, _selectedPalette, _selectedColor);

                _needToSave = true;
            }
        }

        private Ray GetMouseRay()
        {
            Vector2 mouseInput = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            mouseInput = Event.current.mousePosition;
#else
        mouseInput = Mouse.current.position.value;   
#endif
            return HandleUtility.GUIPointToWorldRay(mouseInput);
        }

        private bool TryGetCubePosition(out Vector3 cubePosition, out Vector3 worldNormal, Ray ray)
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

        public void Save()
        {
            VoxelSaveSystem.Save(_meshName, _saveDirectory, _voxelRenderer, _palette, _selectedPalette, _frameList, _computeStaticMesh);
            _needToSave = false;
        }

        #endregion EditingActions
    }
}