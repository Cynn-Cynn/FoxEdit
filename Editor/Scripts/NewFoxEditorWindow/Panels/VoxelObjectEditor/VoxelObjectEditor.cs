using System.Collections;
using System.Collections.Generic;
using FoxEdit.EditorUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditor
    {
        private VisualElement root;

        private Button stopButton = null;

        public VoxelObjectEditor(VisualElement root)
        {
            this.root = root;
            GetElements();
            RegisterCallbacks();
        }

        ~VoxelObjectEditor()
        {
            UnregisterCallbacks();
        }

        private void GetElements()
        {
            stopButton = root.Q<Button>("stop-edit-button");
        }

        private void RegisterCallbacks()
        {
            stopButton.clicked += OnClickStopEdit;
        }

        private void UnregisterCallbacks()
        {
            stopButton.clicked -= OnClickStopEdit;
        }

        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnClickStopEdit()
        {
            FoxEditManager.StopEditVoxelObject();
        }

    }
}
