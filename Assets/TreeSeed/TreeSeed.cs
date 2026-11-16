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
    float startRadius;
    public float maxRadius = 6f;
    public float durationToGrow = 120f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        activationSphereCollider = GetComponent<SphereCollider>();
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
        Forest.Plant(transform.localPosition, transform.localRotation, transform.localScale);
        StartCoroutine(RadiusAugment(startRadius, maxRadius, durationToGrow));
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
    public void Deactivate()
    {
        activationSphereCollider.radius = startRadius;
        bActive = false;
    }
    //Test for 
    private void OnEnable()
    {
        if (bActivateOnEnable)
            Activate();
    }
}
