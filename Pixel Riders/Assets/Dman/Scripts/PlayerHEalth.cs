using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int health = 100;
    private bool isDead = false;

    [Header("Head Settings")]
    public Transform head;
    public LayerMask groundLayer;
    public float checkRadius = 0.1f;

    [Header("Body Parts")]
    public List<Rigidbody2D> bodyParts;         // All ragdoll Rigidbodies
    public List<Collider2D> bodyColliders;      // All ragdoll Colliders

    void Start()
    {
        // Set initial state of body parts (no physics, no collisions)
        foreach (Rigidbody2D rb in bodyParts)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        foreach (Collider2D col in bodyColliders)
        {
            col.enabled = false;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (IsHeadTouchingGround())
        {
            Die();
        }
    }

    bool IsHeadTouchingGround()
    {
        return Physics2D.OverlapCircle(head.position, checkRadius, groundLayer);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player died by hitting head on the ground.");

        // Activate ragdoll parts
        foreach (Rigidbody2D rb in bodyParts)
        {
            rb.transform.parent = null;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.gravityScale = 1f;
        }

        foreach (Collider2D col in bodyColliders)
        {
            col.enabled = true;
        }

        // Disable player control (optional)
        Destroy(this); // Or disable movement script instead
    }

    void OnDrawGizmosSelected()
    {
        if (head != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(head.position, checkRadius);
        }
    }
}
