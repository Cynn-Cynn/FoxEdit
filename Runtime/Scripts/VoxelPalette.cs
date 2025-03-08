using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Voxel Palette", menuName = "Voxel")]
public class VoxelPalette : ScriptableObject
{
    public VoxelColor[] Colors = null;

    public int PaletteSize { get { return Colors.Length; } }
}
