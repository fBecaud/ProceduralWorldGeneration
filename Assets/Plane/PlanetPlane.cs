using UnityEngine;

public class PlanetPlane : MonoBehaviour
{
    [SerializeField] private Planet _planet;
    [SerializeField] private Camera _camera;

    [Header("Flight")]
    [SerializeField] private float _maxSpeed = 2.5f;
    [SerializeField] private float _reverseRatio = 0.35f;
    [SerializeField] private float _speedResponse = 1.5f;
    [SerializeField] private float _turnSpeed = 80f;
    [SerializeField] private float _turnResponse = 3f;
    [SerializeField, Range(0.1f, 1f)] private float _closeSpeedFactor = 0.5f;

    [Header("Altitude")]
    [SerializeField] private float _altitude = 3f;
    [SerializeField] private float _minAltitude = 0.35f;
    [SerializeField] private float _maxAltitude = 14f;
    [SerializeField] private float _climbSpeed = 4f;
    [SerializeField] private float _altitudeResponse = 2.5f;
    [SerializeField] private float _groundClearance = 0.25f;

    [Header("Feel")]
    [SerializeField] private float _size = 0.25f;
    [SerializeField] private float _bankAngle = 35f;
    [SerializeField] private float _climbPitch = 20f;
    [SerializeField] private float _attitudeResponse = 4f;
    [SerializeField] private float _bobAmount = 0.06f;
    [SerializeField] private float _bobSpeed = 2f;

    [Header("Camera")]
    [SerializeField] private float _closeDistance = 0.9f;
    [SerializeField] private float _farDistance = 9f;
    [SerializeField, Range(0f, 89f)] private float _closeElevation = 22f;
    [SerializeField, Range(0f, 89f)] private float _farElevation = 70f;
    [SerializeField, Range(-89f, 89f)] private float _closePitch = 0f;
    [SerializeField, Range(-89f, 89f)] private float _farPitch = 0f;
    [SerializeField] private AnimationCurve _pitchCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float _lookAhead = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _centerFocus = 1f;
    [SerializeField] private AnimationCurve _focusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _cameraClearance = 0.3f;
    [SerializeField] private float _cameraResponse = 4f;

    [Header("Keys")]
    [SerializeField] private KeyCode _forward = KeyCode.UpArrow;
    [SerializeField] private KeyCode _backward = KeyCode.DownArrow;
    [SerializeField] private KeyCode _turnLeft = KeyCode.LeftArrow;
    [SerializeField] private KeyCode _turnRight = KeyCode.RightArrow;
    [SerializeField] private KeyCode _climb = KeyCode.O;
    [SerializeField] private KeyCode _descend = KeyCode.I;

    [Header("Debug")]
    [SerializeField] private bool _showControls = true;

    private Quaternion _frame;
    private float _height;
    private float _speed;
    private float _turn;
    private float _bank;
    private float _pitch;
    private float _propellerAngle;
    private Transform _model;
    private Transform _propeller;
    private Vector3 _cameraPosition;
    private Quaternion _cameraRotation;
    private TreeSeedActivator _seedActivator;
    private GUIStyle _controlsStyle;

    private void Start()
    {
        if (_planet == null)
        {
            _planet = FindFirstObjectByType<Planet>();
        }

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_planet == null || _camera == null)
        {
            Debug.LogWarning("PlanetPlane needs a planet and a camera.", this);
            enabled = false;
            return;
        }

        PlanetOrbitCamera orbit = _camera.GetComponent<PlanetOrbitCamera>();
        if (orbit != null)
        {
            orbit.enabled = false;
        }

        _seedActivator = FindFirstObjectByType<TreeSeedActivator>();

        Transform tail;
        _model = PlaneModel.Build(transform, _size, out _propeller, out tail);
        PlaneModel.BuildTrail(tail, _size);

