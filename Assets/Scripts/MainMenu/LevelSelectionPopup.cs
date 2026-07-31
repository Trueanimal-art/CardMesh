using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [ExecuteAlways]
    public sealed class LevelSelectionPopup : MonoBehaviour
    {
        private const string PopupName = "LevelSelectionPopup";
        private const string ResourcesPrefabPath = "UI/MainMenu/LevelSelectionPopup";
        private const float FadeDuration = 0.18f;
        private const string ScrollbarName = "LevelsScrollbar";
        private const string ScrollbarSlidingAreaName = "SlidingArea";
        private const string ScrollbarHandleName = "Handle";
        private const string FadeTopName = "LevelsFadeTop";
        private const string FadeBottomName = "LevelsFadeBottom";
        private static readonly Vector2 PanelSize = new(920f, 700f);
        private static readonly Vector2 TitleOffset = new(0f, -64f);
        private static readonly Vector2 TitleSize = new(-132f, 84f);
        private static readonly Vector2 StatsOffset = new(0f, -126f);
        private static readonly Vector2 StatsSize = new(-170f, 42f);
        private static readonly Vector2 CloseOffset = new(-80f, -132f);
        private static readonly Vector2 CloseSize = new(58f, 58f);
        private static readonly Vector2 ScrollSize = new(720f, 432f);
        private static readonly Vector2 ScrollOffset = new(0f, -184f);
        private const float ScrollViewportHorizontalInset = 18f;
        private const float ScrollViewportTopInset = 28f;
        private const float ScrollViewportBottomInset = 28f;
        private const float ScrollbarReservedWidth = 56f;
        private const float EdgeFadeHeight = 52f;
        private const float LevelButtonHeight = 62f;
        private const float LevelButtonSpacing = 10f;
        private const int LevelListHorizontalPadding = 20;
        private const int LevelListVerticalPadding = 10;
        private const float RuntimeListMinTopReserve = 184f;
        private const float RuntimeListBottomReserve = 150f;
        private const float RuntimeListHorizontalInset = 104f;
        private const float RuntimeStatsGap = 34f;
        private const float RuntimeTitleNudgeDown = 6f;
        private const float RuntimeStatsNudgeDown = 12f;
        private const float GeneratedListHeaderGap = 44f;
        private const int MaxGeneratedListAdditionalTopPadding = 96;

        private readonly Color textGold = new(1f, 0.83f, 0.34f, 1f);
        private readonly Color textDark = new(0.16f, 0.09f, 0.02f, 1f);
        private readonly Color textCream = new(1f, 0.93f, 0.64f, 1f);

        [Header("Runtime bindings")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private ScrollRect levelsScrollRect;
        [SerializeField] private Scrollbar levelsScrollbar;
        [SerializeField] private Button dimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool useRuntimeSafeLayout;

        private TMP_FontAsset fontAsset;
        private LevelProgressionManager progressionManager;
        private Action<int> onLevelSelected;
        private Coroutine animationRoutine;
        private Vector3 shownPanelScale = Vector3.one;
        private Vector2 runtimeTitleBasePosition;
        private Vector2 runtimeStatsBasePosition;
        private readonly Dictionary<RectTransform, AuthoredRectLayout> authoredPlayModeLayouts = new();
        private bool hasCapturedShownPanelScale;
        private bool hasCapturedRuntimeTextPositions;
        private bool hasCapturedAuthoredPlayModeLayout;
        private bool isBusy;

        public static LevelSelectionPopup SceneInstance =>
            FindFirstObjectByType<LevelSelectionPopup>(FindObjectsInactive.Include);

        public static LevelSelectionPopup GetOrCreate()
        {
            LevelSelectionPopup existing = SceneInstance;
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = GetOrCreateCanvas();
            EnsureEventSystem();

            GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, canvas.transform, false);
                instance.name = PopupName;
                StretchToFill(instance.transform as RectTransform, 0f, 0f, 0f, 0f);

                LevelSelectionPopup popup = instance.GetComponent<LevelSelectionPopup>();
                if (popup == null)
                {
                    popup = instance.AddComponent<LevelSelectionPopup>();
                }

                popup.BindSceneReferences();
                popup.WireStaticButtons();
                popup.ApplyHiddenVisuals();
                instance.SetActive(false);
                return popup;
            }

            GameObject root = new(PopupName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            StretchToFill(root.transform as RectTransform, 0f, 0f, 0f, 0f);

            LevelSelectionPopup fallback = root.AddComponent<LevelSelectionPopup>();
            fallback.EnsureAuthoringHierarchy();
            root.SetActive(false);
            return fallback;
        }

        public void Show(LevelProgressionManager targetProgressionManager, Action<int> selectedCallback)
        {
            BindSceneReferences();
            CaptureAuthoredPlayModeLayout();
            CaptureShownPanelScale();
            if (panel == null || contentRoot == null)
            {
                EnsureAuthoringHierarchy();
                CaptureAuthoredPlayModeLayout();
            }

            progressionManager = targetProgressionManager;
            onLevelSelected = selectedCallback;

            if (progressionManager == null)
            {
                progressionManager = GetComponent<LevelProgressionManager>();
                if (progressionManager == null)
                {
                    progressionManager = gameObject.AddComponent<LevelProgressionManager>();
                }
            }

            progressionManager.GetOrCreateSeedForCurrentLevel();
            ConfigureScrollHierarchy();
            RebuildLevelButtons();
            RestoreAuthoredPlayModeLayout();
            isBusy = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlayAnimation(true, null);
        }

        public void Hide()
        {
            PlayAnimation(false, () => gameObject.SetActive(false));
        }

        public void EnsureAuthoringHierarchy()
        {
            BindSceneReferences();
            if (panel == null || contentRoot == null)
            {
                BuildUi();
                BindSceneReferences();
            }

            RemoveUpperDivider();
            ConfigureScrollHierarchy();
            RestoreAuthoredPlayModeLayout();
            ApplyFontsToAllTexts();
            WireStaticButtons();
            ApplyInitialVisuals();
        }

        private void Reset()
        {
            EnsureAuthoringHierarchy();
        }

        private void Awake()
        {
            BindSceneReferences();
            CaptureAuthoredPlayModeLayout();
            CaptureShownPanelScale();
            RemoveUpperDivider();
            ConfigureScrollHierarchy();
            RestoreAuthoredPlayModeLayout();
            ApplyFontsToAllTexts();
            WireStaticButtons();
            ApplyInitialVisuals();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BindSceneReferences();
                CaptureShownPanelScale();
                RemoveUpperDivider();
                ConfigureScrollHierarchy();
                ApplyFontsToAllTexts();
                WireStaticButtons();
                ApplyEditorPreviewVisuals();
            }
        }

        private void BindSceneReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            panel ??= transform.Find("Panel") as RectTransform;
            statsText ??= FindChildRecursive<TextMeshProUGUI>(transform, "Stats");
            levelsScrollRect ??= FindChildRecursive<ScrollRect>(transform, "LevelsScroll");
            levelsScrollbar ??= FindChildRecursive<Scrollbar>(transform, ScrollbarName);
            dimButton ??= FindButton(transform, "Dim");
            if (panel != null)
            {
                closeButton = PopupShell.EnsureIconCloseButton(panel, "CloseButton");
                SetAnchored(
                    closeButton.transform as RectTransform,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    CloseSize,
                    CloseOffset);

                Image closeImage = closeButton.GetComponent<Image>();
                if (closeImage != null)
                {
                    closeImage.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
                }

                closeButton.transform.SetAsLastSibling();
            }

            if (contentRoot == null)
            {
                Transform content = transform.Find("Panel/LevelsScroll/Viewport/Content");
                contentRoot = content as RectTransform ?? FindChildRecursive<RectTransform>(transform, "Content");
            }

            fontAsset ??= ResolveFont();
        }

        private void RemoveUpperDivider()
        {
            Transform divider = panel != null ? panel.Find("DividerStrip") : null;
            if (divider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(divider.gameObject);
            }
            else
            {
                DestroyImmediate(divider.gameObject);
            }
        }

        private void WireStaticButtons()
        {
            if (dimButton != null)
            {
                dimButton.onClick.RemoveListener(Hide);
                dimButton.onClick.AddListener(Hide);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
        }

        private void BuildUi()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            fontAsset = ResolveFont();

            if (transform.Find("Panel") != null)
            {
                return;
            }

            GameObject dim = CreateChild(gameObject, "Dim", typeof(Image), typeof(Button));
            StretchToFill(dim.transform as RectTransform, 0f, 0f, 0f, 0f);
            Image dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0.01f, 0.045f, 0.028f, 0.72f);
            dimButton = dim.GetComponent<Button>();

            GameObject panelObject = CreateChild(gameObject, "Panel", typeof(Image));
            panel = panelObject.transform as RectTransform;
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), PanelSize, Vector2.zero);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = GetPauseWindowSprite();
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            panelImage.raycastTarget = false;
            AddShadow(panelObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

            TextMeshProUGUI title = CreateText(panel, "Title", "Выбор уровня", 54f, textGold, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), TitleSize, TitleOffset);
            title.fontStyle = FontStyles.Bold;
            AddTextOutline(title, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

            statsText = CreateText(panel, "Stats", string.Empty, 30f, textCream, TextAlignmentOptions.Center);
            SetAnchored(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), StatsSize, StatsOffset);
            AddTextOutline(statsText, new Color(0.12f, 0.055f, 0.012f, 0.48f), new Vector2(1f, -1f));

            closeButton = CreateCloseButton(panel);

            GameObject scrollRoot = CreateChild(panelObject, "LevelsScroll", typeof(RectTransform), typeof(ScrollRect));
            RectTransform scrollRectTransform = scrollRoot.transform as RectTransform;
            SetAnchored(scrollRectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), ScrollSize, ScrollOffset);

            GameObject viewport = CreateChild(scrollRoot, "Viewport", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewportRect = viewport.transform as RectTransform;
            ApplyViewportLayout(viewportRect, false);

            GameObject content = CreateChild(viewport, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentRoot = content.transform as RectTransform;
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(LevelListHorizontalPadding, LevelListHorizontalPadding, LevelListVerticalPadding, LevelListVerticalPadding);
            layout.spacing = LevelButtonSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            levelsScrollRect = scrollRoot.GetComponent<ScrollRect>();
            ConfigureScrollHierarchy();
        }

        private void RebuildLevelButtons()
        {
            if (progressionManager == null || contentRoot == null)
            {
                return;
            }

            for (int index = contentRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(contentRoot.GetChild(index).gameObject);
            }

            int completedLevels = progressionManager.MaxCompletedLevel;
            int displayedLevels = Mathf.Max(1, progressionManager.MaxUnlockedLevel);
            if (statsText != null)
            {
                statsText.text = completedLevels > 0
                    ? $"Пройдено уровней: {completedLevels}"
                    : "Пройденных уровней пока нет";
            }

            for (int level = 1; level <= displayedLevels; level++)
            {
                CreateLevelButton(level);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            if (levelsScrollRect != null)
            {
                levelsScrollRect.verticalNormalizedPosition = 1f;
            }

            UpdateScrollbarVisibility();
            RestoreAuthoredPlayModeLayout();
        }

        private void CreateLevelButton(int level)
        {
            bool isCompleted = level <= progressionManager.MaxCompletedLevel;
            bool isSelected = level == progressionManager.LastSelectedLevel;

            Button button = CreateImageButton(contentRoot, $"Level_{level}", GetLevelButtonSprite(), Color.white);
            button.interactable = isCompleted;

            RectTransform rectTransform = button.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(0f, LevelButtonHeight);

            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = LevelButtonHeight;
            layoutElement.preferredHeight = LevelButtonHeight;

            Image image = button.GetComponent<Image>();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = isSelected
                ? Color.white
                : isCompleted
                    ? new Color(0.96f, 0.96f, 0.9f, 1f)
                    : new Color(0.68f, 0.72f, 0.58f, 0.72f);

            CanvasGroup rowGroup = button.gameObject.AddComponent<CanvasGroup>();
            rowGroup.alpha = isCompleted ? 1f : 0.62f;

            string labelText = isCompleted ? $"Уровень {level}" : "...";
            Color labelColor = isCompleted ? textGold : new Color(0.9f, 0.82f, 0.55f, 0.72f);
            TextMeshProUGUI label = CreateText(rectTransform, "Label", labelText, 36f, labelColor, TextAlignmentOptions.Center);
            StretchToFill(label.rectTransform, 28f, 28f, 0f, 0f);
            label.fontStyle = FontStyles.Bold;
            AddTextOutline(label, new Color(0.12f, 0.055f, 0.012f, isCompleted ? 0.62f : 0.34f), new Vector2(1f, -1f));

            if (isSelected)
            {
                AddOutline(button.gameObject, new Color(1f, 0.95f, 0.46f, 0.9f), new Vector2(2f, -2f));
            }

            if (isCompleted)
            {
                int selectedLevel = level;
                button.onClick.AddListener(() => SelectLevel(selectedLevel));
            }
        }

        private void ConfigureScrollHierarchy()
        {
            if (panel == null)
            {
                return;
            }

            RectTransform scrollRoot = (levelsScrollRect != null ? levelsScrollRect.transform : panel.Find("LevelsScroll")) as RectTransform;
            if (scrollRoot == null)
            {
                return;
            }

            RectTransform viewport = scrollRoot.Find("Viewport") as RectTransform;
            if (viewport != null)
            {
                Image viewportImage = viewport.GetComponent<Image>();
                if (viewportImage != null)
                {
                    viewportImage.color = Color.clear;
                    viewportImage.raycastTarget = false;
                }

                Mask mask = viewport.GetComponent<Mask>();
                if (mask != null)
                {
                    mask.showMaskGraphic = false;
                }

                if (viewport.GetComponent<RectMask2D>() == null)
                {
                    viewport.gameObject.AddComponent<RectMask2D>();
                }
            }

            Image scrollBackground = scrollRoot.GetComponent<Image>();
            if (scrollBackground != null)
            {
                scrollBackground.enabled = false;
                scrollBackground.color = Color.clear;
                scrollBackground.raycastTarget = false;
            }

            if (useRuntimeSafeLayout)
            {
                ApplyRuntimeHeaderNudge();
            }

            if (contentRoot == null)
            {
                contentRoot = scrollRoot.Find("Viewport/Content") as RectTransform;
            }

            if (contentRoot != null)
            {
                VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>() ?? contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>() ?? contentRoot.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            levelsScrollRect = scrollRoot.GetComponent<ScrollRect>() ?? scrollRoot.gameObject.AddComponent<ScrollRect>();
            levelsScrollbar = EnsureLevelsScrollbar(scrollRoot);
            if (useRuntimeSafeLayout)
            {
                ApplyRuntimeSafeListLayout(scrollRoot, viewport);
            }

            EnsureEdgeFades(scrollRoot);
            levelsScrollRect.viewport = viewport;
            levelsScrollRect.content = contentRoot;
            levelsScrollRect.horizontal = false;
            levelsScrollRect.vertical = true;
            levelsScrollRect.verticalScrollbar = levelsScrollbar;
            levelsScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            levelsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            levelsScrollRect.inertia = true;
            levelsScrollRect.scrollSensitivity = 36f;
            if (useRuntimeSafeLayout)
            {
                ApplyViewportLayout(viewport, false);
            }

            SetOverflowDecorationsVisible(false);
        }

        private void EnsureEdgeFades(RectTransform scrollRoot)
        {
            if (scrollRoot == null)
            {
                return;
            }

            Image topFade = EnsureEdgeFade(scrollRoot, FadeTopName, PremiumUISprites.LevelListFadeTop, out bool createdTopFade);
            Image bottomFade = EnsureEdgeFade(scrollRoot, FadeBottomName, PremiumUISprites.LevelListFadeBottom, out bool createdBottomFade);

            if (useRuntimeSafeLayout || createdTopFade)
            {
                PositionTopEdgeFade(topFade != null ? topFade.rectTransform : null);
            }

            if (useRuntimeSafeLayout || createdBottomFade)
            {
                PositionBottomEdgeFade(bottomFade != null ? bottomFade.rectTransform : null);
            }

            if (topFade != null && (createdTopFade || useRuntimeSafeLayout || !Application.isPlaying))
            {
                topFade.transform.SetAsLastSibling();
            }

            if (bottomFade != null && (createdBottomFade || useRuntimeSafeLayout || !Application.isPlaying))
            {
                bottomFade.transform.SetAsLastSibling();
            }

            if (levelsScrollbar != null && (useRuntimeSafeLayout || !Application.isPlaying))
            {
                levelsScrollbar.transform.SetAsLastSibling();
            }

            SetOverflowDecorationsVisible(false);
        }

        private Image EnsureEdgeFade(RectTransform scrollRoot, string objectName, Sprite sprite, out bool created)
        {
            RectTransform fadeRect = scrollRoot.Find(objectName) as RectTransform;
            Image fadeImage;
            created = false;
            if (fadeRect == null)
            {
                GameObject fadeObject = CreateChild(scrollRoot.gameObject, objectName, typeof(Image));
                fadeRect = fadeObject.transform as RectTransform;
                fadeImage = fadeObject.GetComponent<Image>();
                created = true;
            }
            else
            {
                fadeImage = fadeRect.GetComponent<Image>() ?? fadeRect.gameObject.AddComponent<Image>();
            }

            fadeImage.sprite = sprite;
            fadeImage.type = Image.Type.Simple;
            fadeImage.color = Color.white;
            fadeImage.raycastTarget = false;
            return fadeImage;
        }

        private static void PositionTopEdgeFade(RectTransform fadeRect)
        {
            if (fadeRect == null)
            {
                return;
            }

            fadeRect.anchorMin = new Vector2(0f, 1f);
            fadeRect.anchorMax = new Vector2(1f, 1f);
            fadeRect.pivot = new Vector2(0.5f, 1f);
            fadeRect.offsetMin = new Vector2(ScrollViewportHorizontalInset, -ScrollViewportTopInset - EdgeFadeHeight);
            fadeRect.offsetMax = new Vector2(-ScrollViewportHorizontalInset - ScrollbarReservedWidth, -ScrollViewportTopInset);
            fadeRect.localScale = Vector3.one;
        }

        private static void PositionBottomEdgeFade(RectTransform fadeRect)
        {
            if (fadeRect == null)
            {
                return;
            }

            fadeRect.anchorMin = new Vector2(0f, 0f);
            fadeRect.anchorMax = new Vector2(1f, 0f);
            fadeRect.pivot = new Vector2(0.5f, 0f);
            fadeRect.offsetMin = new Vector2(ScrollViewportHorizontalInset, ScrollViewportBottomInset);
            fadeRect.offsetMax = new Vector2(-ScrollViewportHorizontalInset - ScrollbarReservedWidth, ScrollViewportBottomInset + EdgeFadeHeight);
            fadeRect.localScale = Vector3.one;
        }

        private void ApplyRuntimeHeaderNudge()
        {
            if (!Application.isPlaying || panel == null)
            {
                return;
            }

            RectTransform title = FindChildRecursive<RectTransform>(panel, "Title");
            if (!hasCapturedRuntimeTextPositions)
            {
                runtimeTitleBasePosition = title != null ? title.anchoredPosition : Vector2.zero;
                runtimeStatsBasePosition = statsText != null ? statsText.rectTransform.anchoredPosition : Vector2.zero;
                hasCapturedRuntimeTextPositions = true;
            }

            if (title != null)
            {
                title.anchoredPosition = runtimeTitleBasePosition + new Vector2(0f, -RuntimeTitleNudgeDown);
            }

            if (statsText != null)
            {
                statsText.rectTransform.anchoredPosition = runtimeStatsBasePosition + new Vector2(0f, -RuntimeStatsNudgeDown);
            }
        }

        private void ApplyRuntimeSafeListLayout(RectTransform scrollRoot, RectTransform viewport)
        {
            if (!Application.isPlaying || scrollRoot == null || panel == null)
            {
                return;
            }

            float panelWidth = ResolveRectSize(panel, PanelSize).x;
            float panelHeight = ResolveRectSize(panel, PanelSize).y;
            float topReserve = ResolveRuntimeTopReserve(panelHeight);
            float bottomReserve = Mathf.Min(RuntimeListBottomReserve, panelHeight * 0.28f);
            float horizontalInset = Mathf.Min(RuntimeListHorizontalInset, panelWidth * 0.18f);

            if (topReserve + bottomReserve > panelHeight - 240f)
            {
                topReserve = Mathf.Max(RuntimeListMinTopReserve, panelHeight - bottomReserve - 240f);
            }

            StretchToFill(scrollRoot, horizontalInset, horizontalInset, topReserve, bottomReserve);

            if (viewport != null)
            {
                ApplyViewportLayout(viewport, false);
            }

            if (contentRoot != null)
            {
                if (viewport != null && contentRoot.parent != viewport)
                {
                    contentRoot.SetParent(viewport, false);
                }

                contentRoot.anchorMin = new Vector2(0f, 1f);
                contentRoot.anchorMax = new Vector2(1f, 1f);
                contentRoot.pivot = new Vector2(0.5f, 1f);
                contentRoot.offsetMin = Vector2.zero;
                contentRoot.offsetMax = Vector2.zero;

                VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>() ?? contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(LevelListHorizontalPadding, LevelListHorizontalPadding, LevelListVerticalPadding, LevelListVerticalPadding);
                layout.spacing = LevelButtonSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
            }

            if (levelsScrollbar != null)
            {
                SetAnchored(levelsScrollbar.transform as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(34f, -112f), new Vector2(-34f, 0f));
            }

            if (closeButton != null)
            {
                SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), CloseSize, CloseOffset);
                closeButton.transform.SetAsLastSibling();
            }
        }

        private float ResolveRuntimeTopReserve(float panelHeight)
        {
            float topReserve = Mathf.Clamp(panelHeight * 0.27f, RuntimeListMinTopReserve, panelHeight * 0.36f);
            if (statsText == null || panel == null)
            {
                return topReserve;
            }

            Bounds statsBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, statsText.rectTransform);
            float belowStatsReserve = (panelHeight * 0.5f) - statsBounds.min.y + RuntimeStatsGap;
            return Mathf.Clamp(Mathf.Max(topReserve, belowStatsReserve), RuntimeListMinTopReserve, panelHeight * 0.45f);
        }

        private static Vector2 ResolveRectSize(RectTransform rectTransform, Vector2 fallback)
        {
            if (rectTransform == null)
            {
                return fallback;
            }

            Vector2 size = rectTransform.rect.size;
            if (size.x > 1f && size.y > 1f)
            {
                return size;
            }

            size = rectTransform.sizeDelta;
            return new Vector2(
                Mathf.Abs(size.x) > 1f ? Mathf.Abs(size.x) : fallback.x,
                Mathf.Abs(size.y) > 1f ? Mathf.Abs(size.y) : fallback.y);
        }

        private Scrollbar EnsureLevelsScrollbar(RectTransform scrollRoot)
        {
            RectTransform scrollbarRect = scrollRoot.Find(ScrollbarName) as RectTransform;
            bool createdScrollbar = false;
            if (scrollbarRect == null)
            {
                GameObject scrollbarObject = CreateChild(scrollRoot.gameObject, ScrollbarName, typeof(Image), typeof(Scrollbar));
                scrollbarRect = scrollbarObject.transform as RectTransform;
                createdScrollbar = true;
            }

            if (createdScrollbar)
            {
                SetAnchored(scrollbarRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(34f, -112f), new Vector2(-34f, 0f));
            }

            if (createdScrollbar || useRuntimeSafeLayout || !Application.isPlaying)
            {
                scrollbarRect.SetAsLastSibling();
            }

            Image trackImage = scrollbarRect.GetComponent<Image>() ?? scrollbarRect.gameObject.AddComponent<Image>();
            trackImage.sprite = PremiumUISprites.LevelScrollbarTrack;
            trackImage.type = trackImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            trackImage.color = Color.white;
            trackImage.raycastTarget = true;

            RectTransform slidingArea = scrollbarRect.Find(ScrollbarSlidingAreaName) as RectTransform;
            bool createdSlidingArea = false;
            if (slidingArea == null)
            {
                slidingArea = CreateRect(ScrollbarSlidingAreaName, scrollbarRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                createdSlidingArea = true;
            }

            if (createdSlidingArea)
            {
                StretchToFill(slidingArea, 5f, 5f, 14f, 14f);
            }

            RectTransform handleRect = slidingArea.Find(ScrollbarHandleName) as RectTransform;
            bool createdHandle = false;
            if (handleRect == null)
            {
                GameObject handleObject = CreateChild(slidingArea.gameObject, ScrollbarHandleName, typeof(Image));
                handleRect = handleObject.transform as RectTransform;
                createdHandle = true;
            }

            if (createdHandle)
            {
                StretchToFill(handleRect, -2f, -2f, 0f, 0f);
            }

            Image handleImage = handleRect.GetComponent<Image>() ?? handleRect.gameObject.AddComponent<Image>();
            handleImage.sprite = PremiumUISprites.LevelScrollbarHandle;
            handleImage.type = handleImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;

            Scrollbar scrollbar = scrollbarRect.GetComponent<Scrollbar>() ?? scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = scrollbar.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.58f, 1f);
            colors.pressedColor = new Color(0.9f, 0.72f, 0.2f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            scrollbar.colors = colors;

            Navigation navigation = scrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            scrollbar.navigation = navigation;
            return scrollbar;
        }

        private void UpdateScrollbarVisibility()
        {
            if (levelsScrollbar == null || levelsScrollRect == null || levelsScrollRect.viewport == null || contentRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            float contentHeight = CalculateLevelListContentHeight();
            int rowCount = CountVisibleLevelRows();
            int visibleRowCapacity = CalculateVisibleRowCapacity();
            bool shouldShow = rowCount > visibleRowCapacity &&
                              contentHeight > levelsScrollRect.viewport.rect.height + 1f;
            if (useRuntimeSafeLayout)
            {
                ApplyViewportLayout(levelsScrollRect.viewport, shouldShow);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            SetOverflowDecorationsVisible(shouldShow);
            levelsScrollRect.vertical = shouldShow;
            levelsScrollRect.verticalScrollbarVisibility = shouldShow
                ? ScrollRect.ScrollbarVisibility.Permanent
                : ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private static void ApplyViewportLayout(RectTransform viewport, bool reserveScrollbar)
        {
            if (viewport == null)
            {
                return;
            }

            float rightInset = ScrollViewportHorizontalInset + (reserveScrollbar ? ScrollbarReservedWidth : 0f);
            StretchToFill(viewport, ScrollViewportHorizontalInset, rightInset, ScrollViewportTopInset, ScrollViewportBottomInset);
        }

        private float CalculateLevelListContentHeight()
        {
            if (contentRoot == null)
            {
                return 0f;
            }

            int visibleChildren = 0;
            float height = LevelListVerticalPadding * 2f;
            for (int index = 0; index < contentRoot.childCount; index++)
            {
                RectTransform child = contentRoot.GetChild(index) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                float childHeight = layoutElement != null
                    ? Mathf.Max(layoutElement.preferredHeight, layoutElement.minHeight)
                    : Mathf.Max(child.rect.height, LevelButtonHeight);

                if (visibleChildren > 0)
                {
                    height += LevelButtonSpacing;
                }

                height += childHeight;
                visibleChildren++;
            }

            return height;
        }

        private int CountVisibleLevelRows()
        {
            if (contentRoot == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < contentRoot.childCount; index++)
            {
                Transform child = contentRoot.GetChild(index);
                if (child != null && child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private int CalculateVisibleRowCapacity()
        {
            if (levelsScrollRect == null || levelsScrollRect.viewport == null)
            {
                return 1;
            }

            float viewportHeight = levelsScrollRect.viewport.rect.height;
            if (viewportHeight <= 1f)
            {
                viewportHeight = ResolveRectSize(levelsScrollRect.viewport, ScrollSize).y;
            }

            float usableHeight = Mathf.Max(0f, viewportHeight - (LevelListVerticalPadding * 2f));
            int capacity = Mathf.FloorToInt((usableHeight + LevelButtonSpacing) / (LevelButtonHeight + LevelButtonSpacing));
            return Mathf.Max(1, capacity);
        }

        private void SetOverflowDecorationsVisible(bool visible)
        {
            Transform scrollRoot = levelsScrollRect != null ? levelsScrollRect.transform : null;
            SetOnlyTargetChildActive(scrollRoot, ScrollbarName, levelsScrollbar != null ? levelsScrollbar.transform : null, visible);
            SetOnlyTargetChildActive(scrollRoot, FadeTopName, null, visible);
            SetOnlyTargetChildActive(scrollRoot, FadeBottomName, null, visible);
        }

        private static void SetOnlyTargetChildActive(Transform parent, string childName, Transform target, bool active)
        {
            if (parent == null)
            {
                return;
            }

            bool activatedOne = false;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child == null || child.name != childName)
                {
                    continue;
                }

                bool isTarget = target == null
                    ? !activatedOne
                    : child == target;
                child.gameObject.SetActive(active && isTarget);
                activatedOne |= isTarget;
            }
        }

        private void SelectLevel(int level)
        {
            if (isBusy || progressionManager == null || !progressionManager.SelectLevel(level))
            {
                return;
            }

            isBusy = true;
            RebuildLevelButtons();
            PlayAnimation(false, () =>
            {
                gameObject.SetActive(false);
                onLevelSelected?.Invoke(level);
            });
        }

        private void PlayAnimation(bool show, Action completed)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(Animate(show, completed));
        }

        private System.Collections.IEnumerator Animate(bool show, Action completed)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (panel == null)
            {
                panel = transform.Find("Panel") as RectTransform;
            }

            CaptureShownPanelScale();
            RestoreAuthoredPlayModeLayout();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float startAlpha = show ? 0f : canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / FadeDuration);
                float eased = show ? 1f - Mathf.Pow(1f - progress, 2f) : progress * progress;

                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);

                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            canvasGroup.blocksRaycasts = show;
            canvasGroup.interactable = show;
            if (panel != null)
            {
                panel.localScale = shownPanelScale;
            }

            RestoreAuthoredPlayModeLayout();
            animationRoutine = null;
            completed?.Invoke();
        }

        private void ApplyHiddenVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (panel != null)
            {
                CaptureShownPanelScale();
                panel.localScale = shownPanelScale;
            }

            RestoreAuthoredPlayModeLayout();
        }

        private void ApplyInitialVisuals()
        {
            if (Application.isPlaying)
            {
                ApplyHiddenVisuals();
                return;
            }

            ApplyEditorPreviewVisuals();
        }

        private void ApplyEditorPreviewVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (panel != null)
            {
                CaptureShownPanelScale();
            }
        }

        private void CaptureShownPanelScale()
        {
            if (panel == null)
            {
                return;
            }

            if (!hasCapturedShownPanelScale || !Application.isPlaying || canvasGroup == null || canvasGroup.alpha > 0.01f)
            {
                shownPanelScale = panel.localScale;
                hasCapturedShownPanelScale = true;
            }
        }

        private void CaptureAuthoredPlayModeLayout()
        {
            if (!Application.isPlaying || useRuntimeSafeLayout || hasCapturedAuthoredPlayModeLayout || panel == null)
            {
                return;
            }

            authoredPlayModeLayouts.Clear();
            foreach (RectTransform rectTransform in GetComponentsInChildren<RectTransform>(true))
            {
                if (rectTransform == null || rectTransform == contentRoot || IsInsideContentRoot(rectTransform))
                {
                    continue;
                }

                authoredPlayModeLayouts[rectTransform] = new AuthoredRectLayout(rectTransform);
            }

            hasCapturedAuthoredPlayModeLayout = true;
        }

        private void RestoreAuthoredPlayModeLayout()
        {
            if (!Application.isPlaying || useRuntimeSafeLayout || !hasCapturedAuthoredPlayModeLayout)
            {
                return;
            }

            foreach (KeyValuePair<RectTransform, AuthoredRectLayout> pair in authoredPlayModeLayouts)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                pair.Value.Apply(pair.Key);
            }

            KeepGeneratedRowsBelowAuthoredHeader();
        }

        private bool IsInsideContentRoot(RectTransform rectTransform)
        {
            return contentRoot != null &&
                   rectTransform != contentRoot &&
                   rectTransform.transform.IsChildOf(contentRoot);
        }

        private void KeepGeneratedRowsBelowAuthoredHeader()
        {
            if (!Application.isPlaying ||
                useRuntimeSafeLayout ||
                panel == null ||
                contentRoot == null ||
                contentRoot.childCount == 0 ||
                levelsScrollRect == null)
            {
                return;
            }

            VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

            RectOffset basePadding = layout.padding;
            int baseTopPadding = Mathf.Min(basePadding.top, LevelListVerticalPadding);
            layout.padding = new RectOffset(basePadding.left, basePadding.right, baseTopPadding, basePadding.bottom);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            RectTransform firstRow = null;
            for (int index = 0; index < contentRoot.childCount; index++)
            {
                RectTransform child = contentRoot.GetChild(index) as RectTransform;
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    firstRow = child;
                    break;
                }
            }

            if (firstRow == null)
            {
                return;
            }

            RectTransform header = statsText != null
                ? statsText.rectTransform
                : FindChildRecursive<RectTransform>(panel, "Title");
            if (header == null)
            {
                return;
            }

            Bounds headerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, header);
            Bounds rowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, firstRow);
            float highestAllowedRowTop = headerBounds.min.y - GeneratedListHeaderGap;
            if (rowBounds.max.y <= highestAllowedRowTop)
            {
                return;
            }

            int additionalTopPadding = Mathf.Clamp(
                Mathf.CeilToInt(rowBounds.max.y - highestAllowedRowTop),
                0,
                MaxGeneratedListAdditionalTopPadding);
            RectOffset padding = layout.padding;
            int targetTopPadding = baseTopPadding + additionalTopPadding;
            layout.padding = new RectOffset(padding.left, padding.right, targetTopPadding, padding.bottom);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
                text.raycastTarget = false;
            }
        }

        private TMP_FontAsset ResolveFont()
        {
            TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            if (sceneText != null && sceneText.font != null)
            {
                return sceneText.font;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            Font font = Resources.Load<Font>("Fonts/Roboto-Bold");
            return font != null ? TMP_FontAsset.CreateFontAsset(font) : null;
        }

        private Button CreateImageButton(Transform parent, string objectName, Sprite sprite, Color color)
        {
            GameObject buttonObject = CreateChild(parent.gameObject, objectName, typeof(Image), typeof(Button));
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.62f, 1f);
            colors.pressedColor = new Color(0.96f, 0.72f, 0.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            buttonObject.AddComponent<UIJuicyButton>();
            AddShadow(buttonObject, new Color(0.02f, 0.01f, 0f, 0.32f), new Vector2(0f, -5f));
            return button;
        }

        private Button CreateCloseButton(RectTransform parent)
        {
            Button button = PopupShell.EnsureIconCloseButton(parent, "CloseButton");
            SetAnchored(
                button.transform as RectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                CloseSize,
                CloseOffset);

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastPadding = new Vector4(-8f, -8f, -8f, -8f);
            }

            button.transform.SetAsLastSibling();
            return button;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateChild(parent.gameObject, objectName, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = fontAsset != null ? fontAsset : ResolveFont();
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(14f, fontSize * 0.58f);
            label.enableWordWrapping = false;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
            return label;
        }

        private static GameObject CreateChild(GameObject parent, string objectName, params Type[] components)
        {
            GameObject child = new(objectName, components);
            child.layer = parent.layer;
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject child = CreateChild(parent.gameObject, objectName, typeof(RectTransform));
            RectTransform rectTransform = child.transform as RectTransform;
            SetAnchored(rectTransform, anchorMin, anchorMax, pivot, size, anchoredPosition);
            return rectTransform;
        }

        private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void StretchToFill(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddTextOutline(TMP_Text text, Color color, Vector2 distance)
        {
            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Sprite GetPauseWindowSprite()
        {
            return PremiumUISprites.PauseWindowCardMesh != null
                ? PremiumUISprites.PauseWindowCardMesh
                : PremiumUISprites.EmeraldPanel;
        }

        private static Sprite GetLevelButtonSprite()
        {
            return PremiumUISprites.GameplayActionButton != null
                ? PremiumUISprites.GameplayActionButton
                : PremiumUISprites.PrimaryButton;
        }

        private static Button FindButton(Transform root, string targetName)
        {
            return FindChildRecursive<Button>(root, targetName);
        }

        private static T FindChildRecursive<T>(Transform root, string targetName) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.name == targetName)
                {
                    return component;
                }
            }

            return null;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private readonly struct AuthoredRectLayout
        {
            private readonly Vector2 anchorMin;
            private readonly Vector2 anchorMax;
            private readonly Vector2 pivot;
            private readonly Vector2 anchoredPosition;
            private readonly Vector2 sizeDelta;
            private readonly Vector3 localScale;
            private readonly Quaternion localRotation;

            public AuthoredRectLayout(RectTransform rectTransform)
            {
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                pivot = rectTransform.pivot;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                localScale = rectTransform.localScale;
                localRotation = rectTransform.localRotation;
            }

            public void Apply(RectTransform rectTransform)
            {
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.pivot = pivot;
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.localScale = localScale;
                rectTransform.localRotation = localRotation;
            }
        }
    }
}
