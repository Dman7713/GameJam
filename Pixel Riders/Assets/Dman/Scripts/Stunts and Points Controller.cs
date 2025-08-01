using System.Collections;
using UnityEngine;
using TMPro;

public class StuntManager : MonoBehaviour
{
    [Header("Stunt Settings")]
    [SerializeField] private float cleanLandingThreshold = 20f;
    [SerializeField] private float perfectLandingThreshold = 5f;
    [SerializeField] private float airTimeForStunt = 0.5f;
    [SerializeField] private float landingGracePeriod = 0.75f;
    [SerializeField] private int stuntBonusPoints = 25;

    [Header("Point Values")]
    [SerializeField] private int frontflipPoints = 250;
    [SerializeField] private int backflipPoints = 250;
    [SerializeField] private int airTimePointsPerSecond = 50;
    [SerializeField] private int cleanLandingBonus = 100;
    [SerializeField] private int perfectLandingBonus = 250;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreDisplayText;
    [SerializeField] private GameObject stuntTextPrefab;
    public Transform stuntDisplayParent;

    private bool isTrackingStunt;
    private bool isTrackingLanding;
    private bool hasCrashed;
    private float airTime;
    private float rotationOnJump;
    private int flipsCompleted;
    private int flipsInComboCount;

    private int pendingFlipPoints;
    private int pendingAirTimePoints;
    private int pendingLandingBonusPoints;

    private Coroutine stuntCompletionCoroutine;
    private Coroutine scoreAnimationCoroutine;

    private static int score;
    public static int Score
    {
        get => score;
        private set => score = value;
    }

    private void Awake()
    {
        Score = 0;
        if (scoreDisplayText != null)
            scoreDisplayText.text = "0";
    }

    public void HandleStuntTracking(bool isGrounded, bool wasGrounded, Rigidbody2D carRB)
    {
        if (!isGrounded && wasGrounded && !isTrackingLanding)
        {
            isTrackingStunt = true;
            airTime = 0f;
            flipsCompleted = 0;
            rotationOnJump = carRB.rotation;
            flipsInComboCount = 0;
            pendingFlipPoints = 0;
            pendingAirTimePoints = 0;
            pendingLandingBonusPoints = 0;
            hasCrashed = false;
        }

        if (!isGrounded && isTrackingStunt)
        {
            airTime += Time.fixedDeltaTime;
            float rotSinceJump = carRB.rotation - rotationOnJump;
            int newFlips = Mathf.FloorToInt(Mathf.Abs(rotSinceJump) / 360f);

            if (newFlips > flipsCompleted)
            {
                int flipsDetected = newFlips - flipsCompleted;
                flipsCompleted = newFlips;

                string trickName = rotSinceJump > 0 ? "Backflip" : "Frontflip";
                int trickPoints = rotSinceJump > 0 ? backflipPoints : frontflipPoints;

                pendingFlipPoints += trickPoints * flipsDetected;
                DisplayStuntFeedback($"{trickName} ({flipsCompleted}x)", trickPoints * flipsDetected);
                flipsInComboCount += flipsDetected;
            }
        }

        if (isGrounded && !wasGrounded && isTrackingStunt)
        {
            isTrackingStunt = false;
            isTrackingLanding = true;

            float landingAngle = Vector2.Angle(transform.up, Vector2.up);
            hasCrashed = landingAngle > cleanLandingThreshold;

            if (!hasCrashed && airTime >= airTimeForStunt)
            {
                pendingAirTimePoints = Mathf.FloorToInt(airTime) * airTimePointsPerSecond;
                if (pendingAirTimePoints > 0)
                    DisplayStuntFeedback("Air Time!", pendingAirTimePoints);

                if (landingAngle <= perfectLandingThreshold)
                {
                    pendingLandingBonusPoints = perfectLandingBonus;
                    DisplayStuntFeedback("PERFECT LANDING!", perfectLandingBonus);
                }
                else if (landingAngle <= cleanLandingThreshold)
                {
                    pendingLandingBonusPoints = cleanLandingBonus;
                    DisplayStuntFeedback("Clean Landing!", cleanLandingBonus);
                }
            }
            else
            {
                pendingFlipPoints = 0;
                pendingAirTimePoints = 0;
                pendingLandingBonusPoints = 0;
            }

            if (stuntCompletionCoroutine != null) StopCoroutine(stuntCompletionCoroutine);
            stuntCompletionCoroutine = StartCoroutine(HandleSuccessfulLanding());
        }
    }

    private IEnumerator HandleSuccessfulLanding()
    {
        yield return new WaitForSeconds(landingGracePeriod);
        isTrackingLanding = false;

        if (!hasCrashed)
        {
            AddScore(pendingFlipPoints);
            AddScore(pendingAirTimePoints);
            AddScore(pendingLandingBonusPoints);

            if (flipsInComboCount > 1)
            {
                int comboPoints = flipsInComboCount * stuntBonusPoints;
                AddScore(comboPoints);
                DisplayStuntFeedback("Stunt Combo!", comboPoints);
            }
        }

        stuntCompletionCoroutine = null;
    }

    private void AddScore(int points)
    {
        if (points <= 0) return;
        int oldScore = Score;
        Score += points;
        if (scoreAnimationCoroutine != null) StopCoroutine(scoreAnimationCoroutine);
        scoreAnimationCoroutine = StartCoroutine(AnimateScoreText(oldScore, Score));
    }

    private IEnumerator AnimateScoreText(int oldScore, int newScore)
    {
        float duration = 0.5f, timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            int currentScore = (int)Mathf.Lerp(oldScore, newScore, progress);
            scoreDisplayText.text = $"Score: {currentScore}";
            yield return null;
        }
        scoreDisplayText.text = $"Score: {newScore}";
        scoreAnimationCoroutine = null;
    }

    private void DisplayStuntFeedback(string stuntName, int points)
    {
        if (stuntTextPrefab == null || stuntDisplayParent == null) return;

        GameObject textObject = Instantiate(stuntTextPrefab, stuntDisplayParent);
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
}
