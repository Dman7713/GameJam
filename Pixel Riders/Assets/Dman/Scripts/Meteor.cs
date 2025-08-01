// Meteor.cs
// Attach this script to your meteor prefab.
// Ensure you have a collider (e.g., CircleCollider2D or BoxCollider2D) and a Rigidbody2D.
// You'll also need to create a "groundLayer" in Unity's Layer Manager for the prediction to work.

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

    [Header("Camera Shake")]
    [Tooltip("How long the screen will shake on impact.")]
    public float cameraShakeDuration = 0.5f;
    [Tooltip("How intense the camera shake is.")]
    public float cameraShakeMagnitude = 10f;

    private GameObject warningSpriteInstance;
    private Vector2 targetPosition; // Store the exact landing spot
    private bool isFalling = false;
    private bool hasLanded = false; // New state for when the meteor has hit the ground
    private ParticleSystem landingParticles;
    private Transform playerTransform;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Get the Rigidbody2D component.
        rb = GetComponent<Rigidbody2D>();

        // Find the child particle system and make sure it's not playing initially.
        landingParticles = GetComponentInChildren<ParticleSystem>();
        if (landingParticles != null)
        {
            landingParticles.Stop(true);
        }
    }

    private void Start()
    {
        // Give the meteor a random rotation on the Z-axis when it spawns.
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // Find the player object by tag to get a reference for cleanup logic.
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }

        // Set the meteor's initial position
        transform.position = new Vector2(transform.position.x, 50f);

        // Predict the landing spot as soon as the meteor is spawned
        PredictLandingSpot();
    }

    private void Update()
    {
        // Check for player distance and destroy the meteor if it has landed and the player is far away
        if (hasLanded && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > cleanupRadius)
            {
                Destroy(gameObject);
            }
        }
    }

    private void PredictLandingSpot()
    {
        // Fire a raycast down to find the ground
        int groundLayer = LayerMask.GetMask("groundLayer");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, groundLayer);

        if (hit.collider != null)
        {
            targetPosition = hit.point;

            // Only instantiate the warning sprite if the prefab is assigned
            if (warningSpritePrefab != null)
            {
                // Instantiate the warning sprite with an offset to appear above the ground.
                Vector3 warningPos = new Vector3(targetPosition.x, targetPosition.y + warningSpriteOffset, 0);
                warningSpriteInstance = Instantiate(warningSpritePrefab, warningPos, Quaternion.identity);
                StartCoroutine(HoverWarningSprite());
            }

            // Wait for the warning time before starting the fall.
            // The meteor remains stationary until this coroutine finishes.
            StartCoroutine(StartFallAfterDelay());
        }
        else
        {
            // If no ground is found, destroy the meteor
            Debug.LogWarning("Meteor could not find ground. Destroying itself.");
            Destroy(gameObject);
        }
    }

    private IEnumerator StartFallAfterDelay()
    {
        // Wait for the specified warning time
        yield return new WaitForSeconds(warningTime);
        
        // Change the Rigidbody2D type to Dynamic so it can collide and use physics.
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1.0f; // Ensure gravity is applied
        }
        isFalling = true;
    }

    private IEnumerator HoverWarningSprite()
    {
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

    // OnCollisionEnter2D is now the single source of truth for all impact logic.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the meteor has already landed, ignore any further collisions.
        if (hasLanded)
        {
            return;
        }
        
        // Check if the collision is with the player.
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Meteor hit player. Triggering death.");
            if (DeathManager.Instance != null)
            {
                DeathManager.Instance.TriggerDeath();
            }
        }

        // The meteor has now landed, regardless of what it hit, so execute the impact sequence.
        isFalling = false;
        hasLanded = true;
        
        Debug.Log("Collision detected with: " + collision.gameObject.name);

        // Freeze the meteor in place by stopping its physics.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        // Disable the collider to prevent the landed meteor from affecting other objects.
        Collider2D meteorCollider = GetComponent<Collider2D>();
        if (meteorCollider != null)
        {
            meteorCollider.enabled = false;
        }
        
        // Trigger the screenshake
        CameraShake.Instance.ShakeCamera(cameraShakeDuration, cameraShakeMagnitude);

        // Destroy the warning sprite
        if (warningSpriteInstance != null)
        {
            Destroy(warningSpriteInstance);
        }

        // Play the particle effect.
        if (landingParticles != null)
        {
            landingParticles.Play(true);
        }
    }
}