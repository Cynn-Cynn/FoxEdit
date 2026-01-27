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

        private VisualElement _thumbnail;
        private Label _label;

        private Texture2D _thumbnailImage;
        public Texture2D ThumbnailImage
        {
            get => _thumbnailImage;
            set
            {
                _thumbnailImage = value;
                _thumbnail.style.backgroundImage = value;
            }
        }

        private int _index = -1;
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                _label.text = string.Format("#{0}", value);
                name = string.Format("frame-{0}" , _index);
            }
        }

        public bool IsSelelected
        {
            get => ClassListContains(FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME);
            set => EnableInClassList(FRAME_SELECTOR_ITEM_SELECTED_CLASS_NAME, value);
        }

        public FrameButton()
        {
            pickingMode = PickingMode.Position;
            name = "frame";
            AddToClassList(FRAME_SELECTOR_ITEM_CLASS_NAME);

            _label = new Label();
            _label.AddToClassList(FRAME_SELECTOR_ITEM_LABEL_CLASS_NAME);
            Index = 0;

            _thumbnail = new VisualElement();
            _thumbnail.AddToClassList(FRAME_SELECTOR_ITEM_THUMBNAIL_CLASS_NAME);
            _thumbnail.name = "thumbnail";

            Add(_thumbnail);
            Add(_label);
        }
    }
}