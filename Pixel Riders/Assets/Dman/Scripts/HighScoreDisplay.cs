using UnityEngine;
using System.Collections;
using TMPro;

public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _highScoreText;
    
    private const string HighScoreKey = "HighScore";

    public void Awake()
    {
        // We'll leave the component reference here.
        if (_highScoreText == null)
        {
            _highScoreText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        // This is called every time the object is enabled.
        // It will now trigger the DisplayHighScore method which handles the animation from scratch.
        DisplayHighScore();
    }
    
    public void DisplayHighScore()
    {
        if (_highScoreText == null)
        {
            Debug.LogError("TextMeshProUGUI for high score is not assigned!");
            return;
        }

        // --- THE FIX IS HERE ---
        // Reset the text's state immediately before starting the animation.
        // This guarantees the pop-up effect will play every time.
        _highScoreText.alpha = 0f;
        _highScoreText.transform.localScale = Vector3.zero;

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        _highScoreText.text = $"High Score: {highScore}";
        
        StartCoroutine(AnimatePopUpText(_highScoreText, 0.75f, 0f, 1f));
    }
    
    private IEnumerator AnimatePopUpText(TextMeshProUGUI text, float fadeInDuration, float delay, float popScale)
    {
        yield return new WaitForSecondsRealtime(delay);

        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * popScale;

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeInDuration;
            text.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            text.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        text.transform.localScale = endScale;
        text.alpha = 1f;
    }
}