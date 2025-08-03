using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(StuntManager))]
public class DriverController : MonoBehaviour
{
    [Header("References")]
    // Main bike components
    [SerializeField] private Rigidbody2D _bikeRigidbody;
    [SerializeField] private Rigidbody2D _frontTireRB;
    [SerializeField] private Rigidbody2D _backTireRB;
    
    // UI elements
    [SerializeField] private GameObject scoreCanvas;

    // Stunt manager
    [SerializeField] private StuntManager _stuntManager;

    [Header("Car Physics")]
    [SerializeField] private float _speed = 150f;
    [SerializeField] private float _rotationSpeed = 300f;
    [SerializeField] private float _airRotationMultiplier = 2f;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform frontTireGroundCheck;
    [SerializeField] private Transform backTireGroundCheck;
    [SerializeField] [Range(0.01f, 0.5f)] private float groundCheckRadius = 0.1f;

    // State variables
    private bool _isGrounded;
    private bool _wasGroundedLastFrame;
    private bool _landedThisFrame;
    private bool _isDead;
    private bool _frontTireGrounded;
    private bool _backTireGrounded;
    
    private void Awake()
    {
        _stuntManager = GetComponent<StuntManager>();
        if (_stuntManager == null)
        {
            Debug.LogError("StuntManager component not found on DriverController GameObject.");
        }
    }

    private void Start()
    {
        // Ensure UI is active and physics is enabled from the start.
        scoreCanvas.SetActive(true);

        // Turn on physics immediately.
        _bikeRigidbody.bodyType = RigidbodyType2D.Dynamic;
        _frontTireRB.bodyType = RigidbodyType2D.Dynamic;
        _backTireRB.bodyType = RigidbodyType2D.Dynamic;

        // **FIXES:**
        // To prevent high-speed tunneling, we set collision detection to continuous.
        // This is especially important for fast-moving objects like the bike and its wheels.
        _bikeRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _frontTireRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _backTireRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // We also set interpolation to help smooth out visual stuttering.
        _bikeRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _frontTireRB.interpolation = RigidbodyInterpolation2D.Interpolate;
        _backTireRB.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Enable the StuntManager from the beginning.
        if (_stuntManager != null)
        {
            _stuntManager.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        // Ground detection logic for stunt tracking.
        _frontTireGrounded = Physics2D.OverlapCircle(frontTireGroundCheck.position, groundCheckRadius, groundLayer);
        _backTireGrounded = Physics2D.OverlapCircle(backTireGroundCheck.position, groundCheckRadius, groundLayer);
        _isGrounded = _frontTireGrounded || _backTireGrounded;

        _landedThisFrame = !_wasGroundedLastFrame && _isGrounded;
        _wasGroundedLastFrame = _isGrounded;

        // The core driving physics logic.
        HandleMovement();
        HandleAirRotation();
        
        // StuntManager logic.
        _stuntManager?.HandleStuntTracking(
            _isGrounded,
            _isDead,
            _bikeRigidbody,
            _frontTireGrounded,
            _backTireGrounded,
            _landedThisFrame
        );
    }
    
    // Movement logic.
    private void HandleMovement()
    {
        float torque = _speed * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            _frontTireRB.AddTorque(-torque);
            _backTireRB.AddTorque(-torque);
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            _frontTireRB.AddTorque(torque);
            _backTireRB.AddTorque(torque);
        }
    }

    // Air rotation logic.
    private void HandleAirRotation()
    {
        if (!_isGrounded)
        {
            float rotationTorque = _rotationSpeed * _airRotationMultiplier * Time.fixedDeltaTime;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                _bikeRigidbody.AddTorque(rotationTorque);
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                _bikeRigidbody.AddTorque(-rotationTorque);
            }
        }
    }

    // Call this method from your death logic to tell the stunt system the player is dead.
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