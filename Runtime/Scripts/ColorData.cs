using UnityEngine;

namespace FoxEdit
{
    internal struct ColorData
    {
        private Vector4 Color;
        float Emissive;
        float Metallic;
        float Smoothness;

        public ColorData(Vector4 color, float emissive, float metallic, float smoothness)
        {
            Color = color;
            Emissive = emissive;
            Metallic = metallic;
            Smoothness = smoothness;
        }
    }
}
