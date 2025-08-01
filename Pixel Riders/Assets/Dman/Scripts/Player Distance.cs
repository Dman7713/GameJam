using UnityEngine;
using TMPro;

public class PlayerDistanceTracker : MonoBehaviour
{
    private float startX;
    public TextMeshProUGUI distanceText;

    void Start()
    {
        startX = transform.position.x;
    }

    void Update()
    {
        float distance = transform.position.x - startX;
        distanceText.text = "" + Mathf.FloorToInt(distance) + " ft";
    }
}
