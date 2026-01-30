using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FoxEdit
{
    public static class VoxelObjectExtentions
    {

        public static async Task<Texture2D> GetPreviewIcon(this VoxelObject obj)
        {
            GameObject voxelRendererGO = new GameObject("Preview voxel");
            VoxelRenderer voxelRenderer = voxelRendererGO.AddComponent<VoxelRenderer>();
            voxelRenderer.SetVoxelObject(obj);
            //voxelRenderer.Setup();
            voxelRenderer.SetRenderParams();
            voxelRenderer.RenderSwap();

            await Task.Delay(1000);

            Texture2D texture = ThumbnailsTaker.GetThumbnail(voxelRendererGO);

            GameObject.DestroyImmediate(voxelRendererGO);
            return texture;
        }


    }
}