using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Autodesk.Fbx;
using System.Reflection;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace FoxEdit
{
    [InitializeOnLoad]
    internal class FoxEditWindow : EditorWindow
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
        private int _selectedTool = 0;
        private string[] _tools = { "Brush", "Fill" };

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
        private MeshRenderer _voxelPrefab = null;
        private Transform _voxelParent = null;
        private List<VoxelEditorFrame> _frameList;
        private Transform _selectedVoxel = null;

        //Save
        private ComputeShader _computeStaticMesh = null;


        #region Initialization

        static FoxEditWindow()
        {
            VoxelSharedData.Initialize();
        }

        [MenuItem("FoxEdit/Voxel editor", false, 0)]
        public static void ShowExample()
        {
            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.titleContent = new GUIContent("FoxEdit");
        }

        void OnSelection()
        {
            GameObject voxel = Selection.gameObjects.ToList().Find(gameObject => gameObject.name == "EditorVoxel");
            if (voxel == null)
            {
                _selection = false;
                _drag = false;
                _selectedVoxel = null;
                return;
            }

            using (EditorGUI.DisabledGroupScope scope = new EditorGUI.DisabledGroupScope(Selection.activeGameObject == null))
            {
                Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
                EditorWindow window = GetWindow(type);
                MethodInfo exprec = type.GetMethod("SetExpandedRecursive");
                exprec!.Invoke(window, new object[] { voxel.transform.parent.parent.parent.GetInstanceID(), false });
                _selection = true;
                _selectedVoxel = voxel.transform;
            }
        }

        private void OnBecameVisible()
        {
            if (Application.isPlaying)
                return;

            Selection.selectionChanged += OnSelection;

            string voxelPrefabPath = AssetDatabase.GUIDToAssetPath("b372f3a77bc32ba418920cfa5cab2b28");
            _voxelPrefab = AssetDatabase.LoadAssetAtPath(voxelPrefabPath, typeof(MeshRenderer)) as MeshRenderer;

            string computeShaderPath = AssetDatabase.GUIDToAssetPath("2cb62f122b08a144ba5d96639b73bd19");
            _computeStaticMesh = AssetDatabase.LoadAssetAtPath(computeShaderPath, typeof(ComputeShader)) as ComputeShader;
            _frameList = new List<VoxelEditorFrame>();

            _isOpen = true;

            CreateMaterials(); ;

            if (!_edit)
                EnableEditing();
        }


        private void OnBecameInvisible()
        {
            Selection.selectionChanged -= OnSelection;

            if (_edit)
                DisableEditing(true);
        }

        private void OnDisable()
        {
            if (_edit)
                DisableEditing(true);
            Selection.selectionChanged -= OnSelection;
        }

        private void CreateMaterials()
        {
            string materialPath = AssetDatabase.GUIDToAssetPath("3ba88c2707cea7843b37c87a3a206258");
            Material materialPrefab = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
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

        #endregion Initialization

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Cannot edit in play mode", EditorStyles.boldLabel);
                return;
            }

            EditButton();

            if (!_edit)
                return;

            ObjectDisplay();
            JumpLine();
            EditDisplay();
            SelectionDisplay();
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
                    DisableEditing(false);
            }
            GUI.backgroundColor = baseColor;
        }

        #region Shortcuts

        [MenuItem("FoxEdit/Brush &R", false, 1)]
        private static void SelectBrushShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectTool(0);
        }

        [MenuItem("FoxEdit/Fill &F", false, 2)]
        private static void SelectFillShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectTool(1);
        }

        private void SelectTool(int index)
        {
            _selectedTool = index;
        }

        [MenuItem("FoxEdit/Paint &1", false, 3)]
        private static void SelectPaint()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectAction(0);
        }

        [MenuItem("FoxEdit/Erase &2", false, 4)]
        private static void SelectEraseShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.SelectAction(1);
        }

        [MenuItem("FoxEdit/Color &3", false, 5)]
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

        [MenuItem("FoxEdit/New Frame &N", false, 6)]
        private static void NewFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.NewFrame();
        }

        [MenuItem("FoxEdit/Duplicate Frame &D", false, 7)]
        private static void DuplicateFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.DuplicateFrame();
        }

        [MenuItem("FoxEdit/Delete Frame &X", false, 8)]
        private static void DeleteFrameShortcut()
        {
            if (!_isOpen)
                return;

            FoxEditWindow window = GetWindow<FoxEditWindow>();
            window.DeleteFrame();
        }

        #endregion Shortcuts

        #region Data

        private void LoadColors()
        {
            _palette = VoxelSharedData.GetPalette(_selectedPalette);
            _colors = _palette.Colors.Select(color => color.Color).ToArray();

            _selectedColor = 0;
        }

        #endregion Data

        #region Object

        private void ObjectDisplay()
        {
            JumpLine();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh Name", EditorStyles.boldLabel);
            _meshName = EditorGUILayout.TextField(_meshName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mesh Renderer", EditorStyles.boldLabel);
            VoxelRenderer voxelRenderer = EditorGUILayout.ObjectField(_voxelRenderer, typeof(VoxelRenderer), true) as VoxelRenderer;
            EditorGUILayout.EndHorizontal();

            if (voxelRenderer != _voxelRenderer)
            {
                if (voxelRenderer == null)
                    DestroyEditorFrame(false);

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
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void DisableEditing(bool isFromReload)
        {
            if (_needToSave)
            {
                ConfirmWindow window = CreateWindow<ConfirmWindow>();
                window.titleContent = new GUIContent("Confirm");
                window.ShowModalUtility();
                _needToSave = false;
            }

            DestroyEditorFrame(isFromReload);
            _edit = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void DestroyEditorFrame(bool isFromReload)
        {
            if (_voxelParent != null)
            {
                DestroyImmediate(_voxelParent.gameObject);
                _voxelParent = null;
                _frameList.Clear();
            }

            if (_voxelRenderer != null && !isFromReload)
            {
                _voxelRenderer.enabled = true;
                _voxelRenderer.Refresh();
            }
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

        private void SelectionDisplay()
        {
            if (!_selection)
                return;

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("Current transformation", EditorStyles.boldLabel);
            float angle = _selectedVoxel.eulerAngles.magnitude;
            angle = Mathf.Round(angle / 45.0f);
            angle *= 45.0f;
            EditorGUILayout.LabelField($"Rotation: {angle}");
            float scale = 1.0f;
            if (_selectedVoxel.localScale.x > 1)
                scale = Mathf.Round(_selectedVoxel.localScale.x);
            else
                scale = 1.0f / Mathf.Round(1.0f / _selectedVoxel.localScale.x);

            EditorGUILayout.LabelField($"Scale: {scale}");
            EditorGUILayout.LabelField("");
        }


        private void EditDisplay()
        {
            FrameSelection();
            ToolSelection();
            ActionSelection();
            PaletteSelection();
            ColorSelection();

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
            VoxelEditorFrame newFrame = new VoxelEditorFrame(_voxelParent, _frameList.Count, _voxelPrefab, this);
            newFrame.TryAddVoxelNextTo(Vector3Int.zero, Vector3Int.zero, _selectedPalette, 0);
            _frameList.Add(newFrame);

            if (_frameList.Count != 1)
                ChangeFrame(_frameList.Count - 1);

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

        private void DeleteFrame()
        {
            _frameList[_selectedFrame].Destroy();
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

        private void ToolSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tools (Alt + R/F)", EditorStyles.boldLabel);
            _selectedTool = EditorGUILayout.Popup(_selectedTool, _tools);
            EditorGUILayout.EndHorizontal();
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
            StopDragDetection();
        }

        private void StopDragDetection()
        {
            if (_selection)
                Repaint();

#if ENABLE_LEGACY_INPUT_MANAGER
            if (_selection && Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                if (!_drag)
                {
                    _drag = true;
                }
                else
                {
                    _frameList[_selectedFrame].ApplyVoxelTransform(_selectedPalette);
                    if (Selection.gameObjects.Length == 0)
                        _drag = false;
                    _needToSave = true;
                }
            }
#else
            if (_selection && !Mouse.current.leftButton.IsPressed())
            {
                if (!_drag)
                {
                    _drag = true;
                }
                else
                {
                    _frameList[_selectedFrame].AlignVoxels();
                    if (Selection.gameObjects.Length == 0)
                        _drag = false;
                    _needToSave = true;
                }
            }
#endif
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
            _canClick = false;

            Ray mouseRay = GetMouseRay();
            Vector3 worldPosition = Vector3.zero;
            Vector3 worldNormal = Vector3.zero;

            if (TryGetCubePosition(out worldPosition, out worldNormal, mouseRay))
            {
                VoxelEditorFrame currentFrame = _frameList[_selectedFrame];
                Vector3Int gridPosition = currentFrame.WorldToGridPosition(worldPosition);
                Vector3Int direction = currentFrame.NormalToDirection(worldNormal);

                if (_selectedAction == 0)
                {
                    if (_selectedTool == 0)
                        _needToSave = currentFrame.TryAddVoxelNextTo(gridPosition, direction, _selectedPalette, _selectedColor);
                    else if (_selectedTool == 1)
                        _needToSave = currentFrame.TryAddLayer(gridPosition, direction, _selectedPalette, _selectedColor);
                }
                else if (_selectedAction == 1)
                {
                    if (_selectedTool == 0)
                        _needToSave = currentFrame.TryRemoveVoxel(gridPosition);
                    else if (_selectedTool == 1)
                        _needToSave = currentFrame.TryRemoveLayer(gridPosition, direction);
                }
                else if (_selectedAction == 2)
                {
                    if (_selectedTool == 0)
                        _needToSave = currentFrame.TryColorVoxel(gridPosition, _selectedPalette, _selectedColor);
                    else if (_selectedTool == 1)
                        _needToSave = currentFrame.TryFillColor(gridPosition, _selectedPalette, _selectedColor);
                }
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