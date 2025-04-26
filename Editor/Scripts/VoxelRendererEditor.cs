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
        private VoxelSharedData _sharedData = null;

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

            SetupRenderer();
            PaletteSetup();
        }

        private void PaletteSetup()
        {
            _sharedData = FindObjectOfType<VoxelSharedData>();
            _paletteNames = _sharedData.GetPaletteNames();
            _paletteIndexOverride = serializedObject.FindProperty("_paletteIndexOverride").intValue;
        }

        private void SetupRenderer()
        {
            SerializedProperty isSetupProperty = serializedObject.FindProperty("_isSetup");

            if (isSetupProperty.boolValue)
                return;

            serializedObject.FindProperty("_meshFilter").objectReferenceValue = _voxelRenderer.GetComponent<MeshFilter>();

            string computeShaderPath = AssetDatabase.GUIDToAssetPath("eeb2b127e157ea043be7bf2207221e36");
            ComputeShader computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(computeShaderPath);
            ComputeShader computeShaderInstance = Instantiate(computeShader);
            serializedObject.FindProperty("_computeShader").objectReferenceValue = computeShaderInstance;

            string materialPath = AssetDatabase.GUIDToAssetPath("3f0281ad1cf83de4795cd67224e84cb6");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Material materialInstance = Instantiate(material);
            serializedObject.FindProperty("_material").objectReferenceValue = materialInstance;

            string staticMaterialPath = AssetDatabase.GUIDToAssetPath("90c432c76eaa99648809ebe571f57d1e");
            Material staticMaterial = AssetDatabase.LoadAssetAtPath<Material>(staticMaterialPath);
            Material staticMaterialInstance = Instantiate(staticMaterial);
            serializedObject.FindProperty("_staticMaterial").objectReferenceValue = staticMaterialInstance;

            MeshRenderer meshRenderer = _voxelRenderer.GetComponent<MeshRenderer>();
            serializedObject.FindProperty("_meshRenderer").objectReferenceValue = meshRenderer;
            meshRenderer.material = staticMaterialInstance;
            meshRenderer.enabled = false;

            isSetupProperty.boolValue = true;

            Save();
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
        }

        private void VoxelObjectDisplay()
        {
            VoxelObject voxelObject = EditorGUILayout.ObjectField("Voxel Object", _voxelRenderer.VoxelObject, typeof(VoxelObject), false) as VoxelObject;
            if (voxelObject != _voxelRenderer.VoxelObject)
                _voxelRenderer.VoxelObject = voxelObject;
        }

        private void PaletteIndexOverrideDisplay()
        {
            int paletteIndexOverride = EditorGUILayout.Popup("Palette index", _paletteIndexOverride, _paletteNames);
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
