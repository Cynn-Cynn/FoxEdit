using FoxEdit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FoxEdit
{
    internal class Grid3D : IEnumerable
    {
        private Dictionary<Vector3Int, VoxelEditorObject> _grid = null;
        private Vector3Int _min = Vector3Int.zero;
        private Vector3Int _max = Vector3Int.zero;
        private Plane[] _facePlanes = new Plane[6]
        {
            new Plane(new Vector3(1, 0, 0), new Vector3(0.05f, 0, 0)),
            new Plane(new Vector3(-1, 0, 0), new Vector3(-0.05f, 0, 0)),
            new Plane(new Vector3(0, 1, 0), new Vector3(0, 0.1f, 0)),
            new Plane(new Vector3(0, -1, 0), new Vector3(0, 0, 0)),
            new Plane(new Vector3(0, 0, 1), new Vector3(0, 0, 0.05f)),
            new Plane(new Vector3(0, 0, -1), new Vector3(0, 0, -0.05f))
        };

        internal Grid3D()
        {
            _grid = new Dictionary<Vector3Int, VoxelEditorObject>();
        }

        internal int Count { get { return _grid.Count; } }
        internal IEnumerable<Vector3Int> Keys { get { return _grid.Keys; } }
        internal IEnumerable<VoxelEditorObject> Values { get { return _grid.Values; } }
        internal Vector3Int Min { get { return _min; } }
        internal Vector3Int Max { get { return _max; } }

        public VoxelEditorObject this[Vector3Int position]
        {
            get { return _grid.TryGetValue(position, out VoxelEditorObject voxel) ? voxel : null; }
            set { _grid[position] = value; TryExpendBounds(position); }
        }

        public IEnumerator GetEnumerator()
        {
            return new GridEnumerator(_grid, _min, _max);
        }

        internal bool IsEmpty(Vector3Int position)
        {
            return !_grid.ContainsKey(position);
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
            _min.x = Mathf.Min(_min.x, newPosition.x);
            _min.y = Mathf.Min(_min.y, newPosition.y);
            _min.z = Mathf.Min(_min.z, newPosition.z);

            _max.x = Mathf.Max(_max.x, newPosition.x);
            _max.y = Mathf.Max(_max.y, newPosition.y);
            _max.z = Mathf.Max(_max.z, newPosition.z);
        }

        private void TryShrinkBounds(Vector3Int deletedPosition)
        {
            IEnumerable<Vector3Int> keys = _grid.Keys;

            if (deletedPosition.x == _min.x)
                _min.x = keys.Select(p => p.x).Min();
            else if (deletedPosition.x == _max.x)
                _min.x = keys.Select(p => p.x).Max();

            if (deletedPosition.y == _min.y)
                _min.y = keys.Select(p => p.y).Min();
            else if (deletedPosition.y == _max.y)
                _min.y = keys.Select(p => p.y).Max();

            if (deletedPosition.z == _min.z)
                _min.z = keys.Select(p => p.z).Min();
            else if (deletedPosition.z == _max.z)
                _min.z = keys.Select(p => p.z).Max();
        }

        internal void Clear()
        {
            _grid.Clear();
        }

        internal bool VoxelRaycast(Vector3Int gridSpacePosition, Vector3 voxelSpacePosition, Vector3 direction, VoxelEditorFrame parent, out VoxelEditorObject voxel, out Vector3 faceNormal)
        {
            bool[] boundDirections = new bool[3]
            {
                direction.x >= 0.0f,
                direction.y >= 0.0f,
                direction.z >= 0.0f
            };

            Plane[] planes = new Plane[3]
            {
                boundDirections[0] ? _facePlanes[0] : _facePlanes[1],
                boundDirections[1] ? _facePlanes[2] : _facePlanes[3],
                boundDirections[2] ? _facePlanes[4] : _facePlanes[5]
            };

            faceNormal = Vector3.zero;
            voxel = GetIntersectedVoxel(gridSpacePosition, voxelSpacePosition, direction, planes, boundDirections, parent, ref faceNormal);

            if (voxel != null)
                return true;
            return false;
        }

        private VoxelEditorObject GetIntersectedVoxel(Vector3Int gridSpacePosition, Vector3 voxelSpacePosition, Vector3 direction, Plane[] planes, bool[] boundDirections, VoxelEditorFrame parent, ref Vector3 faceNormal)
        {
            if (IsOutOfBounds(gridSpacePosition, boundDirections))
                return null;

            Vector3 wsPosition = parent.GridToWorldPosition(gridSpacePosition);
            //Debug.DrawRay(wsPosition + voxelSpacePosition, direction * 0.12f, Color.red, 20.0f);

            if (_grid.TryGetValue(gridSpacePosition, out VoxelEditorObject voxel))
            {
                return voxel;
            }
            
            Ray ray = new Ray(voxelSpacePosition, direction);

            for (int i = 0; i < 3; i++)
            {
                float enter;
                if (planes[i].Raycast(ray, out enter))
                {
                    Vector3 newVoxelSpacePosition = ray.GetPoint(enter);
                    string axis = i == 0 ? "x" : i == 1 ? "y" : "z";
                    //Debug.Log($"{axis}: ({newVoxelSpacePosition.x}; {newVoxelSpacePosition.y}; {newVoxelSpacePosition.z}) | {Mathf.Abs(newVoxelSpacePosition.x) <= 0.051f}; {Mathf.Abs(newVoxelSpacePosition.y - 0.05f) <= 0.05f}; {Mathf.Abs(newVoxelSpacePosition.z) <= 0.051f}");
                    if (Mathf.Abs(newVoxelSpacePosition.x) <= 0.051f && Mathf.Abs(newVoxelSpacePosition.y - 0.05f) <= 0.051f && Mathf.Abs(newVoxelSpacePosition.z) <= 0.051f)
                    {
                        int gridOffset = boundDirections[i] ? 1 : -1;
                        float voxelPositionMirror = boundDirections[i] ? -0.05f : 0.05f;
                        if (i == 0)
                        {
                            newVoxelSpacePosition.x = voxelPositionMirror;
                            gridSpacePosition.x += gridOffset;
                        }
                        else if (i == 1)
                        {
                            newVoxelSpacePosition.y = voxelPositionMirror + 0.05f;
                            gridSpacePosition.y += gridOffset;
                        }
                        else if (i == 2)
                        {
                            newVoxelSpacePosition.z = voxelPositionMirror;
                            gridSpacePosition.z += gridOffset;
                        }

                        faceNormal = -planes[i].normal;
                        return GetIntersectedVoxel(gridSpacePosition, newVoxelSpacePosition, direction, planes, boundDirections, parent, ref faceNormal);
                    }
                }
            }

            //Debug.Log("bugged out");
            return null;
        }

        private bool IsOutOfBounds(Vector3Int position, bool[] boundDirections)
        {
            if ((boundDirections[0] && position.x > _max.x) || (!boundDirections[0] && position.x < _min.x) ||
                (boundDirections[1] && position.y > _max.y) || (!boundDirections[1] && position.y < _min.y) ||
                (boundDirections[2] && position.z > _max.z) || (!boundDirections[2] && position.z < _min.z))
                return true;

            return false;
        }

        internal Dictionary<Vector3Int, int> GetPositionToColor()
        {
            Dictionary<Vector3Int, int> positionToColor = new Dictionary<Vector3Int, int>();

            foreach (Vector3Int position in _grid.Keys)
            {
                positionToColor.Add(position, _grid[position].ColorIndex);
            }

            return positionToColor;
        }
    }
}
