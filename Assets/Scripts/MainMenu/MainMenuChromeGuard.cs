using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh
{
    /// <summary>
    /// Settings hides the main-menu chrome (play button, navigation) while it is open
    /// and restores it on its own close. When another popup replaces Settings
    /// (e.g. Settings -> Rate Us) Settings is deactivated without closing, and the
    /// chrome used to stay hidden forever, leaving the player with no buttons.
    /// This guard brings the chrome back whenever Settings is not on screen.
    /// </summary>
    public sealed class MainMenuChromeGuard : MonoBehaviour
    {
        private const string ChromeRootName = "MainMenuChromeRoot";
        private const float CheckInterval = 0.2f;

        private GameObject chromeRoot;
        private SettingsPopup settingsPopup;
        private float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Attach(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Attach(scene);
        }

        private static void Attach(Scene scene)
        {
            if (!scene.IsValid() || FindFirstObjectByType<MainMenuChromeGuard>() != null)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rect.name == ChromeRootName)
                    {
                        GameObject host = new(nameof(MainMenuChromeGuard));
                        SceneManager.MoveGameObjectToScene(host, scene);
                        MainMenuChromeGuard guard = host.AddComponent<MainMenuChromeGuard>();
                        guard.chromeRoot = rect.gameObject;
                        return;
                    }
                }
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck || chromeRoot == null || chromeRoot.activeSelf)
            {
                return;
            }

            nextCheck = Time.unscaledTime + CheckInterval;
            if (settingsPopup == null)
            {
                settingsPopup = FindFirstObjectByType<SettingsPopup>(FindObjectsInactive.Include);
            }

            if (settingsPopup == null || !settingsPopup.gameObject.activeInHierarchy)
            {
                chromeRoot.SetActive(true);
            }
        }
    }
}
