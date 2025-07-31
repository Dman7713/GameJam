using UnityEngine;

/// <summary>
/// This script handles parallax scrolling for a background layer.
/// Attach it to any GameObject that should move with the camera,
/// but at a different speed to create a sense of depth.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Determines how much this layer moves relative to the camera. " +
             "0 = no movement (fixed in world space). " +
             "1 = moves exactly with the camera (like foreground elements). " +
             "Values between 0 and 1 create the parallax effect (further layers move less).")]
    [Range(0f, 1f)] // Restrict parallax factor between 0 and 1 for intuitive control
    public float parallaxFactor;

    [SerializeField]
    [Tooltip("Set this to true if this layer should loop horizontally (e.g., for continuous backgrounds).")]
    public bool isLoopingHorizontal = false;

    [SerializeField]
    [Tooltip("Amount of overlap (in world units) between looping sprites to prevent gaps. " +
             "For pixel art, this is typically 1 divided by your Pixels Per Unit (PPU) setting, " +
             "or significantly more if gaps are still visible due to rendering precision or desired visual effect.")]
    [Range(0f, 20f)] // Increased range to allow for much more aggressive overlap
    public float pixelOverlap = 20f; // New default value set to 20f

    private Transform cameraTransform;
    private Vector3 lastCameraPosition; // Stores the camera's position from the previous frame

    // Variables for two-sprite looping
    private float spriteWidth; // The width of the sprite in world units
    private GameObject otherSpriteInstance; // Reference to the other sprite instance for looping

    // Flag to prevent the twin from creating another twin
    private bool isTwinInstance = false;

    void Awake()
    {
        // Check if this instance was created by another ParallaxLayer script
        // This is a simple way to identify the twin and prevent it from re-initializing the loop
        if (gameObject.name.Contains("_LoopInstance"))
        {
            isTwinInstance = true;
        }
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position; // Initialize lastCameraPosition here

        // Only the original instance should create the twin, and only if it hasn't been created yet.
        if (isLoopingHorizontal && !isTwinInstance && otherSpriteInstance == null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                spriteWidth = spriteRenderer.bounds.size.x;

                // Create the second instance of this sprite
                otherSpriteInstance = new GameObject(gameObject.name + "_LoopInstance");
                SpriteRenderer otherSpriteRenderer = otherSpriteInstance.AddComponent<SpriteRenderer>();
                otherSpriteRenderer.sprite = spriteRenderer.sprite;
                otherSpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                otherSpriteRenderer.sortingOrder = spriteRenderer.sortingOrder;
                otherSpriteRenderer.flipX = spriteRenderer.flipX; // Copy flip state
                otherSpriteRenderer.flipY = spriteRenderer.flipY; // Copy flip state
                otherSpriteRenderer.color = spriteRenderer.color; // Copy color/alpha

                // Position the second instance immediately next to the first, with overlap
                otherSpriteInstance.transform.position = new Vector3(transform.position.x + spriteWidth - pixelOverlap, transform.position.y, transform.position.z);
                otherSpriteInstance.transform.parent = transform.parent; // Keep same parent for organization
                otherSpriteInstance.transform.localScale = transform.localScale; // Ensure scale is copied

                // Debug.Log($"Created twin for {gameObject.name}: {otherSpriteInstance.name}"); // For debugging
            }
            else
            {
                Debug.LogWarning("ParallaxLayer: isLoopingHorizontal is true but no SpriteRenderer or sprite found on " + gameObject.name + ". Disabling looping for this instance.");
                isLoopingHorizontal = false; // Disable looping if no sprite is found
            }
        }
        else if (isTwinInstance)
        {
            // If this is a twin, it doesn't need to do anything in Start()
            // Its position will be managed by the original instance.
            // Debug.Log($"Twin instance {gameObject.name} initialized."); // For debugging
        }
    }

    void LateUpdate()
    {
        // If this is a twin instance, it's managed by the original, so it doesn't need to update itself.
        if (isTwinInstance) return;

        // Calculate the camera's movement delta based on its last frame position.
        Vector3 deltaCameraMovement = cameraTransform.position - lastCameraPosition;
        
        // Apply parallax movement to the current object's position.
        // This moves the object based on the camera's movement and its parallax factor.
        transform.position += new Vector3(deltaCameraMovement.x * parallaxFactor, deltaCameraMovement.y * parallaxFactor, 0); // Only X and Y for 2D

        // Update last camera position for the next frame's calculation.
        lastCameraPosition = cameraTransform.position;

        // Handle horizontal looping if enabled and the twin exists.
        if (isLoopingHorizontal && otherSpriteInstance != null)
        {
            // The twin sprite also needs to move with parallax, as it doesn't run its own LateUpdate.
            // This is crucial because the twin doesn't have its own ParallaxLayer script running.
            otherSpriteInstance.transform.position += new Vector3(deltaCameraMovement.x * parallaxFactor, deltaCameraMovement.y * parallaxFactor, 0);

            // Determine the camera's world space edges
            float cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
            float cameraLeftEdge = cameraTransform.position.x - cameraHalfWidth;
            float cameraRightEdge = cameraTransform.position.x + cameraHalfWidth;

            // Check if the current sprite has moved entirely off-screen to the left
            // The condition is that the right edge of the sprite is to the left of the camera's left edge
            if (transform.position.x + spriteWidth < cameraLeftEdge)
            {
                // Move this sprite to the right of the other sprite, with overlap
                transform.position = new Vector3(otherSpriteInstance.transform.position.x + spriteWidth - pixelOverlap, transform.position.y, transform.position.z);
            }
            // Check if the current sprite has moved entirely off-screen to the right
            // The condition is that the left edge of the sprite is to the right of the camera's right edge
            else if (transform.position.x > cameraRightEdge) // This condition was previously `cameraRightEdge + spriteWidth`, which was too far
            {
                // Move this sprite to the left of the other sprite, with overlap
                transform.position = new Vector3(otherSpriteInstance.transform.position.x - spriteWidth + pixelOverlap, transform.position.y, transform.position.z);
            }
        }
    }

    void OnDestroy()
    {
        // Clean up the dynamically created twin instance when the original is destroyed
        // Ensure this only happens for the original instance managing the twin
        if (isLoopingHorizontal && !isTwinInstance && otherSpriteInstance != null)
        {
            // Check if the other instance is still active in the scene before destroying
            if (otherSpriteInstance.activeInHierarchy)
            {
                // To prevent issues if the twin tries to destroy its non-existent 'otherSpriteInstance'
                // when the original is being destroyed.
                // This script is generic and should not have a ParallaxLayer component on the twin.
                // So, we just destroy the GameObject directly.
                Destroy(otherSpriteInstance);
            }
        }
        // Note: The Material cleanup for PixelFogController is handled in PixelFogController.cs
        // This ParallaxLayer script is generic and should not handle material specific cleanup.
    }
}
