using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FoxEdit
{
    [CreateAssetMenu(fileName = "Voxel Palette", menuName = "FoxEdit/Palette")]
    public class VoxelPalette : ScriptableObject
    {
        [SerializeField] private List<VoxelColor> _colors = null;
        public VoxelColor[] Colors { get { return _colors.ToArray(); } }

        public int PaletteSize { get { return _colors.Count; } }

        public void AddColor(VoxelColor color)
        {
            _colors.Add(color);
        }

        public void RemoveColor(int index)
        {
            _colors.RemoveAt(index);
        }

        public void SetColor(int index, VoxelColor color)
        {
            if (index >= _colors.Count)
                return;

            _colors[index] = color;
        }
    }
}
