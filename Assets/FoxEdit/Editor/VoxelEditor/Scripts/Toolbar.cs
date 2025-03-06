using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEditor.Editors
{
    public enum EFoxEditTool
    {
        Voxel,
        Brush,
        Eraser
    }

    public class Toolbar
    {
        private const string TOOLBAR_BUTTON_SELECTED_CLASS = "toolbar-button_enabled";

        private Action<EFoxEditTool> OnToolSelected;
        private Dictionary<EFoxEditTool, Button> buttonFromEditTool;

        public Toolbar(VisualElement root, Action<EFoxEditTool> onToolSelectedCallback)
        {
            OnToolSelected += onToolSelectedCallback;

            buttonFromEditTool = new Dictionary<EFoxEditTool, Button>();

            Button voxelButton = root.Q<Button>("voxel-button");
            Button brushButton = root.Q<Button>("brush-button");
            Button eraserButton = root.Q<Button>("eraser-button");

            buttonFromEditTool.Add(EFoxEditTool.Voxel, voxelButton);
            buttonFromEditTool.Add(EFoxEditTool.Brush, brushButton);
            buttonFromEditTool.Add(EFoxEditTool.Eraser, eraserButton);

            foreach (KeyValuePair<EFoxEditTool, Button> button in buttonFromEditTool)
            {
                button.Value.EnableInClassList(TOOLBAR_BUTTON_SELECTED_CLASS, false);
                button.Value.clicked += () => OnToolButtonPressed(button.Key);
            }

        }

        private void OnToolButtonPressed(EFoxEditTool tool)
        {
            foreach (var button in buttonFromEditTool)
                button.Value.RemoveFromClassList(TOOLBAR_BUTTON_SELECTED_CLASS);
            buttonFromEditTool[tool].AddToClassList(TOOLBAR_BUTTON_SELECTED_CLASS);
            OnToolSelected?.Invoke(tool);
        }
    }
}
