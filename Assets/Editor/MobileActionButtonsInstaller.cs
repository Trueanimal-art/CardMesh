#if UNITY_EDITOR
using CardMesh;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class MobileActionButtonsInstaller
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string MedallionPath =
        "Assets/Art/UI/GameActions/VariantA/button_tactical_medallion_v1.png";
    private const string HintIconPath =
        "Assets/Art/UI/GameActions/VariantA/icon_hint_oracle_final.png";
    private const string UndoIconPath =
        "Assets/Art/UI/GameActions/VariantA/icon_undo_card_final.png";
    private const string AddMovesIconPath =
        "Assets/Art/UI/GameActions/VariantA/icon_add_moves_cards_final.png";
    private const string SharedFontPath =
        "Assets/Resources/Fonts/Roboto-Bold Cyrillic SDF.asset";

    [MenuItem("Tools/Card Mesh/Mobile/Rebuild Tactical Action Medallions")]
    public static void RebuildRoyalActionButtons()
    {
        ConfigureSpriteImporter(MedallionPath, Vector4.zero, 512);
        ConfigureSpriteImporter(HintIconPath, Vector4.zero, 512);
        ConfigureSpriteImporter(UndoIconPath, Vector4.zero, 512);
        ConfigureSpriteImporter(AddMovesIconPath, Vector4.zero, 512);

        Sprite medallion = AssetDatabase.LoadAssetAtPath<Sprite>(MedallionPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedFontPath);
        RebuildPrefab(
            "Assets/Prefabs/UI/Game/HintButton.prefab",
            "HintButtonIcon",
            "HintButtonLabel",
            "HintButtonBadge",
            "ПОДСКАЗКА",
            HintIconPath,
            medallion,
            font);
        RebuildPrefab(
            "Assets/Prefabs/UI/Game/UndoButton.prefab",
            "UndoButtonIcon",
            "UndoButtonLabel",
            "UndoButtonBadge",
            "ОТМЕНИТЬ ХОД",
            UndoIconPath,
            medallion,
            font);
        RebuildPrefab(
            "Assets/Prefabs/UI/Game/BoosterButton.prefab",
            "BoosterDeckButtonIcon",
            "BoosterDeckButtonLabel",
            "BoosterDeckButtonBadge",
            "+10 ХОДОВ",
            AddMovesIconPath,
            medallion,
            font);

        RebuildSceneInstances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tactical action medallions rebuilt and saved.");
    }

    private static void RebuildPrefab(
        string prefabPath,
        string iconName,
        string labelName,
        string badgeName,
        string actionTitle,
        string iconPath,
        Sprite medallionSprite,
        TMP_FontAsset font)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (prefabPath.Contains("BoosterButton"))
            {
                root.name = "BoosterDeckButton";
            }

            bool isUndo = prefabPath.Contains("UndoButton");
            float medallionSize = isUndo ? 196f : 182f;
            float iconSize = isUndo ? 129f : 120f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(224f, 224f);

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = null;
            rootImage.type = Image.Type.Simple;
            rootImage.color = new Color(1f, 1f, 1f, 0.001f);
            rootImage.preserveAspect = false;
            rootImage.raycastTarget = true;

            RectTransform medallion = EnsureImageChild(
                root.transform,
                "MedallionVisual",
                medallionSprite);
            SetRect(
                medallion,
                new Vector2(medallionSize, medallionSize),
                new Vector2(0f, 34f));
            medallion.SetAsFirstSibling();
            Image medallionImage = medallion.GetComponent<Image>();
            medallionImage.type = Image.Type.Simple;
            medallionImage.color = Color.white;
            medallionImage.preserveAspect = true;
            medallionImage.raycastTarget = false;

            Button button = root.GetComponent<Button>();
            button.targetGraphic = medallionImage;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.03f, 0.94f, 1f);
            colors.pressedColor = new Color(0.82f, 0.84f, 0.78f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.38f, 0.58f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = root.AddComponent<LayoutElement>();
            }
            layoutElement.minWidth = 224f;
            layoutElement.preferredWidth = 224f;
            layoutElement.minHeight = 224f;
            layoutElement.preferredHeight = 224f;

            foreach (Shadow effect in root.GetComponents<Shadow>())
            {
                effect.enabled = false;
            }

            SetChildActive(root.transform, "TopGloss", false);
            SetChildActive(root.transform, "HoverGlow", false);
            SetChildActive(root.transform, iconName.Replace("Icon", "IconHalo"), false);

            Transform iconTransform = FindRecursive(root.transform, iconName);
            if (iconTransform != null)
            {
                iconTransform.SetParent(medallion, false);
                RectTransform iconRect = iconTransform as RectTransform;
                SetRect(
                    iconRect,
                    new Vector2(iconSize, iconSize),
                    Vector2.zero);
                Image iconImage = iconTransform.GetComponent<Image>();
                iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            Transform labelTransform = FindRecursive(root.transform, labelName);
            if (labelTransform != null)
            {
                labelTransform.gameObject.SetActive(true);
                RectTransform labelRect = labelTransform as RectTransform;
                SetRect(
                    labelRect,
                    new Vector2(220f, 28f),
                    new Vector2(0f, -78f));
                TMP_Text actionLabel = labelTransform.GetComponent<TMP_Text>();
                actionLabel.text = actionTitle;
                actionLabel.font = font;
                actionLabel.fontSharedMaterial =
                    font != null ? font.material : actionLabel.fontSharedMaterial;
                actionLabel.enableAutoSizing = true;
                actionLabel.fontSizeMin = 18f;
                actionLabel.fontSizeMax = 22f;
                actionLabel.fontStyle = FontStyles.Bold;
                actionLabel.alignment = TextAlignmentOptions.Center;
                actionLabel.color = new Color(0.95f, 0.91f, 0.78f, 1f);
                actionLabel.enableVertexGradient = false;
                actionLabel.textWrappingMode = TextWrappingModes.NoWrap;
                actionLabel.overflowMode = TextOverflowModes.Truncate;
                actionLabel.raycastTarget = false;
                DisableLegacyTextEffects(actionLabel);
            }

            Transform badgeTransform = FindRecursive(root.transform, badgeName);
            if (badgeTransform != null)
            {
                RectTransform badgeRect = badgeTransform as RectTransform;
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.sizeDelta = new Vector2(50f, 50f);
                badgeRect.anchoredPosition = new Vector2(-42f, -24f);
                Image badgeImage = badgeTransform.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.sprite = PremiumUISprites.CleanIconCircle != null
                        ? PremiumUISprites.CleanIconCircle
                        : PremiumUISprites.IconCircle;
                    badgeImage.type = Image.Type.Simple;
                    badgeImage.preserveAspect = true;
                    badgeImage.color = Color.white;
                    badgeImage.raycastTarget = false;
                }

                TMP_Text badgeText = badgeTransform.GetComponentInChildren<TMP_Text>(true);
                if (badgeText != null)
                {
                    badgeText.font = font;
                    badgeText.fontSharedMaterial =
                        font != null ? font.material : badgeText.fontSharedMaterial;
                    badgeText.color = new Color(0.98f, 0.94f, 0.82f, 1f);
                    badgeText.enableAutoSizing = true;
                    badgeText.fontSize = 30f;
                    badgeText.fontSizeMin = 22f;
                    badgeText.fontSizeMax = 30f;
                    badgeText.fontStyle = FontStyles.Bold;
                    badgeText.alignment = TextAlignmentOptions.Center;
                    badgeText.text = "1";
                    badgeText.raycastTarget = false;
                }
            }

            if (prefabPath.Contains("HintButton"))
            {
                EnsureSecondaryLabel(
                    root.transform,
                    "HintButtonSubRow",
                    "900",
                    font,
                    true);
                // The price row is runtime-only: it is revealed by UIManager
                // when the player has no free hints. Keeping it authored as
                // active leaves a lone coin glyph under the label in previews
                // and during the first layout frame.
                SetChildActive(root.transform, "HintButtonSubRow", false);
            }
            else if (prefabPath.Contains("BoosterButton"))
            {
                EnsureSecondaryLabel(
                    root.transform,
                    "BoosterDeckButtonSubRow",
                    "ЗА РЕКЛАМУ",
                    font,
                    false);
            }
            else
            {
                SetChildActive(root.transform, "UndoButtonSubRow", false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSpriteImporter(
        string assetPath,
        Vector4 border,
        int maxTextureSize)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = maxTextureSize;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);

        TextureImporterPlatformSettings android =
            importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = maxTextureSize;
        android.format = TextureImporterFormat.ASTC_4x4;
        android.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }

    private static void EnsureSecondaryLabel(
        Transform root,
        string objectName,
        string value,
        TMP_FontAsset font,
        bool includeCoin)
    {
        Transform existing = FindRecursive(root, objectName);
        RectTransform row = null;
        TextMeshProUGUI label = null;
        string textObjectName = objectName.Replace("SubRow", "SubText");

        // Migrate the legacy layout where TMP_Text was attached directly to
        // the SubRow GameObject. The stable authored hierarchy is:
        // SubRow (Image) -> SubText (TMP_Text).
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI legacyLabel))
        {
            Transform originalParent = existing.parent;
            existing.name = textObjectName;

            GameObject rowObject = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            rowObject.transform.SetParent(originalParent, false);
            row = rowObject.GetComponent<RectTransform>();

            existing.SetParent(row, false);
            label = legacyLabel;
        }
        if (existing == null)
        {
            GameObject rowObject = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            rowObject.transform.SetParent(root, false);
            row = rowObject.GetComponent<RectTransform>();
        }
        else if (label == null)
        {
            row = existing as RectTransform;
            Transform labelTransform = FindRecursive(row, textObjectName);
            label = labelTransform != null
                ? labelTransform.GetComponent<TextMeshProUGUI>()
                : null;
        }

        if (row == null)
        {
            return;
        }

        if (label == null)
        {
            GameObject labelObject = new(
                textObjectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(row, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        SetRect(
            row,
            new Vector2(includeCoin ? 104f : 136f, 18f),
            new Vector2(0f, -102f));
        Image rowImage = row.GetComponent<Image>();
        if (rowImage == null)
        {
            rowImage = row.gameObject.AddComponent<Image>();
        }
        // Price/ad information floats directly on the board. The previous
        // chip-shaped plate read as an unrelated glow beneath the button.
        rowImage.sprite = null;
        rowImage.type = Image.Type.Simple;
        rowImage.color = new Color(1f, 1f, 1f, 0f);
        rowImage.raycastTarget = false;

        Transform coinTransform = FindRecursive(row, "CoinDot");
        if (includeCoin)
        {
            if (coinTransform == null)
            {
                GameObject coinObject = new(
                    "CoinDot",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                coinObject.transform.SetParent(row, false);
                coinTransform = coinObject.transform;
            }

            RectTransform coinRect = coinTransform as RectTransform;
            SetRect(coinRect, new Vector2(20f, 20f), new Vector2(-37f, 0f));
            Image coinImage = coinTransform.GetComponent<Image>();
            coinImage.sprite = PremiumUISprites.CoinIcon;
            coinImage.color = Color.white;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;
            coinTransform.gameObject.SetActive(true);
        }
        else if (coinTransform != null)
        {
            coinTransform.gameObject.SetActive(false);
        }

        if (label == null)
        {
            return;
        }

        SetRect(
            label.rectTransform,
            new Vector2(includeCoin ? 66f : 132f, 18f),
            new Vector2(includeCoin ? 15f : 0f, 0f));
        label.text = value;
        label.font = font;
        label.fontSharedMaterial =
            font != null ? font.material : label.fontSharedMaterial;
        label.fontSize = includeCoin ? 18f : 16f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableVertexGradient = false;
        label.color = new Color(0.78f, 0.76f, 0.67f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        label.raycastTarget = false;
        DisableLegacyTextEffects(label);
    }

    private static void RebuildSceneInstances()
    {
        Scene scene = EditorSceneManager.OpenScene(
            MainScenePath,
            OpenSceneMode.Single);
        RectTransform bottomHud = FindSceneRect(scene, "BottomHUD");
        if (bottomHud != null)
        {
            bottomHud.sizeDelta = new Vector2(0f, 224f);
            bottomHud.anchoredPosition = new Vector2(0f, 208f);
            EditorUtility.SetDirty(bottomHud);
        }

        ConfigureSceneButton(scene, "HintButton", new Vector2(-250f, 0f));
        ConfigureSceneButton(scene, "UndoButton", new Vector2(0f, 14f));
        ConfigureSceneButton(
            scene,
            "BoosterDeckButton",
            new Vector2(250f, 0f));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureSceneButton(
        Scene scene,
        string objectName,
        Vector2 anchoredPosition)
    {
        RectTransform rect = FindSceneRect(scene, objectName);
        if (rect == null)
        {
            Debug.LogError(
                $"Tactical action scene instance not found: {objectName}");
            return;
        }

        if (PrefabUtility.IsAnyPrefabInstanceRoot(rect.gameObject))
        {
            PrefabUtility.RevertPrefabInstance(
                rect.gameObject,
                InteractionMode.AutomatedAction);
        }

        SetRect(rect, new Vector2(224f, 224f), anchoredPosition);
        EditorUtility.SetDirty(rect);
    }

    private static RectTransform FindSceneRect(
        Scene scene,
        string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (RectTransform rect in
                     root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName)
                {
                    return rect;
                }
            }
        }

        return null;
    }

    private static void SetChildActive(
        Transform root,
        string childName,
        bool active)
    {
        Transform child = FindRecursive(root, childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static RectTransform EnsureImageChild(
        Transform parent,
        string objectName,
        Sprite sprite)
    {
        Transform existing = FindRecursive(parent, objectName);
        RectTransform rect = existing as RectTransform;
        if (rect == null)
        {
            GameObject child = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }
        else if (rect.parent != parent)
        {
            rect.SetParent(parent, false);
        }

        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.sprite = sprite;
        image.raycastTarget = false;
        return rect;
    }

    private static void DisableLegacyTextEffects(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        foreach (Shadow effect in text.GetComponents<Shadow>())
        {
            effect.enabled = false;
        }
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
    }

    private static Transform FindRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }
}
#endif
