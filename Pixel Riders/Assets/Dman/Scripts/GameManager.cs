using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for accessing Button component

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] private GameObject _gameOverCanvas;

    // The delay in seconds before the game restarts.
    public float restartDelay = 0.2f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        Time.timeScale = 1f;
    }
    
    /// <summary>
    /// This public method is what the button will now call.
    /// It starts the coroutine to handle the delay.
    /// </summary>
    public void StartDelayedRestart()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    /// <summary>
    /// A coroutine that waits for a delay and then restarts the game.
    /// </summary>
    private IEnumerator RestartGameCoroutine()
    {
        Debug.Log("Restarting the game...");

        // Wait for the specified delay.
        yield return new WaitForSeconds(restartDelay);

        // Now, restart the scene.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
