using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    [CustomEditor(typeof(VoxelRenderer))]
    public class VoxelRendererEditor : Editor
    {
        private VoxelRenderer _voxelRenderer = null;

        private bool _staticRender = false;
        private float _frameDuration = 0.0f;
        private SerializedProperty _frameDurationProperty = null;

        private string[] _paletteNames = null;
        private int _paletteIndexOverride = 0;

        private void OnEnable()
        {
            _voxelRenderer = target as VoxelRenderer;

            _staticRender = serializedObject.FindProperty("_staticRender").boolValue;

            _frameDurationProperty = serializedObject.FindProperty("_frameDuration");
            _frameDuration = _frameDurationProperty.floatValue;

            PaletteSetup();
        }

        private void PaletteSetup()
        {
            _paletteNames = VoxelSharedData.GetPaletteNames();
            _paletteIndexOverride = serializedObject.FindProperty("_paletteIndexOverride").intValue;
        }

        public override void OnInspectorGUI()
        {
            VoxelObjectDisplay();

            if (_voxelRenderer.VoxelObject == null)
                return;

            PaletteIndexOverrideDisplay();
            StaticRenderDisplay();
            if (!_staticRender)
                FrameTimeDisplay();
            if (GUILayout.Button("Edit Voxel"))
                FoxEditManager.StartEditVoxelObject(_voxelRenderer);
        }

        private void VoxelObjectDisplay()
        {
            VoxelObject voxelObject = EditorGUILayout.ObjectField("Voxel Object", _voxelRenderer.VoxelObject, typeof(VoxelObject), false) as VoxelObject;
            if (voxelObject != _voxelRenderer.VoxelObject)
                _voxelRenderer.VoxelObject = voxelObject;
        }

        private void PaletteIndexOverrideDisplay()
        {
            int paletteIndexOverride = EditorGUILayout.Popup("Palette Index", _paletteIndexOverride, _paletteNames);
            if (paletteIndexOverride != _paletteIndexOverride)
            {
                _voxelRenderer.SetPalette(paletteIndexOverride);
                _paletteIndexOverride = paletteIndexOverride;
                Save();
            }

            if (paletteIndexOverride != _voxelRenderer.VoxelObject.PaletteIndex)
            {
                Color baseColor = GUI.contentColor;
                GUI.contentColor = Color.cyan;
                EditorGUILayout.LabelField($"Override {_paletteNames[_voxelRenderer.VoxelObject.PaletteIndex]} with {_paletteNames[_paletteIndexOverride]}");
                GUI.contentColor = baseColor;
            }
        }

        private void StaticRenderDisplay()
        {
            string buttonText = "Switch to " + (_staticRender ? "animated" : "static") + " render";
            if (GUILayout.Button(buttonText))
            {
                _voxelRenderer.RenderSwap();
                _staticRender = !_staticRender;
                Save();
            }
        }

        private void FrameTimeDisplay()
        {
            float frameDuration = EditorGUILayout.FloatField("Frame duration", _frameDuration);
            if (frameDuration != _frameDuration)
            {
                _frameDurationProperty.floatValue = frameDuration;
                _frameDuration = frameDuration;
                Save();
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(_voxelRenderer.gameObject);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

    }
}
