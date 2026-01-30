using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    public static class FoxEditMenuItems
    {
        [MenuItem("FoxEdit/Brush &R", false, 1)]
        private static void SelectBrushShortcut()
        {
            VoxelEditor.Tool = VoxelTools.vxTool.Brush;
        }

        [MenuItem("FoxEdit/Fill &F", false, 2)]
        private static void SelectFillShortcut()
        {
            VoxelEditor.Tool = VoxelTools.vxTool.Fill;
        }

        [MenuItem("FoxEdit/Paint &1", false, 3)]
        private static void SelectPaint()
        {
            VoxelEditor.Action = VoxelTools.vxAction.Paint;
        }

        [MenuItem("FoxEdit/Erase &2", false, 4)]
        private static void SelectEraseShortcut()
        {
            VoxelEditor.Action = VoxelTools.vxAction.Erase;
        }

        [MenuItem("FoxEdit/Color &3", false, 5)]
        private static void SelectColorShortcut()
        {
            VoxelEditor.Action = VoxelTools.vxAction.Color;
        }

        [MenuItem("FoxEdit/New Frame &N", false, 6)]
        private static void NewFrameShortcut()
        {
            if (FoxEditManager.VoxelEditor != null)
            {
                FoxEditManager.VoxelEditor.NewFrame();
            }
        }

        [MenuItem("FoxEdit/Duplicate Frame &D", false, 7)]
        private static void DuplicateFrameShortcut()
        {
            FoxEditManager.VoxelEditor.DuplicateFrame();
        }

        [MenuItem("FoxEdit/Delete Frame &X", false, 8)]
        private static void DeleteFrameShortcut()
        {
            FoxEditManager.VoxelEditor.DeleteFrame();
        }
    }
}