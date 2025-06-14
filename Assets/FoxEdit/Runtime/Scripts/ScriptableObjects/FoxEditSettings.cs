using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    [CreateAssetMenu(fileName = "VoxelGlobalData", menuName = "FoxEdit/Global Data")]
    public class FoxEditSettings : ScriptableObject
    {
        [SerializeField] private List<VoxelPalette> _palettes;

        public VoxelPalette[] Palettes { get { return _palettes.ToArray(); } }

        public void AddPalette(VoxelPalette palette)
        {
            if (!_palettes.Contains(palette))
                _palettes.Add(palette);
        }

        public void RemoveAt(int index)
        {
            _palettes.RemoveAt(index);
        }

        public void SetPalette(VoxelPalette palette, int index)
        {
            if (index >= _palettes.Count)
                return;

            _palettes[index] = palette;
        }

#if UNITY_EDITOR
        public static FoxEditSettings GetSettings()
        {
            return Resources.Load<FoxEditSettings>("FoxEditSettings");
        }
#endif
    }
}
