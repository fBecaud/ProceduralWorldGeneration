using System.Collections.Generic;
using UnityEngine;

public class Forest : MonoBehaviour
{
    [SerializeField] private Planet _planet;
    [SerializeField] private int _seed = 0;
    [SerializeField] private TreeSettings _settings = new TreeSettings();

    [Header("Materials")]
    [SerializeField] private Material _barkMaterial;
    [SerializeField] private Material _leafMaterial;

    [Header("Spawning")]
    [SerializeField, Range(1, 20)] private int _treesPerFrame = 2;
    [SerializeField] private AnimationCurve _growthCurve = AnimationCurve.EaseInOut(0f, 0f, 8f, 1f);
    [SerializeField] private float _growthStagger = 4f;
    [SerializeField, Range(0.5f, 16f)] private float _rebuildBudgetMs = 4f;

    [Header("Test")]
    [SerializeField, Range(0, 500)] private int _testTrees = 0;

    private readonly Queue<Vector3> _pending = new Queue<Vector3>();
    private readonly List<TreeGenerator> _trees = new List<TreeGenerator>();
    private System.Random _random;

    public int Count
    {
        get { return _trees.Count; }
    }

    public int Pending
    {
        get { return _pending.Count; }
    }

    private void OnValidate()
    {
        TreeScheduler.BudgetMilliseconds = _rebuildBudgetMs;
    }

    private void Start()
    {
        TreeScheduler.BudgetMilliseconds = _rebuildBudgetMs;
        _random = new System.Random(_seed);

        if (_testTrees > 0)
        {
            PlantRandom(_testTrees);
        }
    }

    public void Plant(Vector3 worldPosition)
    {
        _pending.Enqueue(worldPosition);
    }

    public void PlantRandom(int count)
    {
        if (_planet == null)
        {
            Debug.LogWarning("Forest needs a planet to plant random trees.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 direction = TreeRandom.OnUnitSphere(_random);
            Plant(_planet.SurfacePoint(_planet.transform.position + direction));
        }
    }

    public void Clear()
    {
        _pending.Clear();

        for (int i = 0; i < _trees.Count; i++)
        {
            if (_trees[i] != null)
            {
                Destroy(_trees[i].gameObject);
            }
        }

        _trees.Clear();
    }

    private void Update()
    {
        for (int i = 0; i < _treesPerFrame && _pending.Count > 0; i++)
        {
            Spawn(_pending.Dequeue());
        }
    }

    private void Spawn(Vector3 worldPosition)
    {
        GameObject tree = new GameObject("Tree " + _trees.Count);
        tree.SetActive(false);
        tree.transform.SetParent(transform);
        tree.transform.position = worldPosition;

        tree.AddComponent<MeshFilter>();
        tree.AddComponent<MeshRenderer>();
        TreeGenerator generator = tree.AddComponent<TreeGenerator>();

        float delay = TreeRandom.Value(_random) * _growthStagger;
        generator.Setup(_planet, _settings, _random.Next(), _barkMaterial, _leafMaterial, _growthCurve, delay);

        tree.SetActive(true);
        _trees.Add(generator);
    }
    public GameObject Plant(in Vector3 localPosition, in Quaternion localRotation, in Vector3 localScale)
    {
        GameObject tree = new GameObject("Tree " + _trees.Count);
        tree.SetActive(false);
        tree.transform.SetParent(transform);
        tree.transform.localPosition = localPosition;
        tree.transform.localRotation = localRotation;
        tree.transform.localScale = localScale;

        tree.AddComponent<MeshFilter>();
        tree.AddComponent<MeshRenderer>();
        TreeGenerator generator = tree.AddComponent<TreeGenerator>();

        float delay = TreeRandom.Value(_random) * _growthStagger;
        generator.Setup(_planet, _settings, _random.Next(), _barkMaterial, _leafMaterial, _growthCurve, delay);

        tree.SetActive(true);
        _trees.Add(generator);
        return tree;
    }
    public void UnPlant(Transform tree)
    {
        _trees.Remove(tree.GetComponent<TreeGenerator>());
        tree.gameObject.SetActive(false);
        Destroy(tree.gameObject, 1f);
    }
}
