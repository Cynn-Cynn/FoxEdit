using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEditor.Editors
{
    public class VoxelEditorWindowFrames
    {
        private const string FRAME_CLASS = "frame-button";
        private const string FRAME_CLASS_ENABLED = "frame-button_enabled";
        private const string FRAMES_CONTAINER = "frames-buttons";
        private const int DEBUG_FRAME_COUNT = 10;
        private VisualElement root;
        private VisualElement framesContainer;
        private VisualElement addFrameButton;

        private VisualElement selectedFrame;

        public VoxelEditorWindowFrames(VisualElement root)
        {
            this.root = root;

            framesContainer = root.Q<VisualElement>(FRAMES_CONTAINER);

            framesContainer.Clear();

            for (int i = 0; i < DEBUG_FRAME_COUNT; i++)
                AddFrame(i);

            CreateAddFrameButton();
        }

        public VisualElement AddFrame(int index)
        {
            VisualElement frame = new VisualElement();
            Label label = new Label(index.ToString());
            frame.Add(label);
            framesContainer.Add(frame);
            frame.AddToClassList(FRAME_CLASS);

            frame.RegisterCallback<MouseDownEvent>(e => {
                if (e.button == 0)
                    OnSelectFrame(frame);
            });
            return frame;
        }

        private void OnSelectFrame(VisualElement frame)
        {
            if (selectedFrame != null)
                selectedFrame.EnableInClassList(FRAME_CLASS_ENABLED, false);

            selectedFrame = frame;
            selectedFrame.EnableInClassList(FRAME_CLASS_ENABLED, true);
        }

        private VisualElement CreateAddFrameButton()
        {
            VisualElement addFrameButton = new VisualElement();
            addFrameButton.name = "frame-button-add";
            VisualElement icon = new VisualElement();

            addFrameButton.Add(icon);
            addFrameButton.AddToClassList(FRAME_CLASS);
            framesContainer.Add(addFrameButton);
            addFrameButton.RegisterCallback<MouseDownEvent>(e => {
                if (e.button == 0)
                    AddFrame(framesContainer.childCount);
            });

            return addFrameButton;
        }

        private void OnPressAddFrame(MouseDownEvent e)
        {
            AddFrame(framesContainer.childCount);
            addFrameButton.BringToFront();
        }
    }
}