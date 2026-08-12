using CardMesh;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LivesOfferPopupInstaller
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string PrefabPath = "Assets/Prefabs/UI/Game/LivesOfferPopup.prefab";
    private const string LivesHudPrefabPath = "Assets/Prefabs/UI/Game/LivesCounter.prefab";
    private const string HeartPath = "Assets/Art/UI/Lives/heart_life_offer.png";
    private const string FramePath = "Assets/Art/UI/Lives/lives_offer_panel.png";
    private const string GameplayButtonPath = "Assets/Resources/UI/Premium/button_gameplay_action_gold.png";
    private const string CoinPath = "Assets/Resources/UI/Premium/icon_coin_stack.png";
    private const string CirclePath = "Assets/Resources/UI/Premium/icon_circle_clean.png";
    private const string PlayPath = "Assets/Art/UI/MainMenuMobile/nav_play.png";
    private const string FontPath = "Assets/Resources/Fonts/Prata Cyrillic SDF.asset";

    [MenuItem("Tools/Card Mesh/UI/Rebuild Lives Offer Popup")]
    public static void Rebuild()
    {
        ConfigureHeartImporter();
        EnsureAuthoredLivesHudPlusButton();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RectTransform safeArea = FindSceneRect(scene, "SafeAreaRoot");
        if (safeArea == null)
        {
            throw new MissingReferenceException("SafeAreaRoot was not found in Main.unity.");
        }

        Transform existing = FindRecursive(safeArea, "LivesEmptyPopup");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject popup = BuildPopup(safeArea);
        popup.SetActive(false);
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            popup,
            PrefabPath,
            InteractionMode.AutomatedAction);
        popup.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"LivesOfferPopupInstaller: rebuilt {PrefabPath} and installed it in {ScenePath}.");
    }

    [MenuItem("Tools/Card Mesh/UI/Preview Lives Offer Popup")]
    public static void Preview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before previewing the lives offer popup.");
            return;
        }

        SceneManager.LoadScene("Main");
        EditorApplication.delayCall += ShowPreviewAfterSceneLoad;
    }

    private static void ShowPreviewAfterSceneLoad()
    {
        LivesManager lives = Object.FindFirstObjectByType<LivesManager>();
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        if (lives == null || ui == null)
        {
            Debug.LogWarning("LivesOfferPopupInstaller: preview dependencies are not ready.");
            return;
        }

        while (lives.CurrentLives > 0)
        {
            lives.SpendLife();
        }

        ui.ShowLivesEmptyPopup(lives);
    }

    [MenuItem("Tools/Card Mesh/UI/Test Active Lives Rewarded Button")]
    public static void TestActiveRewardedButton()
    {
        TestActiveOfferButton("RewardedButton", grantCoins: false);
    }

    [MenuItem("Tools/Card Mesh/UI/Test Active Lives Coin Button")]
    public static void TestActiveCoinButton()
    {
        TestActiveOfferButton("CoinButton", grantCoins: true);
    }

    [MenuItem("Tools/Card Mesh/UI/Test Active Lives Close Button")]
    public static void TestActiveCloseButton()
    {
        LivesOfferPopupController controller = Object.FindFirstObjectByType<LivesOfferPopupController>();
        Button button = controller != null
            ? FindRecursive(controller.transform, "CloseButton")?.GetComponent<Button>()
            : null;
        if (button == null || !controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("LivesOfferPopupInstaller: active lives close button was not found.");
            return;
        }

        Debug.Log(
            $"LivesOfferPopupInstaller: invoking close button; "
            + $"active={button.gameObject.activeInHierarchy}, interactable={button.interactable}.");
        button.onClick.Invoke();
    }

    [MenuItem("Tools/Card Mesh/UI/Test Active HUD Life Plus")]
    public static void TestActiveHudLifePlus()
    {
        LivesHudView hud = Object.FindFirstObjectByType<LivesHudView>();
        Button button = hud != null
            ? FindRecursive(hud.transform, "AddLifeButton")?.GetComponent<Button>()
            : null;
        if (button == null)
        {
            Debug.LogWarning("LivesOfferPopupInstaller: active HUD life plus was not found.");
            return;
        }

        Debug.Log(
            $"LivesOfferPopupInstaller: invoking HUD life plus; "
            + $"active={button.gameObject.activeInHierarchy}, interactable={button.interactable}.");
        button.onClick.Invoke();
    }

    private static void TestActiveOfferButton(string buttonName, bool grantCoins)
    {
        LivesOfferPopupController controller = Object.FindFirstObjectByType<LivesOfferPopupController>();
        LivesManager lives = Object.FindFirstObjectByType<LivesManager>();
        CoinsManager coins = Object.FindFirstObjectByType<CoinsManager>();
        if (controller == null || lives == null || coins == null || !controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("LivesOfferPopupInstaller: active lives offer popup was not found.");
            return;
        }

        int coinsBeforeTest = coins.CurrentCoins;
        if (grantCoins && coins.CurrentCoins < LivesOfferPopupController.LifeCoinCost)
        {
            coins.AddCoins(LivesOfferPopupController.LifeCoinCost - coins.CurrentCoins);
        }

        Button target = FindRecursive(controller.transform, buttonName)?.GetComponent<Button>();
        target?.onClick.Invoke();
        int coinsAfterAction = coins.CurrentCoins;
        if (grantCoins && coins.CurrentCoins < coinsBeforeTest)
        {
            coins.AddCoins(coinsBeforeTest - coins.CurrentCoins);
        }
        Debug.Log(
            $"LivesOfferPopupInstaller: tested {buttonName}; "
            + $"lives={lives.CurrentLives}/{LivesManager.MaxLives}, coinsAfterAction={coinsAfterAction}, "
            + $"popupActive={controller.gameObject.activeSelf}.");
    }

    private static GameObject BuildPopup(RectTransform safeArea)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite frame = LoadSprite(FramePath);
        Sprite heart = LoadSprite(HeartPath);
        Sprite gameplayButton = LoadSprite(GameplayButtonPath);
        Sprite coin = LoadSprite(CoinPath);
        Sprite circle = LoadSprite(CirclePath);
        Sprite play = LoadSprite(PlayPath);

        GameObject popup = new("LivesEmptyPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(LivesOfferPopupController));
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.SetParent(safeArea, false);
        Stretch(popupRect);
        Image overlay = popup.GetComponent<Image>();
        overlay.color = new Color(0.004f, 0.035f, 0.022f, 0.82f);
        overlay.raycastTarget = true;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform panel = CreateImage(popupRect, "Panel", frame, Color.white, new Vector2(900f, 1125f), Vector2.zero);
        AddShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, -9f));

        TMP_Text title = CreateText(panel, "Title", "НУЖНО БОЛЬШЕ\nЖИЗНЕЙ?", font, 43f, new Color(1f, 0.88f, 0.52f, 1f), new Vector2(620f, 104f), new Vector2(0f, 350f));
        title.fontStyle = FontStyles.Bold;
        title.lineSpacing = -8f;
        AddOutline(title.gameObject, new Color(0.08f, 0.025f, 0f, 0.72f), new Vector2(2f, -2f));

        CreateImage(panel, "TitleDivider", null, new Color(0.91f, 0.66f, 0.17f, 0.92f), new Vector2(500f, 3f), new Vector2(0f, 278f));

        RectTransform heartRoot = CreateImage(panel, "HeartArt", heart, Color.white, new Vector2(230f, 230f), new Vector2(0f, 104f));
        heartRoot.GetComponent<Image>().preserveAspect = true;
        AddShadow(heartRoot.gameObject, new Color(0.08f, 0f, 0f, 0.62f), new Vector2(0f, -8f));

        TMP_Text count = CreateText(heartRoot, "LivesCount", "0/5", font, 55f, Color.white, new Vector2(170f, 74f), new Vector2(0f, -6f));
        count.fontStyle = FontStyles.Bold;
        AddOutline(count.gameObject, new Color(0.24f, 0.015f, 0.015f, 0.82f), new Vector2(2f, -2f));

        RectTransform plusBadge = CreateImage(panel, "PlusBadge", circle, Color.white, new Vector2(108f, 108f), new Vector2(78f, 36f));
        plusBadge.GetComponent<Image>().preserveAspect = true;
        AddShadow(plusBadge.gameObject, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -5f));
        TMP_Text plus = CreateText(plusBadge, "PlusLabel", "+1", font, 41f, new Color(0.98f, 0.80f, 0.28f, 1f), new Vector2(94f, 60f), Vector2.zero);
        plus.fontStyle = FontStyles.Bold;

        TMP_Text caption = CreateText(panel, "Caption", "ПОПОЛНИТЬ ЖИЗНИ", font, 25f, new Color(1f, 0.86f, 0.50f, 1f), new Vector2(600f, 44f), new Vector2(0f, -54f));
        caption.fontStyle = FontStyles.Bold;

        TMP_Text timer = CreateText(panel, "TimerText", "СЛЕДУЮЩАЯ ЖИЗНЬ ЧЕРЕЗ 59м 59с", font, 20f, new Color(0.90f, 0.81f, 0.60f, 1f), new Vector2(590f, 38f), new Vector2(0f, -99f));
        timer.fontStyle = FontStyles.Bold;

        Button coinButton = CreateButton(panel, "CoinButton", gameplayButton, new Vector2(600f, 92f), new Vector2(0f, -211f), false);
        RectTransform coinIcon = CreateImage(coinButton.transform as RectTransform, "CoinIcon", coin, Color.white, new Vector2(52f, 52f), new Vector2(-196f, 0f));
        coinIcon.GetComponent<Image>().preserveAspect = true;
        TMP_Text coinLabel = CreateText(coinButton.transform as RectTransform, "Label", "300 МОНЕТ     +1 ЖИЗНЬ", font, 27f, new Color(1f, 0.90f, 0.63f, 1f), new Vector2(470f, 58f), new Vector2(48f, 0f));
        coinLabel.fontStyle = FontStyles.Bold;

        Button rewardedButton = CreateButton(panel, "RewardedButton", gameplayButton, new Vector2(600f, 92f), new Vector2(0f, -341f), false);
        RectTransform playIcon = CreateImage(rewardedButton.transform as RectTransform, "PlayIcon", play, Color.white, new Vector2(48f, 48f), new Vector2(-196f, 0f));
        playIcon.GetComponent<Image>().preserveAspect = true;
        TMP_Text rewardedLabel = CreateText(rewardedButton.transform as RectTransform, "Label", "СМОТРЕТЬ     +1 ЖИЗНЬ", font, 27f, new Color(1f, 0.90f, 0.63f, 1f), new Vector2(470f, 58f), new Vector2(48f, 0f));
        rewardedLabel.fontStyle = FontStyles.Bold;

        TMP_Text feedback = CreateText(panel, "FeedbackText", string.Empty, font, 19f, new Color(1f, 0.52f, 0.35f, 1f), new Vector2(600f, 34f), new Vector2(0f, -402f));
        feedback.fontStyle = FontStyles.Bold;
        feedback.gameObject.SetActive(false);

        Button closeButton = CreateButton(panel, "CloseButton", null, new Vector2(104f, 104f), new Vector2(326f, 414f), false);
        closeButton.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
        TMP_Text closeLabel = CreateText(closeButton.transform as RectTransform, "Label", "×", font, 72f, new Color(1f, 0.36f, 0.20f, 1f), new Vector2(96f, 96f), new Vector2(0f, 5f));
        closeLabel.fontStyle = FontStyles.Bold;
        AddOutline(closeLabel.gameObject, new Color(0.12f, 0.025f, 0.01f, 0.92f), new Vector2(2f, -2f));

        popup.GetComponent<LivesOfferPopupController>().Configure(null, null, null, null, null);
        return popup;
    }

    private static void ConfigureHeartImporter()
    {
        ConfigureSpriteImporter(HeartPath, 2048);
        ConfigureSpriteImporter(FramePath, 2048);
    }

    private static void ConfigureSpriteImporter(string assetPath, int maxTextureSize)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = maxTextureSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void EnsureAuthoredLivesHudPlusButton()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(LivesHudPrefabPath);
        try
        {
            Transform plusRoot = FindRecursive(prefabRoot.transform, "AddLifeButton");
            if (plusRoot == null)
            {
                Debug.LogWarning("LivesOfferPopupInstaller: AddLifeButton was not found in the lives HUD prefab.");
                return;
            }

            plusRoot.gameObject.SetActive(true);
            RectTransform plusRect = plusRoot as RectTransform;
            plusRect.anchorMin = new Vector2(0.5f, 0.5f);
            plusRect.anchorMax = new Vector2(0.5f, 0.5f);
            plusRect.pivot = new Vector2(0.5f, 0.5f);
            plusRect.sizeDelta = new Vector2(52f, 52f);
            plusRect.anchoredPosition = new Vector2(-56f, -19f);

            Image hitArea = plusRoot.GetComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0.001f);
            hitArea.raycastTarget = true;

            RectTransform visual = FindRecursive(plusRoot, "AddLifeVisual") as RectTransform;
            if (visual != null)
            {
                visual.sizeDelta = new Vector2(42f, 42f);
                visual.anchoredPosition = Vector2.zero;
                visual.GetComponent<Image>().preserveAspect = true;
            }

            Button button = plusRoot.GetComponent<Button>() ?? plusRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = visual != null ? visual.GetComponent<Image>() : hitArea;
            button.transition = Selectable.Transition.ColorTint;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, LivesHudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static RectTransform FindSceneRect(Scene scene, string targetName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.name == targetName ? root.transform : FindRecursive(root.transform, targetName);
            if (found is RectTransform rect)
            {
                return rect;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }

            Transform nested = FindRecursive(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, size, position);
        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font, float fontSize, Color color, Vector2 size, Vector2 position)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, size, position);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(16f, fontSize * 0.72f);
        text.fontSizeMax = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, bool addShadow = true)
    {
        RectTransform rect = CreateImage(parent, name, sprite, Color.white, size, position);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.78f, 1f);
        colors.pressedColor = new Color(0.82f, 0.90f, 0.72f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.72f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        rect.gameObject.AddComponent<UIJuicyButton>();
        if (addShadow)
        {
            AddShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -6f));
        }
        return button;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
