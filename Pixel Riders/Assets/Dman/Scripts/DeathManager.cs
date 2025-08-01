using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class DeathManager : MonoBehaviour
{
    // Make the instance of the script accessible from other scripts
    public static DeathManager Instance;

    [SerializeField] private Transform bikeRoot;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private StuntCarController _stuntCarController; // Reference to the StuntCarController script
    [SerializeField] private GameObject _scoreUIObject; // The root GameObject for the score UI

    [Header("Optional - Manual joints to disable")]
    [SerializeField] private List<HingeJoint2D> hingeJointsToDisable = new List<HingeJoint2D>();
    [SerializeField] private List<WheelJoint2D> wheelJointsToDisable = new List<WheelJoint2D>();

    [Header("Death UI")]
    [Tooltip("The main Canvas that contains the 'You Died' screen.")]
    [SerializeField] private GameObject _deathCanvas;
    [Tooltip("The TextMeshProUGUI object for the 'DEAD' message.")]
    [SerializeField] private TextMeshProUGUI _deathText;
    [Tooltip("The TextMeshProUGUI object for displaying the final score.")]
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [Tooltip("The TextMeshProUGUI object for displaying the high score.")]
    [SerializeField] private TextMeshProUGUI _highScoreText;
    
    private bool hasDied = false;

    // The key used to save and load the high score from PlayerPrefs
    private const string HighScoreKey = "HighScore";

    private void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Ensure the death canvas is initially disabled
        if (_deathCanvas != null)
        {
            _deathCanvas.SetActive(false);
        }
        
        // Ensure the final score text is transparent and scaled to zero at the start
        if (_finalScoreText != null)
        {
            _finalScoreText.alpha = 0f;
            _finalScoreText.transform.localScale = Vector3.zero;
        }

        // Ensure the high score text is transparent and scaled to zero at the start
        if (_highScoreText != null)
        {
            _highScoreText.alpha = 0f;
            _highScoreText.transform.localScale = Vector3.zero;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasDied) return;

        // Check if the collision is with the ground layer
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
        
        // Unparent the body parts to create a ragdoll effect
        UnparentAndEnableRagdoll(bikeRoot);
        
        // Start the death sequence
        HandlePlayerDeath();
    }
    
    private void UnparentAndEnableRagdoll(Transform root)
    {
        if (root == null)
        {
            Debug.LogError("bikeRoot is not assigned!");
            return;
        }

        // Disable joints in bikeRoot and children
        foreach (WheelJoint2D wheel in root.GetComponentsInChildren<WheelJoint2D>(true))
        {
            wheel.enabled = false;
        }

        foreach (HingeJoint2D hinge in root.GetComponentsInChildren<HingeJoint2D>(true))
        {
            hinge.enabled = false;
        }

        // Disable manually assigned joints
        foreach (var hinge in hingeJointsToDisable)
        {
            if (hinge != null) hinge.enabled = false;
        }

        foreach (var wheel in wheelJointsToDisable)
        {
            if (wheel != null) wheel.enabled = false;
        }
        
        // Unparent and ensure Rigidbody2D is added and active
        List<Transform> children = new List<Transform>();
        foreach (Transform child in root)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            child.SetParent(null); // Detach from parent

            // Add Rigidbody2D if missing
            Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = child.gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
        }
    }

    private void HandlePlayerDeath()
    {
        // Start the death sequence coroutine
        StartCoroutine(DeathSequence());
    }
    
    private IEnumerator DeathSequence()
    {
        // === 1. Slow down time ===
        Time.timeScale = 0.3f;

        // === 2. Clear all existing stunt pop-ups ===
        if (_stuntCarController != null && _stuntCarController._stuntDisplayParent != null)
        {
            foreach (Transform child in _stuntCarController._stuntDisplayParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        // === Disable the score UI canvas ===
        if (_scoreUIObject != null)
        {
            _scoreUIObject.SetActive(false);
        }
        
        // Wait a moment before showing text
        yield return new WaitForSecondsRealtime(0.5f);
        
        // === 4. Enable the death canvas and animate text ===
        if (_deathCanvas != null)
        {
            _deathCanvas.SetActive(true);
            
            // Wait for the "DEAD" text to finish its pop-in animation before starting the pulse
            if (_deathText != null)
            {
                yield return StartCoroutine(AnimateDeathText(_deathText, 0.75f, 0f, 1f));
                StartCoroutine(PulseText(_deathText, 0.05f, 0.5f));
            }

            // Animate the final score text to appear
            if (_finalScoreText != null)
            {
                _finalScoreText.text = $"Score: {StuntCarController.Score}";
                StartCoroutine(AnimateDeathText(_finalScoreText, 0.75f, 1.5f, 1f));
            }

            // Animate the high score text to appear 1.5 seconds after the final score
            if (_highScoreText != null)
            {
                // Check and update the high score
                int currentScore = StuntCarController.Score;
                int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

                if (currentScore > highScore)
                {
                    highScore = currentScore;
                    PlayerPrefs.SetInt(HighScoreKey, highScore);
                    PlayerPrefs.Save();
                }

                _highScoreText.text = $"High Score: {highScore}";
                StartCoroutine(AnimateDeathText(_highScoreText, 0.75f, 3.0f, 1f));
            }
        }
    }
    
    // Coroutine for a constant, minimal pulsing text effect
    private IEnumerator PulseText(TextMeshProUGUI text, float pulseScale, float duration)
    {
        Vector3 originalScale = text.transform.localScale;
        Vector3 pulseMaxScale = originalScale * (1f + pulseScale);

        while (true)
        {
            // Scale up
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.Lerp(originalScale, pulseMaxScale, timer / duration);
                yield return null;
            }

            // Scale down
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                text.transform.localScale = Vector3.Lerp(pulseMaxScale, originalScale, timer / duration);
                yield return null;
            }
        }
    }
    
    // Text animation for the death screen
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
