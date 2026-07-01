using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class ButtonGroup : VisualElement
    {
        private const char BUTTONS_TEXT_SEPARATOR = ';';
        #region CLASSNAMES
        private const string BUTTON_GROUP_CLASS_NAME = "button-group";
        private const string ICON_BUTTON_GROUP_CLASS_NAME = "icon-button-group";
        private const string TEXT_BUTTON_GROUP_CLASS_NAME = "text-button-group";
        private const string ICON_BUTTON_CLASS_NAME = "button-group-icon-button";
        private const string TEXT_BUTTON_CLASS_NAME = "button-group-text-button";
        private const string BUTTON_CLASS_NAME = "button-group-button";
        private const string HIGHLIGHTED_BUTTON_CLASS_NAME = "button-group-highlighted-button";
        #endregion

        #region UXML
        public new class UxmlFactory : UxmlFactory<ButtonGroup, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlTypeAttributeDescription<IConvertible> enumTypeAttr = new UxmlTypeAttributeDescription<IConvertible>()
            {
                name = "enumType",
                defaultValue = null
            };

            public UxmlIntAttributeDescription indexAttr = new UxmlIntAttributeDescription()
            {
                name = "index",
                defaultValue = 0
            };

            public UxmlStringAttributeDescription buttonsTextAttr = new UxmlStringAttributeDescription()
            {
                name = "buttonsText",
                defaultValue = string.Empty
            };

            public UxmlEnumAttributeDescription<ButtonGroupType> buttonGroupTypeAttr = new UxmlEnumAttributeDescription<ButtonGroupType>()
            {
                name = "buttonGroupType",
                defaultValue = ButtonGroupType.Text
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ButtonGroup buttonGroup = ve as ButtonGroup;

                buttonGroup.EnumType = enumTypeAttr.GetValueFromBag(bag, cc);
                buttonGroup.ButtonsText = buttonsTextAttr.GetValueFromBag(bag, cc);
                buttonGroup.ButtonGroupType = buttonGroupTypeAttr.GetValueFromBag(bag, cc);
                buttonGroup.Value = indexAttr.GetValueFromBag(bag, cc);
            }
        }
        #endregion

        #region FIELDS

        private delegate VisualElement createButton(int value, string name);
        private readonly Dictionary<ButtonGroupType, createButton> createButtonMethods;
        private VisualElement buttonsContainer = null;

        private event Action<int> OnValueChanged;

        private Type _enumType;
        public Type EnumType
        {
            get => _enumType;
            set
            {
                _enumType = value;
                SetTextFromEnumType();
                UpdateButtons();
            }
        }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
                UpdateHighlightedButton();
            }
        }

        private string _buttonsText;
        public string ButtonsText
        {
            get => _buttonsText;
            set
            {
                _buttonsText = value;
                UpdateButtons();
            }
        }

        private ButtonGroupType _buttonGroupType;
        public ButtonGroupType ButtonGroupType
        {
            get => _buttonGroupType;
            set
            {
                _buttonGroupType = value;
                EnableInClassList(TEXT_BUTTON_GROUP_CLASS_NAME, _buttonGroupType == ButtonGroupType.Text);
                EnableInClassList(ICON_BUTTON_GROUP_CLASS_NAME, _buttonGroupType == ButtonGroupType.Icon);
                UpdateButtons();
            }
        }

        private Dictionary<int, VisualElement> buttons = new Dictionary<int, VisualElement>();
        #endregion

        public ButtonGroup()
        {
            createButtonMethods = new Dictionary<ButtonGroupType, createButton>
            {
                { ButtonGroupType.Text, CreateTextButton },
                { ButtonGroupType.Icon, CreateIconButton }
            };

            buttonsContainer = new VisualElement();
            buttonsContainer.name = "buttons";
            Add(buttonsContainer);
            AddToClassList(BUTTON_GROUP_CLASS_NAME);
        }

        private void UpdateButtons()
        {
            if (EnumType == null)
                return;
            buttons.Clear();
            buttonsContainer.Clear();

            string[] enumNames = Enum.GetNames(_enumType).ToArray();
            int[] enumValues = Enum.GetValues(_enumType).Cast<int>().ToArray();

            int value;
            string name;
            for (int i = 0; i < enumNames.Length; i++)
            {
                value = enumValues[i];
                name = enumNames[i];

                VisualElement button = createButtonMethods[_buttonGroupType](value, GetName(i));
                button.AddToClassList(BUTTON_CLASS_NAME);
                buttons.Add(value, button);
                buttonsContainer.Add(button);
            }
        }

        private string GetName(int index)
        {
            if (EnumType == null)
                return string.Empty;
            string[] enumNames = EnumType.GetEnumNames().ToArray();
            string[] names = null;

            if (ButtonsText.Length == 0)
                names = enumNames;
            else
                names = ButtonsText.Split(BUTTONS_TEXT_SEPARATOR);

            if (index < names.Length)
                return names[index];
            return enumNames[index];
        }

        private void SetTextFromEnumType()
        {
            if (_enumType == null)
                return;

            Array enumValues = Enum.GetValues(_enumType);
            StringBuilder buttonsText = new StringBuilder();
            foreach (var value in enumValues)
                buttonsText.Append(value.ToString() + BUTTONS_TEXT_SEPARATOR);
            ButtonsText = buttonsText.ToString();
        }

        private VisualElement CreateIconButton(int value, string name)
        {
            VisualElement button = new VisualElement();
            button.AddToClassList(ICON_BUTTON_CLASS_NAME);

            VisualElement icon = new VisualElement();
            icon.AddToClassList(name);
            icon.name = "icon";

            button.Add(icon);
            button.name = name;

            button.RegisterCallback<ClickEvent>(evt => OnButtonClicked(value));

            return button;
        }

        private VisualElement CreateTextButton(int value, string text)
        {
            VisualElement button = new VisualElement();
            button.AddToClassList(TEXT_BUTTON_CLASS_NAME);

            Label label = new Label();
            label.text = text;
            button.Add(label);
            button.name = text;

            button.RegisterCallback<ClickEvent>(evt => OnButtonClicked(value));

            return button;
        }

        private void OnButtonClicked(int value)
        {
            Value = value;
        }

        private void UpdateHighlightedButton()
        {
            foreach (var item in buttons)
                item.Value.EnableInClassList(HIGHLIGHTED_BUTTON_CLASS_NAME, item.Key == Value);
        }
    }
}