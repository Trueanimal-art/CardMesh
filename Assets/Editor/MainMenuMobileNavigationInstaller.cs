#if UNITY_EDITOR
using System;
using System.IO;
using CardMesh;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MainMenuMobileNavigationInstaller
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string IconDirectory = "Assets/Art/UI/MainMenuEmeraldVault";
    private const string DisplayFontSourcePath = "Assets/Fonts/Prata/Prata-Regular.ttf";
    private const string DisplayFontPath = "Assets/Resources/Fonts/Prata Cyrillic SDF.asset";
    private const string PlayFontMaterialPath =
        "Assets/Resources/Fonts/Prata Cyrillic CTA.mat";
    private const string StatusFontPath = "Assets/Resources/Fonts/Roboto-Bold Cyrillic SDF.asset";

    private static readonly Color Gold = new(0.89f, 0.73f, 0.36f, 1f);
    private static readonly Color Ivory = new(0.93f, 0.86f, 0.67f, 1f);
    private static readonly Color StatusGreen = new(0.55f, 0.76f, 0.43f, 1f);

    [MenuItem("Tools/Card Mesh/Mobile/Rebuild Main Menu Navigation")]
    public static void RebuildMainMenuNavigation()
    {
        EnsureDisplayFontAsset();
        EnsurePlayFontMaterial();
        ConfigureAssetImporters();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RectTransform safeArea = FindInScene<RectTransform>(scene, "SafeAreaRoot");
        if (safeArea == null)
        {
            throw new UnityException("MainMenu mobile navigation: SafeAreaRoot was not found.");
        }

        RectTransform canvasRoot = safeArea.GetComponentInParent<Canvas>()?.transform as RectTransform;
        if (canvasRoot == null)
        {
            throw new UnityException("MainMenu mobile navigation: Canvas was not found.");
        }

        ConfigureMainBackground(canvasRoot);
        RemoveLegacyBottomRail(canvasRoot);

        RectTransform chromeRoot = EnsureRect(safeArea, "MainMenuChromeRoot");
        Stretch(chromeRoot);
        chromeRoot.anchoredPosition = Vector2.zero;
        chromeRoot.sizeDelta = Vector2.zero;

        RectTransform topActivities =
            EnsureZone(safeArea, chromeRoot, "TopActivities");
        SetRect(
            topActivities,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(1080f, 250f),
            new Vector2(0f, -44f));

        RectTransform bottomNavigation =
            EnsureZone(safeArea, chromeRoot, "BottomNavigation");
        bottomNavigation.anchorMin = new Vector2(0f, 0f);
        bottomNavigation.anchorMax = new Vector2(1f, 0f);
        bottomNavigation.pivot = new Vector2(0.5f, 0f);
        bottomNavigation.anchoredPosition = Vector2.zero;
        bottomNavigation.sizeDelta = new Vector2(0f, 490f);
        ConfigureBottomPanel(bottomNavigation);

        GameObject daily = RequireSceneObject(scene, "DailyRewardsButton");
        GameObject chests = RequireSceneObject(scene, "ChestChaseButton");
        GameObject levels = RequireSceneObject(scene, "LevelsButton");
        GameObject collection = RequireSceneObject(scene, "CollectionBookButton");
        GameObject play = RequireSceneObject(scene, "PlayButton");
        GameObject shop = RequireSceneObject(scene, "ShopButton");
        GameObject settings = RequireSceneObject(scene, "SettingsButton");

        ConfigureVariantHBottomItem(
            levels,
            topActivities,
            0.16f,
            "УРОВНИ",
            "h4_nav_medallion_base.png",
            "icon_levels_royal_seal.png",
            false);
        ConfigureVariantHBottomItem(
            settings,
            topActivities,
            0.84f,
            "НАСТРОЙКИ",
            "h4_nav_medallion_base.png",
            "icon_settings_royal_seal.png",
            false,
            0.97f);

        EnsureDailyRewardBadge(daily.transform as RectTransform);
        ConfigureVariantHBottomItem(
            daily,
            bottomNavigation,
            0.125f,
            "НАГРАДА",
            "h4_nav_medallion_base.png",
            "icon_reward_royal_seal.png",
            false);
        ConfigureDailyRewardBadgeAttachment(daily.transform as RectTransform);
        ConfigureVariantHBottomItem(
            chests,
            bottomNavigation,
            0.375f,
            "СУНДУКИ",
            "h4_nav_medallion_base.png",
            "icon_chests_royal_seal.png",
            false);
        ConfigureVariantHBottomItem(
            play,
            bottomNavigation,
            0.5f,
            "ИГРАТЬ",
            "h5_play_cta_clean.png",
            null,
            true);
        ConfigureVariantHBottomItem(
            collection,
            bottomNavigation,
            0.625f,
            "КОЛЛЕКЦИЯ",
            "h4_nav_medallion_base.png",
            "icon_collection_royal_seal.png",
            false,
            1.04f);
        ConfigureVariantHBottomItem(
            shop,
            bottomNavigation,
            0.875f,
            "МАГАЗИН",
            "h4_nav_medallion_base.png",
            "icon_shop_royal_seal.png",
            false);
        SetChildActive(collection.transform as RectTransform, "ProgressBadge", false);
        SetChildActive(collection.transform as RectTransform, "NotificationDot", false);
        SetChildActive(levels.transform as RectTransform, "ProgressBadge", false);
        SetChildActive(levels.transform as RectTransform, "NotificationDot", false);
        SetChildActive(settings.transform as RectTransform, "ProgressBadge", false);
        SetChildActive(settings.transform as RectTransform, "NotificationDot", false);

        SceneLoader sceneLoader = Object.FindFirstObjectByType<SceneLoader>(FindObjectsInactive.Include);
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (sceneLoader == null || controller == null)
        {
            throw new UnityException("MainMenu mobile navigation: SceneLoader or MainMenuController is missing.");
        }

        ConfigurePersistentAction(levels, sceneLoader.ShowLevelsPlaceholder);
        ConfigurePersistentAction(play, sceneLoader.LoadGame);
        ConfigurePersistentAction(settings, sceneLoader.ShowSettingsPlaceholder);
        ClearPersistentActions(collection);
        ClearPersistentActions(shop);
        ClearPersistentActions(daily);
        ClearPersistentActions(chests);

        SetDirectChildActive(safeArea, "ContactButton", false);
        SetDirectChildActive(safeArea, "RateUsButton", false);
        SetSettingsActionActive(scene, "SupportButton", true);
        SetSettingsActionActive(scene, "RateUsButton", true);

        topActivities.SetAsFirstSibling();
        bottomNavigation.SetAsLastSibling();
        chromeRoot.SetAsLastSibling();
        safeArea.SetAsLastSibling();
        MainMenuController.EditorApplyPremiumStyles();
        EditorUtility.SetDirty(safeArea);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneVisibilityManager.instance.ShowAll();
        Selection.activeGameObject = canvasRoot.gameObject;
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.FrameSelected(false);
            sceneView.size *= 1.15f;
            sceneView.Repaint();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Emerald Vault Variant H6 MainMenu rebuilt: Levels and Settings are authored " +
            "as top utility medallions; Reward, Chests, Collection and Shop form the " +
            "bottom action row; Play remains a detached centered CTA; the complete chrome " +
            "is scene-authored under MainMenuChromeRoot with no runtime layout builders.");
    }

    private static void ConfigureVariantHBottomItem(
        GameObject root,
        RectTransform parent,
        float anchorX,
        string label,
        string visualFile,
        string glyphFile,
        bool primary,
        float glyphScale = 1f)
    {
        RectTransform rect = root.transform as RectTransform;
        rect.SetParent(parent, false);
        if (primary)
        {
            SetRect(
                rect,
                new Vector2(anchorX, 0f),
                new Vector2(anchorX, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(780f, 300f),
                new Vector2(0f, 340f));
        }
        else
        {
            SetRect(
                rect,
                new Vector2(anchorX, 0f),
                new Vector2(anchorX, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(236f, 238f),
                new Vector2(0f, 12f));
        }

        Button button = RequireButton(root);
        RectTransform surface = button.transform as RectTransform;
        if (surface != rect)
        {
            Stretch(surface);
        }

        ConfigureHitSurface(surface, button, primary);
        ConfigureLegacyRootGraphic(rect, surface);
        DisableBottomLegacyChildren(rect, surface);

        Image visual = EnsureImage(surface, "Visual");
        if (primary)
        {
            SetRect(
                visual.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 300f),
                Vector2.zero);
        }
        else
        {
            SetRect(
                visual.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(204f, 204f),
                Vector2.zero);
        }
        ConfigureVariantHVisual(visual, visualFile);
        visual.rectTransform.SetAsFirstSibling();

        Image glyph = EnsureImage(surface, "Glyph");
        if (primary)
        {
            glyph.gameObject.SetActive(false);
        }
        else
        {
            glyph.gameObject.SetActive(true);
            float glyphSize = 152f * glyphScale;
            float glyphTop = -25f + ((glyphSize - 152f) * 0.5f);
            SetRect(
                glyph.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(glyphSize, glyphSize),
                new Vector2(0f, glyphTop));
            ConfigureIcon(glyph, glyphFile);
            glyph.rectTransform.SetSiblingIndex(1);
        }

        TextMeshProUGUI text = EnsureLabel(surface, "Label");
        if (primary)
        {
            SetRect(
                text.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(460f, 92f),
                new Vector2(0f, 5f));
            ConfigurePlayLabel(text, label);
        }
        else
        {
            SetRect(
                text.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(220f, 38f),
                new Vector2(0f, 2f));
            ConfigureNavigationLabel(text, label);
        }
        // The CTA art is intentionally opaque; keep live, localizable text above it.
        text.rectTransform.SetAsLastSibling();

        ConfigureBadge(
            FindChildRecursive(rect, "ProgressBadge") as RectTransform,
            new Vector2(-8f, -4f),
            new Vector2(52f, 52f));
        ConfigureBadge(
            FindChildRecursive(rect, "NotificationDot") as RectTransform,
            new Vector2(-8f, -4f),
            new Vector2(52f, 52f));
    }

    private static void ConfigureBottomPanel(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.clear;
        image.raycastTarget = false;

        Image divider = EnsureImage(panel, "TopDivider");
        divider.gameObject.SetActive(false);
        divider.color = Color.clear;
        divider.raycastTarget = false;
        divider.rectTransform.anchorMin = new Vector2(0f, 1f);
        divider.rectTransform.anchorMax = new Vector2(1f, 1f);
        divider.rectTransform.pivot = new Vector2(0.5f, 1f);
        divider.rectTransform.anchoredPosition = Vector2.zero;
        divider.rectTransform.sizeDelta = new Vector2(0f, 3f);
    }

    private static void ConfigureHitSurface(RectTransform surface, Button button, bool primary)
    {
        Image image = surface.GetComponent<Image>() ?? surface.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        UIJuicyButton juicy = surface.GetComponent<UIJuicyButton>()
            ?? surface.gameObject.AddComponent<UIJuicyButton>();
        juicy.ConfigureMobileFeedback(primary ? 0.97f : 0.96f, 0.08f);

        surface.localScale = Vector3.one;
        RecordPrefabOverride(image);
        RecordPrefabOverride(button);
        RecordPrefabOverride(juicy);
        RecordPrefabOverride(surface);
    }

    private static void ConfigureVariantHVisual(Image image, string visualFile)
    {
        image.sprite = LoadSprite(visualFile);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
        image.maskable = true;
        RecordPrefabOverride(image);
    }

    private static void ConfigurePlayLabel(TextMeshProUGUI label, string text)
    {
        label.text = text;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        label.fontSharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(PlayFontMaterialPath);
        label.color = Gold;
        label.fontStyle = FontStyles.Bold;
        label.fontWeight = FontWeight.Bold;
        label.enableAutoSizing = false;
        label.fontSize = 64f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 1.2f;
        label.margin = Vector4.zero;
        label.extraPadding = true;
        label.raycastTarget = false;
        ConfigureTextOutline(label, new Vector2(1f, -1f));
    }

    private static void ConfigureNavigationLabel(TextMeshProUGUI label, string text)
    {
        label.text = text;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StatusFontPath);
        label.color = Ivory;
        label.fontStyle = FontStyles.Normal;
        label.enableAutoSizing = false;
        label.fontSize = 27f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 0.2f;
        label.raycastTarget = false;
        ConfigureTextOutline(label, new Vector2(1f, -1f));
    }

    private static void ConfigureTextOutline(TextMeshProUGUI label, Vector2 distance)
    {
        Outline outline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.02f, 0.01f, 0f, 0.62f);
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
        RecordPrefabOverride(label);
        RecordPrefabOverride(outline);
    }

    private static void ConfigureSurface(RectTransform surface, Button button, bool primary)
    {
        Image image = surface.GetComponent<Image>() ?? surface.gameObject.AddComponent<Image>();
        image.sprite = primary ? LoadSprite("play_crest.png") : null;
        image.type = Image.Type.Simple;
        image.preserveAspect = primary;
        image.color = primary
            ? new Color(0.92f, 0.9f, 0.82f, 1f)
            : new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = primary
            ? Selectable.Transition.ColorTint
            : Selectable.Transition.None;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.05f, 1.03f, 0.94f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.76f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.42f, 0.42f, 0.38f, 0.4f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        UIJuicyButton juicy = surface.GetComponent<UIJuicyButton>()
            ?? surface.gameObject.AddComponent<UIJuicyButton>();
        juicy.ConfigureMobileFeedback(0.96f, 0.08f);

        surface.localScale = Vector3.one;
        RecordPrefabOverride(image);
        RecordPrefabOverride(button);
        RecordPrefabOverride(juicy);
        RecordPrefabOverride(surface);
    }

    private static void ConfigureLegacyRootGraphic(RectTransform root, RectTransform surface)
    {
        Image rootImage = root.GetComponent<Image>();
        if (rootImage == null)
        {
            return;
        }

        if (root == surface)
        {
            rootImage.enabled = true;
            rootImage.sprite = null;
            rootImage.color = new Color(0f, 0f, 0f, 0.001f);
            rootImage.raycastTarget = true;
        }
        else
        {
            rootImage.enabled = false;
            rootImage.raycastTarget = false;
        }

        RecordPrefabOverride(rootImage);
    }

    private static Button RequireButton(GameObject root)
    {
        Button button = root.GetComponent<Button>()
            ?? root.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            throw new UnityException($"MainMenu mobile navigation: {root.name} has no Button.");
        }

        return button;
    }

    private static void ConfigurePersistentAction(GameObject root, UnityAction action)
    {
        Button button = RequireButton(root);
        ClearPersistentListeners(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        RecordPrefabOverride(button);
    }

    private static void ClearPersistentActions(GameObject root)
    {
        Button button = RequireButton(root);
        ClearPersistentListeners(button);
        RecordPrefabOverride(button);
    }

    private static void ClearPersistentListeners(Button button)
    {
        for (int index = button.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, index);
        }
    }

    private static void DisableLegacyChildren(RectTransform parent, params string[] keepNames)
    {
        foreach (Transform child in parent)
        {
            bool keep = Array.IndexOf(keepNames, child.name) >= 0;
            child.gameObject.SetActive(keep);
            RecordPrefabOverride(child.gameObject);
        }
    }

    private static void DisableBottomLegacyChildren(RectTransform root, RectTransform surface)
    {
        if (root != surface)
        {
            foreach (Transform child in root)
            {
                bool keep = child == surface
                    || child.name == "ProgressBadge"
                    || child.name == "NotificationDot";
                child.gameObject.SetActive(keep);
                RecordPrefabOverride(child.gameObject);
            }
        }

        foreach (Transform child in surface)
        {
            bool keep = child.name == "Visual"
                || child.name == "Glyph"
                || child.name == "Label"
                || child.name == "ProgressBadge"
                || child.name == "NotificationDot";
            child.gameObject.SetActive(keep);
            RecordPrefabOverride(child.gameObject);
        }
    }

    private static Image EnsureImage(RectTransform parent, string name)
    {
        RectTransform child = parent.Find(name) as RectTransform;
        if (child == null)
        {
            GameObject childObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            childObject.layer = parent.gameObject.layer;
            child = childObject.transform as RectTransform;
            child.SetParent(parent, false);
        }

        child.gameObject.SetActive(true);
        return child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
    }

    private static TextMeshProUGUI EnsureLabel(RectTransform parent, string name)
    {
        RectTransform child = parent.Find(name) as RectTransform;
        if (child == null)
        {
            GameObject childObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            childObject.layer = parent.gameObject.layer;
            child = childObject.transform as RectTransform;
            child.SetParent(parent, false);
        }

        child.gameObject.SetActive(true);
        return child.GetComponent<TextMeshProUGUI>() ?? child.gameObject.AddComponent<TextMeshProUGUI>();
    }

    private static void ConfigureIcon(Image image, string iconFile)
    {
        image.sprite = LoadSprite(iconFile);
        if (image.sprite == null)
        {
            throw new UnityException($"MainMenu mobile navigation: icon {iconFile} is missing.");
        }

        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        RecordPrefabOverride(image);
    }

    private static void ConfigureLabel(
        TextMeshProUGUI label,
        string text,
        float maxSize,
        float minSize,
        Color color)
    {
        label.text = text;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        label.color = color;
        label.fontStyle = FontStyles.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMax = maxSize;
        label.fontSizeMin = minSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 1.5f;
        label.raycastTarget = false;

        Outline outline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.03f, 0.012f, 0f, 0.48f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
        RecordPrefabOverride(label);
        RecordPrefabOverride(outline);
    }

    private static void ConfigureStatusLabel(TextMeshProUGUI label, string text)
    {
        label.text = text;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StatusFontPath);
        label.color = StatusGreen;
        label.fontStyle = FontStyles.Normal;
        label.enableAutoSizing = true;
        label.fontSizeMax = 24f;
        label.fontSizeMin = 20f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 0.5f;
        label.raycastTarget = false;
        RecordPrefabOverride(label);
    }

    private static void ConfigureBadge(RectTransform badge, Vector2 position, Vector2 size)
    {
        if (badge == null)
        {
            return;
        }

        badge.anchorMin = Vector2.one;
        badge.anchorMax = Vector2.one;
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = position;
        badge.sizeDelta = size;
        badge.SetAsLastSibling();

        Image image = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite("badge_diamond.png");
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        TMP_Text text = badge.Find("BadgeText")?.GetComponent<TMP_Text>()
            ?? badge.Find("Text")?.GetComponent<TMP_Text>();
        if (text != null)
        {
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StatusFontPath);
            text.fontStyle = FontStyles.Normal;
            text.color = Ivory;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = 23f;
            text.fontSizeMin = 16f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            RecordPrefabOverride(text);
        }

        RecordPrefabOverride(image);
        RecordPrefabOverride(badge);
    }

    private static void ConfigureDailyRewardBadgeAttachment(RectTransform root)
    {
        RectTransform badge = FindChildRecursive(root, "NotificationDot") as RectTransform;
        if (badge == null)
        {
            return;
        }

        badge.anchorMin = new Vector2(0.5f, 1f);
        badge.anchorMax = new Vector2(0.5f, 1f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.anchoredPosition = new Vector2(72f, -30f);
        badge.sizeDelta = new Vector2(52f, 52f);
        badge.localScale = Vector3.one;
        badge.SetAsLastSibling();
        RecordPrefabOverride(badge);
    }

    private static void ConfigureAssetImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconDirectory });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            bool largeSurface = path.EndsWith("bottom_rail.png", StringComparison.Ordinal)
                || path.EndsWith("top_activity_frame.png", StringComparison.Ordinal)
                || path.EndsWith("play_crest.png", StringComparison.Ordinal)
                || path.EndsWith("h_play_cta.png", StringComparison.Ordinal)
                || path.EndsWith("h2_play_cta.png", StringComparison.Ordinal)
                || path.EndsWith("h3_play_cta.png", StringComparison.Ordinal)
                || path.EndsWith("h2_top_reward.png", StringComparison.Ordinal)
                || path.EndsWith("h2_top_chests.png", StringComparison.Ordinal);
            importer.maxTextureSize = largeSurface ? 2048 : 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (path.EndsWith("top_activity_frame.png", StringComparison.Ordinal))
            {
                importer.spriteBorder = Vector4.zero;
            }
            else if (path.EndsWith("bottom_rail.png", StringComparison.Ordinal))
            {
                importer.spriteBorder = new Vector4(64f, 64f, 64f, 64f);
            }
            else
            {
                importer.spriteBorder = Vector4.zero;
            }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = largeSurface ? 2048 : 512;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 100;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        ConfigureStandaloneSprite(
            "Assets/Resources/UI/Premium/popup_frame_mobile_h3.png",
            2048);
    }

    private static void ConfigureStandaloneSprite(string path, int maxTextureSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new UnityException($"MainMenu mobile navigation: texture importer is missing for {path}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        TextureImporterSettings settings = new();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = maxTextureSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.spriteBorder = Vector4.zero;

        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = maxTextureSize;
        android.format = TextureImporterFormat.ASTC_4x4;
        android.compressionQuality = 100;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }

    private static void EnsureDisplayFontAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath) != null)
        {
            return;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontSourcePath);
        if (sourceFont == null)
        {
            throw new UnityException(
                $"MainMenu mobile navigation: display font is missing at {DisplayFontSourcePath}.");
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            96,
            10,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);
        if (fontAsset == null)
        {
            throw new UnityException("MainMenu mobile navigation: failed to create Prata TMP font asset.");
        }

        fontAsset.name = "Prata Cyrillic SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.TryAddCharacters(
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "0123456789:/!?-+ ");

        string directory = Path.GetDirectoryName(DisplayFontPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(fontAsset, DisplayFontPath);
        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas != null && !AssetDatabase.Contains(atlas))
            {
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
    }

    private static void EnsurePlayFontMaterial()
    {
        TMP_FontAsset fontAsset =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayFontPath);
        if (fontAsset == null || fontAsset.material == null)
        {
            throw new UnityException(
                "MainMenu mobile navigation: Prata TMP material is missing.");
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(PlayFontMaterialPath);
        if (material == null)
        {
            material = new Material(fontAsset.material)
            {
                name = "Prata Cyrillic CTA"
            };
            AssetDatabase.CreateAsset(material, PlayFontMaterialPath);
        }
        else
        {
            material.shader = fontAsset.material.shader;
            material.CopyPropertiesFromMaterial(fontAsset.material);
        }

        material.EnableKeyword("OUTLINE_ON");
        if (material.HasProperty("_FaceDilate"))
        {
            material.SetFloat("_FaceDilate", 0.22f);
        }
        if (material.HasProperty("_WeightBold"))
        {
            material.SetFloat("_WeightBold", 0.78f);
        }
        if (material.HasProperty("_OutlineColor"))
        {
            material.SetColor(
                "_OutlineColor",
                new Color(0.14f, 0.055f, 0.005f, 0.95f));
        }
        if (material.HasProperty("_OutlineWidth"))
        {
            material.SetFloat("_OutlineWidth", 0.11f);
        }
        if (material.HasProperty("_OutlineSoftness"))
        {
            material.SetFloat("_OutlineSoftness", 0.015f);
        }

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    private static void RemoveLegacyBottomRail(RectTransform canvasRoot)
    {
        RectTransform rail = canvasRoot.Find("BottomRailVisual") as RectTransform;
        if (rail != null)
        {
            Undo.DestroyObjectImmediate(rail.gameObject);
        }
    }

    private static void ConfigureMainBackground(RectTransform canvasRoot)
    {
        RectTransform offsetLayer = EnsureRect(canvasRoot, "BackgroundOffsetLayer");
        Stretch(offsetLayer);
        offsetLayer.sizeDelta = new Vector2(0f, 216f);
        offsetLayer.anchoredPosition = new Vector2(0f, -104f);
        RecordPrefabOverride(offsetLayer);

        RectTransform background =
            canvasRoot.Find("Background") as RectTransform
            ?? offsetLayer.Find("Background") as RectTransform;
        if (background == null)
        {
            throw new UnityException("MainMenu mobile navigation: main Background was not found.");
        }

        background.SetParent(offsetLayer, false);
        offsetLayer.SetAsFirstSibling();
        background.SetAsFirstSibling();
        RecordPrefabOverride(background);
    }

    private static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconDirectory}/{fileName}");
        if (sprite == null)
        {
            throw new UnityException(
                $"MainMenu mobile navigation: Emerald Vault asset {fileName} is missing.");
        }

        return sprite;
    }

    private static RectTransform EnsureRect(RectTransform parent, string name)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            rect = child.transform as RectTransform;
            rect.SetParent(parent, false);
        }

        rect.gameObject.SetActive(true);
        return rect;
    }

    private static RectTransform EnsureZone(
        RectTransform legacyParent,
        RectTransform chromeRoot,
        string name)
    {
        RectTransform zone = chromeRoot.Find(name) as RectTransform;
        if (zone == null)
        {
            zone = legacyParent.Find(name) as RectTransform;
            if (zone != null)
            {
                zone.SetParent(chromeRoot, false);
            }
        }

        return zone ?? EnsureRect(chromeRoot, name);
    }

    private static GameObject RequireSceneObject(Scene scene, string name)
    {
        RectTransform rect = FindInScene<RectTransform>(scene, name);
        if (rect == null)
        {
            throw new UnityException($"MainMenu mobile navigation: {name} was not found.");
        }

        rect.gameObject.SetActive(true);
        return rect.gameObject;
    }

    private static T FindInScene<T>(Scene scene, string name) where T : Component
    {
        foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (component != null && component.gameObject.scene == scene && component.name == name)
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = root.GetChild(index);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void SetDirectChildActive(RectTransform parent, string name, bool active)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect != null)
        {
            rect.gameObject.SetActive(active);
            EditorUtility.SetDirty(rect.gameObject);
        }
    }

    private static void SetChildActive(RectTransform parent, string name, bool active)
    {
        RectTransform rect = parent?.Find(name) as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.gameObject.SetActive(active);
        RecordPrefabOverride(rect.gameObject);
    }

    private static void SetSettingsActionActive(Scene scene, string name, bool active)
    {
        SettingsPopup settingsPopup = null;
        foreach (SettingsPopup candidate in Object.FindObjectsByType<SettingsPopup>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.gameObject.scene == scene)
            {
                settingsPopup = candidate;
                break;
            }
        }

        if (settingsPopup == null)
        {
            return;
        }

        foreach (RectTransform rect in settingsPopup.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name != name)
            {
                continue;
            }

            rect.gameObject.SetActive(active);
            EditorUtility.SetDirty(rect.gameObject);
            return;
        }
    }

    private static void EnsureDailyRewardBadge(RectTransform root)
    {
        Image badge = EnsureImage(root, "NotificationDot");
        SetRect(
            badge.rectTransform,
            Vector2.one,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(56f, 56f),
            new Vector2(-10f, -6f));
        badge.sprite = LoadSprite("badge_diamond.png");
        badge.type = Image.Type.Simple;
        badge.preserveAspect = true;
        badge.color = Color.white;
        badge.raycastTarget = false;

        TextMeshProUGUI badgeText = EnsureLabel(badge.rectTransform, "Text");
        Stretch(badgeText.rectTransform);
        badgeText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StatusFontPath);
        badgeText.text = "!";
        badgeText.color = Ivory;
        badgeText.fontStyle = FontStyles.Normal;
        badgeText.enableAutoSizing = true;
        badgeText.fontSizeMax = 24f;
        badgeText.fontSizeMin = 18f;
        badgeText.textWrappingMode = TextWrappingModes.NoWrap;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.raycastTarget = false;
        if (badge.GetComponent<UIAttentionPulse>() == null)
        {
            badge.gameObject.AddComponent<UIAttentionPulse>();
        }
        badge.rectTransform.SetAsLastSibling();
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 size,
        Vector2 position)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        RecordPrefabOverride(rect);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        RecordPrefabOverride(rect);
    }

    private static void RecordPrefabOverride(Object target)
    {
        if (target == null)
        {
            return;
        }

        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
#endif
