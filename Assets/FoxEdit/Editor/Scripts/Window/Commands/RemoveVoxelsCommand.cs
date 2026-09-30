using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal class RemoveVoxelsCommand : baseVoxelEditorCommand
    {
        private List<int> _colors = new List<int>();

        public RemoveVoxelsCommand(Grid3D grid, Vector3Int editedVoxel, Transform voxelTransform) : base(grid, editedVoxel, voxelTransform)
        {
            _colors = new List<int>() { grid[editedVoxel].ColorIndex };
        }

        public RemoveVoxelsCommand(Grid3D grid, HashSet<Vector3Int> editedVoxels, Transform voxelTransform) : base(grid, editedVoxels, voxelTransform)
        {
            foreach (Vector3Int position in editedVoxels)
            {
                _colors.Add(grid[position].ColorIndex);
            }
        }

        public override void Execute()
        {
            foreach (Vector3Int editedVoxel in _editedVoxels)
            {
                if (!_grid.IsEmpty(editedVoxel))
                    _grid.Remove(editedVoxel);
            }
        }

        public override void Undo()
        {
            int i = 0;
            foreach (Vector3Int position in _editedVoxels)
            {
                _grid[position] = CreateVoxelObject(position);
                _grid[position].SetColor(_colors[i]);
                i++;
            }
        }
    }
}