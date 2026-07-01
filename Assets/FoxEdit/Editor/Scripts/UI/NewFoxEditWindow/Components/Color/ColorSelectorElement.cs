
using UnityEngine.UIElements;
using UnityEngine;

namespace FoxEdit.WindowComponents
{
    public class ColorSelectorElement : VisualElement
    {
        private const string COLOR_SELECTOR_ELEMENT_CLASSNAME = "color-selector-element";

        public new class UxmlFactory : UxmlFactory<ColorSelectorElement, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlColorAttributeDescription colorAttr = new UxmlColorAttributeDescription()
            {
                name = "Color"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ColorSelectorElement colorSelectorElement = ve as ColorSelectorElement;
                
                colorSelectorElement.Color = colorAttr.GetValueFromBag(bag, cc);
            }
        }

        public Color Color
        {
            get => style.backgroundColor.value;
            set => style.backgroundColor = value;
        }

        public ColorSelectorElement(Color color) : this()
        {
            Color = color;
        }

        public ColorSelectorElement()
        {
            AddToClassList(COLOR_SELECTOR_ELEMENT_CLASSNAME);
        }
    }
}