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
            public int FrameCount;
            public int[] InstanceStartIndices;
            public int[] InstanceCount;
            public Vector3[] Vertices;
            public int[] Quads;
            public Bounds Bounds;
        }

        public int PaletteIndex = 0;
        public Material AnimatedMaterial = null;
        public Material StaticMaterial = null;
        public Mesh StaticMesh = null;

        public AnimationFrames[] Animations = null;
        public EditorFrameVoxels[] EditorVoxelPositions = null;
    }
}
