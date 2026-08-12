using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CardMesh;

public class MainMenuController : MonoBehaviour
{
    private LevelProgressionManager progressionManager;
    private LevelSelectionPopup levelSelectionPopup;
    private SettingsPopup settingsPopup;
    private ChestChaseManager chestChaseManager;
    private DailyRewardsManager dailyRewardsManager;
#if UNITY_EDITOR
    private static bool editorStyleRefreshQueued;
#endif

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyPremiumStyles();
        }
#endif
        BindChestChaseButton();
        BindDailyRewardsButton();
        SubscribeChestChaseUpdates();
        SubscribeDailyRewardsUpdates();
    }

    private void OnDisable()
    {
        if (chestChaseManager != null)
        {
            chestChaseManager.StateChanged -= RefreshChestChaseButton;
        }

        if (dailyRewardsManager != null)
        {
            dailyRewardsManager.StateChanged -= RefreshDailyRewardsButton;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        QueueEditorStyleRefresh();
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
        if (settingsPopup == null)
        {
            return;
        }

        settingsPopup.Show();
    }

    public void OnChestChaseClicked()
    {
        ChestChaseManager manager = ChestChaseManager.GetOrCreate(this);
        ChestChasePopup.GetOrCreate().Show(manager);
    }

    public void OnDailyRewardsClicked()
    {
        DailyRewardsManager manager = DailyRewardsManager.GetOrCreate(this);
        DailyRewardsPopup popup = DailyRewardsPopup.SceneInstance;
        if (popup == null)
        {
            Debug.LogWarning("MainMenuController: DailyRewardsPopup is missing from MainMenu scene.");
            return;
        }

        popup.Show(manager);
    }

    public void OnShopClicked()
    {
        ShopPopup popup = ShopPopup.SceneInstance;
        if (popup == null)
        {
            Debug.LogWarning("MainMenuController: ShopPopup is missing from MainMenu scene.");
            return;
        }

        popup.Show();
    }

#if UNITY_EDITOR
    private static void QueueEditorStyleRefresh()
    {
        if (Application.isPlaying || editorStyleRefreshQueued)
        {
            return;
        }

        editorStyleRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            editorStyleRefreshQueued = false;
            if (!Application.isPlaying)
            {
                ApplyPremiumStyles();
            }
        };
    }
#endif

    private static void ApplyPremiumStyles()
    {
        Color playTextColor = new(0.89f, 0.73f, 0.36f, 1f);
        Color navigationTextColor = new(0.93f, 0.86f, 0.67f, 1f);
        StyleAuthoredLabel("PlayButton", "ИГРАТЬ", playTextColor, 64f);
        StyleNavigationLabel("DailyRewardsButton", "НАГРАДА", navigationTextColor);
        StyleNavigationLabel("ChestChaseButton", "СУНДУКИ", navigationTextColor);
        StyleNavigationLabel("LevelsButton", "УРОВНИ", navigationTextColor);
        StyleNavigationLabel("SettingsButton", "НАСТРОЙКИ", navigationTextColor);
        StyleNavigationLabel("CollectionBookButton", "КОЛЛЕКЦИЯ", navigationTextColor);
        StyleNavigationLabel("ShopButton", "МАГАЗИН", navigationTextColor);
        StyleTopEventButtons();
    }

#if UNITY_EDITOR
    public static void EditorApplyPremiumStyles()
    {
        ApplyPremiumStyles();
    }
