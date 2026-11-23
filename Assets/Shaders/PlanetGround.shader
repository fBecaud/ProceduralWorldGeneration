Shader "Planet/Ground"
{
    Properties
    {
        _Radius ("Radius", Float) = 10

        [Header(Bands)]
        _SandLevel ("Sand Level", Float) = 1
        _GrassLevel ("Grass Level", Float) = 1.055
        _RockLevel ("Rock Level", Float) = 1.09
        _PoleLimit ("Pole Limit", Float) = 0.95
        _BlendWidth ("Blend Width", Float) = 0.03

        [Header(Colors)]
        _SandColor ("Sand", Color) = (0.976, 0.976, 0.733, 1)
        _GrassColor ("Grass", Color) = (0.502, 0.788, 0.502, 1)
        _RockColor ("Rock", Color) = (0.682, 0.761, 0.890, 1)
        _SnowColor ("Snow", Color) = (0.898, 0.957, 0.976, 1)
        _PoleColor ("Pole", Color) = (1, 1, 1, 1)

        [Header(Noise)]
        _BorderNoise ("Border Noise", Float) = 0.12
        _BorderNoiseScale ("Border Noise Scale", Float) = 1.5
        _ColorNoise ("Color Noise", Range(0, 0.5)) = 0.12
        _ColorNoiseScale ("Color Noise Scale", Float) = 0.8
        _DetailNoise ("Detail Noise", Range(0, 0.5)) = 0.06
        _DetailNoiseScale ("Detail Noise Scale", Float) = 8

        [Header(Grass Zone)]
        _GrassDarkColor ("Grass Dark", Color) = (0.24, 0.47, 0.22, 1)
        _GrassDryColor ("Grass Dry", Color) = (0.74, 0.77, 0.4, 1)
        _GrassNoise ("Grass Noise", Range(0, 1)) = 1
        _GrassNoiseScale ("Grass Noise Scale", Float) = 2.5
        _GrassDryScale ("Grass Dry Scale", Float) = 0.9
        _GrassContrast ("Grass Contrast", Range(1, 4)) = 2.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float _Radius;
        float _SandLevel;
        float _GrassLevel;
        float _RockLevel;
        float _PoleLimit;
        float _BlendWidth;
        float4 _SandColor;
        float4 _GrassColor;
        float4 _RockColor;
        float4 _SnowColor;
        float4 _PoleColor;
        float _BorderNoise;
        float _BorderNoiseScale;
        float _ColorNoise;
        float _ColorNoiseScale;
        float _DetailNoise;
        float _DetailNoiseScale;
        float4 _GrassDarkColor;
        float4 _GrassDryColor;
        float _GrassNoise;
        float _GrassNoiseScale;
        float _GrassDryScale;
        float _GrassContrast;
        CBUFFER_END

        #include "PlanetNoise.hlsl"

        float3 GrassZoneColor(float3 positionOS)
        {
            float dark = saturate(Fbm(positionOS * _GrassNoiseScale + 53.1) * _GrassContrast + 0.5);
            float dry = saturate(Fbm(positionOS * _GrassDryScale + 91.7) * _GrassContrast + 0.35);
            float tufts = GradientNoise(positionOS * _GrassNoiseScale * 5.0 + 12.9);

            float3 color = lerp(_GrassColor.rgb, _GrassDarkColor.rgb, smoothstep(0.35, 0.8, dark));
            color = lerp(color, _GrassDryColor.rgb, smoothstep(0.55, 0.9, dry));
            color *= 1.0 + tufts * 0.35;

            return lerp(_GrassColor.rgb, color, _GrassNoise);
        }

        float3 GroundColor(float3 positionOS)
        {
            float border = Fbm(positionOS * _BorderNoiseScale) * _BorderNoise;
            float distanceToCenter = length(positionOS) + border;
            float width = max(_BlendWidth, 0.0001);

            float3 color = _SandColor.rgb;
            color = lerp(color, GrassZoneColor(positionOS), smoothstep(-width, width, distanceToCenter - _Radius * _SandLevel));
            color = lerp(color, _RockColor.rgb, smoothstep(-width, width, distanceToCenter - _Radius * _GrassLevel));
            color = lerp(color, _SnowColor.rgb, smoothstep(-width, width, distanceToCenter - _Radius * _RockLevel));

            float pole = abs(positionOS.y) + border;
            color = lerp(color, _PoleColor.rgb, smoothstep(-width, width, pole - _Radius * _PoleLimit));

            float tint = 1.0 + Fbm(positionOS * _ColorNoiseScale + 31.7) * 2.0 * _ColorNoise;
            float detail = 1.0 + GradientNoise(positionOS * _DetailNoiseScale + 7.3) * 2.0 * _DetailNoise;
            return color * tint * detail;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 albedo = GroundColor(input.positionOS);
                float3 normalWS = normalize(input.normalWS);

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float ndl = saturate(dot(normalWS, light.direction));
                float3 direct = light.color * ndl * light.shadowAttenuation * light.distanceAttenuation;
                float3 ambient = SampleSH(normalWS);

                #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                direct *= occlusion.directAmbientOcclusion;
                ambient *= occlusion.indirectAmbientOcclusion;
                #endif

                float3 color = MixFog(albedo * (direct + ambient), input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 ShadowVert(ShadowAttributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half DepthFrag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings input) : SV_Target
            {
                #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(input.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), 0.0);
                #else
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
