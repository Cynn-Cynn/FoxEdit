using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using Autodesk.Fbx;
using UnityEditor;
using NaughtyAttributes;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[ExecuteAlways]
#endif
public class VoxelEditor : MonoBehaviour
{
#if UNITY_EDITOR

    private enum VoxelAction
    {
        Paint,
        Erase,
        Color
    }

    [SerializeField] private string _meshName = "";
    [SerializeField] private string _saveDirectory = "";
    [SerializeField] private VoxelObject _voxelObject = null;

    [SerializeField] private VoxelAction _action = VoxelAction.Paint;

    [SerializeField] private int _selectedPalette = 0;
    [SerializeField] private int _selectedColor = 0;
    [SerializeField] private int _seletectFrame = 0;
    [SerializeField] private List<VoxelStructure> _frameList;

    [SerializeField] private bool _debug = false;
    [ShowIf("_debug")][SerializeField] private List<Material> _materials = null;
    [ShowIf("_debug")][SerializeField] private VoxelStructure _voxelFramePrefab;
    [ShowIf("_debug")][SerializeField] private VoxelStructure _currentFrame;
    [ShowIf("_debug")][SerializeField] private Material _materialPrefab = null;
    [ShowIf("_debug")][SerializeField] private ComputeShader _computeStaticMesh = null;
    [ShowIf("_debug")][SerializeField] private VoxelPalette _palette = null;
    [ShowIf("_debug")][SerializeField] VoxelSharedData _sharedData = null;
    [ShowIf("_debug")][SerializeField] bool _canClick = true;

    #region Initialization

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        if (_materials == null)
            CreateMaterials();

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            return;

        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private bool TryGetSharedData()
    {
        _sharedData = FindObjectOfType<VoxelSharedData>();
        if (_sharedData == null)
            return false;
        return true;
    }

    #endregion  Initialization

    #region Getters

    public VoxelColor GetColor(int index)
    {
        return _palette.Colors[index];
    }

    public Material GetMaterial(int index)
    {
        return _materials[index];
    }

    #endregion Getters

    #region Actions

    [Button("Load Mesh")]
    private void LoadMesh()
    {
        if (_voxelObject == null)
            return;

        for (int i = 0; i < _frameList.Count; i++)
        {
            DestroyImmediate(_frameList[i].gameObject);
        }
        _frameList.Clear();

        for (int i = 0; i < _voxelObject.EditorVoxelPositions.Length; i++)
        {
            VoxelStructure frame = Instantiate(_voxelFramePrefab, transform);
            frame.LoadFromMesh(_voxelObject.EditorVoxelPositions[i].VoxelPositions, _voxelObject.EditorVoxelPositions[i].ColorIndices);
            if (i != 0)
                frame.gameObject.SetActive(false);
            _frameList.Add(frame);
        }

        _currentFrame = _frameList[0];
    }

    [Button("Refresh Colors")]
    void CreateMaterials()
    {
        _materials.Clear();
        _materials = new List<Material>();

        if (_sharedData != null || TryGetSharedData())
        {
            VoxelPalette newPalette = _sharedData.GetPalette(_selectedPalette);
            if (newPalette != null)
                _palette = newPalette;
        }

        for (int i = 0; i < _palette.Colors.Length; i++)
        {
            _materials.Add(new Material(_materialPrefab));
            _materials[i].color = _palette.Colors[i].Color + _palette.Colors[i].Color * _palette.Colors[i].EmissiveIntensity;
            _materials[i].SetFloat("_Smoothness", _palette.Colors[i].Smoothness);
            _materials[i].SetFloat("_Metallic", _palette.Colors[i].Metallic);
        }

        for (int i = 0; i < _frameList.Count; i++)
        {
            _frameList[i].RefreshColors(_materials);
        }
    }

