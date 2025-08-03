using UnityEngine;

public class FreezeRigidbody2D : MonoBehaviour
{
    public Rigidbody2D playerRigidbody;
    public float freezeDelay = 2f;      // Time before freezing starts
    public float freezeDuration = 3f;   // How long to stay frozen

    private void Start()
    {
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();

        StartCoroutine(FreezeWithDelay());
    }

    private System.Collections.IEnumerator FreezeWithDelay()
    {
        // Wait before freezing
        yield return new WaitForSeconds(freezeDelay);

        // Freeze Rigidbody2D
        playerRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;

        // Wait while frozen
        yield return new WaitForSeconds(freezeDuration);

        // Unfreeze Rigidbody2D
        playerRigidbody.constraints = RigidbodyConstraints2D.None;
    }
}
