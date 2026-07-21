
using System;
using System.Diagnostics;
using System.Linq;
using FoxEdit.WindowComponents;
using UnityEditor;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.VoxelObjectEditorPanelHandlers
{
    internal class ColorsHandler : baseVoxelObjectEditorPanelHandler
    {
        private DropdownField _paletteDropdown;
        private ColorPaletteElement _colorSelector;

        public ColorsHandler(VisualElement root) : base(root)
        {
        }

        protected override void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
        }

        public override void GetElements()
        {
            _paletteDropdown = _root.Q<DropdownField>("palette-selector");
            _colorSelector = _root.Q<ColorPaletteElement>();
        }

        public override void RegisterCallbacks()
        {
            _paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            _colorSelector.OnIndexChanged += OnColorSelectorValueChanged;
            _colorSelector.OnPressEditPaletteItem += OnPressEditPaletteItem;
            _colorSelector.OnPressDeletePaletteItem += OnPressDeletePaletteItem;
            _colorSelector.OnPressAddPaletteItem += OnPressAddPaletteItem;
            VoxelEditor.OnChangeColor += OnChangeColor;
        }


        public override void UnregisterCallbacks()
        {
            _paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            _colorSelector.OnIndexChanged -= OnColorSelectorValueChanged;
            _colorSelector.OnPressEditPaletteItem -= OnPressEditPaletteItem;
            _colorSelector.OnPressDeletePaletteItem -= OnPressDeletePaletteItem;
            VoxelEditor.OnChangeColor -= OnChangeColor;
        }

        public override void SetupFields()
        {
            _paletteDropdown.choices = VoxelSharedData.GetPaletteNames().ToList();
            _paletteDropdown.SetValueWithoutNotify(_paletteDropdown.choices[VoxelEditor.PaletteIndex]);
            UpdateColorSelector();
        }

        private void UpdateColorSelector()
        {
            VoxelPalette selectedPalette = VoxelEditor.CurrentPalette;
            _colorSelector.ClearPaletteItems();
            for (int i = 0; i < selectedPalette.Colors.Length; i++)
                _colorSelector.AddPaletteItem(selectedPalette.Colors[i]);
            _colorSelector.SetIndexValue(VoxelEditor.ColorIndex, false);
            _colorSelector.UpdatePaletteItemsManipulators();
        }

        private void OnPaletteValueChanged(ChangeEvent<string> evt)
        {
            int index = _paletteDropdown.choices.IndexOf(evt.newValue);

            if (index == -1)
                return;
            VoxelEditor.PaletteIndex = index;
            UpdateColorSelector();
            SceneView.RepaintAll();
        }

        private void OnPressAddPaletteItem()
        {
            ColorEditorPopUp.Open(ApplyAddColor, false, false);
        }

        private void ApplyAddColor(VoxelColor newVoxelColor)
        {
            VoxelEditor.CurrentPalette.AddColor(newVoxelColor);
            EditorUtility.SetDirty(VoxelEditor.CurrentPalette);
            VoxelSharedData.RefreshColorBuffer(VoxelEditor.PaletteIndex);
            _voxelEditor.RefreshPreviewColors();
            UpdateColorSelector();
        }

        private void OnPressDeletePaletteItem(int paletteItemIndex)
        {
            if (EditorUtility.DisplayDialog("Delete Color", "This will permanently remove the selected color from the palette. This action cannot be undone. Do you want to continue ?", "Delete", "Cancel"))
            {
                VoxelEditor.CurrentPalette.RemoveAt(paletteItemIndex);
                VoxelSharedData.RefreshColorBuffer(VoxelEditor.PaletteIndex);
                EditorUtility.SetDirty(VoxelEditor.CurrentPalette);
                _voxelEditor.RefreshPreviewColors();
            }
            UpdateColorSelector();
        }

        private void OnPressEditPaletteItem(int paletteItemIndex)
        {
            ColorEditorPopUp.Open((newColor) => ApplyEditPaletteItem(paletteItemIndex, newColor),
            VoxelEditor.CurrentPalette.Colors[paletteItemIndex], true, true);
        }

        private void ApplyEditPaletteItem(int paletteItemIndex, VoxelColor newVoxelColor)
        {
            _colorSelector.SetPaletteItemColor(paletteItemIndex, newVoxelColor);
            VoxelEditor.CurrentPalette.SetColor(paletteItemIndex, newVoxelColor);
            VoxelSharedData.RefreshColorBuffer(VoxelEditor.PaletteIndex);
            EditorUtility.SetDirty(VoxelEditor.CurrentPalette);
            _voxelEditor.RefreshPreviewColors();
        }

        private void OnChangeColor(int colorIndex)
        {
            _colorSelector.SetIndexValue(colorIndex, false);
        }

        private void OnColorSelectorValueChanged(int colorIndex)
        {
            VoxelEditor.ColorIndex = colorIndex;
        }

        protected override void OnStopEditVoxelObject()
        {
        }
    }
}