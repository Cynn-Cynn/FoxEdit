using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelEditorObject
    {
        internal int ColorIndex { get; private set; } = 0;
        private MeshRenderer _voxelRenderer = null;

        internal VoxelEditorObject(MeshRenderer voxelRenderer, Vector3 localPosition)
        {
            _voxelRenderer = voxelRenderer;
            _voxelRenderer.transform.localPosition = localPosition;
            ColorIndex = 0;
        }

        internal void SetColor(Material material, int colorIndex)
        {
            _voxelRenderer.material = material;
            ColorIndex = colorIndex;
        }

        internal void Destroy()
        {
            GameObject.DestroyImmediate(_voxelRenderer.gameObject);
        }
    }
}
