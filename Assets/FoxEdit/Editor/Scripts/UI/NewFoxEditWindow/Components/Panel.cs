using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEdit.WindowComponents
{
    public class Panel : VisualElement
    {
        private const string PANEL_CLASS_NAME = "panel";
        private const string TITLE_CLASS_NAME = "panel-title";
        private const string CONTENT_CLASS_NAME = "panel-content";

        public new class UxmlFactory : UxmlFactory<Panel, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlStringAttributeDescription titleAttr = new UxmlStringAttributeDescription()
            {
                name = "title",
                defaultValue = "Title"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Panel panel = ve as Panel;
                panel.Title = titleAttr.GetValueFromBag(bag, cc);
            }
        }

        private Label titleLabel;
        private VisualElement content;

        public string Title
        {
            get => titleLabel.text;
            set => titleLabel.text = value;
        }

        public Panel()
        {
            titleLabel = new Label();
            titleLabel.AddToClassList(TITLE_CLASS_NAME);
            titleLabel.name = "tile";
            hierarchy.Add(titleLabel);

            content = new VisualElement();
            content.AddToClassList(CONTENT_CLASS_NAME);
            content.name = "content";
            hierarchy.Add(content);

            AddToClassList(PANEL_CLASS_NAME);
        }

        public override VisualElement contentContainer => content;
    }
}