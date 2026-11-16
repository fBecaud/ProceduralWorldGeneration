using System;
using UnityEngine;

/// <summary>
/// Keyboard camera that orbits around the planet, using polar (spherical) coordinates:
///
///     distance   : how far the camera is from the planet center
///     azimuth    : angle around the vertical axis (like a longitude), in degrees
///     elevation  : angle above the equator (like a latitude), in degrees
///
////// </summary>
public class PlanetOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("What to orbit around (usually the Planet transform).")]
    public Transform target;
    [Tooltip("Optional. If set, the camera can never get inside the highest possible terrain.")]
    public Planet planet;
    [Tooltip("Minimum gap kept between the camera and the highest possible terrain.")]
    [Min(0f)] public float surfaceClearance = 2f;

    [Header("Polar coordinates")]
    public float distance = 35f;
    public float azimuth = 0f;
    [Range(-89f, 89f)] public float elevation = 20f;
    [Min(0.1f)] public float minDistance = 14f;
    [Min(0.1f)] public float maxDistance = 90f;

    [Header("Speeds")]
    [Tooltip("Degrees per second.")]
    public float rotationSpeed = 70f;
    [Tooltip("World units per second.")]
    public float zoomSpeed = 18f;
    [Tooltip("Rotation speed multiplier when the camera is as close as possible (finer control near the ground).")]
    [Range(0.05f, 1f)] public float closeRotationFactor = 0.35f;

    [Header("Keys")]
    public KeyCode rotateLeft = KeyCode.LeftArrow;
    public KeyCode rotateRight = KeyCode.RightArrow;
    public KeyCode rotateUp = KeyCode.UpArrow;
    public KeyCode rotateDown = KeyCode.DownArrow;
    public KeyCode zoomIn = KeyCode.I;
    public KeyCode zoomOut = KeyCode.O;

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.unscaledDeltaTime;

        // Closest allowed distance: never inside the ground, even after the player raised it.
        float lowerLimit = minDistance;
        if (planet != null)
            lowerLimit = Mathf.Max(lowerLimit, planet.Radius + surfaceClearance);
        float upperLimit = Mathf.Max(maxDistance, lowerLimit);

        // The closer we are, the slower we turn.
        float closeness = Mathf.InverseLerp(lowerLimit, upperLimit, distance); // 0 = close, 1 = far
        float speed = rotationSpeed * Mathf.Lerp(closeRotationFactor, 1f, closeness);

        // Left arrow moves the camera to the left of the screen: azimuth decreases.
        if (Input.GetKey(rotateLeft)) azimuth -= speed * dt;
        if (Input.GetKey(rotateRight)) azimuth += speed * dt;
        if (Input.GetKey(rotateUp)) elevation += speed * dt;
        if (Input.GetKey(rotateDown)) elevation -= speed * dt;

        if (Input.GetKey(zoomIn)) distance -= zoomSpeed * dt;
        if (Input.GetKey(zoomOut)) distance += zoomSpeed * dt;

        azimuth = Mathf.Repeat(azimuth, 360f);                  // wraps around, no limit needed
        elevation = Mathf.Clamp(elevation, -89f, 89f);          // stops before the poles (the 'up' vector would flip)
        distance = Mathf.Clamp(distance, lowerLimit, upperLimit);

        // Polar -> Cartesian.
        float lat = elevation * Mathf.Deg2Rad;
        float lon = azimuth * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(lat) * Mathf.Sin(lon),
            Mathf.Sin(lat),
            Mathf.Cos(lat) * Mathf.Cos(lon)) * distance;

        transform.position = target.position + offset;
        transform.LookAt(target.position, Vector3.up);
    }
}