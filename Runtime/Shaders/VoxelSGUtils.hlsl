#ifndef VOXELSGUTILS_INCLUDED
#define VOXELSGUTILS_INCLUDED

struct ColorData
{
    float4 color;
    float emissive;
    float metallic;
    float smoothness;
};

StructuredBuffer<ColorData> _Colors;
uint _ColorCount;

void GetColorFromPalette_float(float colorIndex, out float4 color, out float emissive, out float metallic, out float smoothness)
{
#ifdef SHADERGRAPH_PREVIEW
    color = float4(0,0,0,1);
    emissive = 0.0f;
    metallic = 0.0f;
    smoothness = 0.0f;
#else
    if (colorIndex < _ColorCount)
    {
        ColorData colorData = _Colors[colorIndex];
        color = colorData.color;
        emissive = colorData.emissive;
        metallic = colorData.metallic;
        smoothness = colorData.smoothness;
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