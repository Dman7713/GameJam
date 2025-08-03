using UnityEngine;
using UnityEngine.UI;

public class MoveUIRight : MonoBehaviour
{
    public float moveDuration = 2f;  // How long to move (seconds)
    public float moveSpeed = 100f;   // Speed in pixels per second

    private RectTransform rectTransform;
    private float timer = 0f;
    private bool moving = true;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (moving)
        {
            if (timer < moveDuration)
            {
                float moveAmount = moveSpeed * Time.deltaTime;
                rectTransform.anchoredPosition += new Vector2(moveAmount, 0);
                timer += Time.deltaTime;
            }
            else
            {
                moving = false;
            }
        }
    }
}
