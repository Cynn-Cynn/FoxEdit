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

        public void RemovePaletteAt(int paletteIndex)
        {
            VoxelPalette voxelPalette = _palettes[paletteIndex];

            if (voxelPalette == null)
            {
                Debug.LogError("The palette you try to duplicate is null");
                return;
            }

            string palettePath = AssetDatabase.GetAssetPath(voxelPalette);

            if (palettePath == null || palettePath == string.Empty)
            {
                Debug.LogError("The palette path is not found");
                return;
            }

            AssetDatabase.DeleteAsset(palettePath);
            AssetDatabase.Refresh();
            _palettes.RemoveAt(paletteIndex);
        }

        public void SetPalette(VoxelPalette palette, int index)
        {
            if (index >= _palettes.Count)
                return;

            _palettes[index] = palette;
        }

        public bool RenamePaletteAt(int paletteIndex, string newName)
        {
            if (paletteIndex >= _palettes.Count)
                return false;

            VoxelPalette voxelPalette = _palettes[paletteIndex];

            if (voxelPalette == null)
            {
                Debug.LogError("The palette you try to rename is null");
                return false;
            }

            string palettePath = AssetDatabase.GetAssetPath(voxelPalette);

            if (palettePath == null || palettePath == string.Empty)
            {
                Debug.LogError("The palette path is not found");
                return false;
            }

            string result = AssetDatabase.RenameAsset(palettePath, newName);
            if (result != string.Empty)
            {
                Debug.LogFormat("An error occured when you tried to rename the palette {0}", voxelPalette.name);
                return false;
            }

            AssetDatabase.Refresh();

            return true;
        }

        public void DuplicatePalette(int paletteIndex)
        {
            VoxelPalette voxelPalette = _palettes[paletteIndex];

            if (voxelPalette == null)
            {
                Debug.LogError("The palette you try to duplicate is null");
                return;
            }

            string palettePath = AssetDatabase.GetAssetPath(voxelPalette);

            if (palettePath == null || palettePath == string.Empty)
            {
                Debug.LogError("The palette path is not found");
                return;
            }

            string destinationPath = AssetDatabase.GenerateUniqueAssetPath(palettePath);
            AssetDatabase.CopyAsset(palettePath, destinationPath);
            AssetDatabase.Refresh();

            AddPalette(AssetDatabase.LoadAssetAtPath<VoxelPalette>(destinationPath));
        }

        public static FoxEditSettings GetSettings()
        {
            if (instance == null)
                instance = Resources.Load<FoxEditSettings>("FoxEditSettings");
            return instance;
        }
    }
}
