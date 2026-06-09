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
    private ChestChaseManager chestChaseManager;

    private void OnEnable()
    {
        ApplyPremiumStyles();
        BindChestChaseButton();
        SubscribeChestChaseUpdates();
    }

    private void OnDisable()
    {
        if (chestChaseManager != null)
        {
            chestChaseManager.StateChanged -= RefreshChestChaseButton;
        }
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

    public void OnChestChaseClicked()
    {
        ChestChaseManager manager = ChestChaseManager.GetOrCreate(this);
        ChestChasePopup.GetOrCreate().Show(manager);
    }

    private static void ApplyPremiumStyles()
    {
        Color menuTextColor = new(0.16f, 0.09f, 0.02f, 1f);
        StyleButton("PlayButton", PremiumUISprites.PrimaryButton, menuTextColor, 42f, true);
        StyleButton("LevelsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleButton("SettingsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleButton("SoundToggleButton", PremiumUISprites.IconCircle, menuTextColor, 28f, false);
    }

    private void BindChestChaseButton()
    {
        GameObject existing = GameObject.Find("ChestChaseButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        Button button = existing != null ? existing.GetComponent<Button>() : null;

        if (root == null || button == null)
        {
            Debug.LogWarning("MainMenuController: ChestChaseButton is missing from MainMenu scene.");
            return;
        }

        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RoyalEventSprites.ButtonIcon != null ? RoyalEventSprites.ButtonIcon : PremiumUISprites.IconCircle;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(OnChestChaseClicked);
        }

        Shadow shadow = root.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = new Color(0.06f, 0.02f, 0f, 0.38f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;
        }

        if (root.GetComponent<UIJuicyButton>() == null)
        {
            root.gameObject.AddComponent<UIJuicyButton>();
        }

        StyleChestChaseIcon(root);
        RefreshChestChaseBadge(root);
    }

    private static void StyleChestChaseIcon(RectTransform root)
    {
        SetIconSprite(root.Find("Glow"), PremiumUISprites.IconCircle, new Color(0.58f, 1f, 0.22f, 0.11f), new Vector2(122f, 122f), Vector2.zero);
        SetChildActive(root, "CrownBase", false);
        SetChildActive(root, "CrownBand", false);
        SetChildActive(root, "CrownLeft", false);
        SetChildActive(root, "CrownCenter", false);
        SetChildActive(root, "CrownRight", false);
        SetChildActive(root, "CrownGem", false);
        StyleProgressBadge(root.Find("ProgressBadge") as RectTransform);
    }

    private static void RefreshChestChaseBadge(RectTransform root)
    {
        RectTransform badge = root.Find("ProgressBadge") as RectTransform;
        TMP_Text badgeText = root.Find("ProgressBadge/BadgeText")?.GetComponent<TMP_Text>();
        if (badgeText == null)
        {
            return;
        }

        ChestChaseManager manager = ChestChaseManager.GetOrCreate(root);
        int current = Mathf.Clamp(manager.CurrentKeys, 0, Mathf.Max(1, manager.KeysRequired));
        if (badge != null)
        {
            badge.gameObject.SetActive(current > 0);
        }

        if (current <= 0)
        {
            badgeText.text = string.Empty;
            return;
        }

        badgeText.text = current.ToString();
        RuntimeCyrillicFontProvider.Apply(badgeText, badgeText.text);
        badgeText.enableAutoSizing = true;
        badgeText.fontSizeMax = 18f;
        badgeText.fontSizeMin = 9f;
        badgeText.enableWordWrapping = false;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = Color.white;
        badgeText.fontStyle = FontStyles.Bold;

        RectTransform textRect = badgeText.transform as RectTransform;
        if (textRect != null)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(3f, 1f);
            textRect.offsetMax = new Vector2(-3f, -2f);
            textRect.localScale = Vector3.one;
        }
    }

    private static void SetImageColor(Transform target, Color color, Sprite sprite = null)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        image.color = color;
        image.raycastTarget = false;
    }

    private static void SetIconSprite(Transform target, Sprite sprite, Color color, Vector2 size, Vector2 anchoredPosition)
    {
        Image image = target != null ? target.GetComponent<Image>() : null;
        RectTransform rect = target as RectTransform;
        if (image == null || rect == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = color;
        image.raycastTarget = false;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localRotation = Quaternion.identity;
    }

    private static void StyleProgressBadge(RectTransform badge)
    {
        if (badge == null)
        {
            return;
        }

        badge.anchorMin = new Vector2(1f, 1f);
        badge.anchorMax = new Vector2(1f, 1f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.sizeDelta = new Vector2(48f, 31f);
        badge.anchoredPosition = new Vector2(-12f, -14f);
        badge.localRotation = Quaternion.identity;
        badge.localScale = Vector3.one;

        Image image = badge.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RoyalEventSprites.RedBadge != null ? RoyalEventSprites.RedBadge : PremiumUISprites.IconCircle;
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
        }
    }

    private static void SetChildActive(Transform root, string childName, bool active)
    {
        Transform child = root != null ? root.Find(childName) : null;
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private void SubscribeChestChaseUpdates()
    {
        ChestChaseManager manager = ChestChaseManager.GetOrCreate(this);
        if (chestChaseManager == manager)
        {
            return;
        }

        if (chestChaseManager != null)
        {
            chestChaseManager.StateChanged -= RefreshChestChaseButton;
        }

        chestChaseManager = manager;
        if (chestChaseManager != null)
        {
            chestChaseManager.StateChanged += RefreshChestChaseButton;
        }
    }

    private void RefreshChestChaseButton()
    {
        GameObject existing = GameObject.Find("ChestChaseButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (root != null)
        {
            StyleChestChaseIcon(root);
            RefreshChestChaseBadge(root);
        }
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
