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
    [SerializeField] private Transform frontTireGroundCheck;
    [SerializeField] private Transform backTireGroundCheck;
    [SerializeField] [Range(0.01f, 0.5f)] private float groundCheckRadius = 0.1f;

    [Header("Camera Zoom")]
    public CameraZoomOnAir cameraZoom;  // Assign this in inspector

    private bool _isGrounded;
    private bool _wasGroundedLastFrame;
    private bool _landedThisFrame;
    private bool _isDead; // Set this flag externally when player dies

    private bool _frontTireGrounded;
    private bool _backTireGrounded;

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
        _frontTireGrounded = Physics2D.OverlapCircle(frontTireGroundCheck.position, groundCheckRadius, groundLayer);
        _backTireGrounded = Physics2D.OverlapCircle(backTireGroundCheck.position, groundCheckRadius, groundLayer);
        _isGrounded = _frontTireGrounded || _backTireGrounded;

        _landedThisFrame = !_wasGroundedLastFrame && _isGrounded;
        _wasGroundedLastFrame = _isGrounded;

        if (cameraZoom != null)
        {
            cameraZoom.isGrounded = _isGrounded;
        }

        HandleMovement();
        HandleAirRotation();

        _stuntManager?.HandleStuntTracking(
            _isGrounded,
            _isDead,
            _carRB,
            _frontTireGrounded,
            _backTireGrounded,
            _landedThisFrame
        );
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

    /// <summary>
    /// Call this method from your death logic to tell the stunt system the player is dead.
    /// </summary>
    public void SetDead(bool dead)
    {
        _isDead = dead;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (frontTireGroundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(frontTireGroundCheck.position, groundCheckRadius);
        }

        if (backTireGroundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(backTireGroundCheck.position, groundCheckRadius);
        }
    }
#endif
}
