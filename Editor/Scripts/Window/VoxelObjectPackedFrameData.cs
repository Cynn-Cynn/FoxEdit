using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelObjectPackedFrameData
    {
        public VoxelData[] Data;
        public Vector3Int MinBounds;
        public Vector3Int MaxBounds;
        public Vector3Int[] VoxelPositions;
        public int[] ColorIndices;
    }
}
