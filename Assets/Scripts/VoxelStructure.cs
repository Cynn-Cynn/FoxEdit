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

    public VoxelMeshData[] GetMeshData(out Bounds bounds)
    {
        if (_grid == null)
            GetAllVoxel();

        Vector3Int min = Vector3Int.zero;
        Vector3Int max = Vector3Int.zero;
        GetBounds(out min, out max);

        bounds = new Bounds();
        bounds.center = (new Vector3(min.x + max.x + 1.0f, min.y + max.y + 1.0f, min.z + max.z + 1.0f) / 2.0f) * 0.1f;

        Vector3Int size = max - min;
        size.x = Mathf.Abs(size.x) + 1;
        size.y = Mathf.Abs(size.y) + 1;
        size.z = Mathf.Abs(size.z) + 1;

        bounds.extents = new Vector3((float)size.x / 2.0f, (float)size.y / 2.0f, (float)size.z / 2.0f) * 0.1f;

        return GetFaces();
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
        if (!_grid.ContainsKey(position))
            return;

        VoxelEditor voxelEditor = FindFirstObjectByType<VoxelEditor>();
        if (voxelEditor == null)
            return;

        Material material = voxelEditor.GetMaterial(colorIndex);
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
        if (!_grid.ContainsKey(position) || _grid.Count == 1)
            return false;

        DestroyImmediate(_grid[position].gameObject);
        _grid.Remove(position);
        return true;
    }

    public bool TryColorCube(Vector3Int position, int colorIndex)
    {
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

    private VoxelMeshData[] GetFaces()
    {
        List<VoxelMeshData> meshData = new List<VoxelMeshData>();

        foreach(Vector3Int key in _grid.Keys)
        {
            VoxelMeshData data = SetFaces(new VoxelMeshData(key), key);
            data.ColorIndex = _grid[key].ColorIndex;
            if (data.GetFaces().Length != 0)
                meshData.Add(data);
        }

        VoxelEditor voxelEditor = FindFirstObjectByType<VoxelEditor>();
        if (voxelEditor != null)
        {
            VoxelMeshData[] opaqueMeshData = meshData.Where(mesh => voxelEditor.GetColor(mesh.ColorIndex).Color.a >= 1.0f).ToArray();
            VoxelMeshData[] transparentMeshData = meshData.Where(mesh => voxelEditor.GetColor(mesh.ColorIndex).Color.a < 1.0f).ToArray();
            meshData = opaqueMeshData.Concat(transparentMeshData).ToList();
        }

        return meshData.ToArray();
    }

    private VoxelMeshData SetFaces(VoxelMeshData meshData, Vector3Int key)
    {
        bool isTransparent = false;
        VoxelEditor voxelEditor = FindFirstObjectByType<VoxelEditor>();
        if (voxelEditor != null)
        {
            VoxelColor voxelColor = voxelEditor.GetColor(_grid[key].ColorIndex);
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

        VoxelEditor voxelEditor = FindFirstObjectByType<VoxelEditor>();
        if (voxelEditor == null)
            return false;

        VoxelColor voxelColor = voxelEditor.GetColor(_grid[key].ColorIndex);
        if (voxelColor.Color.a < 1.0f)
            return !isTransparent;

        return false;
    }
}
