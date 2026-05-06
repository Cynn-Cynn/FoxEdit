using UnityEngine;
using UnityEditor;
using FoxEdit.EditorUtils;
using System.IO;
using System;

internal class FoxEditEditorSettings
{
    private static FoxEditEditorSettings _instance;
    public static FoxEditEditorSettings Instance
    {
        get
        {
            if (_instance == null)
                _instance = new FoxEditEditorSettings();
            return _instance;
        }
    }
    public GUIDAssetLoader<Material> VoxelStageBackgroundMaterial = new GUIDAssetLoader<Material>("18013ba6d01cea946a3fd252f8d26eea");
    public GUIDAssetLoader<Material> VoxelEditorCubeMaterial = new GUIDAssetLoader<Material>("3ba88c2707cea7843b37c87a3a206258");
    public GUIDAssetLoader<MeshRenderer> VoxelPrefab = new GUIDAssetLoader<MeshRenderer>("b372f3a77bc32ba418920cfa5cab2b28");

    public EditorPrefColor ToolAddColor = new EditorPrefColor("foxedit-add-color", Color.green);
    public EditorPrefColor ToolRemoveColor = new EditorPrefColor("foxedit-remove-color", Color.red);
    public EditorPrefColor ToolPaintColor = new EditorPrefColor("foxedit-paint-color", Color.blue);

    public EditorPrefColor StageBackgroundColor = new EditorPrefColor("foxedit-background-color", Color.gray);
    public EditorPrefFloat StageBackgroundSphereSize = new EditorPrefFloat("foxedit-background-size", 100f, 0f, 1000f);
    public EditorPrefColor StageLightColor = new EditorPrefColor("foxedit-light-color", new Color(1f, 0.9568627f, 0.8392157f));
    public EditorPrefFloat StageLightIntensity = new EditorPrefFloat("foxedit-light-intensity", 1f, min: 0f);
    public EditorPrefFloat StageLightIndirectMultiplier = new EditorPrefFloat("foxedit-light-indirect-multiplier", 1f, min: 0f);

    public EditorPrefString DefaultSavePath = new EditorPrefString("foxedit-save-path", Path.Join(Application.dataPath, "FoxEdit", "Objects"));
}

internal class FoxEditEditorSettingsProvider : SettingsProvider
{
    FoxEditEditorSettings settings;
    public FoxEditEditorSettingsProvider() : base("FoxEdit", SettingsScope.User, new string[] { "Voxel", "Fox" })
    {
        settings = FoxEditEditorSettings.Instance;
    }

    public override void OnGUI(string searchContext)
    {
        base.OnGUI(searchContext);

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.ObjectField(new GUIContent("Voxel cube material"), settings.VoxelEditorCubeMaterial.Asset, typeof(Material), allowSceneObjects: false);
        EditorGUILayout.ObjectField(new GUIContent("Voxel stage background"), settings.VoxelStageBackgroundMaterial.Asset, typeof(Material), allowSceneObjects: false);
        EditorGUILayout.ObjectField(new GUIContent("Voxel object prefab"), settings.VoxelPrefab.Asset, typeof(MeshRenderer), allowSceneObjects: false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool colors", EditorStyles.boldLabel);
        settings.ToolAddColor.Value = EditorGUILayout.ColorField(new GUIContent("Add"), settings.ToolAddColor.Value);
        settings.ToolPaintColor.Value = EditorGUILayout.ColorField(new GUIContent("Paint"), settings.ToolPaintColor.Value);
        settings.ToolRemoveColor.Value = EditorGUILayout.ColorField(new GUIContent("Remove"), settings.ToolRemoveColor.Value);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
        settings.StageBackgroundColor.Value = EditorGUILayout.ColorField(new GUIContent("Background color"), settings.StageBackgroundColor.Value);
        settings.StageBackgroundSphereSize.Value = EditorGUILayout.FloatField(new GUIContent("Background sphere size"), settings.StageBackgroundSphereSize.Value);
        settings.StageLightColor.Value = EditorGUILayout.ColorField(new GUIContent("Light color"), settings.StageLightColor.Value);
        settings.StageLightIntensity.Value = EditorGUILayout.FloatField(new GUIContent("Light intensity"), settings.StageLightIntensity.Value);
        settings.StageLightIndirectMultiplier.Value = EditorGUILayout.FloatField(new GUIContent("Light indirect multiplier"), settings.StageLightIndirectMultiplier.Value);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
        GUI.enabled = false;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField(new GUIContent("Default save path"), settings.DefaultSavePath.Value);
        GUI.enabled = true;
        if (GUILayout.Button(EditorGUIUtility.IconContent("FolderOpened On Icon"), GUILayout.Width(25), GUILayout.Height(25)))
        {
            string path = EditorUtility.OpenFolderPanel("Default save", Application.dataPath, "VoxelObjects");
            if (path == null || !IsPathInsideDataPath(path))
                return;
            settings.DefaultSavePath.Value = path;
        }
        EditorGUILayout.EndHorizontal();

    }

    public static bool IsPathInsideDataPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string dataPath = Path.GetFullPath(Application.dataPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        string fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase);
    }

    [SettingsProvider]
    public static SettingsProvider CreateMyPackageSettingsProvider()
    {
        return new FoxEditEditorSettingsProvider();
    }
}