
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal class VoxelEditorAnimation
    {
        public string Name = null;
        public float FrameDuration = 1.0f;

        public VoxelEditorAnimation(string name, float frameDuration = 1.0f)
        {
            if (string.IsNullOrEmpty(name))
                name = "New animation";
            this.Name = name;

            if (frameDuration < 0.0f)
                frameDuration = 1.0f;
            FrameDuration = frameDuration;
        }
        public List<VoxelEditorFrame> frames = new List<VoxelEditorFrame>();

        public VoxelEditorFrame this[int index]
        {
            get
            {
                if (frames.Count == 0)
                    return null;
                return frames[index];
            }
            set => frames[index] = value;
        }

        public void AddFrame(VoxelEditorFrame voxelEditorFrame)
        {
            frames.Add(voxelEditorFrame);
        }

        public void RemoveFrameAt(int index)
        {
            if (index < 0 || index >= frames.Count)
            {
                Debug.LogError("Index out of bounds.");
                return;
            }

            frames.RemoveAt(index);
        }

        public VoxelEditorFrame Move(int oldIndex, int newIndex)
        {
            return frames.Move(oldIndex, newIndex);
        }

        public int FramesCount => frames.Count;
    }
}