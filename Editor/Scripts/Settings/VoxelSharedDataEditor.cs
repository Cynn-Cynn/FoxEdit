using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelSharedData))]
public class VoxelSharedDataEditor : Editor
{
    private VoxelSharedData _sharedData = null;
    private FoxEditSettings _settings = null;

    private Editor _paletteEditor = null;

    private void OnEnable()
    {
        _sharedData = target as VoxelSharedData;
        GetSettings();
        _paletteEditor = CreateEditor(_settings);
    }

    private void GetSettings()
    {
        _settings = FoxEditSettings.GetSettings();
        serializedObject.FindProperty("_settings").objectReferenceValue = _settings;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_sharedData);
        AssetDatabase.SaveAssets();
    }

    public override void OnInspectorGUI()
    {
        if (_paletteEditor != null)
        {
            _paletteEditor.OnInspectorGUI();
        }

        if (GUI.changed)
            RefreshBuffers();
    }

    private void RefreshBuffers()
    {
        _sharedData.CreateColorsBuffers();

        VoxelRenderer[] renderers = FindObjectsOfType<VoxelRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].RefreshColors();
        }
    }
}
