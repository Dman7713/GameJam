using UnityEngine;

public class StuntCarController : MonoBehaviour
{
    [Header("Car Physics")]
    [SerializeField] private Rigidbody2D _frontTireRB;
    [SerializeField] private Rigidbody2D _backTireRB;
    [SerializeField] private Rigidbody2D _carRB;
    [SerializeField] private float _speed = 150f;
    [SerializeField] private float _rotationSpeed = 300f;
    [SerializeField] private float _airRotationMultiplier = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;

    private bool _isGrounded;
    private StuntManager _stuntManager;

    private void Awake()
    {
        _stuntManager = GetComponent<StuntManager>();
        if (_stuntManager == null)
            Debug.LogError("StuntManager component is missing!");
    }

    private void FixedUpdate()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.down, 0.1f, groundLayer);

        // Drive logic
        if (Input.GetKey(KeyCode.UpArrow))
        {
            float torque = _speed * Time.fixedDeltaTime;
            _frontTireRB.AddTorque(-torque);
            _backTireRB.AddTorque(-torque);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            float torque = _speed * Time.fixedDeltaTime;
            _frontTireRB.AddTorque(torque);
            _backTireRB.AddTorque(torque);
        }

        // Air rotation
        if (!_isGrounded)
        {
            float rotationTorque = _rotationSpeed * _airRotationMultiplier * Time.fixedDeltaTime;

            if (Input.GetKey(KeyCode.LeftArrow))
                _carRB.AddTorque(rotationTorque);
            else if (Input.GetKey(KeyCode.RightArrow))
                _carRB.AddTorque(-rotationTorque);
        }

        // Notify StuntManager
        _stuntManager?.HandleStuntTracking(_isGrounded, wasGrounded, _carRB);
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
