using UnityEngine;

/// <summary>
/// This script controls the dynamic properties of the PixelFogShader,
/// such as its alpha threshold to create a pulsating/breathing effect.
/// It also ensures the shader receives the correct time for animation.
/// </summary>
[RequireComponent(typeof(Renderer))] // Requires a Renderer component (MeshRenderer for Quad)
public class PixelFogController : MonoBehaviour
{
    [Tooltip("The minimum alpha threshold for the fog (0 = fully transparent, 1 = fully opaque).")]
    [Range(0f, 1f)]
    public float minAlphaThreshold = 0.4f;

    [Tooltip("The maximum alpha threshold for the fog (0 = fully transparent, 1 = fully opaque).")]
    [Range(0f, 1f)]
    public float maxAlphaThreshold = 0.7f;

    [Tooltip("How fast the fog pulsates/animates its transparency.")]
    public float pulseSpeed = 0.5f;

    private Material fogMaterial;
    private float timeOffset; // Used to make different fog clouds animate out of sync

    void Awake()
    {
        // Get the material from the Renderer component attached to this GameObject.
        // Important: Use .material to get an instance, not .sharedMaterial,
        // so you don't modify the asset directly.
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            fogMaterial = renderer.material;
        }
        else
        {
            Debug.LogError("PixelFogController: No Renderer found on this GameObject. Please add a MeshRenderer (for a Quad) or SpriteRenderer.");
            enabled = false; // Disable the script if no renderer is found
            return;
        }

        // Give each fog cloud a random starting point in its animation cycle
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (fogMaterial == null) return;

        // Calculate the alpha threshold value using a sine wave, which creates a smooth back-and-forth motion.
        // Mathf.Sin returns values between -1 and 1.
        // We remap this to our minAlphaThreshold and maxAlphaThreshold range.
        float currentAlphaThreshold = Mathf.Lerp(minAlphaThreshold, maxAlphaThreshold, (Mathf.Sin(Time.time * pulseSpeed + timeOffset) + 1) / 2);

        // Pass the calculated alpha threshold to the shader.
        fogMaterial.SetFloat("_AlphaThreshold", currentAlphaThreshold);

        // The shader itself uses _Time.y for animation speed.
        // Unity automatically updates _Time in shaders, so we don't need to pass it explicitly here.
    }

    // Clean up the instantiated material when the GameObject is destroyed
    void OnDestroy()
    {
        if (fogMaterial != null)
        {
            Destroy(fogMaterial);
        }
    }
}
