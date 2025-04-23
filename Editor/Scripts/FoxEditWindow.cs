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

public class FoxEditWindow : EditorWindow
{
    private static bool _isOpen = false;
    private bool _edit = false;
    private bool _canClick = true;
    private bool _needToSave = false;

    private VoxelSharedData _data = null;

    //Mesh
    private string _meshName = null;
    private VoxelRenderer _voxelRenderer = null;
    private string _saveDirectory = "Meshes";

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

    //Editor voxels
    private Transform _voxelParent = null;
    private List<VoxelStructure> _frameList;
    private VoxelStructure _voxelFramePrefab;

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
        _voxelFramePrefab = AssetDatabase.LoadAssetAtPath(structurePath, typeof(VoxelStructure)) as VoxelStructure;

        string computeShaderPath = AssetDatabase.GUIDToAssetPath("2cb62f122b08a144ba5d96639b73bd19");
        _computeStaticMesh = AssetDatabase.LoadAssetAtPath(computeShaderPath, typeof(ComputeShader)) as ComputeShader;
        _frameList = new List<VoxelStructure>();

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
        EditorGUILayout.LabelField("");
        EditDisplay();
        EditorGUILayout.LabelField("");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mesh path", EditorStyles.boldLabel);
        _saveDirectory = EditorGUILayout.TextField(_saveDirectory);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Save"))
        {
            Save();
        }
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
        if (_data == null)
        {
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Shared data not loaded");
            if (GUILayout.Button("Load shared data"))
                LoadSharedData();
            EditorGUILayout.LabelField("");
        }
    }

    private void LoadSharedData()
    {
        _data = FindObjectOfType<VoxelSharedData>();
        if (_data == null)
            return;

        _paletteNames = _data.GetPaletteNames();
        if (_voxelRenderer == null)
            _selectedPalette = 0;
        else
            _selectedPalette = _voxelRenderer.VoxelObject.PaletteIndex;

        LoadColors();
    }

    private void LoadColors()
    {
        _palette = _data.GetPalette(_selectedPalette);
        _colors = _palette.Colors.Select(color => color.Color).ToArray();

        _selectedColor = 0;
    }

    #endregion Data

    #region Object

    private void ObjectDisplay()
    {
        EditorGUILayout.LabelField("");

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
                LoadObject();
        }
    }

    private void LoadObject()
    {
        _meshName = _voxelRenderer.VoxelObject.name;
        _saveDirectory = ExtractDirectoryFromPath(AssetDatabase.GetAssetPath(_voxelRenderer.VoxelObject));
        CreateFrameIndices(_voxelRenderer.VoxelObject.FrameCount);
        _selectedPalette = _voxelRenderer.VoxelObject.PaletteIndex;
        LoadColors();
        EnableEditing();
    }

    private void EnableEditing()
    {
        if (_voxelRenderer == null)
            return;

        if (_selectedFrame >= _frameList.Count)
            _selectedFrame = 0;

        VoxelObject voxelObject = _voxelRenderer.VoxelObject;
        _selectedPalette = voxelObject.PaletteIndex;
        LoadColors();

        if (_voxelParent != null)
        {
            DestroyImmediate(_voxelParent.gameObject);
            _frameList.Clear();
        }

        _voxelParent = new GameObject($"{_meshName}Editor").transform;
        _voxelParent.parent = _voxelRenderer.transform;
        _voxelParent.localPosition = Vector3.zero;

        for (int i = 0; i < voxelObject.EditorVoxelPositions.Length; i++)
        {
            VoxelStructure frame = Instantiate(_voxelFramePrefab, _voxelParent);
            frame.LoadFromMesh(voxelObject.EditorVoxelPositions[i].VoxelPositions, _selectedPalette, voxelObject.EditorVoxelPositions[i].ColorIndices);
            if (i != _selectedFrame)
                frame.gameObject.SetActive(false);
            _frameList.Add(frame);
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

    #region EditDisplay

    private void EditDisplay()
    {
        FrameSelection();

        if (_data != null)
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

        EditorGUILayout.LabelField("");
    }

    private void NewFrame()
    {
        _frameList.Add(Instantiate(_voxelFramePrefab, _voxelParent));
        if (_frameList.Count != 0)
            ChangeFrame(_frameList.Count - 1);
        _frameList[_selectedFrame].Initialize(_selectedPalette);
        CreateFrameIndices(_frameList.Count);
        _needToSave = true;
    }

    private void DuplicateFrame()
    {
        _frameList.Add(Instantiate(_frameList[_selectedFrame], _voxelParent));
        ChangeFrame(_frameList.Count - 1);
        _frameList[_selectedFrame].Initialize(_selectedPalette);
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

    #endregion EditDisplay

    #region Editing

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying)
            return;
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

        Vector2 mouseInput = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
        mouseInput = Event.current.mousePosition;
#else
        mouseInput = Mouse.current.position.value;   
#endif
        Ray ray = HandleUtility.GUIPointToWorldRay(mouseInput);

        Vector3 worldPosition = Vector3.zero;
        Vector3 worldNormal = Vector3.zero;

        if (TryGetCubePosition(out worldPosition, out worldNormal, ray))
        {
            VoxelStructure currentFrame = _frameList[_selectedFrame];
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

    #endregion Editing

    #region SaveSystem

    [Button("Create/Update Mesh")]
    public void Save()
    {
        string assetPath = null;
        if (_saveDirectory == null || _saveDirectory == "")
            assetPath = $"Assets/{_meshName}.asset";
        else
            assetPath = $"Assets/{_saveDirectory}/{_meshName}.asset";

        VoxelObject voxelObject = _voxelRenderer.VoxelObject;

        if (voxelObject == null)
        {
            voxelObject = ScriptableObject.CreateInstance<VoxelObject>();
            AssetDatabase.CreateAsset(voxelObject, assetPath);
        }

        Vector3Int[] minBounds = new Vector3Int[_frameList.Count];
        Vector3Int[] maxBounds = new Vector3Int[_frameList.Count];

        List<Vector4> positionsAndColorIndices = new List<Vector4>();

        int[] faceIndices = new int[_frameList.Count * 6];
        int[] frameFaceIndices = new int[6];
        int[] startIndices = new int[_frameList.Count];
        int[] instanceCounts = new int[_frameList.Count];
        int[] voxelIndices = new int[0];
        VoxelObject.EditorFrameVoxels[] editorVoxelPositions = new VoxelObject.EditorFrameVoxels[_frameList.Count];

        int startIndex = 0;

        voxelObject.VoxelIndices = new int[6];
        List<int>[] voxelIndicesByFace = CreateListArray(6);

        for (int frame = 0; frame < _frameList.Count; frame++)
        {
            Vector3Int min;
            Vector3Int max;
            bool[] isColorTransparent = _palette.Colors.Select(material => material.Color.a < 1.0f).ToArray();
            VoxelData[] voxelData = _frameList[frame].GetMeshData(out min, out max, isColorTransparent);
            editorVoxelPositions[frame].VoxelPositions = _frameList[frame].GetEditorVoxelPositions();
            editorVoxelPositions[frame].ColorIndices = _frameList[frame].GetEditorVoxelColorIndices();
            minBounds[frame] = min;
            maxBounds[frame] = max;

            int instanceCount = 0;

            for (int voxel = 0; voxel < voxelData.Length; voxel++)
            {
                VoxelData data = voxelData[voxel];

                int voxelIndex = StorePositonAndColorIndex(positionsAndColorIndices, data);
                int faceCount = StoreIndicesByFace(voxelIndicesByFace, frameFaceIndices, data.GetFaces(), voxelIndex);

                instanceCount += faceCount;
            }

            SortIndices(faceIndices, frameFaceIndices, ref voxelIndices, voxelIndicesByFace, frame);

            startIndices[frame] = startIndex;
            instanceCounts[frame] = instanceCount;
            startIndex += instanceCount;

            ClearVoxelIndicesByFace(voxelIndicesByFace);
        }

        voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
        voxelObject.PaletteIndex = _selectedPalette;

        voxelObject.VoxelPositions = positionsAndColorIndices.Select(voxelData => (Vector3)voxelData).ToArray();
        voxelObject.VoxelIndices = voxelIndices;
        voxelObject.FaceIndices = faceIndices;
        voxelObject.ColorIndices = positionsAndColorIndices.Select(voxelData => (int)voxelData.w).ToArray();

        voxelObject.FrameCount = _frameList.Count;
        voxelObject.InstanceCount = instanceCounts;
        voxelObject.MaxInstanceCount = instanceCounts.Max();
        voxelObject.InstanceStartIndices = startIndices;
        voxelObject.EditorVoxelPositions = editorVoxelPositions;

        string fbxPath = null;
        if (_saveDirectory == null || _saveDirectory == "")
            fbxPath = $"Assets/{_meshName}.asset";
        else
            fbxPath = $"Assets/{_saveDirectory}/{_meshName}.fbx";


        CreateFBX(fbxPath, voxelObject);
        AssetDatabase.Refresh();
        GameObject go = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
        voxelObject.StaticMesh = go.GetComponent<MeshFilter>().sharedMesh;

        EditorUtility.SetDirty(voxelObject);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = voxelObject;
    }

    private void CreateFBX(string fbxPath, VoxelObject voxelObject)
    {
        using (var fbxManager = FbxManager.Create())
        {
            FbxIOSettings fbxIOSettings = FbxIOSettings.Create(fbxManager, Globals.IOSROOT);

            fbxManager.SetIOSettings(fbxIOSettings);
            FbxExporter fbxExporter = FbxExporter.Create(fbxManager, "Exporter");
            int fileFormat = fbxManager.GetIOPluginRegistry().FindWriterIDByDescription("FBX ascii (*.fbx)");
            bool status = fbxExporter.Initialize(fbxPath, fileFormat, fbxIOSettings);

            if (!status)
            {
                Debug.LogError(string.Format("failed to initialize exporter, reason: {0}", fbxExporter.GetStatus().GetErrorString()));
                return;
            }

            FbxScene fbxScene = FbxScene.Create(fbxManager, "Voxel Scene");
            FbxDocumentInfo fbxSceneInfo = FbxDocumentInfo.Create(fbxManager, "Voxel Static Mesh");
            fbxSceneInfo.mTitle = $"{_meshName}";
            fbxSceneInfo.mAuthor = "FoxEdit";
            fbxScene.SetSceneInfo(fbxSceneInfo);

            FbxNode mesh = CreateStaticMesh(fbxManager, voxelObject);
            fbxScene.GetRootNode().AddChild(mesh);

            fbxExporter.Export(fbxScene);

            fbxScene.Destroy();
            fbxExporter.Destroy();
        }
    }

    private Bounds CreateBounds(Vector3Int[] minBounds, Vector3Int[] maxBounds)
    {
        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        for (int i = 0; i < minBounds.Length; i++)
        {
            min.x = GetMin(min.x, minBounds[i].x);
            min.y = GetMin(min.y, minBounds[i].y);
            min.z = GetMin(min.z, minBounds[i].z);

            max.x = GetMax(max.x, maxBounds[i].x);
            max.y = GetMax(max.y, maxBounds[i].y);
            max.z = GetMax(max.z, maxBounds[i].z);
        }

        Bounds bounds = new Bounds();
        bounds.center = (new Vector3(min.x + max.x + 1.0f, min.y + max.y + 1.0f, min.z + max.z + 1.0f) / 2.0f) * 0.1f;

        Vector3Int size = max - min;
        size.x = Mathf.Abs(size.x) + 1;
        size.y = Mathf.Abs(size.y) + 1;
        size.z = Mathf.Abs(size.z) + 1;

        bounds.extents = new Vector3((float)size.x / 2.0f, (float)size.y / 2.0f, (float)size.z / 2.0f) * 0.1f;

        return bounds;
    }

    private int GetMin(int a, int b)
    {
        return a < b ? a : b;
    }

    private int GetMax(int a, int b)
    {
        return a > b ? a : b;
    }

    private List<int>[] CreateListArray(int size)
    {
        List<int>[] listArray = new List<int>[size];
        for (int i = 0; i < 6; i++)
        {
            listArray[i] = new List<int>();
        }
        return listArray;
    }

    private void ClearVoxelIndicesByFace(List<int>[] voxelIndicesByFace)
    {
        for (int i = 0; i < 6; i++)
        {
            voxelIndicesByFace[i].Clear();
        }
    }

    private int StorePositonAndColorIndex(List<Vector4> voxelData, VoxelData data)
    {
        int index = 0;
        Vector4 positionAndColorIndex = new Vector4(data.Position.x, data.Position.y, data.Position.z, data.ColorIndex);

        if (!voxelData.Contains(positionAndColorIndex))
        {
            index = voxelData.Count;
            voxelData.Add(positionAndColorIndex);
        }
        else
        {
            index = voxelData.IndexOf(positionAndColorIndex);
        }

        return index;
    }

    private int StoreIndicesByFace(List<int>[] voxelIndicesByFace, int[] frameFaceIndices, int[] faces, int voxelIndex)
    {
        for (int index = 0; index < faces.Length; index++)
        {
            voxelIndicesByFace[faces[index]].Add(voxelIndex);
            frameFaceIndices[faces[index]] += 1;
        }

        return faces.Length;
    }

    private void SortIndices(int[] faceIndices, int[] frameFaceIndices, ref int[] voxelIndices, List<int>[] voxelIndicesByFace, int frameIndex)
    {
        int frameOffset = frameIndex * 6;

        for (int i = 0; i < 6; i++)
        {
            if (i != 0)
                frameFaceIndices[i] += faceIndices[i - 1 + frameOffset];
            faceIndices[i + frameOffset] = frameFaceIndices[i];
            frameFaceIndices[i] = 0;

            voxelIndices = voxelIndices.Concat(voxelIndicesByFace[i]).ToArray();
        }
    }

    private Matrix4x4[] GetRotationMatrices()
    {
        float halfPi = Mathf.PI / 2.0f;

        Matrix4x4[] rotationMatrices = new Matrix4x4[6];
        rotationMatrices[0] = GetRotationMatrixX(0);
        rotationMatrices[1] = GetRotationMatrixX(halfPi);
        rotationMatrices[2] = GetRotationMatrixX(halfPi * 2);
        rotationMatrices[3] = GetRotationMatrixX(-halfPi);
        rotationMatrices[4] = GetRotationMatrixZ(halfPi);
        rotationMatrices[5] = GetRotationMatrixZ(-halfPi);

        return rotationMatrices;
    }

    private Matrix4x4 GetRotationMatrixX(float angle)
    {
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);

        return new Matrix4x4
        (
            new Vector4(1, 0, 0, 0),
            new Vector4(0, c, -s, 0),
            new Vector4(0, s, c, 0),
            new Vector4(0, 0, 0, 1)
        );
    }

    private Matrix4x4 GetRotationMatrixZ(float angle)
    {
        float c = Mathf.Cos(angle);
        float s = Mathf.Sin(angle);

        return new Matrix4x4
        (
            new Vector4(c, -s, 0, 0),
            new Vector4(s, c, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 1)
        );
    }

    private void ComputeVerticesPositionsAndNormals(out Vector3[] positions, out Vector3[] normals, VoxelObject voxelObject)
    {
        int kernel = _computeStaticMesh.FindKernel("VoxelGeneration");

        //Voxel
        GraphicsBuffer voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelPositions.Length, sizeof(float) * 3);
        voxelPositionBuffer.SetData(voxelObject.VoxelPositions);
        _computeStaticMesh.SetBuffer(kernel, "_VoxelPositions", voxelPositionBuffer);

        GraphicsBuffer faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.FaceIndices.Length, sizeof(int));
        faceIndicesBuffer.SetData(voxelObject.FaceIndices);
        _computeStaticMesh.SetBuffer(kernel, "_FaceIndices", faceIndicesBuffer);

        GraphicsBuffer voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.VoxelIndices.Length, sizeof(int));
        voxelIndicesBuffer.SetData(voxelObject.VoxelIndices);
        _computeStaticMesh.SetBuffer(kernel, "_VoxelIndices", voxelIndicesBuffer);

        Matrix4x4[] rotationMatrices = GetRotationMatrices();
        GraphicsBuffer rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
        rotationMatricesBuffer.SetData(rotationMatrices);
        _computeStaticMesh.SetBuffer(kernel, "_RotationMatrices", rotationMatricesBuffer);

        GraphicsBuffer positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0] * 4, sizeof(float) * 3);
        _computeStaticMesh.SetBuffer(kernel, "_VertexPosition", positionsBuffer);

        GraphicsBuffer normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, voxelObject.InstanceCount[0], sizeof(float) * 3);
        _computeStaticMesh.SetBuffer(kernel, "_VertexNormals", normalsBuffer);

        int instanceCount = voxelObject.InstanceCount[0];
        _computeStaticMesh.SetInt("_InstanceCount", instanceCount);

        uint threadGroupSize = 0;
        _computeStaticMesh.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);
        int threadGroups = Mathf.CeilToInt((float)instanceCount / threadGroupSize);
        _computeStaticMesh.Dispatch(kernel, threadGroups, 1, 1);

        positions = new Vector3[voxelObject.InstanceCount[0] * 4];
        normals = new Vector3[voxelObject.InstanceCount[0]];

        positionsBuffer.GetData(positions);
        normalsBuffer.GetData(normals);

        voxelPositionBuffer.Dispose();
        voxelIndicesBuffer.Dispose();
        faceIndicesBuffer.Dispose();
        rotationMatricesBuffer.Dispose();
        positionsBuffer.Dispose();
        normalsBuffer.Dispose();
    }

    private FbxNode CreateStaticMesh(FbxManager fbxManager, VoxelObject voxelObject)
    {
        Vector3[] positions = null;
        Vector3[] normals = null;
        ComputeVerticesPositionsAndNormals(out positions, out normals, voxelObject);

        FbxMesh fbxMesh = ConvertUnityMeshToFbxMesh(fbxManager, positions, normals, voxelObject);

        FbxNode meshNode = FbxNode.Create(fbxManager, $"{_meshName}");
        meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
        meshNode.SetNodeAttribute(fbxMesh);

        return meshNode;
    }

    private FbxMesh ConvertUnityMeshToFbxMesh(FbxManager fbxManager, Vector3[] vertices, Vector3[] normals, VoxelObject voxelObject)
    {
        FbxMesh fbxMesh = FbxMesh.Create(fbxManager, $"SM_{_meshName}");

        //Vertices
        fbxMesh.InitControlPoints(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            fbxMesh.SetControlPointAt(new FbxVector4(vertices[i].x, vertices[i].y, vertices[i].z, 1), i);
        }

        //Triangles
        for (int i = 0; i < vertices.Length / 4; i++)
        {
            fbxMesh.BeginPolygon();
            fbxMesh.AddPolygon(0 + i * 4);
            fbxMesh.AddPolygon(1 + i * 4);
            fbxMesh.AddPolygon(2 + i * 4);
            fbxMesh.EndPolygon();

            fbxMesh.BeginPolygon();
            fbxMesh.AddPolygon(0 + i * 4);
            fbxMesh.AddPolygon(2 + i * 4);
            fbxMesh.AddPolygon(3 + i * 4);
            fbxMesh.EndPolygon();
        }

        //Normals
        var normalElement = FbxLayerElementNormal.Create(fbxMesh, "Normals");
        normalElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        normalElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        var normalArray = normalElement.GetDirectArray();
        for (int i = 0; i < normals.Length; i++)
        {
            FbxVector4 fbxNormal = new FbxVector4(normals[i].x, normals[i].y, normals[i].z, 0);
            normalArray.Add(fbxNormal);
            normalArray.Add(fbxNormal);
            normalArray.Add(fbxNormal);
            normalArray.Add(fbxNormal);
        }
        fbxMesh.GetLayer(0).SetNormals(normalElement);

        //UVs (used for color indices)
        var uvElement = FbxLayerElementUV.Create(fbxMesh, "UVs");
        uvElement.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        uvElement.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);
        var uvArray = uvElement.GetDirectArray();
        for (int i = 0; i < vertices.Length / 4; i++)
        {
            int voxelIndex = voxelObject.VoxelIndices[i];
            int colorIndex = voxelObject.ColorIndices[voxelIndex];
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
        }
        fbxMesh.GetLayer(0).SetUVs(uvElement);

        return fbxMesh;
    }

    #endregion SaveSystem
}
