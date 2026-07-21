#ifndef VOXELSGUTILS_INCLUDED
#define VOXELSGUTILS_INCLUDED

void GetColorFromPalette_float(float colorIndex, StructuredBuffer<ColorData> colorBuffer, float colorCount, out float4 color, out float emissive, out float metallic, out float smoothness)
{
#ifdef SHADERGRAPH_PREVIEW
    color = float4(0,0,0,1);
    emissive = 0.0f;
    metallic = 0.0f;
    smoothness = 0.0f;
#else
    if (colorIndex < colorCount)
    {
        ColorData colorData = colorBuffer[colorIndex];
        color = colorData.Color;
        emissive = colorData.Emissive;
        metallic = colorData.Metallic;
        smoothness = colorData.Smoothness;
    }
    else
    {
        color = float4(1,0,1,1);
        emissive = (sin(_Time.y * 10) * 0.5f + 0.5f) * 50;
        metallic = 0.0f;
        smoothness = 0.0f;
    }
#endif
}

#endif