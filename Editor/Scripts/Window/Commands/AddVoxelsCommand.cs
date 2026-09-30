using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal class AddVoxelsCommand : baseVoxelEditorCommand
    {
        private int _color;

        public AddVoxelsCommand(Grid3D grid, Vector3Int editedVoxel, int color, Transform voxelTransform) : base(grid, editedVoxel, voxelTransform)
        {
            _color = color;
        }

        public AddVoxelsCommand(Grid3D grid, HashSet<Vector3Int> editedVoxels, Transform voxelTransform) : base(grid, editedVoxels, voxelTransform)
        {
        }

        public override void Execute()
        {
            foreach (Vector3Int editedVoxel in _editedVoxels)
            {
                if (_grid.IsEmpty(editedVoxel))
                {
                    _grid[editedVoxel] = CreateVoxelObject(editedVoxel);
                    _grid[editedVoxel].SetColor(_color);
                }
            }
        }

        public override void Undo()
        {
            foreach (Vector3Int editedVoxel in _editedVoxels)
            {
                if (!_grid.IsEmpty(editedVoxel))
                {
                    _grid.Remove(editedVoxel);
                }
            }
        }
    }
}