        Vector3 up = (_camera.transform.position - Center()).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.up, up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        }

        _frame = Quaternion.LookRotation(forward.normalized, up);
        _altitude = Mathf.Clamp(_altitude, _minAltitude, _maxAltitude);
        _height = Mathf.Max(_altitude, GroundHeight(up) + _groundClearance);

        transform.position = Center() + up * (_planet.Radius + _height);
        transform.rotation = _frame;
        _cameraPosition = DesiredCameraPosition();
        _cameraRotation = DesiredCameraRotation();
        ApplyCamera();
    }

    private void Update()
    {
        ControlsOverlay.HandleToggle();

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        float farness = Farness();
        float throttle = Axis(_forward, _backward);
        float steering = Axis(_turnRight, _turnLeft);
        float climbing = Axis(_climb, _descend);

        float targetSpeed = throttle * _maxSpeed * Mathf.Lerp(_closeSpeedFactor, 1f, farness);
        if (throttle < 0f)
        {
            targetSpeed *= _reverseRatio;
        }

        _speed = Mathf.Lerp(_speed, targetSpeed, Smoothing(_speedResponse, dt));
        _turn = Mathf.Lerp(_turn, steering, Smoothing(_turnResponse, dt));
        _altitude = Mathf.Clamp(_altitude + climbing * _climbSpeed * Mathf.Lerp(0.3f, 1f, farness) * dt, _minAltitude, _maxAltitude);

        float radius = _planet.Radius + _height;
        float travel = _speed * dt / Mathf.Max(radius, 0.01f) * Mathf.Rad2Deg;
        _frame = _frame * Quaternion.AngleAxis(_turn * _turnSpeed * dt, Vector3.up) * Quaternion.AngleAxis(travel, Vector3.right);
        _frame = Quaternion.Normalize(_frame);

        Vector3 up = _frame * Vector3.up;
        float ground = GroundHeight(up);
        float previousHeight = _height;
        _height = Mathf.Lerp(_height, Mathf.Max(_altitude, ground + _groundClearance), Smoothing(_altitudeResponse, dt));
        _height = Mathf.Max(_height, ground + _groundClearance * 0.5f);

        float climbRate = (_height - previousHeight) / dt;
        _bank = Mathf.Lerp(_bank, -_turn * _bankAngle, Smoothing(_attitudeResponse, dt));
        _pitch = Mathf.Lerp(_pitch, -Mathf.Clamp(climbRate * 15f, -_climbPitch, _climbPitch), Smoothing(_attitudeResponse, dt));

        transform.position = Center() + up * (_planet.Radius + _height);
        transform.rotation = _frame * Quaternion.Euler(_pitch, 0f, _bank);

        _model.localPosition = Vector3.up * Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;
        _propellerAngle += (500f + Mathf.Abs(_speed) * 900f) * dt;
        _propeller.localRotation = Quaternion.Euler(0f, 0f, _propellerAngle);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        float k = Smoothing(_cameraResponse, dt);
        _cameraPosition = Vector3.Lerp(_cameraPosition, DesiredCameraPosition(), k);
        _cameraRotation = Quaternion.Slerp(_cameraRotation, DesiredCameraRotation(), k);
        ApplyCamera();
    }

    private Vector3 DesiredCameraPosition()
    {
        float farness = Farness();
        float distance = Mathf.Lerp(_closeDistance, _farDistance, farness);
        float elevation = Mathf.Lerp(_closeElevation, _farElevation, farness) * Mathf.Deg2Rad;

        Vector3 up = _frame * Vector3.up;
        Vector3 forward = _frame * Vector3.forward;
        Vector3 desired = transform.position - forward * (distance * Mathf.Cos(elevation)) + up * (distance * Mathf.Sin(elevation));

        Vector3 radial = desired - Center();
        Vector3 direction = radial.normalized;
        float minimum = _planet.Radius + GroundHeight(direction) + _cameraClearance;
        if (radial.magnitude < minimum)
        {
            desired = Center() + direction * minimum;
        }

        return desired;
    }

    private Quaternion DesiredCameraRotation()
    {
        float farness = Farness();
        float distance = Mathf.Lerp(_closeDistance, _farDistance, farness);
        Vector3 ahead = transform.position + (_frame * Vector3.forward) * (distance * _lookAhead);
        Vector3 target = Vector3.Lerp(ahead, Center(), _focusCurve.Evaluate(farness) * _centerFocus);
        Vector3 toTarget = target - _cameraPosition;
        if (toTarget.sqrMagnitude < 0.000001f)
        {
            return _cameraRotation;
        }

        Vector3 up = (_cameraPosition - Center()).normalized;
        float pitch = Mathf.LerpUnclamped(_closePitch, _farPitch, _pitchCurve.Evaluate(farness));
        return Quaternion.LookRotation(toTarget, up) * Quaternion.Euler(-pitch, 0f, 0f);
    }

    private void ApplyCamera()
    {
        _camera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation);
    }

    private float Farness()
    {
        return Mathf.InverseLerp(_minAltitude, _maxAltitude, _height);
    }

    private float GroundHeight(Vector3 direction)
    {
        Vector3 above = Center() + direction * (_planet.Radius + 1f);
        return Vector3.Distance(_planet.SurfacePoint(above), Center()) - _planet.Radius;
    }

    private Vector3 Center()
    {
        return _planet.transform.position;
    }

    private static float Axis(KeyCode positive, KeyCode negative)
    {
        float value = 0f;
        if (Input.GetKey(positive))
        {
            value += 1f;
        }

        if (Input.GetKey(negative))
        {
            value -= 1f;
        }

        return value;
    }

    private static float Smoothing(float response, float dt)
    {
        return 1f - Mathf.Exp(-response * dt);
    }

    private void OnGUI()
    {
        if (!_showControls || !ControlsOverlay.Visible)
        {
            return;
        }

        if (_controlsStyle == null)
        {
            _controlsStyle = new GUIStyle(GUI.skin.label);
            _controlsStyle.fontSize = 12;
            _controlsStyle.normal.textColor = Color.white;
        }

        string text =
            "Avancer / Reculer : " + _forward + " / " + _backward + "\n" +
            "Tourner : " + _turnLeft + " / " + _turnRight + "\n" +
            "Monter / Descendre : " + _climb + " / " + _descend;

        if (_seedActivator != null)
        {
            text +=
                "\nPlanter une graine : " + MouseButtonName(_seedActivator.mouseButtonToActivate) + "\n" +
                "Retirer une graine : " + MouseButtonName(_seedActivator.mouseButtonToDeActivate);
        }

        text += "\nMasquer l'interface : " + ControlsOverlay.ToggleKeyLabel;

        GUI.Label(new Rect(10, 10, 400, 120), text, _controlsStyle);
    }

    private static string MouseButtonName(int button)
    {
        if (button == 0)
        {
            return "Clic gauche";
        }

        if (button == 1)
        {
            return "Clic droit";
        }

        return "Clic molette";
    }
}