    [Button("New Frame")]
    private void InstantiateNewVoxelStructure()
    {
        _frameList.Add(Instantiate(_voxelFramePrefab, transform));
        if (_frameList.Count != 0)
            _seletectFrame = _frameList.Count - 1;
        ChangeFrame();
        _frameList[_seletectFrame].Initialize();
    }

    [Button("Duplicate Current Frame")]
    private void DuplicateVoxelStructure()
    {
        _frameList.Add(Instantiate(_currentFrame, transform));
        _seletectFrame = _frameList.Count - 1;
        ChangeFrame();
        _frameList[_seletectFrame].Initialize();
    }

    [Button("Change Frame")]
    private void ChangeFrame()
    {
        if (0 > _seletectFrame || _seletectFrame >= _frameList.Count)
            return;

        _currentFrame?.gameObject.SetActive(false);
        _currentFrame = _frameList[_seletectFrame];
        _currentFrame.gameObject.SetActive(true);
    }

    #endregion Actions

    #region VoxelEditor

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
            Vector3Int gridPosition = _currentFrame.WorldToGridPosition(worldPosition);
            Vector3Int direction = _currentFrame.NormalToDirection(worldNormal);
            if (_action == VoxelAction.Paint)
                _currentFrame.TryAddCubeNextTo(gridPosition, direction, _selectedColor);
            else if (_action == VoxelAction.Erase)
                _currentFrame.TryRemoveCube(gridPosition);
            else if (_action == VoxelAction.Color)
                _currentFrame.TryColorCube(gridPosition, _selectedColor);
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

#endregion VoxelEditor

    #region SaveSystem

    [Button("Create/Update Mesh")]
    private void ConstructVoxelObject()
    {
        string assetPath = null;
        if (_saveDirectory == null || _saveDirectory == "")
            assetPath = $"Assets/{_meshName}.asset";
        else
            assetPath = $"Assets/{_saveDirectory}/{_meshName}.asset";

        if (_voxelObject == null)
        {
            _voxelObject = ScriptableObject.CreateInstance<VoxelObject>();
            AssetDatabase.CreateAsset(_voxelObject, assetPath);
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

        _voxelObject.VoxelIndices = new int[6];
        List<int>[] voxelIndicesByFace = CreateListArray(6);

        for (int frame = 0; frame < _frameList.Count; frame++)
        {
            Vector3Int min;
            Vector3Int max;
            VoxelData[] voxelData = _frameList[frame].GetMeshData(out min, out max);
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

        _voxelObject.Bounds = CreateBounds(minBounds, maxBounds);
        _voxelObject.PaletteIndex = _selectedPalette;

        _voxelObject.VoxelPositions = positionsAndColorIndices.Select(voxelData => (Vector3)voxelData).ToArray();
        _voxelObject.VoxelIndices = voxelIndices;
        _voxelObject.FaceIndices = faceIndices;
        _voxelObject.ColorIndices = positionsAndColorIndices.Select(voxelData => (int)voxelData.w).ToArray();

        _voxelObject.FrameCount = _frameList.Count;
        _voxelObject.InstanceCount = instanceCounts;
        _voxelObject.MaxInstanceCount = instanceCounts.Max();
        _voxelObject.InstanceStartIndices = startIndices;
        _voxelObject.EditorVoxelPositions = editorVoxelPositions;

        string fbxPath = null;
        if (_saveDirectory == null || _saveDirectory == "")
            fbxPath = $"Assets/{_meshName}.asset";
        else
            fbxPath = $"Assets/{_saveDirectory}/{_meshName}.fbx";


        CreateFBX(fbxPath);
        AssetDatabase.Refresh();
        GameObject go = AssetDatabase.LoadAssetAtPath(fbxPath, typeof(GameObject)) as GameObject;
        _voxelObject.StaticMesh = go.GetComponent<MeshFilter>().sharedMesh;

        EditorUtility.SetDirty(_voxelObject);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = _voxelObject;
    }

    private void CreateFBX(string fbxPath)
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

            FbxNode mesh = CreateStaticMesh(fbxManager);
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

    private void ComputeVerticesPositionsAndNormals(out Vector3[] positions, out Vector3[] normals)
    {
        int kernel = _computeStaticMesh.FindKernel("VoxelGeneration");

        //Voxel
        GraphicsBuffer voxelPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelPositions.Length, sizeof(float) * 3);
        voxelPositionBuffer.SetData(_voxelObject.VoxelPositions);
        _computeStaticMesh.SetBuffer(kernel, "_VoxelPositions", voxelPositionBuffer);

        GraphicsBuffer faceIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.FaceIndices.Length, sizeof(int));
        faceIndicesBuffer.SetData(_voxelObject.FaceIndices);
        _computeStaticMesh.SetBuffer(kernel, "_FaceIndices", faceIndicesBuffer);

        GraphicsBuffer voxelIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.VoxelIndices.Length, sizeof(int));
        voxelIndicesBuffer.SetData(_voxelObject.VoxelIndices);
        _computeStaticMesh.SetBuffer(kernel, "_VoxelIndices", voxelIndicesBuffer);

