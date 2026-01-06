
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class FrameSelectorElement : VisualElement
    {
        #region CLASS_NAMES
        public const string FRAME_SELECTOR_CLASS_NAME = "frameselector";
        public const string FRAME_SELECTOR_ITEM_CLASS_NAME = "frameselector-item";
        public const string FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME = "frameselector-item-selected";
        public const string FRAME_SELECTOR_ITEM_LABEL_CLASS_NAME = "frameselector-item-label";
        public const string FRAME_SELECTOR_CONTAINER_CLASS_NAME = "frameselector-items-container";
        public const string FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME = "frameselector-add-button";
        public const string FRAME_SELECTOR_ADD_BUTTON_CONTAINER_CLASS_NAME = "frameselector-add-button-container";
        #endregion
        public new class UxmlFactory : UxmlFactory<FrameSelectorElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlIntAttributeDescription FramesCount = new UxmlIntAttributeDescription
            {
                name = "FramesCount",
                defaultValue = 5,
            };

            public UxmlIntAttributeDescription FrameIndex = new UxmlIntAttributeDescription
            {
                name = "FrameIndex",
                defaultValue = 0
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                FrameSelectorElement frameSelectorElement = ve as FrameSelectorElement;

                frameSelectorElement.FramesCount = FramesCount.GetValueFromBag(bag, cc);
                frameSelectorElement.FramesIndex = FrameIndex.GetValueFromBag(bag, cc);
            }
        }

        private int _framesCount = 1;
        public int FramesCount
        {
            get => _framesCount;
            set
            {
                _framesCount = Mathf.Max(1, value);
                UpdateFrameButtons();

                if (_frameIndex >= _framesCount)
                    SelectFrame(_framesCount - 1);
            }
        }

        private int _frameIndex = 0;
        public int FramesIndex
        {
            get => _frameIndex;
            set
            {
                SelectFrame(Mathf.Clamp(value, 0, _framesCount - 1));
            }
        }

        private VisualElement framesContainer = null;
        private List<Button> frameItems = new List<Button>();
        private Button addButton = null;
        public event Action<int> onFrameChanged;

        public FrameSelectorElement()
        {
            framesContainer = new VisualElement();
            framesContainer.AddToClassList(FRAME_SELECTOR_CONTAINER_CLASS_NAME);
            framesContainer.name = "frames-container";

            Add(framesContainer);
            SetupAddButtons();
        }

#region AddButton
        private void SetupAddButtons()
        {
            addButton = new Button();
            addButton.name = "add-frame-button";
            addButton.AddToClassList(FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME);
            addButton.AddToClassList(FRAME_SELECTOR_ITEM_CLASS_NAME);

            VisualElement addButtonsContainer = new VisualElement();
            addButtonsContainer.name = "Add buttons";
            addButtonsContainer.AddToClassList(FRAME_SELECTOR_ADD_BUTTON_CONTAINER_CLASS_NAME);

            Button duplicateLastFrameButton = new Button();
            duplicateLastFrameButton.text = "Duplicate last frame";
            duplicateLastFrameButton.AddToClassList(FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME);
            duplicateLastFrameButton.clicked += DuplicateLastFrame;
            addButtonsContainer.Add(duplicateLastFrameButton);

            Button newEmptyFrameButton = new Button();
            newEmptyFrameButton.text = "New empty frame";
            newEmptyFrameButton.AddToClassList(FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME);
            newEmptyFrameButton.clicked += NewEmptyFrame;
            addButtonsContainer.Add(newEmptyFrameButton);

            this.Add(addButtonsContainer);
        }
#endregion

        private void NewEmptyFrame()
        {
            FramesCount++;
            SelectFrame(FramesCount - 1);
        }

        private void DuplicateLastFrame()
        {
            FramesCount++;
            SelectFrame(FramesCount - 1);
        }

        private void UpdateFrameButtons()
        {
            if (_framesCount == frameItems.Count || _framesCount < 0)
            {
                return;
            }
            else if (frameItems.Count < _framesCount)
            {
                int diff = _framesCount - frameItems.Count;

                for (int i = 0; i < diff; i++)
                    AddFrameElement(frameItems.Count);
            }
            else
            {
                ClearFrameItems();
                for (int i = 0; i < _framesCount; i++)
                    AddFrameElement(i);
            }

            addButton.BringToFront();
        }

        private void ClearFrameItems()
        {
            foreach (VisualElement frame in frameItems)
                frame.RemoveFromHierarchy();
            frameItems.Clear();
        }

        private void AddFrameElement(int frameIndex)
        {
            Button frameItem = new Button();

            frameItem.name = string.Format("frame-{0}", frameIndex);
            frameItem.AddToClassList(FRAME_SELECTOR_ITEM_CLASS_NAME);

            Label label = new Label();
            label.AddToClassList(FRAME_SELECTOR_ITEM_LABEL_CLASS_NAME);
            label.text = frameIndex.ToString();

            frameItem.Add(label);
            frameItems.Add(frameItem);
            framesContainer.Add(frameItem);

            frameItem.clicked += () => SelectFrame(frameIndex);

            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(FrameMenuManipulator);
            contextualMenuManipulator.target = frameItem;

            addButton.BringToFront();
        }

        private void FrameMenuManipulator(ContextualMenuPopulateEvent evt)
        {
            Button target = evt.target as Button;
            evt.menu.AppendAction("Delete", (DropdownMenuAction) => RemoveFrameItem(target));
        }

        private void RemoveFrameItem(Button frameItem)
        {
            Debug.LogFormat("Remove {0}", frameItem.name);
        }

        private void SelectFrame(int index)
        {
            index = Mathf.Clamp(index, 0, frameItems.Count - 1);
            frameItems[_frameIndex].RemoveFromClassList(FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME);
            _frameIndex = index;
            frameItems[_frameIndex].AddToClassList(FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME);
            onFrameChanged?.Invoke(index);
        }
    }
}