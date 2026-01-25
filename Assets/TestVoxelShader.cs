using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TestVoxelShader : MonoBehaviour
{
    [SerializeField] private VoxelObject _voxelObject = null;
    [SerializeField] private Material _staticMaterial = null;

    private void Update()
    {
        _staticMaterial.SetBuffer("_Colors", VoxelSharedData.GetColorBuffer(_voxelObject.PaletteIndex));
    }
}
