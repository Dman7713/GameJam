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

    // Landing speed thresholds (tweak as needed)
    public float perfectLandingMinSpeed = 7f;    // Must be >= this for perfect landing
    public float perfectLandingMaxSpeed = 20f;   // Max speed for perfect landing
    public float cleanLandingMinSpeed = 5f;      // >= this for clean landing
    public float cleanLandingMaxSpeed = 7f;      // max speed for clean landing

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

    // Perfect landing max angle (upright tolerance)
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

    /// <summary>
    /// Parameters:
    /// grounded = is player grounded now
    /// playerDead = is player dead (don't award points if dead)
    /// rb = bike's Rigidbody2D
    /// frontTireGrounded = is front wheel grounded
    /// backTireGrounded = is back wheel grounded
    /// landedThisFrame = did player just land this frame
    /// </summary>
    public void HandleStuntTracking(
        bool grounded,
        bool playerDead,
        Rigidbody2D rb,
        bool frontTireGrounded,
        bool backTireGrounded,
        bool landedThisFrame)
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
            if (landedThisFrame && !playerDead)
            {
                int fullFlips = Mathf.FloorToInt(Mathf.Abs(cumulativeRotation) / 360f);
                float landingAngle = Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, 0f));
                float speed = rb.linearVelocity.magnitude;

                bool landedTwoWheels = frontTireGrounded && backTireGrounded;
                bool landedOneWheel = frontTireGrounded || backTireGrounded;

                bool perfectLanding = false;
                bool cleanLanding = false;

                // Check perfect and clean landing ONLY if landed on two wheels
                if (landedTwoWheels)
                {
                    if (speed >= perfectLandingMinSpeed && speed <= perfectLandingMaxSpeed && landingAngle <= perfectLandingMaxAngle)
                        perfectLanding = true;
                    else if (speed >= cleanLandingMinSpeed && speed < perfectLandingMinSpeed && landingAngle <= perfectLandingMaxAngle)
                        cleanLanding = true;
                }

                int basePoints = 0;
                string stuntLabel = null;

                // Flips are awarded if landed on at least one wheel
                if (fullFlips > 0 && landedOneWheel)
                {
                    bool isFrontFlip = cumulativeRotation < 0;
                    basePoints = fullFlips * 100;
                    stuntLabel = isFrontFlip ? $"Frontflip! x{fullFlips}" : $"Backflip! x{fullFlips}";
                    Debug.Log($"Completed {stuntLabel} for {basePoints} points.");
                }
                else if (perfectLanding)
                {
                    basePoints = 50;
                    stuntLabel = "Perfect Landing!";
                    Debug.Log($"Completed {stuntLabel} for {basePoints} points.");
                }
                else if (cleanLanding)
                {
                    basePoints = 25;
                    stuntLabel = "Clean Landing!";
                    Debug.Log($"Completed {stuntLabel} for {basePoints} points.");
                }

                // Airtime points only awarded if > 1 second
                if (airtime > 1f)
                {
                    float roundedAirTime = Mathf.Round(airtime * 2f) / 2f; // round to nearest 0.5
                    int airtimePoints = Mathf.RoundToInt(roundedAirTime * 10);
                    if (airtimePoints > 0)
                    {
                        ShowStuntPopup($"Airtime! ({roundedAirTime}s)", airtimePoints, Color.cyan);
                        AddScore(airtimePoints);
                        Debug.Log($"Completed Airtime! ({roundedAirTime}s) for {airtimePoints} points.");
                    }
                }

                if (basePoints > 0 && stuntLabel != null)
                {
                    // Combo count increment and reset timer
                    currentComboCount++;
                    comboTimer = comboResetDelay;

                    float comboMultiplier = 1f + (currentComboCount - 1) * 0.5f;
                    int points = Mathf.RoundToInt(basePoints * comboMultiplier);

                    // Gradient colors per stunt type
                    Color popupColor = Color.white;
                    if (stuntLabel.Contains("Frontflip") || stuntLabel.Contains("Backflip"))
                        popupColor = new Color(1f, 0.4f, 0f); // orange
                    else if (stuntLabel == "Perfect Landing!")
                        popupColor = new Color(0f, 1f, 0f); // green
                    else if (stuntLabel == "Clean Landing!")
                        popupColor = new Color(0f, 0.7f, 1f); // blue

                    ShowStuntPopup(stuntLabel, points, popupColor);

                    if (currentComboCount > 1)
                        ShowComboPopup(currentComboCount);

                    AddScore(points);

                    if (CameraShake.Instance != null)
                        CameraShake.Instance.Shake(0.15f, 0.5f);
                }
                else
                {
                    // Reset combo if no stunt points awarded
                    currentComboCount = 0;
                }
            }

            cumulativeRotation = 0f;
            airtime = 0f;
        }

        lastRotationZ = currentRotationZ;
        wasInAirLastFrame = !grounded;
    }

    void ShowStuntPopup(string label, int points, Color baseColor)
    {
        if (stuntTextPrefab == null || uiCanvas == null) return;

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

            var gradient = new VertexGradient(
                baseColor,
                Color.white,
                Color.white,
                baseColor
            );
            text.colorGradient = gradient;
        }

        StartCoroutine(AnimateStuntPopup(popupRect, text));
    }

    void ShowComboPopup(int comboCount)
    {
        if (stuntTextPrefab == null || uiCanvas == null) return;

        Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.8f);

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);

        GameObject popup = Instantiate(stuntTextPrefab, uiCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.localPosition = localPos;
        popupRect.localScale = Vector3.zero;

        popupRect.localRotation = Quaternion.identity;

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"x{comboCount} Combo!";
            text.alpha = 0f;

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

        yield return new WaitForSeconds(popupVisibleDuration);

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
        float bounceDuration = 0.15f;
        float timer = 0f;

        Vector3 startScale = Vector3.zero;
        Vector3 overshootScale = Vector3.one * 1.2f;
        Vector3 targetScale = Vector3.one;

        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;
            float t = timer / bounceDuration;
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.75f);
            popupRect.localScale = Vector3.LerpUnclamped(startScale, overshootScale, scaleT);
            text.alpha = t;
            yield return null;
        }

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

        yield return new WaitForSeconds(popupVisibleDuration);

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
}
