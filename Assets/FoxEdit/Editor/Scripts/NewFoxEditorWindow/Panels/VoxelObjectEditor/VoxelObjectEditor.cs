using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels
{
    public class VoxelObjectEditor
    {
        private VisualElement root;
        public VoxelObjectEditor(VisualElement root)
        {
            this.root = root;
        }

        public void SetVisibility(bool visible)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
