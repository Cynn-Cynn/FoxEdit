
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

namespace FoxEdit.WindowComponents
{
    public class ColorSelector : VisualElement
    {
        private const int COLUMNS = 8;
        private const string COLOR_SELECTOR_CLASSNAME = "color-selector";
        private const string COLOR_SELECTOR_LINE_CLASSNAME = "color-selector-line";
        private const string COLOR_SELECTOR_CURSOR = "color-selector-cursor";
        private const string COLOR_SELECTOR_LINES_CONTAINER = "color-selector-lines-container";

        public new class UxmlFactory : UxmlFactory<ColorSelector, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlIntAttributeDescription ElementsCountAttr = new UxmlIntAttributeDescription()
            {
                name = "ElementSCount",
                defaultValue = COLUMNS * 10
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ColorSelector colorSelector = ve as ColorSelector;

                colorSelector.ElementsCount = ElementsCountAttr.GetValueFromBag(bag, cc);
            }
        }

        private int _elementCount = 0;
        public int ElementsCount
        {
            get => _elementCount;
            set
            {
                _elementCount = value;
                UpdateElements();
            }
        }

        private VisualElement linesContainer;
        private List<VisualElement> lines = new List<VisualElement>();
        private VisualElement cursor = null;

        public ColorSelector()
        {
            AddToClassList(COLOR_SELECTOR_CLASSNAME);

            linesContainer = new VisualElement();
            linesContainer.AddToClassList(COLOR_SELECTOR_LINES_CONTAINER);
            Add(linesContainer);

            cursor = CreateCursor();
            Add(cursor);
        }

        private VisualElement CreateCursor()
        {
            VisualElement cursor = new VisualElement();
            cursor.name = "cursor";
            cursor.AddToClassList(COLOR_SELECTOR_CURSOR);
            cursor.style.display = DisplayStyle.None;
            cursor.style.position = Position.Absolute;

            return cursor;
        }

        private void UpdateElements()
        {
            List<Color> colors = GetTmpColors();
            ClearLines();

            VisualElement currentLine = null;
            ColorSelectorElement colorSelectorElement;
            for (int i = 0; i < colors.Count; i++)
            {
                if (currentLine == null || currentLine.childCount >= COLUMNS)
                    currentLine = AddLine();
                colorSelectorElement = NewColorSelectorElement(colors[i]);
                currentLine.Add(colorSelectorElement);
                if (i == 0)
                    SelectElement(colorSelectorElement);
            }
        }

        private ColorSelectorElement NewColorSelectorElement(Color color)
        {
            ColorSelectorElement colorSelectorElement = new ColorSelectorElement(color);

            colorSelectorElement.RegisterCallback<ClickEvent>(OnSelectElement);

            return colorSelectorElement;
        }

        private void OnSelectElement(ClickEvent evt)
        {
            ColorSelectorElement target = evt.currentTarget as ColorSelectorElement;
            if (target != null)
                SelectElement(target);
        }

        private void SelectElement(ColorSelectorElement target)
        {
            Rect targetBounds = target.worldBound;
            
            Remove(cursor);
            Add(cursor);

            Vector2 localPos = cursor.parent.WorldToLocal(new Vector2(targetBounds.x, targetBounds.y));
            cursor.style.left = localPos.x;
            cursor.style.top = localPos.y;
            cursor.style.width = targetBounds.width;
            cursor.style.height = targetBounds.height;
            cursor.style.display = DisplayStyle.Flex;
        }

        private VisualElement AddLine()
        {
            VisualElement newLine = new VisualElement();
            newLine.name = string.Format("line-{0}", lines.Count);
            newLine.AddToClassList(COLOR_SELECTOR_LINE_CLASSNAME);
            lines.Add(newLine);
            linesContainer.Add(newLine);
            return newLine;
        }

        private void ClearLines()
        {
            foreach (VisualElement line in lines)
                linesContainer.Remove(line);
            lines.Clear();
        }

        private List<Color> GetTmpColors()
        {
            List<Color> colors = new List<Color>();

            for (int i = 0; i < ElementsCount; i++)
                colors.Add(Color.HSVToRGB((float)i/ElementsCount, 1, 1));

            return colors;
        }
    }
}