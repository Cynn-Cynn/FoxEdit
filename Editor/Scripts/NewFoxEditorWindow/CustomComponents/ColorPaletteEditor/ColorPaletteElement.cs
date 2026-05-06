using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class ColorPaletteElement : VisualElement
    {
        #region Class names
        public const string COLOR_PALETTE_CLASS_NAME = "color-palette";
        public const string COLOR_PALETTE_ITEMS_CONTAINER_CLASS_NAME = "color-palette-items-container";
        #endregion


        public new class UxmlFactory : UxmlFactory<ColorPaletteElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlIntAttributeDescription ItemIndexAttr = new UxmlIntAttributeDescription
            {
                name = "ItemIndex",
                defaultValue = 0,
            };

            UxmlIntAttributeDescription PaletteSizeAttr = new UxmlIntAttributeDescription
            {
                name = "PaletteSize",
                defaultValue = 5,
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ColorPaletteElement colorPaletteElement = ve as ColorPaletteElement;

                colorPaletteElement.ItemIndex = ItemIndexAttr.GetValueFromBag(bag, cc);
                colorPaletteElement.PaletteSize = PaletteSizeAttr.GetValueFromBag(bag, cc);
            }
        }

        public event Action<int> OnIndexChanged;

        private VisualElement itemsContainer = null;
        private VoxelPalette voxelPalette = null;
        private List<PaletteItem> _paletteItems = new List<PaletteItem>();
        private PaletteItem lastSelectedPaletteItem = null;
        private VisualElement addColorButton;

        private int _itemIndex = -1;
        public int ItemIndex
        {
            get => _itemIndex;
            set => SetIndexValue(value);
        }

        private int _paletteSize = 0;
        public int PaletteSize
        {
            get => _paletteSize;
            set
            {
                _paletteSize = value;
                UpdatePaletteSize();
            }
        }

        public void SetIndexValue(int value, bool notify = true)
        {
            if (value >= _paletteItems.Count || value < 0)
                return;
            if (lastSelectedPaletteItem != null)
                lastSelectedPaletteItem.SetSelected(false);
            lastSelectedPaletteItem = _paletteItems[value];
            lastSelectedPaletteItem.SetSelected(true);
            _itemIndex = value;
            if (notify)
                OnIndexChanged?.Invoke(_itemIndex);
        }


        public ColorPaletteElement()
        {
            this.AddToClassList(COLOR_PALETTE_CLASS_NAME);

            PaletteItem.OnSelectPaletteItem += OnSelectPaletteItem;

            itemsContainer = new VisualElement();
            itemsContainer.name = "items-container";
            itemsContainer.AddToClassList(COLOR_PALETTE_ITEMS_CONTAINER_CLASS_NAME);

            SetupAddColorButton();

            this.Add(itemsContainer);
        }


        ~ColorPaletteElement()
        {
            PaletteItem.OnSelectPaletteItem -= OnSelectPaletteItem;
        }

        private void SetupAddColorButton()
        {
            addColorButton = new VisualElement();
            addColorButton.name = "add-color-button";

            VisualElement addColorButtonIcon = new VisualElement();
            addColorButtonIcon.name = "add-color-button-icon";
            addColorButton.Add(addColorButtonIcon);

            itemsContainer.Add(addColorButton);

            addColorButton.RegisterCallback<ClickEvent>(OnClickAddColor);
        }

        private void OnClickAddColor(ClickEvent clickEvent)
        {
            AddPaletteItem(VoxelColor.GetRandomColor());
        }

        private void UpdatePaletteSize()
        {
            if (_paletteSize == _paletteItems.Count || _paletteSize < 0)
            {
                return;
            }
            else if (_paletteItems.Count < _paletteSize)
            {
                int diff = _paletteSize - _paletteItems.Count;

                for (int i = 0; i < diff; i++)
                    AddPaletteItem(VoxelColor.GetRandomColor());
            }
            else
            {
                ClearPaletteItems();
                for (int i = 0; i < _paletteSize; i++)
                    AddPaletteItem(VoxelColor.GetRandomColor());
            }
        }

        public void ClearPaletteItems()
        {
            foreach (PaletteItem paletteItem in _paletteItems)
                paletteItem.RemoveFromHierarchy();
            _paletteItems.Clear();
        }


        private void OnSelectPaletteItem(int paletteItemIndex)
        {
            ItemIndex = paletteItemIndex;
        }

        public void Setup(VoxelPalette voxelPalette)
        {
            ClearPaletteItems();
            foreach (VoxelColor voxelColor in voxelPalette.Colors)
                AddPaletteItem(voxelColor);
            _paletteSize = voxelPalette.Colors.Length;
            this.voxelPalette = voxelPalette;
        }

        public void AddPaletteItem(VoxelColor voxelColor)
        {
            int newPaletteItemIndex = _paletteItems.Count;
            PaletteItem paletteItem = new PaletteItem();

            paletteItem.name = string.Format("color-{0}", newPaletteItemIndex);
            paletteItem.Setup(voxelColor, newPaletteItemIndex);
            _paletteItems.Add(paletteItem);
            itemsContainer.Add(paletteItem);

            addColorButton.BringToFront();
        }
    }
}