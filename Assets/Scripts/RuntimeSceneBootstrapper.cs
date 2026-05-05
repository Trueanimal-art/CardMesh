using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public static class RuntimeSceneBootstrapper
    {
        private const string DefaultLevelResourcePath = "Levels/Level_1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "Main")
            {
                return;
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null && gameManager.HasPresentationBindings)
            {
                return;
            }

            BindManualScene(gameManager);
        }

        private static void BindManualScene(GameManager existingGameManager)
        {
            EnsureEventSystem();

            GameManager gameManager = existingGameManager;
            if (gameManager == null)
            {
                GameObject gameRoot = new("GameRoot");
                gameManager = gameRoot.AddComponent<GameManager>();
                gameRoot.AddComponent<StubPlatformService>();
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
            CardView cardPrefab = CreateCardPrefab(sceneRefs.SafeAreaRoot != null ? sceneRefs.SafeAreaRoot : gameManager.transform, fontAsset);

            UIManager uiManager = gameManager.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = gameManager.gameObject.AddComponent<UIManager>();
            }

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
                    columnRef.CardsStackRoot);
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

            DrawSlot drawSlot = deckCardBack.GetComponent<DrawSlot>();
            if (drawSlot == null)
            {
                drawSlot = deckCardBack.gameObject.AddComponent<DrawSlot>();
            }

            drawSlot.ConfigureComponents(
                deckCardBack.GetComponent<Image>(),
                deckCardBack.GetComponent<LayoutElement>(),
                sceneRefs.DeckCounter,
                deckCardBack);

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
            cardView.ConfigureComponents(backgroundImage, outline, layoutElement, canvasGroup, titleLabel, footerLabel, iconImage);
            return cardView;
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
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
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
