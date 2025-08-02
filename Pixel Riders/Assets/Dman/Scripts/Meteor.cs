using UnityEngine;
using System.Collections;

public class Meteor : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The prefab for the warning sprite that appears on the ground.")]
    public GameObject warningSpritePrefab;

    [Header("Meteor Settings")]
    [Tooltip("The speed at which the meteor falls.")]
    public float fallSpeed = 10f;
    [Tooltip("How long the warning sprite is displayed before the meteor falls.")]
    public float warningTime = 2f;
    [Tooltip("The vertical offset for the warning sprite to appear above the ground.")]
    public float warningSpriteOffset = 1.5f;
    [Tooltip("The radius from the player at which the landed meteor will be destroyed.")]
    public float cleanupRadius = 100f;

    [Header("Warning Shake Settings")]
    [Tooltip("Radius from the player within which the warning shake starts.")]
    public float warningShakeRadius = 20f;
    [Tooltip("Duration of the shake before impact.")]
    public float warningShakeDuration = 0.5f;
    [Tooltip("Magnitude (intensity) of the shake before impact.")]
    public float warningShakeMagnitude = 0.2f;

    [Header("Camera Shake")]
    [Tooltip("How long the screen will shake on impact.")]
    public float cameraShakeDuration = 0.5f;
    [Tooltip("How intense the camera shake is.")]
    public float cameraShakeMagnitude = 1.5f;

    private GameObject warningSpriteInstance;
    private Vector2 targetPosition; // Store the exact landing spot
    private bool isFalling = false;
    private bool hasLanded = false;
    private ParticleSystem landingParticles;
    private Transform playerTransform;
    private Rigidbody2D rb;

    private Vector2 fallDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        landingParticles = GetComponentInChildren<ParticleSystem>();
        if (landingParticles != null)
            landingParticles.Stop(true);
    }

    private void Start()
    {
        // Choose a random angle roughly downward (270° ± 15°)
        float angleDegrees = Random.Range(-15f, 15f) + 270f;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // Calculate fall direction vector
        fallDirection = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

        // Rotate meteor visually to match fall angle
        transform.rotation = Quaternion.Euler(0, 0, angleDegrees);

        // Find player by tag
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            playerTransform = playerObject.transform;

        // Start high up
        transform.position = new Vector2(transform.position.x, 50f);

        // Predict where it will land along fallDirection
        PredictLandingSpot();
    }

    private void Update()
    {
        // Cleanup if landed and player far away
        if (hasLanded && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > cleanupRadius)
                Destroy(gameObject);
        }
    }

    private void PredictLandingSpot()
    {
        int groundLayer = LayerMask.GetMask("groundLayer");

        RaycastHit2D hit = Physics2D.Raycast(transform.position, fallDirection, Mathf.Infinity, groundLayer);

        if (hit.collider != null)
        {
            targetPosition = hit.point;

            if (warningSpritePrefab != null)
            {
                Vector3 warningPos = new Vector3(targetPosition.x, targetPosition.y + warningSpriteOffset, 0);
                warningSpriteInstance = Instantiate(warningSpritePrefab, warningPos, Quaternion.identity);
                StartCoroutine(HoverWarningSprite());
            }

            StartCoroutine(StartFallAfterDelay());
        }
        else
        {
            Debug.LogWarning("Meteor could not find ground. Destroying itself.");
            Destroy(gameObject);
        }
    }

    private IEnumerator StartFallAfterDelay()
    {
        // Start warning shake if player is near the predicted landing spot
        if (playerTransform != null && Vector2.Distance(targetPosition, playerTransform.position) <= warningShakeRadius)
        {
            StartCoroutine(WarningShakeRoutine());
        }

        yield return new WaitForSeconds(warningTime);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // Disable gravity, we'll control velocity manually
            rb.linearVelocity = fallDirection * fallSpeed;
        }

        isFalling = true;
    }

    private IEnumerator HoverWarningSprite()
    {
        if (warningSpriteInstance == null) yield break;

        Vector3 startPos = warningSpriteInstance.transform.position;
        float timer = 0f;
        float hoverSpeed = 5f;
        float hoverHeight = 0.5f;

        while (warningSpriteInstance != null)
        {
            timer += Time.deltaTime * hoverSpeed;
            float yOffset = Mathf.Sin(timer) * hoverHeight;
            warningSpriteInstance.transform.position = startPos + new Vector3(0, yOffset, 0);
            yield return null;
        }
    }

    private IEnumerator WarningShakeRoutine()
    {
        float timer = 0f;

        while (timer < warningShakeDuration)
        {
            timer += Time.deltaTime;
            // Shake intensity ramps up and down smoothly
            float intensity = Mathf.Sin((timer / warningShakeDuration) * Mathf.PI);
            CameraShake.Instance?.Shake(0.1f, warningShakeMagnitude * intensity);
            yield return null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // Kill player only if meteor is falling
            if (isFalling)
            {
                if (DeathManager.Instance != null)
                    DeathManager.Instance.TriggerDeath();
            }
        }
        else
        {
            // If meteor hits ground or other surface, freeze it and play effects
            if (((1 << collision.gameObject.layer) & LayerMask.GetMask("groundLayer")) != 0)
            {
                LandMeteor();
            }
        }
    }

    private void LandMeteor()
    {
        isFalling = false;
        hasLanded = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D meteorCollider = GetComponent<Collider2D>();
        if (meteorCollider != null)
            meteorCollider.enabled = false;

        // Destroy warning sprite
        if (warningSpriteInstance != null)
            Destroy(warningSpriteInstance);

        // Play landing particles
        if (landingParticles != null)
            landingParticles.Play(true);

        // Camera shake on impact if near player
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= warningShakeRadius)
        {
            CameraShake.Instance?.Shake(cameraShakeDuration, cameraShakeMagnitude);
        }
    }
}
