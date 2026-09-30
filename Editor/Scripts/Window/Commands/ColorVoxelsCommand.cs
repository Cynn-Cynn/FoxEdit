
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal class ColorVoxelsCommand : baseVoxelEditorCommand
    {
        private List<int> _baseVoxelsColor = new List<int>();
        private int _newColor;


        public ColorVoxelsCommand(Grid3D grid, Vector3Int editedVoxel, int newColor, Transform voxelTransform) : base(grid, editedVoxel, voxelTransform)
        {
            _baseVoxelsColor.Add(grid[editedVoxel].ColorIndex);
            _newColor = newColor;
        }

        public ColorVoxelsCommand(Grid3D grid, HashSet<Vector3Int> editedVoxels, int newColor, Transform voxelTransform) : base(grid, editedVoxels, voxelTransform)
        {
            foreach (Vector3Int editedVoxel in editedVoxels)
                _baseVoxelsColor.Add(grid[editedVoxel].ColorIndex);
            _newColor = newColor;
        }

        public override void Execute()
        {
            foreach (Vector3Int editedVoxel in _editedVoxels)
            {
                if (!_grid.IsEmpty(editedVoxel))
                {
                    _grid[editedVoxel].SetColor(_newColor);
                    Debug.LogFormat("Set color {0}", _newColor);
                }
            }
        }

        public override void Undo()
        {
            int color = -1;
            int i = 0;

            foreach (Vector3Int position in _editedVoxels)
            {
                color = _baseVoxelsColor[i];

                if (!_grid.IsEmpty(position))
                {
                    _grid[position].SetColor(color);
                    Debug.LogFormat("Set color {0}", color);
                }
                i++;
            }
        }
    }
}