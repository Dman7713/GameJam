using UnityEngine;
using TMPro;
using System.Collections;

public class StuntManager : MonoBehaviour
{
    [Header("References")]
    public GameObject stuntTextPrefab;       // Prefab with TextMeshProUGUI for stunt popup
    public Canvas uiCanvas;                   // UI Canvas (Screen Space - Overlay)
    public TextMeshProUGUI totalScoreText;   // Score display text

    [Header("Settings")]
    public float minAirTimeForStunts = 0.3f;     
    public float popupScaleDuration = 0.3f;
    public float popupVisibleDuration = 1f;
    public float popupFadeDuration = 1.5f;
    public float maxPopupRotationAngle = 10f;
    public float comboResetDelay = 1.5f;     // Time to reset combo if no stunt occurs

    [Header("Slow Motion Settings")]
    public float slowMoDuration = 0.5f;
    public float slowMoTimeScale = 0.2f;

    private Rigidbody2D bikeRigidbody;

    private bool wasInAirLastFrame = false;
    private float airtime = 0f;

    private float lastRotationZ = 0f;
    private float cumulativeRotation = 0f;

    private int totalScore = 0;
    private Coroutine scoreCountingCoroutine;

    // Combo tracking
    private int currentComboCount = 0;
    private float comboTimer = 0f;

    // Perfect landing flag (for demo, here we define perfect landing as less than 10 degrees rotation on landing)
    private const float perfectLandingMaxAngle = 10f;

    public static int Score { get; private set; } = 0;

    void Awake()
    {
        bikeRigidbody = GetComponent<Rigidbody2D>();
        lastRotationZ = NormalizeAngle(transform.eulerAngles.z);
        UpdateScoreUIInstant();
    }

