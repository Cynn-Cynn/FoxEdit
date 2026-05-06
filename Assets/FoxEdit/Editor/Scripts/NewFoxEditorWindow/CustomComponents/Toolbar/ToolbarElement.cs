using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class ToolbarElement : VisualElement
    {
    #region CLASSNAMES
    public const string TOOLBAR_CLASS_NAME = "toolbar";
    public const string TOOLBAR_BUTTON_CLASS_NAME = "toolbar-button";
    public const string TOOLBAR_BUTTON_SELECTED_CLASS_NAME = "toolbar-button-selected";
    public const string TOOLBAR_BUTTON_ICON_CLASS_NAME = "toolbar-button-icon";
    public const string TOOLBAR_FIRST_BUTTON_CLASS_NAME = "toolbar-button-first";
    public const string TOOLBAR_LAST_BUTTON_CLASS_NAME = "toolbar-button-last";
    private const char TOOLTIPS_SEPARATOR = '|';
    #endregion

        public new class UxmlFactory : UxmlFactory<ToolbarElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlTypeAttributeDescription<IConvertible> enumTypeAttr = new UnityEngine.UIElements.UxmlTypeAttributeDescription<IConvertible>
            {
                name = "EnumType",
            };

            UxmlIntAttributeDescription IndexAttr = new UxmlIntAttributeDescription
            {
                name = "Index",
                defaultValue = 0
            };

            UxmlStringAttributeDescription Tooltips = new UxmlStringAttributeDescription()
            {
                name = "Tooltips",
                defaultValue = string.Empty,
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ToolbarElement toolbarElement = ve as ToolbarElement;

                toolbarElement.EnumType = enumTypeAttr.GetValueFromBag(bag, cc);
                toolbarElement.Index = IndexAttr.GetValueFromBag(bag, cc);
                toolbarElement.Tooltips = Tooltips.GetValueFromBag(bag, cc);
            }
        }

        private string _tooltips;
        public string Tooltips
        {
            get => _tooltips;
            set
            {
                _tooltips = value;
                UpdateTooltips();
            }
        }

        private Type _enumType;
        public Type EnumType
        {
            get => _enumType;
            set
            {
                Setup(value);
                _enumType = value;
            }
        }

        private int _index = -1;
        public int Index
        {
            get => _index;
            set
            {
                SelectTool(value);
            }
        }



        public event Action<int> OnToolSelected;
        private List<Button> buttons = new List<Button>();

        public ToolbarElement()
        {
            this.AddToClassList(TOOLBAR_CLASS_NAME);
        }

        public void Setup(Type type)
        {
            if (type == null) return;
            this.Clear();
            buttons.Clear();

            string[] enumNames = Enum.GetNames(type);

            for (int i = 0; i < enumNames.Length; i++)
            {
                int tmp_index = i;
                Button button = CreateButtonIcon(enumNames[i]);

                if (i == 0)
                    button.AddToClassList(TOOLBAR_FIRST_BUTTON_CLASS_NAME);
                if (i == enumNames.Length - 1)
                    button.AddToClassList(TOOLBAR_LAST_BUTTON_CLASS_NAME);
                button.clicked += () => SelectTool(tmp_index);
                buttons.Add(button);
                this.Add(button);
            }
            
            Array values = Enum.GetValues(type);

            SelectTool(0);
        }

        public void SelectTool(int toolIndex, bool notify = true)
        {
            if (Index == toolIndex)
                return;

            for (int i = 0; i < buttons.Count; i++)
                buttons[i].EnableInClassList(TOOLBAR_BUTTON_SELECTED_CLASS_NAME, i == toolIndex);
            if (notify)
                OnToolSelected?.Invoke(toolIndex);
            _index = toolIndex;
        }

        private Button CreateButtonIcon(string name)
        {
            Button newButton = new Button();
            VisualElement icon = new VisualElement();

            newButton.AddToClassList(TOOLBAR_BUTTON_CLASS_NAME);
            icon.AddToClassList(TOOLBAR_BUTTON_ICON_CLASS_NAME);
            newButton.Add(icon);

            newButton.name = string.Format("{0}-button", name);

            return newButton;
        }

        private void UpdateTooltips()
        {
            string[] tooltips = _tooltips.Split(TOOLTIPS_SEPARATOR);

            for (int i = 0; i < tooltips.Length && i < buttons.Count; i++)
            {
                buttons[i].tooltip = tooltips[i].Trim();
            }
        }
    }
}