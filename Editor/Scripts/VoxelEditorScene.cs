using FoxEdit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class VoxelEditorScene
{
    private const string SCENE_NAME = "Assets/FoxEdit/Editor/Editor Default Resources/EditScene/FoxEdit_VoxelObjectEditorScene.unity";
    private const string VOXEL_OBJECT_NAME = "EditedVoxelObject";
    private const string LIGHT_OBJECT_NAME = "Light";
    private const string BACKGROUND_OBJECT_NAME = "Background";

    private static Scene editorScene = default(Scene);
    private static VoxelRenderer voxelRenderer;
    public static bool IsOpen { get; private set; } = false;


    public static VoxelRenderer Open(VoxelObject voxelObject)
    {
        editorScene = EditorSceneManager.OpenScene(SCENE_NAME, OpenSceneMode.Additive);
        SetupEditorScenesObjects(out voxelRenderer);
        SetObjectsVisibilityInScenes(false);

        IsOpen = true;
        voxelRenderer.SetVoxelObject(voxelObject);

        Selection.activeGameObject = voxelRenderer.gameObject;
        SceneView.lastActiveSceneView.FrameSelected();
        voxelRenderer.RenderSwap();
        voxelRenderer.gameObject.SetActive(true);


        return voxelRenderer;
    }

    private static void SetObjectsVisibilityInScenes(bool visibility)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene != editorScene)
            {
                if (visibility)
                    SceneVisibilityManager.instance.Show(scene);
                else
                    SceneVisibilityManager.instance.Hide(scene);
            }
        }
    }

    private static void SetupEditorScenesObjects(out VoxelRenderer voxelRenderer)
    {
        GameObject backgroundGO = null;
        GameObject lightGO = null;
        GameObject voxelGO = null;

        foreach (GameObject gameObject in editorScene.GetRootGameObjects())
        {
            if (gameObject.name.CompareTo(BACKGROUND_OBJECT_NAME) == 0)
                backgroundGO = gameObject;
            else if (gameObject.name.CompareTo(LIGHT_OBJECT_NAME) == 0)
                lightGO = gameObject;
            else if (gameObject.name.CompareTo(VOXEL_OBJECT_NAME) == 0)
                voxelGO = gameObject;
        }

        SceneVisibilityManager.instance.DisablePicking(backgroundGO, true);

        backgroundGO.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
        voxelGO.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
        lightGO.hideFlags = HideFlags.DontSave;

        voxelRenderer = voxelGO.GetComponent<VoxelRenderer>();
    }

    public static void Close()
    {
        if (!IsOpen)
            return;

        SetObjectsVisibilityInScenes(true);
        EditorSceneManager.CloseScene(editorScene, true);
        IsOpen = false;
        voxelRenderer = null;
        editorScene = default(Scene);
    }
}
