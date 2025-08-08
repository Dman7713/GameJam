using UnityEngine;
using TMPro;

/// <summary>
/// A simple controller for a collectible coin.
/// This script handles the coin's hovering animation,
/// collision detection, and plays a particle and sound effect.
/// It now directly communicates with the DataManager to update the
/// player's coin count.
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

    // --- Private variables ---
    private Vector3 startPosition;
    private bool isCollected = false;
    private DriverController player;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to initialize the coin's state.
    /// </summary>
    void Start()
    {
        // Store the coin's starting position for the hover animation.
        startPosition = transform.position;
        player = GameObject.Find("Bike Controller").GetComponent<DriverController>();
    }

    /// <summary>
    /// Update is called once per frame.
    /// Used here to perform the subtle hovering animation.
    /// </summary>/// 
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
        Debug.Log(player.IsDead);
        if (player.IsDead) { return; }
        // Check if the colliding object has the specified player tag and the coin hasn't been collected yet.
        if (other.CompareTag(playerTag) && !isCollected)
        {
            // Mark the coin as collected to prevent multiple triggers.
            isCollected = true;

            // --- UI and Data Persistence Logic ---
            // We are no longer managing a static coin count. Instead, we
            // add a coin directly to the DataManager.
            if (DataManager.Instance != null)
            {
                DataManager.Instance.AddCoins(1);
            }
            else
            {
                Debug.LogError("DataManager.Instance is null! Cannot add coins.");
            }

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
}
