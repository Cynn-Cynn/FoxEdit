using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    internal class ConfirmWindow : EditorWindow
    {
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save before exiting ?", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Yes"))
            {
                GetWindow<FoxEditWindow>().Save();
                Close();
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("No"))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
