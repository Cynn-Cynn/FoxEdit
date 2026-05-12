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

        [Serializable]
        public struct AnimationFrames
        {
            public string AnimName;
            public int StartIndex;
            public int FrameCount;
        }

        public EditorFrameVoxels[] EditorVoxelPositions = null;

        public Bounds Bounds;

        public int PaletteIndex = 0;
        public Material AnimatedMaterial = null;
        public Material StaticMaterial = null;

        public Mesh StaticMesh = null;

        public int[] InstanceStartIndices;
        public int[] InstanceCount = null;
        public AnimationFrames[] AnimationIndices = null;
        public Vector3[] Vertices = null;
        public int[] Quads = null;
    }
}
