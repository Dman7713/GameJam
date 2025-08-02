using UnityEngine;
using TMPro;
using System.Collections;

public class StuntTest : MonoBehaviour
{
    public GameObject stuntTextPrefab;       // Prefab with TextMeshProUGUI component (enabled!)
    public Canvas uiCanvas;                   // Canvas (Screen Space Overlay recommended)
    public TextMeshProUGUI totalScoreText;   // Text showing total score

    private int Score = 0;
    private Coroutine countingCoroutine;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AwardStunt("TestFlip!", 500);
        }
    }

    void AwardStunt(string label, int points)
    {
        Score += points;

        if (stuntTextPrefab == null || uiCanvas == null)
        {
            Debug.LogWarning("Assign stuntTextPrefab and uiCanvas in inspector.");
            return;
        }

        // Spawn popup at random screen position within bounds
        Vector2 screenPos = new Vector2(
            Random.Range(Screen.width * 0.2f, Screen.width * 0.8f),
            Random.Range(Screen.height * 0.4f, Screen.height * 0.7f)
        );

        // Convert screen space to local canvas position
        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);

        GameObject popup = Instantiate(stuntTextPrefab, uiCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.localPosition = localPos;

        // Start invisible and small for animation
        popupRect.localScale = Vector3.zero;

        // Add small random rotation on Z axis (keeps text upright)
        popupRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-10f, 10f));

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"{label}\n+{points}";
            text.alpha = 0f;
            text.color = Color.white; // ensure visible
        }

        StartCoroutine(AnimateStuntPopup(popupRect, text));

        if (countingCoroutine != null) StopCoroutine(countingCoroutine);
        countingCoroutine = StartCoroutine(AnimateScoreCountUp());
    }

    IEnumerator AnimateStuntPopup(RectTransform popupRect, TextMeshProUGUI text)
    {
        float popDuration = 0.3f;
        float visibleDuration = 1f;
        float fadeDuration = 1.5f;

        // Pop in: scale 0->1 and fade 0->1 smoothly (smoothstep)
        float timer = 0f;
        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / popDuration);
            popupRect.localScale = Vector3.one * t;
            text.alpha = t;
            yield return null;
        }
        popupRect.localScale = Vector3.one;
        text.alpha = 1f;

        yield return new WaitForSeconds(visibleDuration);

        // Fade out: alpha 1->0 and scale 1->0.5 smoothly
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);
            text.alpha = 1f - t;
            popupRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);
            yield return null;
        }

        Destroy(popupRect.gameObject);
    }

    IEnumerator AnimateScoreCountUp()
    {
        if (totalScoreText == null) yield break;

        int displayedScore = 0;
        int targetScore = Score;
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
}
