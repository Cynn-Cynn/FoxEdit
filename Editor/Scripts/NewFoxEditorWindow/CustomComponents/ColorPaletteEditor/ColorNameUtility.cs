using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorNameUtility : MonoBehaviour
{
    private struct ColorName
    {
        public Color color;
        public string name;

        public ColorName(string name, Color color)
        {
            this.color = color;
            this.name = name;
        }
    }

    private static readonly ColorName[] colorNames =
    {
    #region Color names
        new("AliceBlue", new Color(0.94f, 0.97f, 1.00f)),
        new("AntiqueWhite", new Color(0.98f, 0.92f, 0.84f)),
        new("Aqua", new Color(0.00f, 1.00f, 1.00f)),
        new("Aquamarine", new Color(0.50f, 1.00f, 0.83f)),
        new("Azure", new Color(0.94f, 1.00f, 1.00f)),
        new("Beige", new Color(0.96f, 0.96f, 0.86f)),
        new("Bisque", new Color(1.00f, 0.89f, 0.77f)),
        new("Black", new Color(0.00f, 0.00f, 0.00f)),
        new("BlanchedAlmond", new Color(1.00f, 0.92f, 0.80f)),
        new("Blue", new Color(0.00f, 0.00f, 1.00f)),
        new("BlueViolet", new Color(0.54f, 0.17f, 0.89f)),
        new("Brown", new Color(0.65f, 0.16f, 0.16f)),
        new("BurlyWood", new Color(0.87f, 0.72f, 0.53f)),
        new("CadetBlue", new Color(0.37f, 0.62f, 0.63f)),
        new("Chartreuse", new Color(0.50f, 1.00f, 0.00f)),
        new("Chocolate", new Color(0.82f, 0.41f, 0.12f)),
        new("Coral", new Color(1.00f, 0.50f, 0.31f)),
        new("CornflowerBlue", new Color(0.39f, 0.58f, 0.93f)),
        new("Cornsilk", new Color(1.00f, 0.97f, 0.86f)),
        new("Crimson", new Color(0.86f, 0.08f, 0.24f)),
        new("Cyan", new Color(0.00f, 1.00f, 1.00f)),
        new("DarkBlue", new Color(0.00f, 0.00f, 0.55f)),
        new("DarkCyan", new Color(0.00f, 0.55f, 0.55f)),
        new("DarkGoldenRod", new Color(0.72f, 0.53f, 0.04f)),
        new("DarkGray", new Color(0.66f, 0.66f, 0.66f)),
        new("DarkGreen", new Color(0.00f, 0.39f, 0.00f)),
        new("DarkKhaki", new Color(0.74f, 0.72f, 0.42f)),
        new("DarkMagenta", new Color(0.55f, 0.00f, 0.55f)),
        new("DarkOliveGreen", new Color(0.33f, 0.42f, 0.18f)),
        new("DarkOrange", new Color(1.00f, 0.55f, 0.00f)),
        new("DarkOrchid", new Color(0.60f, 0.20f, 0.80f)),
        new("DarkRed", new Color(0.55f, 0.00f, 0.00f)),
        new("DarkSalmon", new Color(0.91f, 0.59f, 0.48f)),
        new("DarkSeaGreen", new Color(0.56f, 0.74f, 0.56f)),
        new("DarkSlateBlue", new Color(0.28f, 0.24f, 0.55f)),
        new("DarkSlateGray", new Color(0.18f, 0.31f, 0.31f)),
        new("DarkTurquoise", new Color(0.00f, 0.81f, 0.82f)),
        new("DarkViolet", new Color(0.58f, 0.00f, 0.83f)),
        new("DeepPink", new Color(1.00f, 0.08f, 0.58f)),
        new("DeepSkyBlue", new Color(0.00f, 0.75f, 1.00f)),
        new("DimGray", new Color(0.41f, 0.41f, 0.41f)),
        new("DodgerBlue", new Color(0.12f, 0.56f, 1.00f)),
        new("FireBrick", new Color(0.70f, 0.13f, 0.13f)),
        new("FloralWhite", new Color(1.00f, 0.98f, 0.94f)),
        new("ForestGreen", new Color(0.13f, 0.55f, 0.13f)),
        new("Fuchsia", new Color(1.00f, 0.00f, 1.00f)),
        new("Gainsboro", new Color(0.86f, 0.86f, 0.86f)),
        new("GhostWhite", new Color(0.97f, 0.97f, 1.00f)),
        new("Gold", new Color(1.00f, 0.84f, 0.00f)),
        new("GoldenRod", new Color(0.85f, 0.65f, 0.13f)),
        new("Gray", new Color(0.50f, 0.50f, 0.50f)),
        new("Green", new Color(0.00f, 0.50f, 0.00f)),
        new("GreenYellow", new Color(0.68f, 1.00f, 0.18f)),
        new("HoneyDew", new Color(0.94f, 1.00f, 0.94f)),
        new("HotPink", new Color(1.00f, 0.41f, 0.71f)),
        new("IndianRed", new Color(0.80f, 0.36f, 0.36f)),
        new("Indigo", new Color(0.29f, 0.00f, 0.51f)),
        new("Ivory", new Color(1.00f, 1.00f, 0.94f)),
        new("Khaki", new Color(0.94f, 0.90f, 0.55f)),
        new("Lavender", new Color(0.90f, 0.90f, 0.98f)),
        new("LavenderBlush", new Color(1.00f, 0.94f, 0.96f)),
        new("LawnGreen", new Color(0.49f, 0.99f, 0.00f)),
        new("LemonChiffon", new Color(1.00f, 0.98f, 0.80f)),
        new("LightBlue", new Color(0.68f, 0.85f, 0.90f)),
        new("LightCoral", new Color(0.94f, 0.50f, 0.50f)),
        new("LightCyan", new Color(0.88f, 1.00f, 1.00f)),
        new("LightGoldenRodYellow", new Color(0.98f, 0.98f, 0.82f)),
        new("LightGray", new Color(0.83f, 0.83f, 0.83f)),
        new("LightGreen", new Color(0.56f, 0.93f, 0.56f)),
        new("LightPink", new Color(1.00f, 0.71f, 0.76f)),
        new("LightSalmon", new Color(1.00f, 0.63f, 0.48f)),
        new("LightSeaGreen", new Color(0.13f, 0.70f, 0.67f)),
        new("LightSkyBlue", new Color(0.53f, 0.81f, 0.98f)),
        new("LightSlateGray", new Color(0.47f, 0.53f, 0.60f)),
        new("LightSteelBlue", new Color(0.69f, 0.77f, 0.87f)),
        new("LightYellow", new Color(1.00f, 1.00f, 0.88f)),
        new("Lime", new Color(0.00f, 1.00f, 0.00f)),
        new("LimeGreen", new Color(0.20f, 0.80f, 0.20f)),
        new("Linen", new Color(0.98f, 0.94f, 0.90f)),
        new("Magenta", new Color(1.00f, 0.00f, 1.00f)),
        new("Maroon", new Color(0.50f, 0.00f, 0.00f)),
        new("MediumAquaMarine", new Color(0.40f, 0.80f, 0.67f)),
        new("MediumBlue", new Color(0.00f, 0.00f, 0.80f)),
        new("MediumOrchid", new Color(0.73f, 0.33f, 0.83f)),
        new("MediumPurple", new Color(0.58f, 0.44f, 0.86f)),
        new("MediumSeaGreen", new Color(0.24f, 0.70f, 0.44f)),
        new("MediumSlateBlue", new Color(0.48f, 0.41f, 0.93f)),
        new("MediumSpringGreen", new Color(0.00f, 0.98f, 0.60f)),
        new("MediumTurquoise", new Color(0.28f, 0.82f, 0.80f)),
        new("MediumVioletRed", new Color(0.78f, 0.08f, 0.52f)),
        new("MidnightBlue", new Color(0.10f, 0.10f, 0.44f)),
        new("MintCream", new Color(0.96f, 1.00f, 0.98f)),
        new("MistyRose", new Color(1.00f, 0.89f, 0.88f)),
        new("Moccasin", new Color(1.00f, 0.89f, 0.71f)),
        new("NavajoWhite", new Color(1.00f, 0.87f, 0.68f)),
        new("Navy", new Color(0.00f, 0.00f, 0.50f)),
        new("OldLace", new Color(0.99f, 0.96f, 0.90f)),
        new("Olive", new Color(0.50f, 0.50f, 0.00f)),
        new("OliveDrab", new Color(0.42f, 0.56f, 0.14f)),
        new("Orange", new Color(1.00f, 0.65f, 0.00f)),
        new("OrangeRed", new Color(1.00f, 0.27f, 0.00f)),
        new("Orchid", new Color(0.85f, 0.44f, 0.84f)),
        new("PaleGoldenRod", new Color(0.93f, 0.91f, 0.67f)),
        new("PaleGreen", new Color(0.60f, 0.98f, 0.60f)),
        new("PaleTurquoise", new Color(0.69f, 0.93f, 0.93f)),
        new("PaleVioletRed", new Color(0.86f, 0.44f, 0.58f)),
        new("PapayaWhip", new Color(1.00f, 0.94f, 0.84f)),
        new("PeachPuff", new Color(1.00f, 0.85f, 0.73f)),
        new("Peru", new Color(0.80f, 0.52f, 0.25f)),
        new("Pink", new Color(1.00f, 0.75f, 0.80f)),
        new("Plum", new Color(0.87f, 0.63f, 0.87f)),
        new("PowderBlue", new Color(0.69f, 0.88f, 0.90f)),
        new("Purple", new Color(0.50f, 0.00f, 0.50f)),
        new("RebeccaPurple", new Color(0.40f, 0.20f, 0.60f)),
        new("Red", new Color(1.00f, 0.00f, 0.00f)),
        new("RosyBrown", new Color(0.74f, 0.56f, 0.56f)),
        new("RoyalBlue", new Color(0.25f, 0.41f, 0.88f)),
        new("SaddleBrown", new Color(0.55f, 0.27f, 0.07f)),
        new("Salmon", new Color(0.98f, 0.50f, 0.45f)),
        new("SandyBrown", new Color(0.96f, 0.64f, 0.38f)),
        new("SeaGreen", new Color(0.18f, 0.55f, 0.34f)),
        new("SeaShell", new Color(1.00f, 0.96f, 0.93f)),
        new("Sienna", new Color(0.63f, 0.32f, 0.18f)),
        new("Silver", new Color(0.75f, 0.75f, 0.75f)),
        new("SkyBlue", new Color(0.53f, 0.81f, 0.92f)),
        new("SlateBlue", new Color(0.42f, 0.35f, 0.80f)),
        new("SlateGray", new Color(0.44f, 0.50f, 0.56f)),
        new("Snow", new Color(1.00f, 0.98f, 0.98f)),
        new("SpringGreen", new Color(0.00f, 1.00f, 0.50f)),
        new("SteelBlue", new Color(0.27f, 0.51f, 0.71f)),
        new("Tan", new Color(0.82f, 0.71f, 0.55f)),
        new("Teal", new Color(0.00f, 0.50f, 0.50f)),
        new("Thistle", new Color(0.85f, 0.75f, 0.85f)),
        new("Tomato", new Color(1.00f, 0.39f, 0.28f)),
        new("Turquoise", new Color(0.25f, 0.88f, 0.82f)),
        new("Violet", new Color(0.93f, 0.51f, 0.93f)),
        new("Wheat", new Color(0.96f, 0.87f, 0.70f)),
        new("White", new Color(1.00f, 1.00f, 1.00f)),
        new("WhiteSmoke", new Color(0.96f, 0.96f, 0.96f)),
        new("Yellow", new Color(1.00f, 1.00f, 0.00f)),
        new("YellowGreen", new Color(0.60f, 0.80f, 0.20f)),
    #endregion
    };


    public static string GetClosestColorName(Color input)
    {
        string closestName = "Inconnu";
        float closestDistance = float.MaxValue;

        foreach (var namedColor in colorNames)
        {
            float distance = DistanceHSV(input, namedColor.color);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestName = namedColor.name;
            }
        }

        return closestName;
    }

    private static float DistanceHSV(Color a, Color b)
    {
        Color.RGBToHSV(a, out float h1, out float s1, out float v1);
        Color.RGBToHSV(b, out float h2, out float s2, out float v2);

        float dh = Mathf.Min(Mathf.Abs(h1 - h2), 1f - Mathf.Abs(h1 - h2));
        float ds = Mathf.Abs(s1 - s2);
        float dv = Mathf.Abs(v1 - v2);

        return dh * 2f + ds + dv;
    }
}
