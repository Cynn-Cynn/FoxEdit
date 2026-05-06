using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    public static class FoxEditColorUtility
    {
        public static Color GetRandomColor()
        {
            return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        public static Color GetInvertedColor(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            float negH = (h + 0.5f) % 1f;

            return Color.HSVToRGB(negH, s, v);
        }
    }
}