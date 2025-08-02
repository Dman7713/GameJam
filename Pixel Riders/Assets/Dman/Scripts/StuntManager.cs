using UnityEngine;
using TMPro;
using System.Collections;

public class StuntManager : MonoBehaviour
{
    [Header("References")]
    public GameObject stuntTextPrefab;
    public Canvas uiCanvas;
    public TextMeshProUGUI totalScoreText;

    [Header("Settings")]
    public float minAirTimeForStunts = 0.3f;
    public float popupScaleDuration = 0.3f;
    public float popupVisibleDuration = 1f;
    public float popupFadeDuration = 1.5f;
    public float maxPopupRotationAngle = 10f;
    public float comboResetDelay = 1.5f;

    [Header("Landing Thresholds")]
    public float cleanLandingMinSpeed = 20f;    // Clean: 20-45
    public float cleanLandingMaxSpeed = 45f;
    public float perfectLandingMinSpeed = 46f;  // Perfect: 46-1000
    public float perfectLandingMaxSpeed = 1000f;
    public float landingAngleTolerance = 15f;   // Allow up to 15° tilt

    [Header("Landing Window")]
    [Tooltip("Max time difference between wheel contacts to count as two-wheel landing.")]
    public float dualWheelGroundWindow = 0.2f;

    private Rigidbody2D bikeRigidbody;
    private float lastRotationZ;
    private float cumulativeRotation;
    private float airtime;

    private int totalScore;
    private Coroutine scoreCountingCoroutine;

    private int currentComboCount;
    private float comboTimer;

    private float lastFrontGroundTime = -Mathf.Infinity;
    private float lastBackGroundTime = -Mathf.Infinity;

    public static int Score { get; private set; }

    void Awake()
    {
        // Enforce thresholds at runtime
        cleanLandingMinSpeed = 20f;
        cleanLandingMaxSpeed = 45f;
        perfectLandingMinSpeed = 46f;
        perfectLandingMaxSpeed = 1000f;
        landingAngleTolerance = 15f;
        dualWheelGroundWindow = 0.2f;

        bikeRigidbody = GetComponent<Rigidbody2D>();
        lastRotationZ = NormalizeAngle(transform.eulerAngles.z);
        UpdateScoreUIInstant();
    }

    void Update()
    {
        bool grounded = bikeRigidbody.IsTouchingLayers(); // Replace with proper ground check
        float currentZ = NormalizeAngle(transform.eulerAngles.z);
        float delta = Mathf.DeltaAngle(lastRotationZ, currentZ);
        if (!grounded)
        {
            airtime += Time.deltaTime;
            cumulativeRotation += delta;
        }
        lastRotationZ = currentZ;

        if (currentComboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                currentComboCount = 0;
        }
    }

    public void HandleStuntTracking(bool grounded, bool playerDead, Rigidbody2D rb, bool frontGrounded, bool backGrounded, bool landedThisFrame)
    {
        if (frontGrounded) lastFrontGroundTime = Time.time;
        if (backGrounded) lastBackGroundTime = Time.time;

        if (grounded && landedThisFrame && !playerDead)
        {
            int flips = Mathf.FloorToInt(Mathf.Abs(cumulativeRotation) / 360f);
            float angle = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f));
            float speed = rb.linearVelocity.magnitude;
            bool twoWheels = (Time.time - lastFrontGroundTime <= dualWheelGroundWindow) &&
                             (Time.time - lastBackGroundTime <= dualWheelGroundWindow);
            bool oneWheel = frontGrounded || backGrounded;

            bool clean = twoWheels && speed >= cleanLandingMinSpeed && speed <= cleanLandingMaxSpeed && angle <= landingAngleTolerance;
            bool perfect = twoWheels && speed >= perfectLandingMinSpeed && speed <= perfectLandingMaxSpeed && angle <= landingAngleTolerance;

            Debug.Log($"[LandingCheck] speed={speed:F2}, angle={angle:F1}, twoWheels={twoWheels}, clean={clean}, perfect={perfect}, " +
                      $"Thresholds - clean[{cleanLandingMinSpeed}-{cleanLandingMaxSpeed}], perfect[{perfectLandingMinSpeed}-{perfectLandingMaxSpeed}], window={dualWheelGroundWindow}, tol={landingAngleTolerance}");

            int basePoints = 0;
            string label = null;
            if (flips > 0 && oneWheel)
            {
                basePoints = flips * 100;
                label = cumulativeRotation < 0 ? $"Frontflip! x{flips}" : $"Backflip! x{flips}";
            }
            else if (clean)
            {
                basePoints = 25;
                label = "Clean Landing!";
            }
            else if (perfect)
            {
                basePoints = 50;
                label = "Perfect Landing!";
            }

