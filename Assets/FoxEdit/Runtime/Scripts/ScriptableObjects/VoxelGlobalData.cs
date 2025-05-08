using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VoxelGlobalData", menuName = "FoxEdit/Global Data")]
public class VoxelGlobalData : ScriptableObject
{
    public VoxelPalette[] Palettes;
}
