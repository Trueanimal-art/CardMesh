using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public static class RuntimeSceneBootstrapper
    {
        private const string DefaultLevelResourcePath = "Levels/Level_1";
        private const string CyrillicValidationSample = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюя";
        private const string RuntimeFontResourcePath = "Fonts/Roboto-Bold";
        private const string CyrillicValidationSampleSafe =
            "\u0410\u0411\u0412\u0413\u0414\u0415\u0416\u0417\u0418\u0419\u041A\u041B\u041C\u041D\u041E\u041F\u0420\u0421\u0422\u0423\u0424\u0425\u0426\u0427\u0428\u0429\u042A\u042B\u042C\u042D\u042E\u042F" +
            "\u0430\u0431\u0432\u0433\u0434\u0435\u0436\u0437\u0438\u0439\u043A\u043B\u043C\u043D\u043E\u043F\u0440\u0441\u0442\u0443\u0444\u0445\u0446\u0447\u0448\u0449\u044A\u044B\u044C\u044D\u044E\u044F";

        private static TMP_FontAsset cachedRuntimeFontAsset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "Main")
            {
                return;
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null && gameManager.HasPresentationBindings && gameManager.AssignedLevelDefinition != null)
            {
                return;
            }

            BindManualScene(gameManager);
        }

        public static void BindManualScene(GameManager existingGameManager)
        {
            EnsureEventSystem();

            GameManager gameManager = existingGameManager;
            if (gameManager == null)
            {
                GameObject gameRoot = new("GameRoot");
                gameManager = gameRoot.AddComponent<GameManager>();
                gameRoot.AddComponent<StubPlatformService>();
            }

            CardDatabase cardDatabase = gameManager.GetComponent<CardDatabase>();
            if (cardDatabase == null)
            {
                cardDatabase = gameManager.gameObject.AddComponent<CardDatabase>();
            }

            if (gameManager.AssignedLevelDefinition == null)
            {
                gameManager.SetLevelDefinition(Resources.Load<LevelDefinition>(DefaultLevelResourcePath));
            }

            SolitaireSceneRefs sceneRefs = Object.FindFirstObjectByType<SolitaireSceneRefs>();
            if (sceneRefs == null)
            {
                sceneRefs = gameManager.GetComponent<SolitaireSceneRefs>();
                if (sceneRefs == null)
                {
                    sceneRefs = gameManager.gameObject.AddComponent<SolitaireSceneRefs>();
                }
            }

            if (!sceneRefs.ResolveSceneReferences())
            {
                Debug.LogError("RuntimeSceneBootstrapper: Failed to bind manual scene references.");
                return;
            }

            TMP_FontAsset fontAsset = LoadRuntimeFont();
            ApplyRuntimeFontToScene(sceneRefs.SafeAreaRoot != null ? sceneRefs.SafeAreaRoot : gameManager.transform, fontAsset);
            CardView cardPrefab = CreateCardPrefab(sceneRefs.SafeAreaRoot != null ? sceneRefs.SafeAreaRoot : gameManager.transform, fontAsset);

            UIManager uiManager = gameManager.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = gameManager.gameObject.AddComponent<UIManager>();
            }

            EnsureWinScreen(sceneRefs.SafeAreaRoot != null ? sceneRefs.SafeAreaRoot : gameManager.transform, fontAsset);

            PlatformService platformService = gameManager.GetComponent<PlatformService>();
            if (platformService == null)
            {
                platformService = gameManager.gameObject.AddComponent<StubPlatformService>();
            }

            uiManager.Configure(sceneRefs, null, null);

            gameManager.Configure(
                uiManager,
                platformService,
                sceneRefs.SafeAreaRoot != null ? sceneRefs.SafeAreaRoot : sceneRefs.ColumnSlotsRoot,
                sceneRefs.ColumnSlotsRoot,
                sceneRefs.ColumnSlotsRoot != null ? sceneRefs.ColumnSlotsRoot.GetComponent<HorizontalLayoutGroup>() : null,
                cardPrefab,
                BuildColumns(sceneRefs),
                BuildGoalSlots(sceneRefs),
                BuildDrawSlot(sceneRefs),
                null,
                cardDatabase,
                gameManager.AssignedLevelDefinition,
                sceneRefs);

            Debug.Log($"RuntimeSceneBootstrapper: Bound manual scene refs. Columns={sceneRefs.Columns.Count}.");
        }

        private static List<CardColumn> BuildColumns(SolitaireSceneRefs sceneRefs)
        {
            List<CardColumn> columns = new();
            foreach (ColumnRef columnRef in sceneRefs.Columns)
            {
                if (columnRef?.Root == null)
                {
                    continue;
                }

                EnsureRaycastGraphic(columnRef.Root.gameObject);

                CardColumn column = columnRef.Root.GetComponent<CardColumn>();
                if (column == null)
                {
                    column = columnRef.Root.gameObject.AddComponent<CardColumn>();
                }

                column.ConfigureComponents(
                    columnRef.Root.GetComponent<Image>(),
                    columnRef.Root.GetComponent<LayoutElement>(),
                    columnRef.CardsStackRoot,
                    columnRef.CrownSlot as RectTransform);
                columns.Add(column);
            }

            return columns;
        }

        private static List<GoalSlot> BuildGoalSlots(SolitaireSceneRefs sceneRefs)
        {
            List<GoalSlot> goalSlots = new();
            foreach (ColumnRef columnRef in sceneRefs.Columns)
            {
                if (columnRef?.CrownSlot == null)
                {
                    continue;
                }

                EnsureRaycastGraphic(columnRef.CrownSlot.gameObject);

                GoalSlot goalSlot = columnRef.CrownSlot.GetComponent<GoalSlot>();
                if (goalSlot == null)
                {
                    goalSlot = columnRef.CrownSlot.gameObject.AddComponent<GoalSlot>();
                }

                goalSlot.ConfigureComponents(
                    columnRef.CrownSlot.GetComponent<Image>(),
                    columnRef.CrownSlot.GetComponent<LayoutElement>(),
                    columnRef.CrownSlot);
                goalSlots.Add(goalSlot);
            }

            return goalSlots;
        }

        private static DrawSlot BuildDrawSlot(SolitaireSceneRefs sceneRefs)
        {
            RectTransform deckCardBack = sceneRefs.DeckCardBack;
            EnsureRaycastGraphic(deckCardBack.gameObject);
            Transform drawCardsRoot = deckCardBack.parent != null ? deckCardBack.parent : deckCardBack;

            DrawSlot drawSlot = deckCardBack.GetComponent<DrawSlot>();
            if (drawSlot == null)
            {
                drawSlot = deckCardBack.gameObject.AddComponent<DrawSlot>();
            }

            drawSlot.ConfigureComponents(
                deckCardBack.GetComponent<Image>(),
                deckCardBack.GetComponent<LayoutElement>(),
                sceneRefs.DeckCounter,
                drawCardsRoot);

            return drawSlot;
        }

        private static void EnsureRaycastGraphic(GameObject target)
        {
            if (target == null || target.GetComponent<Graphic>() != null)
            {
                return;
            }

            Image image = target.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
        }

        private static CardView CreateCardPrefab(Transform parent, TMP_FontAsset fontAsset)
        {
            RectTransform cardRoot = CreateRect(
                "__RuntimeCardTemplate",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(150f, 220f),
                Vector2.zero);
            cardRoot.gameObject.hideFlags = HideFlags.HideInHierarchy;
            cardRoot.gameObject.SetActive(false);

            Image backgroundImage = cardRoot.gameObject.AddComponent<Image>();
            LayoutElement layoutElement = cardRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 150f;
            layoutElement.preferredHeight = 220f;
            layoutElement.minWidth = 150f;
            layoutElement.minHeight = 220f;

            Outline outline = cardRoot.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.effectColor = new Color(0f, 0f, 0f, 0.14f);

            CanvasGroup canvasGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();

            RectTransform iconRoot = CreateRect(
                "Icon",
                cardRoot,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(30f, 30f),
                new Vector2(-10f, -10f));
            Image iconImage = iconRoot.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.enabled = false;

            RectTransform titleRoot = CreateRect(
                "Title",
                cardRoot,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-24f, -64f),
                Vector2.zero);
            TextMeshProUGUI titleLabel = titleRoot.gameObject.AddComponent<TextMeshProUGUI>();
            titleLabel.font = fontAsset;
            titleLabel.fontSize = 34f;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 18f;
            titleLabel.fontSizeMax = 34f;
            titleLabel.enableWordWrapping = true;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = new Color(0.26f, 0.2f, 0.16f, 1f);
            titleLabel.raycastTarget = false;

            RectTransform footerRoot = CreateRect(
                "Footer",
                cardRoot,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(74f, 32f),
                new Vector2(-10f, -10f));
            TextMeshProUGUI footerLabel = footerRoot.gameObject.AddComponent<TextMeshProUGUI>();
            footerLabel.font = fontAsset;
            footerLabel.fontSize = 24f;
            footerLabel.fontStyle = FontStyles.Bold;
            footerLabel.enableAutoSizing = true;
            footerLabel.fontSizeMin = 14f;
            footerLabel.fontSizeMax = 24f;
            footerLabel.alignment = TextAlignmentOptions.TopRight;
            footerLabel.color = new Color(0.35f, 0.28f, 0.14f, 0.9f);
            footerLabel.raycastTarget = false;

            CardView cardView = cardRoot.gameObject.AddComponent<CardView>();
            cardView.ConfigureComponents(backgroundImage, outline, layoutElement, canvasGroup, titleRoot, titleLabel, footerRoot, footerLabel, iconImage);
            return cardView;
        }

        private static WinScreenView EnsureWinScreen(Transform parent, TMP_FontAsset fontAsset)
        {
            WinScreenView existingWinScreen = Object.FindFirstObjectByType<WinScreenView>(FindObjectsInactive.Include);
            if (existingWinScreen != null)
            {
                return existingWinScreen;
            }

            RectTransform root = CreateRect(
                "WinScreen",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            Image overlay = root.gameObject.AddComponent<Image>();
            overlay.color = new Color(0.05f, 0.12f, 0.08f, 0.58f);

            RectTransform panel = CreateRect(
                "Panel",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(520f, 300f),
                Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 0.95f, 0.78f, 0.98f);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0.08f, 0.05f, 0.01f, 0.35f);
            panelShadow.effectDistance = new Vector2(0f, -10f);

            TextMeshProUGUI titleText = CreateWinText(
                "Title",
                panel,
                fontAsset,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(460f, 70f),
                new Vector2(0f, -36f),
                42f,
                new Color(0.23f, 0.18f, 0.08f, 1f));
            titleText.text = "\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u043f\u0440\u043e\u0439\u0434\u0435\u043d";

            TextMeshProUGUI movesText = CreateWinText(
                "Moves",
                panel,
                fontAsset,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(360f, 48f),
                new Vector2(0f, 24f),
                30f,
                new Color(0.37f, 0.29f, 0.14f, 1f));

            Button retryButton = CreateWinButton(panel, fontAsset, "\u041f\u043e\u0432\u0442\u043e\u0440\u0438\u0442\u044c", new Vector2(-120f, -92f));
            Button menuButton = CreateWinButton(panel, fontAsset, "\u0412 \u043c\u0435\u043d\u044e", new Vector2(120f, -92f));

            WinScreenView winScreen = root.gameObject.AddComponent<WinScreenView>();
            winScreen.ConfigureComponents(canvasGroup, panel, titleText, movesText, retryButton, menuButton);
            root.gameObject.SetActive(false);
            return winScreen;
        }

        private static TextMeshProUGUI CreateWinText(
            string name,
            Transform parent,
            TMP_FontAsset fontAsset,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 position,
            float fontSize,
            Color color)
        {
            RectTransform textRoot = CreateRect(name, parent, anchorMin, anchorMax, pivot, size, position);
            TextMeshProUGUI text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize - 12f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateWinButton(Transform parent, TMP_FontAsset fontAsset, string label, Vector2 position)
        {
            RectTransform buttonRoot = CreateRect(
                label,
                parent,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(190f, 62f),
                position);
            Image image = buttonRoot.gameObject.AddComponent<Image>();
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 0.64f, 0.08f, 1f);

            Button button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Shadow shadow = buttonRoot.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.19f, 0.1f, 0f, 0.3f);
            shadow.effectDistance = new Vector2(0f, -5f);

            TextMeshProUGUI buttonText = CreateWinText(
                "Text",
                buttonRoot,
                fontAsset,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                24f,
                new Color(0.22f, 0.18f, 0.08f, 1f));
            buttonText.rectTransform.offsetMin = Vector2.zero;
            buttonText.rectTransform.offsetMax = Vector2.zero;
            buttonText.text = label;

            return button;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static TMP_FontAsset LoadRuntimeFont()
        {
            if (cachedRuntimeFontAsset != null)
            {
                return cachedRuntimeFontAsset;
            }

            TMP_FontAsset runtimeFontAsset = CreateCyrillicRuntimeFontAsset();
            if (runtimeFontAsset != null)
            {
                cachedRuntimeFontAsset = runtimeFontAsset;
                return cachedRuntimeFontAsset;
            }

            TMP_FontAsset defaultFontAsset = TMP_Settings.defaultFontAsset;
            cachedRuntimeFontAsset = defaultFontAsset != null
                ? defaultFontAsset
                : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            return cachedRuntimeFontAsset;
        }

        private static bool HasRequiredCharacters(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return false;
            }

            return fontAsset.HasCharacters(CyrillicValidationSampleSafe);
        }

        private static TMP_FontAsset CreateCyrillicRuntimeFontAsset()
        {
            Font sourceFont = Resources.Load<Font>(RuntimeFontResourcePath);
            if (sourceFont == null)
            {
                sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (sourceFont == null)
            {
                Debug.LogWarning("RuntimeSceneBootstrapper: failed to locate a runtime font with Cyrillic support.");
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                Debug.LogWarning($"RuntimeSceneBootstrapper: failed to create TMP font asset from '{sourceFont.name}'.");
                return null;
            }

            fontAsset.name = $"Runtime_{sourceFont.name}_Cyrillic";
            WarmUpCyrillicGlyphs(fontAsset);

            if (!HasRequiredCharacters(fontAsset))
            {
                Debug.LogWarning($"RuntimeSceneBootstrapper: runtime font '{sourceFont.name}' still misses some Cyrillic glyphs.");
            }

            return fontAsset;
        }

        private static void ApplyRuntimeFontToScene(Transform root, TMP_FontAsset fontAsset)
        {
            if (root == null || fontAsset == null)
            {
                return;
            }

            foreach (TMP_Text textComponent in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (textComponent == null)
                {
                    continue;
                }

                textComponent.font = fontAsset;
                textComponent.fontSharedMaterial = fontAsset.material;
            }
        }

        private static void WarmUpCyrillicGlyphs(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            MethodInfo tryAddCharactersMethod = typeof(TMP_FontAsset).GetMethod(
                "TryAddCharacters",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);

            if (tryAddCharactersMethod != null)
            {
                tryAddCharactersMethod.Invoke(fontAsset, new object[] { CyrillicValidationSampleSafe });
            }
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }
    }
}
