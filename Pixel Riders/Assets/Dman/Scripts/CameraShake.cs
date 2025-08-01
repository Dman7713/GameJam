// CameraShake.cs
// Place this script on your Main Camera.
// It uses a singleton pattern to be easily accessible from other scripts.
//
// The 'using' clauses below must be at the very top of the file to prevent the CS1529 error.
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // Singleton instance
    public static CameraShake Instance { get; private set; }

    private Vector3 initialPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        initialPosition = transform.localPosition;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(Shake(duration, magnitude));
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(initialPosition.x + x, initialPosition.y + y, initialPosition.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Reset the camera's position back to its original spot after shaking
        transform.localPosition = initialPosition;
    }
}