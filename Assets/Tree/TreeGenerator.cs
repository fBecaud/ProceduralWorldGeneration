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
    [SerializeField] private float _growthDuration = 8f;
    [SerializeField] private float _growthDelay = 0f;
    [SerializeField] private float _rebuildsPerSecond = 15f;

    [Header("Debug")]
    [SerializeField] private bool _buildMesh = true;
    [SerializeField] private bool _drawSkeleton = true;
    [SerializeField] private bool _drawAttractors = false;

    private static readonly GrowthState _sharedState = new GrowthState();
    private static readonly MeshData _sharedData = new MeshData();

    private Mesh _mesh;
    private Mesh _leafMesh;
    private GameObject _leafObject;
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

    public void Setup(Planet planet, TreeSettings settings, int seed, Material barkMaterial, Material leafMaterial, float growthDuration, float growthDelay)
    {
        _planet = planet;
        _settings = settings;
        _seed = seed;
        _barkMaterial = barkMaterial;
        _leafMaterial = leafMaterial;
        _animateOnPlay = true;
        _growthDuration = growthDuration;
        _growthDelay = growthDelay;
    }

    public void Grow(float duration, float delay)
    {
        _growthDuration = duration;
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
            Grow(_growthDuration, _growthDelay);
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
            float elapsed = Time.time - _growthStartTime - _growthDelay;
            _growth = Mathf.Clamp01(elapsed / Mathf.Max(_growthDuration, 0.01f));

            if (_growth >= 1f)
            {
                _growing = false;
            }
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
            if (_leafMesh != null)
            {
                _leafMesh.Clear();
            }

            return;
        }

        TreeGrowth.Evaluate(_skeleton, _effective, _growth, _sharedState);
        TreeMeshBuilder.Build(_sharedData, _skeleton, _sharedState, _effective);

        if (_leafMesh != null)
        {
            _sharedData.ApplySplit(_mesh, _leafMesh);
        }
        else
        {
            _sharedData.Apply(_mesh);
        }
    }

    private void EnsureMeshes()
    {
        if (_mesh == null || !_mesh.isReadable)
        {
            DestroyAsset(_mesh);
            _mesh = CreateMesh("Tree");
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        bool split = _effective != null && !_effective.LeafShadows;
        if (!split)
        {
            DestroyLeafObject();
            return;
        }

        if (_leafObject == null)
        {
            _leafObject = new GameObject("Leaves");
            _leafObject.hideFlags = HideFlags.HideAndDontSave;
            _leafObject.transform.SetParent(transform, false);
            _leafObject.AddComponent<MeshFilter>();

            MeshRenderer leafRenderer = _leafObject.AddComponent<MeshRenderer>();
            leafRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        if (_leafMesh == null || !_leafMesh.isReadable)
        {
            DestroyAsset(_leafMesh);
            _leafMesh = CreateMesh("Tree Leaves");
            _leafObject.GetComponent<MeshFilter>().sharedMesh = _leafMesh;
        }
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

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (_leafObject != null)
        {
            renderer.sharedMaterials = new Material[] { bark };
            _leafObject.GetComponent<MeshRenderer>().sharedMaterial = leaf;
            return;
        }

        Material[] current = renderer.sharedMaterials;
        if (current.Length == 2 && current[0] == bark && current[1] == leaf)
        {
            return;
        }

        renderer.sharedMaterials = new Material[] { bark, leaf };
    }

    private static Material CreateFallbackMaterial(string name)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = new Material(shader);
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
