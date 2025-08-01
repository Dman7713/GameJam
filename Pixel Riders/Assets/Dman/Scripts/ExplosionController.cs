using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This script should be attached to the explosive barrel GameObject.
public class ExplosiveBarrel : MonoBehaviour
{
    // === Inspector Variables ===
    [Header("Explosion Effects")]
    [Tooltip("The particle system prefab for the main explosion.")]
    [SerializeField] private GameObject explosionParticlesPrefab;
    [Tooltip("An optional second particle system prefab for more flair.")]
    [SerializeField] private GameObject secondaryParticlesPrefab;

    [Header("Explosion Physics")]
    [Tooltip("The force applied to objects' Rigidbody2D within the explosion radius. Increased for a more dramatic effect.")]
    [SerializeField] private float explosionForce = 2000f; // Increased default force
    [Tooltip("The radius of the explosion. Objects within this radius will be affected. Increased for a more dramatic effect.")]
    [SerializeField] private float explosionRadius = 6f; // Increased default radius
    [Tooltip("The tag of the player or other destructible objects.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Visual & Audio Effects")]
    [Tooltip("The duration of the camera shake effect.")]
    [SerializeField] private float cameraShakeDuration = 0.8f;
    [Tooltip("The intensity of the camera shake effect. Increased from 0.2f to 0.5f.")]
    [SerializeField] private float cameraShakeIntensity = 0.5f; // Increased for a more visible effect
    [Tooltip("The sound effect to play on explosion (optional).")]
    [SerializeField] private AudioClip explosionSound;

    // === Private Variables ===
    private bool isExploded = false;
    private Camera mainCamera;
    
    // NOTE: We no longer need originalCameraPosition as a class-level variable.
    // The coroutine will now get the camera's position at the start of the shake.

    // === MonoBehaviour Methods ===
    private void Start()
    {
        // Cache a reference to the main camera at the start.
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Please ensure your camera has the 'MainCamera' tag!");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Debugging line to see if any collision is detected.
        Debug.Log("Collision detected with: " + collision.gameObject.name);

        // We now check the collision object's tag instead of its layer.
        // The comparison is more direct: if it has the correct tag and hasn't exploded yet.
        if (isExploded || !collision.gameObject.CompareTag(playerTag))
        {
            return;
        }

        // Trigger the explosion sequence.
        Explode();
    }

    // === Public Methods ===
    // This public method allows other scripts to trigger the explosion as well.
    public void Explode()
    {
        isExploded = true;
        Debug.Log("Explosive barrel detonated!");

        // 1. Play the explosion effects.
        PlayExplosionEffects();

        // 2. Apply force to all Rigidbodies within the explosion radius and trigger player death.
        ApplyExplosionForce();

        // 3. Play an audio clip for a satisfying sound.
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // 4. Start the camera shake effect.
        StartCoroutine(ShakeCamera(cameraShakeDuration, cameraShakeIntensity));

        // 5. Destroy the barrel GameObject after a short delay.
        Destroy(gameObject, 1.5f);
    }

    // === Private Helper Methods ===
    private void PlayExplosionEffects()
    {
        // Main Particles
        if (explosionParticlesPrefab != null)
        {
            // Instantiate the particles as a temporary object so they are not destroyed with the barrel.
            var mainParticles = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
            
            // Get the particle system component and ensure it plays.
            ParticleSystem ps = mainParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                
                // Destroy the particle object after a fixed time.
                // This ensures the particles have time to play out their full lifetime.
                Destroy(mainParticles, 3f); 
            }
            else
            {
                // If it doesn't have a particle system, destroy it after a fixed time.
                Destroy(mainParticles, 3f);
            }
            Debug.Log("Playing main explosion particles.");
        }
        else
        {
            Debug.LogWarning("Explosion Particles Prefab is not assigned on the ExplosiveBarrel script!");
        }

        // Secondary Particles
        if (secondaryParticlesPrefab != null)
        {
            var secondaryParticles = Instantiate(secondaryParticlesPrefab, transform.position, Quaternion.identity);
            
            ParticleSystem ps = secondaryParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                
                Destroy(secondaryParticles, 3f);
            }
            else
            {
                Destroy(secondaryParticles, 3f);
            }
            Debug.Log("Playing secondary explosion particles.");
        }
        else
        {
            Debug.LogWarning("Secondary Particles Prefab is not assigned on the ExplosiveBarrel script!");
        }
    }

    private void ApplyExplosionForce()
    {
        // Find all colliders within the explosion radius.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hit in colliders)
        {
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = rb.transform.position - transform.position;
                rb.AddForce(direction.normalized * explosionForce, ForceMode2D.Impulse);
            }

            // Check if the hit object is the player and trigger death.
            if (hit.CompareTag(playerTag))
            {
                if (DeathManager.Instance != null)
                {
                    Debug.Log("Player part hit, triggering death.");
                    DeathManager.Instance.TriggerDeath();
                }
                else
                {
                    Debug.LogError("DeathManager.Instance not found when attempting to trigger player death!");
                }
            }
        }
    }

    private IEnumerator ShakeCamera(float duration, float intensity)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Cannot shake camera. Main Camera is not set!");
            yield break;
        }

        // Cache the camera's original position at the start of the coroutine.
        Vector3 originalPos = mainCamera.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * intensity;
            mainCamera.transform.position = originalPos + new Vector3(shakeOffset.x, shakeOffset.y, 0);

            timer += Time.deltaTime;
            yield return null;
        }

        // Return the camera to its original position after the shake is complete.
        mainCamera.transform.position = originalPos;
    }
}
