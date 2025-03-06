using System.Runtime.InteropServices;
using UnityEngine;

public class VoxelObject
{
    public struct ColorData
    {
        Vector4 Color;
        float Emissive;
        float Metallic;
        float Smoothness;

        public ColorData(Vector4 color, float emissive, float metallic, float smoothness)
        {
            Color = color;
            Emissive = emissive;
            Metallic = metallic;
            Smoothness = smoothness;
        }
    }

    public ColorData[] Colors = null;
    public Bounds Bounds;

    public Vector3[] VoxelPositions = null;
    public int[] VoxelIndices = null;
    public int[] FaceIndices = null;

    public int[] ColorIndices = null;

    public int FrameCount = 0;

    public int[] InstanceStartIndices;
    public int[] InstanceCount = null;
    public int MaxInstanceCount = 0;

    public void Print()
    {
        string result = $"Colors: ";
        for (int i = 0; i < Colors.Length; i++)
        {
            result += $"{Colors[i]}";
            if (i < Colors.Length - 1)
                result += " | " ;
        }

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
