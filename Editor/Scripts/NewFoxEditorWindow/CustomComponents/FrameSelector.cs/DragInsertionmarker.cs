using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DragInsertionMarker : VisualElement
{
    public const string DRAG_INSERION_MARKER_CLASS_NAME = "drag-insertion-marker";
    public const int width = 2;
    public const int height = 50;

    public new class UxmlFactory : UxmlFactory<DragInsertionMarker, UxmlTraits> { }

    public DragInsertionMarker()
    {
        AddToClassList(DRAG_INSERION_MARKER_CLASS_NAME);
        style.width = width;
        style.height = height;

        pickingMode = PickingMode.Ignore;
    }
}
