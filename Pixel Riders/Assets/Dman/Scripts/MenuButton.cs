using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    // You can adjust the delay time in the Inspector
    public float delay = 0.2f;

    // This method will be called by your button's OnClick event
    public void GoToMenu()
    {
        // We start the coroutine to handle the delayed scene load
        StartCoroutine(LoadSceneAfterDelay("MainMenu"));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        // This line is what creates the delay
        yield return new WaitForSeconds(delay);
        
        // After the delay, the scene will be loaded
        SceneManager.LoadScene(sceneName);
    }
}