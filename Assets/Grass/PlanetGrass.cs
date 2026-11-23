using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(100)]
public class PlanetGrass : MonoBehaviour
{
    private const string ShaderName = "Planet/Grass";

    [SerializeField] private Planet _planet;
    [SerializeField] private Material _material;
    [SerializeField] private Camera _camera;
    [SerializeField] private int _seed = 0;

    [Header("Zone")]
    [SerializeField] private float _minAltitude = 1f;
    [SerializeField] private float _maxAltitude = 1.055f;
    [SerializeField] private float _poleLimit = 0.95f;
    [SerializeField, Range(0f, 0.5f)] private float _edgeFade = 0.25f;

    [Header("Density")]
    [SerializeField, Range(10f, 2000f)] private float _density = 400f;
    [SerializeField, Range(1000, 1000000)] private int _maxClumps = 300000;
    [SerializeField, Range(0.5f, 30f)] private float _patchScale = 6f;
    [SerializeField, Range(0f, 1f)] private float _patchStrength = 0.6f;

    [Header("Blades")]
    [SerializeField] private Vector2 _heightRange = new Vector2(0.05f, 0.1f);
    [SerializeField] private Vector2 _widthRange = new Vector2(0.008f, 0.014f);
    [SerializeField, Range(1, 6)] private int _bladesPerClump = 3;
    [SerializeField, Range(1, 5)] private int _bladeSegments = 3;
    [SerializeField, Range(0f, 0.2f)] private float _sink = 0.02f;

