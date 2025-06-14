using FoxEdit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace FoxEdit
{
    internal class Grid3D : IEnumerable
    {
        private Dictionary<Vector3Int, VoxelEditorObject> _grid = null;

        private Vector3Int _min = Vector3Int.zero;
        private Vector3Int _max = Vector3Int.zero;

        internal int Count { get { return _grid.Count; } }
        internal IEnumerable<Vector3Int> Keys { get { return _grid.Keys; } }
        internal IEnumerable<VoxelEditorObject> Values { get { return _grid.Values; } }
        internal Vector3Int Min { get { return _min; } }
        internal Vector3Int Max { get { return _max; } }

        internal Grid3D()
        {
            _grid = new Dictionary<Vector3Int, VoxelEditorObject>();
        }

        public IEnumerator GetEnumerator()
        {
            return new GridEnumerator(_grid, _min, _max);
        }

        internal bool IsEmpty(Vector3Int position)
        {
            return !_grid.ContainsKey(position);
        }

        internal VoxelEditorObject Get(Vector3Int position)
        {
            if (_grid.ContainsKey(position))
                return _grid[position];
            return null;
        }

        internal void Set(Vector3Int position, VoxelEditorObject voxel)
        {
            _grid[position] = voxel;
            TryExpendBounds(position);
        }

        internal void Remove(Vector3Int position)
        {
            if (_grid.ContainsKey(position))
            {
                _grid.Remove(position);
                TryShrinkBounds(position);
            }
        }

        private void TryExpendBounds(Vector3Int newPosition)
        {
            if (newPosition.x < _min.x)
                _min.x = newPosition.x;
            else if (newPosition.x > _max.x)
                _max.x = newPosition.x;

            if (newPosition.y < _min.y)
                _min.y = newPosition.y;
            else if (newPosition.y > _max.y)
                _max.y = newPosition.y;

            if (newPosition.z < _min.z)
                _min.z = newPosition.z;
            else if (newPosition.z > _max.z)
                _max.z = newPosition.z;
        }

        private void TryShrinkBounds(Vector3Int deletedPosition)
        {
            IEnumerable<Vector3Int> keys = _grid.Keys;
            IEnumerable<int> xKeys = keys.Select(position => position.x);
            IEnumerable<int> yKeys = keys.Select(position => position.y);
            IEnumerable<int> zKeys = keys.Select(position => position.z);

            if (deletedPosition.x == _min.x)
                _min.x = xKeys.Min();
            else if (deletedPosition.x == _max.x)
                _max.x = xKeys.Max();

            if (deletedPosition.y == _min.y)
                _min.y = yKeys.Min();
            else if (deletedPosition.y == _max.y)
                _max.y = yKeys.Max();

            if (deletedPosition.z == _min.z)
                _min.z = zKeys.Min();
            else if (deletedPosition.z == _max.z)
                _max.z = zKeys.Max();
        }

        internal void Clear()
        {
            _grid.Clear();
        }
    }
}
