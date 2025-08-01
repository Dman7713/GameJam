using UnityEngine;
using TMPro; // Important: Add this to use TextMeshProUGUI

/// <summary>
/// A simple controller for a collectible coin.
/// This script handles the coin's hovering animation,
/// collision detection, and plays a particle and sound effect.
/// It also manages a static coin counter and updates a UI text element
/// directly, without needing to communicate with a separate script.
/// </summary>
public class CoinController : MonoBehaviour
{
    // --- Public variables for easy editing in the Unity Inspector ---
    [Header("Hover Animation Settings")]
    [Tooltip("The speed at which the coin hovers up and down.")]
    public float hoverSpeed = 1f;
    [Tooltip("The maximum distance the coin moves up and down from its starting position.")]
    public float hoverAmplitude = 0.2f;
    
    [Header("Coin Collection Settings")]
    [Tooltip("The tag of the GameObject that can collect the coin (e.g., 'Player').")]
    public string playerTag = "Player";
    
    [Header("Effects")]
    [Tooltip("The particle system prefab to spawn when the coin is collected.")]
    public GameObject collectEffectPrefab;
    
    [Header("Audio Settings")]
    [Tooltip("The AudioSource component on this GameObject.")]
    public AudioSource collectAudioSource;
    [Tooltip("The sound effect to play when the coin is collected.")]
    public AudioClip collectSoundClip;

    [Header("UI Counter Settings")]
    [Tooltip("The name of the top-level parent GameObject (e.g., 'ScoreCanvas').")]
    public string scoreCanvasParentName = "ScoreCanvas";
    [Tooltip("The name of the child GameObject that holds the coin display text (e.g., 'CoinDisplay').")]
    public string coinDisplayChildName = "CoinDisplay";
    
    [Tooltip("The format string for the coin counter text. Use {0} as a placeholder for the count.")]
    public string coinTextFormat = "Coins: {0}";

    [Header("Data Persistence")]
    [Tooltip("The PlayerPrefs key used to save and load the coin count.")]
    public string coinPlayerPrefsKey = "CoinsCollected";

    // --- Private and Static variables ---
    private Vector3 startPosition;
    private bool isCollected = false;
    
    // A static variable to store the total number of coins collected across all instances.
    private static int coinsCollected = 0;
    
    // A static reference to the TextMeshProUGUI object to display the coin count.
    private static TextMeshProUGUI coinText;

    // A flag to ensure static variables and UI are initialized only once.
    private static bool isInitialized = false;
    
    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize the coin's state.
    /// </summary>
    void Start()
    {
        // Store the coin's starting position for the hover animation.
        startPosition = transform.position;
        
        // This block will only run once for the first coin object created in the scene.
        if (!isInitialized)
        {
            // Find and set the UI text element.
            GameObject scoreCanvas = GameObject.Find(scoreCanvasParentName);
            if (scoreCanvas != null)
            {
                Transform coinDisplayTransform = scoreCanvas.transform.Find(coinDisplayChildName);
                if (coinDisplayTransform != null)
                {
                    coinText = coinDisplayTransform.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            // Load the coin count from PlayerPrefs.
            coinsCollected = PlayerPrefs.GetInt(coinPlayerPrefsKey, 0);

            // Set the initialization flag.
            isInitialized = true;
        }
        
        // Update the UI on start to show the loaded count.
        UpdateCoinDisplay();
    }

    /// <summary>
    /// Update is called once per frame.
    /// Used here to perform the subtle hovering animation.
    /// </summary>
    void Update()
    {
        // Only perform the animation if the coin has not been collected yet.
        if (!isCollected)
        {
            // Use a sine wave to create a smooth, looping up and down motion.
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }
    }

    /// <summary>
    /// OnTriggerEnter2D is called when another 2D collider enters the trigger collider attached to this object.
    /// </summary>
    /// <param name="other">The other Collider2D involved in the collision.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object has the specified player tag and the coin hasn't been collected yet.
        if (other.CompareTag(playerTag) && !isCollected)
        {
            // Mark the coin as collected to prevent multiple triggers.
            isCollected = true;

            // --- UI and Data Persistence Logic ---
            // Increment the static coin count and save it to PlayerPrefs.
            coinsCollected++;
            PlayerPrefs.SetInt(coinPlayerPrefsKey, coinsCollected);
            UpdateCoinDisplay();

            // Hide the coin's sprite and collider immediately.
            Renderer coinRenderer = GetComponent<Renderer>();
            if (coinRenderer != null)
            {
                coinRenderer.enabled = false;
            }
            Collider2D coinCollider = GetComponent<Collider2D>();
            if (coinCollider != null)
            {
                coinCollider.enabled = false;
            }
            
            // A variable to track the longest duration of all effects.
            float destroyDelay = 0f;

            // --- Audio Logic ---
            if (collectAudioSource != null && collectSoundClip != null)
            {
                collectAudioSource.PlayOneShot(collectSoundClip);
                // The delay must be at least as long as the audio clip.
                destroyDelay = collectSoundClip.length;
            }

            // --- Effects Logic ---
            if (collectEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);

                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    // Explicitly play the particle system.
                    ps.Play();
                    
                    // Set the particle effect to self-destruct after its duration.
                    float particleDuration = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(effect, particleDuration + 0.1f);
                    
                    // The main destroy delay must be long enough for both the audio and particles.
                    if (particleDuration > destroyDelay)
                    {
                        destroyDelay = particleDuration;
                    }
                }
            }

            // The coin GameObject is destroyed after the longest effect (audio or particles) is done.
            Destroy(gameObject, destroyDelay + 0.1f);
        }
    }
    
    /// <summary>
    /// Updates the UI text with the current coin count.
    /// </summary>
    private void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = string.Format(coinTextFormat, coinsCollected);
        }
    }
}
