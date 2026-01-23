using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class FrameButton : Button
    {
        public const string FRAME_SELECTOR_ITEM_CLASS_NAME = "frameselector-item";
        public const string FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME = "frameselector-item-selected";
        public const string FRAME_SELECTOR_ITEM_LABEL_CLASS_NAME = "frameselector-item-label";
        public const string FRAME_SELECTOR_ITEM_THUMBNAIL_CLASS_NAME = "frameselector-item-thumbnail";

        private VisualElement thumbnail;
        private Label label;

        private int _index = -1;
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                label.text = string.Format("#{0}", value);
                name = string.Format("frame-{0}" , _index);
            }
        }

        public FrameButton()
        {
            pickingMode = PickingMode.Position;
            name = "frame";
            AddToClassList(FRAME_SELECTOR_ITEM_CLASS_NAME);

            label = new Label();
            label.AddToClassList(FRAME_SELECTOR_ITEM_LABEL_CLASS_NAME);
            Index = 0;

            thumbnail = new VisualElement();
            thumbnail.AddToClassList(FRAME_SELECTOR_ITEM_THUMBNAIL_CLASS_NAME);
            thumbnail.name = "thumbnail";

            Add(thumbnail);
            Add(label);
        }

        public void SetTexture(Texture2D texture)
        {
            thumbnail.style.backgroundImage = texture;
        }

        public void SetSelected(bool isSelected)
        {
            EnableInClassList(FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME, isSelected);
        }
    }
}