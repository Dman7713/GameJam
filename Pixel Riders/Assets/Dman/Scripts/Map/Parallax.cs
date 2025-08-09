using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script handles parallax scrolling for a background layer.
/// Attach it to ONE GameObject per parallax layer that should move with the camera,
/// but at a different speed to create a sense of depth.
/// It will automatically create and manage additional copies for seamless horizontal looping.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField]
    [Tooltip("Determines how much this layer moves relative to the camera. " +
             "0 = no movement (fixed in world space). " +
             "1 = moves exactly with the camera (like foreground elements). " +
             "Values between 0 and 1 create the parallax effect (further layers move less).")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;

    [Header("Looping Settings")]
    [SerializeField]
    [Tooltip("Set this to true if this layer should loop horizontally (e.g., for continuous backgrounds).")]
    public bool isLoopingHorizontal = false;

    [SerializeField]
    [Tooltip("Amount of overlap (in world units) between looping sprites. " +
             "A positive value makes sprites overlap (closer together), " +
             "a negative value creates a gap (further apart).")]
    [Range(-20f, 20f)]
    public float overlapAmount = 20f;

    [SerializeField]
    [Tooltip("How far ahead (in world units) of the camera's right edge the last sprite should be initially spawned. " +
             "This ensures the player never sees sprites pop into existence.")]
    [Range(0f, 100f)] // Increased range to allow for higher values
    private float spawnAheadDistance = 40f; // <--- Changed default to 40f!

    private Transform cameraTransform;
    private float spriteWidth; // The width of the sprite in world units

    // A list to manage all sprite instances that form this looping layer
    private List<GameObject> spriteInstances = new List<GameObject>();

    // We store the camera's previous X position to calculate its movement delta
    private float lastCameraX;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxLayer: Main Camera not found! Please ensure your camera is tagged 'MainCamera'.");
            enabled = false; // Disable the script if no camera is found
            return;
        }

        SpriteRenderer originalSpriteRenderer = GetComponent<SpriteRenderer>();
        if (originalSpriteRenderer == null || originalSpriteRenderer.sprite == null)
        {
            Debug.LogError("ParallaxLayer: No SpriteRenderer or sprite found on " + gameObject.name + ". This script requires a sprite to calculate its size for looping.");
            enabled = false;
            return;
        }

        spriteWidth = originalSpriteRenderer.bounds.size.x;
        if (spriteWidth <= 0)
        {
            Debug.LogError("ParallaxLayer: Sprite width is zero. Cannot perform parallax. Is the sprite imported correctly and visible? (Bounds.size.x: " + spriteWidth + ")");
            enabled = false;
            return;
        }

        lastCameraX = cameraTransform.position.x;

        // Initialize sprite instances only if looping is enabled
        if (isLoopingHorizontal)
        {
            InitializeLoopingSprites(originalSpriteRenderer);
        }
        else
        {
            // If not looping, just add the original to the list for consistent update logic
            spriteInstances.Add(gameObject);
        }
    }

    /// <summary>
    /// Initializes the original sprite and creates necessary copies for seamless looping.
    /// This method aims to position sprites so they cover the camera's initial view
    /// and extend sufficiently beyond it to prevent popping.
    /// </summary>
    /// <param name="originalSr">The SpriteRenderer of the original GameObject.</param>
    private void InitializeLoopingSprites(SpriteRenderer originalSr)
    {
        // First, clear any existing instances if this method is called again (e.g. from editor scripts)
        foreach (GameObject go in spriteInstances)
        {
            if (go != gameObject) // Don't destroy the original
            {
                Destroy(go);
            }
        }
        spriteInstances.Clear();

        // Add the original sprite to the list
        spriteInstances.Add(gameObject);

        // --- IMPORTANT: Safety check for effective segment width ---
        // This is the actual width a single sprite occupies when considering overlap.
        float effectiveSegmentWidth = spriteWidth - overlapAmount;
        if (effectiveSegmentWidth <= 0)
        {
            Debug.LogError("ParallaxLayer on " + gameObject.name + ": The 'Overlap Amount' (" + overlapAmount + ") is too large, making the effective segment width (" + effectiveSegmentWidth + ") zero or negative. " +
                           "This will cause division by zero or incorrect positioning. Please reduce 'Overlap Amount' or increase your sprite's actual width. Disabling script.");
            enabled = false;
            return;
        }


        // Calculate the camera's visible width in world units
        float cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float cameraVisibleWidth = cameraHalfWidth * 2;

        // Determine how many sprites are needed to cover the screen plus a buffer
        // We need enough sprites to cover from 'cameraViewLeft' to 'cameraViewRight + spawnAheadDistance'
        float requiredTotalWidth = cameraVisibleWidth + spawnAheadDistance * 2; // Extra buffer for left and right

        // We want to ensure at least 3 sprites for smooth looping (current, left, right)
        // Adjust the number of copies based on the calculated required width
        int minCopiesNeeded = Mathf.CeilToInt(requiredTotalWidth / effectiveSegmentWidth);
        if (minCopiesNeeded < 3) minCopiesNeeded = 3; // Ensure at least 3 for robust looping

        // Initial horizontal position for the first sprite relative to the camera
        // We want the original sprite to be roughly centered or strategically placed
        // The most robust way is to make sure the "total scene" covered by the sprites
        // starts well before the camera's left edge.
        float startX = cameraTransform.position.x - cameraHalfWidth - (minCopiesNeeded / 2.0f) * effectiveSegmentWidth;

        // Position the original sprite (this GameObject)
        // We calculate its position based on the calculated startX
        // and then adjust for parallax in LateUpdate.
        transform.position = new Vector3(startX, transform.position.y, transform.position.z);


        // Create and position copies
        for (int i = 0; i < minCopiesNeeded - 1; i++) // Create 'minCopiesNeeded - 1' copies (since original is one)
        {
            GameObject newInstance = new GameObject(gameObject.name + "_LoopInstance_" + (i + 1));
            SpriteRenderer newSr = newInstance.AddComponent<SpriteRenderer>();
            newSr.sprite = originalSr.sprite;
            newSr.sortingLayerID = originalSr.sortingLayerID;
            newSr.sortingOrder = originalSr.sortingOrder;
            newSr.flipX = originalSr.flipX;
            newSr.flipY = originalSr.flipY;
            newSr.color = originalSr.color;

            // Position the new instance to the right of the last one added
            // The first new instance will be to the right of the original.
            Vector3 lastInstancePos = spriteInstances[spriteInstances.Count - 1].transform.position;
            newInstance.transform.position = new Vector3(lastInstancePos.x + effectiveSegmentWidth, transform.position.y, transform.position.z);
            newInstance.transform.parent = transform.parent; // Keep same parent for organization
            newInstance.transform.localScale = transform.localScale; // Ensure scale is copied

            spriteInstances.Add(newInstance);
        }
    }

    void LateUpdate()
    {
        // If the script was disabled due to an error during initialization, stop here.
        if (!enabled) return;

        // Calculate camera movement delta X since last frame
        float cameraDeltaX = cameraTransform.position.x - lastCameraX;

        // Calculate how much this parallax layer should move
        float layerMoveAmountX = cameraDeltaX * parallaxFactor;

        // Apply movement to all instances
        foreach (GameObject instance in spriteInstances)
        {
            if (instance != null) // Check for null in case an instance was destroyed externally
            {
                instance.transform.position += new Vector3(layerMoveAmountX, 0, 0);
            }
        }

        lastCameraX = cameraTransform.position.x; // Update lastCameraX for next frame

        // Handle looping only if enabled
        if (isLoopingHorizontal)
        {
            // Get current camera view boundaries
            float cameraViewLeft = cameraTransform.position.x - (Camera.main.orthographicSize * Camera.main.aspect);
            float cameraViewRight = cameraTransform.position.x + (Camera.main.orthographicSize * Camera.main.aspect);

            // Recalculate effective segment width and total strip width (though it should be constant)
            // This is done to ensure the check below is consistent.
            float effectiveSegmentWidth = spriteWidth - overlapAmount;
            if (effectiveSegmentWidth <= 0) // Should have been caught in Start, but as a safeguard
            {
                // This shouldn't happen if Start() worked correctly.
                // If it does, it indicates a dynamic change or an edge case.
                enabled = false;
                Debug.LogError("ParallaxLayer on " + gameObject.name + ": Effective Segment Width became zero or negative during LateUpdate. Disabling script.");
                return;
            }
            float totalStripWidth = effectiveSegmentWidth * spriteInstances.Count;


            // Loop through all instances to check for repositioning
            for (int i = 0; i < spriteInstances.Count; i++)
            {
                GameObject currentSprite = spriteInstances[i];
                if (currentSprite == null) continue; // Skip if somehow null

                // Reposition logic:
                // If a sprite's right edge is behind the camera's left edge (plus a small buffer)
                // then move it to the right of the entire parallax strip.
                if (currentSprite.transform.position.x + spriteWidth < cameraViewLeft)
                {
                    currentSprite.transform.position = new Vector3(currentSprite.transform.position.x + totalStripWidth, currentSprite.transform.position.y, currentSprite.transform.position.z);
                }
                // If a sprite's left edge is beyond the camera's right edge (plus a small buffer)
                // when moving left, move it to the left of the entire parallax strip.
                else if (currentSprite.transform.position.x > cameraViewRight + spawnAheadDistance)
                {
                    currentSprite.transform.position = new Vector3(currentSprite.transform.position.x - totalStripWidth, currentSprite.transform.position.y, currentSprite.transform.position.z);
                }
            }
        }
    }

    void OnDestroy()
    {
        // Clean up all dynamically created instances when the original GameObject is destroyed
        foreach (GameObject instance in spriteInstances)
        {
            // Only destroy if it's not the original GameObject this script is attached to,
            // and if it hasn't been destroyed by Unity already.
            if (instance != gameObject && instance != null)
            {
                Destroy(instance);
            }
        }
        spriteInstances.Clear();
    }

    // Optional: A way to re-initialize from the inspector for debugging
    // You can add a button in a custom editor for this
    public void Editor_ReinitializeParallax()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Cannot reinitialize in Play mode via Editor_ReinitializeParallax. Use Awake/Start for runtime setup.");
            return;
        }

        // Clean up existing copies before re-initialization
        foreach (GameObject go in spriteInstances)
        {
            if (go != gameObject)
            {
                DestroyImmediate(go); // Use DestroyImmediate in editor mode
            }
        }
        spriteInstances.Clear();

        // Call Start to re-initialize everything
        Start();
    }
}