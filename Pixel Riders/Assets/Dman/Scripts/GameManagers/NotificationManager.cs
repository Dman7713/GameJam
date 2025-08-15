using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages in-game notifications that slide in from the bottom of the screen.
/// This is a singleton, allowing easy access from any other script.
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    // Defines the different types of notifications with corresponding colors.
    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    [Header("UI Elements")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image panelImage;

    [Header("Settings")]
    [SerializeField] private float slideInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float slideOutDuration = 0.5f;
    [SerializeField] private float startYPosition = -600f; // The starting Y position (off-screen)
    [SerializeField] private float targetYPosition = -350f; // The final Y position (on-screen)
    [SerializeField] private AnimationCurve slideCurve;

    [Header("Colors")]
    [SerializeField] private Color infoColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip notificationSound;

    private RectTransform panelRect;
    //private bool isAnimating = false;
    private Color originalTextColor; // Store the target color for the text

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Get the RectTransform for dynamic positioning.
            panelRect = notificationPanel.GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set the initial position of the panel off-screen.
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, startYPosition);

        // Start with the panel inactive.
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Displays a notification with a specified type and message.
    /// Example usage: NotificationManager.Instance.ShowNotification(NotificationManager.NotificationType.Error, "Game data failed to save!");
    /// </summary>
    /// <param name="type">The type of notification (Info, Warning, Error).</param>
    /// <param name="message">The text to display in the notification.</param>
    public void ShowNotification(NotificationType type, string message)
    {
        // This check helps to diagnose if the script's GameObject is inactive.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("NotificationManager GameObject is inactive. The notification will not be shown.");
            return;
        }

        // Stop any previous animation to prevent overlap.
        StopAllCoroutines();

        notificationText.text = message;
        switch (type)
        {
            case NotificationType.Info:
                originalTextColor = infoColor;
                break;
            case NotificationType.Warning:
                originalTextColor = warningColor;
                break;
            case NotificationType.Error:
                originalTextColor = errorColor;
                break;
        }

        StartCoroutine(AnimateNotification());
    }

    private IEnumerator AnimateNotification()
    {
        // Play sound if available
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound);
        }

        notificationPanel.SetActive(true);
        isAnimating = true;

        // Ensure the panel is transparent by setting its alpha to 0.
        Color panelColor = panelImage.color;
        panelImage.color = new Color(panelColor.r, panelColor.g, panelColor.b, 0f);

        // Slide in & Fade in text
        float timer = 0f;
        Color textStartColor = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        while (timer < slideInDuration)
        {
            timer += Time.unscaledDeltaTime;
            // Use the slide curve for a smooth effect
            float t = (slideCurve != null) ? slideCurve.Evaluate(timer / slideInDuration) : timer / slideInDuration;

            float newY = Mathf.Lerp(startYPosition, targetYPosition, t);
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, newY);

            notificationText.color = Color.Lerp(textStartColor, originalTextColor, timer / slideInDuration);
            yield return null;
        }
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, targetYPosition);
        notificationText.color = originalTextColor;

        // Display
        yield return new WaitForSecondsRealtime(displayDuration);

        // Slide out & Fade out text
        timer = 0f;
        while (timer < slideOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = (slideCurve != null) ? slideCurve.Evaluate(timer / slideOutDuration) : timer / slideOutDuration;

            float newY = Mathf.Lerp(targetYPosition, startYPosition, t);
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, newY);

            notificationText.color = Color.Lerp(originalTextColor, textStartColor, t);
            yield return null;
        }

        // Reset positions and hide the panel
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, startYPosition);
        notificationPanel.SetActive(false);
        isAnimating = false;
    }
}

// HOW TO NOTIFY
// NotificationManager.Instance.ShowNotification(NotificationManager.NotificationType.Error, "Player is dead!");

