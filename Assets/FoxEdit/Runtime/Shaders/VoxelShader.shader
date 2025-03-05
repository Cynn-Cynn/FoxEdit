Shader "Voxel/VoxelShader"
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
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                uint voxelID : TEXCOORD0;
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

            UNITY_INSTANCING_BUFFER_START(Props)
                uint _InstanceStart;
                float3 _Scale;
            UNITY_INSTANCING_BUFFER_END(Props)

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float4x4> _TransformMatrices;
            StructuredBuffer<ColorData> _Colors;
            StructuredBuffer<int> _ColorIndices;
            StructuredBuffer<int> _VoxelIndices;

            float4x4 GetNormalObjectToWorldMatrix(float4x4 objectToWorldMatrix)
            {
                float4x4 normalObjectToWorld = objectToWorldMatrix;

                normalObjectToWorld[0][3] = 0;
                normalObjectToWorld[1][3] = 0;
                normalObjectToWorld[2][3] = 0;

                normalObjectToWorld[0][0] /= _Scale.x;
                normalObjectToWorld[1][0] /= _Scale.x;
                normalObjectToWorld[2][0] /= _Scale.x;

                normalObjectToWorld[0][1] /= _Scale.y;
                normalObjectToWorld[1][1] /= _Scale.y;
                normalObjectToWorld[2][1] /= _Scale.y;

                normalObjectToWorld[0][2] /= _Scale.z;
                normalObjectToWorld[1][2] /= _Scale.z;
                normalObjectToWorld[2][2] /= _Scale.z;

                return normalObjectToWorld;
            }

            v2f vert(appdata v, uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                uint voxelID = instanceID + _InstanceStart;
                float4x4 objectToVoxelMatrix = _TransformMatrices[instanceID];
                float4x4 normalObjectToVoxelMatrix = GetNormalObjectToWorldMatrix(objectToVoxelMatrix);

                float4 positionOS = float4(_Positions[vertexID], 1.0);
                float4 normalOS = float4(0.0, 1.0, 0.0, 1.0);
                float4 tangeantOS = float4(-1.0, 0.0, 0.0, 1.0);

                float4 positionWS = mul(objectToVoxelMatrix, positionOS);
                float3 normalWS =  mul(normalObjectToVoxelMatrix, normalOS).xyz;
                float4 tangentWS = float4(mul(normalObjectToVoxelMatrix, tangeantOS).xyz, -1.0);

                v2f o;

                o.positionWS = positionWS.xyz;
                o.positionCS = TransformWorldToHClip(positionWS.xyz);

                o.normalWS = normalWS;

                float sign = tangentWS.w;
                o.tangentWS = float4(tangentWS.xyz, sign);

                o.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS.xyz);

                o.shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);

                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
                o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unityDynamicLightmapST.xy + unityDynamicLightmapST.zw;
#endif
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);

                o.voxelID = voxelID;

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

#if USE_EMISSION_ON
                float3 emission = float3(0.0, 0.0, 0.0);
                if (color.emissive > 0.0)
                     emission = color.color.rbg * color.emissive;
                surfaceData.emission = emission;
#endif

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

#if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGO = SAMPLE_GI(i.staticLightmapUV, i.dynamicLightmapUV, i.vertexSH, inputData.normalWS);
#else
                inputData.bakedGI = SAMPLE_GI(i.staticLightmapUV, i.vertexSH, inputData.normalWS);
#endif
                inputData.shadowMask = SAMPLE_SHADOWMASK(i.staticLightmapUV);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                return inputData;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                int voxelIndex = _VoxelIndices[i.voxelID];
                int colorIndex = _ColorIndices[voxelIndex];
                ColorData color = _Colors[colorIndex];

                SurfaceData surfaceData = createSurfaceData(i, color);
                InputData inputData = createInputData(i, surfaceData.normalTS);

                return UniversalFragmentPBR(inputData, surfaceData);
            }
        ENDHLSL
        }

        Pass
        {
            Tags{ "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct v2f
                {
                    float4 positionCS : SV_Position;
                };

                StructuredBuffer<float3> _Positions;
                StructuredBuffer<float4x4> _TransformMatrices;

                float3 _Scale;

                float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
                {
                    Light mainLight = GetMainLight();
                    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, mainLight.direction));
     
#if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
     
                    return positionCS;
                }

                float4x4 GetNormalObjectToWorldMatrix(float4x4 objectToWorldMatrix)
                {
                    float4x4 normalObjectToWorld = objectToWorldMatrix;

                    normalObjectToWorld[0][3] = 0;
                    normalObjectToWorld[1][3] = 0;
                    normalObjectToWorld[2][3] = 0;

                    normalObjectToWorld[0][0] /= _Scale.x;
                    normalObjectToWorld[1][0] /= _Scale.x;
                    normalObjectToWorld[2][0] /= _Scale.x;

                    normalObjectToWorld[0][1] /= _Scale.y;
                    normalObjectToWorld[1][1] /= _Scale.y;
                    normalObjectToWorld[2][1] /= _Scale.y;

                    normalObjectToWorld[0][2] /= _Scale.z;
                    normalObjectToWorld[1][2] /= _Scale.z;
                    normalObjectToWorld[2][2] /= _Scale.z;

                    return normalObjectToWorld;
                }

                v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
                {
                    float4x4 objectToWorldMatrix = _TransformMatrices[instanceID];

                    v2f o;

                    float4 vertexPosition = float4(_Positions[vertexID], 1.0f);
                    float3 positionWS = mul(objectToWorldMatrix, vertexPosition).xyz;

                    float4x4 normalObjectToWorld = GetNormalObjectToWorldMatrix(objectToWorldMatrix);
                    float3 normalWS = mul(normalObjectToWorld, float4(0.0f, 1.0f, 0.0f, 1.0f)).xyz;

                    o.positionCS = GetShadowPositionHClip(positionWS, normalWS);

                    return o;
                }

                float4 frag(v2f i) : SV_TARGET
                {
                    return 0;
                }
            ENDHLSL
        }
    }

    Fallback Off
}
