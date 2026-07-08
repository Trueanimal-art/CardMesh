using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class RuntimeDevTools : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private const string RootName = "RuntimeDevTools";
        private const int DefaultMovesGrant = 10;
        private const int DefaultCoinsGrant = 100;
        private const int MinDevLevelNumber = 1;
        private const int MaxDevLevelNumber = 1000;
        private const int ImageCardsPerPage = 30;
        private const int ImageGridColumns = 6;
        private const float HeaderHeight = 54f;
        private const float ButtonHeight = 58f;
        private const float LevelOptionButtonHeight = 48f;
        private static readonly Vector2 ExpandedSize = new(330f, 668f);
        private static readonly Vector2 ExpandedLevelMenuSize = new(330f, 828f);
        private static readonly Vector2 ExpandedLevelNumberMenuSize = new(330f, 956f);
        private static readonly Vector2 CollapsedSize = new(240f, HeaderHeight);
        private static readonly Color PanelColor = new(0.03f, 0.05f, 0.07f, 0.94f);
        private static readonly Color HeaderColor = new(0.11f, 0.18f, 0.24f, 0.98f);
        private static readonly Color ButtonColor = new(0.2f, 0.32f, 0.4f, 0.98f);
        private static readonly Color ButtonHighlightedColor = new(0.32f, 0.5f, 0.62f, 1f);
        private static readonly Color ButtonDisabledColor = new(0.12f, 0.13f, 0.14f, 0.72f);
        private static readonly Color TextColor = new(0.92f, 0.96f, 1f, 1f);

        private static RuntimeDevTools instance;

        private RectTransform panelRoot;
        private RectTransform bodyRoot;
        private TextMeshProUGUI collapseLabel;
        private TextMeshProUGUI statusLabel;
        private Button hintButton;
        private Button undoButton;
        private Button addMovesButton;
        private Button addCoinsButton;
        private Button completeLevelButton;
        private Button levelMenuButton;
        private Button loadThreeColumnsButton;
        private Button loadFourColumnsButton;
        private Button loadFiveColumnsButton;
        private Button levelNumberMenuButton;
        private Button loadSelectedLevelButton;
        private Button imageCardsButton;
        private RectTransform levelMenuRoot;
        private RectTransform levelNumberMenuRoot;
        private RectTransform imageCardsViewerRoot;
        private RectTransform imageCardsGridRoot;
        private TextMeshProUGUI imageCardsTitleLabel;
        private TextMeshProUGUI imageCardsPageLabel;
        private Button imageCardsPrevButton;
        private Button imageCardsNextButton;
        private TMP_InputField levelNumberInput;
        private readonly System.Collections.Generic.List<CardData> imageCardPreviewData = new();
        private bool isCollapsed;
        private bool isLevelMenuOpen;
        private bool isLevelNumberMenuOpen;
        private int imageCardsPageIndex;
        private int selectedLevelNumber = MinDevLevelNumber;
        private Vector2 dragStartPanelPosition;
        private Vector2 dragStartPointerPosition;
        private float nextRefreshTime;
        private Coroutine pendingLevelLoadRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            RuntimeDevTools existing = FindFirstObjectByType<RuntimeDevTools>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                existing.EnsureUi();
                return;
            }

            new GameObject(RootName, typeof(RectTransform)).AddComponent<RuntimeDevTools>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = RootName;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EnsureUi();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                instance = null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.35f;
            RefreshButtonState();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panelRoot == null || panelRoot.parent is not RectTransform parent)
            {
                return;
            }

            dragStartPanelPosition = panelRoot.anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                eventData.position,
                eventData.pressEventCamera,
                out dragStartPointerPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRoot == null || panelRoot.parent is not RectTransform parent)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerPosition))
            {
                return;
            }

            panelRoot.anchoredPosition = dragStartPanelPosition + (pointerPosition - dragStartPointerPosition);
        }

        private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            EnsureUi();
            RefreshButtonState();
        }

        private void EnsureUi()
        {
            EnsureEventSystem();
            EnsureCanvas();

            if (panelRoot != null)
            {
                return;
            }

            panelRoot = CreateRect("Panel", transform as RectTransform);
            panelRoot.anchorMin = new Vector2(1f, 0f);
            panelRoot.anchorMax = new Vector2(1f, 0f);
            panelRoot.pivot = new Vector2(1f, 0f);
            panelRoot.sizeDelta = ExpandedSize;
            panelRoot.anchoredPosition = new Vector2(-26f, 132f);

            Image panelImage = panelRoot.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;

            RectTransform header = CreateRect("Header", panelRoot);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -HeaderHeight);
            header.offsetMax = Vector2.zero;

            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = HeaderColor;
            headerImage.raycastTarget = true;

            TextMeshProUGUI title = CreateText(header, "Title", "DEV TOOLS", 24f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.rectTransform.anchorMin = Vector2.zero;
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(14f, 0f);
            title.rectTransform.offsetMax = new Vector2(-58f, 0f);

            Button collapseButton = CreateButton(header, "CollapseButton", "-", ToggleCollapsed);
            RectTransform collapseRect = collapseButton.transform as RectTransform;
            collapseRect.anchorMin = new Vector2(1f, 0.5f);
            collapseRect.anchorMax = new Vector2(1f, 0.5f);
            collapseRect.pivot = new Vector2(1f, 0.5f);
            collapseRect.sizeDelta = new Vector2(44f, 42f);
            collapseRect.anchoredPosition = new Vector2(-6f, 0f);
            collapseLabel = collapseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (collapseLabel != null)
            {
                collapseLabel.fontSize = 24f;
                collapseLabel.fontSizeMin = 24f;
                collapseLabel.fontSizeMax = 24f;
            }

            bodyRoot = CreateRect("Body", panelRoot);
            bodyRoot.anchorMin = Vector2.zero;
            bodyRoot.anchorMax = new Vector2(1f, 1f);
            bodyRoot.offsetMin = new Vector2(0f, 0f);
            bodyRoot.offsetMax = new Vector2(0f, -HeaderHeight);

            VerticalLayoutGroup layout = bodyRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            hintButton = CreateBodyButton("HintButton", "Подсказка", UseHint);
            undoButton = CreateBodyButton("UndoButton", "Отмена", UseUndo);
            addMovesButton = CreateBodyButton("AddMovesButton", $"+{DefaultMovesGrant} ходов", AddMoves);
            addCoinsButton = CreateBodyButton("AddCoinsButton", $"+{DefaultCoinsGrant} золота", AddCoins);
            completeLevelButton = CreateBodyButton("CompleteLevelButton", "Пройти уровень", CompleteLevel);
            levelMenuButton = CreateBodyButton("LevelMenuButton", "Выбрать раскладку", ToggleLevelMenu);
            CreateLevelMenu();
            levelNumberMenuButton = CreateBodyButton("LevelNumberMenuButton", "Выбрать уровень", ToggleLevelNumberMenu);
            CreateLevelNumberMenu();
            imageCardsButton = CreateBodyButton("ImageCardsButton", "Графические карты", ShowImageCardsViewer);

            statusLabel = CreateText(bodyRoot, "Status", "Готово", 18f, FontStyles.Normal);
            statusLabel.alignment = TextAlignmentOptions.TopLeft;
            statusLabel.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.78f);
            statusLabel.enableWordWrapping = true;
            LayoutElement statusLayout = statusLabel.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 54f;

            RefreshButtonState();
        }

        private void EnsureCanvas()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private Button CreateBodyButton(string objectName, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(bodyRoot, objectName, label, action);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = ButtonHeight;
            layout.preferredHeight = ButtonHeight;
            return button;
        }

        private void CreateLevelMenu()
        {
            levelMenuRoot = CreateRect("LevelMenu", bodyRoot);

            VerticalLayoutGroup layout = levelMenuRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            LayoutElement rootLayout = levelMenuRoot.gameObject.AddComponent<LayoutElement>();
            rootLayout.minHeight = (LevelOptionButtonHeight * 3f) + (layout.spacing * 2f);
            rootLayout.preferredHeight = rootLayout.minHeight;

            loadThreeColumnsButton = CreateLevelOptionButton("Load3ColumnsButton", "3 столбца", () => LoadLevelWithColumns(3));
            loadFourColumnsButton = CreateLevelOptionButton("Load4ColumnsButton", "4 столбца", () => LoadLevelWithColumns(4));
            loadFiveColumnsButton = CreateLevelOptionButton("Load5ColumnsButton", "5 столбцов", () => LoadLevelWithColumns(5));

            levelMenuRoot.gameObject.SetActive(false);
        }

        private Button CreateLevelOptionButton(string objectName, string label, UnityEngine.Events.UnityAction action)
        {
            return CreateMenuButton(levelMenuRoot, objectName, label, action, LevelOptionButtonHeight);
        }

        private void CreateLevelNumberMenu()
        {
            levelNumberMenuRoot = CreateRect("LevelNumberMenu", bodyRoot);

            VerticalLayoutGroup layout = levelNumberMenuRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            LayoutElement rootLayout = levelNumberMenuRoot.gameObject.AddComponent<LayoutElement>();
            rootLayout.minHeight = (LevelOptionButtonHeight * 5f) + (layout.spacing * 4f);
            rootLayout.preferredHeight = rootLayout.minHeight;

            levelNumberInput = CreateLevelNumberInput();

            RectTransform subtractRow = CreateLevelStepRow("SubtractRow");
            CreateLevelStepButton(subtractRow, "-100", -100);
            CreateLevelStepButton(subtractRow, "-10", -10);
            CreateLevelStepButton(subtractRow, "-1", -1);

            RectTransform addRow = CreateLevelStepRow("AddRow");
            CreateLevelStepButton(addRow, "+1", 1);
            CreateLevelStepButton(addRow, "+10", 10);
            CreateLevelStepButton(addRow, "+100", 100);

            loadSelectedLevelButton = CreateMenuButton(levelNumberMenuRoot, "LoadSelectedLevelButton", "Загрузить", LoadSelectedLevel, LevelOptionButtonHeight);
            UpdateSelectedLevelNumber(selectedLevelNumber);
            levelNumberMenuRoot.gameObject.SetActive(false);
        }

        private TMP_InputField CreateLevelNumberInput()
        {
            RectTransform rect = CreateRect("LevelNumberInput", levelNumberMenuRoot);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.13f, 0.17f, 0.98f);
            image.raycastTarget = true;

            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 4;
            input.lineType = TMP_InputField.LineType.SingleLine;

            TextMeshProUGUI text = CreateText(rect, "Text", selectedLevelNumber.ToString(), 25f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(12f, 0f);
            text.rectTransform.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI placeholder = CreateText(rect, "Placeholder", "1-1000", 22f, FontStyles.Normal);
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.42f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(12f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-12f, 0f);

            input.textViewport = rect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.SetTextWithoutNotify(selectedLevelNumber.ToString());
            input.onEndEdit.AddListener(ApplyLevelNumberInput);

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = LevelOptionButtonHeight;
            layout.preferredHeight = LevelOptionButtonHeight;
            return input;
        }

        private RectTransform CreateLevelStepRow(string objectName)
        {
            RectTransform row = CreateRect(objectName, levelNumberMenuRoot);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = LevelOptionButtonHeight;
            rowLayout.preferredHeight = LevelOptionButtonHeight;
            return row;
        }

        private void CreateLevelStepButton(RectTransform parent, string label, int delta)
        {
            Button button = CreateButton(parent, $"LevelStep{label}", label, () => AdjustSelectedLevelNumber(delta));
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = LevelOptionButtonHeight;
            layout.preferredHeight = LevelOptionButtonHeight;
            layout.flexibleWidth = 1f;
        }

        private static Button CreateMenuButton(RectTransform parent, string objectName, string label, UnityEngine.Events.UnityAction action, float height)
        {
            Button button = CreateButton(parent, objectName, label, action);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            return button;
        }

        private void EnsureImageCardsViewer()
        {
            if (imageCardsViewerRoot != null)
            {
                return;
            }

            imageCardsViewerRoot = CreateRect("ImageCardsViewer", transform as RectTransform);
            imageCardsViewerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            imageCardsViewerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            imageCardsViewerRoot.pivot = new Vector2(0.5f, 0.5f);
            imageCardsViewerRoot.sizeDelta = new Vector2(940f, 1080f);
            imageCardsViewerRoot.anchoredPosition = Vector2.zero;

            Image backdrop = imageCardsViewerRoot.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.035f, 0.045f, 0.97f);
            backdrop.raycastTarget = true;

            RectTransform header = CreateRect("Header", imageCardsViewerRoot);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -76f);
            header.offsetMax = Vector2.zero;

            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = HeaderColor;
            headerImage.raycastTarget = false;

            imageCardsTitleLabel = CreateText(header, "Title", "Графические карты", 30f, FontStyles.Bold);
            imageCardsTitleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            imageCardsTitleLabel.rectTransform.anchorMin = Vector2.zero;
            imageCardsTitleLabel.rectTransform.anchorMax = Vector2.one;
            imageCardsTitleLabel.rectTransform.offsetMin = new Vector2(22f, 0f);
            imageCardsTitleLabel.rectTransform.offsetMax = new Vector2(-160f, 0f);

            Button closeButton = CreateButton(header, "CloseButton", "X", HideImageCardsViewer);
            RectTransform closeRect = closeButton.transform as RectTransform;
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(72f, 56f);
            closeRect.anchoredPosition = new Vector2(-12f, 0f);

            imageCardsGridRoot = CreateRect("Grid", imageCardsViewerRoot);
            imageCardsGridRoot.anchorMin = new Vector2(0f, 0f);
            imageCardsGridRoot.anchorMax = new Vector2(1f, 1f);
            imageCardsGridRoot.offsetMin = new Vector2(20f, 100f);
            imageCardsGridRoot.offsetMax = new Vector2(-20f, -98f);

            GridLayoutGroup grid = imageCardsGridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ImageGridColumns;
            grid.cellSize = new Vector2(132f, 166f);
            grid.spacing = new Vector2(14f, 14f);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;

            RectTransform footer = CreateRect("Footer", imageCardsViewerRoot);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.offsetMin = Vector2.zero;
            footer.offsetMax = new Vector2(0f, 92f);

            Image footerImage = footer.gameObject.AddComponent<Image>();
            footerImage.color = new Color(0.04f, 0.07f, 0.09f, 0.96f);
            footerImage.raycastTarget = false;

            imageCardsPrevButton = CreateButton(footer, "PrevButton", "Назад", ShowPreviousImageCardsPage);
            RectTransform prevRect = imageCardsPrevButton.transform as RectTransform;
            prevRect.anchorMin = new Vector2(0f, 0.5f);
            prevRect.anchorMax = new Vector2(0f, 0.5f);
            prevRect.pivot = new Vector2(0f, 0.5f);
            prevRect.sizeDelta = new Vector2(180f, 58f);
            prevRect.anchoredPosition = new Vector2(22f, 0f);

            imageCardsNextButton = CreateButton(footer, "NextButton", "Вперед", ShowNextImageCardsPage);
            RectTransform nextRect = imageCardsNextButton.transform as RectTransform;
            nextRect.anchorMin = new Vector2(1f, 0.5f);
            nextRect.anchorMax = new Vector2(1f, 0.5f);
            nextRect.pivot = new Vector2(1f, 0.5f);
            nextRect.sizeDelta = new Vector2(180f, 58f);
            nextRect.anchoredPosition = new Vector2(-22f, 0f);

            imageCardsPageLabel = CreateText(footer, "PageLabel", "0/0", 24f, FontStyles.Bold);
            imageCardsPageLabel.alignment = TextAlignmentOptions.Center;
            imageCardsPageLabel.rectTransform.anchorMin = Vector2.zero;
            imageCardsPageLabel.rectTransform.anchorMax = Vector2.one;
            imageCardsPageLabel.rectTransform.offsetMin = new Vector2(220f, 0f);
            imageCardsPageLabel.rectTransform.offsetMax = new Vector2(-220f, 0f);

            imageCardsViewerRoot.gameObject.SetActive(false);
        }

        private void CreateImageCardPreview(CardData cardData)
        {
            RectTransform tile = CreateRect(cardData != null ? cardData.Id : "ImageCard", imageCardsGridRoot);

            Image cardBackground = tile.gameObject.AddComponent<Image>();
            cardBackground.color = new Color(0.97f, 0.96f, 0.92f, 1f);
            cardBackground.raycastTarget = false;

            Outline outline = tile.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.2f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform iconRoot = CreateRect("Icon", tile);
            iconRoot.anchorMin = new Vector2(0.06f, 0.2f);
            iconRoot.anchorMax = new Vector2(0.94f, 0.8f);
            iconRoot.offsetMin = Vector2.zero;
            iconRoot.offsetMax = Vector2.zero;

            Image icon = iconRoot.gameObject.AddComponent<Image>();
            icon.sprite = cardData != null ? cardData.Icon : null;
            icon.color = icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.18f);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.useSpriteMesh = false;
            icon.raycastTarget = false;

            TextMeshProUGUI title = CreateText(tile, "Title", cardData != null ? cardData.DisplayText : string.Empty, 19f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.22f, 0.18f, 0.14f, 1f);
            title.rectTransform.anchorMin = new Vector2(0f, 0.82f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(6f, -2f);
            title.rectTransform.offsetMax = new Vector2(-6f, -4f);

            TextMeshProUGUI id = CreateText(tile, "Id", cardData != null ? cardData.Id : string.Empty, 11f, FontStyles.Normal);
            id.alignment = TextAlignmentOptions.Center;
            id.color = new Color(0.18f, 0.16f, 0.14f, 0.72f);
            id.rectTransform.anchorMin = new Vector2(0f, 0f);
            id.rectTransform.anchorMax = new Vector2(1f, 0.17f);
            id.rectTransform.offsetMin = new Vector2(5f, 2f);
            id.rectTransform.offsetMax = new Vector2(-5f, -2f);
        }

        private static RectTransform CreateRect(string objectName, RectTransform parent)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Button CreateButton(RectTransform parent, string objectName, string label, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateRect(objectName, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHighlightedColor;
            colors.pressedColor = new Color(0.11f, 0.2f, 0.27f, 1f);
            colors.selectedColor = ButtonHighlightedColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(action);

            TextMeshProUGUI text = CreateText(rect, "Label", label, 24f, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10f, 0f);
            text.rectTransform.offsetMax = new Vector2(-10f, 0f);
            return button;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string objectName, string text, float fontSize, FontStyles style)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            RuntimeCyrillicFontProvider.Apply(label, text);
            label.text = text;
            label.fontSize = fontSize;
            label.fontSizeMin = Mathf.Max(9f, fontSize - 4f);
            label.fontSizeMax = fontSize;
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.color = TextColor;
            label.raycastTarget = false;

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }

        private void ToggleCollapsed()
        {
            isCollapsed = !isCollapsed;
            if (bodyRoot != null)
            {
                bodyRoot.gameObject.SetActive(!isCollapsed);
            }

            if (panelRoot != null)
            {
                panelRoot.sizeDelta = isCollapsed ? CollapsedSize : GetExpandedPanelSize();
            }

            if (collapseLabel != null)
            {
                collapseLabel.text = isCollapsed ? "+" : "-";
            }
        }

        private void RefreshButtonState()
        {
            bool hasGameManager = FindActiveGameManager() != null;
            SetInteractable(hintButton, hasGameManager);
            SetInteractable(undoButton, hasGameManager);
            SetInteractable(addMovesButton, hasGameManager);
            SetInteractable(addCoinsButton, true);
            SetInteractable(completeLevelButton, hasGameManager);
            SetInteractable(levelMenuButton, true);
            SetInteractable(loadThreeColumnsButton, true);
            SetInteractable(loadFourColumnsButton, true);
            SetInteractable(loadFiveColumnsButton, true);
            SetInteractable(levelNumberMenuButton, true);
            SetInteractable(loadSelectedLevelButton, true);
            SetInteractable(imageCardsButton, true);
        }

        private static void SetInteractable(Button button, bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
            }
        }

        private void UseHint()
        {
            GameManager gameManager = FindActiveGameManager();
            if (gameManager == null)
            {
                SetStatus("Game scene is not active.");
                return;
            }

            gameManager.UseFreeHintForDevTools();
            SetStatus("Подсказка запущена.");
        }

        private void UseUndo()
        {
            GameManager gameManager = FindActiveGameManager();
            if (gameManager == null)
            {
                SetStatus("Game scene is not active.");
                return;
            }

            bool restored = gameManager.TryUseUndoForDevTools();
            SetStatus(restored ? "Ход отменен." : "Нечего отменять.");
        }

        private void AddMoves()
        {
            GameManager gameManager = FindActiveGameManager();
            if (gameManager == null)
            {
                SetStatus("Game scene is not active.");
                return;
            }

            gameManager.AddMovesForDevTools(DefaultMovesGrant);
            SetStatus($"+{DefaultMovesGrant} ходов выдано.");
        }

        private void AddCoins()
        {
            GameManager gameManager = FindActiveGameManager();
            if (gameManager != null)
            {
                gameManager.AddCoinsForDevTools(DefaultCoinsGrant);
                SetStatus($"+{DefaultCoinsGrant} золота выдано.");
                return;
            }

            CoinsManager coinsManager = FindOrCreateCoinsManager();
            if (coinsManager == null)
            {
                SetStatus("CoinsManager is unavailable.");
                return;
            }

            coinsManager.ReloadFromPrefs();
            int added = coinsManager.AddCoins(DefaultCoinsGrant);
            SetStatus(added > 0 ? $"+{added} золота выдано." : "Золото не выдано.");
            Debug.Log($"[DEV] Grant: Coins +{added}, coins={coinsManager.CurrentCoins}");
        }

        private void CompleteLevel()
        {
            GameManager gameManager = FindActiveGameManager();
            if (gameManager == null)
            {
                SetStatus("Game scene is not active.");
                return;
            }

            bool completed = gameManager.CompleteLevelForDevTools();
            SetStatus(completed ? "Уровень пройден." : "Уровень уже завершается.");
        }

        private void ToggleLevelMenu()
        {
            isLevelMenuOpen = !isLevelMenuOpen;
            if (isLevelMenuOpen)
            {
                SetLevelNumberMenuOpen(false);
            }

            if (levelMenuRoot != null)
            {
                levelMenuRoot.gameObject.SetActive(isLevelMenuOpen);
            }

            if (panelRoot != null && !isCollapsed)
            {
                panelRoot.sizeDelta = GetExpandedPanelSize();
            }
        }

        private void ToggleLevelNumberMenu()
        {
            SetLevelNumberMenuOpen(!isLevelNumberMenuOpen);
        }

        private void SetLevelNumberMenuOpen(bool isOpen)
        {
            isLevelNumberMenuOpen = isOpen;
            if (isLevelNumberMenuOpen)
            {
                isLevelMenuOpen = false;
                if (levelMenuRoot != null)
                {
                    levelMenuRoot.gameObject.SetActive(false);
                }
            }

            if (levelNumberMenuRoot != null)
            {
                levelNumberMenuRoot.gameObject.SetActive(isLevelNumberMenuOpen);
            }

            if (panelRoot != null && !isCollapsed)
            {
                panelRoot.sizeDelta = GetExpandedPanelSize();
            }
        }

        private Vector2 GetExpandedPanelSize()
        {
            if (isLevelNumberMenuOpen)
            {
                return ExpandedLevelNumberMenuSize;
            }

            return isLevelMenuOpen ? ExpandedLevelMenuSize : ExpandedSize;
        }

        private void LoadLevelWithColumns(int columnsCount)
        {
            int safeColumnsCount = Mathf.Clamp(columnsCount, 3, 5);
            GameManager gameManager = FindActiveGameManager();
            if (gameManager != null)
            {
                gameManager.LoadDevLevelWithColumns(safeColumnsCount);
                SetStatus($"Загружено: {safeColumnsCount} столб.");
                return;
            }

            SetStatus($"Гружу игру: {safeColumnsCount} столб.");
            LoadGameScene();

            if (pendingLevelLoadRoutine != null)
            {
                StopCoroutine(pendingLevelLoadRoutine);
            }

            pendingLevelLoadRoutine = StartCoroutine(ApplyPendingColumnsWhenGameLoads(safeColumnsCount));
        }

        private void AdjustSelectedLevelNumber(int delta)
        {
            UpdateSelectedLevelNumber(selectedLevelNumber + delta);
        }

        private void ApplyLevelNumberInput(string value)
        {
            if (!int.TryParse(value, out int parsedLevel))
            {
                UpdateSelectedLevelNumber(selectedLevelNumber);
                return;
            }

            UpdateSelectedLevelNumber(parsedLevel);
        }

        private void UpdateSelectedLevelNumber(int value)
        {
            selectedLevelNumber = Mathf.Clamp(value, MinDevLevelNumber, MaxDevLevelNumber);
            if (levelNumberInput != null)
            {
                levelNumberInput.SetTextWithoutNotify(selectedLevelNumber.ToString());
            }
        }

        private void LoadSelectedLevel()
        {
            if (levelNumberInput != null)
            {
                ApplyLevelNumberInput(levelNumberInput.text);
            }

            LoadLevelNumber(selectedLevelNumber);
        }

        private void LoadLevelNumber(int levelNumber)
        {
            int safeLevelNumber = Mathf.Clamp(levelNumber, MinDevLevelNumber, MaxDevLevelNumber);
            GameManager gameManager = FindActiveGameManager();
            if (gameManager != null)
            {
                gameManager.LoadDevLevelNumber(safeLevelNumber);
                SetStatus($"Загружен уровень {safeLevelNumber}.");
                return;
            }

            SetStatus($"Гружу уровень {safeLevelNumber}.");
            LoadGameScene();

            if (pendingLevelLoadRoutine != null)
            {
                StopCoroutine(pendingLevelLoadRoutine);
            }

            pendingLevelLoadRoutine = StartCoroutine(ApplyPendingLevelWhenGameLoads(safeLevelNumber));
        }

        private void ShowImageCardsViewer()
        {
            EnsureImageCardsViewer();
            imageCardPreviewData.Clear();

            CardDatabase cardDatabase = FindFirstObjectByType<CardDatabase>(FindObjectsInactive.Include);
            if (cardDatabase == null)
            {
                cardDatabase = gameObject.GetComponent<CardDatabase>();
                if (cardDatabase == null)
                {
                    cardDatabase = gameObject.AddComponent<CardDatabase>();
                }
            }

            imageCardPreviewData.AddRange(cardDatabase.CreateAllImageCardInstances());
            imageCardsPageIndex = 0;
            imageCardsViewerRoot.gameObject.SetActive(true);
            RenderImageCardsPage();
            SetStatus($"Графические карты: {imageCardPreviewData.Count}.");
        }

        private void HideImageCardsViewer()
        {
            if (imageCardsViewerRoot != null)
            {
                imageCardsViewerRoot.gameObject.SetActive(false);
            }
        }

        private void ShowPreviousImageCardsPage()
        {
            if (imageCardsPageIndex <= 0)
            {
                return;
            }

            imageCardsPageIndex--;
            RenderImageCardsPage();
        }

        private void ShowNextImageCardsPage()
        {
            int pageCount = GetImageCardsPageCount();
            if (imageCardsPageIndex >= pageCount - 1)
            {
                return;
            }

            imageCardsPageIndex++;
            RenderImageCardsPage();
        }

        private int GetImageCardsPageCount()
        {
            return Mathf.Max(1, Mathf.CeilToInt(imageCardPreviewData.Count / (float)ImageCardsPerPage));
        }

        private void RenderImageCardsPage()
        {
            if (imageCardsGridRoot == null)
            {
                return;
            }

            for (int index = imageCardsGridRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(imageCardsGridRoot.GetChild(index).gameObject);
            }

            int pageCount = GetImageCardsPageCount();
            imageCardsPageIndex = Mathf.Clamp(imageCardsPageIndex, 0, pageCount - 1);
            int startIndex = imageCardsPageIndex * ImageCardsPerPage;
            int endIndex = Mathf.Min(startIndex + ImageCardsPerPage, imageCardPreviewData.Count);

            for (int index = startIndex; index < endIndex; index++)
            {
                CreateImageCardPreview(imageCardPreviewData[index]);
            }

            if (imageCardsTitleLabel != null)
            {
                imageCardsTitleLabel.text = $"Графические карты ({imageCardPreviewData.Count})";
            }

            if (imageCardsPageLabel != null)
            {
                int firstShown = imageCardPreviewData.Count > 0 ? startIndex + 1 : 0;
                imageCardsPageLabel.text = $"{imageCardsPageIndex + 1}/{pageCount}  {firstShown}-{endIndex}";
            }

            SetInteractable(imageCardsPrevButton, imageCardsPageIndex > 0);
            SetInteractable(imageCardsNextButton, imageCardsPageIndex < pageCount - 1);
        }

        private IEnumerator ApplyPendingColumnsWhenGameLoads(int columnsCount)
        {
            float deadline = Time.unscaledTime + 8f;
            while (Time.unscaledTime < deadline)
            {
                GameManager gameManager = FindActiveGameManager();
                if (gameManager != null)
                {
                    gameManager.LoadDevLevelWithColumns(columnsCount);
                    SetStatus($"Загружено: {columnsCount} столб.");
                    pendingLevelLoadRoutine = null;
                    yield break;
                }

                yield return null;
            }

            SetStatus("GameManager не найден.");
            pendingLevelLoadRoutine = null;
        }

        private IEnumerator ApplyPendingLevelWhenGameLoads(int levelNumber)
        {
            float deadline = Time.unscaledTime + 8f;
            while (Time.unscaledTime < deadline)
            {
                GameManager gameManager = FindActiveGameManager();
                if (gameManager != null)
                {
                    gameManager.LoadDevLevelNumber(levelNumber);
                    SetStatus($"Загружен уровень {levelNumber}.");
                    pendingLevelLoadRoutine = null;
                    yield break;
                }

                yield return null;
            }

            SetStatus("GameManager не найден.");
            pendingLevelLoadRoutine = null;
        }

        private static void LoadGameScene()
        {
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
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private static GameManager FindActiveGameManager()
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            return gameManager != null && gameManager.isActiveAndEnabled ? gameManager : null;
        }

        private CoinsManager FindOrCreateCoinsManager()
        {
            CoinsManager coinsManager = FindFirstObjectByType<CoinsManager>(FindObjectsInactive.Include);
            if (coinsManager != null)
            {
                return coinsManager;
            }

            coinsManager = GetComponent<CoinsManager>();
            if (coinsManager == null)
            {
                coinsManager = gameObject.AddComponent<CoinsManager>();
            }

            return coinsManager;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
