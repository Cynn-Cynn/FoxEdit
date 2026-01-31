using System.Linq;
using FoxEdit.WindowComponents;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using FoxEdit.VoxelTools;
using FoxEdit.WindowPanels.SubPanels;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditorPanel
    {
        private VisualElement root;

        private Button stopButton;
        private Button saveAsButton;
        private Button saveButton;
        private ToolbarElement toolToolbar;
        private ToolbarElement actionToolbar;
        private DropdownField paletteDropdown;
        private ColorPaletteElement colorSelector;
        private FrameSelectorElement frameSelector;
        private AnimationSelectorSubPanel animationSelector;

        private VoxelObject voxelObject;
        private VoxelRenderer voxelRenderer;
        private VoxelEditor voxelEditor;

        public VoxelObjectEditorPanel(VisualElement root)
        {
            this.root = root;
            GetElements();
            RegisterCallbacks();
            SetupFields();
        }

        ~VoxelObjectEditorPanel()
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

        private void UpdateFrameSelector()
        {
            frameSelector.FramesCount = voxelEditor.CurrentAnimation.FramesCount;
            frameSelector.SelectFrame(0, false);
        }

        private void UpdateColorSelector()
        {
            VoxelPalette selectedPalette = VoxelEditor.CurrentPalette;
            colorSelector.ClearPaletteItems();
            for (int i = 0; i < selectedPalette.Colors.Length; i++)
                colorSelector.AddPaletteItem(selectedPalette.Colors[i]);
            colorSelector.SetIndexValue(VoxelEditor.ColorIndex, false);
        }

        private void GetElements()
        {
            stopButton = root.Q<Button>("stop-edit-button");
            toolToolbar = root.Q<ToolbarElement>("tools");
            actionToolbar = root.Q<ToolbarElement>("actions");
            paletteDropdown = root.Q<DropdownField>("palette-selector");
            colorSelector = root.Q<ColorPaletteElement>();
            frameSelector = root.Q<FrameSelectorElement>();
            saveAsButton = root.Q<Button>("save-as-button");
            saveButton = root.Q<Button>("save-button");
            animationSelector = new AnimationSelectorSubPanel(root.Q("animation-selector-container"));
        }

        #region Callbacks
        private void RegisterCallbacks()
        {
            stopButton.clicked += OnClickStopEdit;
            saveButton.clicked += Save;
            saveAsButton.clicked += SaveAs;
            toolToolbar.OnToolSelected += OnToolSelected;
            actionToolbar.OnToolSelected += OnActionSelected;
            paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            colorSelector.OnIndexChanged += OnColorSelectorValueChanged;
            frameSelector.OnFrameChanged += OnSelectFrame;
            frameSelector.OnMoveFrame += OnMoveFrame;
            frameSelector.OnDuplicateFrame += OnDuplicateFrame;
            frameSelector.OnNewFrame += OnNewFrame;
            frameSelector.OnDeleteFrame += OnDeleteFrame;

            animationSelector.OnAddAnimation += OnAddAnimationFromAnimSelector;
            animationSelector.OnDeleteAnimation += OnDeleteAnimationFromAnimSelector;
            animationSelector.OnRenameAnimation += OnRenameAnimationFromAnimSelector;
            animationSelector.OnSelectAnimation += OnSelectAnimationFromAnimSelector;

            VoxelEditor.OnChangeColor += OnChangeColor;
            VoxelEditor.OnChangeAction += OnChangeAction;
            VoxelEditor.OnChangeTool += OnChangeTool;
            FoxEditManager.OnStartEditVoxelObject += OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject += OnStopEditVoxelObject;
        }

        private void OnSelectAnimationFromAnimSelector(int animationIndex)
        {
            voxelEditor.SelectedAnimationIndex = animationIndex;
        }

        private void OnRenameAnimationFromAnimSelector(int animIndex, string newName)
        {
            VoxelEditorAnimation voxelEditorAnimation = voxelEditor.GetVoxelEditorAnimation(animIndex);
            voxelEditorAnimation.Name = newName;
        }

        private void OnDeleteAnimationFromAnimSelector(int animIndex)
        {
            voxelEditor.DeleteAnimation(animIndex);
        }

        private void OnAddAnimationFromAnimSelector(string obj)
        {
            voxelEditor.NewAnimation(obj);
        }

        private void UnregisterCallbacks()
        {
            stopButton.clicked -= OnClickStopEdit;
            saveButton.clicked -= Save;
            saveAsButton.clicked -= SaveAs;
            toolToolbar.OnToolSelected -= OnToolSelected;
            actionToolbar.OnToolSelected -= OnActionSelected;
            paletteDropdown.RegisterValueChangedCallback<string>(OnPaletteValueChanged);
            colorSelector.OnIndexChanged -= OnColorSelectorValueChanged;
            frameSelector.OnFrameChanged -= OnSelectFrame;
            frameSelector.OnMoveFrame -= OnMoveFrame;
            frameSelector.OnDuplicateFrame -= OnDuplicateFrame;
            frameSelector.OnNewFrame -= OnNewFrame;

            VoxelEditor.OnChangeColor -= OnChangeColor;
            VoxelEditor.OnChangeColor -= OnChangeColor;
            VoxelEditor.OnChangeAction -= OnChangeAction;
            FoxEditManager.OnStartEditVoxelObject -= OnStartEditVoxelObject;
            FoxEditManager.OnStopEditVoxelObject -= OnStopEditVoxelObject;
        }

        private void OnDeleteFrame()
        {
            voxelEditor.DeleteFrame();
        }

        private void OnNewFrame()
        {
            voxelEditor.NewFrame();
        }

        private void OnDuplicateFrame()
        {
            voxelEditor.DuplicateFrame();
        }

        private void Save()
        {
            FoxEditManager.Save();
        }

        private void SaveAs()
        {
            FoxEditManager.SaveAs();
        }

        private void OnChangeTool(vxTool tool)
        {
            toolToolbar.SelectTool((int)tool, false);
        }

        private void OnChangeAction(vxAction action)
        {
            actionToolbar.SelectTool((int)action, false);
        }

        private void OnMoveFrame(int oldIndex, int newIndex)
        {
            voxelEditor.MoveFrame(oldIndex, newIndex);
        }

        private void OnFrameThumnbailUpdated(int animIndex, int index, Texture2D texture)
        {
            //frameSelector.SetFrameThumbnail(index, texture);
        }

        private void OnSelectFrame(int newFrame)
        {
            if (FoxEditManager.VoxelEditor != null)
                FoxEditManager.VoxelEditor.ChangeFrame(newFrame);
        }

        private void OnStopEditVoxelObject()
        {
            if (voxelEditor != null)
            {
                voxelEditor.OnFramesThumbnailsUpdated -= OnFrameThumnbailUpdated;
                voxelEditor.OnFrameIndexChanged += OnFrameIndexChanged;
                voxelEditor.OnAnimationIndexChanged -= OnAnimationIndexChanged;
            }
            voxelObject = null;
            voxelRenderer = null;
            voxelEditor = null;
        }

        private void OnStartEditVoxelObject(VoxelObject voxelObject, VoxelRenderer voxelRenderer, VoxelEditor voxelEditor)
        {
            this.voxelObject = voxelObject;
            this.voxelRenderer = voxelRenderer;
            this.voxelEditor = voxelEditor;
            voxelEditor.OnFramesThumbnailsUpdated += OnFrameThumnbailUpdated;
            voxelEditor.OnFrameIndexChanged += OnFrameIndexChanged;
            voxelEditor.OnAnimationIndexChanged += OnAnimationIndexChanged;

            animationSelector.SetAnimationNames(voxelEditor.GetAnimationNames());
            animationSelector.SetAnimationIndex(0);

            SetupFields();
            UpdateFrameSelector();
        }

        private void OnAnimationIndexChanged(int newAnimationIndex)
        {
            animationSelector.SetAnimationIndex(newAnimationIndex);
            frameSelector.FramesCount = voxelEditor.CurrentAnimation.FramesCount;
        }

        private void OnFrameIndexChanged(int frameIndex)
        {
            frameSelector.SelectFrame(frameIndex, false);
        }


        #endregion

        #region Callbacks
        private void OnColorSelectorValueChanged(int colorIndex)
        {
            VoxelEditor.ColorIndex = colorIndex;
        }

        private void OnChangeColor(int colorIndex)
        {
            colorSelector.SetIndexValue(colorIndex, false);
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
        }


    }
}
