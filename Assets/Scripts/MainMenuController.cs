using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnPlayClicked()
    {
        if (Application.CanStreamedLevelBeLoaded("Game"))
        {
            SceneManager.LoadScene("Game");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded("Main"))
        {
            SceneManager.LoadScene("Main");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded("Assets/Scenes/Main.unity"))
        {
            SceneManager.LoadScene("Assets/Scenes/Main.unity");
            return;
        }

        Debug.LogError("MainMenuController: game scene is not in Build Settings.");
    }

    public void OnLevelsClicked()
    {
        Debug.Log("CardMesh MVP: levels button clicked.");
    }

    public void OnSettingsClicked()
    {
        Debug.Log("CardMesh MVP: settings button clicked.");
    }
}
