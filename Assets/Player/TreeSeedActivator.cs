using UnityEngine;

public class TreeSeedActivator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used to cast the mouse ray. Camera.main if left empty.")]
    public Camera cam;

    [Header("Click")]
    [Tooltip("0 = left, 1 = right, 2 = middle. Left and right are already used by PlanetTerrainEditor " +
             "(raise / lower), so the middle button is the default.")]
    [Range(0, 2)] public int mouseButtonToActivate = 0;
    [Range(0, 2)] public int mouseButtonToDeActivate = 1;
    [Min(1f)] public float maxDistance = 500f;
    [Tooltip("Layers the click can hit.")]
    public LayerMask layerMask = ~0;

    // Reused buffer: RaycastNonAlloc does not allocate memory at every click.
    readonly RaycastHit[] hits = new RaycastHit[32];

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButtonToActivate) && !Input.GetMouseButtonDown(mouseButtonToDeActivate)) return;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // The activation collider is a trigger, so we explicitly ask the ray to hit triggers
        // (otherwise it depends on the Physics > Queries Hit Triggers project setting).
        int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, layerMask, QueryTriggerInteraction.Collide);

        // RaycastNonAlloc returns the hits in no particular order: keep the CLOSEST seed that is not active yet.
        // Seeds already active are skipped on purpose: their sphere keeps growing, and it would
        // otherwise hide every seed located behind it or inside it.
        TreeSeed closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            TreeSeed seed = hits[i].collider.GetComponentInParent<TreeSeed>();
            if (seed == null) continue;

            if (hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                closest = seed;
            }
        }

        if (closest != null)
        {
            if (Input.GetMouseButtonDown(mouseButtonToActivate))
                closest.Activate();
            else
                closest.Deactivate();
        }
    }
}