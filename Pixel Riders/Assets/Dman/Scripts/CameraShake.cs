using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Header("Impulse Settings")]
    public CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Trigger a camera shake with given magnitude.
    /// Duration is controlled by the impulse profile.
    /// </summary>
    /// <param name="duration">Not used here - control via profile</param>
    /// <param name="magnitude">Shake intensity (amplitude)</param>
    public void Shake(float duration = 0.1f, float magnitude = 0.2f)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(magnitude);
        }
        else
        {
            Debug.LogWarning("CameraShake: impulseSource is not assigned.");
        }
    }
}
