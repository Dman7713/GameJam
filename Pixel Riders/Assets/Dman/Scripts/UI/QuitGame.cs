using UnityEngine;
using System.Collections; // Required for using Coroutines
using UnityEngine.UI;

public class QuitGameButton : MonoBehaviour
{
    // The AudioSource to play the sound from.
    public AudioSource audioSource;

    // The sound clip to play when the button is clicked.
    public AudioClip clickSound;

    // The delay in seconds before the game quits.
    public float quitDelay = 0.2f;

    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // We'll add a listener that starts a coroutine to handle the delay.
            button.onClick.AddListener(StartQuitGame);
        }
        else
        {
            Debug.LogWarning("QuitGameButton is not on a Button.");
        }
    }

    /// <summary>
    /// This method is called by the button's OnClick event and starts the coroutine.
    /// </summary>
    public void StartQuitGame()
    {
        StartCoroutine(QuitGameCoroutine());
    }

    /// <summary>
    /// A coroutine that plays a sound, waits for a delay, and then quits the application.
    /// </summary>
    private IEnumerator QuitGameCoroutine()
    {
        // Play the click sound if it's assigned.
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log("Quitting the game...");

        // Wait for the specified delay to let the sound play.
        yield return new WaitForSeconds(quitDelay);

        // Now, quit the application.
        Application.Quit();

#if UNITY_EDITOR
        // This line ensures the game stops playing in the Unity Editor.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
