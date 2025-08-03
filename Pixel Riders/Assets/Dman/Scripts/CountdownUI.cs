using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    public Image[] countdownImages; // Assign 4 images here in inspector (3,2,1,GO)
    public float startDelay = 1f;   // Delay before countdown begins
    public float displayTime = 1f;  // How long each image shows
    public float fadeTime = 0.5f;   // How long fade in/out takes

    private void Start()
    {
        // Start countdown coroutine
        StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        // Wait before starting countdown
        yield return new WaitForSeconds(startDelay);

        // Disable all images at start and make transparent
        foreach (var img in countdownImages)
        {
            img.gameObject.SetActive(false);
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        }

        for (int i = 0; i < countdownImages.Length; i++)
        {
            var img = countdownImages[i];
            img.gameObject.SetActive(true);

            // Fade in
            yield return StartCoroutine(FadeImage(img, 0f, 1f, fadeTime));

            // Wait visible time
            yield return new WaitForSeconds(displayTime);

            // Fade out
            yield return StartCoroutine(FadeImage(img, 1f, 0f, fadeTime));

            img.gameObject.SetActive(false);
        }

        Debug.Log("GO! Countdown finished.");
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
