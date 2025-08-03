using UnityEngine;
using System.Collections;
using TMPro;

public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private bool _isMainMenu = false;
    
    private const string HighScoreKey = "HighScore";

    public void Awake()
    {
        if (_highScoreText == null)
        {
            _highScoreText = GetComponent<TextMeshProUGUI>();
        }
    }
    
    public void Start()
    {
        if (_isMainMenu)
        {
            DisplayWithDelayedCountUpAnimation();
        }
    }

    public void DisplayWithPopUpAnimation(TextMeshProUGUI highScoreText)
    {
        if (highScoreText == null)
        {
            Debug.LogError("HighScoreDisplay: TextMeshProUGUI for high score is not assigned!");
            return;
        }
        
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        highScoreText.text = $"High Score: {highScore}";
        
        StartCoroutine(AnimatePopUp(highScoreText, 0.75f, 0f, 1f));
    }
    
    public void DisplayWithDelayedCountUpAnimation()
    {
        if (_highScoreText == null)
        {
            Debug.LogError("HighScoreDisplay: TextMeshProUGUI for high score is not assigned!");
            return;
        }
        
        _highScoreText.alpha = 0f;
        _highScoreText.transform.localScale = Vector3.zero;
        
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        
        StartCoroutine(AnimatePopUpAndCountUp(_highScoreText, highScore));
    }
    
    private IEnumerator AnimatePopUp(TextMeshProUGUI text, float fadeInDuration, float delay, float popScale)
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

    private IEnumerator AnimatePopUpAndCountUp(TextMeshProUGUI text, int targetScore)
    {
        yield return new WaitForSecondsRealtime(3f);

        float popUpDuration = 0.5f;
        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        while (timer < popUpDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / popUpDuration;
            text.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            text.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        text.transform.localScale = endScale;
        text.alpha = 1f;
        text.text = $"High Score: 0";

        float countDuration = 1.5f;
        timer = 0f;
        int currentScore = 0;

        while (timer < countDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / countDuration;
            currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, progress));
            text.text = $"High Score: {currentScore}";
            yield return null;
        }

        text.text = $"High Score: {targetScore}";
    }
}