// DeathManager.cs
using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// This script manages the player's death sequence.
/// It is designed to be attached directly to the head GameObject.
/// </summary>
public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance;

    [SerializeField] private StuntManager stuntManager;
    [SerializeField] private GameObject _scoreUIObject;
    [SerializeField] private LayerMask groundLayer;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _headPopParticles;
    // New field to control the size of the particles.
    [SerializeField] private float _particleSize = 1.0f;

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

        // Check if the necessary components are present on the head GameObject.
        if (GetComponent<Rigidbody2D>() == null || GetComponent<Collider2D>() == null)
        {
            Debug.LogError("DeathManager script requires a Rigidbody2D and Collider2D on the same GameObject to detect collisions. Make sure the head has these components.");
        }
        
        // Log a message if a joint is found, as it will be destroyed on death.
        if (GetComponent<Joint2D>() != null)
        {
            Debug.Log("DeathManager found a Joint2D on the head. This joint will be destroyed on death to unattach the head.");
        }

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
    
    /// <summary>
    /// Detects collisions with the ground to trigger the death sequence.
    /// This method is on the DeathManager script itself, which should be attached to the head.
    /// </summary>
    /// <param name="collision">The collision data.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collided object is on the ground layer and the player hasn't died yet.
        if (!hasDied && ((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Debug.Log("Driver's head hit the ground!");
            TriggerDeath();
        }
    }

    /// <summary>
    /// Triggers the player's death sequence, including unparenting the head and playing particles.
    /// </summary>
    public void TriggerDeath()
    {
        if (hasDied) return;

        hasDied = true;

        // Play the particle effect at the head's position.
        if (_headPopParticles != null)
        {
            // Unparent the particle system from the head so it doesn't move with it.
            _headPopParticles.transform.SetParent(null);

            // Set the particle size to the desired value.
            var mainModule = _headPopParticles.main;
            mainModule.startSize = _particleSize;

            _headPopParticles.Play();
        }

        UnparentAndEnableRagdoll();
        HandlePlayerDeath();
    }

    private void UnparentAndEnableRagdoll()
    {
        // Find and destroy all Joint2D components on this GameObject to sever the physical connection.
        Joint2D[] joints = GetComponents<Joint2D>();
        foreach (Joint2D joint in joints)
        {
            Destroy(joint);
        }

        // Unparent the head (this GameObject) so it can move independently.
        this.transform.SetParent(null);

        // Get the Rigidbody2D component which should already be on the head.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        // This check is now mostly for safety, as Awake() should have caught this.
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

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
                
                Debug.Log($"DeathManager: Current score from StuntManager is {currentScore}");

                int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
                
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
