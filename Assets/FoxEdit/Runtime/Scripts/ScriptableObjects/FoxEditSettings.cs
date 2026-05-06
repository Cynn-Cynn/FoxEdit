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
            public Material animatedMaterial;
            public Material staticMaterial;
        }

        [SerializeField] private List<VoxelPalette> _palettes;

        public VoxelPalette[] Palettes { get { return _palettes.ToArray(); } }
        public MaterialsSettings Materials;
        public ComputeShader computeShader;
        public ComputeShader staticShader;

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

        public static FoxEditSettings GetSettings()
        {
            if (instance == null)
                instance = Resources.Load<FoxEditSettings>("FoxEditSettings");
            return instance;
        }
    }
}
