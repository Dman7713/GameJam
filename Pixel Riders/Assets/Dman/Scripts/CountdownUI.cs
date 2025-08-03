using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    [Header("References")]
    public Image[] countdownImages;              // Assign 4 images here in inspector (3,2,1,GO)
    public AudioSource[] countdownSounds;        // Assign 4 audio sources matching each image
    public StuntManager stuntManager;            // Reference to the StuntManager script

    [Header("Settings")]
    public float startDelay = 1f;                // Delay before countdown begins
    public float displayTime = 1f;               // How long each image shows
    public float fadeTime = 0.5f;                // How long fade in/out takes

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if (stuntManager == null)
        {
            Debug.LogError("StuntManager reference is not assigned in the Inspector!");
            return;
        }

        stuntManager.ResetScore();
        StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        yield return new WaitForSeconds(startDelay);

        // Initialize all images as invisible
        foreach (var img in countdownImages)
        {
            img.gameObject.SetActive(false);
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        }

        for (int i = 0; i < countdownImages.Length; i++)
        {
            var img = countdownImages[i];
            img.gameObject.SetActive(true);

            // Play sound for this image if assigned
            if (countdownSounds != null && i < countdownSounds.Length && countdownSounds[i] != null)
            {
                countdownSounds[i].Play();
            }

            // Fade in
            yield return StartCoroutine(FadeImage(img, 0f, 1f, fadeTime));

            // Display duration
            yield return new WaitForSeconds(displayTime);

            // Fade out
            yield return StartCoroutine(FadeImage(img, 1f, 0f, fadeTime));

            img.gameObject.SetActive(false);
        }

        Debug.Log("GO! Countdown finished.");

        if (stuntManager != null)
        {
            stuntManager.StartStunts();
        }
    }

    private IEnumerator FadeImage(Image img, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = img.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            img.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        img.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}
