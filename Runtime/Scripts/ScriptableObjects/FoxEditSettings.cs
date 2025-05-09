using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "VoxelGlobalData", menuName = "FoxEdit/Global Data")]
public class FoxEditSettings : ScriptableObject
{
    [SerializeField] private List<VoxelPalette> _palettes;

    public VoxelPalette[] Palettes { get { return _palettes.ToArray(); } }

    public void AddPalette(VoxelPalette palette)
    {
        if (!_palettes.Contains(palette))
            _palettes.Add(palette);
    }

    public void RemoveAt(int index)
    {
        _palettes.RemoveAt(index);
    }

    public void SetPalette(VoxelPalette palette, int index)
    {
        if (index >= _palettes.Count)
            return;

        _palettes[index] = palette;
    }

    public static FoxEditSettings GetSettings()
    {
        string settingsPath = AssetDatabase.GUIDToAssetPath("025fe4d424868cb438483d89fb07a75e");
        return AssetDatabase.LoadAssetAtPath<FoxEditSettings>(settingsPath);
    }
}
