
using System;
using UnityEngine.UIElements;
using UnityEngine;

namespace FoxEdit.WindowComponents
{
    public class IconButton : VisualElement
    {
        public const string ICON_BUTTON_CLASSNAME = "icon-button";
        public const string HOVER_ICON_BUTTON = "icon-button-hover";
        public const string ICON_BUTTON_ICON_CLASSNAME = "icon-button-icon";

        public new class UxmlFactory : UxmlFactory<IconButton, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits {}

        private VisualElement icon;
        public event Action OnClick;

        public IconButton()
        {
            AddToClassList(ICON_BUTTON_CLASSNAME);

            icon = new VisualElement();
            icon.AddToClassList(ICON_BUTTON_ICON_CLASSNAME);
            icon.name = "icon";
            hierarchy.Add(icon);

            RegisterCallbacks();
        }

        ~IconButton()
        {
            UnregisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<ClickEvent>(OnPointerClick);
        }


        private void UnregisterCallbacks()
        {
            UnregisterCallback<ClickEvent>(OnPointerClick);
        }

        private void OnPointerClick(ClickEvent evt)
        {
            OnClick?.Invoke();
        }
    }
}