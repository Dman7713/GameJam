using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSpeedUI : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D playerRigidbody;
    public TextMeshProUGUI speedText;
    public Image speedFillImage;

    [Header("Settings")]
    public float maxSpeed = 100f;
    public string prefix = "Speed: ";
    public string unit = " m/s";
    public int decimalPlaces = 1;

    [Header("RPM Feel")]
    public float rpmSmoothSpeed = 5f; // How quickly the RPM reacts

    private float currentFill = 0f;

    void Update()
    {
        if (playerRigidbody == null)
            return;

        // Get player speed
        float speed = playerRigidbody.linearVelocity.magnitude;

        // === Update Text ===
        if (speedText != null)
        {
            speedText.text = prefix + speed.ToString("F" + decimalPlaces) + unit;
        }

        // === Update Bar Fill (RPM-style) ===
        if (speedFillImage != null)
        {
            float targetFill = Mathf.Clamp01(speed / maxSpeed);
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * rpmSmoothSpeed);
            speedFillImage.fillAmount = currentFill;
        }
    }
}
