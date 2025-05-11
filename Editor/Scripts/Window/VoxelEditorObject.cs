using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelEditorObject
    {
        internal int ColorIndex { get; private set; } = 0;
        internal GameObject GameObject { get { return _voxelRenderer?.gameObject; } }
        internal Vector3 WorldPosition { get { return _voxelRenderer.transform.position; } }
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

        internal void SetLocalPosition(Vector3 localPosition)
        {
            _voxelRenderer.transform.localPosition = localPosition;
        }

        internal void ResetRotation()
        {
            _voxelRenderer.transform.rotation = Quaternion.identity;
        }
    }
}
