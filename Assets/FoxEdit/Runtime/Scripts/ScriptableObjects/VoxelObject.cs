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
        public struct MeshData
        {
            public int[] InstanceStartIndices;
            public int[] InstanceCount;
            public Vector3[] Vertices;
            public int[] Quads;
        }

        [Serializable]
        public struct AnimationFrames
        {
            public string AnimName;
            public int FrameCount;
            public MeshData OpaqueMesh;
            public MeshData TransparentMesh;
            public Bounds Bounds;
            public bool HasOpaqueFaces;
            public bool HasTransparentFaces;
        }

        public int PaletteIndex = 0;
        public Material StaticOpaqueMaterial = null;
        public Material StaticTransparentMaterial = null;
        public Mesh StaticMesh = null;

        public AnimationFrames[] Animations = null;
        public EditorFrameVoxels[] EditorVoxelPositions = null;
    }
}
