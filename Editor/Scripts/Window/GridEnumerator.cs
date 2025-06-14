using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class GridEnumerator : IEnumerator
    {
        private Dictionary<Vector3Int, VoxelEditorObject> _grid = null;

        private Vector3Int _index = Vector3Int.zero;
        private Vector3Int _min = Vector3Int.zero;
        private Vector3Int _max = Vector3Int.zero;

        public object Current { get; private set; }

        internal GridEnumerator(Dictionary<Vector3Int, VoxelEditorObject> grid, Vector3Int min, Vector3Int max)
        {
            _grid = grid;
            _index = min;
            _min = min;
            _max = max;
            Current = null;
        }

        public bool MoveNext()
        {
            _index.x += 1;
            if (_index.x > _max.x)
            {
                _index.x = _min.x;
                _index.y += 1;
                if (_index.y > _max.y)
                {
                    _index.y = _min.y;
                    _index.z += 1;
                    if (_index.z > _max.z)
                    {
                        Current = null;
                        return false;
                    }
                }
            }

            if (_grid.ContainsKey(_index))
                Current = _grid[_index];
            else
                Current = null;

            return true;
        }

        public void Reset()
        {
            Current = null;
            _index = Vector3Int.zero;
            _min = Vector3Int.zero;
            _max = Vector3Int.zero;
        }
    }
}
