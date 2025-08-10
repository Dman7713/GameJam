using UnityEngine;

using System;



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

   

    [Header("Physics Settings")]

    [Tooltip("Target motor speed for forward movement.")]

    [SerializeField] private float _motorSpeed = 1000f;

    [Tooltip("Maximum motor torque. Adjust this to control acceleration.")]

    [SerializeField] private float _motorTorque = 1000f;

    [Tooltip("Torque applied to the bike body for in-air rotation.")]

    [SerializeField] private float _airRotationTorque = 150f;



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



    public bool IsDead {

        get { return _isDead; }

    }



    // Public properties for easy access

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



        // Check for take-off and landing events

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

        _isGrounded = FrontWheelGrounded || BackWheelGrounded;

        _wasGroundedLastFrame = _isGrounded;

    }



    private void HandleMovement()

    {

        // Get input for forward/backward movement

        float driveInput = 0f;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))

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

            _motor.motorSpeed = -driveInput * _motorSpeed;

            _frontWheelJoint.motor = _motor;

            _backWheelJoint.motor = _motor;

        }

        else

        {

            // Stop the motor when in the air to prevent unwanted spinning

            _frontWheelJoint.motor = new JointMotor2D();

            _backWheelJoint.motor = new JointMotor2D();

        }

    }



    private void HandleAirRotation()

    {

        if (_isGrounded) return;

       

        float rotationInput = 0f;

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))

        {

            rotationInput = 1f;

        }

        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))

        {

            rotationInput = -1f;

        }

       

        if (rotationInput != 0)

        {

            _bikeRigidbody.AddTorque(rotationInput * _airRotationTorque * Time.fixedDeltaTime);

        }

    }



    // Public method for death handling

    public void HandleDeath()

    {

        if (_isDead) return;



        _isDead = true;

        Debug.Log(IsDead);

        // Stop all movement

        _frontWheelJoint.motor = new JointMotor2D();

        _backWheelJoint.motor = new JointMotor2D();

        _bikeRigidbody.angularVelocity = 0;

       

        OnDeath?.Invoke();

        Debug.Log("Player is dead!");

    }



    // --- Visualization for Ground Check ---

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