using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelEditorObject
    {
        internal int ColorIndex { get; private set; } = 0;
        internal Vector3 WorldPosition { get; private set; }
        internal Vector3Int GridPosition { get; private set; }

        internal VoxelEditorObject(Vector3 worldPosition, Vector3Int gridPosition)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
            ColorIndex = 0;
        }

        internal void SetColor(int colorIndex)
        {
            ColorIndex = colorIndex;
        }

        internal void SetPosition(Vector3 worldPosition, Vector3Int gridPosition)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
        }
    }
}
