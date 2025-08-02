using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string sceneName; // Name of the scene to switch to

    private void Start()
    {
        // Automatically attach the OnClick event if this is on a Button
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(SwitchScene);
        }
        else
        {
            Debug.LogWarning("SceneSwitcher is not on a Button.");
        }
    }

    public void SwitchScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
