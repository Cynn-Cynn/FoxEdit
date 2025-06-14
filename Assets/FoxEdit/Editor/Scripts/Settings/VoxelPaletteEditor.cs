using FoxEdit;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    [CustomEditor(typeof(VoxelPalette))]
    public class VoxelPaletteEditor : Editor
    {
        VoxelPalette _palette = null;
        VoxelColor[] _colors = null;

        private void OnEnable()
        {
            _palette = target as VoxelPalette;
            _colors = _palette.Colors;
        }

        public override void OnInspectorGUI()
        {
            ColorsDisplay();

            if (GUI.changed)
                Save();
        }

        private void ColorsDisplay()
        {
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);

            for (int i = 0; i < _colors.Length; i++)
            {
                Color color = EditorGUILayout.ColorField("Color", _colors[i].Color);
                float emissiveIntensity = EditorGUILayout.FloatField("Emissive Intensity", _colors[i].EmissiveIntensity);
                float metallic = EditorGUILayout.Slider("Metallic", _colors[i].Metallic, 0.0f, 1.0f);
                float smoothness = EditorGUILayout.Slider("Smoothness", _colors[i].Smoothness, 0.0f, 1.0f);

                if (GUI.changed)
                {
                    _palette.SetColor(i, new VoxelColor(color, emissiveIntensity, metallic, smoothness));
                    _colors = _palette.Colors;
                }

                if (GUILayout.Button("Remove"))
                {
                    _palette.RemoveAt(i);
                    _colors = _palette.Colors;
                }

                EditorGUILayout.LabelField("");
            }

            if (GUILayout.Button("Add color"))
            {
                _palette.AddColor(new VoxelColor());
                _colors = _palette.Colors;
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(_palette);
            AssetDatabase.SaveAssets();
        }
    }
}
