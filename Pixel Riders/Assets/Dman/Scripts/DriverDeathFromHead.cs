using UnityEngine;
using System.Collections.Generic;

public class DriverDeathFromHead : MonoBehaviour
{
    [SerializeField] private Transform bikeRoot;
    [SerializeField] private LayerMask groundLayer;

    [Header("Optional - Manual joints to disable")]
    [SerializeField] private List<HingeJoint2D> hingeJointsToDisable = new List<HingeJoint2D>();
    [SerializeField] private List<WheelJoint2D> wheelJointsToDisable = new List<WheelJoint2D>();

    private bool hasDied = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasDied) return;

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            hasDied = true;
            Debug.Log("Driver's head hit the ground!");

            UnparentAndEnableRagdoll(bikeRoot);

            if (GameManager.instance != null)
            {
                GameManager.instance.GameOver();
            }
            else
            {
                Debug.LogWarning("GameManager.instance is null!");
            }
        }
    }

    private void UnparentAndEnableRagdoll(Transform root)
    {
        if (root == null)
        {
            Debug.LogError("bikeRoot is not assigned!");
            return;
        }

        // Disable joints in bikeRoot and children
        foreach (WheelJoint2D wheel in root.GetComponentsInChildren<WheelJoint2D>(true))
        {
            wheel.enabled = false;
        }

        foreach (HingeJoint2D hinge in root.GetComponentsInChildren<HingeJoint2D>(true))
        {
            hinge.enabled = false;
        }

        // Disable manually assigned joints
        foreach (var hinge in hingeJointsToDisable)
        {
            if (hinge != null) hinge.enabled = false;
        }

        foreach (var wheel in wheelJointsToDisable)
        {
            if (wheel != null) wheel.enabled = false;
        }

        // Unparent and ensure Rigidbody2D is added and active
        List<Transform> children = new List<Transform>();
        foreach (Transform child in root)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            child.SetParent(null); // Detach from parent

            // Add Rigidbody2D if missing
            Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = child.gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
        }
    }
}
