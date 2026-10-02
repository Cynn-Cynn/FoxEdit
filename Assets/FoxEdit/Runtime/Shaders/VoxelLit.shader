Shader "Voxel/Lit"
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
            Cull Back
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            Tags{
                "LightMode" = "UniversalForward"
            }

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local USE_EMISSION_ON __

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_local _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_local _ _ADDITIONAL_LIGHTS_VERTEX
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

            #pragma multi_compile_local _ ANIMATED_VOXEL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

            struct appdata
            {
#if ANIMATED_VOXEL
                UNITY_VERTEX_INPUT_INSTANCE_ID
#else
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
#endif
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                float2 dynamicLightmapUV : TEXCOORD6;
                int colorIndex : TEXCOORD7;
            };

            struct ColorData
            {
                float4 color;
                float emissive;
                float metallic;
                float smoothness;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
#if ANIMATED_VOXEL
                uint _InstanceStartIndex;
                float4x4 _ObjectToWorld;
#endif
                uint _ColorCount;
            UNITY_INSTANCING_BUFFER_END(Props)

#if ANIMATED_VOXEL
            StructuredBuffer<float3> _Vertices;
            StructuredBuffer<int> _Quads;
#endif
            StructuredBuffer<ColorData> _Colors;
            
#if ANIMATED_VOXEL
            v2f vert(appdata v, uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;

                uint faceID = _InstanceStartIndex + instanceID;

                uint localVertexID = vertexID - (vertexID % 4);
                uint nextVertexID = (localVertexID + 1) % 4;
                uint previousVertexID = (localVertexID - 1 + 4) % 4;

                localVertexID = _Quads[faceID * 5 + localVertexID];
                nextVertexID = _Quads[faceID * 5 + nextVertexID];
                previousVertexID = _Quads[faceID * 5 + previousVertexID];

                float4 positionOS = float4(_Vertices[localVertexID], 1.0f);
                float4 nextPositionOS = float4(_Vertices[nextVertexID], 1.0f);
                float4 previousPositionOS = float4(_Vertices[previousVertexID], 1.0f);

                float3 tangeantOS = normalize(nextPositionOS.xyz - positionOS.xyz);
                float3 bitangeantOS = normalize(previousPositionOS.xyz - positionOS.xyz);

                float3 normalOS = cross(tangeantOS, bitangeantOS);

                float4 vertexPositionOS = float4(_Vertices[_Quads[faceID * 5 + vertexID]], 1.0f);
                o.positionWS = mul(_ObjectToWorld, vertexPositionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS.xyz);
                o.normalWS = normalize(mul(_ObjectToWorld, normalOS).xyz);
                o.tangentWS = float4(mul(_ObjectToWorld, float4(tangeantOS, 0)).xyz, 1);
                o.shadowCoord = TransformWorldToShadowCoord(o.positionWS.xyz);

                o.colorIndex = _Quads[faceID * 5 + 4];
#else
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
                o.shadowCoord = GetShadowCoord(vertexInput);
#endif
                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
                 o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unityDynamicLightmapST.xy + unityDynamicLightmapST.zw;
#endif
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

                surfaceData.normalTS = float3(0,0,-1);

                float3 emission = float3(0.0, 0.0, 0.0);
                if (color.emissive > 0.0)
                     emission = color.color.rgb * color.emissive;
                surfaceData.emission = emission;

                surfaceData.occlusion = 1.0;

                surfaceData.alpha = color.color.a;

                return surfaceData;
            }

            InputData createInputData(v2f i)
            {
                InputData inputData = (InputData)0;

                inputData.positionWS = i.positionWS;

                float3 normal = normalize(i.normalWS);
                float3 tangeant = normalize(i.tangentWS.xyz);

#if ANIMATED_VOXEL
                float3 bitangent = normalize(cross(normal, tangeant));
#else
                float3 bitangent = i.tangentWS.w * cross(normal, tangeant);
#endif
                inputData.tangentToWorld = float3x3(tangeant, bitangent, normal);
                inputData.normalWS = normal;

                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS.xyz);
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
                ColorData color = _Colors[i.colorIndex];
                if (i.colorIndex >= _ColorCount)
                {
                    color.color = float4(1,0,1,1);
                    color.emissive = (sin(_Time.y * 10) * 0.5f + 0.5f) * 50;
                    color.metallic = 0.0f;
                    color.smoothness = 0.0f;
                }

                SurfaceData surfaceData = createSurfaceData(i, color);
                InputData inputData = createInputData(i);

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
                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment
                #pragma multi_compile_instancing
                #pragma multi_compile_local _ ANIMATED_VOXEL

#if ANIMATED_VOXEL
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                struct v2f
                {
                    float4 positionCS : SV_Position;
                };

                StructuredBuffer<float3> _Vertices;
                StructuredBuffer<int> _Quads;

                uint _InstanceStartIndex;
                float4x4 _ObjectToWorld;

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

                v2f ShadowPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
                {
                    v2f o;

                    uint faceID = _InstanceStartIndex + instanceID;

                    uint localVertexID = vertexID - (vertexID % 4);
                    uint nextVertexID = (localVertexID + 1) % 4;
                    uint previousVertexID = (localVertexID - 1 + 4) % 4;

                    localVertexID = _Quads[faceID * 5 + localVertexID];
                    nextVertexID = _Quads[faceID * 5 + nextVertexID];
                    previousVertexID = _Quads[faceID * 5 + previousVertexID];

                    float4 positionOS = float4(_Vertices[localVertexID], 1.0f);
                    float4 nextPositionOS = float4(_Vertices[nextVertexID], 1.0f);
                    float4 previousPositionOS = float4(_Vertices[previousVertexID], 1.0f);

                    float3 tangeantOS = normalize(nextPositionOS.xyz - positionOS.xyz);
                    float3 bitangeantOS = normalize(previousPositionOS.xyz - positionOS.xyz);

                    float3 normalOS = cross(tangeantOS, bitangeantOS);

                    float4 vertexPositionOS = float4(_Vertices[_Quads[faceID * 5 + vertexID]], 1.0f);
                    float3 positionWS = mul(_ObjectToWorld, vertexPositionOS).xyz;
                    float3 normalWS = mul(_ObjectToWorld, normalOS).xyz;

                    o.positionCS = GetShadowPositionHClip(positionWS, normalWS);

                    return o;
                }

                float4 ShadowPassFragment(v2f i) : SV_TARGET
                {
                    return 0;
                }
#else
                #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
#endif
            ENDHLSL
        }
    }

    Fallback Off
}