    [Header("Culling")]
    [SerializeField, Range(2, 32)] private int _chunksPerFace = 10;
    [SerializeField] private float _drawDistance = 40f;
    [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.6f;
    [SerializeField] private bool _receiveShadows = true;

    private class Chunk
    {
        public int Offset;
        public int Count;
        public Bounds Bounds;
        public Vector3 Normal;
        public float Extent;
        public MaterialPropertyBlock Properties;
    }

    private readonly List<Chunk> _chunks = new List<Chunk>();
    private readonly Plane[] _planes = new Plane[6];
    private GraphicsBuffer _buffer;
    private Mesh _mesh;
    private Material _fallbackMaterial;
    private bool _dirty;

    public int Count
    {
        get
        {
            if (_buffer == null)
            {
                return 0;
            }

            return _buffer.count;
        }
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        _dirty = true;
    }

    private void OnValidate()
    {
        _dirty = true;
    }

    private void OnDisable()
    {
        Release();
    }

    private void OnEnable()
    {
        _dirty = true;
    }

    private void LateUpdate()
    {
        if (_dirty)
        {
            Generate();
        }

        Draw();
    }

    private void Generate()
    {
        _dirty = false;
        Release();

        if (_planet == null)
        {
            _planet = GetComponent<Planet>();
        }

        if (_planet == null)
        {
            Debug.LogWarning("PlanetGrass needs a planet.", this);
            return;
        }

        Material material = GetMaterial();
        if (material == null)
        {
            Debug.LogWarning("PlanetGrass could not find the " + ShaderName + " shader.", this);
            return;
        }

        int faces = 6 * _chunksPerFace * _chunksPerFace;
        List<GrassInstance>[] cells = new List<GrassInstance>[faces];
        Scatter(cells);

        List<GrassInstance> all = new List<GrassInstance>();
        for (int c = 0; c < cells.Length; c++)
        {
            if (cells[c] == null || cells[c].Count == 0)
            {
                continue;
            }

            Chunk chunk = new Chunk();
            chunk.Offset = all.Count;
            chunk.Count = cells[c].Count;
            chunk.Bounds = ChunkBounds(cells[c]);
            chunk.Normal = (chunk.Bounds.center - _planet.transform.position).normalized;
            chunk.Extent = chunk.Bounds.extents.magnitude;
            all.AddRange(cells[c]);
            _chunks.Add(chunk);
        }

        if (all.Count == 0)
        {
            _chunks.Clear();
            return;
        }

        _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, all.Count, GrassInstance.Stride);
        _buffer.SetData(all);
        _mesh = GrassMesh.Create(_bladesPerClump, _bladeSegments, _seed);

        for (int i = 0; i < _chunks.Count; i++)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetBuffer("_Instances", _buffer);
            properties.SetInteger("_InstanceOffset", _chunks[i].Offset);
            properties.SetFloat("_FadeStart", _drawDistance * _fadeStart);
            properties.SetFloat("_FadeEnd", _drawDistance);
            _chunks[i].Properties = properties;
        }
    }

    private void Scatter(List<GrassInstance>[] cells)
    {
        System.Random random = new System.Random(_seed);
        Transform planetTransform = _planet.transform;
        float radius = _planet.Radius;
        float low = radius * _minAltitude;
        float high = radius * _maxAltitude;
        float pole = radius * _poleLimit;

        Vector3 patchOffset = new Vector3(Value(random), Value(random), Value(random)) * 1000f;
        int candidates = Mathf.RoundToInt(4f * Mathf.PI * radius * radius * _density);
        int kept = 0;

        for (int i = 0; i < candidates && kept < _maxClumps; i++)
        {
            Vector3 direction = Direction(Value(random), Value(random));
            float keepRoll = Value(random);
            float heightRoll = Value(random);
            float widthRoll = Value(random);
            float angleRoll = Value(random);
            float tintRoll = Value(random);

            float distance = radius + _planet.Relief(direction);
            if (distance <= low || distance > high)
            {
                continue;
            }

            if (Mathf.Abs(direction.y) * distance > pole)
            {
                continue;
            }

            float chance = EdgeWeight((distance - low) / (high - low)) * PatchWeight(direction, patchOffset);
            if (keepRoll > chance)
            {
                continue;
            }

            Vector3 world = planetTransform.TransformPoint(direction * (distance - _sink));
            Vector3 up = planetTransform.TransformDirection(direction).normalized;
            float height = Mathf.Lerp(_heightRange.x, _heightRange.y, heightRoll);
            float width = Mathf.Lerp(_widthRange.x, _widthRange.y, widthRoll);

            GrassInstance instance = new GrassInstance();
            instance.PositionHeight = new Vector4(world.x, world.y, world.z, height);
            instance.UpAngle = new Vector4(up.x, up.y, up.z, angleRoll * Mathf.PI * 2f);
            instance.Extra = new Vector4(width, tintRoll, 0f, 0f);

            int cell = Cell(direction);
            if (cells[cell] == null)
            {
                cells[cell] = new List<GrassInstance>();
            }

            cells[cell].Add(instance);
            kept++;
        }
    }

    private void Draw()
    {
        if (_buffer == null || _mesh == null)
        {
            return;
        }

        Camera view = _camera;
        if (view == null)
        {
            view = Camera.main;
        }

        if (view == null)
        {
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(view, _planes);
        Vector3 eye = view.transform.position;
        float drawSqr = _drawDistance * _drawDistance;
        Material material = GetMaterial();

        for (int i = 0; i < _chunks.Count; i++)
        {
            Chunk chunk = _chunks[i];

            if (chunk.Bounds.SqrDistance(eye) > drawSqr)
            {
                continue;
            }

            if (Vector3.Dot(eye - chunk.Bounds.center, chunk.Normal) < -chunk.Extent)
            {
                continue;
            }

            if (!GeometryUtility.TestPlanesAABB(_planes, chunk.Bounds))
            {
                continue;
            }

            RenderParams parameters = new RenderParams(material);
            parameters.worldBounds = chunk.Bounds;
            parameters.matProps = chunk.Properties;
            parameters.shadowCastingMode = ShadowCastingMode.Off;
            parameters.receiveShadows = _receiveShadows;
            parameters.layer = gameObject.layer;

            Graphics.RenderMeshPrimitives(parameters, _mesh, 0, chunk.Count);
        }
    }

    private Bounds ChunkBounds(List<GrassInstance> instances)
    {
        Vector4 first = instances[0].PositionHeight;
        Bounds bounds = new Bounds(new Vector3(first.x, first.y, first.z), Vector3.zero);

        for (int i = 1; i < instances.Count; i++)
        {
            Vector4 p = instances[i].PositionHeight;
            bounds.Encapsulate(new Vector3(p.x, p.y, p.z));
        }

        bounds.Expand(_heightRange.y * 3f);
        return bounds;
    }

    private float EdgeWeight(float band)
    {
        if (_edgeFade <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(Mathf.Min(band, 1f - band) / _edgeFade);
    }

    private float PatchWeight(Vector3 direction, Vector3 offset)
    {
        if (_patchStrength <= 0f)
        {
            return 1f;
        }

        float noise = Noise.Perlin(direction * _patchScale + offset) * 0.5f + 0.5f;
        return Mathf.Lerp(1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.65f, noise)), _patchStrength);
    }

    private int Cell(Vector3 direction)
    {
        float ax = Mathf.Abs(direction.x);
        float ay = Mathf.Abs(direction.y);
        float az = Mathf.Abs(direction.z);
        int face;
        float u;
        float v;

        if (ax >= ay && ax >= az)
        {
            face = Side(direction.x, 0);
            u = direction.y / ax;
            v = direction.z / ax;
        }
        else if (ay >= az)
        {
            face = Side(direction.y, 2);
            u = direction.x / ay;
            v = direction.z / ay;
        }
        else
        {
            face = Side(direction.z, 4);
            u = direction.x / az;
            v = direction.y / az;
        }

        int n = _chunksPerFace;
        int cu = Mathf.Clamp((int)((u * 0.5f + 0.5f) * n), 0, n - 1);
        int cv = Mathf.Clamp((int)((v * 0.5f + 0.5f) * n), 0, n - 1);
        return (face * n + cu) * n + cv;
    }

    private static int Side(float component, int positiveFace)
    {
        if (component >= 0f)
        {
            return positiveFace;
        }

        return positiveFace + 1;
    }

    private static Vector3 Direction(float a, float b)
    {
        float y = a * 2f - 1f;
        float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float phi = b * Mathf.PI * 2f;
        return new Vector3(ring * Mathf.Cos(phi), y, ring * Mathf.Sin(phi));
    }

    private static float Value(System.Random random)
    {
        return (float)random.NextDouble();
    }

    private Material GetMaterial()
    {
        if (_material != null)
        {
            return _material;
        }

        if (_fallbackMaterial == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                return null;
            }

            _fallbackMaterial = new Material(shader);
            _fallbackMaterial.name = "Grass";
            _fallbackMaterial.hideFlags = HideFlags.DontSave;
        }

        return _fallbackMaterial;
    }

    private void Release()
    {
        if (_buffer != null)
        {
            _buffer.Release();
            _buffer = null;
        }

        if (_mesh != null)
        {
            Destroy(_mesh);
            _mesh = null;
        }

        _chunks.Clear();
    }

    private void OnDestroy()
    {
        Release();

        if (_fallbackMaterial != null)
        {
            Destroy(_fallbackMaterial);
        }
    }
}
