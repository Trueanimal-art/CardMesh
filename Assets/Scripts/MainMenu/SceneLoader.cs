using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MainMenuScenePath = "Assets/MainMenu.unity";
    private const string GameSceneName = "Game";
    private const string CurrentGameSceneName = "Main";
    private const string CurrentGameScenePath = "Assets/Scenes/Main.unity";

    public void LoadMainMenu()
    {
        LoadSceneWithFallback(MainMenuSceneName, MainMenuScenePath);
    }

    public void LoadGame()
    {
        LoadSceneWithFallback(GameSceneName, CurrentGameSceneName, CurrentGameScenePath);
    }

    public void LoadSecretGame()
    {
        Debug.Log("CardMesh MVP: secret game scene is disabled.");
    }

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneLoader.LoadSceneByName skipped: scene name is empty.");
            return;
        }

        LoadSceneWithFallback(sceneName, null, null);
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowLevelsPlaceholder()
    {
        Debug.Log("CardMesh MVP: levels button clicked.");
    }

    public void ShowSettingsPlaceholder()
    {
        Debug.Log("CardMesh MVP: settings button clicked.");
    }

    private static void LoadSceneWithFallback(string preferredScene, string fallbackSceneName)
    {
        LoadSceneWithFallback(preferredScene, fallbackSceneName, null);
    }

    private static void LoadSceneWithFallback(string preferredScene, string fallbackSceneName, string fallbackScenePath)
    {
        if (!string.IsNullOrWhiteSpace(preferredScene) && Application.CanStreamedLevelBeLoaded(preferredScene))
        {
            SceneManager.LoadScene(preferredScene);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSceneName) && Application.CanStreamedLevelBeLoaded(fallbackSceneName))
        {
            SceneManager.LoadScene(fallbackSceneName);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackScenePath) && Application.CanStreamedLevelBeLoaded(fallbackScenePath))
        {
            SceneManager.LoadScene(fallbackScenePath);
            return;
        }

        Debug.LogError($"SceneLoader: scene '{preferredScene}' is not in Build Settings.");
    }
}
