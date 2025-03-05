using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField] private int _selectedColor = 0;
    [SerializeField] private VoxelColor[] _colors = null;
    [SerializeField][HideInInspector] private List<Material> _materials = null;
    [SerializeField] private VoxelStructure _voxelStructurePrefab;

    [SerializeField] private int _currentFrame = 0;
    [SerializeField] private VoxelStructure _currentVoxelStructure;
    [SerializeField] private List<VoxelStructure> _currentVoxelStructures;
    [SerializeField] private Material _materialPrefab = null;

    private Ray _ray;

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        if (_materials == null)
            CreateMaterials();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            return;
    }

    [Button("Refresh Colors")]
    void CreateMaterials()
    {
        _materials.Clear();
        _materials = new List<Material>();

        for (int i = 0; i < _colors.Length; i++)
        {
            _materials.Add(new Material(_materialPrefab));
            _materials[i].color = _colors[i].Color + _colors[i].Color * _colors[i].EmissiveIntensity;
            _materials[i].SetFloat("_Smoothness", _colors[i].Smoothness);
            _materials[i].SetFloat("_Metallic", _colors[i].Metallic);
        }

        RefreshColors();
    }

    private void RefreshColors()
    {
        for (int i = 0; i < _currentVoxelStructures.Count; i++)
        {
            _currentVoxelStructures[i].RefreshColors(_materials);
        }
    }

    [Button("New Voxel Structure")]
    private void InstantiateNewVoxelStructure()
    {
        _currentVoxelStructures.Add(Instantiate(_voxelStructurePrefab));
        _currentFrame = _currentVoxelStructures.Count - 1;
        ChangeFrame();
    }

    [Button("Duplicate Current Voxel Structure")]
    private void DuplicateVoxelStructure()
    {
        _currentVoxelStructures.Add(Instantiate(_currentVoxelStructure));
        _currentFrame = _currentVoxelStructures.Count - 1;
        ChangeFrame();
    }

    [Button("Change Frame")]
    private void ChangeFrame()
    {
        if (0 > _currentFrame || _currentFrame >= _currentVoxelStructures.Count)
            return;

        _currentVoxelStructure.gameObject.SetActive(false);
        _currentVoxelStructure = _currentVoxelStructures[_currentFrame];
        _currentVoxelStructure.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Application.isPlaying)
            return;

        if (Mouse.current.rightButton.IsPressed())
            Click();
    }

    private void Click()
    {
        if (_voxelStructurePrefab == null)
            return;

        Camera current = Camera.current;
        if (current == null)
            return;

        SceneView sceneWindow = SceneView.GetWindow<SceneView>();
        Vector2 scenePosition = Vector2.zero;
        if (sceneWindow != null)
            scenePosition = sceneWindow.position.position;

        Vector2 mouseInput = Mouse.current.position.ReadValue();
        Vector2 guiPosition = mouseInput - scenePosition - new Vector2(0.0f, 45.0f);

        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

        Vector3 worldPosition = Vector3.zero;
        Vector3 worldNormal = Vector3.zero;

        if (TryGetCubePosition(out worldPosition, out worldNormal, ray))
        {
            Vector3Int gridPosition = _currentVoxelStructure.WorldToGridPosition(worldPosition);
            Vector3Int direction = _currentVoxelStructure.NormalToDirection(worldNormal);
            if (_action == VoxelAction.Paint)
                _currentVoxelStructure.TryAddCubeNextTo(gridPosition, direction, _selectedColor);
            else if (_action == VoxelAction.Erase)
                _currentVoxelStructure.TryRemoveCube(gridPosition);
            else if (_action == VoxelAction.Color)
                _currentVoxelStructure.TryColorCube(gridPosition, _selectedColor);
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

    public VoxelObject ConstructVoxelObject()
    {
        VoxelObject voxelObject = new VoxelObject();

        voxelObject.Colors = _colors.Select<VoxelColor, VoxelObject.ColorData>(color => {
            return new VoxelObject.ColorData(
                new Vector4(color.Color.r, color.Color.g, color.Color.b, color.Color.a),
                color.EmissiveIntensity, color.Metallic, color.Smoothness
            );
        }).ToArray();
        Bounds bounds = new Bounds();

        List<Vector3> positions = new List<Vector3>();
        List<int> colorIndices = new List<int>();
        int[] faceIndices = new int[0];
        List<int> voxelIndices = new List<int>();

        List<int> startIndices = new List<int>();
        List<int> instanceCounts = new List<int>();

        int startIndex = 0;
        int voxelIndex = 0;
        for (int frame = 0; frame < _currentVoxelStructures.Count; frame++)
        {
            VoxelMeshData[] meshData = _currentVoxelStructures[frame].GetMeshData(out bounds);
            startIndices.Add(startIndex);
            int instanceCount = 0;
            for (int voxel = 0; voxel < meshData.Length; voxel++)
            {
                VoxelMeshData data = meshData[voxel];
                positions.Add(data.Position);
                colorIndices.Add(data.ColorIndex);
                int[] faces = data.GetFaces();
                faceIndices = faceIndices.Concat(faces).ToArray();
                for (int face = 0; face < faces.Length; face++)
                {
                    voxelIndices.Add(voxelIndex);
                }
                instanceCount += faces.Length;
                voxelIndex += 1;
            }
            instanceCounts.Add(instanceCount);
            startIndex += instanceCount;
        }

        voxelObject.Bounds = bounds;

        voxelObject.Positions = positions.ToArray();
        voxelObject.VoxelIndices = voxelIndices.ToArray();
        voxelObject.FaceIndices = faceIndices.ToArray();
        voxelObject.ColorIndices = colorIndices.ToArray();

        voxelObject.FrameCount = _currentVoxelStructures.Count;
        voxelObject.InstanceCounts = instanceCounts.ToArray();
        voxelObject.MaxInstanceCount = instanceCounts.Max();
        voxelObject.StartIndices = startIndices.ToArray();

        return voxelObject;
    }


    public VoxelColor GetColor(int index)
    {
        return _colors[index];
    }

    public Material GetMaterial(int index)
    {
        return _materials[index];
    }
}
