using Codice.CM.Common.Tree;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class NewFoxEditWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("FoxEdit/WIP/NewFoxEditWindow")]
    public static void ShowExample()
    {
        NewFoxEditWindow wnd = GetWindow<NewFoxEditWindow>();
        wnd.titleContent = new GUIContent("NewFoxEditWindow");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        m_VisualTreeAsset.CloneTree(root);
    }
}
