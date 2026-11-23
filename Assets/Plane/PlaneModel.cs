using UnityEngine;

public static class PlaneModel
{
    private static readonly Color Body = new Color(1f, 0.93f, 0.82f);
    private static readonly Color Accent = new Color(0.95f, 0.36f, 0.34f);
    private static readonly Color Glass = new Color(0.62f, 0.86f, 1f);
    private static readonly Color Dark = new Color(0.27f, 0.27f, 0.32f);

    private static Texture2D _softDot;

    public static Transform Build(Transform parent, float size, out Transform propeller, out Transform tail)
    {
        Material template = Resources.Load<Material>("PlaneLit");
        Material body = Tint(template, Body);
        Material accent = Tint(template, Accent);
        Material glass = Tint(template, Glass);
        Material dark = Tint(template, Dark);

        Transform model = new GameObject("Model").transform;
        model.SetParent(parent, false);
        model.localScale = Vector3.one * size;

        Part(model, PrimitiveType.Capsule, new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.45f, 0.6f, 0.45f), body);
        Part(model, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.55f), Vector3.zero, new Vector3(0.36f, 0.36f, 0.3f), accent);
        Part(model, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0.08f), Vector3.zero, new Vector3(0.28f, 0.22f, 0.34f), glass);

        Part(model, PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.05f), Vector3.zero, new Vector3(1.5f, 0.06f, 0.34f), accent);
        Part(model, PrimitiveType.Sphere, new Vector3(0.75f, -0.06f, 0.05f), Vector3.zero, new Vector3(0.12f, 0.06f, 0.34f), accent);
        Part(model, PrimitiveType.Sphere, new Vector3(-0.75f, -0.06f, 0.05f), Vector3.zero, new Vector3(0.12f, 0.06f, 0.34f), accent);

        Part(model, PrimitiveType.Cube, new Vector3(0f, 0.05f, -0.52f), Vector3.zero, new Vector3(0.6f, 0.04f, 0.16f), accent);
        Part(model, PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.52f), new Vector3(-15f, 0f, 0f), new Vector3(0.05f, 0.28f, 0.18f), accent);

        Part(model, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.7f), Vector3.zero, Vector3.one * 0.1f, dark);
        propeller = new GameObject("Propeller").transform;
        propeller.SetParent(model, false);
        propeller.localPosition = new Vector3(0f, 0f, 0.73f);
        Part(propeller, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.55f, 0.06f, 0.02f), dark);
        Part(propeller, PrimitiveType.Cube, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.55f, 0.06f, 0.02f), dark);

        tail = new GameObject("Tail").transform;
        tail.SetParent(model, false);
        tail.localPosition = new Vector3(0f, 0f, -0.62f);

        return model;
    }

    public static ParticleSystem BuildTrail(Transform parent, float size)
    {
        GameObject host = new GameObject("Trail");
        host.transform.SetParent(parent, false);

        ParticleSystem system = host.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.35f, size * 0.55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 18f / size;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.05f;

        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.88f, 0.94f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.08f), new GradientAlphaKey(0.45f, 0.5f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = fade;

        AnimationCurve growth = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.6f));
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, growth);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = size * 0.15f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;

        Material material = new Material(Resources.Load<Material>("PlaneTrail"));
        material.SetTexture("_BaseMap", SoftDot());

        ParticleSystemRenderer renderer = host.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        system.Play();
        return system;
    }

    private static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());

        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }

    private static Material Tint(Material template, Color color)
    {
        Material material = new Material(template);
        material.SetColor("_BaseColor", color);
        return material;
    }

    private static Texture2D SoftDot()
    {
        if (_softDot != null)
        {
            return _softDot;
        }

        const int size = 64;
        _softDot = new Texture2D(size, size, TextureFormat.RGBA32, true);
        _softDot.name = "Soft Dot";
        _softDot.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.6f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _softDot.SetPixels(pixels);
        _softDot.Apply(true);
        return _softDot;
    }
}
