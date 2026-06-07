using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using SolitaireAssociations;

public class SceneLoader : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string MainMenuScenePath = "Assets/MainMenu.unity";
    private const string GameSceneName = "Game";
    private const string CurrentGameSceneName = "Main";
    private const string CurrentGameScenePath = "Assets/Scenes/Main.unity";

    private LevelProgressionManager progressionManager;
    private LevelSelectionPopup levelSelectionPopup;
    private SettingsPopup settingsPopup;

    public void LoadMainMenu()
    {
        LoadSceneWithFallback(MainMenuSceneName, MainMenuScenePath);
    }

    public void LoadGame()
    {
        LevelProgressionManager progress = EnsureProgressionManager();
        progress.SelectLevel(progress.LastSelectedLevel);
        progress.GetOrCreateSeedForCurrentLevel();
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
        levelSelectionPopup = LevelSelectionPopup.GetOrCreate();
        levelSelectionPopup.Show(EnsureProgressionManager(), _ => LoadGame());
    }

    public void ShowSettingsPlaceholder()
    {
        settingsPopup = SettingsPopup.GetOrCreate();
        settingsPopup.Show();
    }

    private static void LoadSceneWithFallback(string preferredScene, string fallbackSceneName)
    {
        LoadSceneWithFallback(preferredScene, fallbackSceneName, null);
    }

    private static void LoadSceneWithFallback(string preferredScene, string fallbackSceneName, string fallbackScenePath)
    {
        ResetSceneLoadState();

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

    private static void ResetSceneLoadState()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private LevelProgressionManager EnsureProgressionManager()
    {
        if (progressionManager != null)
        {
            return progressionManager;
        }

        progressionManager = FindFirstObjectByType<LevelProgressionManager>(FindObjectsInactive.Include);
        if (progressionManager == null)
        {
            progressionManager = gameObject.AddComponent<LevelProgressionManager>();
        }

        return progressionManager;
    }
}
