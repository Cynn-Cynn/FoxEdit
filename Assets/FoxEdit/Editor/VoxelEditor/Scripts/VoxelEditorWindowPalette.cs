using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEditor.Editors
{
    public class VoxelEditorWindowPalette
    {
        public enum SortType
        {
            Color,
            Emissive,
            Metallic,
            Smoothness,
        }
        private const string PALETTE_ELEMENTS_CONTAINER_NAME = "palette-elements";
        private const string PALETTE_ADD_BUTTON_NAME = "palette-element-add";
        private SortType sortType = SortType.Color;

        public event Action<PaletteElement> OnSelect;

        private VisualElement container;
        private VisualElement addButton;
        private PaletteElement selectedElement;

        public VoxelEditorWindowPalette(VisualElement root, List<VoxelColor> colors, Action<PaletteElement> onSelectCallback)
        {
            OnSelect += onSelectCallback;

            container = root.Q<VisualElement>(PALETTE_ELEMENTS_CONTAINER_NAME);

            container.Clear();

            foreach (var color in colors)
                container.Add(new PaletteElement(color, OnPressPaletteElement));

            addButton = CreateAddButtonElement();
            container.Add(addButton);

            var menu = new ContextualMenuManipulator(OnContextMenu);
            container.AddManipulator(menu);

            Sort(sortType);
        }

        ~VoxelEditorWindowPalette()
        {
            OnSelect = null;
        }

        private void OnPressPaletteElement(PaletteElement element)
        {
            if (selectedElement != null)
                selectedElement.IsSelected = false;
            selectedElement = element;
            selectedElement.IsSelected = true;
            OnSelect?.Invoke(selectedElement);
        }

        private VisualElement CreateAddButtonElement()
        {
            // Create add button
            VisualElement addButton = new VisualElement();
            addButton.name = PALETTE_ADD_BUTTON_NAME;
            addButton.AddToClassList(PaletteElement.PALETTE_ELEMENT_CLASS);
            addButton.RegisterCallback<MouseDownEvent>(OnPressAddColor);

            // Add buttonIcon
            addButton.Add(new VisualElement());

            return addButton;
        }

        private void OnPressAddColor(MouseDownEvent e)
        {
            container.Add(new PaletteElement(OnPressPaletteElement));
            addButton.BringToFront();
        }

        private void OnContextMenu(ContextualMenuPopulateEvent e)
        {
            foreach (SortType sortType in Enum.GetValues(typeof(SortType)))
                e.menu.AppendAction(string.Format("Sort/{0}", sortType.ToString()), (a) => {
                    Sort(sortType);
                },
                sortType == this.sortType ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        public void Sort(SortType sortType)
        {
            List<PaletteElement> elements = new List<PaletteElement>();
            this.sortType = sortType;
            foreach (var element in container.Children())
                elements.Add(element as PaletteElement);

            elements.Sort((a, b) => {
                return SortMethod(a, b, sortType);
            });

            container.Clear();
            foreach (var element in elements)
                container.Add(element);
            addButton = CreateAddButtonElement();
            container.Add(addButton);
        }

        private int SortMethod(PaletteElement a, PaletteElement b, SortType sortType)
        {
            if (a == null || b == null)
                return 0;

            Color colorA = a.Color.Color;
            Color colorB = b.Color.Color;

            float hA, sA, vA;
            float hB, sB, vB;

            Color.RGBToHSV(colorA, out hA, out sA, out vA);
            Color.RGBToHSV(colorB, out hB, out sB, out vB);

            switch (sortType)
            {
                default:
                case SortType.Color:
                    return hA.CompareTo(hB);
                case SortType.Emissive:
                    return a.Color.EmissiveIntensity.CompareTo(b.Color.EmissiveIntensity);
                case SortType.Metallic:
                    return a.Color.Metallic.CompareTo(b.Color.Metallic);
                case SortType.Smoothness:
                    return a.Color.Smoothness.CompareTo(b.Color.Smoothness);
            }
        }
    }
}