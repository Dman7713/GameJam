using UnityEngine;

public class AutoDespawn : MonoBehaviour
{
    public float lifetime = 5f; // Time in seconds before the object is destroyed

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
