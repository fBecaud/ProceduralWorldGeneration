Shader "Planet/Ocean"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow", Color) = (0.12, 0.66, 0.72, 0.55)
        _DeepColor ("Deep", Color) = (0.02, 0.2, 0.42, 0.95)
        _DepthRange ("Depth Range", Float) = 0.8
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _FresnelPower ("Fresnel Power", Float) = 5
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.6
        _SpecularStrength ("Specular Strength", Float) = 1.5

        [Header(Waves)]
        _WaveAmplitude ("Wave Amplitude", Float) = 0.04
        _WaveLength ("Wave Length", Float) = 2.5
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveSteepness ("Wave Steepness", Range(0, 1)) = 0.5
        _WaveGroups ("Wave Groups", Range(0, 1)) = 0.7
        _WaveGroupScale ("Wave Group Scale", Float) = 0.15
        _WaveWarp ("Wave Warp", Float) = 3
        _WaveWarpScale ("Wave Warp Scale", Float) = 0.35

        [Header(Ripples)]
        _DetailWaveStrength ("Ripple Strength", Range(0, 2)) = 0.6
        _DetailScale ("Ripple Scale", Float) = 4
        _DetailSpeed ("Ripple Speed", Float) = 0.15

        [Header(Foam)]
        _FoamColor ("Foam", Color) = (0.95, 0.98, 1, 1)
        _CrestFoam ("Crest Foam", Range(0, 1)) = 0.7
        _CrestFoamThreshold ("Crest Foam Threshold", Range(0, 1)) = 0.6
        _OpenSeaFoam ("Open Sea Foam", Range(0, 1)) = 0.15
        _BreakDepth ("Break Depth", Float) = 0.4
        _ShoreFoamDepth ("Shore Foam Depth", Float) = 0.12
        _ShoreFoamBands ("Shore Foam Bands", Float) = 40
        _ShoreFoamSpeed ("Shore Foam Speed", Float) = 1.2
        _FoamNoiseScale ("Foam Noise Scale", Float) = 6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PlanetNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor;
            float4 _DeepColor;
            float _DepthRange;
            float _Smoothness;
            float _FresnelPower;
            float _ReflectionStrength;
            float _SpecularStrength;
            float _WaveAmplitude;
            float _WaveLength;
            float _WaveSpeed;
            float _WaveSteepness;
            float _WaveGroups;
            float _WaveGroupScale;
            float _WaveWarp;
            float _WaveWarpScale;
            float _DetailWaveStrength;
            float _DetailScale;
            float _DetailSpeed;
            float4 _FoamColor;
            float _CrestFoam;
            float _CrestFoamThreshold;
            float _OpenSeaFoam;
            float _BreakDepth;
            float _ShoreFoamDepth;
            float _ShoreFoamBands;
            float _ShoreFoamSpeed;
            float _FoamNoiseScale;
            CBUFFER_END

            #define WAVE_COUNT 6
            #define NO_SHORE_DEPTH 1000.0

            static const float3 WaveDirections[WAVE_COUNT] =
            {
                float3(0.83, 0.17, 0.53),
                float3(-0.41, 0.62, 0.67),
                float3(0.12, -0.88, 0.46),
                float3(-0.73, -0.31, -0.61),
                float3(0.55, 0.69, -0.47),
                float3(-0.21, 0.35, -0.91)
            };
            static const float WaveScales[WAVE_COUNT] = { 1.0, 0.71, 0.53, 0.37, 0.29, 0.19 };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 shore : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float depth : TEXCOORD1;
                float waveHeight : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            float3 PlanetCenter()
            {
                return TransformObjectToWorld(float3(0, 0, 0));
            }

            void Waves(float3 relative, float3 normal, out float3 offset, out float height, out float3 gradient)
            {
                offset = float3(0, 0, 0);
                height = 0.0;
                gradient = float3(0, 0, 0);
                float total = 0.0;

                float warp = GradientNoise(relative * _WaveWarpScale + float3(0.0, _Time.y * 0.03, 0.0)) * _WaveWarp;

                for (int i = 0; i < WAVE_COUNT; i++)
                {
                    float3 direction = normalize(WaveDirections[i]);
                    float wavelength = _WaveLength * WaveScales[i];
                    float k = 6.2831853 / wavelength;
                    float group = GradientNoise(relative * _WaveGroupScale + float3(i * 7.13, i * 3.71, _Time.y * 0.05)) * 2.0 + 0.5;
                    float amplitude = _WaveAmplitude * WaveScales[i] * lerp(1.0, saturate(group) * 1.6, _WaveGroups);
                    float speed = sqrt(9.8 / k) * _WaveSpeed * 0.3;
                    float phase = k * (dot(relative, direction) - speed * _Time.y) + warp * (1.0 + i * 0.37);

                    float s;
                    float c;
                    sincos(phase, s, c);

                    float3 tangent = direction - normal * dot(direction, normal);
                    offset += normal * amplitude * s + tangent * (_WaveSteepness * amplitude * c);
                    gradient += direction * (amplitude * k * c);
                    height += amplitude * s;
                    total += _WaveAmplitude * WaveScales[i];
                }

                height /= max(total, 0.0001);
            }

            float3 NoiseGradient(float3 p)
            {
                const float e = 0.05;
                float n = GradientNoise(p);
                return float3(
                    GradientNoise(p + float3(e, 0, 0)) - n,
                    GradientNoise(p + float3(0, e, 0)) - n,
                    GradientNoise(p + float3(0, 0, e)) - n) / e;
            }

            float3 RippleGradient(float3 relative)
            {
                float time = _Time.y * _DetailSpeed;
                float frequency = _DetailScale;
                float amplitude = _WaveAmplitude * 0.15 * _DetailWaveStrength;

                float3 first = NoiseGradient(relative * frequency + float3(time, 0.0, time * 0.6));
                float3 second = NoiseGradient(relative * frequency * 2.37 + float3(-time * 0.8, time * 1.3, 0.0) + 41.3);
                return (first * frequency + second * frequency * 2.37 * 0.45) * amplitude;
            }

            float3 SurfaceNormal(float3 normal, float3 gradient)
            {
                float3 tangential = gradient - normal * dot(gradient, normal);
                return normalize(normal - tangential);
            }

            Varyings Vert(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 relative = positionWS - PlanetCenter();
                float3 normal = normalize(relative);

                float3 offset;
                float height;
                float3 gradient;
                Waves(relative, normal, offset, height, gradient);
                positionWS += offset;

                Varyings output;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.depth = lerp(NO_SHORE_DEPTH, input.shore.x, step(0.5, input.shore.y));
                output.waveHeight = height;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 relative = input.positionWS - PlanetCenter();
                float3 baseNormal = normalize(relative);

                float3 offset;
                float height;
                float3 gradient;
                Waves(relative, baseNormal, offset, height, gradient);

                float3 normalWS = SurfaceNormal(baseNormal, gradient + RippleGradient(relative));
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float depthFactor = saturate(input.depth / max(_DepthRange, 0.0001));
                float4 water = lerp(_ShallowColor, _DeepColor, depthFactor);

                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float shadow = light.shadowAttenuation * light.distanceAttenuation;
                float daylight = saturate(dot(baseNormal, light.direction) * 4.0);
                float ndl = saturate(dot(normalWS, light.direction));
                float3 ambient = SampleSH(normalWS);
                float3 diffuse = water.rgb * (light.color * ndl * shadow + ambient);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewWS)), _FresnelPower);
                float3 reflection = GlossyEnvironmentReflection(reflect(-viewWS, normalWS), 1.0 - _Smoothness, 1.0);

                float3 halfDirection = normalize(light.direction + viewWS);
                float specularPower = exp2(10.0 * _Smoothness + 1.0);
                float specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) * _SpecularStrength * shadow * daylight;

                float3 color = diffuse + reflection * fresnel * _ReflectionStrength + light.color * specular;
                float alpha = lerp(water.a, 1.0, fresnel * 0.5);

                float foamNoise = saturate(Fbm(relative * _FoamNoiseScale + float3(0.0, _Time.y * 0.3, 0.0)) + 0.5);
                float shallow = 1.0 - saturate(input.depth / max(_BreakDepth, 0.0001));

                float crest = smoothstep(_CrestFoamThreshold, 1.0, input.waveHeight * 0.5 + 0.5);
                crest *= smoothstep(0.35, 0.65, foamNoise) * _CrestFoam * lerp(_OpenSeaFoam, 1.0, shallow);

                float shore = 1.0 - saturate(input.depth / max(_ShoreFoamDepth, 0.0001));
                float bands = sin(input.depth * _ShoreFoamBands + _Time.y * _ShoreFoamSpeed) * 0.5 + 0.5;
                float shoreFoam = shore * smoothstep(0.5, 0.85, bands * foamNoise + shore * 0.6);

                float foam = saturate(max(crest, shoreFoam));
                float3 foamLight = light.color * saturate(dot(baseNormal, light.direction)) * shadow + SampleSH(baseNormal);
                color = lerp(color, _FoamColor.rgb * foamLight, foam);
                alpha = lerp(alpha, _FoamColor.a, foam);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
