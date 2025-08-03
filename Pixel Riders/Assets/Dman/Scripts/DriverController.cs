using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(StuntManager), typeof(AudioSource))]
public class DriverController : MonoBehaviour
{
    // A nested class to handle collision detection specifically for the head object.
    // This allows us to detect collisions on a child object and relay the information
    // to the main DriverController script.
    private class HeadCollisionDetector : MonoBehaviour
    {
        public DriverController driverController;

        private void OnCollisionEnter2D(Collision2D other)
        {
            // Only trigger the death audio if the collision is with the "Ground"
            if (other.gameObject.CompareTag("Ground"))
            {
                driverController.HandleDeath();
            }
        }
    }

    [Header("References")]
    // Main bike components
    [SerializeField] private Rigidbody2D _bikeRigidbody;
    [SerializeField] private Rigidbody2D _frontTireRB;
    [SerializeField] private Rigidbody2D _backTireRB;

    // --- NEW ADDITIONS FOR SPRITE SYSTEM ---
    [SerializeField] private SpriteRenderer _bikeBodyRenderer;
    [SerializeField] private List<BikeBodySpriteSO> _allAvailableSprites;
    // --- END NEW ADDITIONS ---
    
    // UI elements
    [SerializeField] private GameObject scoreCanvas;

    // Stunt manager
    [SerializeField] private StuntManager _stuntManager;

    // Audio components
    [SerializeField] private AudioSource _audioSource;
    private AudioSource _audioSourceGroundHit;

    // The player's head transform, used to detect fatal collisions
    [SerializeField] private Transform _headTransform;

    [Header("Car Physics")]
    [SerializeField] private float _speed = 150f;
    [SerializeField] private float _rotationSpeed = 300f;
    [SerializeField] private float _airRotationMultiplier = 2f;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform frontTireGroundCheck;
    [SerializeField] private Transform backTireGroundCheck;
    [SerializeField] [Range(0.01f, 0.5f)] private float groundCheckRadius = 0.1f;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _idleAudioClip;
    [SerializeField] private AudioClip _driveAudioClip;
    [SerializeField] private AudioClip _reverseAudioClip;
    [SerializeField] private AudioClip _groundHitAudioClip;
    [SerializeField] private AudioClip _deathAudioClip;
    

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
        
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogError("AudioSource component not found on DriverController GameObject. Please add one.");
        }

        _audioSourceGroundHit = gameObject.AddComponent<AudioSource>();
        
        if (_headTransform != null)
        {
            HeadCollisionDetector detector = _headTransform.gameObject.AddComponent<HeadCollisionDetector>();
            detector.driverController = this;
        }
    }

    private void Start()
    {
        // --- NEW LINE: APPLY THE EQUIPPED SPRITE ON START ---
        ShopManager.ApplyEquippedSprite(_bikeBodyRenderer, _allAvailableSprites);
        // --- END NEW LINE ---

        scoreCanvas.SetActive(true);

        _bikeRigidbody.bodyType = RigidbodyType2D.Dynamic;
        _frontTireRB.bodyType = RigidbodyType2D.Dynamic;
        _backTireRB.bodyType = RigidbodyType2D.Dynamic;

        _bikeRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _frontTireRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _backTireRB.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        _bikeRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _frontTireRB.interpolation = RigidbodyInterpolation2D.Interpolate;
        _backTireRB.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        if (_stuntManager != null)
        {
            _stuntManager.enabled = true;
        }

        if (_audioSource != null && _idleAudioClip != null)
        {
            _audioSource.clip = _idleAudioClip;
            _audioSource.loop = true;
            _audioSource.Play();
        }
    }

    private void FixedUpdate()
    {
        _frontTireGrounded = Physics2D.OverlapCircle(frontTireGroundCheck.position, groundCheckRadius, groundLayer);
        _backTireGrounded = Physics2D.OverlapCircle(backTireGroundCheck.position, groundCheckRadius, groundLayer);
        _isGrounded = _frontTireGrounded || _backTireGrounded;

        _landedThisFrame = !_wasGroundedLastFrame && _isGrounded;
        _wasGroundedLastFrame = _isGrounded;

        if (!_isDead)
        {
            HandleMovement();
            HandleAirRotation();
        }
        
        _stuntManager?.HandleStuntTracking(
            _isGrounded,
            _isDead,
            _bikeRigidbody,
            _frontTireGrounded,
            _backTireGrounded,
            _landedThisFrame
        );
    }
    
    private void HandleMovement()
    {
        float torque = _speed * Time.fixedDeltaTime;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            _frontTireRB.AddTorque(-torque);
            _backTireRB.AddTorque(-torque);
            
            if (_audioSource.clip != _driveAudioClip)
            {
                PlayAudioClip(_driveAudioClip, true);
            }
        }
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            _frontTireRB.AddTorque(torque);
            _backTireRB.AddTorque(torque);

            if (_audioSource.clip != _reverseAudioClip)
            {
                PlayAudioClip(_reverseAudioClip, true);
            }
        }
        else
        {
            if (_audioSource.clip != _idleAudioClip)
            {
                PlayAudioClip(_idleAudioClip, true);
            }
        }
    }

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

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground") && other.gameObject != _headTransform.gameObject && !_isDead)
        {
            if (_groundHitAudioClip != null)
            {
                _audioSourceGroundHit.PlayOneShot(_groundHitAudioClip);
            }
        }
    }
    
    public void HandleDeath()
    {
        if (_isDead) return;

        _isDead = true;

        _audioSource.Stop();
        _audioSourceGroundHit.Stop();

        if (_deathAudioClip != null)
        {
            _audioSourceGroundHit.PlayOneShot(_deathAudioClip);
        }

        Debug.Log("Player is dead!");
    }

    public void SetDead(bool dead)
    {
        _isDead = dead;
    }

    private void PlayAudioClip(AudioClip clip, bool loop)
    {
        if (_audioSource != null && clip != null && !_isDead)
        {
            _audioSource.clip = clip;
            _audioSource.loop = loop;
            _audioSource.Play();
        }
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