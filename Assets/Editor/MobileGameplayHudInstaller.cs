#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class MobileGameplayHudInstaller
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string LivesPrefabPath = "Assets/Prefabs/UI/Game/LivesCounter.prefab";
    private const string CoinsPrefabPath = "Assets/Prefabs/UI/Game/CurrencyCounter.prefab";
    private const string MovesPrefabPath = "Assets/Prefabs/UI/Game/MovesCounter.prefab";
    private const string PlateSpritePath = "Assets/Resources/UI/Premium/hud_resource_row_light.png";
    private const string SharedFontPath = "Assets/Resources/Fonts/Prata Cyrillic SDF.asset";

    private static readonly Color ValueColor = new(0.98f, 0.94f, 0.82f, 1f);
    private static readonly Color CaptionColor = new(0.78f, 0.74f, 0.62f, 0.86f);

    [MenuItem("Tools/Card Mesh/Mobile/Rebuild Gameplay Status Plates")]
    public static void RebuildGameplayStatusPlates()
    {
        ConfigurePlateSpriteImporter();
        Sprite plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlateSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedFontPath);

        RebuildLivesPrefab(font, plateSprite);
        RebuildCoinsPrefab(font, plateSprite);
        RebuildMovesPrefab(font, plateSprite);
        RebuildScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Gameplay status plates rebuilt and saved.");
    }

    private static void RebuildLivesPrefab(TMP_FontAsset font, Sprite plateSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(LivesPrefabPath);
        try
        {
            ConfigurePlateRoot(root, new Vector2(280f, 96f), plateSprite);
            SetRect(FindRect(root.transform, "HeartIcon"), new Vector2(62f, 62f), new Vector2(-86f, 0f));

            TMP_Text count = FindText(root.transform, "LivesCount");
            ConfigureText(count, font, 34f, 26f, ValueColor);
            if (count != null)
            {
                count.text = "5/5";
            }
            SetRect(count?.rectTransform, new Vector2(154f, 42f), new Vector2(24f, 12f));

            TMP_Text status = FindText(root.transform, "LivesStatus");
            ConfigureText(status, font, 14f, 12f, CaptionColor);
            if (status != null)
            {
                status.text = "ЖИЗНИ";
            }
            SetRect(status?.rectTransform, new Vector2(154f, 20f), new Vector2(24f, -22f));

            SetActive(root.transform, "LivesStatusFill", false);
            Transform addLife = FindTransform(root.transform, "AddLifeButton");
            if (addLife != null)
            {
                Button deadButton = addLife.GetComponent<Button>();
                if (deadButton != null)
                {
                    Object.DestroyImmediate(deadButton);
                }
                addLife.gameObject.SetActive(false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, LivesPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RebuildCoinsPrefab(TMP_FontAsset font, Sprite plateSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CoinsPrefabPath);
        try
        {
            ConfigurePlateRoot(root, new Vector2(190f, 96f), plateSprite);
            RectTransform icon = FindRect(root.transform, "CoinIcon");
            SetRect(icon, new Vector2(54f, 54f), new Vector2(-46f, 1f));
            SetImageNonInteractive(icon);

            TMP_Text value = FindText(root.transform, "CoinsText");
            ConfigureText(value, font, 30f, 20f, ValueColor);
            SetRect(value?.rectTransform, new Vector2(108f, 40f), new Vector2(27f, 12f));

            TMP_Text label = FindText(root.transform, "CoinsLabel");
            ConfigureText(label, font, 13f, 11f, CaptionColor);
            if (label != null)
            {
                label.text = "МОНЕТЫ";
            }
            SetRect(label?.rectTransform, new Vector2(108f, 18f), new Vector2(27f, -22f));

            SetActive(root.transform, "PlusButton", false);
            PrefabUtility.SaveAsPrefabAsset(root, CoinsPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RebuildMovesPrefab(TMP_FontAsset font, Sprite plateSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MovesPrefabPath);
        try
        {
            ConfigurePlateRoot(root, new Vector2(240f, 96f), plateSprite);
            RectTransform icon = FindRect(root.transform, "MovesIcon");
            SetRect(icon, new Vector2(56f, 56f), new Vector2(-73f, 0f));
            SetImageNonInteractive(icon);

            RectTransform glyph = FindRect(root.transform, "MovesIconGlyph");
            SetRect(glyph, new Vector2(52f, 52f), Vector2.zero);
            SetImageNonInteractive(glyph);

            TMP_Text value = FindText(root.transform, "MovesValue");
            ConfigureText(value, font, 38f, 30f, new Color(1f, 0.95f, 0.80f, 1f));
            SetRect(value?.rectTransform, new Vector2(150f, 46f), new Vector2(22f, 11f));

            TMP_Text label = FindText(root.transform, "MovesLabel");
            ConfigureText(label, font, 14f, 12f, new Color(0.84f, 0.75f, 0.40f, 0.92f));
            if (label != null)
            {
                label.text = "ХОДОВ";
            }
            SetRect(label?.rectTransform, new Vector2(150f, 18f), new Vector2(22f, -22f));

            PrefabUtility.SaveAsPrefabAsset(root, MovesPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RebuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        RectTransform safeArea = FindSceneRect(scene, "SafeAreaRoot");
        RectTransform topHud = FindSceneRect(scene, "TopHUD");
        RectTransform lives = FindSceneRect(scene, "LivesHud");
        RectTransform coins = FindSceneRect(scene, "CoinsPanel");
        RectTransform moves = FindSceneRect(scene, "MovesPanel");
        RectTransform menu = FindSceneRect(scene, "RewardBonusButton");
        if (safeArea == null || topHud == null || lives == null || coins == null || moves == null)
        {
            Debug.LogError("Gameplay status plates could not resolve the required Main scene objects.");
            return;
        }

        SetTopStretch(topHud, 224f);
        if (menu != null)
        {
            SetTopRight(menu, new Vector2(88f, 88f), new Vector2(-76f, -60f));
        }

        RectTransform legacyRail = FindRect(topHud, "GameplayStatusRail");
        RectTransform plates = FindRect(topHud, "GameplayStatusPlates");
        if (plates == null && legacyRail != null)
        {
            legacyRail.name = "GameplayStatusPlates";
            plates = legacyRail;
        }
        plates ??= EnsureImageChild(topHud, "GameplayStatusPlates", null);
        SetTopCenter(plates, new Vector2(960f, 96f), new Vector2(35f, -162f));
        Image platesImage = plates.GetComponent<Image>();
        platesImage.sprite = null;
        platesImage.color = new Color(1f, 1f, 1f, 0f);
        platesImage.raycastTarget = false;
        RemoveChildIfExists(plates, "DividerLivesCoins");
        RemoveChildIfExists(plates, "DividerCoinsMoves");

        lives.SetParent(plates, false);
        coins.SetParent(plates, false);
        moves.SetParent(plates, false);
        SetRect(lives, new Vector2(280f, 96f), new Vector2(-330f, 0f));
        SetRect(coins, new Vector2(190f, 96f), Vector2.zero);
        SetRect(moves, new Vector2(240f, 96f), new Vector2(280f, 0f));

        TMP_Text sceneLivesCount = FindText(lives, "LivesCount");
        TMP_Text sceneLivesStatus = FindText(lives, "LivesStatus");
        TMP_Text sceneCoinsLabel = FindText(coins, "CoinsLabel");
        TMP_Text sceneMovesLabel = FindText(moves, "MovesLabel");
        if (sceneLivesCount != null) sceneLivesCount.text = "5/5";
        if (sceneLivesStatus != null) sceneLivesStatus.text = "ЖИЗНИ";
        if (sceneCoinsLabel != null) sceneCoinsLabel.text = "МОНЕТЫ";
        if (sceneMovesLabel != null) sceneMovesLabel.text = "ХОДОВ";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigurePlateRoot(GameObject root, Vector2 size, Sprite plateSprite)
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = plateSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.96f);
            image.raycastTarget = false;
        }
    }

    private static void ConfigureText(TMP_Text text, TMP_FontAsset font, float maxSize, float minSize, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(true);
        text.font = font;
        text.fontSharedMaterial = font != null ? font.material : text.fontSharedMaterial;
        text.enableAutoSizing = true;
        text.fontSize = maxSize;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontStyle = FontStyles.Bold;
        text.fontWeight = FontWeight.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        foreach (Shadow shadow in text.GetComponents<Shadow>())
        {
            shadow.enabled = false;
        }
    }

    private static void ConfigurePlateSpriteImporter()
    {
        AssetDatabase.ImportAsset(PlateSpritePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(PlateSpritePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spriteBorder = new Vector4(30f, 30f, 30f, 30f);
        importer.SaveAndReimport();
    }

    private static RectTransform EnsureImageChild(Transform parent, string objectName, Sprite sprite)
    {
        Transform existing = FindTransform(parent, objectName);
        RectTransform rect = existing as RectTransform;
        if (rect == null)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }

        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rect;
    }

    private static void RemoveChildIfExists(Transform parent, string objectName)
    {
        Transform child = FindTransform(parent, objectName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetImageNonInteractive(RectTransform rect)
    {
        Image image = rect != null ? rect.GetComponent<Image>() : null;
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private static void SetActive(Transform root, string objectName, bool active)
    {
        Transform target = FindTransform(root, objectName);
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }

    private static RectTransform FindRect(Transform root, string objectName) => FindTransform(root, objectName) as RectTransform;
    private static TMP_Text FindText(Transform root, string objectName) => FindTransform(root, objectName)?.GetComponent<TMP_Text>();

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }
        return null;
    }

    private static RectTransform FindSceneRect(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            RectTransform match = FindRect(root.transform, objectName);
            if (match != null)
            {
                return match;
            }
        }
        return null;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
        {
            return;
        }
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void SetTopStretch(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void SetTopRight(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private static void SetTopCenter(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }
}
#endif
