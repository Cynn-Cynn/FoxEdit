using System;
using FoxEdit;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ColorEditorPopUp : EditorWindow
{
    public static void Open(Action<VoxelColor> newColorCallback, bool liveUpdate, bool invokeOnCancel)
    {
        Open(newColorCallback, VoxelColor.GetRandomColor(), liveUpdate, invokeOnCancel);
    }

    public static void Open(Action<VoxelColor> newColorCallback, VoxelColor startVoxelColor, bool liveUpdate, bool invokeOnCancel)
    {
        ColorEditorPopUp colorEditorPopUp = EditorWindow.CreateInstance<ColorEditorPopUp>();

        colorEditorPopUp.ShowPopup();
        colorEditorPopUp.Setup(newColorCallback, startVoxelColor, liveUpdate, invokeOnCancel);
    }

    private Action<VoxelColor> newColorCallback;
    private VoxelColor startVoxelColor;
    private bool liveUpdate = false;
    private bool liveUpdateOn = false;
    private bool invokeOnCancel = false;
    private bool updatable = false;

    private VoxelColor newVoxelColor;
    private Color newColor = Color.clear;
    private float newEmissive = -1;
    private float newMetallic = -1;
    private float newSmoothness = -1f;

    public void Setup(Action<VoxelColor> newColorCallback, VoxelColor startVoxelColor, bool liveUpdate, bool invokeOnCancel)
    {
        this.newColorCallback = newColorCallback;
        this.startVoxelColor = startVoxelColor;
        this.liveUpdate = liveUpdate;
        this.invokeOnCancel = invokeOnCancel;

        newVoxelColor = new VoxelColor(startVoxelColor);
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        newColor = EditorGUILayout.ColorField(newVoxelColor.Color);
        if (newColor != newVoxelColor.Color)
        {
            newVoxelColor.Color = newColor;
            updatable = true;
        }
        newEmissive = EditorGUILayout.FloatField("Emissive", newVoxelColor.EmissiveIntensity);
        if (newEmissive != newVoxelColor.EmissiveIntensity)
        {
            newVoxelColor.EmissiveIntensity = newEmissive;
            updatable = true;
        }
        newMetallic = EditorGUILayout.Slider("Metallic", newVoxelColor.Metallic, 0f, 1f);
        if (newMetallic != newVoxelColor.Metallic)
        {
            newVoxelColor.Metallic = newMetallic;
            updatable = true;
        }
        newSmoothness = EditorGUILayout.Slider("Smothness", newVoxelColor.Smoothness, 0f, 1f);
        if (newSmoothness != newVoxelColor.Smoothness)
        {
            newVoxelColor.Smoothness = newSmoothness;
            updatable = true;
        }
        if (liveUpdate)
            liveUpdateOn = EditorGUILayout.Toggle("Live Update", liveUpdateOn);
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply"))
            Apply();
        if (GUILayout.Button("Cancel"))
            Cancel();
        GUILayout.EndHorizontal();

        SetWindowHeight();
        if (liveUpdateOn && updatable)
            newColorCallback?.Invoke(newVoxelColor);

        updatable = false;
    }

    private void SetWindowHeight()
    {
        if (Event.current.type == EventType.Repaint)
        {
            float h = GUILayoutUtility.GetLastRect().yMax + 10;

            if (Mathf.Abs(position.height - h) > 1)
                position = new Rect(position.x, position.y, position.width, h);
        }
    }

    private void Apply()
    {
        newColorCallback?.Invoke(newVoxelColor);
        newColorCallback = null;
        Close();
    }

    private void Cancel()
    {
        newColorCallback = null;
        Close();
    }
}
