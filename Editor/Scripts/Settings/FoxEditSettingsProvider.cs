using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoxEdit
{
    public static class FoxEditSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Project/FoxEdit", SettingsScope.Project)
            {
                label = "FoxEdit",
                guiHandler = (searchContext) =>
                {
                    FoxEditSettings settings = FoxEditSettings.GetSettings();

                    Editor editor = Editor.CreateEditor(settings);
                    if (editor != null)
                        editor.DrawDefaultInspector();

                    if (GUI.changed)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                    }
                },
                keywords = new HashSet<string> { "FoxEdit", "Voxel", "Palette" }
            };
            return provider;
        }
    }
}

