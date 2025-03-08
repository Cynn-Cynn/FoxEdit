using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using System.Linq;

[ExecuteAlways]
public class VoxelStructure : MonoBehaviour
{
    private Dictionary<Vector3Int, VoxelPlaceHolder> _grid = null;

    [SerializeField] private VoxelPlaceHolder _voxelPrefab = null;

    [SerializeField] [HideInInspector] VoxelEditor _voxelEditor = null;

    private void Awake()
    {
        if (_grid == null)
            GetAllVoxel();

        if (_grid.Count == 0)
            TryAddCubeNextTo(Vector3Int.zero, Vector3Int.zero, 0);
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        if (_grid == null)
            GetAllVoxel();
    }

    public void LoadFromMesh(Vector3Int[] voxelPositions, int[] colorIndices)
    {
        DestroyImmediate(_grid[Vector3Int.zero]);
        _grid.Clear();

        for (int i = 0; i < voxelPositions.Length; i++)
        {
            Vector3 localPosition = GridToLocalPosition(voxelPositions[i]);
            VoxelPlaceHolder voxel = Instantiate(_voxelPrefab, transform);
            voxel.transform.localPosition = localPosition;
            _grid[voxelPositions[i]] = voxel;
            SetColor(voxelPositions[i], colorIndices[i]);
        }
    }

    public VoxelData[] GetMeshData(out Vector3Int minBounds, out Vector3Int maxBounds)
    {
        if (_grid == null)
            GetAllVoxel();

        GetBounds(out minBounds, out maxBounds);

        return GetFaces();
    }

    public Vector3Int[] GetEditorVoxelPositions()
    {
        if (_grid == null)
            GetAllVoxel();

        return _grid.Keys.ToArray();
    }

    public int[] GetEditorVoxelColorIndices()
    {
        if (_grid == null)
            GetAllVoxel();

        return _grid.Values.Select(voxel => voxel.ColorIndex).ToArray();
    }

    private void GetAllVoxel()
    {
        _grid = new Dictionary<Vector3Int, VoxelPlaceHolder>();

        foreach (Transform child in transform)
        {
            Vector3Int gridPosition = WorldToGridPosition(child.position);
            _grid[gridPosition] = child.GetComponent<VoxelPlaceHolder>();
        }
    }

    public bool TryAddCubeNextTo(Vector3Int position, Vector3Int direction, int colorIndex)
    {
        if (!_grid.ContainsKey(position) && position != Vector3Int.zero)
            return false;

        Vector3Int newPosition = position + direction;

        if (_grid.ContainsKey(newPosition))
            return false;

        _grid[newPosition] = Instantiate(_voxelPrefab, transform);
        _grid[newPosition].transform.localPosition = GridToLocalPosition(newPosition);
        SetColor(newPosition, colorIndex);

        return true;
    }

    private void SetColor(Vector3Int position, int colorIndex)
    {
        if (_grid == null)
            GetAllVoxel();

        if (!_grid.ContainsKey(position))
            return;

        if (_voxelEditor == null && !TryGetVoxelEditor())
            return;

        Material material = _voxelEditor.GetMaterial(colorIndex);
        _grid[position].ColorIndex = colorIndex;
        _grid[position].GetComponent<MeshRenderer>().material = material;
    }

    public void RefreshColors(List<Material> materials)
    {
        foreach (Vector3Int position in _grid.Keys)
        {
            int colorIndex = _grid[position].ColorIndex;
            _grid[position].GetComponent<MeshRenderer>().material = materials[colorIndex];
        }
    }

    public bool TryRemoveCube(Vector3Int position)
    {
        if (_grid == null)
            GetAllVoxel();

        if (!_grid.ContainsKey(position) || _grid.Count == 1)
            return false;

        DestroyImmediate(_grid[position].gameObject);
        _grid.Remove(position);
        return true;
    }

    public bool TryColorCube(Vector3Int position, int colorIndex)
    {
        if (_grid == null)
            GetAllVoxel();

        if (!_grid.ContainsKey(position) || _grid.Count == 1)
            return false;

        SetColor(position, colorIndex);
        return true;
    }

    public void OnCubeDeletion(Vector3 worldPosition)
    {
        Vector3Int gridPosition = WorldToGridPosition(worldPosition);
        _grid.Remove(gridPosition);
    }

