using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[ExecuteAlways]
public class VoxelEditor : MonoBehaviour
{
    private enum VoxelAction
    {
        Paint,
        Erase,
        Color
    }

    [SerializeField] private VoxelAction _action = VoxelAction.Paint;

    [SerializeField] private int _selectedPalette = 0;
    [SerializeField] private int _selectedColor = 0;

    [SerializeField][HideInInspector] private List<Material> _materials = null;
    [SerializeField] private VoxelStructure _voxelFramePrefab;

    [SerializeField] private int _currentFrameIndex = 0;
    [SerializeField] private VoxelStructure _currentFrame;
    [SerializeField] private List<VoxelStructure> _frameList;
    [SerializeField] private Material _materialPrefab = null;

    [SerializeField][HideInInspector] private VoxelPalette _palette = null;

    [SerializeField][HideInInspector] VoxelSharedData _sharedData = null;

    [SerializeField][HideInInspector] bool _canClick = true;

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

        RefreshColors();
    }

    private bool TryGetSharedData()
    {
        _sharedData = FindObjectOfType<VoxelSharedData>();
        if (_sharedData == null)
            return false;
        return true;
    }

    private void RefreshColors()
    {
        for (int i = 0; i < _frameList.Count; i++)
        {
            _frameList[i].RefreshColors(_materials);
        }
    }

    [Button("New Voxel Structure")]
    private void InstantiateNewVoxelStructure()
    {
        _frameList.Add(Instantiate(_voxelFramePrefab));
        _currentFrameIndex = _frameList.Count - 1;
        ChangeFrame();
    }

    [Button("Duplicate Current Voxel Structure")]
    private void DuplicateVoxelStructure()
    {
        _frameList.Add(Instantiate(_currentFrame));
        _currentFrameIndex = _frameList.Count - 1;
        ChangeFrame();
    }

    [Button("Change Frame")]
    private void ChangeFrame()
    {
        if (0 > _currentFrameIndex || _currentFrameIndex >= _frameList.Count)
            return;

        _currentFrame.gameObject.SetActive(false);
        _currentFrame = _frameList[_currentFrameIndex];
        _currentFrame.gameObject.SetActive(true);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying)
            return;

#if ENABLE_INPUT_SYSTEM
        if (!_canClick && !Mouse.current.rightButton.IsPressed())
            _canClick = true;

        if (Mouse.current.rightButton.IsPressed() && _canClick)
#else
        if (!_canClick && !Event.current.IsRightMouseButton())
            _canClick = true;
        if (Event.current.IsRightMouseButton() && _canClick)
#endif
            Click();
    }

    private void Click()
    {
        if (_voxelFramePrefab == null)
            return;

        Camera current = Camera.current;
        if (current == null)
            return;

        SceneView sceneWindow = SceneView.GetWindow<SceneView>();
        Vector2 scenePosition = Vector2.zero;
        if (sceneWindow != null)
            scenePosition = sceneWindow.position.position;

        Vector2 mouseInput = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        mouseInput = Mouse.current.position.ReadValue();
#else
        mouseInput = Event.current.mousePosition;
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

        _canClick = false;
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

    public VoxelObject ConstructVoxelObject()
    {
        VoxelObject voxelObject = new VoxelObject();

        Vector3Int[] minBounds = new Vector3Int[_frameList.Count];
        Vector3Int[] maxBounds = new Vector3Int[_frameList.Count];

        List<Vector4> positionsAndColorIndices = new List<Vector4>();

        int[] faceIndices = new int[_frameList.Count * 6];
        int[] frameFaceIndices = new int[6];
        int[] startIndices = new int[_frameList.Count];
        int[] instanceCounts = new int[_frameList.Count];
        int[] voxelIndices = new int[0];

        int startIndex = 0;

        voxelObject.VoxelIndices = new int[6];
        List<int>[] voxelIndicesByFace = CreateListArray(6);

        for (int frame = 0; frame < _frameList.Count; frame++)
        {
            Vector3Int min;
            Vector3Int max;
            VoxelData[] voxelData = _frameList[frame].GetMeshData(out min, out max);
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

        return voxelObject;
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

    public VoxelColor GetColor(int index)
    {
        return _palette.Colors[index];
    }

    public Material GetMaterial(int index)
    {
        return _materials[index];
    }
}
