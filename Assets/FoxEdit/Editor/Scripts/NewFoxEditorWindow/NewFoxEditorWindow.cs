using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class NewFoxEditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/UI Toolkit/NewFoxEditorWindow")]
    public static void ShowExample()
    {
        NewFoxEditorWindow wnd = GetWindow<NewFoxEditorWindow>();
        wnd.titleContent = new GUIContent("NewFoxEditorWindow");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        m_VisualTreeAsset.CloneTree(root);
    }
}
