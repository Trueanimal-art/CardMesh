using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SolitaireAssociations;

public class MainMenuController : MonoBehaviour
{
    private LevelProgressionManager progressionManager;
    private LevelSelectionPopup levelSelectionPopup;
    private SettingsPopup settingsPopup;

    private void OnEnable()
    {
        ApplyPremiumStyles();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyPremiumStyles();
    }
#endif

    public void OnPlayClicked()
    {
        ResetSceneLoadState();
        LevelProgressionManager progress = EnsureProgressionManager();
        progress.SelectLevel(progress.LastSelectedLevel);
        progress.GetOrCreateSeedForCurrentLevel();

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

    private static void ResetSceneLoadState()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnLevelsClicked()
    {
        levelSelectionPopup = LevelSelectionPopup.GetOrCreate();
        levelSelectionPopup.Show(EnsureProgressionManager(), _ => OnPlayClicked());
    }

    public void OnSettingsClicked()
    {
        settingsPopup = SettingsPopup.GetOrCreate();
        settingsPopup.Show();
    }

    private static void ApplyPremiumStyles()
    {
        Color menuTextColor = new(0.16f, 0.09f, 0.02f, 1f);
        StyleButton("PlayButton", PremiumUISprites.PrimaryButton, menuTextColor, 42f, true);
        StyleButton("LevelsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleButton("SettingsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleButton("SoundToggleButton", PremiumUISprites.IconCircle, menuTextColor, 28f, false);
    }

    private static void StyleButton(string objectName, Sprite sprite, Color textColor, float fontSize, bool styleLabel)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null || sprite == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        TMP_Text label = target.GetComponentInChildren<TMP_Text>(true);
        if (label != null && styleLabel)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(14f, fontSize * 0.58f);
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.fontStyle = FontStyles.Bold;

            Shadow textShadow = label.GetComponent<Shadow>();
            if (textShadow == null)
            {
                textShadow = label.gameObject.AddComponent<Shadow>();
            }

            textShadow.effectColor = new Color(1f, 0.88f, 0.34f, 0.42f);
            textShadow.effectDistance = new Vector2(0f, -2f);
            textShadow.useGraphicAlpha = true;

            Outline outline = label.GetComponent<Outline>();
            if (outline == null)
            {
                outline = label.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(0.03f, 0.02f, 0f, 0.26f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        if (target.GetComponent<UIJuicyButton>() == null)
        {
            target.AddComponent<UIJuicyButton>();
        }

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0.06f, 0.02f, 0f, 0.38f);
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;
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