#endif

    private static void StyleTopEventButtons()
    {
        RectTransform dailyRewards = GameObject.Find("DailyRewardsButton")?.GetComponent<RectTransform>();
        ApplyTopEventIconLayout(dailyRewards);

        RectTransform chestChase = GameObject.Find("ChestChaseButton")?.GetComponent<RectTransform>();
        ApplyTopEventIconLayout(chestChase);
    }

    private static void ApplyTopEventIconLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Image image = rect.Find("Icon")?.GetComponent<Image>();
        if (image != null)
        {
            image.preserveAspect = true;
            image.color = Color.white;
        }
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

        button.targetGraphic = image;
        button.onClick.RemoveListener(OnChestChaseClicked);
        button.onClick.AddListener(OnChestChaseClicked);

        RefreshChestChaseBadge(root);
    }

    private void BindDailyRewardsButton()
    {
        GameObject existing = GameObject.Find("DailyRewardsButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        Button button = existing != null ? existing.GetComponent<Button>() : null;

        if (root == null || button == null)
        {
            return;
        }

        Image image = root.GetComponent<Image>();
        button.targetGraphic = image;
        RefreshDailyRewardsBadge(root);
    }

    private static void RefreshChestChaseBadge(RectTransform root)
    {
        TMP_Text badgeText = root.Find("ProgressBadge/BadgeText")?.GetComponent<TMP_Text>();
        ChestChaseManager manager = ChestChaseManager.GetOrCreate(root);
        int required = Mathf.Max(1, manager.KeysRequired);
        int current = Mathf.Clamp(manager.CurrentKeys, 0, required);
        Transform badge = root.Find("ProgressBadge");
        if (badge != null)
        {
            badge.gameObject.SetActive(current > 0);
        }

        if (badgeText != null)
        {
            badgeText.text = $"{current}/{required}";
        }

        TMP_Text status = root.Find("Status")?.GetComponent<TMP_Text>();
        if (status != null)
        {
            status.text = current >= required ? "ГОТОВО К ОТКРЫТИЮ" : $"ДОСТУПНО: {current}";
        }
    }

    private static void RefreshDailyRewardsBadge(RectTransform root)
    {
        Transform dot = root.Find("NotificationDot");
        if (dot == null)
        {
            return;
        }

        DailyRewardsManager manager = DailyRewardsManager.GetOrCreate(root);
        dot.gameObject.SetActive(manager.CanClaimToday);

        TMP_Text dotText = dot.Find("Text")?.GetComponent<TMP_Text>();
        if (dotText != null)
        {
            dotText.text = "!";
            RuntimeCyrillicFontProvider.Apply(dotText, dotText.text);
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

    private void SubscribeDailyRewardsUpdates()
    {
        DailyRewardsManager manager = DailyRewardsManager.GetOrCreate(this);
        if (dailyRewardsManager == manager)
        {
            return;
        }

        if (dailyRewardsManager != null)
        {
            dailyRewardsManager.StateChanged -= RefreshDailyRewardsButton;
        }

        dailyRewardsManager = manager;
        if (dailyRewardsManager != null)
        {
            dailyRewardsManager.StateChanged += RefreshDailyRewardsButton;
        }
    }

    private void RefreshChestChaseButton()
    {
        GameObject existing = GameObject.Find("ChestChaseButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (root != null)
        {
            RefreshChestChaseBadge(root);
        }
    }

    private void RefreshDailyRewardsButton()
    {
        GameObject existing = GameObject.Find("DailyRewardsButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (root != null)
        {
            RefreshDailyRewardsBadge(root);
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
            label.textWrappingMode = TextWrappingModes.NoWrap;
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

        UIJuicyButton juicyButton = target.GetComponent<UIJuicyButton>()
            ?? target.AddComponent<UIJuicyButton>();
        juicyButton.ConfigureMobileFeedback(0.96f, 0.08f);

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0.06f, 0.02f, 0f, 0.38f);
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;
    }

    private static void StyleAuthoredLabel(string objectName, string text, Color color, float maxSize)
    {
        GameObject target = GameObject.Find(objectName);
        TMP_Text label = FindExactLabel(target);
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.color = color;
        label.enableVertexGradient = false;
        label.enableAutoSizing = false;
        label.fontSize = maxSize;
        label.fontStyle = FontStyles.Bold;
        label.fontWeight = FontWeight.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 1.2f;
        label.margin = Vector4.zero;
        label.extraPadding = true;
        TMP_FontAsset displayFont =
            Resources.Load<TMP_FontAsset>("Fonts/Prata Cyrillic SDF");
        if (displayFont != null)
        {
            label.font = displayFont;
            Material playMaterial =
                Resources.Load<Material>("Fonts/Prata Cyrillic CTA");
            label.fontSharedMaterial =
                playMaterial != null ? playMaterial : displayFont.material;
        }
    }

    private static void StyleNavigationLabel(string objectName, string text, Color color)
    {
        GameObject target = GameObject.Find(objectName);
        TMP_Text label = FindExactLabel(target);
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.color = color;
        label.enableVertexGradient = false;
        label.enableAutoSizing = false;
        label.fontSize = 27f;
        label.fontStyle = FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 0.2f;
        TMP_FontAsset navigationFont =
            Resources.Load<TMP_FontAsset>("Fonts/Roboto-Bold Cyrillic SDF");
        if (navigationFont != null)
        {
            label.font = navigationFont;
            label.fontSharedMaterial = navigationFont.material;
        }
    }

    private static TMP_Text FindExactLabel(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        foreach (TMP_Text candidate in target.GetComponentsInChildren<TMP_Text>(true))
        {
            if (candidate.name == "Label")
            {
                return candidate;
            }
        }

        return null;
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
