using System;
using UnityEngine;

namespace FoxEdit
{
    [Serializable]
    public class VoxelColor
    {
        public Color Color = Color.black;
        public float EmissiveIntensity = 0.0f;
        [Range(0.0f, 1.0f)] public float Metallic = 0.0f;
        [Range(0.0f, 1.0f)] public float Smoothness = 1.0f;

        public VoxelColor() { }

        public VoxelColor(Color color, float emissiveIntensity, float metallic, float smoothness)
        {
            Color = color;
            EmissiveIntensity = Mathf.Max(0.0f, emissiveIntensity);
            Metallic = Mathf.Clamp01(metallic);
            Smoothness = Mathf.Clamp01(smoothness);
        }

        public static VoxelColor GetRandomColor()
        {
            VoxelColor color = new VoxelColor();
            color.Color = FoxEditColorUtility.GetRandomColor();
            color.EmissiveIntensity = UnityEngine.Random.Range(0.0f, 1.0f);
            color.Metallic = UnityEngine.Random.Range(0.0f, 1.0f);
            color.Smoothness = UnityEngine.Random.Range(0.0f, 1.0f);
            return color;
        }
    }
}
