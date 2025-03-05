using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMeshData
{
    private List<int> _faces = null;
    public Vector3Int Position { get; private set; } = Vector3Int.zero;
    public int ColorIndex = 0;

    public VoxelMeshData(Vector3Int position)
    {
        _faces = new List<int>();
        Position = position;   
    }

    public void AddFace(int index)
    {
        if (index >= 6 || _faces.Contains(index))
            return;

        _faces.Add(index);
    }

    public void PrintFaces()
    {
        string result = $"{Position} - {ColorIndex}\n";
        foreach (int face in _faces)
        {
            result += $"{face} ";
        }
        Debug.Log(result);
    }

    public int[] GetFaces()
    {
        return _faces.ToArray();
    }
}
