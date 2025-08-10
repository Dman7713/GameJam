using UnityEngine;

public class MoveRight : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f; // Units per second

    void Update()
    {
        // Move to the right at the given speed
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
}
