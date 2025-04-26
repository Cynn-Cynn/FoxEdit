using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    public class VoxelData
    {
        private List<int> _faces = null;
        public Vector3Int Position { get; private set; } = Vector3Int.zero;
        public int ColorIndex { get; set; } = 0;

        public VoxelData(Vector3Int position)
        {
            _faces = new List<int>();
            Position = position;
        }

        public void AddFace(int index)
        {
            if (index >= 6 || _faces.Contains(index))
                return;

            _faces.Add(index);
        }

        public int[] GetFaces()
        {
            return _faces.ToArray();
        }
    }
}
