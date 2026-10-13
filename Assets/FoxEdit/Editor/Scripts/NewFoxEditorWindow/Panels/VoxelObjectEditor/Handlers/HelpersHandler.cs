using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class HelpersHandler : baseVoxelObjectEditorPanelHandler
    {
        private const string GRID_COLOR_KEY = "FoxEdit.GridColorKey";
        private const string GRID_VISIBILITY_KEY = "FoxEdit.GridVisibilityKey";
        private const float GIZMO_DRAW_TIME = 0.01f;

        private Toggle showGridToggle;
        private ColorField gridColor;
        private double lastGizmoDrawTime;

        private Color GridColor
        {
            get
            {
                string colorString = EditorPrefs.GetString(GRID_COLOR_KEY, "#000000FF");
                if(ColorUtility.TryParseHtmlString(colorString, out Color color))
                    return color;
                return Color.black;
            }
            set
            {
                EditorPrefs.SetString(GRID_COLOR_KEY, string.Format("#{0}", ColorUtility.ToHtmlStringRGBA(value)));
            }
        }

        private bool GridVisibility
        {
            get => EditorPrefs.GetBool(GRID_VISIBILITY_KEY, true);
            set => EditorPrefs.SetBool(GRID_VISIBILITY_KEY, value);
        }

        public HelpersHandler(VisualElement root) : base(root)
        {
        }

        public override void GetElements()
        {
            showGridToggle = _root.Q<Toggle>("show-grid");
            gridColor = _root.Q<ColorField>("grid-color");
        }

        public override void SetupFields()
        {
        }

        public override void RegisterCallbacks()
        {
            showGridToggle.RegisterValueChangedCallback(OnShowGridChanged);
            gridColor.RegisterValueChangedCallback(OnColorChanged);
        }


        public override void UnregisterCallbacks()
        {
            showGridToggle.UnregisterValueChangedCallback(OnShowGridChanged);
            gridColor.UnregisterValueChangedCallback(OnColorChanged);
        }


        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            gridColor.value = GridColor;
            showGridToggle.value = GridVisibility;
        }

        private void OnShowGridChanged(ChangeEvent<bool> evt)
        {
            _voxelEditor.SetEdgesVisibility(evt.newValue);
            GridVisibility = evt.newValue;
        }

        private void OnColorChanged(ChangeEvent<Color> evt)
        {
            _voxelEditor.SetEdgesColor(evt.newValue);
            GridColor = evt.newValue;
        }


        protected override void OnStopEditVoxelObject()
        {
        }

    }
}