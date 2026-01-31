using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowPanels.SubPanels
{
    public class AnimationSelectorSubPanel
    {
        private VisualElement root = null;
        private TextField renameField = null;
        private DropdownField animationSelector = null;
        private Button renameButton = null;
        private Button addButton = null;
        private Button deleteButton = null;

        public event Action<int> OnSelectAnimation;
        public event Action<int> OnDeleteAnimation;
        public event Action<string> OnAddAnimation;
        public event Action<int, string> OnRenameAnimation;
        public event Action<int> OnRemoveAnimation;

        public AnimationSelectorSubPanel(VisualElement root)
        {
            this.root = root;
            GetElements();
            RegisterCallbacks();
        }

        ~AnimationSelectorSubPanel()
        {
            UnregisterCallbacks();
        }

        private void GetElements()
        {
            renameField = root.Q<TextField>("animator-selector-rename-field");
            animationSelector = root.Q<DropdownField>("animation-selector");
            renameButton = root.Q<Button>("rename-button");
            deleteButton = root.Q<Button>("delete-button");
            addButton = root.Q<Button>("add-button");
        }

        private void RegisterCallbacks()
        {
            animationSelector.RegisterValueChangedCallback(OnSelectedAnimationChanged);
            renameButton.clicked += OnRenameClicked;
            addButton.clicked += OnAddButtonClicked;
            deleteButton.clicked += OnDeleteButtonClicked;
        }

        private void UnregisterCallbacks()
        {
            animationSelector.UnregisterValueChangedCallback(OnSelectedAnimationChanged);
            renameButton.clicked -= OnRenameClicked;
            addButton.clicked -= OnAddButtonClicked;
            deleteButton.clicked -= OnDeleteButtonClicked;
        }

        private void OnDeleteButtonClicked()
        {
            OnDeleteAnimation?.Invoke(GetSelectedAnimationIndex());
        }

        private void OnAddButtonClicked()
        {
            string newAnimName = string.Format("Anim {0}", animationSelector.choices.Count);
            animationSelector.choices.Add(newAnimName);
            animationSelector.value = newAnimName;
            OnAddAnimation?.Invoke(newAnimName);
        }

        private void OnRenameClicked()
        {
            renameField.SetValueWithoutNotify(animationSelector.value);
            renameField.RegisterCallback<KeyDownEvent>(RenameFieldKeyDownEvent);
            renameField.RegisterCallback<FocusOutEvent>(RenameFieldFocusOutEvent);
            SetRenameDisplay(true);
        }


        private void SetRenameDisplay(bool displayRenameField)
        {
            animationSelector.style.display = displayRenameField ? DisplayStyle.None : DisplayStyle.Flex;
            addButton.style.display = displayRenameField ? DisplayStyle.None : DisplayStyle.Flex;
            renameButton.style.display = displayRenameField ? DisplayStyle.None : DisplayStyle.Flex;
            deleteButton.style.display = displayRenameField ? DisplayStyle.None : DisplayStyle.Flex;
            renameField.style.display = displayRenameField ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RenameFieldFocusOutEvent(FocusOutEvent evt)
        {
            ValidateRename();
        }

        private void RenameFieldKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                ValidateRename();
            if (evt.keyCode == KeyCode.Escape)
                CancelRename();
        }

        private void CancelRename()
        {
            StopRename();
        }

        private void ValidateRename()
        {
            int changedIndex = GetSelectedAnimationIndex();
            animationSelector.choices[changedIndex] = renameField.value;
            animationSelector.value = renameField.value;
            OnRenameAnimation?.Invoke(changedIndex, animationSelector.choices[changedIndex]);
            StopRename();
        }

        private void StopRename()
        {
            renameField.UnregisterCallback<KeyDownEvent>(RenameFieldKeyDownEvent);
            renameField.UnregisterCallback<FocusOutEvent>(RenameFieldFocusOutEvent);
            SetRenameDisplay(false);
        }

        private void OnSelectedAnimationChanged(ChangeEvent<string> evt)
        {
            int newIndex = GetSelectedAnimationIndex();
            OnSelectAnimation?.Invoke(newIndex);
        }


        public void SetAnimationNames(List<string> animationNames)
        {
            animationSelector.choices = animationNames;
        }

        public void SetAnimationIndex(int index, bool notify = true)
        {
            string choice = animationSelector.choices[index];
            if (notify)
                animationSelector.value = choice;
            else
                animationSelector.SetValueWithoutNotify(choice);
        }

        private int GetSelectedAnimationIndex()
        {
            return animationSelector.choices.IndexOf(animationSelector.value);
        }

    }
}