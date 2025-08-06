using UnityEngine;
using TMPro;
using System.Collections;

public class StuntManagerV2 : MonoBehaviour
{
    // A nested enum to define the current stunt state.
    private enum StuntState { Grounded, Airborne }

    [Header("References")]
    [SerializeField] private DriverControllerV2 _driverController;
    [SerializeField] private TextMeshProUGUI _totalScoreText;
    [SerializeField] private GameObject _stuntTextPrefab;
    [SerializeField] private Canvas _uiCanvas;

    [Header("Stunt Settings")]
    [Tooltip("The minimum amount of airtime required to register a stunt.")]
    [SerializeField] private float _minAirtimeForStunts = 0.3f;
    [Tooltip("The time a stunt combo will remain active after the last stunt.")]
    [SerializeField] private float _comboResetDelay = 1.5f;

    [Header("Landing Thresholds")]
    [Tooltip("Max angle from vertical for a landing to be considered 'clean' or 'perfect'.")]
    [SerializeField] private float _landingAngleTolerance = 15f;
    [Tooltip("Min speed for a landing to be considered 'clean'.")]
    [SerializeField] private float _cleanLandingMinSpeed = 20f;
    [Tooltip("Min speed for a landing to be considered 'perfect'.")]
    [SerializeField] private float _perfectLandingMinSpeed = 46f;

    // Internal state variables
    private StuntState _currentState = StuntState.Grounded;
    private float _airtime;
    private float _cumulativeRotation;
    private float _lastRotationZ;
    private int _totalScore;
    private int _currentComboCount;
    private Coroutine _comboTimerCoroutine;

    private void Awake()
    {
        _totalScoreText.text = "Score: 0";
    }

    private void OnEnable()
    {
        // Subscribe to events from the DriverController
        DriverControllerV2.OnTakeOff += HandleTakeOff;
        DriverControllerV2.OnLanded += HandleLanding;
        DriverControllerV2.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        // Unsubscribe from events to prevent memory leaks
        DriverControllerV2.OnTakeOff -= HandleTakeOff;
        DriverControllerV2.OnLanded -= HandleLanding;
        DriverControllerV2.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        // Only track stunts when airborne
        if (_currentState == StuntState.Airborne)
        {
            _airtime += Time.deltaTime;
            
            float currentRotationZ = NormalizeAngle(transform.eulerAngles.z);
            float rotationDelta = Mathf.DeltaAngle(_lastRotationZ, currentRotationZ);
            _cumulativeRotation += rotationDelta;
            _lastRotationZ = currentRotationZ;
        }
    }

    private void HandleTakeOff()
    {
        _currentState = StuntState.Airborne;
        _airtime = 0f;
        _cumulativeRotation = 0f;
        _lastRotationZ = NormalizeAngle(transform.eulerAngles.z);
    }

    private void HandleLanding()
    {
        _currentState = StuntState.Grounded;

        // Check for stunts
        ProcessFlips();
        ProcessLandings();
        ProcessAirtime();

        // Reset stunt values after landing
        _airtime = 0f;
        _cumulativeRotation = 0f;
    }

    private void HandleDeath()
    {
        // Optionally, penalize or reset score on death
        // For now, we'll just stop all stunt-related coroutines.
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
        }
    }

    private void ProcessFlips()
    {
        int flips = Mathf.FloorToInt(Mathf.Abs(_cumulativeRotation) / 360f);

        if (flips > 0)
        {
            // Determine direction of flip and calculate points
            string label = _cumulativeRotation < 0 ? "Frontflip!" : "Backflip!";
            int points = flips * 100;
            
            // Check for perfect landing to apply bonuses
            if (IsPerfectLanding()) {
                label += " (Perfect Landing!)";
                points *= 2; // Double points for perfect landing
            }

            // A successful flip counts as a new combo
            IncrementCombo();

            ShowStuntPopup($"{label} x{flips}", points, new Color(1f, 0.4f, 0f));
            AddScore(points);
        }
    }

    private void ProcessLandings()
    {
        if (IsPerfectLanding())
        {
            IncrementCombo();
            AddScore(100 * _currentComboCount);
            ShowStuntPopup("Perfect Landing!", 100 * _currentComboCount, Color.green);
        }
        else if (IsCleanLanding())
        {
            IncrementCombo();
            AddScore(50 * _currentComboCount);
            ShowStuntPopup("Clean Landing!", 50 * _currentComboCount, new Color(0f, 0.7f, 1f));
        }
        else
        {
            ResetCombo();
        }
    }
    
    private void ProcessAirtime()
    {
        if (_airtime >= _minAirtimeForStunts)
        {
            float roundedAir = Mathf.Round(_airtime * 2f) / 2f;
            int airPoints = Mathf.RoundToInt(roundedAir * 10f);
            ShowStuntPopup($"Airtime! ({roundedAir:F1}s)", airPoints, Color.cyan);
            AddScore(airPoints);
        }
    }

    // A perfect landing requires a very low angle and high speed.
    private bool IsPerfectLanding()
    {
        float landingAngle = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f));
        float landingSpeed = _driverController.GetComponent<Rigidbody2D>().linearVelocity.magnitude;

        return landingAngle <= _landingAngleTolerance && landingSpeed >= _perfectLandingMinSpeed &&
               _driverController.FrontWheelGrounded && _driverController.BackWheelGrounded;
    }

    // A clean landing is less strict, but still requires a decent angle and speed.
    private bool IsCleanLanding()
    {
        float landingAngle = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f));
        float landingSpeed = _driverController.GetComponent<Rigidbody2D>().linearVelocity.magnitude;

        return landingAngle <= _landingAngleTolerance && landingSpeed >= _cleanLandingMinSpeed &&
               _driverController.FrontWheelGrounded && _driverController.BackWheelGrounded;
    }

    private void IncrementCombo()
    {
        _currentComboCount++;
        // Stop the old timer and start a new one
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
        }
        _comboTimerCoroutine = StartCoroutine(ResetComboAfterDelay());
        
        if (_currentComboCount > 1)
        {
            ShowStuntPopup($"Combo! x{_currentComboCount}", 0, new Color32(255, 165, 0, 255));
        }
    }

    private void ResetCombo()
    {
        _currentComboCount = 0;
        if (_comboTimerCoroutine != null)
        {
            StopCoroutine(_comboTimerCoroutine);
        }
    }
    
    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(_comboResetDelay);
        ResetCombo();
    }

    private void AddScore(int points)
    {
        _totalScore += points;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        // You can add a score counting animation here if you like
        _totalScoreText.text = $"Score: {_totalScore}";
    }

    private void ShowStuntPopup(string label, int points, Color color)
    {
        // TODO: Implement the visual popup animation logic here
        // This is where you would instantiate your stuntTextPrefab and animate it.
        // It's recommended to have a separate script for this.
        Debug.Log($"Stunt! {label}, Points: {points}");
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}