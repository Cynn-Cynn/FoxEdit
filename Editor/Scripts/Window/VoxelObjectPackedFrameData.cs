using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal struct VoxelObjectPackedFrameData
    {
        public Vector3Int MinBounds;
        public Vector3Int MaxBounds;
        public Dictionary<Vector3Int, int> VoxelPositionToColor;
    }
}
