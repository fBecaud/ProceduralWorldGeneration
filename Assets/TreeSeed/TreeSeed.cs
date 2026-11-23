using NUnit;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TreeSeed : MonoBehaviour
{
    [Header("Forest")]
    [Tooltip("Forest to scatter. Found automatically on this GameObject if left empty.")]
    public Forest Forest = null;
    public bool bActivateOnEnable = false;
    public bool bActive = false;
    public SphereCollider activationSphereCollider;
    public Transform SeedRenderer;
    
    private Transform activeTree = null;

    float startRadius;
    public float maxRadius = 6f;
    public float durationToGrow = 120f;

    float startSeedScale;
    public float minSeedScale = 0.01f;
    public float durationToShrink = 3f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!activationSphereCollider)
            activationSphereCollider = GetComponent<SphereCollider>();
        if (!SeedRenderer)
            SeedRenderer = transform.GetChild(0);
        startSeedScale = SeedRenderer.localScale.x;
        startRadius = activationSphereCollider.radius;
        if (!Forest)
            Forest = FindFirstObjectByType<Forest>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Activate();
    }
    public void Activate()
    {
        if (bActive)
            return;
        bActive = true;
        activeTree = Forest.Plant(transform.localPosition, transform.localRotation, transform.localScale);
        if (activationSphereCollider)
            StartCoroutine(RadiusAugment(startRadius, maxRadius, durationToGrow));
        if (SeedRenderer)
            StartCoroutine(SeedShrink(startSeedScale*1.2f, minSeedScale, durationToShrink));
    }

    IEnumerator RadiusAugment(float start, float end, float duration)
    {
        float timeSinceStart = 0f;

        while (timeSinceStart < duration)
        {
            timeSinceStart += Time.deltaTime;

            float progression = timeSinceStart / duration;

            activationSphereCollider.radius = Mathf.Lerp(start, end, progression);
            yield return null;
        }

        // Garantit que la valeur finale est exacte à la fin des 5 secondes
        activationSphereCollider.radius = end;
    }
    IEnumerator SeedShrink(float start, float end, float duration)
    {
        float timeSinceStart = 0f;

        while (timeSinceStart < duration)
        {
            timeSinceStart += Time.deltaTime;

            float progression = timeSinceStart / duration;
            float scale = Mathf.Lerp(start, end, progression);
            SeedRenderer.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }

        // Garantit que la valeur finale est exacte à la fin des 5 secondes
        SeedRenderer.localScale = new Vector3(end, end, end);
    }
    public void Deactivate()
    {
        if (!bActive)
            return;
        activationSphereCollider.radius = startRadius;
        SeedRenderer.localScale = new Vector3(startSeedScale, startSeedScale, startSeedScale);
        bActive = false;
        Forest.UnPlant(activeTree);
        activeTree = null;
    }
    //Test for 
    private void OnEnable()
    {
        if (bActivateOnEnable)
            Activate();
    }
}
