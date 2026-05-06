using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FoxEdit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class PaletteItem : VisualElement
    {
        #region Class names
        public const string PALETTE_ITEM_CLASS_NAME = "palette-item";
        public const string PALETTE_ITEM_SELECTED_CLASS_NAME = "palette-item-selected";
        #endregion

        public new class UxmlFactory : UxmlFactory<PaletteItem, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
        }

        private VoxelColor _color = null;
        public VoxelColor Color
        {
            get { return _color; }
            set
            {
                _color = value;
                this.style.backgroundColor = _color.Color;
                Color invColor = _color.Color.GetInvertedColor();
                this.style.borderTopColor = invColor;
                this.style.borderBottomColor = invColor;
                this.style.borderLeftColor = invColor;
                this.style.borderRightColor = invColor;
                this.tooltip = GetTooltip();
            }
        }

        public static event Action<int> OnSelectPaletteItem = null;
        private int _colorIndex = -1;

        public PaletteItem()
        {
            this.AddToClassList(PALETTE_ITEM_CLASS_NAME);
            this.style.backgroundColor = FoxEditColorUtility.GetRandomColor();

            this.RegisterCallback<ClickEvent>(OnElementClicked);
        }

        public void Setup(VoxelColor color, int colorIndex)
        {
            Color = color;
            _colorIndex = colorIndex;
        }

        private void OnElementClicked(ClickEvent evt)
        {
            if (_colorIndex == -1 || Color == null)
            {
                Debug.LogError("ColorPalette element is not setup");
            }
            OnSelectPaletteItem?.Invoke(_colorIndex);
        }

        public void SetSelected(bool selected)
        {
            this.EnableInClassList(PALETTE_ITEM_SELECTED_CLASS_NAME, selected);
        }

        private string GetTooltip()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(string.Format("Color: {0}", ColorNameUtility.GetClosestColorName(_color.Color)));
            stringBuilder.AppendLine(string.Format("Emissive: {0}", _color.EmissiveIntensity));
            stringBuilder.AppendLine(string.Format("Metallic: {0}%", Mathf.Round(_color.Metallic * 100)));
            stringBuilder.Append(string.Format("Smoothness: {0}%", Mathf.Round(_color.Smoothness * 100)));

            return stringBuilder.ToString();
        }
    }
}