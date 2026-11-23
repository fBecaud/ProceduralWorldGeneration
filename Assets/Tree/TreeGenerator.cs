using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TreeGenerator : MonoBehaviour
{
    [SerializeField] private Planet _planet;
    [SerializeField] private int _seed = 0;
    [SerializeField] private TreeSettings _settings = new TreeSettings();

    [Header("Materials")]
    [SerializeField] private Material _barkMaterial;
    [SerializeField] private Material _leafMaterial;

    [Header("Growth")]
    [SerializeField, Range(0f, 1f)] private float _growth = 1f;
    [SerializeField] private bool _animateOnPlay = true;
    [SerializeField] private AnimationCurve _growthCurve = AnimationCurve.EaseInOut(0f, 0f, 8f, 1f);
    [SerializeField] private float _growthDelay = 0f;
    [SerializeField] private float _rebuildsPerSecond = 15f;

    [Header("Debug")]
    [SerializeField] private bool _buildMesh = true;
    [SerializeField] private bool _drawSkeleton = true;
    [SerializeField] private bool _drawAttractors = false;

    private static readonly GrowthState _sharedState = new GrowthState();
    private static readonly MeshData _sharedData = new MeshData();
    private static Material _shadowMaterial;

    private Mesh _mesh;
    private Mesh _leafMesh;
    private Mesh _shadowMesh;
    private GameObject _leafObject;
    private GameObject _shadowObject;
    private Material _fallbackBark;
    private Material _fallbackLeaf;
    private TreeSkeleton _skeleton;
    private TreeSettings _effective;

    private bool _growing;
    private bool _queued;
    private float _growthStartTime;
    private float _lastRebuildTime;
    private float _builtGrowth = -1f;

    public bool IsGrowing
    {
        get { return _growing; }
    }

    public float Growth
    {
        get { return _growth; }
    }

    public void Setup(Planet planet, TreeSettings settings, int seed, Material barkMaterial, Material leafMaterial, AnimationCurve growthCurve, float growthDelay)
    {
        _planet = planet;
        _settings = settings;
        _seed = seed;
        _barkMaterial = barkMaterial;
        _leafMaterial = leafMaterial;
        _animateOnPlay = true;
        _growthCurve = growthCurve;
        _growthDelay = growthDelay;
    }

    public void Grow(AnimationCurve curve, float delay)
    {
        _growthCurve = curve;
        _growthDelay = delay;
        _growthStartTime = Time.time;
        _growth = 0f;
        _growing = true;

        if (_skeleton == null)
        {
            Generate();
        }
        else
        {
            RebuildMesh();
        }

        enabled = true;
    }

    public void ScheduledRebuild()
    {
        _queued = false;

        if (_skeleton == null)
        {
            return;
        }

        RebuildMesh();
    }

    private void OnEnable()
    {
        if (_mesh != null && _skeleton != null)
        {
            return;
        }

        if (Application.isPlaying && _animateOnPlay)
        {
            Grow(_growthCurve, _growthDelay);
        }
        else
        {
            Generate();
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            Cleanup();
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += DelayedGenerate;
#endif
    }

    private void DelayedGenerate()
    {
        if (this != null)
        {
            Generate();
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            PlayUpdate();
        }
        else
        {
            EditorUpdate();
        }
    }

    private void PlayUpdate()
    {
        if (_growing)
        {
            float elapsed = Mathf.Max(Time.time - _growthStartTime - _growthDelay, 0f);
            float end = CurveEnd(_growthCurve);

            if (elapsed >= end)
            {
                elapsed = end;
                _growing = false;
            }

            _growth = Mathf.Clamp01(_growthCurve.Evaluate(elapsed));
        }

        if (_queued)
        {
            return;
        }

        if (_growth != _builtGrowth)
        {
            float interval = 1f / Mathf.Max(_rebuildsPerSecond, 1f);
            if (!_growing || Time.time - _lastRebuildTime >= interval)
            {
                _queued = true;
                TreeScheduler.Request(this);
            }

            return;
        }

        if (!_growing)
        {
            Finish();
        }
    }

    private void EditorUpdate()
    {
        if (transform.hasChanged)
        {
            Generate();
            return;
        }

        if (_growth != _builtGrowth)
        {
            RebuildMesh();
        }
    }

    private static float CurveEnd(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return 0f;
        }

        return curve[curve.length - 1].time;
    }

    private void Finish()
    {
        _skeleton = null;
        enabled = false;

        if (_mesh != null)
        {
            _mesh.UploadMeshData(true);
        }

        if (_leafMesh != null)
        {
            _leafMesh.UploadMeshData(true);
        }

        if (_shadowMesh != null)
        {
            _shadowMesh.UploadMeshData(true);
        }
    }

    private void Generate()
    {
        Vector3 groundNormal = Vector3.up;

        if (_planet != null)
        {
            groundNormal = SnapToPlanet();
        }

        transform.hasChanged = false;

        float detail = Mathf.Clamp(transform.lossyScale.y / _settings.FullDetailScale, _settings.MinDetail, 1f);
        _effective = _settings.WithDetail(detail);
        _skeleton = TreeSkeletonBuilder.Build(_effective, groundNormal, GroundHeight, _seed);

        EnsureMeshes();
        EnsureMaterials();
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        _lastRebuildTime = Time.time;
        _builtGrowth = _growth;

        EnsureMeshes();

        if (!_buildMesh)
        {
            _mesh.Clear();
            _leafMesh.Clear();
            if (_shadowMesh != null)
            {
                _shadowMesh.Clear();
            }

            return;
        }

        TreeGrowth.Evaluate(_skeleton, _effective, _growth, _sharedState);
        TreeMeshBuilder.Build(_sharedData, _skeleton, _sharedState, _effective);
        _sharedData.Apply(_mesh, _leafMesh, _shadowMesh);
    }

    private void EnsureMeshes()
    {
        if (_mesh == null || !_mesh.isReadable)
        {
            DestroyAsset(_mesh);
            _mesh = CreateMesh("Tree");
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        if (_leafObject == null)
        {
            _leafObject = CreateChild("Leaves", ShadowCastingMode.Off);
        }

        if (_leafMesh == null || !_leafMesh.isReadable)
        {
            DestroyAsset(_leafMesh);
            _leafMesh = CreateMesh("Tree Leaves");
            _leafObject.GetComponent<MeshFilter>().sharedMesh = _leafMesh;
        }

        bool shadows = _effective != null && _effective.CastShadows;
        if (!shadows)
        {
            DestroyShadowObject();
            return;
        }

        if (_shadowObject == null)
        {
            _shadowObject = CreateChild("Shadow", ShadowCastingMode.ShadowsOnly);
            _shadowObject.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        if (_shadowMesh == null || !_shadowMesh.isReadable)
        {
            DestroyAsset(_shadowMesh);
            _shadowMesh = CreateMesh("Tree Shadow");
            _shadowObject.GetComponent<MeshFilter>().sharedMesh = _shadowMesh;
        }
    }

    private GameObject CreateChild(string name, ShadowCastingMode shadowMode)
    {
        GameObject child = new GameObject(name);
        child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(transform, false);
        child.AddComponent<MeshFilter>();

        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = shadowMode;
        return child;
    }

    private static Mesh CreateMesh(string name)
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.hideFlags = HideFlags.DontSave;
        mesh.MarkDynamic();
        return mesh;
    }

    private void Cleanup()
    {
        DestroyLeafObject();
        DestroyShadowObject();
        DestroyAsset(_mesh);
        DestroyAsset(_fallbackBark);
        DestroyAsset(_fallbackLeaf);
        _mesh = null;
        _fallbackBark = null;
        _fallbackLeaf = null;
    }

    private void DestroyLeafObject()
    {
        DestroyAsset(_leafMesh);
        _leafMesh = null;

        if (_leafObject != null)
        {
            DestroyAsset(_leafObject);
            _leafObject = null;
        }
    }

    private void DestroyShadowObject()
    {
        DestroyAsset(_shadowMesh);
        _shadowMesh = null;

        if (_shadowObject != null)
        {
            DestroyAsset(_shadowObject);
            _shadowObject = null;
        }
    }

    private static void DestroyAsset(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private Vector3 SnapToPlanet()
    {
        Vector3 sky = _planet.Up(transform.position);
        transform.position = _planet.SurfacePoint(transform.position);
        transform.rotation = Quaternion.FromToRotation(transform.up, sky) * transform.rotation;

        Vector3 worldNormal = _planet.SurfaceNormal(transform.position);
        return transform.InverseTransformDirection(worldNormal);
    }

    private float GroundHeight(Vector3 localPosition)
    {
        if (_planet == null)
        {
            return 0f;
        }

        Vector3 world = transform.TransformPoint(localPosition);
        Vector3 surface = _planet.SurfacePoint(world);
        return transform.InverseTransformPoint(surface).y;
    }

    private void EnsureMaterials()
    {
        Material bark = _barkMaterial;
        if (bark == null)
        {
            if (_fallbackBark == null)
            {
                _fallbackBark = CreateFallbackMaterial("Tree Bark");

                if (_fallbackBark.HasProperty("_BaseMap"))
                {
                    _fallbackBark.SetTexture("_BaseMap", BarkTexture.Get());
                    _fallbackBark.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
                }
            }

            bark = _fallbackBark;
        }

        Material leaf = _leafMaterial;
        if (leaf == null)
        {
            if (_fallbackLeaf == null)
            {
                _fallbackLeaf = CreateFallbackMaterial("Tree Leaves");
            }

            leaf = _fallbackLeaf;
        }

        GetComponent<MeshRenderer>().sharedMaterial = bark;

        if (_leafObject != null)
        {
            _leafObject.GetComponent<MeshRenderer>().sharedMaterial = leaf;
        }

        if (_shadowObject != null)
        {
            Material shadow = ShadowMaterial();
            _shadowObject.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { shadow, shadow };
        }
    }

    private static Material ShadowMaterial()
    {
        if (_shadowMaterial == null)
        {
            _shadowMaterial = new Material(Resources.Load<Material>("TreeShadow"));
            _shadowMaterial.name = "Tree Shadow";
            _shadowMaterial.hideFlags = HideFlags.DontSave;
        }

        return _shadowMaterial;
    }

    private static Material CreateFallbackMaterial(string name)
    {
        Material material = new Material(Resources.Load<Material>("TreeParticles"));
        material.name = name;
        material.hideFlags = HideFlags.DontSave;

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0f);
        }

        return material;
    }

    private void OnDrawGizmosSelected()
    {
        if (_skeleton == null || _effective == null)
        {
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;

        if (_drawAttractors)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _skeleton.Attractors.Count; i++)
            {
                Gizmos.DrawCube(_skeleton.Attractors[i], Vector3.one * _effective.SegmentLength * 0.3f);
            }
        }

        if (_drawSkeleton)
        {
            for (int i = 0; i < _skeleton.Nodes.Count; i++)
            {
                TreeNode node = _skeleton.Nodes[i];
                if (node.Parent < 0)
                {
                    continue;
                }

                if (node.IsRoot)
                {
                    Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawLine(_skeleton.Nodes[node.Parent].Position, node.Position);
            }
        }
    }
}
