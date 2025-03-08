using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VoxelColor
{
    public Color Color = Color.black;
    public float EmissiveIntensity = 0.0f;
    [Range(0.0f, 1.0f)] public float Metallic = 0.0f;
    [Range(0.0f, 1.0f)] public float Smoothness = 1.0f;
}
