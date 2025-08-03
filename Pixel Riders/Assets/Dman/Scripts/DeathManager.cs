using UnityEngine;
using System.Collections;
using TMPro;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance;

    [SerializeField] private Transform bikeRoot;
    [SerializeField] private Transform head; 
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private StuntManager stuntManager;
    [SerializeField] private GameObject _scoreUIObject;

    [Header("Death UI")]
    [SerializeField] private GameObject _deathCanvas;
    [SerializeField] private TextMeshProUGUI _deathText;
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    
    [SerializeField] private TextMeshProUGUI _highScoreText;
    
    private HighScoreDisplay _deathHighScoreDisplay;

    private bool hasDied = false;
    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (_deathCanvas != null)
            _deathCanvas.SetActive(false);

        if (_finalScoreText != null)
        {
            _finalScoreText.alpha = 0f;
            _finalScoreText.transform.localScale = Vector3.zero;
        }

        if (_highScoreText != null)
        {
            _highScoreText.alpha = 0f;
            _highScoreText.transform.localScale = Vector3.zero;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasDied) return;

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Debug.Log("Driver's head hit the ground!");
            TriggerDeath();
        }
    }

    public void TriggerDeath()
    {
        if (hasDied) return;

        hasDied = true;
        UnparentAndEnableRagdoll();
        HandlePlayerDeath();
    }

    private void UnparentAndEnableRagdoll()
    {
        if (head == null)
        {
            Debug.LogError("Head is not assigned in DeathManager!");
            return;
        }

        head.SetParent(null);
        Rigidbody2D rb = head.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = head.gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Time.timeScale = 0.3f;

        if (stuntManager != null && stuntManager.uiCanvas != null)
        {
            foreach (Transform child in stuntManager.uiCanvas.transform)
                Destroy(child.gameObject);
        }

        if (_scoreUIObject != null)
            _scoreUIObject.SetActive(false);

        yield return new WaitForSecondsRealtime(0.5f);

        if (_deathCanvas != null)
        {
            _deathCanvas.SetActive(true);

            if (_deathText != null)
            {
                yield return StartCoroutine(AnimateDeathText(_deathText, 0.75f, 0f, 1f));
                StartCoroutine(PulseText(_deathText, 0.05f, 0.5f));
            }

            if (_finalScoreText != null)
            {
                int currentScore = StuntManager.Score;
                
                // New Debug Log: See what the current score is right before the check
                Debug.Log($"DeathManager: Current score from StuntManager is {currentScore}");

                int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
                
                // New Debug Log: See what the high score is being read as
                Debug.Log($"DeathManager: High score from PlayerPrefs is {highScore}");

                if (currentScore > highScore)
                {
                    PlayerPrefs.SetInt(HighScoreKey, currentScore);
                    PlayerPrefs.Save();
                    Debug.Log($"DeathManager: New high score set to {currentScore}");
                }

                _finalScoreText.text = $"Score: {currentScore}";
                yield return StartCoroutine(AnimateDeathText(_finalScoreText, 0.5f, 0f, 1f));
            }
            
            _deathHighScoreDisplay = _deathCanvas.GetComponentInChildren<HighScoreDisplay>();
            if (_deathHighScoreDisplay != null)
            {
                yield return new WaitForSecondsRealtime(0.75f);
                _deathHighScoreDisplay.DisplayWithPopUpAnimation(_highScoreText);
            }
            else
            {
                Debug.LogError("HighScoreDisplay component not found! Make sure it's a child of the death canvas.");
            }
        }
    }

    public bool IsDead()
    {
        return hasDied;
    }

    private IEnumerator PulseText(TextMeshProUGUI text, float pulseScale, float duration)
    {
        Vector3 originalScale = text.transform.localScale;
        Vector3 pulseMaxScale = originalScale * (1f + pulseScale);

        while (true)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.Lerp(originalScale, pulseMaxScale, timer / duration);
                yield return null;
            }

            timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.Lerp(pulseMaxScale, originalScale, timer / duration);
                yield return null;
            }
        }
    }

    private IEnumerator AnimateDeathText(TextMeshProUGUI text, float fadeInDuration, float delay, float popScale)
    {
        text.transform.localScale = Vector3.zero;
        text.alpha = 0f;
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