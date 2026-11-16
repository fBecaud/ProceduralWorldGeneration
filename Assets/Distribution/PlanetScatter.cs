
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scatters prefabs over a procedural planet (see Planet.cs).
///
/// Pipeline:
///   1) Poisson disk sampling (Bridson's algorithm) directly ON A SPHERE,
///   2) for each point, read the terrain height from the planet's noise (Planet.Relief),
///   3) reject points using filters: height, slope and an optional density mask,
///   4) instantiate prefabs (position, orientation along the surface, random scale).
///
/// Attach this component to the same GameObject as the Planet MonoBehaviour.
///
/// </summary>
public class PlanetScatter : MonoBehaviour
{
    const string ContainerName = "Scattered";
    Transform Container = null;

    const int MaxPoints = 50000;

    [Header("Planet")]
    [Tooltip("Planet to scatter on. Found automatically on this GameObject if left empty.")]
    public Planet planet;
    [Header("Forest")]

    [Header("Poisson disk")]
    [Tooltip("Minimum straight-line distance between two points, in world units.")]
    [Min(0.05f)] public float minDistance = 2f;
    [Tooltip("Bridson's 'k': candidates tried around a point before giving up on it.")]
    [Range(5, 60)] public int attemptsPerPoint = 30;
    public int seed = 0;

    [Header("Filters")]
    [Tooltip("Terrain height relative to the base sphere radius, in world units " +
             "(0 = sea level, positive = hills, negative = rifts).")]
    public float minHeight = 0.001f;
    public float maxHeight = 2f;
    [Tooltip("Slope in degrees. 0 = flat ground, 90 = vertical cliff.")]
    [Range(0f, 90f)] public float minSlope = 0f;
    [Range(0f, 90f)] public float maxSlope = 35f;
    [Tooltip("Step used to estimate the surface normal (world units). Smaller = more precise, but noisier.")]
    [Min(0.01f)] public float normalSampleDistance = 0.25f;

