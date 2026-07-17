using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SolitaireAssociations;

public class MainMenuController : MonoBehaviour
{
    private const float TopEventIconSize = 144f;
    private const float TopEventIconRightX = -120f;
    private const float TopEventIconSpacing = 176f;

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
        BindShopButton();
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
        Color menuTextColor = new(1f, 0.847f, 0.353f, 1f);
        StyleButton("PlayButton", PremiumUISprites.PrimaryButton, menuTextColor, 42f, true);
        StyleButton("LevelsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleButton("SettingsButton", PremiumUISprites.PrimaryButton, menuTextColor, 28f, true);
        StyleMainMenuButtonText("PlayButton", "ИГРАТЬ", 66f, 42f, 2f, 0f);
        StyleMainMenuButtonText("LevelsButton", "УРОВНИ", 42f, 30f, 4f, -3f);
        StyleMainMenuButtonText("SettingsButton", "НАСТРОЙКИ", 33f, 25f, 9f, -3f);
        StyleTopEventButtons();
    }

    private static void StyleTopEventButtons()
    {
        RectTransform shop = GameObject.Find("ShopButton")?.GetComponent<RectTransform>();
        ApplyTopEventIconLayout(shop, null);

        RectTransform dailyRewards = GameObject.Find("DailyRewardsButton")?.GetComponent<RectTransform>();
        ApplyTopEventIconLayout(dailyRewards, TopEventIconRightX);

        RectTransform chestChase = GameObject.Find("ChestChaseButton")?.GetComponent<RectTransform>();
        ApplyTopEventIconLayout(chestChase, TopEventIconRightX - TopEventIconSpacing);

        SoundToggleButton collectionBookButton = FindFirstObjectByType<SoundToggleButton>(FindObjectsInactive.Include);
        RectTransform collectionBook = collectionBookButton != null ? collectionBookButton.GetComponent<RectTransform>() : null;
        ApplyTopEventIconLayout(collectionBook, TopEventIconRightX - (TopEventIconSpacing * 2f));
    }

    private static void ApplyTopEventIconLayout(RectTransform rect, float? xPosition)
    {
        if (rect == null)
        {
            return;
        }

        rect.sizeDelta = new Vector2(TopEventIconSize, TopEventIconSize);
        if (xPosition.HasValue)
        {
            rect.anchoredPosition = new Vector2(xPosition.Value, rect.anchoredPosition.y);
        }

        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            RestoreTopEventIconImage(rect, image);
            image.preserveAspect = true;
            image.color = Color.white;
        }
    }

    private static void RestoreTopEventIconImage(RectTransform rect, Image image)
    {
        Transform generatedIcon = rect.Find("TopEventIcon");
        if (generatedIcon != null && generatedIcon.TryGetComponent(out Image iconImage) && iconImage.sprite != null)
        {
            image.sprite = iconImage.sprite;
            image.type = Image.Type.Simple;
            image.color = iconImage.color;
        }

        RemoveGeneratedTopEventLayer(rect, "TopEventFrameGlow");
        RemoveGeneratedTopEventLayer(rect, "TopEventIconHalo");
        RemoveGeneratedTopEventLayer(rect, "TopEventIcon");
        RemoveGeneratedTopEventLayer(rect, "TopEventTopGloss");
    }

    private static void RemoveGeneratedTopEventLayer(RectTransform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(child.gameObject);
        }
        else
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private void BindShopButton()
    {
        GameObject existing = GameObject.Find("ShopButton");
        RectTransform root = existing != null ? existing.GetComponent<RectTransform>() : null;
        Button button = existing != null ? existing.GetComponent<Button>() : null;

        if (root == null || button == null)
        {
            return;
        }

        Image image = root.GetComponent<Image>();
        button.targetGraphic = image;
        button.onClick.RemoveListener(OnShopClicked);
        button.onClick.AddListener(OnShopClicked);
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
        if (badgeText == null)
        {
            return;
        }

        ChestChaseManager manager = ChestChaseManager.GetOrCreate(root);
        int required = Mathf.Max(1, manager.KeysRequired);
        int current = Mathf.Clamp(manager.CurrentKeys, 0, required);
        badgeText.text = $"{current}/{required}";
        RuntimeCyrillicFontProvider.Apply(badgeText, badgeText.text);
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

    private static void StyleMainMenuButtonText(string objectName, string text, float maxSize, float minSize, float xOffset, float yOffset)
    {
        GameObject target = GameObject.Find(objectName);
        TMP_Text label = target != null ? target.GetComponentInChildren<TMP_Text>(true) : null;
        if (label == null)
        {
            return;
        }

        label.text = text;
        label.color = new Color(1f, 0.847f, 0.353f, 1f);
        label.enableVertexGradient = false;
        label.enableAutoSizing = true;
        label.fontSizeMax = maxSize;
        label.fontSizeMin = minSize;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 0f;

        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform labelRect = label.rectTransform;
        if (targetRect != null && labelRect != null)
        {
            float rectWidth = Mathf.Max(120f, targetRect.rect.width * 0.72f);
            float rectHeight = Mathf.Max(54f, targetRect.rect.height * 0.34f);
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(xOffset, yOffset);
            labelRect.sizeDelta = new Vector2(rectWidth, rectHeight);
        }

        Material material = label.fontMaterial;
        if (material != null)
        {
            material.EnableKeyword("OUTLINE_ON");
            material.DisableKeyword("UNDERLAY_ON");
            material.DisableKeyword("GLOW_ON");
            material.SetColor(ShaderUtilities.ID_FaceColor, new Color(1f, 0.847f, 0.353f, 1f));
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.169f, 0.078f, 0.016f, 1f));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.01f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.055f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);
            label.fontMaterial = material;
        }

        Shadow textShadow = label.GetComponent<Shadow>();
        if (textShadow == null)
        {
            textShadow = label.gameObject.AddComponent<Shadow>();
        }

        textShadow.effectColor = new Color(0.03f, 0.012f, 0f, 0.3f);
        textShadow.effectDistance = new Vector2(1f, -2f);
        textShadow.useGraphicAlpha = true;

        Outline outline = label.GetComponent<Outline>();
        if (outline == null)
        {
            outline = label.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.17f, 0.078f, 0.016f, 0.7f);
        outline.effectDistance = new Vector2(1.25f, -1.25f);
        outline.useGraphicAlpha = true;
        label.UpdateMeshPadding();
        label.ForceMeshUpdate();
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
