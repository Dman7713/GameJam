using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameStartFade : MonoBehaviour
{
    public GameObject rootUIToFade; // assign the root GameObject containing all UI images (e.g., your canvas or loadingUI)
    public float delayBeforeFade = 2f;
    public float fadeDuration = 1f;
    public GameObject loadingUI; // optional, disable after fade

    private Image[] imagesToFade;
    private bool fading = false;
    private float timer = 0f;

    void Start()
    {
        timer = 0f;

        // Get all Image components inside the root GameObject (including children)
        if (rootUIToFade != null)
            imagesToFade = rootUIToFade.GetComponentsInChildren<Image>(true);

        // Set all images fully opaque at start
        if (imagesToFade != null)
        {
            foreach (var img in imagesToFade)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!fading && timer >= delayBeforeFade)
        {
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        fading = true;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);

            foreach (var img in imagesToFade)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
            yield return null;
        }

        // Ensure all images fully transparent at end
        foreach (var img in imagesToFade)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }

        if (loadingUI != null)
            loadingUI.SetActive(false);
    }
}