    private Vector3 GridToLocalPosition(Vector3Int position)
    {
        return new Vector3(position.x, position.y, position.z) * 0.1f;
    }

    public Vector3Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        localPosition *= 10.0f;
        return new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y), Mathf.RoundToInt(localPosition.z));
    }

    public Vector3Int NormalToDirection(Vector3 normal)
    {
        Quaternion inverseRotation = Quaternion.Inverse(transform.rotation);
        normal = inverseRotation * normal;

        return new Vector3Int(Mathf.RoundToInt(normal.x), Mathf.RoundToInt(normal.y), Mathf.RoundToInt(normal.z));
    }

    private void PrintVoxelArray(Transform[][][] array)
    {
        string result = "";
        for (int x = 0; x < array.Length; x++)
        {
            for (int y = 0; y < array[x].Length; y++)
            {
                for (int z = 0; z < array[x][y].Length; z++)
                {
                    if (array[x][y][z] == null)
                        result += "_";
                    else
                        result += "O";
                }
                result += "\n";
            }
            result += "\n\n";
        }

        Debug.Log(result);
    }

    private void GetBounds(out Vector3Int min, out Vector3Int max)
    {
        min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

        foreach (Vector3Int position in _grid.Keys)
        {
            min.x = GetMin(position.x, min.x);
            min.y = GetMin(position.y, min.y);
            min.z = GetMin(position.z, min.z);

            max.x = GetMax(position.x, max.x);
            max.y = GetMax(position.y, max.y);
            max.z = GetMax(position.z, max.z);
        }
    }

    private int GetMin(int a, int b)
    {
        return a < b ? a : b;
    }

    private int GetMax(int a, int b)
    {
        return a > b ? a : b;
    }

    private VoxelData[] GetFaces()
    {
        List<VoxelData> meshData = new List<VoxelData>();

        foreach(Vector3Int key in _grid.Keys)
        {
            VoxelData data = SetFaces(new VoxelData(key), key);
            data.ColorIndex = _grid[key].ColorIndex;
            if (data.GetFaces().Length != 0)
                meshData.Add(data);
        }

        if (_voxelEditor != null || TryGetVoxelEditor())
        {
            VoxelData[] opaqueMeshData = meshData.Where(mesh => _voxelEditor.GetColor(mesh.ColorIndex).Color.a >= 1.0f).ToArray();
            VoxelData[] transparentMeshData = meshData.Where(mesh => _voxelEditor.GetColor(mesh.ColorIndex).Color.a < 1.0f).ToArray();
            meshData = opaqueMeshData.Concat(transparentMeshData).ToList();
        }

        return meshData.ToArray();
    }

    private VoxelData SetFaces(VoxelData meshData, Vector3Int key)
    {
        bool isTransparent = false;
        if (_voxelEditor != null || TryGetVoxelEditor())
        {
            VoxelColor voxelColor = _voxelEditor.GetColor(_grid[key].ColorIndex);
            if (voxelColor.Color.a < 1.0f)
                isTransparent = true;
        }

        if (IsFaceVisible(key + new Vector3Int(0, 1, 0), isTransparent))
            meshData.AddFace(0);
        if (IsFaceVisible(key + new Vector3Int(0, 0, -1), isTransparent))
            meshData.AddFace(1);
        if (IsFaceVisible(key + new Vector3Int(0, -1, 0), isTransparent))
            meshData.AddFace(2);
        if (IsFaceVisible(key + new Vector3Int(0, 0, 1), isTransparent))
            meshData.AddFace(3);
        if (IsFaceVisible(key + new Vector3Int(-1, 0, 0), isTransparent))
            meshData.AddFace(4);
        if (IsFaceVisible( key + new Vector3Int(1, 0, 0), isTransparent))
            meshData.AddFace(5);

        return meshData;
    }

    private bool IsFaceVisible(Vector3Int key, bool isTransparent)
    {
        if (!_grid.ContainsKey(key))
            return true;

        if (_voxelEditor == null && !TryGetVoxelEditor())
            return false;

        VoxelColor voxelColor = _voxelEditor.GetColor(_grid[key].ColorIndex);
        if (voxelColor.Color.a < 1.0f)
            return !isTransparent;

        return false;
    }

    private bool TryGetVoxelEditor()
    {
        _voxelEditor = FindFirstObjectByType<VoxelEditor>();
        if (_voxelEditor == null)
            return false;

        return true;
    }
}
