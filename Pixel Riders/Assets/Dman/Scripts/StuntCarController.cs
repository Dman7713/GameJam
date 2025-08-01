using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("Stunt Logic")]
    [SerializeField] private float _cleanLandingThreshold = 20f;
    [SerializeField] private float _perfectLandingThreshold = 5f;
    [SerializeField] private float _airTimeForStunt = 0.5f;
    [SerializeField] private float _landingGracePeriod = 0.75f;
    [SerializeField] private int _stuntBonusPoints = 25;

    [Header("Point Values")]
    [SerializeField] private int _frontflipPoints = 250;
    [SerializeField] private int _backflipPoints = 250;
    [SerializeField] private int _airTimePointsPerSecond = 50;
    [SerializeField] private int _cleanLandingBonus = 100;
    [SerializeField] private int _perfectLandingBonus = 250;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _scoreDisplayText;
    [SerializeField] private GameObject _stuntTextPrefab;
    public Transform _stuntDisplayParent;

    private bool _isGrounded;
    private float _airTime;
    private float _rotationOnJump;
    private int _flipsCompleted;
    private bool _isTrackingStunt;
    private bool _hasCrashed;
    private bool _isTrackingLanding;
    private int _flipsInComboCount;

    private Coroutine _scoreAnimationCoroutine;
    private Coroutine _stuntCompletionCoroutine;

    private int _pendingFlipPoints;
    private int _pendingAirTimePoints;
    private int _pendingLandingBonusPoints;

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
            _scoreDisplayText.text = "Score: 0";
    }

    private void Update() { }

    private void FixedUpdate()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, Vector2.down, 0.1f, groundLayer);

        // === Drive ===
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

        // === Rotate in air ===
        if (!_isGrounded)
        {
            float rotationTorque = _rotationSpeed * _airRotationMultiplier * Time.fixedDeltaTime;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _carRB.AddTorque(rotationTorque); // Left = counterclockwise
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                _carRB.AddTorque(-rotationTorque); // Right = clockwise
            }
        }

        HandleStuntTracking(wasGrounded);
    }

    private void HandleStuntTracking(bool wasGrounded)
    {
        if (!_isGrounded && wasGrounded && !_isTrackingLanding)
        {
            _isTrackingStunt = true;
            _airTime = 0f;
            _flipsCompleted = 0;
            _rotationOnJump = _carRB.rotation;
            _flipsInComboCount = 0;
            _pendingFlipPoints = 0;
            _pendingAirTimePoints = 0;
            _pendingLandingBonusPoints = 0;
            _hasCrashed = false;
        }

        if (!_isGrounded && _isTrackingStunt)
        {
            _airTime += Time.fixedDeltaTime;
            float rotSinceJump = _carRB.rotation - _rotationOnJump;
            int newFlips = Mathf.FloorToInt(Mathf.Abs(rotSinceJump) / 360f);

            if (newFlips > _flipsCompleted)
            {
                int flipsDetected = newFlips - _flipsCompleted;
                _flipsCompleted = newFlips;

                string trickName = rotSinceJump > 0 ? "Backflip" : "Frontflip";
                int trickPoints = rotSinceJump > 0 ? _backflipPoints : _frontflipPoints;

                _pendingFlipPoints += trickPoints * flipsDetected;
                DisplayStuntFeedback($"{trickName} ({_flipsCompleted}x)", trickPoints * flipsDetected);
                _flipsInComboCount += flipsDetected;
            }
        }

        if (_isGrounded && !wasGrounded && _isTrackingStunt)
        {
            _isTrackingStunt = false;
            _isTrackingLanding = true;

            float landingAngle = Vector2.Angle(transform.up, Vector2.up);
            _hasCrashed = landingAngle > _cleanLandingThreshold;

            if (!_hasCrashed && _airTime >= _airTimeForStunt)
            {
                _pendingAirTimePoints = Mathf.FloorToInt(_airTime) * _airTimePointsPerSecond;
                if (_pendingAirTimePoints > 0)
                    DisplayStuntFeedback("Air Time!", _pendingAirTimePoints);

                if (landingAngle <= _perfectLandingThreshold)
                {
                    _pendingLandingBonusPoints = _perfectLandingBonus;
                    DisplayStuntFeedback("PERFECT LANDING!", _perfectLandingBonus);
                }
                else if (landingAngle <= _cleanLandingThreshold)
                {
                    _pendingLandingBonusPoints = _cleanLandingBonus;
                    DisplayStuntFeedback("Clean Landing!", _cleanLandingBonus);
                }
            }
            else
            {
                _pendingFlipPoints = 0;
                _pendingAirTimePoints = 0;
                _pendingLandingBonusPoints = 0;
            }

            if (_stuntCompletionCoroutine != null) StopCoroutine(_stuntCompletionCoroutine);
            _stuntCompletionCoroutine = StartCoroutine(HandleSuccessfulLanding());
        }
    }

    private IEnumerator HandleSuccessfulLanding()
    {
        yield return new WaitForSeconds(_landingGracePeriod);
        _isTrackingLanding = false;

        if (!_hasCrashed)
        {
            AddScore(_pendingFlipPoints);
            AddScore(_pendingAirTimePoints);
            AddScore(_pendingLandingBonusPoints);

            if (_flipsInComboCount > 1)
            {
                int comboPoints = _flipsInComboCount * _stuntBonusPoints;
                AddScore(comboPoints);
                DisplayStuntFeedback("Stunt Combo!", comboPoints);
            }
        }

        _stuntCompletionCoroutine = null;
    }

    private void AddScore(int points)
    {
        if (points <= 0) return;
        int oldScore = Score;
        Score += points;
        if (_scoreAnimationCoroutine != null) StopCoroutine(_scoreAnimationCoroutine);
        _scoreAnimationCoroutine = StartCoroutine(AnimateScoreText(oldScore, Score));
    }

    private IEnumerator AnimateScoreText(int oldScore, int newScore)
    {
        float duration = 0.5f, timer = 0f;
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
        if (_stuntTextPrefab == null || _stuntDisplayParent == null) return;

        GameObject textObject = Instantiate(_stuntTextPrefab, _stuntDisplayParent);
        textObject.SetActive(true);
        TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();

        if (textMesh != null)
        {
            textObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            textObject.transform.position = new Vector3(Random.Range(Screen.width * 0.1f, Screen.width * 0.9f), Random.Range(Screen.height * 0.4f, Screen.height * 0.9f), 0);
            textObject.transform.localScale = Vector3.zero;
            textMesh.text = $"{stuntName}\n+{points}";

            if (stuntName.Contains("PERFECT")) textMesh.color = Color.yellow;
            else if (stuntName.Contains("Clean")) textMesh.color = Color.green;
            else if (stuntName.Contains("Air Time")) textMesh.color = Color.cyan;
            else if (stuntName.Contains("Combo")) textMesh.color = Color.magenta;
            else textMesh.color = Color.white;

            StartCoroutine(AnimateStuntText(textObject, textMesh));
        }
    }

    private IEnumerator AnimateStuntText(GameObject textObject, TextMeshProUGUI textMesh)
    {
        float popInDuration = 0.25f;
        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 1.1f;
        Vector3 startPos = textObject.transform.position;
        Vector3 endPos = startPos + new Vector3(0, 0.5f, 0);

        while (timer < popInDuration)
        {
            timer += Time.deltaTime;
            float t = timer / popInDuration;
            textObject.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            textObject.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        float fadeDuration = 1.5f;
        timer = 0f;
        Color startColor = textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Vector3 fadeStart = textObject.transform.position;
        Vector3 fadeEnd = fadeStart + new Vector3(0, 5f, 0);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            textMesh.color = Color.Lerp(startColor, endColor, t);
            textObject.transform.position = Vector3.Lerp(fadeStart, fadeEnd, t);
            yield return null;
        }

        Destroy(textObject);
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
