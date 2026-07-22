using FoxEdit;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    internal class DragDropVoxelObject
    {
        [InitializeOnLoad]
        internal static class ScriptableObjectSceneDropHandler
        {
            static ScriptableObjectSceneDropHandler()
            {
                DragAndDrop.AddDropHandlerV2(SceneDrop);
            }

            static DragAndDropVisualMode SceneDrop(
                Object dropUpon,
                Vector3 worldPosition,
                Vector2 viewportPosition,
                Transform parent,
                bool perform)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is not VoxelObject voxel)
                        continue;

                    if (!perform)
                        return DragAndDropVisualMode.Copy;

                    var go = new GameObject(voxel.name);
                    Undo.RegisterCreatedObjectUndo(go, "Create Voxel");

                    go.transform.position = worldPosition;

                    CreateVoxelRenderer(voxel, go);

                    Selection.activeGameObject = go;

                    return DragAndDropVisualMode.Copy;
                }

                return DragAndDropVisualMode.None;
            }

            private static void CreateVoxelRenderer(VoxelObject voxelObject, GameObject go)
            {
                VoxelRenderer voxelRenderer = go.AddComponent<VoxelRenderer>();
                voxelRenderer.SetVoxelObject(voxelObject);
            }
        }
    }
}
