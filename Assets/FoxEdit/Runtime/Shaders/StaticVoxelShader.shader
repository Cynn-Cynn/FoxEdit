Shader "Voxel/StaticVoxelShader"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            Tags{
                "LightMode" = "UniversalForward"
            }

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local USE_EMISSION_ON __

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_local _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_local _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_local_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_local_fragment _ _REFLECTIN_PROBE_BLENDING
            #pragma multi_compile_local_fragment _ _REFLECTION_PROBE_BOX_P0ROJECTION
            #pragma multi_compile_local_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_local_fragment _ _SCREEN_SPACE_OCCLUSION

            #pragma multi_compile_local _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_local _ SHADOWS_SHADOWMASK
            #pragma multi_compile_local _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_local _ LIGHTMAP_ON
            #pragma multi_compile_local _ DYNAMICLIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                uint colorIndex : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                float2 dynamicLightmapUV : TEXCOORD7;
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
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);

                o.positionWS = vertexInput.positionWS;
                o.positionCS = vertexInput.positionCS;

                o.colorIndex = v.uv.x;

                o.normalWS = normalInput.normalWS;

                float sign = v.tangentOS.w;
                o.tangentWS = float4(normalInput.tangentWS.xyz, sign);

                o.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                o.shadowCoord = GetShadowCoord(vertexInput);

                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
// #ifdef DYNAMICLIGHTMAP_ON
//                 o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unityDynamicLightmapST.xy + unityDynamicLightmapST.zw;
// #endif
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);

                return o;
            }

            SurfaceData createSurfaceData(v2f i, ColorData color)
            {
                SurfaceData surfaceData = (SurfaceData)0;

                surfaceData.albedo = color.color.rgb;

                surfaceData.metallic = 0.0;
                surfaceData.metallic = color.metallic;

                surfaceData.smoothness = 1.0;
                surfaceData.smoothness = color.smoothness;

                float3 normalSample = UnpackNormal(float4(1, 0.5, 0.5, 0.5));
                surfaceData.normalTS = normalSample;

                float3 emission = float3(0.0, 0.0, 0.0);
                if (color.emissive > 0.0)
                     emission = color.color.rgb * color.emissive;
                surfaceData.emission = emission;

                surfaceData.occlusion = 1.0;

                surfaceData.alpha = color.color.a;

                return surfaceData;
            }

            InputData createInputData(v2f i, float3 normalTS)
            {
                InputData inputData = (InputData)0;

                inputData.positionWS = i.positionWS;

                float3 bitangent = i.tangentWS.w * cross(i.normalWS, i.tangentWS.xyz);
                inputData.tangentToWorld = float3x3(i.tangentWS.xyz, bitangent, i.normalWS);
                inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);

                inputData.viewDirectionWS = SafeNormalize(i.viewDirWS);

                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);

// #if defined(DYNAMICLIGHTMAP_ON)
//                 inputData.bakedGO = SAMPLE_GI(i.staticLightmapUV, i.dynamicLightmapUV, i.vertexSH, inputData.normalWS);
// #else
                inputData.bakedGI = SAMPLE_GI(i.staticLightmapUV, i.vertexSH, inputData.normalWS);
//#endif
                inputData.shadowMask = SAMPLE_SHADOWMASK(i.staticLightmapUV);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                return inputData;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                ColorData color = _Colors[i.colorIndex];

                SurfaceData surfaceData = createSurfaceData(i, color);
                InputData inputData = createInputData(i, surfaceData.normalTS);

                return UniversalFragmentPBR(inputData, surfaceData);
            }
        ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"

            Tags { "Lightmode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"


            ENDHLSL
        }
    }

    Fallback Off
}
