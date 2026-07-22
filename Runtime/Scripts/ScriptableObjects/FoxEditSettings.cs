using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    [CreateAssetMenu(fileName = "VoxelGlobalData", menuName = "FoxEdit/Global Data")]
    public class FoxEditSettings : ScriptableObject
    {
        private static FoxEditSettings instance = null;
        [System.Serializable]
        public class MaterialsSettings
        {
            public Material animatedOpaqueMaterial;
            public Material animatedTransparentMaterial;
            public Material staticOpaqueMaterial;
            public Material staticTransparentMaterial;
        }

        [SerializeField] private List<VoxelPalette> _palettes;

        public VoxelPalette[] Palettes { get { return _palettes.ToArray(); } }
        public MaterialsSettings Materials;

        public void AddPalette(VoxelPalette palette)
        {
            if (!_palettes.Contains(palette))
                _palettes.Add(palette);
        }

        public void SetPalette(VoxelPalette palette, int index)
        {
            if (index >= _palettes.Count)
                return;

            _palettes[index] = palette;
        }

        public void RemovePalette(VoxelPalette voxelPalette)
        {
            if (!_palettes.Contains(voxelPalette))
                return;
            _palettes.Remove(voxelPalette);
        }

        public void RemovePaletteAt(int voxelPaletteIndex)
        {
            if (voxelPaletteIndex >= _palettes.Count)
                return;

            _palettes.RemoveAt(voxelPaletteIndex);
        }

        public static FoxEditSettings GetSettings()
        {
            if (instance == null)
                instance = Resources.Load<FoxEditSettings>("FoxEditSettings");
            return instance;
        }
    }
}
