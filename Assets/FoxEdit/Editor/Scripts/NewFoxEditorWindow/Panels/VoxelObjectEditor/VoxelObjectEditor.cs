using System;
using System.Linq;
using FoxEdit.WindowComponents;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditor
    {
        private VisualElement root;

        private Button stopButton = null;
        private ToolbarElement toolToolbar;
        private ToolbarElement actionToolbar;
        private DropdownField paletteDropdown;
        private ColorPaletteElement colorSelector;

        public VoxelObjectEditor(VisualElement root)
        {
            this.root = root;
            GetElements();
            RegisterCallbacks();

            SetupFields();
        }

        ~VoxelObjectEditor()
        {
            UnregisterCallbacks();
        }

        private void SetupFields()
        {
            paletteDropdown.choices = VoxelSharedData.GetPaletteNames().ToList();
            paletteDropdown.SetValueWithoutNotify(paletteDropdown.choices[VoxelEditor.PaletteIndex]);

            toolToolbar.SelectTool((int)VoxelEditor.Tool, false);
            actionToolbar.SelectTool((int)VoxelEditor.Action, false);

            UpdateColorSelector();
        }

        private void UpdateColorSelector()
        {
            VoxelPalette selectedPalette = VoxelEditor.CurrentPalette;
            colorSelector.ClearPaletteItems();
            for (int i = 0; i < selectedPalette.Colors.Length; i++)
                colorSelector.AddPaletteItem(selectedPalette.Colors[i]);
            colorSelector.SetIndexValue(VoxelEditor.ColorIndex ,false);
        }

        private void GetElements()
        {
            stopButton = root.Q<Button>("stop-edit-button");
            toolToolbar = root.Q<ToolbarElement>("tools");
            actionToolbar = root.Q<ToolbarElement>("actions");
            paletteDropdown = root.Q<DropdownField>("palette-selector");
            colorSelector = root.Q<ColorPaletteElement>();
        }

#region Callbacks
        private void RegisterCallbacks()
        {
            stopButton.clicked += OnClickStopEdit;
            toolToolbar.OnToolSelected += OnToolSelected;
            actionToolbar.OnToolSelected += OnActionSelected;
            paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            colorSelector.OnIndexChanged += OnColorSelectorValueChanged;
        }


        private void UnregisterCallbacks()
        {
            stopButton.clicked -= OnClickStopEdit;
            toolToolbar.OnToolSelected += OnToolSelected;
            actionToolbar.OnToolSelected += OnActionSelected;
            paletteDropdown.UnregisterValueChangedCallback<string>(OnPaletteValueChanged);
            colorSelector.OnIndexChanged -= OnColorSelectorValueChanged;
        }
#endregion

#region Callbacks
        private void OnColorSelectorValueChanged(int colorIndex)
        {
            VoxelEditor.ColorIndex = colorIndex;
        }

        private void OnPaletteValueChanged(ChangeEvent<string> evt)
        {
            int index = paletteDropdown.choices.IndexOf(evt.newValue);

            if (index == -1)
                return;
            VoxelEditor.PaletteIndex = index;
            UpdateColorSelector();
        }


        private void OnActionSelected(int toolIndex)
        {
            VoxelEditor.Action = (VoxelTools.vxAction)toolIndex;
        }

        private void OnToolSelected(int toolIndex)
        {
            VoxelEditor.Tool = (VoxelTools.vxTool)toolIndex;
        }

        private void OnClickStopEdit()
        {
            FoxEditManager.StopEditVoxelObject();
        }
#endregion

        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible == true)
                SetupFields();
        }


    }
}
