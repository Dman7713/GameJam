using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
    // Name of the scene to switch to.
    [SerializeField] private string sceneName; 

    // The AudioSource to play the sound from.
    public AudioSource audioSource;
    
    // The sound clip to play when the button is clicked.
    public AudioClip clickSound;

    // The delay in seconds before the scene switches.
    public float switchDelay = 0.2f;

    private void Start()
    {
        // Automatically attach the OnClick event if this is on a Button
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(StartSwitchScene);
        }
        else
        {
            Debug.LogWarning("SceneSwitcher is not on a Button.");
        }
    }

    /// <summary>
    /// This public method is what the button will now call.
    /// It starts the coroutine to handle the delay.
    /// </summary>
    public void StartSwitchScene()
    {
        StartCoroutine(SwitchSceneCoroutine());
    }

    /// <summary>
    /// A coroutine that plays a sound, waits for a delay, and then switches the scene.
    /// </summary>
    private IEnumerator SwitchSceneCoroutine()
    {
        // Play the click sound if it's assigned.
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log("Switching to scene: " + sceneName);

        // Wait for the specified delay.
        yield return new WaitForSeconds(switchDelay);

        // Now, switch the scene.
        SceneManager.LoadScene(sceneName);
    }
}
