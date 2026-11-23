#ifndef PLANET_NOISE_INCLUDED
#define PLANET_NOISE_INCLUDED

float3 Hash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx) * 2.0 - 1.0;
}

float GradientNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float n000 = dot(Hash33(i + float3(0, 0, 0)), f - float3(0, 0, 0));
    float n100 = dot(Hash33(i + float3(1, 0, 0)), f - float3(1, 0, 0));
    float n010 = dot(Hash33(i + float3(0, 1, 0)), f - float3(0, 1, 0));
    float n110 = dot(Hash33(i + float3(1, 1, 0)), f - float3(1, 1, 0));
    float n001 = dot(Hash33(i + float3(0, 0, 1)), f - float3(0, 0, 1));
    float n101 = dot(Hash33(i + float3(1, 0, 1)), f - float3(1, 0, 1));
    float n011 = dot(Hash33(i + float3(0, 1, 1)), f - float3(0, 1, 1));
    float n111 = dot(Hash33(i + float3(1, 1, 1)), f - float3(1, 1, 1));

    float x00 = lerp(n000, n100, u.x);
    float x10 = lerp(n010, n110, u.x);
    float x01 = lerp(n001, n101, u.x);
    float x11 = lerp(n011, n111, u.x);
    return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
}

float Fbm(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;

    for (int octave = 0; octave < 4; octave++)
    {
        value += GradientNoise(p) * amplitude;
        p = p * 2.03 + 17.1;
        amplitude *= 0.5;
    }

    return value;
}

#endif
