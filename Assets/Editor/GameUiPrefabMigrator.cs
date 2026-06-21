#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SolitaireAssociations;

[InitializeOnLoad]
public static class GameUiPrefabMigrator
{
    private const string RequestPath = "Assets/Editor/.run_game_ui_prefab_migration";
    private const string PrefabRoot = "Assets/Prefabs/UI/Game";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string GameplayButtonSpritePath = "Assets/Resources/UI/Premium/button_gameplay_action_gold.png";
    private const string CategorySlotSpritePath = "Assets/Resources/UI/Premium/category_slot_cardmesh.png";
    private const string CategoryCrownSpritePath = "Assets/Resources/UI/Premium/category_slot_crown_cardmesh.png";
    private const string LivesHeartSpritePath = "Assets/Resources/UI/Premium/icon_lives_heart.png";
    private const string MenuButtonSpritePath = "Assets/Resources/UI/Premium/menu_button_cardmesh.png";
    private const string PauseWindowSpritePath = "Assets/Resources/UI/Premium/pause_window_cardmesh.png";

    private static readonly Vector2 BottomButtonSize = new(392f, 116f);
    private static readonly Color TextGold = new(1f, 0.89f, 0.42f, 1f);
    private static readonly Color TextDarkGold = new(0.17f, 0.078f, 0.016f, 0.68f);

    static GameUiPrefabMigrator()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    [MenuItem("Tools/CardMesh/Migrate Main UI To Prefabs")]
    public static void RunFromMenu()
    {
        RunMigration();
    }

    [MenuItem("Tools/CardMesh/Validate Main Runtime Binding")]
    public static void ValidateMainRuntimeBinding()
    {
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        SolitaireSceneRefs refs = UnityEngine.Object.FindFirstObjectByType<SolitaireSceneRefs>();
        bool refsValid = refs != null && refs.ResolveSceneReferences();
        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        RuntimeSceneBootstrapper.BindManualScene(gameManager);
        gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        Debug.Log(
            $"GameUiPrefabMigrator: validation refsValid={refsValid}, " +
            $"hasBindings={gameManager != null && gameManager.HasPresentationBindings}, " +
            $"level={(gameManager != null && gameManager.AssignedLevelDefinition != null ? gameManager.AssignedLevelDefinition.name : "null")}, " +
            $"columns={(refs != null && refs.Columns != null ? refs.Columns.Count : 0)}.");
    }

