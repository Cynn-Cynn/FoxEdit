
using System.Linq;
using FoxEdit.WindowComponents;
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
            VoxelEditor.OnChangeColor += OnChangeColor;
        }

        public override void UnregisterCallbacks()
        {
            _paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            _colorSelector.OnIndexChanged -= OnColorSelectorValueChanged;
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
        }

        private void OnPaletteValueChanged(ChangeEvent<string> evt)
        {
            int index = _paletteDropdown.choices.IndexOf(evt.newValue);

            if (index == -1)
                return;
            VoxelEditor.PaletteIndex = index;
            UpdateColorSelector();
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