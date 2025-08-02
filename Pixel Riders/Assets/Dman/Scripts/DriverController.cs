using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro support

[RequireComponent(typeof(StuntManager))]
public class DriverController : MonoBehaviour
{
    [Header("References")]
    // Main bike components
    [SerializeField] private Rigidbody2D _bikeRigidbody;
    [SerializeField] private Rigidbody2D _frontTireRB;
    [SerializeField] private Rigidbody2D _backTireRB;

    // Camera and UI elements
    [SerializeField] private GameObject dropCamera;
    [SerializeField] private GameObject followCamera;
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private GameObject scoreCanvas;
    [SerializeField] private TextMeshProUGUI _countdownText; // Changed to TextMeshProUGUI for TMPro compatibility

    // Stunt and Camera
    [SerializeField] private StuntManager _stuntManager; // Reference to the StuntManager component

    [Header("Car Physics")]
    [SerializeField] private float _speed = 150f;
    [SerializeField] private float _rotationSpeed = 300f;
    [SerializeField] private float _airRotationMultiplier = 2f;
    [SerializeField] private float uprightFallTorque = 200f;
    [SerializeField] private float dropForce = 1500f; // Added back as a serialized field
    
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
    private bool _hasLanded = false;
    
    private bool _canDrive = false;
    private bool _canTilt = false;
    private bool _isCutsceneActive = true;
    private bool _isDropping = false;

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
        // Initial setup for the cutscene. The bike starts completely still.
        followCamera.SetActive(false);
        scoreCanvas.SetActive(false);
        countdownCanvas.SetActive(false);
        dropCamera.SetActive(true);

        // Turn off physics. The bike will not move until TriggerDrop() is called.
        // All rigidbodies are set to Kinematic
        _bikeRigidbody.bodyType = RigidbodyType2D.Kinematic;
        _frontTireRB.bodyType = RigidbodyType2D.Kinematic;
        _backTireRB.bodyType = RigidbodyType2D.Kinematic;

        // Disable StuntManager at the start
        if (_stuntManager != null)
        {
            _stuntManager.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        // Ground detection logic for stunt tracking runs every frame.
        _frontTireGrounded = Physics2D.OverlapCircle(frontTireGroundCheck.position, groundCheckRadius, groundLayer);
        _backTireGrounded = Physics2D.OverlapCircle(backTireGroundCheck.position, groundCheckRadius, groundLayer);
        _isGrounded = _frontTireGrounded || _backTireGrounded;

        _landedThisFrame = !_wasGroundedLastFrame && _isGrounded;
        _wasGroundedLastFrame = _isGrounded;
        
        // The core physics logic is managed here based on the current state.
        if (_isCutsceneActive)
        {
            // Check for landing to end the cutscene.
            HandleCutsceneLanding();
            
            // During the drop, keep the bike upright regardless of player input.
            if (_isDropping)
            {
                // Apply a strong torque to force the bike to stay upright.
                float angleDiff = Vector2.SignedAngle(transform.up, Vector2.up);
                _bikeRigidbody.AddTorque(angleDiff * uprightFallTorque * Time.fixedDeltaTime);
            }
        }
        else // Post-cutscene gameplay
        {
            if (_canDrive)
            {
                HandleMovement();
            }

            if (_canTilt)
            {
                HandleAirRotation();
            }
        }
        
        // StuntManager logic from your original script
        _stuntManager?.HandleStuntTracking(
            _isGrounded,
            _isDead,
            _bikeRigidbody,
            _frontTireGrounded,
            _backTireGrounded,
            _landedThisFrame
        );
    }
    
    // Movement logic from your original script
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

    // Air rotation logic from your original script
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

    // This method is called to initiate the bike dropping from the plane.
    public void TriggerDrop()
    {
        if (_isDropping) return;
        Debug.Log("TriggerDrop() called. Dropping bike.");

        // Enable physics on all bike components
        // All rigidbodies are set to Dynamic
        _bikeRigidbody.bodyType = RigidbodyType2D.Dynamic;
        _frontTireRB.bodyType = RigidbodyType2D.Dynamic;
        _backTireRB.bodyType = RigidbodyType2D.Dynamic;

        // Ensure the bike starts with zero rotation
        _bikeRigidbody.angularVelocity = 0f;

        // Apply an initial force to push the bike out of the plane
        _bikeRigidbody.AddForce(transform.right * dropForce);
        Debug.Log("Applied drop force. Bike velocity: " + _bikeRigidbody.linearVelocity);
        
        _isDropping = true;
        _isCutsceneActive = true;
    }
    
    // Renamed the method to clarify it's for the cutscene landing.
    private void HandleCutsceneLanding()
    {
        // This ground check is used specifically to end the cutscene.
        bool newIsGrounded = Physics2D.OverlapCircle(frontTireGroundCheck.position, groundCheckRadius, groundLayer) ||
                             Physics2D.OverlapCircle(backTireGroundCheck.position, groundCheckRadius, groundLayer);
        
        if (!_hasLanded && newIsGrounded)
        {
            _hasLanded = true;
            _isGrounded = true;
            Debug.Log("Bike has landed. Ending cutscene.");

            // End the cutscene and start the game
            StartCoroutine(EndCutsceneAndStartGame());
        }
    }

    private IEnumerator EndCutsceneAndStartGame()
    {
        _isCutsceneActive = false;

        // Immediately stop all velocity upon landing for a cleaner look
        _bikeRigidbody.linearVelocity = Vector2.zero;
        _bikeRigidbody.angularVelocity = 0f;
        
        // Freeze the bike's physics completely for the countdown
        // All rigidbodies are set to Kinematic
        _bikeRigidbody.bodyType = RigidbodyType2D.Kinematic;
        _frontTireRB.bodyType = RigidbodyType2D.Kinematic;
        _backTireRB.bodyType = RigidbodyType2D.Kinematic;

        // Force the bike to be perfectly upright.
        _bikeRigidbody.AddTorque(Vector2.SignedAngle(transform.up, Vector2.up) * 100f);
        
        // Switch cameras
        dropCamera.SetActive(false);
        followCamera.SetActive(true);
        
        // Wait 0.5 seconds before starting the countdown
        yield return new WaitForSeconds(0.5f);

        // Start countdown
        countdownCanvas.SetActive(true);
        _countdownText.canvasRenderer.SetAlpha(0f); // Ensure the text is invisible before the first number
        yield return ShowCountdownText("3");
        yield return ShowCountdownText("2");
        yield return ShowCountdownText("1");
        yield return ShowCountdownText("GO!");

        // Re-enable physics and transition to player control
        // All rigidbodies are set to Dynamic
        _bikeRigidbody.bodyType = RigidbodyType2D.Dynamic;
        _frontTireRB.bodyType = RigidbodyType2D.Dynamic;
        _backTireRB.bodyType = RigidbodyType2D.Dynamic;
        
        _canDrive = true;
        _canTilt = true;
        
        // Enable StuntManager when the game starts
        if (_stuntManager != null)
        {
            _stuntManager.enabled = true;
        }
        
        countdownCanvas.SetActive(false);
        scoreCanvas.SetActive(true);
    }
    
    private IEnumerator ShowCountdownText(string text)
    {
        // Set the text, fade it in, wait, and fade it out
        _countdownText.text = text;
        _countdownText.CrossFadeAlpha(1f, 0.2f, false);
        yield return new WaitForSeconds(0.6f);
        _countdownText.CrossFadeAlpha(0f, 0.2f, false);
        yield return new WaitForSeconds(0.4f);
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
