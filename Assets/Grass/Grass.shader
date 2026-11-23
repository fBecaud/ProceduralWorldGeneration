Shader "Planet/Grass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.36, 0.62, 0.33, 1)
        _TipColor ("Tip Color", Color) = (0.62, 0.84, 0.45, 1)
        _RootDarkness ("Root Darkness", Range(0, 1)) = 0.6
        _TintVariation ("Tint Variation", Range(0, 0.5)) = 0.15
        _Bend ("Bend", Range(0, 1)) = 0.35
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.25
        _WindSpeed ("Wind Speed", Float) = 1.5
        _WindScale ("Wind Scale", Float) = 3
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.4, 0)
        _CameraFacing ("Camera Facing", Range(0, 1)) = 0.8
        _FacingDistance ("Facing Distance", Float) = 8
        _VerticalFacing ("Vertical Facing", Range(0, 1)) = 0.3
        _MinPixelWidth ("Min Pixel Width", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct GrassInstance
        {
            float4 positionHeight;
            float4 upAngle;
            float4 extra;
        };

        StructuredBuffer<GrassInstance> _Instances;
        int _InstanceOffset;
        float _FadeStart;
        float _FadeEnd;

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _TipColor;
        float _RootDarkness;
        float _TintVariation;
        float _Bend;
        float _WindStrength;
        float _WindSpeed;
        float _WindScale;
        float4 _WindDirection;
        float _CameraFacing;
        float _FacingDistance;
        float _VerticalFacing;
        float _MinPixelWidth;
        CBUFFER_END

        struct GrassVertex
        {
            float3 positionWS;
            float3 normalWS;
            float height;
            float tint;
        };

        GrassVertex BuildGrassVertex(float3 positionOS, float3 bendOS, float4 tangentOS, float2 uv, uint instanceID)
        {
            GrassInstance data = _Instances[instanceID + (uint)_InstanceOffset];
            float3 root = data.positionHeight.xyz;
            float height = data.positionHeight.w;
            float3 up = normalize(data.upAngle.xyz);
            float width = data.extra.x;

            float3 reference = lerp(float3(0, 1, 0), float3(1, 0, 0), step(0.99, abs(up.y)));
            float3 tangent = normalize(cross(up, reference));
            float3 bitangent = cross(up, tangent);
            float s;
            float c;
            sincos(data.upAngle.w, s, c);
            float3 side = tangent * c + bitangent * s;
            float3 forward = cross(side, up);

            float h = uv.y;
            float3 bend = side * bendOS.x + forward * bendOS.z;

            float3 wind = _WindDirection.xyz - up * dot(up, _WindDirection.xyz);
            wind = wind / max(length(wind), 0.0001);
            float phase = _Time.y * _WindSpeed + dot(root, float3(1.0, 0.7, 1.3)) * _WindScale;
            float sway = (sin(phase) * 0.7 + sin(phase * 2.3 + 1.7) * 0.3) * _WindStrength;

            float3 centerOS = positionOS - tangentOS.xyz * tangentOS.w;
            float3 bladeSide = normalize(side * tangentOS.x + forward * tangentOS.z);
            float3 bladeFace = cross(bladeSide, up);

            float3 toCamera = _WorldSpaceCameraPos - root;
            float cameraDistance = length(toCamera);
            float3 view = toCamera - up * dot(up, toCamera);
            float horizontal = length(view);
            view = view / max(horizontal, 0.0001);
            float3 cameraSide = cross(up, view);
            float flip = step(0.0, dot(bladeSide, cameraSide)) * 2.0 - 1.0;
            float edgeOn = 1.0 - abs(dot(bladeFace, view));
            float near = 1.0 - saturate(cameraDistance / max(_FacingDistance, 0.001));
            float grazing = saturate(horizontal / max(cameraDistance, 0.0001) * 4.0);
            float facing = saturate(edgeOn * near * grazing * _CameraFacing);
            bladeSide = normalize(lerp(bladeSide, cameraSide * flip, facing));

            float elevation = saturate(dot(up, toCamera) / max(cameraDistance, 0.0001));
            float3 bladeUp = normalize(up - view * grazing * elevation * _VerticalFacing);

            float pixel = 2.0 * cameraDistance / (abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y);
            float bladeWidth = max(width, pixel * _MinPixelWidth);

            float3 offset = (side * centerOS.x + forward * centerOS.z) * width + bladeUp * centerOS.y * height;
            offset += bladeSide * tangentOS.w * bladeWidth;
            offset += (bend * _Bend + wind * sway) * height * h * h;

            float fade = 1.0 - saturate((cameraDistance - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));

            GrassVertex result;
            result.positionWS = root + offset * fade;
            result.normalWS = up;
            result.height = h;
            result.tint = data.extra.y;
            return result;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 heightTint : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                GrassVertex grass = BuildGrassVertex(input.positionOS, input.normalOS, input.tangentOS, input.uv, input.instanceID);

                Varyings output;
                output.positionWS = grass.positionWS;
                output.positionCS = TransformWorldToHClip(grass.positionWS);
                output.normalWS = grass.normalWS;
                output.heightTint = float2(grass.height, grass.tint);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float h = saturate(input.heightTint.x);
                float3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, h);
                albedo *= lerp(1.0 - _TintVariation, 1.0 + _TintVariation, input.heightTint.y);
                albedo *= lerp(_RootDarkness, 1.0, h);

                float3 normalWS = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float ndl = saturate(dot(normalWS, light.direction));
                float3 lighting = light.color * ndl * light.shadowAttenuation * light.distanceAttenuation + SampleSH(normalWS);

                float3 color = MixFog(albedo * lighting, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            float4 DepthVert(DepthAttributes input) : SV_POSITION
            {
                GrassVertex grass = BuildGrassVertex(input.positionOS, input.normalOS, input.tangentOS, input.uv, input.instanceID);
                return TransformWorldToHClip(grass.positionWS);
            }

            half DepthFrag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }
    }
}