    void Update()
    {
        // Combo reset timer counts down in Update
        if (currentComboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                currentComboCount = 0;
            }
        }
    }

    // Call every FixedUpdate from DriverController
    public void HandleStuntTracking(bool grounded, bool wasGroundedPrev, Rigidbody2D rb)
    {
        float currentRotationZ = NormalizeAngle(transform.eulerAngles.z);
        float deltaRotation = Mathf.DeltaAngle(lastRotationZ, currentRotationZ);

        if (!grounded)
        {
            airtime += Time.fixedDeltaTime;
            cumulativeRotation += deltaRotation;
        }
        else
        {
            // Just landed this frame
            if (wasInAirLastFrame)
            {
                if (airtime >= minAirTimeForStunts)
                {
                    int fullFlips = Mathf.FloorToInt(Mathf.Abs(cumulativeRotation) / 360f);
                    float landingAngle = Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, 0f)); // angle difference from upright

                    bool perfectLanding = landingAngle <= perfectLandingMaxAngle;

                    int basePoints = 0;
                    string stuntLabel = null;

                    if (fullFlips > 0)
                    {
                        bool isFrontFlip = cumulativeRotation < 0;
                        basePoints = fullFlips * 100;
                        stuntLabel = isFrontFlip ? $"Frontflip! x{fullFlips}" : $"Backflip! x{fullFlips}";
                    }
                    else if (perfectLanding)
                    {
                        // Award for perfect landing even without flips
                        basePoints = 50;
                        stuntLabel = "Perfect Landing!";
                    }
                    else
                    {
                        // Could add clean landing points here
                        basePoints = 0;
                    }

                    if (basePoints > 0)
                    {
                        // Increase combo count and reset combo timer
                        currentComboCount++;
                        comboTimer = comboResetDelay;

                        // Calculate combo multiplier
                        float comboMultiplier = 1f + (currentComboCount - 1) * 0.5f; // e.g. x1, x1.5, x2, etc.
                        int points = Mathf.RoundToInt(basePoints * comboMultiplier);

                        // Show stunt popup
                        if (!string.IsNullOrEmpty(stuntLabel))
                            ShowStuntPopup(stuntLabel, points);

                        // Show combo popup if 2+ combo
                        if (currentComboCount > 1)
                            ShowComboPopup(currentComboCount);

                        AddScore(points);

                        // Screen shake for every stunt landing
                        if (CameraShake.Instance != null)
                        {
                            CameraShake.Instance.Shake(0.15f, 0.5f);
                        }

                        // Slow motion on perfect landing or multi-flip
                        if (perfectLanding || fullFlips > 1)
                        {
                            StartCoroutine(DoSlowMotion());
                        }
                    }
                    else
                    {
                        // Reset combo if no points
                        currentComboCount = 0;
                    }
                }

                // Reset rotation and airtime every landing
                cumulativeRotation = 0f;
                airtime = 0f;
            }
        }

        lastRotationZ = currentRotationZ;
        wasInAirLastFrame = !grounded;
    }

    void ShowStuntPopup(string label, int points)
    {
        if (stuntTextPrefab == null || uiCanvas == null)
            return;

        Vector2 screenPos = new Vector2(
            Random.Range(Screen.width * 0.2f, Screen.width * 0.8f),
            Random.Range(Screen.height * 0.4f, Screen.height * 0.7f)
        );

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);

        GameObject popup = Instantiate(stuntTextPrefab, uiCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.localPosition = localPos;
        popupRect.localScale = Vector3.zero;

        float randomAngle = Random.Range(-maxPopupRotationAngle, maxPopupRotationAngle);
        popupRect.localRotation = Quaternion.Euler(0f, 0f, randomAngle);

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"{label}\n+{points}";
            text.alpha = 0f;
            text.color = Color.white;
        }

        StartCoroutine(AnimateStuntPopup(popupRect, text));
    }

    void ShowComboPopup(int comboCount)
    {
        if (stuntTextPrefab == null || uiCanvas == null)
            return;

        // Create combo popup at center top-ish screen, bigger and with gradient colors
        Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.8f);

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);

        GameObject popup = Instantiate(stuntTextPrefab, uiCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.localPosition = localPos;
        popupRect.localScale = Vector3.zero;

        // No random rotation for combo, keep upright
        popupRect.localRotation = Quaternion.identity;

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"x{comboCount} Combo!";
            text.alpha = 0f;

            // Gradient colors: start orange, end yellow for combo hype
            var gradient = new VertexGradient(
                new Color32(255, 165, 0, 255),   // Orange
                new Color32(255, 215, 0, 255),   // Gold
                new Color32(255, 215, 0, 255),
                new Color32(255, 165, 0, 255)
            );
            text.colorGradient = gradient;
        }

        StartCoroutine(AnimateComboPopup(popupRect, text));
    }

    IEnumerator AnimateStuntPopup(RectTransform popupRect, TextMeshProUGUI text)
    {
        float timer = 0f;

        // Scale & fade in
        while (timer < popupScaleDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / popupScaleDuration);
            popupRect.localScale = Vector3.one * t;
            text.alpha = t;
            yield return null;
        }
        popupRect.localScale = Vector3.one;
        text.alpha = 1f;

        // Visible duration
        yield return new WaitForSeconds(popupVisibleDuration);

        // Fade out & scale down
        timer = 0f;
        while (timer < popupFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / popupFadeDuration);
            text.alpha = 1f - t;
            popupRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);
            yield return null;
        }

        Destroy(popupRect.gameObject);
    }

    IEnumerator AnimateComboPopup(RectTransform popupRect, TextMeshProUGUI text)
    {
        // Bounce + fade in + scale up quickly for satisfying effect
        float bounceDuration = 0.15f;
        float timer = 0f;

        Vector3 startScale = Vector3.zero;
        Vector3 overshootScale = Vector3.one * 1.2f;
        Vector3 targetScale = Vector3.one;

        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;
            float t = timer / bounceDuration;
            // Bounce with overshoot curve
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.75f);
            popupRect.localScale = Vector3.LerpUnclamped(startScale, overshootScale, scaleT);
            text.alpha = t;
            yield return null;
        }

        // Bounce back to normal scale
        timer = 0f;
        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;
            float t = timer / bounceDuration;
            popupRect.localScale = Vector3.LerpUnclamped(overshootScale, targetScale, t);
            yield return null;
        }

        popupRect.localScale = targetScale;
        text.alpha = 1f;

        // Hold for a bit
        yield return new WaitForSeconds(popupVisibleDuration);

        // Fade out smoothly
        timer = 0f;
        while (timer < popupFadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / popupFadeDuration);
            text.alpha = 1f - t;
            popupRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);
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

        scoreCountingCoroutine = StartCoroutine(AnimateScoreCountUp());
    }

    IEnumerator AnimateScoreCountUp()
    {
        if (totalScoreText == null)
            yield break;

        int displayedScore = 0;
        int targetScore = totalScore;
        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
            int newScore = Mathf.RoundToInt(Mathf.Lerp(displayedScore, targetScore, easedT));
            totalScoreText.text = $"Score: {newScore}";
            yield return null;
        }

        totalScoreText.text = $"Score: {targetScore}";
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

    IEnumerator DoSlowMotion()
    {
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowMoDuration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
