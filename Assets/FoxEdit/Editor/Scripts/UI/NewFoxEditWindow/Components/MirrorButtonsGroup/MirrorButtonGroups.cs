using System;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class MirrorButtonGroups : VisualElement
    {
        private enum Axis
        {
            X,
            Y,
            Z
        }

        #region CLASSNAMES
        private const string MIRROR_BUTTON_GROUP_CLASS_NAME = "mirror-button-group";
        private const string MIRROR_BUTTON_GROUP_CONTENT_CLASS_NAME = "mirror-button-group-content";
        private const string MIRROR_BUTTON_CLASS_NAME = "mirror-button";
        private const string MIRROR_BUTTON_HIGHLIGHT_CLASS_NAME = "mirror-button-highlight";
        #endregion

        #region UXML
        public new class UxmlFactory : UxmlFactory<MirrorButtonGroups, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }
        #endregion

        private MirrorSettings mirrorSettings = new MirrorSettings();
        private VisualElement buttonsContainer;

        private Button resetButton = null;
        private Button xButton = null;
        private Button yButton = null;
        private Button zButton = null;

        public event Action<MirrorSettings> onMirrorSettingsUpdate;

        public MirrorButtonGroups()
        {
            AddToClassList(MIRROR_BUTTON_GROUP_CLASS_NAME);

            buttonsContainer = new VisualElement();
            buttonsContainer.name = "content";
            buttonsContainer.AddToClassList(MIRROR_BUTTON_GROUP_CONTENT_CLASS_NAME);
            Add(buttonsContainer);

            SetupButtons();
        }

        private void SetupButtons()
        {
            resetButton = new Button();
            resetButton.name = "reset";
            resetButton.clicked += Reset;
            resetButton.Add(new VisualElement() { name = "icon" });
            resetButton.AddToClassList(MIRROR_BUTTON_CLASS_NAME);
            buttonsContainer.Add(resetButton);

            xButton = CreateAxisButton(Axis.X);
            yButton = CreateAxisButton(Axis.Y);
            zButton = CreateAxisButton(Axis.Z);
        }

        private Button CreateAxisButton(Axis axis)
        {
            Button button = new Button();
            button.name = string.Format("{0}-button", axis.ToString().ToLower());
            button.text = axis.ToString().ToUpper();
            button.clicked += () => Toggle(axis);
            button.AddToClassList(MIRROR_BUTTON_CLASS_NAME);
            buttonsContainer.Add(button);
            return button;
        }

        private void Toggle(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    mirrorSettings.X = !mirrorSettings.X;
                    break;
                case Axis.Y:
                    mirrorSettings.Y = !mirrorSettings.Y;
                    break;
                case Axis.Z:
                    mirrorSettings.Z = !mirrorSettings.Z;
                    break;
            }
            UpdateHighlight();
            SendEvent();
        }

        private void UpdateHighlight()
        {
            xButton.EnableInClassList(MIRROR_BUTTON_HIGHLIGHT_CLASS_NAME, mirrorSettings.X);
            yButton.EnableInClassList(MIRROR_BUTTON_HIGHLIGHT_CLASS_NAME, mirrorSettings.Y);
            zButton.EnableInClassList(MIRROR_BUTTON_HIGHLIGHT_CLASS_NAME, mirrorSettings.Z);
        }

        private void Reset()
        {
            mirrorSettings.Reset();
            UpdateHighlight();
            SendEvent();
        }

        private void SendEvent()
        {
            onMirrorSettingsUpdate?.Invoke(mirrorSettings);
        }

    }
}