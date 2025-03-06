using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEditor.Editors
{
    /// <summary>
    /// A visual element that represents a color in the palette.
    /// </summary>
    public class PaletteElement : VisualElement
    {
        public const string PALETTE_ELEMENT_CLASS = "palette-element";
        private const string PALETTE_ELEMENT_SELECTED_CLASS = "palette-element_enabled";

        public new class UxmlFactory : UxmlFactory<PaletteElement, UxmlTraits> { }
        private event Action<PaletteElement> OnSelected;

        private VoxelColor _color;
        public VoxelColor Color
        {
            get => _color;
            private set
            {
                _color = value;
                this.style.backgroundColor = _color.Color;
                this.tooltip = GetColorTooltip(_color);
            }
        }

        public bool IsSelected
        {
            get => this.ClassListContains(PALETTE_ELEMENT_SELECTED_CLASS);
            set => this.EnableInClassList(PALETTE_ELEMENT_SELECTED_CLASS, value);
        }

        public void MoveToEnd()
        {
            this.SendToBack();
        }

        public void MoveToFront()
        {
            this.BringToFront();
        }

        public PaletteElement() : this(GetRandomColor(), null)
        {
        }

        public PaletteElement(Action<PaletteElement> onSelectCallback) : this(GetRandomColor(), onSelectCallback)
        {
        }

        public PaletteElement(VoxelColor color, Action<PaletteElement> onSelectCallback)
        {
            this.AddToClassList(PALETTE_ELEMENT_CLASS);
            this.RegisterCallback<MouseDownEvent>(OnMouseDown);
            this.style.backgroundColor = color.Color;
            this.name = string.Format("{0}-{1}", PALETTE_ELEMENT_CLASS, ColorUtility.ToHtmlStringRGBA(color.Color));
            this.tooltip = GetColorTooltip(color);
            this.OnSelected += onSelectCallback;

            this.Color = color;

            var menu = new ContextualMenuManipulator(OnContextMenu);
            this.AddManipulator(menu);
        }

        private void OnContextMenu(ContextualMenuPopulateEvent e)
        {
            e.menu.AppendAction("Edit", (a) => {
                Debug.Log("Edit");
            });
        }

        ~PaletteElement()
        {
            this.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.button == 0)
            {
                OnSelected?.Invoke(this);
            }
        }

        private string GetColorTooltip(VoxelColor color)
        {
            return string.Format("Color: {0}\nEmissive Intensity: {1}%\nMetallic: {2}%\nSmoothness: {3}%",
                ColorUtility.ToHtmlStringRGBA(color.Color),
                (100 * color.EmissiveIntensity).ToString("00"),
                (100 * color.Metallic).ToString("00"),
                (100 * color.Smoothness).ToString("00")
            );
        }

        private static VoxelColor GetRandomColor()
        {
            VoxelColor color = new VoxelColor();
            color.Color = UnityEngine.Random.ColorHSV(0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f);
            color.EmissiveIntensity = UnityEngine.Random.Range(0.0f, 1.0f);
            color.Metallic = UnityEngine.Random.Range(0.0f, 1.0f);
            color.Smoothness = UnityEngine.Random.Range(0.0f, 1.0f);
            return color;
        }
    }
}
