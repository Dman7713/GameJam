using UnityEngine;
using TMPro;
using System.Collections;

public class StuntManager : MonoBehaviour
{
    [Header("References")]
    public GameObject stuntTextPrefab;       // Your single text prefab (must contain TextMeshProUGUI)
    public Canvas uiCanvas;                   // UI Canvas to spawn popups in (Screen Space - Overlay recommended)
    public TextMeshProUGUI totalScoreText;   // UI text to show total score with counting animation

    [Header("Settings")]
    public float minAirTimeForStunts = 0.3f;     // Minimum airtime for flips to count
    public float popupScaleDuration = 0.3f;      // Popup scaling time
    public float popupVisibleDuration = 1f;      // Popup stay time before fading
    public float popupFadeDuration = 1.5f;       // Popup fade out time
    public float maxPopupRotationAngle = 10f;    // Max random popup rotation in degrees (kept upright-ish)

    private Rigidbody2D bikeRigidbody;

    private bool wasInAirLastFrame = false;
    private float airtime = 0f;

    private float lastRotationZ = 0f;
    private float cumulativeRotation = 0f;  // Accumulate rotation in degrees while airborne

    private int totalScore = 0;
    private Coroutine scoreCountingCoroutine;

    // Static property to expose score for DeathManager
    public static int Score { get; private set; } = 0;

    void Awake()
    {
        bikeRigidbody = GetComponent<Rigidbody2D>();
        lastRotationZ = NormalizeAngle(transform.eulerAngles.z);
        UpdateScoreUIInstant();
    }

    // Call this every FixedUpdate from DriverController, passing current grounded state, previous grounded state, and Rigidbody2D
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
            // Just landed this frame after being in air
            if (wasInAirLastFrame)
            {
                if (airtime >= minAirTimeForStunts)
                {
                    int fullFlips = Mathf.FloorToInt(Mathf.Abs(cumulativeRotation) / 360f);

                    if (fullFlips > 0)
                    {
                        bool isFrontFlip = cumulativeRotation < 0;
                        int points = fullFlips * 100;

                        if (isFrontFlip)
                            ShowStuntPopup($"Frontflip! x{fullFlips}", points);
                        else
                            ShowStuntPopup($"Backflip! x{fullFlips}", points);

                        AddScore(points);
                    }

                    // TODO: Add clean and perfect landing detection here and award points accordingly

                    // Reset
                    cumulativeRotation = 0f;
                    airtime = 0f;
                }
                else
                {
                    // Landed too soon to count flips
                    cumulativeRotation = 0f;
                    airtime = 0f;
                }
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

        // Random small rotation but keep mostly upright
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

        // Wait visible duration
        yield return new WaitForSeconds(popupVisibleDuration);

        // Fade out + scale down
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
