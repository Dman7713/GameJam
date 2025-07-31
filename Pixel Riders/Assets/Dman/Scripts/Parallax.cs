using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// This script handles parallax scrolling for a background layer.
/// Attach it to ONE GameObject per parallax layer that should move with the camera,
/// but at a different speed to create a sense of depth.
/// It will automatically create and manage additional copies for seamless horizontal looping.
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
    public float pixelOverlap = 0.0625f; // Default to 1 pixel overlap for 16 PPU (1/16)

    [SerializeField]
    [Tooltip("Number of copies of this sprite to maintain for seamless looping. " +
             "Typically 3 (original + 2 copies) is sufficient to cover the screen.")]
    [Range(2, 5)] // Allow 2 to 5 copies
    public int numLoopingCopies = 3; // Default to 3 copies for robust looping

    private Transform cameraTransform;
    private float initialCameraX; // Stores the camera's X position at Start
    private float initialLayerX;  // Stores the original layer's X position at Start

    private float spriteWidth; // The width of the sprite in world units

    // List to hold all instances (original + copies) managed by this script.
    private List<GameObject> loopingInstances = new List<GameObject>();

    // Flag to ensure only the original GameObject creates and manages the copies.
    private bool isOriginalManager = true;

    void Awake()
    {
        // If this GameObject was dynamically created by another ParallaxLayer script,
        // it's a copy and should not act as the manager.
        if (gameObject.name.Contains("_LoopInstance"))
        {
            isOriginalManager = false;
        }
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
        initialCameraX = cameraTransform.position.x; // Store initial camera X for parallax calculation
        initialLayerX = transform.position.x;        // Store initial layer X for relative positioning

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
        }
        else
        {
            // If no sprite, set a default width to prevent division by zero in LateUpdate
            spriteWidth = 1f; 
            Debug.LogWarning("ParallaxLayer: No SpriteRenderer or sprite found on " + gameObject.name + ". Using default spriteWidth of 1 for parallax calculations.");
        }

        // Only the original manager instance should create and manage the copies.
        if (isLoopingHorizontal && isOriginalManager)
        {
            // Add the original GameObject to the list of instances.
            loopingInstances.Add(gameObject);

            // Create the additional copies and position them.
            for (int i = 1; i < numLoopingCopies; i++)
            {
                GameObject newInstance = new GameObject(gameObject.name + "_LoopInstance_" + i);
                SpriteRenderer newSpriteRenderer = newInstance.AddComponent<SpriteRenderer>();
                newSpriteRenderer.sprite = spriteRenderer.sprite;
                newSpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                newSpriteRenderer.sortingOrder = spriteRenderer.sortingOrder;
                newSpriteRenderer.flipX = spriteRenderer.flipX;
                newSpriteRenderer.flipY = spriteRenderer.flipY;
                newSpriteRenderer.color = spriteRenderer.color;

                // Position the new instance to the right of the previous one, with overlap.
                // We reference the X position of the *last* instance added to the list.
                newInstance.transform.position = new Vector3(loopingInstances[i-1].transform.position.x + spriteWidth - pixelOverlap, transform.position.y, transform.position.z);
                newInstance.transform.parent = transform.parent; // Keep same parent for organization
                newInstance.transform.localScale = transform.localScale; // Ensure scale is copied

                loopingInstances.Add(newInstance); // Add to the list
            }
        }
    }

    void LateUpdate()
    {
        // Only the original manager instance handles the updates for all copies.
        if (!isOriginalManager) return;

        // Calculate the total horizontal distance the camera has moved from its starting point.
        float cameraTravelX = cameraTransform.position.x - initialCameraX;

        // Calculate the parallax-adjusted offset for this layer.
        // This is the total distance this layer should have moved based on camera travel and parallax factor.
        float parallaxOffset = cameraTravelX * parallaxFactor;

        // Calculate the X position of the "base" point for the looping system.
        // This is where the first sprite in the conceptual loop should be, relative to its initial position.
        // Mathf.Repeat ensures this value loops within the range of a single sprite width.
        float currentLoopX = initialLayerX + Mathf.Repeat(parallaxOffset, spriteWidth);

        // Adjust the currentLoopX to account for negative parallaxOffset (moving left)
        // Mathf.Repeat handles positive modulo, but for negative inputs, it might not behave as expected for wrapping.
        // This ensures correct wrapping when moving left.
        if (parallaxOffset < 0)
        {
            currentLoopX -= spriteWidth; // Shift back one sprite width if moving left
        }
        
        // Position the original sprite based on the calculated currentLoopX.
        transform.position = new Vector3(currentLoopX, transform.position.y, transform.position.z);

        // Position all other looping instances relative to the original.
        // This ensures they are always laid out correctly and continuously.
        if (isLoopingHorizontal)
        {
            // Position subsequent sprites relative to the one before them.
            // The original sprite (loopingInstances[0]) is already positioned.
            for (int i = 0; i < loopingInstances.Count; i++)
            {
                // Ensure the current instance is valid before accessing its transform.
                if (loopingInstances[i] == null) continue;

                // For the first instance (i=0), its position is already set above.
                // For subsequent instances, position them relative to the one before.
                if (i > 0)
                {
                    loopingInstances[i].transform.position = new Vector3(
                        loopingInstances[i-1].transform.position.x + spriteWidth - pixelOverlap,
                        loopingInstances[i-1].transform.position.y,
                        loopingInstances[i-1].transform.position.z
                    );
                }
            }

            // Now, handle the "wrapping" of the entire set of sprites.
            // When the leftmost sprite moves entirely off-screen to the left,
            // move it to the right of the rightmost sprite.
            // We need to find the current leftmost and rightmost sprites.
            // Sorting ensures we always have the correct order.
            loopingInstances.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            GameObject currentLeftmost = loopingInstances[0];
            GameObject currentRightmost = loopingInstances[loopingInstances.Count - 1];

            // Define a proactive repositioning threshold based on the camera's view.
            // We want to reposition when the leftmost sprite is about to leave the camera's view (on the left).
            // This ensures the next sprite is already visible.
            float cameraViewLeft = cameraTransform.position.x - (Camera.main.orthographicSize * Camera.main.aspect);
            float cameraViewRight = cameraTransform.position.x + (Camera.main.orthographicSize * Camera.main.aspect);

            // If the leftmost sprite's right edge is behind the camera's left edge (plus a small buffer)
            // This condition determines when to move the leftmost sprite to the right.
            // The buffer ensures it moves before it's fully visible at the edge.
            if (currentLeftmost.transform.position.x + spriteWidth < cameraViewLeft)
            {
                currentLeftmost.transform.position = new Vector3(currentRightmost.transform.position.x + spriteWidth - pixelOverlap, currentLeftmost.transform.position.y, currentLeftmost.transform.position.z);
                // No need to re-sort immediately here, as the next frame's loop will sort again.
            }
            // If the rightmost sprite's left edge is ahead of the camera's right edge (plus a small buffer)
            // This handles moving left and determines when to move the rightmost sprite to the left.
            else if (currentRightmost.transform.position.x > cameraViewRight)
            {
                currentRightmost.transform.position = new Vector3(currentLeftmost.transform.position.x - spriteWidth + pixelOverlap, currentRightmost.transform.position.y, currentRightmost.transform.position.z);
                // No need to re-sort immediately here, as the next frame's loop will sort again.
            }
        }
    }

    void OnDestroy()
    {
        // Only the original manager instance should clean up the copies.
        if (isOriginalManager && isLoopingHorizontal)
        {
            foreach (GameObject instance in loopingInstances)
            {
                // Destroy all instances except the one this script is on,
                // as Unity handles the destruction of the GameObject itself.
                if (instance != gameObject && instance != null) 
                {
                    Destroy(instance);
                }
            }
            loopingInstances.Clear(); // Clear the list after destruction
        }
    }
}
