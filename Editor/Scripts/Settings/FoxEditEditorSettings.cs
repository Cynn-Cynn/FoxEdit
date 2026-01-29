using UnityEngine;
using UnityEditor;
using FoxEdit.EditorUtils;

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

    public EditorPrefColor ToolAddColor = new EditorPrefColor("foxedit-add-color", Color.green);
    public EditorPrefColor ToolRemoveColor = new EditorPrefColor("foxedit-remove-color", Color.red);
    public EditorPrefColor ToolPaintColor = new EditorPrefColor("foxedit-paint-color", Color.blue);

    public EditorPrefColor BackgroundColor = new EditorPrefColor("foxedit-background-color", Color.gray);
    public EditorPrefFloat BackgroundSphereSize = new EditorPrefFloat("foxedit-background-size", 100f, 0f, 1000f);
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
        EditorGUILayout.Space();
        GUI.enabled = false;
        EditorGUILayout.ObjectField(new GUIContent("Voxel cube material"), settings.VoxelEditorCubeMaterial.Asset, typeof(Material), allowSceneObjects: false);
        EditorGUILayout.ObjectField(new GUIContent("Voxel stage background"), settings.VoxelStageBackgroundMaterial.Asset, typeof(Material), allowSceneObjects: false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tool colors", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        settings.ToolAddColor.Value = EditorGUILayout.ColorField(new GUIContent("Add"), settings.ToolAddColor.Value);
        settings.ToolPaintColor.Value = EditorGUILayout.ColorField(new GUIContent("Paint"), settings.ToolPaintColor.Value);
        settings.ToolRemoveColor.Value = EditorGUILayout.ColorField(new GUIContent("Remove"), settings.ToolRemoveColor.Value);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage background", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        settings.BackgroundColor.Value = EditorGUILayout.ColorField(new GUIContent("Background color"), settings.BackgroundColor.Value);
        settings.BackgroundSphereSize.Value = EditorGUILayout.FloatField(new GUIContent("Background sphere size"), settings.BackgroundSphereSize.Value);
    }

    [SettingsProvider]
    public static SettingsProvider CreateMyPackageSettingsProvider()
    {
        return new FoxEditEditorSettingsProvider();
    }
}