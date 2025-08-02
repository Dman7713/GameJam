using UnityEngine;
using UnityEngine.UI;

public class QuitGameButton : MonoBehaviour
{
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogWarning("QuitGameButton is not on a Button.");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting the game...");
        Application.Quit();

#if UNITY_EDITOR
        // This only works in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