            // Airtime popup
            if (airtime >= minAirTimeForStunts)
            {
                float roundedAir = Mathf.Round(airtime * 2f) / 2f;
                int airPoints = Mathf.RoundToInt(roundedAir * 10f);
                ShowStuntPopup($"Airtime! ({roundedAir:F1}s)", airPoints, Color.cyan);
                AddScore(airPoints);
            }

            if (basePoints > 0)
            {
                currentComboCount++;
                comboTimer = comboResetDelay;
                int pts = Mathf.RoundToInt(basePoints * (1f + (currentComboCount - 1) * 0.5f));
                Color popupColor = label.Contains("flip") ? new Color(1f, 0.4f, 0f) :
                                   (label == "Perfect Landing!" ? Color.green : new Color(0f, 0.7f, 1f));

                ShowStuntPopup(label, pts, popupColor);
                if (currentComboCount > 1)
                    ShowComboPopup(currentComboCount);

                AddScore(pts);
                CameraShake.Instance?.Shake(0.15f, 0.5f);
            }
            else
            {
                currentComboCount = 0;
            }

            cumulativeRotation = 0f;
            airtime = 0f;
        }
    }

    void ShowStuntPopup(string label, int points, Color baseColor)
    {
        if (stuntTextPrefab == null || uiCanvas == null) return;
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 screenPos = new Vector2(
            Random.Range(Screen.width * 0.2f, Screen.width * 0.8f),
            Random.Range(Screen.height * 0.4f, Screen.height * 0.7f)
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos);
        RectTransform popup = Instantiate(stuntTextPrefab, uiCanvas.transform).GetComponent<RectTransform>();
        popup.localPosition = localPos;
        popup.localScale = Vector3.zero;
        popup.localRotation = Quaternion.Euler(0, 0, Random.Range(-maxPopupRotationAngle, maxPopupRotationAngle));
        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"{label}\n+{points}";
            text.colorGradient = new VertexGradient(baseColor, Color.white, Color.white, baseColor);
        }
        StartCoroutine(AnimatePopup(popup, text));
    }

    void ShowComboPopup(int comboCount)
    {
        if (stuntTextPrefab == null || uiCanvas == null) return;
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(Screen.width * 0.5f, Screen.height * 0.8f), null, out Vector2 localPos);
        RectTransform popup = Instantiate(stuntTextPrefab, uiCanvas.transform).GetComponent<RectTransform>();
        popup.localPosition = localPos;
        popup.localScale = Vector3.zero;
        popup.localRotation = Quaternion.identity;
        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"x{comboCount} Combo!";
            text.colorGradient = new VertexGradient(
                new Color32(255, 165, 0, 255),
                new Color32(255, 215, 0, 255),
                new Color32(255, 215, 0, 255),
                new Color32(255, 165, 0, 255)
            );
        }
        StartCoroutine(AnimatePopup(popup, text));
    }

    IEnumerator AnimatePopup(RectTransform popupRect, TextMeshProUGUI text)
    {
        float timer = 0f;
        while (timer < popupScaleDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / popupScaleDuration);
            popupRect.localScale = Vector3.one * t;
            text.alpha = t;
            yield return null;
        }
        yield return new WaitForSeconds(popupVisibleDuration);
        timer = 0f;
        while (timer < popupFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / popupFadeDuration;
            popupRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);
            text.alpha = 1f - t;
            yield return null;
        }
        Destroy(popupRect.gameObject);
    }

    void AddScore(int points)
    {
        totalScore += points;
        Score = totalScore;
        if (scoreCountingCoroutine != null)
            StopCoroutine(scoreCountingCoroutine);
        scoreCountingCoroutine = StartCoroutine(CountUp());
    }

    IEnumerator CountUp()
    {
        if (totalScoreText == null) yield break;
        int displayed = 0;
        int target = totalScore;
        float duration = 1.5f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float easedT = Mathf.Sin(timer / duration * Mathf.PI * 0.5f);
            int value = Mathf.RoundToInt(Mathf.Lerp(displayed, target, easedT));
            totalScoreText.text = $"Score: {value}";
            yield return null;
        }
        totalScoreText.text = $"Score: {target}";
    }

    void UpdateScoreUIInstant()
    {
        if (totalScoreText != null)
            totalScoreText.text = $"Score: {totalScore}";
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }
}
