using UnityEngine;

public static class BarkTexture
{
    private const int Size = 512;
    private const float Slice = 0.37f;

    private static Texture2D _texture;

    public static Texture2D Get()
    {
        if (_texture == null)
        {
            _texture = Create(Size);
            _texture.hideFlags = HideFlags.DontSave;
        }

        return _texture;
    }

    public static Texture2D Create(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        texture.name = "Bark";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        texture.mipMapBias = -0.5f;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float value = Bark((float)x / size, (float)y / size);
                pixels[y * size + x] = new Color(value, value, value, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true);
        return texture;
    }

    private static float Bark(float u, float v)
    {
        float furrows = Tileable(u, v, 8f, 2f, 3);
        float ridges = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(furrows) * 3f), 0.7f);
        float detail = Tileable(u, v, 32f, 32f, 3);
        float grain = Tileable(u, v, 64f, 64f, 1);

        return Mathf.Clamp01(0.5f + 0.45f * ridges + 0.15f * detail + 0.12f * grain);
    }

    private static float Tileable(float u, float v, float frequencyU, float frequencyV, int octaves)
    {
        float a = Fbm(u * frequencyU, v * frequencyV, octaves);
        float b = Fbm((u - 1f) * frequencyU, v * frequencyV, octaves);
        float c = Fbm(u * frequencyU, (v - 1f) * frequencyV, octaves);
        float d = Fbm((u - 1f) * frequencyU, (v - 1f) * frequencyV, octaves);

        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static float Fbm(float x, float y, int octaves)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            value += Noise.Perlin(new Vector3(x * frequency, y * frequency, Slice)) * amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return value;
    }
}
