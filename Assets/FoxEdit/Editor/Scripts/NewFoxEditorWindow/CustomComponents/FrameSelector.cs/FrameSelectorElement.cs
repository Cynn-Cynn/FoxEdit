
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
        public const string FRAME_SELECTOR_CONTAINER_CLASS_NAME = "frameselector-items-container";
        public const string FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME = "frameselector-add-button";
        public const string FRAME_SELECTOR_ADD_BUTTON_CONTAINER_CLASS_NAME = "frameselector-add-button-container";
        public const string FRAME_SELECTOR_GHOSTICON_CLASS_NAME = "frameselector-ghost-icon";
        public const int GHOST_ICON_SIZE = 30;
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
        private Dictionary<int, FrameButton> frameItems = new Dictionary<int, FrameButton>();
        private Button addButton = null;
        private VisualElement ghostIcon;
        private DragInsertionMarker dragInsertionMarker;
        public delegate void OnMoveFrameDelegate(int oldIndex, int newIndex);
        public event Action<int> onFrameChanged;
        public event OnMoveFrameDelegate OnMoveFrame;

        //Drag
        private FrameButton dragButton;
        private FrameButton hoveredFrameButton;
        private int lastDropIndex = -1;

        public FrameSelectorElement()
        {
            framesContainer = new VisualElement();
            framesContainer.AddToClassList(FRAME_SELECTOR_CONTAINER_CLASS_NAME);
            framesContainer.name = "frames-container";

            dragInsertionMarker = new DragInsertionMarker();

            ghostIcon = new VisualElement();
            ghostIcon.AddToClassList(FRAME_SELECTOR_GHOSTICON_CLASS_NAME);
            ghostIcon.name = "ghost-icon";
            ghostIcon.style.width = GHOST_ICON_SIZE;
            ghostIcon.style.height = GHOST_ICON_SIZE;
            ghostIcon.style.position = Position.Absolute;
            ghostIcon.pickingMode = PickingMode.Ignore;
            ghostIcon.style.display = DisplayStyle.None;

            Add(framesContainer);
            SetupAddButtons();
            Add(ghostIcon);
        }

        #region AddButton
        private void SetupAddButtons()
        {
            addButton = new Button();
            addButton.name = "add-frame-button";
            addButton.AddToClassList(FRAME_SELECTOR_ADD_BUTTON_CLASS_NAME);
            addButton.AddToClassList(FrameButton.FRAME_SELECTOR_ITEM_CLASS_NAME);

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
            foreach (var item in frameItems)
                item.Value.RemoveFromHierarchy();
            frameItems.Clear();
        }

        private void AddFrameElement(int frameIndex)
        {
            FrameButton frameButton = new FrameButton();

            frameButton.Index = frameIndex;
            framesContainer.Add(frameButton);
            frameItems.Add(frameIndex, frameButton);

            frameButton.clicked += () => SelectFrame(frameIndex);

            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(FrameMenuManipulator);
            contextualMenuManipulator.target = frameButton;

            RegisterFrameCallbacks(frameButton);

            addButton.BringToFront();
        }

        private void RegisterFrameCallbacks(FrameButton frameButton)
        {
            frameButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                dragButton = frameButton;
                ChangeDragInsertionMakerPosition(frameButton.Index);
                lastDropIndex = frameButton.Index;
                frameButton.CapturePointer(evt.pointerId);
                hoveredFrameButton = null;

            }, TrickleDown.TrickleDown);

            frameButton.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (dragButton == null)
                    return;
                MoveFrame(dragButton.Index, lastDropIndex);
                ChangeDragInsertionMakerPosition(-1);
                dragButton = null;
                ghostIcon.style.display = DisplayStyle.None;
                frameButton.ReleasePointer(evt.pointerId);
            });

            frameButton.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (dragButton == null || !frameButton.HasPointerCapture(evt.pointerId))
                    return;
                Vector2 mousePos = ghostIcon.parent.WorldToLocal(evt.position);
                ghostIcon.style.display = DisplayStyle.Flex;
                ghostIcon.style.left = mousePos.x - GHOST_ICON_SIZE / 2;
                ghostIcon.style.top = mousePos.y - GHOST_ICON_SIZE / 2;

                if (hoveredFrameButton != null)
                {
                    bool isOnLeft = IsMouseOnLeftOfElement(hoveredFrameButton, evt.position);
                    int index = hoveredFrameButton.Index + (isOnLeft ? 0 : 1);

                    ChangeDragInsertionMakerPosition(index);
                    lastDropIndex = index;
                }
            });

            frameButton.RegisterCallback<PointerEnterEvent>(evt =>
            {
                if (dragButton == null)
                    return;
                hoveredFrameButton = frameButton;
            });
        }

        private void ChangeDragInsertionMakerPosition(int newIndex)
        {
            if (framesContainer.Contains(dragInsertionMarker))
                framesContainer.Remove(dragInsertionMarker);
            if (newIndex >= 0)
            {
                framesContainer.Insert(newIndex, dragInsertionMarker);
            }
        }

        private bool IsMouseOnLeftOfElement(VisualElement visualElement, Vector2 mousePos)
        {
            Vector3 center = visualElement.worldBound.center;
            return mousePos.x < center.x;
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

        private void MoveFrame(int oldFrameIndex, int newFrameIndex)
        {
            if (oldFrameIndex == newFrameIndex)
                return;
            Debug.LogFormat("Frame #{0} move at #{1}", oldFrameIndex, newFrameIndex);
            OnMoveFrame?.Invoke(oldFrameIndex, newFrameIndex);
        }

        private void SelectFrame(int index)
        {
            index = Mathf.Clamp(index, 0, frameItems.Count - 1);
            if (frameItems.TryGetValue(index, out FrameButton oldFrameButton))
                oldFrameButton.SetSelected(false);
            _frameIndex = index;
            if (frameItems.TryGetValue(index, out FrameButton newFrameButton))
                newFrameButton.SetSelected(true);
            onFrameChanged?.Invoke(index);
        }

        public void SetFramesThumbnails(List<Texture2D> textures)
        {
            for (int i = 0; i < textures.Count; i++)
            {
                SetFrameThumbnail(i, textures[i]);
            }
        }

        public void SetFrameThumbnail(int index, Texture2D texture)
        {
            FrameButton frameButton = null;
            if (frameItems.TryGetValue(index, out frameButton))
            {
                frameButton.SetTexture(texture);
            }
        }
    }
}