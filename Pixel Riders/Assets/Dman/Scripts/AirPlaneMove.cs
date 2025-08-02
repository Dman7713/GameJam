using UnityEngine;

public class MoveRight : MonoBehaviour
{
    public float speed = 5f; // units per second

    void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }
}
