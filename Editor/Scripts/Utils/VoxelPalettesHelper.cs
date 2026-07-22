using UnityEngine;
using UnityEditor;

namespace FoxEdit
{
    internal static class VoxelPalettesHelper
    {
        private static FoxEditSettings _settings;

        private static FoxEditSettings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = FoxEditSettings.GetSettings();
                return _settings;
            }
        }

        public static void RemovePaletteAt(int paletteIndex)
        {
            VoxelPalette voxelPalette = Settings.Palettes[paletteIndex];

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
            Settings.RemovePaletteAt(paletteIndex);
        }


        public static bool RenamePaletteAt(int paletteIndex, string newName)
        {
            if (paletteIndex >= Settings.Palettes.Length)
                return false;

            VoxelPalette voxelPalette = Settings.Palettes[paletteIndex];

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


        public static void DuplicatePalette(int paletteIndex)
        {
            VoxelPalette voxelPalette = Settings.Palettes[paletteIndex];

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

            Settings.AddPalette(AssetDatabase.LoadAssetAtPath<VoxelPalette>(destinationPath));
        }
    }
}