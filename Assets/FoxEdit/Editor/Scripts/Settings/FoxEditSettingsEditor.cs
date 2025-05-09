using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FoxEditSettings))]
public class FoxEditSettingsEditor : Editor
{
    private FoxEditSettings _settings;
    private VoxelPalette[] _palettes;
    private List<bool> _editPalette = null;

    private void OnEnable()
    {
        _settings = target as FoxEditSettings;
        _editPalette = new List<bool>();
        if (_settings != null)
        {
            for (int i = 0; i < _settings.Palettes.Length; i++)
            {
                _editPalette.Add(false);
            }
            _palettes = _settings.Palettes;
        }
    }

    public override void OnInspectorGUI()
    {
        PalettesDisplay(); ;

        if (GUI.changed)
            Save();
    }

    private void PalettesDisplay()
    {
        EditorGUILayout.LabelField("Palettes", EditorStyles.boldLabel);

        for (int i = 0; i < _palettes.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();

            VoxelPalette palette = EditorGUILayout.ObjectField(_settings.Palettes[i], typeof(VoxelPalette), true) as VoxelPalette;

            if (palette != _palettes[i])
            {
                _settings.SetPalette(palette, i);
                _palettes = _settings.Palettes;
            }

            if (GUILayout.Button(_editPalette[i] ? "Stop editing" : "Edit"))
            {
                _editPalette[i] = !_editPalette[i];
            }

            if (GUILayout.Button("Remove"))
            {
                _settings.RemoveAt(i);
                _palettes = _settings.Palettes;
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (_editPalette[i] && _palettes[i] != null)
            {
                Editor paletteEditor = CreateEditor(_palettes[i]);
                paletteEditor?.OnInspectorGUI();
            }
        }

        if (GUILayout.Button("Add palette slot"))
        {
            _settings.AddPalette(null);
            _palettes = _settings.Palettes;
        }
    }

    private void Save()
    {
        EditorUtility.SetDirty(_settings);
        AssetDatabase.SaveAssets();
    }
}
