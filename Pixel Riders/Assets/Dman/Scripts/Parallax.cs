using UnityEngine;

/// <summary>
/// This script handles parallax scrolling for a background layer.
/// Attach it to any GameObject that should move with the camera,
/// but at a different speed to create a sense of depth.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    // [SerializeField] makes private fields visible and editable in the Inspector.
    // We also keep them public for direct access if needed, but [SerializeField] is explicit.

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

    private Transform cameraTransform;
    private Vector3 lastCameraPosition; // Stores the camera's position from the previous frame

    private float spriteWidth; // The width of the sprite for looping calculations
    private float startX;      // The initial X position of the layer for looping

    void Start()
    {
        // Find the main camera in the scene. Ensure your camera is tagged "MainCamera".
        cameraTransform = Camera.main.transform;
        
        // Store the camera's initial position to calculate movement delta.
        lastCameraPosition = cameraTransform.position;

        // If looping is enabled, get the width of the sprite.
        if (isLoopingHorizontal)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // Get the world unit width of the sprite
                spriteWidth = spriteRenderer.bounds.size.x;
                // Store the initial X position for resetting when looping
                startX = transform.position.x;
            }
            else
            {
                Debug.LogWarning("ParallaxLayer: isLoopingHorizontal is true but no SpriteRenderer or sprite found on " + gameObject.name);
                isLoopingHorizontal = false; // Disable looping if no sprite is found
            }
        }
    }

    /// <summary>
    /// LateUpdate is called after all Update functions have been called.
    /// This ensures the camera has already moved for the current frame,
    /// so we can accurately calculate the parallax movement.
    /// </summary>
    void LateUpdate()
    {
        // Calculate the change in camera's position since the last frame.
        Vector3 deltaCameraMovement = cameraTransform.position - lastCameraPosition;

        // Calculate the new position for this layer based on the parallax factor.
        // We only apply parallax movement on the X and Y axes for 2D games.
        // The Z-axis (depth) should remain constant for sorting layers.
        float newX = transform.position.x + deltaCameraMovement.x * parallaxFactor;
        float newY = transform.position.y + deltaCameraMovement.y * parallaxFactor;

        // Update the layer's position.
        transform.position = new Vector3(newX, newY, transform.position.z);

        // Store the current camera position for the next frame's calculation.
        lastCameraPosition = cameraTransform.position;

        // Handle horizontal looping if enabled.
        if (isLoopingHorizontal)
        {
            // Calculate how far the camera has moved relative to the layer's start.
            // This calculation ensures the parallax effect is consistent with the looping.
            float distanceMoved = cameraTransform.position.x * (1 - parallaxFactor);

            // If the camera has moved past half the sprite's width from its effective start,
            // move the layer to the other side to create a seamless loop.
            if (distanceMoved > startX + spriteWidth / 2)
            {
                startX += spriteWidth; // Move the effective start point forward
            }
            else if (distanceMoved < startX - spriteWidth / 2)
            {
                startX -= spriteWidth; // Move the effective start point backward
            }
            
            // Recalculate the layer's position based on the updated effective startX.
            // This keeps the layer correctly positioned relative to the camera and its loop.
            transform.position = new Vector3(startX + (cameraTransform.position.x * parallaxFactor), transform.position.y, transform.position.z);
        }
    }
}
