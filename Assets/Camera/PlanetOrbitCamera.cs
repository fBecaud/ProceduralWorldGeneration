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
    [Tooltip("Optional. If set, the camera can never get inside the terrain below it.")]
    public Planet planet;
    [Tooltip("Minimum gap kept between the camera and the terrain right below it.")]
    [Min(0f)] public float surfaceClearance = 2f;

    [Header("Polar coordinates")]
    public float distance = 35f;
    public float azimuth = 0f;
    [Range(-89f, 89f)] public float elevation = 20f;
    public float minDistance = 14f;
    [Min(0.1f)] public float maxDistance = 90f;

    [Header("Look offset")]
    [Range(-89f, 89f)] public float closePitch = 30f;
    [Range(-89f, 89f)] public float farPitch = 0f;
    public AnimationCurve pitchCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Range(-180f, 180f)] public float lookYaw = 0f;

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

    [Header("Debug")]
    public bool showControls = true;

    Quaternion orbit;
    bool orbitReady;
    GUIStyle controlsStyle;
    TreeSeedActivator seedActivator;
    bool seedActivatorSearched;

    void OnGUI()
    {
        if (!showControls || !ControlsOverlay.Visible) return;

        if (controlsStyle == null)
        {
            controlsStyle = new GUIStyle(GUI.skin.label);
            controlsStyle.fontSize = 12;
            controlsStyle.normal.textColor = Color.white;
        }

        if (!seedActivatorSearched)
        {
            seedActivator = FindFirstObjectByType<TreeSeedActivator>();
            seedActivatorSearched = true;
        }

        string text =
            "Tourner : " + rotateLeft + " / " + rotateRight + " / " + rotateUp + " / " + rotateDown + "\n" +
            "Zoomer : " + zoomIn + " / " + zoomOut;

        if (seedActivator != null)
        {
            text +=
                "\nPlanter une graine : " + MouseButtonName(seedActivator.mouseButtonToActivate) + "\n" +
                "Retirer une graine : " + MouseButtonName(seedActivator.mouseButtonToDeActivate);
        }

        text += "\nMasquer l'interface : " + ControlsOverlay.ToggleKeyLabel;

        GUI.Label(new Rect(10, 10, 400, 100), text, controlsStyle);
    }

    static string MouseButtonName(int button)
    {
        if (button == 0) return "Clic gauche";
        if (button == 1) return "Clic droit";
        return "Clic molette";
    }

    void OnValidate()
    {
        orbitReady = false;
    }

    void ResetOrbit()
    {
        float lat = elevation * Mathf.Deg2Rad;
        float lon = azimuth * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(
            Mathf.Cos(lat) * Mathf.Sin(lon),
            Mathf.Sin(lat),
            Mathf.Cos(lat) * Mathf.Cos(lon));

        Vector3 north = new Vector3(
            -Mathf.Sin(lat) * Mathf.Sin(lon),
            Mathf.Cos(lat),
            -Mathf.Sin(lat) * Mathf.Cos(lon));

        orbit = Quaternion.LookRotation(-direction, north);
        orbitReady = true;
    }

    void LateUpdate()
    {
        ControlsOverlay.HandleToggle();

        if (target == null) return;
        if (!orbitReady) ResetOrbit();

        float dt = Time.unscaledDeltaTime;

        float reference = minDistance;
        if (planet != null)
            reference = Mathf.Max(reference, planet.Radius);
        float upperLimit = Mathf.Max(maxDistance, reference);

        // The closer we are, the slower we turn.
        float closeness = Mathf.InverseLerp(reference, upperLimit, distance); // 0 = close, 1 = far
        float speed = rotationSpeed * Mathf.Lerp(closeRotationFactor, 1f, closeness);

        float yaw = 0f;
        float tilt = 0f;
        if (Input.GetKey(rotateLeft)) yaw -= speed * dt;
        if (Input.GetKey(rotateRight)) yaw += speed * dt;
        if (Input.GetKey(rotateUp)) tilt += speed * dt;
        if (Input.GetKey(rotateDown)) tilt -= speed * dt;

        if (Input.GetKey(zoomIn)) distance -= zoomSpeed * dt;
        if (Input.GetKey(zoomOut)) distance += zoomSpeed * dt;

        orbit = orbit * Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(tilt, Vector3.right);
        orbit = Quaternion.Normalize(orbit);

        // Closest allowed distance: never inside the ground right below the camera.
        Vector3 direction = orbit * Vector3.back;
        float lowerLimit = minDistance;
        if (planet != null)
            lowerLimit = Mathf.Max(lowerLimit, GroundDistance(direction) + surfaceClearance);

        distance = Mathf.Clamp(distance, lowerLimit, Mathf.Max(upperLimit, lowerLimit));

        Vector3 offset = direction * distance;
        float farness = Mathf.InverseLerp(reference, upperLimit, distance);
        float pitch = Mathf.LerpUnclamped(closePitch, farPitch, pitchCurve.Evaluate(farness));

        transform.position = target.position + offset;
        transform.rotation = orbit * Quaternion.Euler(-pitch, lookYaw, 0f);
    }

    float GroundDistance(Vector3 direction)
    {
        Vector3 above = target.position + direction * (planet.Radius + 1f);
        return Vector3.Distance(planet.SurfacePoint(above), target.position);
    }
}