    private static void RunIfRequested()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        File.Delete(RequestPath);
        RunMigration();
    }

    private static void RunMigration()
    {
        if (File.Exists(RequestPath))
        {
            File.Delete(RequestPath);
        }

        Directory.CreateDirectory(PrefabRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(CategorySlotSpritePath));

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != MainScenePath)
        {
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        CreateGeneratedSlotSprites();
        CreatePrefabs();
        ApplyMainScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GameUiPrefabMigrator: Main UI prefab migration completed.");
    }

    private static void CreatePrefabs()
    {
        SavePrefab(CreateGameHudPrefab(), $"{PrefabRoot}/GameHud.prefab");
        SavePrefab(CreateBottomGameButtonPrefab("BottomGameButton", "КНОПКА", PremiumUISprites.HintBulbIcon, true), $"{PrefabRoot}/BottomGameButton.prefab");
        SavePrefab(CreateBottomGameButtonPrefab("HintButton", "Подсказка", PremiumUISprites.HintBulbIcon, true), $"{PrefabRoot}/HintButton.prefab");
        SavePrefab(CreateBottomGameButtonPrefab("UndoButton", "Отмена", PremiumUISprites.UndoIcon, true), $"{PrefabRoot}/UndoButton.prefab");
        SavePrefab(CreateBottomGameButtonPrefab("BoosterDeckButton", "+10 ходов", PremiumUISprites.BoosterRocketIcon, true), $"{PrefabRoot}/BoosterButton.prefab");
        SavePrefab(CreateCategorySlotPrefab(), $"{PrefabRoot}/CategorySlot.prefab");
        SavePrefab(CreateCounterPrefab("CurrencyCounter", "CoinsPanel", "C", "185"), $"{PrefabRoot}/CurrencyCounter.prefab");
        SavePrefab(CreateMovesCounterPrefab(), $"{PrefabRoot}/MovesCounter.prefab");
        SavePrefab(CreateLivesCounterPrefab(), $"{PrefabRoot}/LivesCounter.prefab");
        SavePrefab(CreateMenuButtonPrefab(), $"{PrefabRoot}/MenuButton.prefab");
        SavePrefab(CreateCounterPrefab("DeckCounter", "DeckCounterBadgeBg", string.Empty, "54"), $"{PrefabRoot}/DeckCounter.prefab");
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateGeneratedSlotSprites()
    {
        WriteSpritePng(GameplayButtonSpritePath, CreateGameplayActionButtonTexture());
        WriteSpritePng(CategorySlotSpritePath, CreateCategorySlotTexture());
        WriteSpritePng(CategoryCrownSpritePath, CreateCategoryCrownTexture());
        WriteSpritePng(LivesHeartSpritePath, CreateLivesHeartTexture());
        ImportSprite(MenuButtonSpritePath);
        ImportSprite(PauseWindowSpritePath);
    }

    private static Texture2D CreateGameplayActionButtonTexture()
    {
        const int width = 560;
        const int height = 150;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        texture.name = "button_gameplay_action_gold";

        Color clear = Color.clear;
        Color shadow = new(0.18f, 0.07f, 0f, 0.5f);
        Color goldDark = new(0.44f, 0.24f, 0.04f, 1f);
        Color goldMid = new(0.74f, 0.51f, 0.13f, 1f);
        Color goldLight = new(0.95f, 0.77f, 0.25f, 1f);
        Color greenTop = new(0.08f, 0.36f, 0.19f, 1f);
        Color greenBottom = new(0.01f, 0.2f, 0.13f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float outer = RoundedRectSignedDistance(x + 0.5f, y + 0.5f, width, height, 54f, 10f);
                if (outer > 0f)
                {
                    float softShadow = Mathf.Clamp01(1f - outer / 8f);
                    texture.SetPixel(x, y, softShadow > 0f ? new Color(shadow.r, shadow.g, shadow.b, shadow.a * softShadow) : clear);
                    continue;
                }

                float inner = RoundedRectSignedDistance(x + 0.5f, y + 0.5f, width, height, 46f, 18f);
                float vertical = Mathf.Clamp01((float)y / (height - 1));
                float center = Mathf.Clamp01(1f - Mathf.Abs(x - width * 0.5f) / (width * 0.5f));
                Color pixel;

                if (inner <= 0f)
                {
                    float enamelT = Mathf.Clamp01((y - 34f) / 82f);
                    pixel = Color.Lerp(greenTop, greenBottom, enamelT);

                    float topGloss = Mathf.Clamp01(1f - Mathf.Abs(y - 40f) / 11f) * Mathf.Clamp01(1f - Mathf.Abs(x - width * 0.5f) / 250f);
                    pixel = Color.Lerp(pixel, new Color(0.35f, 0.66f, 0.39f, 1f), topGloss * 0.08f);
                }
                else
                {
                    float bevel = Mathf.Clamp01(Mathf.Abs(outer) / 22f);
                    pixel = Color.Lerp(goldLight, Color.Lerp(goldMid, goldDark, vertical), bevel);
                    if (inner < 5f)
                    {
                        pixel = Color.Lerp(pixel, greenTop, Mathf.Clamp01(1f - inner / 5f) * 0.12f);
                    }
                }

                float topLine = Mathf.Clamp01(1f - Mathf.Abs(y - 21f) / 1.7f);
                if ((x > 55 && x < 255) || (x > 355 && x < 515))
                {
                    pixel = Color.Lerp(pixel, new Color(1f, 0.95f, 0.55f, 1f), topLine * 0.42f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateCategorySlotTexture()
    {
        const int width = 196;
        const int height = 286;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        texture.name = "category_slot_cardmesh";

        Color clear = Color.clear;
        Color outerGold = new(0.88f, 0.68f, 0.22f, 0.78f);
        Color innerGold = new(0.48f, 0.33f, 0.09f, 0.58f);
        Color softGreen = new(0.05f, 0.42f, 0.18f, 0.13f);
        Color deepGreen = new(0.02f, 0.18f, 0.08f, 0.21f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float edgeDistance = RoundedRectSignedDistance(x + 0.5f, y + 0.5f, width, height, 13f, 4f);
                if (edgeDistance > 0f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float nx = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
                float ny = Mathf.Abs((y + 0.5f - height * 0.5f) / (height * 0.5f));
                float center = Mathf.Clamp01(1f - Mathf.Max(nx, ny));
                Color fill = Color.Lerp(deepGreen, softGreen, center);
                fill.a *= Mathf.Clamp01((-edgeDistance) / 10f);

                float border = Mathf.Abs(edgeDistance);
                Color pixel = fill;
                if (border < 2.2f)
                {
                    pixel = Color.Lerp(innerGold, outerGold, Mathf.Clamp01(1f - border / 2.2f));
                }
                else if (border > 7.8f && border < 9.8f)
                {
                    pixel = new Color(0.9f, 0.75f, 0.32f, 0.32f);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateCategoryCrownTexture()
    {
        const int width = 128;
        const int height = 96;
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
        texture.name = "category_slot_crown_cardmesh";

        Color clear = Color.clear;
        Color shadow = new(0.28f, 0.15f, 0.03f, 0.68f);
        Color goldDark = new(0.66f, 0.39f, 0.08f, 0.96f);
        Color goldMid = new(0.9f, 0.68f, 0.2f, 0.96f);
        Color goldLight = new(1f, 0.86f, 0.36f, 0.96f);
        Color emerald = new(0.08f, 0.7f, 0.34f, 0.92f);

        Vector2[] crown = {
            new(18f, 66f), new(18f, 38f), new(41f, 54f), new(64f, 18f),
            new(87f, 54f), new(110f, 38f), new(110f, 66f)
        };
        Vector2[] baseBand = {
            new(20f, 64f), new(108f, 64f), new(105f, 79f), new(23f, 79f)
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new(x + 0.5f, height - y - 0.5f);
                bool inCrown = PointInPolygon(p, crown) || PointInPolygon(p, baseBand);
                bool inShadow = PointInPolygon(p - new Vector2(0f, 3f), crown) || PointInPolygon(p - new Vector2(0f, 3f), baseBand);

                Color pixel = clear;
                if (inShadow)
                {
                    pixel = shadow;
                }

                if (inCrown)
                {
                    float t = Mathf.Clamp01((height - p.y) / height);
                    float center = Mathf.Clamp01(1f - Mathf.Abs(p.x - width * 0.5f) / 56f);
                    pixel = Color.Lerp(goldDark, Color.Lerp(goldMid, goldLight, center), t * 0.7f + 0.18f);
                }

                if (DistanceToSegment(p, new Vector2(20f, 65f), new Vector2(108f, 65f)) < 1.6f
                    || DistanceToSegment(p, new Vector2(24f, 77f), new Vector2(104f, 77f)) < 1.4f)
                {
                    pixel = new Color(0.37f, 0.2f, 0.04f, 0.76f);
                }

                float gemDistance = Mathf.Abs(p.x - 64f) + Mathf.Abs(p.y - 58f);
                if (gemDistance < 9.5f)
                {
                    pixel = Color.Lerp(emerald, new Color(0.76f, 1f, 0.58f, 0.96f), Mathf.Clamp01(1f - gemDistance / 9.5f));
                }

                if (IsCircle(p, new Vector2(18f, 38f), 5.5f) || IsCircle(p, new Vector2(64f, 18f), 5.5f) || IsCircle(p, new Vector2(110f, 38f), 5.5f))
                {
                    pixel = goldLight;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Texture2D CreateLivesHeartTexture()
    {
        const int size = 96;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "icon_lives_heart";
        Color clear = Color.clear;
        Color redDark = new(0.78f, 0.03f, 0.09f, 1f);
        Color redMid = new(1f, 0.16f, 0.22f, 1f);
        Color redLight = new(1f, 0.48f, 0.42f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - 48f) / 39f;
                float ny = (y - 45f) / 37f;
                float heart = Mathf.Pow(nx * nx + ny * ny - 0.32f, 3f) - nx * nx * ny * ny * ny;
                if (heart > 0f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(34f, 62f)) / 42f);
                Color pixel = Color.Lerp(redDark, redMid, Mathf.Clamp01((y + 10f) / size));
                pixel = Color.Lerp(pixel, redLight, highlight * 0.32f);
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static void WriteSpritePng(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        ImportSprite(path);
    }

    private static void ImportSprite(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static float RoundedRectSignedDistance(float x, float y, float width, float height, float radius, float inset)
    {
        float halfWidth = width * 0.5f - inset - radius;
        float halfHeight = height * 0.5f - inset - radius;
        Vector2 point = new(Mathf.Abs(x - width * 0.5f) - halfWidth, Mathf.Abs(y - height * 0.5f) - halfHeight);
        Vector2 outside = new(Mathf.Max(point.x, 0f), Mathf.Max(point.y, 0f));
        return outside.magnitude + Mathf.Min(Mathf.Max(point.x, point.y), 0f) - radius;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            bool intersects = (a.y > point.y) != (b.y > point.y)
                && point.x < ((b.x - a.x) * (point.y - a.y) / (b.y - a.y)) + a.x;
            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(point, a + ab * t);
    }

    private static bool IsCircle(Vector2 point, Vector2 center, float radius)
    {
        return Vector2.SqrMagnitude(point - center) <= radius * radius;
    }

    private static void ApplyMainScene()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        EnsureEventSystem();
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeArea = EnsureRect(canvas.transform, "SafeAreaRoot");
        StretchToFill(safeArea);

        RectTransform background = EnsureRect(safeArea, "Background");
        StretchToFill(background);
        Image bg = EnsureImage(background);
        bg.sprite = LoadSprite("Art/Backgrounds/BG1");
        bg.type = Image.Type.Simple;
        bg.color = Color.white;
        bg.raycastTarget = false;
        background.SetAsFirstSibling();

        RectTransform topHud = EnsureRect(safeArea, "TopHUD");
        SetAnchored(topHud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 128f), new Vector2(0f, -18f));

        RectTransform coinsPanel = ReplaceWithPrefabRect(topHud, "CoinsPanel", $"{PrefabRoot}/CurrencyCounter.prefab");
        ConfigureCurrencyCounter(coinsPanel);
        RemoveDirectChild(coinsPanel, "PlusButton");

        RectTransform movesPanel = ReplaceWithPrefabRect(safeArea, "MovesPanel", $"{PrefabRoot}/MovesCounter.prefab");
        RemoveDirectChild(movesPanel, "Icon");
        RemoveDirectChild(movesPanel, "Value");
        ConfigureMovesCounter(movesPanel);

        RectTransform livesHud = ReplaceWithPrefabRect(safeArea, "LivesHud", $"{PrefabRoot}/LivesCounter.prefab");
        RemoveDirectChild(livesHud, "Icon");
        RemoveDirectChild(livesHud, "Value");
        ConfigureLivesCounter(livesHud);

        RectTransform menuRoot = ReplaceWithPrefabRect(topHud, "RewardBonusButton", $"{PrefabRoot}/MenuButton.prefab", true);
        Button menuButton = GetOrAdd<Button>(menuRoot.gameObject);
        ConfigureMenuButton(menuButton.GetComponent<RectTransform>());
        RemoveDirectChild(topHud, "MenuButton");

        RectTransform boardArea = EnsureRect(safeArea, "BoardArea");
        StretchToFill(boardArea, 320f, 560f, 225f, 260f);
        EnsureImage(boardArea).color = new Color(1f, 1f, 1f, 0f);

        RectTransform columnsRoot = EnsureRect(boardArea, "ColumnSlotsRoot");
        StretchToFill(columnsRoot);
        HorizontalLayoutGroup layoutGroup = GetOrAdd<HorizontalLayoutGroup>(columnsRoot.gameObject);
        layoutGroup.spacing = GameManager.GetGameplayLayoutProfile(5).columnSpacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        for (int i = 0; i < 5; i++)
        {
            RectTransform column = ReplaceWithPrefabRect(columnsRoot, $"Column_{i + 1:00}", $"{PrefabRoot}/CategorySlot.prefab");
            ConfigureCategorySlot(column, i < 4);
        }

        RectTransform deckArea = EnsureRect(safeArea, "DeckArea");
        SetAnchored(deckArea, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(520f, 310f), new Vector2(-269f, -470f));
        EnsureImage(deckArea).color = new Color(1f, 1f, 1f, 0f);
        RectTransform deckCardBack = EnsureRect(deckArea, "DeckCardBack");
        SetAnchored(deckCardBack, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), new Vector2(196f, 286f), new Vector2(-239f, -2f));
        Image deckImage = EnsureImage(deckCardBack);
        deckImage.sprite = LoadSprite("Art/Cards/card_back_blue_emblem");
        deckImage.type = Image.Type.Sliced;
        deckImage.color = Color.white;
        RectTransform drawCardsRoot = EnsureRect(deckArea, "DrawCardsRoot");
        SetAnchored(drawCardsRoot, deckCardBack.anchorMin, deckCardBack.anchorMax, deckCardBack.pivot, deckCardBack.sizeDelta, deckCardBack.anchoredPosition);
        TextMeshProUGUI deckCounter = EnsureText(deckArea, "DeckCounter", "54");
        SetAnchored(deckCounter.rectTransform, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), new Vector2(52f, 40f), new Vector2(-165f, 120f));
        ConfigurePlainText(deckCounter, 32f, 22f, TextAlignmentOptions.Center, new Color(0.12f, 0.18f, 0.25f, 1f));
        RectTransform deckBadge = EnsureRect(deckArea, "DeckCounterBadgeBg");
        SetAnchored(deckBadge, deckCounter.rectTransform.anchorMin, deckCounter.rectTransform.anchorMax, deckCounter.rectTransform.pivot, deckCounter.rectTransform.sizeDelta, deckCounter.rectTransform.anchoredPosition);
        Image deckBadgeImage = EnsureImage(deckBadge);
        deckBadgeImage.sprite = GetRoundedSprite();
        deckBadgeImage.type = Image.Type.Sliced;
        deckBadgeImage.color = new Color(1f, 1f, 0.96f, 0.96f);
        deckBadge.SetSiblingIndex(deckCounter.transform.GetSiblingIndex());

        RectTransform bottomHud = EnsureRect(safeArea, "BottomHUD");
        SetAnchored(bottomHud, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 178f), new Vector2(0f, 40f));
        EnsureImage(bottomHud).color = new Color(1f, 1f, 1f, 0f);
        ConfigureSceneBottomButton(ReplaceWithPrefabRect(bottomHud, "HintButton", $"{PrefabRoot}/HintButton.prefab"), "HintButton", "Подсказка", PremiumUISprites.HintBulbIcon, new Vector2(-440f, 6f), true, true);
        ConfigureSceneBottomButton(ReplaceWithPrefabRect(bottomHud, "UndoButton", $"{PrefabRoot}/UndoButton.prefab"), "UndoButton", "Отмена", PremiumUISprites.UndoIcon, new Vector2(0f, 6f), true, true);
        ConfigureSceneBottomButton(ReplaceWithPrefabRect(bottomHud, "BoosterDeckButton", $"{PrefabRoot}/BoosterButton.prefab"), "BoosterDeckButton", "+10 ходов", PremiumUISprites.BoosterRocketIcon, new Vector2(440f, 6f), true, true);

        RectTransform adBanner = FindChild<RectTransform>(safeArea, "AdBannerPlaceholder");
        if (adBanner != null)
        {
            adBanner.gameObject.SetActive(false);
        }

        GameObject pausePopup = EnsurePauseSettingsPopup(safeArea);
        EnsureRestartConfirmationPopup(safeArea);
        ScenePauseMenuButton pauseMenuButton = GetOrAdd<ScenePauseMenuButton>(menuRoot.gameObject);
        pauseMenuButton.SetPopup(pausePopup);

        SolitaireSceneRefs refs = UnityEngine.Object.FindFirstObjectByType<SolitaireSceneRefs>();
        if (refs == null)
        {
            GameObject gameRoot = GameObject.Find("GameRoot") ?? new GameObject("GameRoot");
            refs = GetOrAdd<SolitaireSceneRefs>(gameRoot);
        }

        GameObject runtimeRoot = refs.gameObject;
        GetOrAdd<StubPlatformService>(runtimeRoot);
        GetOrAdd<GameManager>(runtimeRoot);
        GetOrAdd<CardDatabase>(runtimeRoot);
        GetOrAdd<UIManager>(runtimeRoot);

        refs.NormalizeSceneLayout();
        CleanupGameplaySceneOverrides(safeArea);
        EditorUtility.SetDirty(refs);
    }

    private static GameObject EnsurePauseSettingsPopup(RectTransform safeArea)
    {
        RectTransform popup = EnsureRect(safeArea, "PauseSettingsPopup");
        popup.gameObject.SetActive(true);
        StretchToFill(popup);
        popup.SetAsLastSibling();

        Image overlay = EnsureImage(popup);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(0.01f, 0.045f, 0.028f, 0.66f);
        overlay.raycastTarget = true;

        CanvasGroup group = GetOrAdd<CanvasGroup>(popup.gameObject);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = false;

        RectTransform panel = EnsureRect(popup, "Panel");
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920f, 760f), Vector2.zero);
        Image panelImage = EnsureImage(panel);
        panelImage.sprite = LoadSprite(PauseWindowSpritePath) != null ? LoadSprite(PauseWindowSpritePath) : PremiumUISprites.PauseWindowCardMesh;
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;
        AddOrUpdateShadow(panel.gameObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

        TextMeshProUGUI title = EnsureText(panel, "Title", "Пауза");
        SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-132f, 78f), new Vector2(0f, -118f));
        ConfigurePlainText(title, 54f, 34f, TextAlignmentOptions.Center, new Color(0.98f, 0.86f, 0.52f, 1f));
        AddOrUpdateOutline(title.gameObject, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

        Button continueButton = EnsurePauseMenuButton(panel, "ContinueButton", "Продолжить", new Vector2(0f, -58f), true);
        Button restartButton = EnsurePauseMenuButton(panel, "RestartButton", "Начать заново", new Vector2(0f, -170f), false);
        Button mainMenuButton = EnsurePauseMenuButton(panel, "MainMenuButton", "Главное меню", new Vector2(0f, -282f), false);
        Button closeButton = EnsurePauseCloseButton(panel);

        PauseMenuPopupController controller = GetOrAdd<PauseMenuPopupController>(popup.gameObject);
        controller.Configure(popup.gameObject, continueButton, restartButton, mainMenuButton, closeButton);

        popup.gameObject.SetActive(false);
        EditorUtility.SetDirty(popup.gameObject);
        return popup.gameObject;
    }

    private static GameObject EnsureRestartConfirmationPopup(RectTransform safeArea)
    {
        RectTransform popup = EnsureRect(safeArea, "RestartConfirmationPopup");
        popup.gameObject.SetActive(true);
        StretchToFill(popup);
        popup.SetAsLastSibling();

        Image overlay = EnsureImage(popup);
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.color = new Color(0.01f, 0.045f, 0.028f, 0.70f);
        overlay.raycastTarget = true;

        CanvasGroup group = GetOrAdd<CanvasGroup>(popup.gameObject);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.ignoreParentGroups = false;

        RectTransform panel = EnsureRect(popup, "Panel");
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920f, 700f), Vector2.zero);
        Image panelImage = EnsureImage(panel);
        panelImage.sprite = LoadSprite(PauseWindowSpritePath) != null ? LoadSprite(PauseWindowSpritePath) : PremiumUISprites.PauseWindowCardMesh;
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;
        AddOrUpdateShadow(panel.gameObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

        TextMeshProUGUI title = EnsureText(panel, "Title", "Начать заново?");
        SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-132f, 76f), new Vector2(0f, -126f));
        ConfigurePlainText(title, 44f, 30f, TextAlignmentOptions.Center, new Color(0.98f, 0.86f, 0.52f, 1f));
        AddOrUpdateOutline(title.gameObject, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

        TextMeshProUGUI body = EnsureText(panel, "Body", "Будет потрачена 1 жизнь");
        SetAnchored(body.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, 72f), new Vector2(0f, 18f));
        ConfigurePlainText(body, 31f, 22f, TextAlignmentOptions.Center, new Color(0.98f, 0.86f, 0.52f, 1f));
        AddOrUpdateOutline(body.gameObject, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

        Button confirmButton = EnsurePauseMenuButton(panel, "ConfirmButton", "Продолжить", new Vector2(0f, -110f), true);
        Button cancelButton = EnsurePauseMenuButton(panel, "CancelButton", "Отмена", new Vector2(0f, -222f), false);
        Button closeButton = EnsurePauseCloseButton(panel);

        RestartConfirmationPopupController controller = GetOrAdd<RestartConfirmationPopupController>(popup.gameObject);
        controller.Configure(popup.gameObject, confirmButton, cancelButton, closeButton);

        popup.gameObject.SetActive(false);
        EditorUtility.SetDirty(popup.gameObject);
        return popup.gameObject;
    }

    private static Button EnsurePauseMenuButton(RectTransform panel, string name, string label, Vector2 position, bool primary)
    {
        RectTransform root = EnsureRect(panel, name);
        SetAnchored(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 96f), position);

        Image image = EnsureImage(root);
        image.sprite = PremiumUISprites.GameplayActionButton != null ? PremiumUISprites.GameplayActionButton : PremiumUISprites.PrimaryButton;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = primary ? Color.white : new Color(0.92f, 0.92f, 0.88f, 1f);
        image.raycastTarget = true;

        Button button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = image;
        button.interactable = true;

        if (root.GetComponent<UIJuicyButton>() == null)
        {
            root.gameObject.AddComponent<UIJuicyButton>();
        }

        TextMeshProUGUI text = EnsureText(root, "Label", label);
        StretchToFill(text.rectTransform, 28f, 28f, 0f, 0f);
        ConfigureButtonText(text, 36f, 24f, 0.09f);
        text.raycastTarget = false;

        return button;
    }

    private static Button EnsurePauseCloseButton(RectTransform panel)
    {
        RectTransform root = EnsureRect(panel, "CloseButton");
        SetAnchored(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), new Vector2(-78f, -72f));

        Image image = EnsureImage(root);
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = true;

        Button button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = image;
        button.interactable = true;

        TextMeshProUGUI label = EnsureText(root, "Label", "X");
        StretchToFill(label.rectTransform, 0f, 0f, 0f, 0f);
        ConfigurePlainText(label, 34f, 22f, TextAlignmentOptions.Center, new Color(0.98f, 0.86f, 0.52f, 1f));
        AddOrUpdateOutline(label.gameObject, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
        label.raycastTarget = false;

        if (root.GetComponent<UIJuicyButton>() == null)
        {
            root.gameObject.AddComponent<UIJuicyButton>();
        }

        return button;
    }

    private static void CleanupGameplaySceneOverrides(RectTransform safeArea)
    {
        if (safeArea == null)
        {
            return;
        }

        foreach (Transform child in safeArea.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "HeartIcon")
            {
                foreach (TMP_Text text in child.GetComponents<TMP_Text>())
                {
                    UnityEngine.Object.DestroyImmediate(text);
                }

                Image image = child.GetComponent<Image>();
                if (image == null)
                {
                    image = child.gameObject.AddComponent<Image>();
                }

                image.sprite = GetHeartSprite();
                image.color = Color.white;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                EditorUtility.SetDirty(image);
            }

            if (child.name == "MovesPanel")
            {
                CleanupMovesPanelChildren(child);
            }

            if (child.name == "MovesLabel")
            {
                RectTransform rect = child as RectTransform;
                if (rect != null)
                {
                    SetAnchored(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-26f, 28f), new Vector2(0f, -17f));
                }

                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    ConfigurePlainText(text, 25f, 17f, TextAlignmentOptions.Center, new Color(0.37f, 0.22f, 0.06f, 1f));
                    text.fontStyle = FontStyles.Normal;
                    EditorUtility.SetDirty(text);
                }
            }

            if (child.name == "MovesValue")
            {
                RectTransform rect = child as RectTransform;
                if (rect != null)
                {
                    SetAnchored(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 54f), new Vector2(0f, 10f));
                }

                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    ConfigurePlainText(text, 38f, 38f, TextAlignmentOptions.Center, new Color(0.31f, 0.17f, 0.04f, 1f));
                    text.enableAutoSizing = false;
                    text.fontSize = 38f;
                    text.fontStyle = FontStyles.Normal;
                    RemoveUiEffects(text.gameObject);
                    EditorUtility.SetDirty(text);
                }
            }

            if (child.name == "HintButtonLabel")
            {
                RectTransform rect = child as RectTransform;
                if (rect != null)
                {
                    SetAnchored(rect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-84f, 50f), new Vector2(20f, 3f));
                }
            }
        }
    }

    private static GameObject CreateGameHudPrefab()
    {
        GameObject root = CreateRoot("GameHud", new Vector2(1920f, 1080f));
        EnsureRect(root.transform, "TopHUD");
        EnsureRect(root.transform, "BoardArea");
        EnsureRect(root.transform, "BottomHUD");
        return root;
    }

    private static GameObject CreateBottomGameButtonPrefab(string name, string label, Sprite icon, bool showBadge)
    {
        GameObject root = CreateRoot(name, BottomButtonSize);
        Button button = root.AddComponent<Button>();
        Image image = EnsureImage(root.transform as RectTransform);
        image.sprite = PremiumUISprites.GameplayActionButton;
        image.type = Image.Type.Sliced;
        button.targetGraphic = image;
        ConfigureSceneBottomButton(root.transform as RectTransform, name, label, icon, Vector2.zero, showBadge, true);
        return root;
    }

    private static GameObject CreateCategorySlotPrefab()
    {
        GameObject root = CreateRoot("CategorySlot", new Vector2(196f, 760f));
        ConfigureCategorySlot(root.transform as RectTransform, true);
        return root;
    }

    private static GameObject CreateCounterPrefab(string prefabName, string objectName, string icon, string value)
    {
        GameObject root = CreateRoot(prefabName, new Vector2(236f, 70f));
        ConfigureCounter(root.transform as RectTransform, objectName, icon, value);
        return root;
    }

    private static GameObject CreateMovesCounterPrefab()
    {
        GameObject root = CreateRoot("MovesCounter", new Vector2(164f, 108f));
        ConfigureMovesCounter(root.transform as RectTransform);
        return root;
    }

    private static GameObject CreateLivesCounterPrefab()
    {
        GameObject root = CreateRoot("LivesCounter", new Vector2(172f, 64f));
        ConfigureLivesCounter(root.transform as RectTransform);
        return root;
    }

    private static GameObject CreateMenuButtonPrefab()
    {
        GameObject root = CreateRoot("MenuButton", new Vector2(88f, 88f));
        ConfigureMenuButton(root.transform as RectTransform);
        return root;
    }

    private static GameObject CreateRoot(string name, Vector2 size)
    {
        GameObject root = new(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return root;
    }

    private static void ConfigureSceneBottomButton(RectTransform parent, string name, string label, Sprite icon, Vector2 position, bool showBadge, bool rootIsButton = false)
    {
        RectTransform root = rootIsButton ? parent : EnsureRect(parent, name);
        if (!rootIsButton)
        {
            Button button = GetOrAdd<Button>(root.gameObject);
            Image rootImage = EnsureImage(root);
            rootImage.sprite = PremiumUISprites.GameplayActionButton;
            rootImage.type = Image.Type.Sliced;
            rootImage.color = Color.white;
            button.targetGraphic = rootImage;
        }

        SetAnchored(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), BottomButtonSize, position);
        AddOrUpdateShadow(root.gameObject, new Color(0.08f, 0.03f, 0f, 0.32f), new Vector2(0f, -8f));

        RectTransform hoverGlow = EnsureImageLayer(root, "HoverGlow", PremiumUISprites.GameplayActionButton, Image.Type.Sliced, new Color(1f, 0.74f, 0.2f, 0.16f));
        StretchToFill(hoverGlow, -8f, -8f, -7f, -7f);
        hoverGlow.SetAsFirstSibling();
        CanvasGroup hoverGroup = GetOrAdd<CanvasGroup>(hoverGlow.gameObject);
        hoverGroup.alpha = 0f;

        RectTransform gloss = EnsureImageLayer(root, "TopGloss", GetRoundedSprite(), Image.Type.Sliced, new Color(0.82f, 1f, 0.74f, 0.13f));
        SetAnchored(gloss, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-104f, 23f), new Vector2(10f, -30f));

        RectTransform iconHalo = EnsureImageLayer(root, $"{name}IconHalo", PremiumUISprites.IconCircle, Image.Type.Sliced, Color.white);
        SetAnchored(iconHalo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(95f, 95f), new Vector2(66f, -1f));
        EnsureImage(iconHalo).color = new Color(0.78f, 0.62f, 0.28f, 0.92f);
        AddOrUpdateShadow(iconHalo.gameObject, new Color(0.32f, 0.13f, 0f, 0.26f), new Vector2(0f, -4f));

        RectTransform iconRect = EnsureImageLayer(root, $"{name}Icon", icon, Image.Type.Simple, new Color(0.9f, 0.9f, 0.78f, 1f));
        SetAnchored(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(74f, 74f), new Vector2(66f, 2f));

        TextMeshProUGUI labelText = EnsureText(root, $"{name}Label", label);
        ConfigureButtonText(labelText);
        Vector2 labelOffset = name switch
        {
            "HintButton" => new Vector2(20f, 1f),
            "UndoButton" => new Vector2(-10f, 2f),
            "BoosterDeckButton" => new Vector2(-10f, 1f),
            _ => new Vector2(-10f, 0f)
        };
        SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-84f, 50f), labelOffset);

        RectTransform badge = EnsureRect(root, $"{name}Badge");
        SetAnchored(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 24f), new Vector2(-82f, -16f));
        Image badgeImage = EnsureImage(badge);
        badgeImage.sprite = PremiumUISprites.HudCounterGold;
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = name == "UndoButton" ? new Color(0.72f, 0.45f, 0.13f, 0.72f) : new Color(0.42f, 0.14f, 0.07f, 0.76f);
        TextMeshProUGUI badgeText = EnsureText(badge, "BadgeText", "x1");
        ConfigureButtonText(badgeText, 20f, 14f, 0.08f);
        StretchToFill(badgeText.rectTransform, 7f, 7f, 0f, 2f);
        badge.gameObject.SetActive(showBadge);
    }

    private static void ConfigureCategorySlot(RectTransform root, bool active)
    {
        root.gameObject.SetActive(active);
        SetAnchored(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(196f, 760f), Vector2.zero);
        LayoutElement element = GetOrAdd<LayoutElement>(root.gameObject);
        element.minWidth = element.preferredWidth = 196f;
        element.minHeight = element.preferredHeight = 760f;
        EnsureImage(root).color = new Color(1f, 1f, 1f, 0f);

        RectTransform labelBg = EnsureRect(root, "CrownSlotLabelBg");
        SetAnchored(labelBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(150f, 32f), new Vector2(0f, -18f));
        Image labelImage = EnsureImage(labelBg);
        labelImage.sprite = PremiumUISprites.CategoryLabel;
        labelImage.type = Image.Type.Sliced;
        labelImage.color = new Color(1f, 0.86f, 0.38f, 0.88f);
        TextMeshProUGUI label = EnsureText(labelBg, "CrownSlotLabelText", string.Empty);
        ConfigurePlainText(label, 23f, 16f, TextAlignmentOptions.Center, new Color(0.26f, 0.14f, 0.02f, 1f));
        StretchToFill(label.rectTransform);

        RectTransform crown = EnsureRect(root, "CrownSlot");
        SetAnchored(crown, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(196f, 286f), new Vector2(0f, -78f));
        Image crownImage = EnsureImage(crown);
        crownImage.sprite = LoadSprite(CategorySlotSpritePath) != null ? LoadSprite(CategorySlotSpritePath) : PremiumUISprites.CategorySlot;
        crownImage.type = Image.Type.Simple;
        crownImage.color = Color.white;
        AddOrUpdateShadow(crown.gameObject, new Color(0.02f, 0.07f, 0.02f, 0.16f), new Vector2(0f, -2f));

        RemoveDirectChild(crown, "CrownMark");
        RectTransform crownMark = EnsureImageLayer(crown, "CrownMarkIcon", LoadSprite(CategoryCrownSpritePath), Image.Type.Simple, new Color(1f, 1f, 1f, 0.9f));
        SetAnchored(crownMark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, 70f), new Vector2(0f, -2f));

        RectTransform goalCardsRoot = EnsureRect(crown, "GoalCardsRoot");
        SetAnchored(goalCardsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(196f, 286f), Vector2.zero);

        RectTransform stack = EnsureRect(root, "CardsStackRoot");
        SetAnchored(stack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(196f, 390f), new Vector2(0f, -235f));
    }

    private static void ConfigureCurrencyCounter(RectTransform panel)
    {
        SetAnchored(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(236f, 70f), new Vector2(196f, -100f));
        ConfigureCounter(panel, "CoinsPanel", "C", "185");
    }

    private static void ConfigureMovesCounter(RectTransform panel)
    {
        SetAnchored(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(164f, 108f), new Vector2(154f, -242f));
        CleanupMovesPanelChildren(panel);
        RemoveDirectChild(panel, "Icon");
        RemoveDirectChild(panel, "Value");
        RemoveMovesPanelTexts(panel);
        Image image = EnsureImage(panel);
        image.sprite = PremiumUISprites.HudPanelCreamGold;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.92f);
        TextMeshProUGUI title = EnsureText(panel, "MovesLabel", "Ходы");
        ConfigurePlainText(title, 25f, 17f, TextAlignmentOptions.Center, new Color(0.37f, 0.22f, 0.06f, 1f));
        title.fontStyle = FontStyles.Normal;
        SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-26f, 28f), new Vector2(0f, -17f));
        TextMeshProUGUI value = EnsureText(panel, "MovesValue", "140");
        ConfigurePlainText(value, 30f, 30f, TextAlignmentOptions.Center, new Color(0.31f, 0.17f, 0.04f, 1f));
        value.enableAutoSizing = false;
        value.fontSize = 30f;
        value.fontStyle = FontStyles.Normal;
        RemoveUiEffects(value.gameObject);
        SetAnchored(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-28f, 48f), new Vector2(0f, 12f));
    }

    private static void ConfigureLivesCounter(RectTransform root)
    {
        SetAnchored(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(172f, 64f), new Vector2(154f, -379f));
        RemoveDirectChild(root, "Icon");
        RemoveDirectChild(root, "Value");
        EnsureImage(root).color = new Color(1f, 1f, 1f, 0f);
        RectTransform heart = EnsureImageLayer(root, "HeartIcon", GetHeartSprite(), Image.Type.Simple, Color.white);
        TMP_Text staleHeartText = heart.GetComponent<TMP_Text>();
        if (staleHeartText != null)
        {
            UnityEngine.Object.DestroyImmediate(staleHeartText);
        }
        SetAnchored(heart, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), new Vector2(42f, 0f));
        TextMeshProUGUI count = EnsureText(root, "LivesCount", "5/5");
        ConfigurePlainText(count, 38f, 26f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.66f, 1f));
        SetAnchored(count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-64f, -2f), new Vector2(42f, 0f));
        LivesHudView view = GetOrAdd<LivesHudView>(root.gameObject);
        EditorUtility.SetDirty(view);
    }

    private static void ConfigureMenuButton(RectTransform root)
    {
        SetAnchored(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(88f, 88f), new Vector2(-126f, -106f));
        Button button = GetOrAdd<Button>(root.gameObject);
        Image image = EnsureImage(root);
        image.sprite = PremiumUISprites.MenuButtonCardMesh != null ? PremiumUISprites.MenuButtonCardMesh : GetRoundSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        button.targetGraphic = image;
        RemoveDirectChild(root, "PauseIcon");
        RemoveDirectChild(root, "MenuBarTop");
        RemoveDirectChild(root, "MenuBarMiddle");
        RemoveDirectChild(root, "MenuBarBottom");
    }

    private static void ConfigureCounter(RectTransform root, string objectName, string icon, string value)
    {
        root.name = objectName;
        Image image = EnsureImage(root);
        image.sprite = PremiumUISprites.HudCounterGold;
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 1f, 1f, 0.96f);
        TextMeshProUGUI iconText = EnsureText(root, objectName == "CoinsPanel" ? "CoinGlyph" : "Icon", icon);
        ConfigurePlainText(iconText, 25f, 15f, TextAlignmentOptions.Center, new Color(0.43f, 0.22f, 0.04f, 1f));
        SetAnchored(iconText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50f, 50f), new Vector2(38f, 0f));
        TextMeshProUGUI text = EnsureText(root, objectName == "CoinsPanel" ? "CoinsText" : "Value", value);
        ConfigurePlainText(text, 36f, 23f, TextAlignmentOptions.MidlineLeft, new Color(0.29f, 0.16f, 0.04f, 1f));
        SetAnchored(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-92f, 44f), new Vector2(38f, 0f));
    }

    private static RectTransform EnsureImageLayer(RectTransform parent, string name, Sprite sprite, Image.Type type, Color color)
    {
        RectTransform rect = EnsureRect(parent, name);
        Image image = EnsureImage(rect);
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static void EnsureMenuBar(RectTransform parent, string name, Vector2 position)
    {
        RectTransform bar = EnsureRect(parent, name);
        SetAnchored(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 8f), position);
        Image image = EnsureImage(bar);
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = new Color(0.9f, 1f, 0.78f, 1f);
        image.raycastTarget = false;
    }

    private static Button EnsureButton(Transform parent, string name)
    {
        RectTransform rect = EnsureRect(parent, name);
        Button button = GetOrAdd<Button>(rect.gameObject);
        Image image = EnsureImage(rect);
        if (button.targetGraphic == null)
        {
            button.targetGraphic = image;
        }

        return button;
    }

    private static RectTransform ReplaceWithPrefabRect(Transform parent, string name, string prefabPath, bool forceReplace = false)
    {
        RectTransform existing = FindDirectChild<RectTransform>(parent, name);
        if (!forceReplace && existing != null && string.Equals(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing.gameObject), prefabPath, StringComparison.Ordinal))
        {
            return existing;
        }

        int siblingIndex = existing != null ? existing.GetSiblingIndex() : parent.childCount;
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return EnsureRect(parent, name);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return EnsureRect(parent, name);
        }

        instance.name = name;
        instance.transform.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));
        RectTransform rect = GetOrAdd<RectTransform>(instance);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void RemoveDirectChild(Transform parent, string name)
    {
        Transform child = FindDirectChild<Transform>(parent, name);
        if (child != null)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void CleanupMovesPanelChildren(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        for (int index = panel.childCount - 1; index >= 0; index--)
        {
            Transform child = panel.GetChild(index);
            string childName = child.name;
            if (childName == "MovesLabel" || childName == "MovesValue" || childName == "MovesTopGloss")
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            string textName = text.transform.name;
            if (textName == "MovesLabel" || textName == "MovesValue")
            {
                TrimExtraTextComponents(text.gameObject, text);
                continue;
            }

            if (text.transform == panel)
            {
                UnityEngine.Object.DestroyImmediate(text);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(text.gameObject);
        }
    }

    private static void RemoveMovesPanelTexts(Transform panel)
    {
        if (panel == null)
        {
            return;
        }

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.transform == panel)
            {
                UnityEngine.Object.DestroyImmediate(text);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(text.gameObject);
        }
    }

    private static void TrimExtraTextComponents(GameObject target, TMP_Text keep)
    {
        if (target == null)
        {
            return;
        }

        foreach (TMP_Text text in target.GetComponents<TMP_Text>())
        {
            if (text == keep)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(text);
        }
    }

    private static void RemoveUiEffects(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        foreach (Outline outline in target.GetComponents<Outline>())
        {
            UnityEngine.Object.DestroyImmediate(outline);
        }

        foreach (Shadow shadow in target.GetComponents<Shadow>())
        {
            if (shadow is Outline)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(shadow);
        }
    }

    private static T FindDirectChild<T>(Transform parent, string name) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, name, StringComparison.Ordinal))
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        RectTransform existing = FindChild<RectTransform>(parent, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static T FindChild<T>(Transform parent, string name) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private static Image EnsureImage(RectTransform rect)
    {
        return GetOrAdd<Image>(rect.gameObject);
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name, string value)
    {
        RectTransform rect = EnsureRect(parent, name);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        text.text = value;
        RuntimeCyrillicFontProvider.Apply(text, value);
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigurePlainText(TMP_Text text, float maxSize, float minSize, TextAlignmentOptions alignment, Color color)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = minSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
    }

    private static void ConfigureButtonText(TMP_Text text, float maxSize = 34f, float minSize = 18f, float outlineWidth = 0.105f)
    {
        ConfigurePlainText(text, maxSize, minSize, TextAlignmentOptions.Center, TextGold);
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            new Color(0.98f, 0.88f, 0.48f, 1f),
            new Color(0.94f, 0.74f, 0.28f, 1f),
            new Color(0.58f, 0.32f, 0.06f, 1f),
            new Color(0.72f, 0.44f, 0.09f, 1f));
        AddOrUpdateOutline(text.gameObject, TextDarkGold, new Vector2(1.05f, -1.05f));

        Material material = new(text.fontMaterial);
        if (material.HasProperty(ShaderUtilities.ID_FaceColor))
        {
            material.SetColor(ShaderUtilities.ID_FaceColor, new Color(0.96f, 0.77f, 0.32f, 1f));
        }
        if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.11f, 0.052f, 0.012f, 1f));
        }
        if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        }
        text.fontMaterial = material;
    }

    private static void AddOrUpdateShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = GetOrAdd<Shadow>(target);
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddOrUpdateOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = GetOrAdd<Outline>(target);
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static Sprite LoadSprite(string resourcesPath)
    {
        string assetPath = resourcesPath.StartsWith("Assets/", StringComparison.Ordinal)
            ? resourcesPath
            : $"Assets/{resourcesPath}.png";
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static Sprite GetRoundedSprite()
    {
        Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
        texture.name = "EditorRoundedSprite";
        int radius = 18;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nearestX = Mathf.Clamp(x, radius - 0.5f, 64 - radius + 0.5f);
                float nearestY = Mathf.Clamp(y, radius - 0.5f, 64 - radius + 0.5f);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 0.5f - distance)));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Sprite GetHeartSprite()
    {
        Sprite assetSprite = LoadSprite(LivesHeartSpritePath);
        if (assetSprite != null)
        {
            return assetSprite;
        }

        Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
        texture.name = "EditorHeartSprite";
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float nx = (x - 32f) / 30f;
                float ny = (y - 31f) / 30f;
                float heart = Mathf.Pow(nx * nx + ny * ny - 0.32f, 3f) - nx * nx * ny * ny * ny;
                texture.SetPixel(x, y, heart <= 0f ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GetRoundSprite()
    {
        Texture2D texture = new(96, 96, TextureFormat.RGBA32, false);
        texture.name = "EditorRoundSprite";
        Vector2 center = new(47.5f, 47.5f);
        for (int y = 0; y < 96; y++)
        {
            for (int x = 0; x < 96; x++)
            {
                float alpha = Mathf.Clamp01(48.5f - Vector2.Distance(new Vector2(x, y), center));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 96f, 96f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(48f, 48f, 48f, 48f));
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void StretchToFill(RectTransform rect) => StretchToFill(rect, 0f, 0f, 0f, 0f);

    private static void StretchToFill(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.localScale = Vector3.one;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }
}
#endif
