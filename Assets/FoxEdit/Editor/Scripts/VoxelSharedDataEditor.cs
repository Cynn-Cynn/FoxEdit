using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelSharedData))]
public class VoxelSharedDataEditor : Editor
{
    private VoxelSharedData _sharedData = null;
    private VoxelGlobalData _globalData = null;
    private SerializedProperty _globalDataProperty = null;

    private List<bool> _editPalette = null;

    private void OnEnable()
    {
        _sharedData = target as VoxelSharedData;

        _globalDataProperty = serializedObject.FindProperty("_globalData");
        _globalData = _globalDataProperty.objectReferenceValue as VoxelGlobalData;

        _editPalette = new List<bool>();
        if (_globalData != null)
        {
            for (int i = 0; i < _globalData.Palettes.Length; i++)
            {
                _editPalette.Add(false);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        GlobalDataDisplay();

        if (_globalData != null)
            PalettesDisplay();
    }

    private void GlobalDataDisplay()
    {
        VoxelGlobalData globalData = EditorGUILayout.ObjectField("Global data", _globalData, typeof(VoxelGlobalData), false) as VoxelGlobalData;
        if (globalData != _globalData)
        {
            _globalData = globalData;
            _globalDataProperty.objectReferenceValue = globalData;
            EditorUtility.SetDirty(_sharedData.gameObject);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }

    private void PalettesDisplay()
    {
        EditorGUILayout.LabelField("Palettes", EditorStyles.boldLabel);

        bool save = false;
        for (int i = 0; i < _globalData.Palettes.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();

            VoxelPalette palette = EditorGUILayout.ObjectField(_globalData.Palettes[i], typeof(VoxelPalette), true) as VoxelPalette;

            if (palette != _globalData.Palettes[i])
            {
                _globalData.Palettes[i] = palette;
                save = true;
            }

            _editPalette[i] = EditorGUILayout.Toggle("Edit ?", _editPalette[i]);

            EditorGUILayout.EndHorizontal();

            if (_editPalette[i])
            {
                if (ColorsDisplay(_globalData.Palettes[i]))
                    save = true;
            }
        }

        if (save)
            RefreshColorPalettes();
    }

    private bool ColorsDisplay(VoxelPalette palette)
    {
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);

        bool save = false;
        VoxelColor[] colors = palette.Colors;

        for (int i = 0; i < colors.Length; i++)
        {
            Color color = EditorGUILayout.ColorField("Color", colors[i].Color);
            if (color != colors[i].Color)
            {
                colors[i].Color = color;
                save = true;
            }

            float emissiveIntensity = EditorGUILayout.FloatField("Emissive Intensity", colors[i].EmissiveIntensity);
            if (emissiveIntensity != colors[i].EmissiveIntensity)
            {
                colors[i].EmissiveIntensity = emissiveIntensity;
                save = true;
            }

            float metallic = EditorGUILayout.Slider("Metallic", colors[i].Metallic, 0.0f, 1.0f);
            if (metallic != colors[i].Metallic)
            {
                colors[i].Metallic = metallic;
                save = true;
            }

            float smoothness = EditorGUILayout.Slider("Smoothness", colors[i].Smoothness, 0.0f, 1.0f);
            if (smoothness != colors[i].Smoothness)
            {
                colors[i].Smoothness = smoothness;
                save = true;
            }

            if (i != colors.Length - 1)
                EditorGUILayout.LabelField("");
        }

        return save;
    }

    private void RefreshColorPalettes()
    {
        _sharedData.CreateColorsBuffers();

        VoxelRenderer[] renderers = FindObjectsOfType<VoxelRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].RefreshColors();
        }

        EditorUtility.SetDirty(_globalData);
        AssetDatabase.SaveAssets();
    }
}
