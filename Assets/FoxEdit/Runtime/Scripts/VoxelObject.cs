using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoxelObject : ScriptableObject
{
    [Serializable]
    public struct EditorFrameVoxels
    {
        public Vector3Int[] VoxelPositions;
        public int[] ColorIndices;
    }

    public Bounds Bounds;

    public int PaletteIndex = 0;
    public Vector3[] VoxelPositions = null;
    public int[] VoxelIndices = null;
    public int[] FaceIndices = null;

    public int[] ColorIndices = null;

    public int FrameCount = 0;

    public int[] InstanceStartIndices;
    public int[] InstanceCount = null;
    public int MaxInstanceCount = 0;

    public EditorFrameVoxels[] EditorVoxelPositions = null;
    public Mesh StaticMesh = null;

    public void Print()
    {
        string result = $"Colors: ";

        result += $"\nBounds: {Bounds}";

        result += $"\nPositions: ";
        for (int i = 0; i < VoxelPositions.Length; i++)
        {
            result += $"{VoxelPositions[i]}";
            if (i < VoxelPositions.Length - 1)
                result += " | ";
        }

        result += $"\nVoxel Indices: ";
        for (int i = 0; i < VoxelIndices.Length; i++)
        {
            result += $"{VoxelIndices[i]}";
            if (i < VoxelIndices.Length - 1)
                result += " | ";
        }

        result += $"\nFace Indices: ";
        for (int i = 0; i < FaceIndices.Length; i++)
        {
            result += $"{FaceIndices[i]}";
            if (i < FaceIndices.Length - 1)
                result += " | ";
        }

        result += $"\nColor Indices: ";
        for (int i = 0; i < ColorIndices.Length; i++)
        {
            result += $"{ColorIndices[i]}";
            if (i < ColorIndices.Length - 1)
                result += " | ";
        }

        result += $"\nFrames: {FrameCount}";

        result += $"\nIndices Start/Count: ";
        for (int i = 0; i < InstanceCount.Length; i++)
        {
            result += $"({InstanceStartIndices[i]}; {InstanceCount[i]})";
            if (i < InstanceCount.Length - 1)
                result += " | ";
        }
        result += $"\nMax indices: {MaxInstanceCount}";

        Debug.Log(result);
    }
}
