using UnityEngine;

[RequireComponent(typeof(StuntManager))]
public class DriverController : MonoBehaviour
{
    [Header("Car Physics")]
    [SerializeField] private Rigidbody2D _frontTireRB;
    [SerializeField] private Rigidbody2D _backTireRB;
    [SerializeField] private Rigidbody2D _carRB;
    [SerializeField] private float _speed = 150f;
    [SerializeField] private float _rotationSpeed = 300f;
    [SerializeField] private float _airRotationMultiplier = 2f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Camera Zoom")]
    public CameraZoomOnAir cameraZoom;  // Assign this in inspector

    private bool _isGrounded;
    private StuntManager _stuntManager;

    private void Awake()
    {
        _stuntManager = GetComponent<StuntManager>();
        if (_stuntManager == null)
        {
            Debug.LogError("StuntManager component not found on DriverController GameObject.");
        }
    }

    private void FixedUpdate()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Update camera zoom based on grounded state
        if (cameraZoom != null)
        {
            cameraZoom.isGrounded = _isGrounded;
        }

        HandleMovement();
        HandleAirRotation();

        _stuntManager?.HandleStuntTracking(_isGrounded, wasGrounded, _carRB);
    }

    private void HandleMovement()
    {
        float torque = _speed * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            _frontTireRB.AddTorque(-torque);
            _backTireRB.AddTorque(-torque);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            _frontTireRB.AddTorque(torque);
            _backTireRB.AddTorque(torque);
        }
    }

    private void HandleAirRotation()
    {
        if (!_isGrounded)
        {
            float rotationTorque = _rotationSpeed * _airRotationMultiplier * Time.fixedDeltaTime;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _carRB.AddTorque(rotationTorque);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                _carRB.AddTorque(-rotationTorque);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}