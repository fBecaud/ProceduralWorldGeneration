using UnityEngine;

public class TreeShading
{
    private const float HoleHeight = 0.9f;
    private const float HoleWidth = 0.6f;

    private readonly TreeSettings _settings;
    private readonly Vector3 _crownCenter;
    private readonly float _crownRadius;
    private readonly float _footHeight;
    private readonly float _trunkRadius;
    private readonly bool _hasHole;
    private readonly Vector3 _holeCenter;
    private readonly Vector3 _holeAxis;
    private readonly float _holeRadius;
    private readonly float _holeHeight;
    private readonly float _holeWidth;

    public TreeShading(TreeSettings settings, TreeSkeleton skeleton, GrowthState state)
    {
        _settings = settings;
        _crownCenter = skeleton.CrownCenter * state.SizeScale;
        _crownRadius = Mathf.Max(skeleton.CrownRadius * state.SizeScale, 0.01f);

        Vector3 holeCenter;
        Vector3 holeAzimuth;
        _hasHole = TreeHole.Frame(skeleton, state, out holeCenter, out _holeAxis, out holeAzimuth, out _holeRadius);
        _holeCenter = holeCenter + holeAzimuth * _holeRadius;
        _holeHeight = Mathf.Max(_holeRadius * HoleHeight, 0.001f);
        _holeWidth = Mathf.Max(_holeRadius * HoleWidth, 0.001f);

        _footHeight = 0f;
        _trunkRadius = settings.TipRadius;

        if (skeleton.Nodes.Count > 0)
        {
            _footHeight = state.Positions[0].y;
            _trunkRadius = Mathf.Max(state.Radii[0], settings.TipRadius);
        }
    }

    public bool HasHole
    {
        get { return _hasHole; }
    }

    public float HoleRadius
    {
        get { return _holeRadius; }
    }

    public Color Bark(Vector3 position, float radius, bool isRoot)
    {
        float thickness = 1f;
        if (!isRoot)
        {
            thickness = Mathf.Sqrt(Mathf.Clamp01(radius / _trunkRadius));
        }

        Color color = Color.Lerp(_settings.BarkLight, _settings.BarkDark, thickness);

        float noise = Noise.Perlin(position * 3f) * _settings.ColorNoise;
        float ground = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01((position.y - _footHeight) / Mathf.Max(_settings.FlareHeight, 0.01f)));

        color = color * (1f + noise) * ground * Canopy(position);
        color.a = 1f;
        return color;
    }

    public float Hole(Vector3 position)
    {
        if (!_hasHole)
        {
            return 0f;
        }

        Vector3 delta = position - _holeCenter;
        float along = Vector3.Dot(delta, _holeAxis);
        Vector3 across = delta - _holeAxis * along;

        float distance = Mathf.Sqrt(along * along / (_holeHeight * _holeHeight) + across.sqrMagnitude / (_holeWidth * _holeWidth));
        if (distance >= 1f)
        {
            return 0f;
        }

        return 1f - Mathf.SmoothStep(0f, 1f, distance);
    }

    public bool NearHole(Vector3 from, Vector3 to)
    {
        if (!_hasHole)
        {
            return false;
        }

        Vector3 middle = (from + to) * 0.5f;
        float reach = Mathf.Max(_holeHeight, _holeWidth) + (to - from).magnitude;
        return (middle - _holeCenter).sqrMagnitude < reach * reach;
    }

    public Color Leaf(Vector3 position, float shade)
    {
        Color color = Color.Lerp(_settings.LeafColor, _settings.LeafColorVariation, shade);
        color = color * Canopy(position);
        color.a = 1f;
        return color;
    }

    public Vector3 LeafNormal(Vector3 position, Vector3 faceNormal)
    {
        Vector3 spherical = position - _crownCenter;
        if (spherical.sqrMagnitude < 0.0001f)
        {
            return faceNormal;
        }

        spherical.Normalize();

        Vector3 normal = Vector3.Lerp(faceNormal, spherical, 0.7f);
        if (normal.sqrMagnitude < 0.001f)
        {
            return spherical;
        }

        return normal.normalized;
    }

    private float Canopy(Vector3 position)
    {
        Vector3 offset = position - _crownCenter;
        float depth = Mathf.Clamp01(offset.magnitude / _crownRadius);
        float occlusion = Mathf.Lerp(1f - _settings.Occlusion, 1f, depth);
        float sun = 1f + _settings.SunGradient * Mathf.Clamp(offset.y / _crownRadius, -1f, 1f);
        return occlusion * sun;
    }
}
