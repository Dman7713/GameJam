using UnityEngine;
using System;
using System.Linq;

[RequireComponent(typeof(StuntManager))]
public class DriverControllerV2 : MonoBehaviour
{
    // --- Events for Stunt Manager ---
    public static event Action OnLanded;
    public static event Action OnTakeOff;
    public static event Action OnDeath;

    [Header("References")]
    [SerializeField] private Rigidbody2D _bikeRigidbody;
    [SerializeField] private WheelJoint2D _frontWheelJoint;
    [SerializeField] private WheelJoint2D _backWheelJoint;
    
    [Header("PC Physics Settings")]
    [Tooltip("Target motor speed for forward movement on PC.")]
    [SerializeField] private float _motorSpeed = 1000f;
    [Tooltip("Maximum motor torque for PC. Adjust this to control acceleration.")]
    [SerializeField] private float _motorTorque = 1000f;
    [Tooltip("Torque applied to the bike body for in-air rotation on PC.")]
    [SerializeField] private float _airRotationTorque = 150f;
    [Tooltip("Damping applied to air rotation on PC.")]
    [SerializeField] private float _airRotationDamping = 0.5f;
    
    [Header("Mobile Physics Settings")]
    [Tooltip("Target motor speed for forward movement on mobile.")]
    [SerializeField] private float _mobileMotorSpeed = 1000f;
    [Tooltip("Maximum motor torque for mobile. Adjust this to control acceleration.")]
    [SerializeField] private float _mobileMotorTorque = 1000f;
    [Tooltip("Torque applied to the bike body for in-air rotation on mobile.")]
    [SerializeField] private float _mobileAirRotationTorque = 150f;
    [Tooltip("Damping applied to air rotation on mobile.")]
    [SerializeField] private float _mobileAirRotationDamping = 0.5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Transform _frontGroundCheck;
    [SerializeField] private Transform _backGroundCheck;
    [SerializeField] [Range(0.01f, 0.5f)] private float _groundCheckRadius = 0.1f;

    // Internal state
    private JointMotor2D _motor;
    private bool _isDead;
    private bool _isGrounded;
    private bool _wasGroundedLastFrame;

    // Public properties for easy access
    public bool IsDead { get { return _isDead; } }
    public bool IsGrounded => _isGrounded;
    public bool FrontWheelGrounded => Physics2D.OverlapCircle(_frontGroundCheck.position, _groundCheckRadius, _groundLayer);
    public bool BackWheelGrounded => Physics2D.OverlapCircle(_backGroundCheck.position, _groundCheckRadius, _groundLayer);

    private void Awake()
    {
        _motor = new JointMotor2D { maxMotorTorque = _motorTorque };
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        UpdateGroundState();

        if (_wasGroundedLastFrame && !_isGrounded)
        {
            OnTakeOff?.Invoke();
        }
        else if (!_wasGroundedLastFrame && _isGrounded)
            {
            OnLanded?.Invoke();
        }
        
        HandleMovement();
        HandleAirRotation();
    }

    private void UpdateGroundState()
    {
        _wasGroundedLastFrame = _isGrounded;
        _isGrounded = FrontWheelGrounded || BackWheelGrounded;
    }

    private void HandleMovement()
    {
        float driveInput = 0f;
        float currentMotorTorque = _motorTorque;
        float currentMotorSpeed = _motorSpeed;

        // --- Mobile Input ---
        if (MobileInputManager.DriveInput != 0f)
        {
            driveInput = MobileInputManager.DriveInput;
            currentMotorTorque = _mobileMotorTorque;
            currentMotorSpeed = _mobileMotorSpeed;
        }
        // --- Keyboard Input ---
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            driveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            driveInput = -1f;
        }

        // Apply motor speed to wheels if on the ground
        if (_isGrounded)
        {
            // Re-apply the high motor speed to both wheels
            _motor.motorSpeed = -driveInput * currentMotorSpeed;
            _motor.maxMotorTorque = currentMotorTorque;

            // Create a new motor for the front wheel with a fraction of the torque
            // This allows it to stabilize the bike without flipping it.
            JointMotor2D frontMotor = new JointMotor2D
            {
                maxMotorTorque = currentMotorTorque * 0.2f,
                motorSpeed = -driveInput * currentMotorSpeed
            };

            _backWheelJoint.motor = _motor;
            _frontWheelJoint.motor = frontMotor;
        }
        else
        {
            _frontWheelJoint.motor = new JointMotor2D();
            _backWheelJoint.motor = new JointMotor2D();
        }
    }

    private void HandleAirRotation()
    {
        if (_isGrounded) return;
        
        float rotationInput = 0f;
        float currentAirRotationTorque = _airRotationTorque;
        float currentAirRotationDamping = _airRotationDamping;
        
        // --- Mobile Joystick Input ---
        // Overrides keyboard input if the joystick is being used
        if (MobileInputManager.RotationJoystickInput != 0f)
        {
            rotationInput = -MobileInputManager.RotationJoystickInput;
            currentAirRotationTorque = _mobileAirRotationTorque;
            currentAirRotationDamping = _mobileAirRotationDamping;
        }
        // --- Keyboard Input ---
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            rotationInput = 1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            rotationInput = -1f;
        }

        if (rotationInput != 0)
        {
            _bikeRigidbody.AddTorque(rotationInput * currentAirRotationTorque * Time.fixedDeltaTime);
        }
        else
        {
            // Apply damping when there is no active rotation input
            _bikeRigidbody.angularVelocity *= (1f - currentAirRotationDamping);
        }
    }

    public void HandleDeath()
    {
        if (_isDead) return;

        _isDead = true;
        _frontWheelJoint.motor = new JointMotor2D();
        _backWheelJoint.motor = new JointMotor2D();
        _bikeRigidbody.angularVelocity = 0;
        
        OnDeath?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (_frontGroundCheck != null)
            Gizmos.DrawWireSphere(_frontGroundCheck.position, _groundCheckRadius);
        
        Gizmos.color = Color.cyan;
        if (_backGroundCheck != null)
            Gizmos.DrawWireSphere(_backGroundCheck.position, _groundCheckRadius);
    }
#endif
}
