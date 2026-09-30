
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit.Commands
{
    internal abstract class baseVoxelEditorCommand : ICommand
    {
        protected Grid3D _grid;
        protected List<Vector3Int> _editedVoxels;
        protected Transform _voxelTransform;

        public baseVoxelEditorCommand(Grid3D grid, List<Vector3Int> editedVoxels, Transform voxelTransform)
        {
            _grid = grid;
            _editedVoxels = editedVoxels;
            _voxelTransform = voxelTransform;
        }


        public baseVoxelEditorCommand(Grid3D grid, Vector3Int editedVoxel, Transform voxelTransform)
        {
            _grid = grid;
            _editedVoxels = new List<Vector3Int>() { editedVoxel };
            _voxelTransform = voxelTransform;
        }

        public abstract void Execute();
        public abstract void Undo();

        protected VoxelEditorObject CreateVoxelObject(Vector3Int gridPosition)
        {
            Vector3 worldPosition = GridToWorldPosition(gridPosition);
            VoxelEditorObject voxelObject = new VoxelEditorObject(worldPosition, gridPosition);

            return voxelObject;
        }

        private Vector3 GridToWorldPosition(Vector3Int gridPosition)
        {
            Vector3 localPosition = (Vector3)gridPosition;
            localPosition *= 0.1f;
            return _voxelTransform.TransformPoint(localPosition);
        }
    }
}