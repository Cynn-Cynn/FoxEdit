using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FoxEdit
{
    public class VoxelObject : ScriptableObject
    {
        [Serializable]
        public struct EditorFrameVoxels
        {
            public Vector3Int[] VoxelPositions;
            public int[] ColorIndices;
        }

        public Bounds Bounds;

        public int PaletteIndex = 0;
        public Vector3[] VoxelPositions = null;
        public int[] VoxelIndices = null;
        public int[] FaceIndices = null;

        public int[] ColorIndices = null;

        public int FrameCount = 0;

        public int[] InstanceStartIndices;
        public int[] InstanceCount = null;
        public int MaxInstanceCount = 0;

        public EditorFrameVoxels[] EditorVoxelPositions = null;
        public Mesh StaticMesh = null;
    }
}