        Matrix4x4[] rotationMatrices = GetRotationMatrices();
        GraphicsBuffer rotationMatricesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 6, sizeof(float) * 16);
        rotationMatricesBuffer.SetData(rotationMatrices);
        _computeStaticMesh.SetBuffer(kernel, "_RotationMatrices", rotationMatricesBuffer);

        GraphicsBuffer positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.InstanceCount[0] * 4, sizeof(float) * 3);
        _computeStaticMesh.SetBuffer(kernel, "_VertexPosition", positionsBuffer);

        GraphicsBuffer normalsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _voxelObject.InstanceCount[0], sizeof(float) * 3);
        _computeStaticMesh.SetBuffer(kernel, "_VertexNormals", normalsBuffer);

        int instanceCount = _voxelObject.InstanceCount[0];
        _computeStaticMesh.SetInt("_InstanceCount", instanceCount);

        uint threadGroupSize = 0;
        _computeStaticMesh.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);
        int threadGroups = Mathf.CeilToInt((float)instanceCount / threadGroupSize);
        _computeStaticMesh.Dispatch(kernel, threadGroups, 1, 1);

        positions = new Vector3[_voxelObject.InstanceCount[0] * 4];
        normals = new Vector3[_voxelObject.InstanceCount[0]];

        positionsBuffer.GetData(positions);
        normalsBuffer.GetData(normals);

        voxelPositionBuffer.Dispose();
        voxelIndicesBuffer.Dispose();
        faceIndicesBuffer.Dispose();
        rotationMatricesBuffer.Dispose();
        positionsBuffer.Dispose();
        normalsBuffer.Dispose();
    }

    private FbxNode CreateStaticMesh(FbxManager fbxManager)
    {
        Vector3[] positions = null;
        Vector3[] normals = null;
        ComputeVerticesPositionsAndNormals(out positions, out normals);

        FbxMesh fbxMesh = ConvertUnityMeshToFbxMesh(fbxManager, positions, normals);

        FbxNode meshNode = FbxNode.Create(fbxManager, $"{_meshName}");
        meshNode.LclTranslation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        meshNode.LclRotation.Set(new FbxDouble3(0.0, 0.0, 0.0));
        meshNode.LclScaling.Set(new FbxDouble3(1.0, 1.0, 1.0));
        meshNode.SetNodeAttribute(fbxMesh);

        return meshNode;
    }

    private FbxMesh ConvertUnityMeshToFbxMesh(FbxManager fbxManager, Vector3[] vertices, Vector3[] normals)
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
            int voxelIndex = _voxelObject.VoxelIndices[i];
            int colorIndex = _voxelObject.ColorIndices[voxelIndex];
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
            uvArray.Add(new FbxVector2(colorIndex, 0));
        }
        fbxMesh.GetLayer(0).SetUVs(uvElement);

        return fbxMesh;
    }

    #endregion SaveSystem

#endif
    }
