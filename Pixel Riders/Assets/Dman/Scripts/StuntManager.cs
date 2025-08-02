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

    private Rigidbody2D bikeRigidbody;

    private bool wasInAirLastFrame = false;
    private float airtime = 0f;

    private float lastRotationZ = 0f;
    private float cumulativeRotation = 0f;

    private int totalScore = 0;
    private Coroutine scoreCountingCoroutine;

    private int currentComboCount = 0;
    private float comboTimer = 0f;

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
        if (currentComboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                currentComboCount = 0;
            }
        }
    }

    public void HandleStuntTracking(bool grounded, bool isDead, Rigidbody2D rb, bool frontTireGrounded, bool backTireGrounded, bool landedThisFrame)
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
            if (wasInAirLastFrame && landedThisFrame)
            {
                if (airtime >= minAirTimeForStunts)
                {
                    int fullFlips = Mathf.FloorToInt(Mathf.Abs(cumulativeRotation) / 360f);
                    float landingAngle = Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, 0f));
                    bool perfectLanding = landingAngle <= perfectLandingMaxAngle;

                    int basePoints = 0;
                    string stuntLabel = null;

                    VertexGradient stuntGradient = default;

                    // FLIPS: award if player survived (not dead)
                    if (fullFlips > 0)
                    {
                        bool isFrontFlip = cumulativeRotation < 0;
                        stuntLabel = isFrontFlip ? $"Frontflip! x{fullFlips}" : $"Backflip! x{fullFlips}";
                        basePoints = fullFlips * 100;

                        stuntGradient = isFrontFlip ? VertexGradientFrontflip() : VertexGradientBackflip();

                        if (isDead)
                        {
                            Debug.Log($"Completed {stuntLabel} for 0 points (dead on landing)");
                            ResetStuntTracking();
                            return;
                        }
                    }
                    else if (frontTireGrounded && backTireGrounded)
                    {
                        float landingSpeed = rb.linearVelocity.magnitude;

                        if (perfectLanding && landingSpeed > 7f)
                        {
                            stuntLabel = "Perfect Landing!";
                            basePoints = 50;
                            stuntGradient = VertexGradientPerfectLanding();
                        }
                        else if (!perfectLanding && landingSpeed > 3.5f)
                        {
                            stuntLabel = "Clean Landing!";
                            basePoints = 25;
                            stuntGradient = VertexGradientCleanLanding();
                        }
                        else
                        {
                            stuntLabel = null;
                            basePoints = 0;
                        }

                        if (isDead)
                        {
                            if (stuntLabel != null)
                                Debug.Log($"Completed {stuntLabel} for 0 points (dead on landing)");
                            ResetStuntTracking();
                            return;
                        }
                    }
                    else
                    {
                        basePoints = 0;
                        stuntLabel = null;

                        if (isDead)
                        {
                            ResetStuntTracking();
                            return;
                        }
                    }

                    // Airtime points only if airtime > 1 second
                    if (airtime > 1f)
                    {
                        float airtimeRounded = Mathf.Round(airtime * 2f) / 2f;
                        int airtimePoints = Mathf.RoundToInt(airtimeRounded * 10f);
                        if (airtimePoints > 0)
                        {
                            ShowStuntPopup($"Airtime! ({airtimeRounded}s)", airtimePoints, VertexGradientAirtime());
                            AddScore(airtimePoints);
                            Debug.Log($"Completed Airtime! ({airtimeRounded}s) for {airtimePoints} points");
                        }
                    }

                    if (basePoints > 0 && stuntLabel != null)
                    {
                        currentComboCount++;
                        comboTimer = comboResetDelay;

                        float comboMultiplier = 1f + (currentComboCount - 1) * 0.5f;
                        int points = Mathf.RoundToInt(basePoints * comboMultiplier);

                        ShowStuntPopup(stuntLabel, points, stuntGradient);

                        if (currentComboCount > 1)
                            ShowComboPopup(currentComboCount);

                        Debug.Log($"Completed {stuntLabel} for {points} points");

                        AddScore(points);

                        if (CameraShake.Instance != null)
                        {
                            CameraShake.Instance.Shake(0.15f, 0.5f);
                        }
                    }
                    else
                    {
                        currentComboCount = 0;
                    }
                }
                ResetStuntTracking();
            }
        }

        lastRotationZ = currentRotationZ;
        wasInAirLastFrame = !grounded;
    }

    void ResetStuntTracking()
    {
        cumulativeRotation = 0f;
        airtime = 0f;
    }

    void ShowStuntPopup(string label, int points, VertexGradient gradient)
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
            text.colorGradient = gradient;
        }

        StartCoroutine(AnimateStuntPopup(popupRect, text));
    }

    void ShowComboPopup(int comboCount)
    {
        if (stuntTextPrefab == null || uiCanvas == null)
            return;

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
                new Color32(255, 165, 0, 255),
                new Color32(255, 215, 0, 255),
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

    // VertexGradient presets for different stunt types
    VertexGradient VertexGradientFrontflip()
    {
        return new VertexGradient(
            new Color32(0, 122, 255, 255),
            new Color32(0, 255, 255, 255),
            new Color32(0, 122, 255, 255),
            new Color32(0, 255, 255, 255));
    }

    VertexGradient VertexGradientBackflip()
    {
        return new VertexGradient(
            new Color32(128, 0, 128, 255),
            new Color32(255, 0, 255, 255),
            new Color32(128, 0, 128, 255),
            new Color32(255, 0, 255, 255));
    }

    VertexGradient VertexGradientPerfectLanding()
    {
        return new VertexGradient(
            new Color32(0, 255, 0, 255),
            new Color32(144, 238, 144, 255),
            new Color32(0, 255, 0, 255),
            new Color32(144, 238, 144, 255));
    }

    VertexGradient VertexGradientCleanLanding()
    {
        return new VertexGradient(
            new Color32(255, 140, 0, 255),
            new Color32(255, 215, 0, 255),
            new Color32(255, 140, 0, 255),
            new Color32(255, 215, 0, 255));
    }

    VertexGradient VertexGradientAirtime()
    {
        return new VertexGradient(
            new Color32(0, 128, 128, 255),
            new Color32(173, 216, 230, 255),
            new Color32(0, 128, 128, 255),
            new Color32(173, 216, 230, 255));
    }
}
