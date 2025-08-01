using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StuntCarController : MonoBehaviour
{
    // === CAR PHYSICS SETTINGS ===
    [Header("Car Physics")]
    [Tooltip("The Rigidbody2D for the front tire.")]
    [SerializeField] private Rigidbody2D _frontTireRB;
    [Tooltip("The Rigidbody2D for the back tire.")]
    [SerializeField] private Rigidbody2D _backTireRB;
    [Tooltip("The main Rigidbody2D for the car's chassis.")]
    [SerializeField] private Rigidbody2D _carRB;
    [Tooltip("How fast the tires spin to drive the car.")]
    [SerializeField] private float _speed = 150f;
    [Tooltip("How fast the car's chassis rotates in the air.")]
    [SerializeField] private float _rotationSpeed = 300f;
    [Tooltip("Multiplier for rotation speed while airborne.")]
    [SerializeField] private float _airRotationMultiplier = 2f;
    [Tooltip("The LayerMask used to detect what is considered 'ground'.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("The Transform used to check if the car is on the ground.")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("The radius of the ground check circle.")]
    [SerializeField] private float groundCheckRadius = 0.1f;

    // === STUNT SYSTEM SETTINGS ===
    [Header("Stunt Logic")]
    [Tooltip("The maximum angle (in degrees) from vertical for a 'Clean Landing'.")]
    [SerializeField] private float _cleanLandingThreshold = 20f;
    [Tooltip("The maximum angle (in degrees) from vertical for a 'Perfect Landing'.")]
    [SerializeField] private float _perfectLandingThreshold = 5f;
    [Tooltip("Minimum time airborne to be considered for a stunt.")]
    [SerializeField] private float _airTimeForStunt = 0.5f;
    [Tooltip("The time (in seconds) the player must survive after landing to be awarded points.")]
    [SerializeField] private float _landingGracePeriod = 0.75f;

    // === POINT VALUES ===
    [Header("Point Values")]
    [SerializeField] private int _frontflipPoints = 250;
    [SerializeField] private int _backflipPoints = 250;
    [SerializeField] private int _airTimePointsPerSecond = 50;
    [SerializeField] private int _cleanLandingBonus = 100;
    [SerializeField] private int _perfectLandingBonus = 250;

    // === UI REFERENCES ===
    [Header("UI References")]
    [Tooltip("The permanent TextMeshProUGUI object that displays the total score.")]
    [SerializeField] private TextMeshProUGUI _scoreDisplayText;
    [Tooltip("The prefab for the pop-up text that announces stunts. It should contain a TextMeshProUGUI component.")]
    [SerializeField] private GameObject _stuntTextPrefab;
    [Tooltip("The Canvas object that will be the parent for all pop-up stunt text.")]
    public Transform _stuntDisplayParent;

    // Internal State
    private float _moveInput;
    private bool _isGrounded;
    private float _airTime;
    private float _currentRotationSinceJump;
    private float _previousAngle;
    private int _flipsCompleted;
    private bool _isTrackingStunt;
    private bool _hasCrashed;
    
    // Stunt Pop-up Management
    private Coroutine _scoreAnimationCoroutine;
    private Coroutine _stuntCompletionCoroutine;
    
    // Stunt Points
    private int _pendingStuntPoints;
    
    // Score management
    private static int _score;
    public static int Score
    {
        get => _score;
        private set => _score = value;
    }

    private void Awake()
    {
        Score = 0;
        if (_scoreDisplayText != null)
        {
            _scoreDisplayText.text = "Score: 0";
        }
        else
        {
            Debug.LogWarning("Score Display Text is not assigned. Score will not be visible.");
        }
    }

    private void Update()
    {
        _moveInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        bool wasGrounded = _isGrounded;
        
        RaycastHit2D hit = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.down, 0.1f, groundLayer);
        _isGrounded = hit.collider != null;

        float rotation = _rotationSpeed;
        if (!_isGrounded)
        {
            rotation *= _airRotationMultiplier;
        }
        _frontTireRB.AddTorque(-_moveInput * _speed * Time.fixedDeltaTime);
        _backTireRB.AddTorque(-_moveInput * _speed * Time.fixedDeltaTime);
        _carRB.AddTorque(_moveInput * rotation * Time.fixedDeltaTime);

        // === STUNT LOGIC ===
        if (!_isGrounded && wasGrounded)
        {
            _isTrackingStunt = true;
            _airTime = 0f;
            _flipsCompleted = 0;
            _currentRotationSinceJump = 0f;
            _pendingStuntPoints = 0;
            _hasCrashed = false;
            _previousAngle = _carRB.transform.eulerAngles.z;
        }

        if (!_isGrounded && _isTrackingStunt)
        {
            _airTime += Time.fixedDeltaTime;

            float currentAngle = _carRB.transform.eulerAngles.z;
            float angleChange = currentAngle - _previousAngle;
            
            if (angleChange > 180f) angleChange -= 360f;
            if (angleChange < -180f) angleChange += 360f;

            _currentRotationSinceJump += angleChange;
            _previousAngle = currentAngle;

            int newFlips = Mathf.FloorToInt(Mathf.Abs(_currentRotationSinceJump) / 360f);

            if (newFlips > _flipsCompleted)
            {
                _flipsCompleted = newFlips;
                
                string trickName;
                int trickPoints;
                
                if (_currentRotationSinceJump > 0)
                {
                    trickName = "Backflip";
                    trickPoints = _backflipPoints;
                }
                else
                {
                    trickName = "Frontflip";
                    trickPoints = _frontflipPoints;
                }
                
                _pendingStuntPoints += trickPoints;
                DisplayStuntFeedback($"{trickName} ({_flipsCompleted}x)", trickPoints);
            }
        }
        
        if (_isGrounded && !wasGrounded && _isTrackingStunt)
        {
            _isTrackingStunt = false;

            if (_airTime >= _airTimeForStunt)
            {
                // Calculate all landing points and add to the pending total
                int airTimeScore = Mathf.FloorToInt(_airTime) * _airTimePointsPerSecond;
                if (airTimeScore > 0)
                {
                    _pendingStuntPoints += airTimeScore;
                    DisplayStuntFeedback("Air Time!", airTimeScore);
                }

                float landingAngle = Vector2.Angle(transform.up, hit.normal);
                if (landingAngle <= _perfectLandingThreshold)
                {
                    _pendingStuntPoints += _perfectLandingBonus;
                    DisplayStuntFeedback("PERFECT LANDING!", _perfectLandingBonus);
                }
                else if (landingAngle <= _cleanLandingThreshold)
                {
                    _pendingStuntPoints += _cleanLandingBonus;
                    DisplayStuntFeedback("Clean Landing!", _cleanLandingBonus);
                }

                // Start the grace period timer to award all pending points
                if (_stuntCompletionCoroutine != null) StopCoroutine(_stuntCompletionCoroutine);
                _stuntCompletionCoroutine = StartCoroutine(AwardStuntPointsAfterLanding());
            }
            else
            {
                // If the jump was too short, cancel any pending points.
                _pendingStuntPoints = 0;
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") && _isTrackingStunt)
        {
            _hasCrashed = true;
            _isTrackingStunt = false;
            
            if (_stuntCompletionCoroutine != null)
            {
                StopCoroutine(_stuntCompletionCoroutine);
            }
            _pendingStuntPoints = 0;
        }
    }

    private IEnumerator AwardStuntPointsAfterLanding()
    {
        yield return new WaitForSeconds(_landingGracePeriod);

        if (_pendingStuntPoints > 0 && !_hasCrashed)
        {
            AddScore(_pendingStuntPoints);
            DisplayStuntFeedback("Stunt Combo!", _pendingStuntPoints);
        }
        
        _pendingStuntPoints = 0;
        _stuntCompletionCoroutine = null;
    }

    private void AddScore(int pointsToAdd)
    {
        int oldScore = Score;
        Score += pointsToAdd;

        if (_scoreAnimationCoroutine != null)
        {
            StopCoroutine(_scoreAnimationCoroutine);
        }
        _scoreAnimationCoroutine = StartCoroutine(AnimateScoreText(oldScore, Score));
    }

    private IEnumerator AnimateScoreText(int oldScore, int newScore)
    {
        float duration = 0.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            int currentScore = (int)Mathf.Lerp(oldScore, newScore, progress);
            _scoreDisplayText.text = $"Score: {currentScore}";
            yield return null;
        }

        _scoreDisplayText.text = $"Score: {newScore}";
        _scoreAnimationCoroutine = null;
    }

    private void DisplayStuntFeedback(string stuntName, int points)
    {
        if (_stuntTextPrefab == null || _stuntDisplayParent == null)
        {
            Debug.LogWarning("Stunt Text Prefab or Display Parent not set. Cannot display feedback.");
            return;
        }

        GameObject textObject = Instantiate(_stuntTextPrefab, _stuntDisplayParent);
        textObject.SetActive(true);
        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
        
        if (textMesh != null)
        {
            float randomRotation = Random.Range(-30f, 30f);
            textObject.transform.rotation = Quaternion.Euler(0, 0, randomRotation);
            
            float randomX = Random.Range(Screen.width * 0.1f, Screen.width * 0.9f);
            float randomY = Random.Range(Screen.height * 0.4f, Screen.height * 0.9f);
            textObject.transform.position = new Vector3(randomX, randomY, 0);

            textObject.transform.localScale = Vector3.zero;
            
            textMesh.text = $"{stuntName}\n+{points}";

            if (stuntName.Contains("PERFECT"))
            {
                textMesh.color = Color.yellow;
            }
            else if (stuntName.Contains("Clean"))
            {
                textMesh.color = Color.green;
            }
            else if (stuntName.Contains("Air Time"))
            {
                textMesh.color = Color.cyan;
            }
            else if (points > 0)
            {
                textMesh.color = Color.white;
            }

            StartCoroutine(AnimateStuntText(textObject, textMesh));
        }
    }
    
    private IEnumerator AnimateStuntText(GameObject textObject, TextMeshProUGUI textMesh)
    {
        if (textObject == null) yield break;

        float popInDuration = 0.25f;
        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 1.1f;
        Vector3 startPos = textObject.transform.position;
        Vector3 endPos = startPos + new Vector3(0, 0.5f, 0);

        while (timer < popInDuration)
        {
            if (textObject == null) yield break;
            timer += Time.deltaTime;
            float t = timer / popInDuration;
            textObject.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            textObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        if (textObject == null) yield break;

        float fadeDuration = 1.5f;
        timer = 0f;
        Color startColor = textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Vector3 fadeStartPos = textObject.transform.position;
        Vector3 fadeEndPos = fadeStartPos + new Vector3(0, 5.0f, 0);

        while (timer < fadeDuration)
        {
            if (textObject == null) yield break;
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            textMesh.color = Color.Lerp(startColor, endColor, t);
            textObject.transform.position = Vector3.Lerp(fadeStartPos, fadeEndPos, t);
            yield return null;
        }
        
        if (textObject != null)
        {
            Destroy(textObject);
        }
    }
    
    private float NormalizeAngle(float angle)
    {
        angle %= 360;
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        return angle;
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