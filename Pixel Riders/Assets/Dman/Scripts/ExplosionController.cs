using UnityEngine;
using System.Collections;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Explosion Effects")]
    [SerializeField] private GameObject explosionParticlesPrefab;
    [SerializeField] private GameObject secondaryParticlesPrefab;

    [Header("Explosion Physics")]
    [SerializeField] private float explosionForce = 2000f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private string playerTag = "Player";

    [Header("Visual & Audio Effects")]
    [SerializeField] private float cameraShakeDuration = 0.8f;
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [SerializeField] private AudioClip explosionSound;

    private bool isExploded = false;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Please ensure your camera has the 'MainCamera' tag!");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);

        if (isExploded || !collision.gameObject.CompareTag(playerTag))
            return;

        Explode();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger detected with: " + collision.gameObject.name);

        if (isExploded || !collision.gameObject.CompareTag(playerTag))
            return;

        Explode();
    }

    public void Explode()
    {
        if (isExploded) return;

        isExploded = true;
        Debug.Log("Explosive barrel detonated!");

        PlayExplosionEffects();

        ApplyExplosionForce();

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        StartCoroutine(ShakeCamera(cameraShakeDuration, cameraShakeIntensity));

        Destroy(gameObject, 1.5f);
    }

    private void PlayExplosionEffects()
    {
        if (explosionParticlesPrefab != null)
        {
            var mainParticles = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = mainParticles.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(mainParticles, 3f);
            Debug.Log("Playing main explosion particles.");
        }
        else
        {
            Debug.LogWarning("Explosion Particles Prefab is not assigned!");
        }

        if (secondaryParticlesPrefab != null)
        {
            var secondaryParticles = Instantiate(secondaryParticlesPrefab, transform.position, Quaternion.identity);
            ParticleSystem ps = secondaryParticles.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(secondaryParticles, 3f);
            Debug.Log("Playing secondary explosion particles.");
        }
    }

    private void ApplyExplosionForce()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        bool playerHit = false;

        foreach (Collider2D hit in colliders)
        {
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = rb.transform.position - transform.position;
                rb.AddForce(direction.normalized * explosionForce, ForceMode2D.Impulse);
            }

            if (hit.CompareTag(playerTag))
            {
                playerHit = true;
                Debug.Log("Player hit by explosion, triggering death.");
                if (DeathManager.Instance != null)
                {
                    DeathManager.Instance.TriggerDeath();
                }
                else
                {
                    Debug.LogError("DeathManager.Instance not found!");
                }
            }
        }

        if (!playerHit)
            Debug.LogWarning("No player detected in explosion radius!");
    }

    private IEnumerator ShakeCamera(float duration, float intensity)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Cannot shake camera, main camera is null!");
            yield break;
        }

        Vector3 originalPos = mainCamera.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * intensity;
            mainCamera.transform.position = originalPos + new Vector3(shakeOffset.x, shakeOffset.y, 0);

            timer += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }
}
