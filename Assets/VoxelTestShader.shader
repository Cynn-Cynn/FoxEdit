Shader "Examples/BaseShader"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Tags{
                "LightMode" = "UniversalForward"
            }

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : Position;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_Position;
                float colorIndex : TEXCOORD0;
            };

            struct ColorData
            {
                float4 color;
                float emissive;
                float metallic;
                float smoothness;
            };

            StructuredBuffer<ColorData> _Colors;

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.colorIndex = v.uv.x;
                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                ColorData color = _Colors[i.colorIndex];
                return color.color;
            }
        ENDHLSL
        }
    }
}