    [Header("Density mask (optional)")]
    public bool useDensityNoise = true;
    [Tooltip("Frequency of the mask on the unit sphere: higher = smaller patches.")]
    [Min(0.01f)] public float densityNoiseScale = 4f;
    [Tooltip("Noise value (0..1) -> probability of keeping the point (0..1).")]
    public AnimationCurve densityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Tooltip("Tick if Noise.Perlin returns values in [-1, 1]; untick if it returns [0, 1].")]
    public bool noiseOutputIsSigned = true;

    [Header("Instances")]
    public GameObject prefab;
    [Tooltip("Offset along the radial direction (away from the planet center).")]
    public float surfaceOffset = 0f;
    [Tooltip("0 = prefab 'up' points away from the planet center, 1 = aligned on the real surface normal.")]
    [Range(0f, 1f)] public float alignToNormal = 0.5f;
    public bool randomYaw = true;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    readonly List<Matrix4x4> matrices = new List<Matrix4x4>();

    /// <summary>World matrices of the generated instances (handy for GPU instancing).</summary>
    public IReadOnlyList<Matrix4x4> Matrices => matrices;
    private void Start()
    {
        Scatter();
    }
    // ------------------------------------------------------------------
    //  Public API
    // ------------------------------------------------------------------

    [ContextMenu("Scatter")]
    public void Scatter()
    {
        if (!EnsurePlanet()) return;

        Clear();
        matrices.Clear();

        // One single System.Random drives everything -> same seed = same result.
        var rng = new System.Random(seed);

        float baseRadius = planet.Radius;

        // Poisson disk on the sphere. We only keep unit DIRECTIONS (points on a sphere of radius 1):
        List<Vector3> directions = GeneratePoissonOnSphere(rng, baseRadius, minDistance, attemptsPerPoint);

        // Random offset for the density mask, so that different seeds give different patches.
        var noiseOffset = new Vector3(
            (float)rng.NextDouble() * 1000f,
            (float)rng.NextDouble() * 1000f,
            (float)rng.NextDouble() * 1000f);

        if (prefab != null)
        {
            Container = new GameObject(ContainerName).transform;
            Container.SetParent(transform, false);
        }


        foreach (Vector3 dir in directions)
        {
            // Draw ALL the random numbers before filtering.
            float rDensity = (float)rng.NextDouble();
            float rYaw = (float)rng.NextDouble();
            float rScale = (float)rng.NextDouble();

            // Terrain height at this point.
            // Planet.Relief(dir) is exactly what the planet adds to its base radius, so
            // the real surface is at distance (baseRadius + height) from the center.
            float height = planet.Relief(dir);
            if (height < minHeight || height > maxHeight) continue;

            // Slope: angle between the surface normal and the "vertical" of a planet,
            // which is the radial direction (center -> point).
            Vector3 normal = ComputeSurfaceNormal(dir, baseRadius);
            float slope = Vector3.Angle(normal, dir);
            if (slope < minSlope || slope > maxSlope) continue;

            // Density mask.
            if (useDensityNoise)
            {
                float n = Noise.Perlin(dir * densityNoiseScale + noiseOffset);
                float n01 = noiseOutputIsSigned ? n * 0.5f + 0.5f : n;
                float keepProbability = Mathf.Clamp01(densityCurve.Evaluate(Mathf.Clamp01(n01)));
                if (rDensity > keepProbability) continue;
            }

            // Build the transform in the Planet's local space.
            Vector3 localPos = dir * (baseRadius + height + surfaceOffset);

            // Blend between "away from the center" and "real surface normal".
            Vector3 up = Vector3.Slerp(dir, normal, alignToNormal);
            // First spin around the prefab's own vertical axis, then tilt that axis onto 'up'.
            Quaternion yaw = randomYaw ? Quaternion.Euler(0f, rYaw * 360f, 0f) : Quaternion.identity;
            Quaternion localRot = Quaternion.FromToRotation(Vector3.up, up) * yaw;

            Vector3 localScale = Vector3.one * Mathf.Lerp(scaleRange.x, scaleRange.y, rScale);

            matrices.Add(transform.localToWorldMatrix * Matrix4x4.TRS(localPos, localRot, localScale));

            if (Container != null)
            {
                GameObject go = Spawn(Container);
                go.transform.localPosition = localPos;
                go.transform.localRotation = localRot;
                go.transform.localScale = localScale;
            }

        }

        Debug.Log($"[PlanetScatter] {matrices.Count} instances kept out of {directions.Count} Poisson points.", this);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (Container == null) return;

        if (Application.isPlaying)
        {
            Container.name = ContainerName + " (old)"; // Destroy() is deferred: rename to avoid name collisions
            Destroy(Container.gameObject);
        }
        else
        {
            DestroyImmediate(Container.gameObject);
        }
    }

    // ------------------------------------------------------------------
    //  Surface helpers
    // ------------------------------------------------------------------

    bool EnsurePlanet()
    {
        if (planet == null) planet = GetComponent<Planet>();
        if (planet == null)
        {
            Debug.LogError("[PlanetScatter] No Planet found. Assign one or put this script on the Planet GameObject.", this);
            return false;
        }
        return true;
    }

    /// <summary>Point on the REAL surface (base radius + relief) for a given direction.</summary>
    Vector3 SurfacePoint(Vector3 direction)
    {
        Vector3 d = direction.normalized;
        return d * (planet.Radius + planet.Relief(d));
    }

    /// <summary>
    /// Estimates the outward surface normal with finite differences.
    /// Idea: take 2 pairs of neighbouring surface points along two perpendicular tangent
    /// directions. The cross product of the two difference vectors is perpendicular to
    /// the surface. (Same trick as the heightmap version, but on a sphere.)
    /// </summary>
    Vector3 ComputeSurfaceNormal(Vector3 dir, float baseRadius)
    {
        BuildTangentBasis(dir, out Vector3 t1, out Vector3 t2);

        // Moving 'normalSampleDistance' along the surface ~ tilting the direction by
        // (distance / radius) radians.
        float eps = normalSampleDistance / baseRadius;

        Vector3 a1 = SurfacePoint(dir + t1 * eps);
        Vector3 b1 = SurfacePoint(dir - t1 * eps);
        Vector3 a2 = SurfacePoint(dir + t2 * eps);
        Vector3 b2 = SurfacePoint(dir - t2 * eps);

        Vector3 n = Vector3.Cross(a1 - b1, a2 - b2).normalized;

        // The cross product can point inward depending on the basis orientation: force it outward.
        if (Vector3.Dot(n, dir) < 0f) n = -n;
        return n;
    }

    /// <summary>
    /// Builds two unit vectors t1, t2 that are perpendicular to each other and to 'normal'
    /// (they span the plane tangent to the sphere at that point).
    /// </summary>
    static void BuildTangentBasis(Vector3 normal, out Vector3 t1, out Vector3 t2)
    {
        // Pick any axis that is not (almost) parallel to the normal, otherwise Cross() ~ zero.
        Vector3 helper = Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right;
        t1 = Vector3.Cross(normal, helper).normalized;
        t2 = Vector3.Cross(normal, t1);
    }

    // ------------------------------------------------------------------
    //  Poisson disk sampling on a sphere (Bridson)
    // ------------------------------------------------------------------
    //
    // Differences with the classic 2D version:
    //  - Points live on the surface of a sphere, but distances are measured in 3D
    //    (straight line). So the acceleration grid must be 3D too.
    //  - A candidate is made by "walking" a given distance from an existing point
    //    along the sphere, in a random direction (a rotation around the center).
    //  - The sphere only touches a thin shell of the 3D grid, so a dense array would waste
    //    memory: we use a Dictionary (sparse grid) instead.
    //
    // Note: sampling is done on the PERFECT base sphere, then the heights are applied
    // afterwards. Since the relief is small compared to the radius, the spacing is
    // almost unchanged.

    /// <returns>Unit directions (points on a sphere of radius 1), at least minDist apart (on the base sphere).</returns>
    static List<Vector3> GeneratePoissonOnSphere(System.Random rng, float sphereRadius, float minDist, int attempts)
    {
        // Cell size: the diagonal of a cube is side * sqrt(3). If the diagonal is smaller than
        // minDist, two accepted points (>= minDist apart) can NEVER share a cell.
        // So each cell holds at most one point. (0.99999 guards against rounding errors.)
        float cellSize = minDist / Mathf.Sqrt(3f) * 0.99999f;
        float minDistSqr = minDist * minDist;

        Dictionary<Vector3Int, int> grid = new();    // cell -> index in 'points'
        List<Vector3> points = new();                // positions on the base sphere
        List<int> active = new();                    // points that may still have free space around them

        Vector3Int CellOf(Vector3 p)
        {
            return new Vector3Int(
                Mathf.FloorToInt(p.x / cellSize),
                Mathf.FloorToInt(p.y / cellSize),
                Mathf.FloorToInt(p.z / cellSize));
        }

        void Add(Vector3 p)
        {
            points.Add(p);
            active.Add(points.Count - 1);
            grid[CellOf(p)] = points.Count - 1;
        }

        bool IsValid(Vector3 p)
        {
            // A neighbour closer than minDist can be at most ceil(minDist / cellSize) = ceil(sqrt(3)) = 2
            // cells away on each axis -> we check a 5x5x5 block of cells.
            Vector3Int c = CellOf(p);
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (grid.TryGetValue(new Vector3Int(c.x + dx, c.y + dy, c.z + dz), out int idx)
                            && (points[idx] - p).sqrMagnitude < minDistSqr)
                            return false;
                    }
            return true;
        }

        // Step 1: first point, uniformly distributed on the sphere.
        // (Picking y uniformly in [-1, 1] and an angle gives a uniform distribution on the sphere's surface.)
        float y = (float)rng.NextDouble() * 2f - 1f;
        float phi = (float)rng.NextDouble() * Mathf.PI * 2f;
        float ring = Mathf.Sqrt(1f - y * y);
        Add(new Vector3(ring * Mathf.Cos(phi), y, ring * Mathf.Sin(phi)) * sphereRadius);

        // Step 2: grow from the active points.
        while (active.Count > 0 && points.Count < MaxPoints)
        {
            int ai = rng.Next(active.Count);
            Vector3 center = points[active[ai]];
            Vector3 centerDir = center / sphereRadius;
            BuildTangentBasis(centerDir, out Vector3 t1, out Vector3 t2);

            bool found = false;
            for (int i = 0; i < attempts; i++)
            {
                // Random direction in the tangent plane, and a distance in [minDist, 2*minDist].
                // The sqrt(1 + 3u) makes the candidates uniform in AREA inside the ring
                // (otherwise they would cluster near the inner edge).
                float theta = (float)rng.NextDouble() * Mathf.PI * 2f;
                float arcLength = minDist * Mathf.Sqrt(1f + 3f * (float)rng.NextDouble());
                float alpha = Mathf.Min(arcLength / sphereRadius, Mathf.PI); // arc length -> angle at the planet center

                // Rotate 'centerDir' by 'alpha' toward the chosen tangent direction.
                Vector3 tangent = t1 * Mathf.Cos(theta) + t2 * Mathf.Sin(theta);
                Vector3 candidateDir = centerDir * Mathf.Cos(alpha) + tangent * Mathf.Sin(alpha);
                Vector3 candidate = candidateDir * sphereRadius;

                if (IsValid(candidate))
                {
                    Add(candidate);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // No room left around this point: remove it from the active list
                // (swap with the last element so the removal is O(1)).
                active[ai] = active[active.Count - 1];
                active.RemoveAt(active.Count - 1);
            }
        }

        // Convert positions to unit directions.
        var directions = new List<Vector3>(points.Count);
        foreach (Vector3 p in points) directions.Add(p / sphereRadius);
        return directions;
    }

    // ------------------------------------------------------------------
    //  Instancing
    // ------------------------------------------------------------------

    GameObject Spawn(Transform parent)
    {
#if UNITY_EDITOR
        // Outside play mode, keep the link to the prefab asset (nicer in the editor).
        if (!Application.isPlaying)
        {
            var inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (inst != null) return inst;
        }
#endif
        return Instantiate(prefab, parent);
    }
}
