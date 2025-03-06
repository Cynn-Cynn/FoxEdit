using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FoxEditor.Editors
{
    public class VoxelEditorWindow : EditorWindow
    {
        private const int DEBUG_PALETTE_ELEMENT_COUNT = 50;
        private const string PALETTE_ROOT_NAME = "palette";
        private const string TOOLBAR_ROOT_NAME = "tools";
        private const string FRAMES_ROOT_NAME = "frames";

        [MenuItem("Window/FoxEdit/VoxelEditorWindow")]
        public static void ShowExample()
        {
            VoxelEditorWindow wnd = GetWindow<VoxelEditorWindow>();
            wnd.titleContent = new GUIContent("VoxelEditorWindow");
        }

        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;
        private VoxelEditorWindowPalette palette;

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            m_VisualTreeAsset.CloneTree(root);
            VisualElement paletteRoot = root.Q<VisualElement>(PALETTE_ROOT_NAME);
            VisualElement toolbarRoot = root.Q<VisualElement>(TOOLBAR_ROOT_NAME);
            VisualElement framesRoot = root.Q<VisualElement>(FRAMES_ROOT_NAME);
    
            SetupPalette(paletteRoot);
            SetupToolbar(toolbarRoot);
            SetupFrames(framesRoot);
        }

        private void SetupPalette(VisualElement paletteRoot)
        {
            List<VoxelColor> colors = new List<VoxelColor>();
            for (int i = 0; i < DEBUG_PALETTE_ELEMENT_COUNT; i++)
                colors.Add(VoxelColor.GetRandom());

            palette = new VoxelEditorWindowPalette(paletteRoot, colors, OnSelectPaletteElement);
        }

        private void SetupToolbar(VisualElement toolbarRoot)
        {
            Toolbar toolbar = new Toolbar(toolbarRoot, OnToolSelected);
        }

        private void SetupFrames(VisualElement framesRoot)
        {
            VoxelEditorWindowFrames frames = new VoxelEditorWindowFrames(framesRoot);
        }

        private void OnToolSelected(EFoxEditTool tool)
        {
            Debug.Log("Selected tool: " + tool);
        }

        private void OnSelectPaletteElement(PaletteElement element)
        {
            Debug.Log("Selected palette element: " + element.Color.Color);
        }
    }
}