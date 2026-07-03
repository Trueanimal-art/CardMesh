using System;
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
        private const int PreviewLevelRows = 6;

        private readonly Color textGold = new(1f, 0.83f, 0.34f, 1f);
        private readonly Color textDark = new(0.16f, 0.09f, 0.02f, 1f);
        private readonly Color textCream = new(1f, 0.93f, 0.64f, 1f);

        [Header("Runtime bindings")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button dimButton;
        [SerializeField] private Button closeButton;

        private TMP_FontAsset fontAsset;
        private LevelProgressionManager progressionManager;
        private Action<int> onLevelSelected;
        private Coroutine animationRoutine;
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
            if (panel == null || contentRoot == null)
            {
                EnsureAuthoringHierarchy();
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
            RebuildLevelButtons();
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

            ApplyFontsToAllTexts();
            WireStaticButtons();
            ApplyHiddenVisuals();
        }

        private void Reset()
        {
            EnsureAuthoringHierarchy();
        }

        private void Awake()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireStaticButtons();
            ApplyHiddenVisuals();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BindSceneReferences();
                ApplyFontsToAllTexts();
                WireStaticButtons();
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
            dimButton ??= FindButton(transform, "Dim");
            closeButton ??= FindButton(transform, "CloseButton");

            if (contentRoot == null)
            {
                Transform content = transform.Find("Panel/LevelsScroll/Viewport/Content");
                contentRoot = content as RectTransform ?? FindChildRecursive<RectTransform>(transform, "Content");
            }

            fontAsset ??= ResolveFont();
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
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(920f, 760f), Vector2.zero);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = GetPauseWindowSprite();
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            panelImage.raycastTarget = false;
            AddShadow(panelObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

            TextMeshProUGUI title = CreateText(panel, "Title", "Выбор уровня", 54f, textGold, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-132f, 78f), new Vector2(0f, -118f));
            title.fontStyle = FontStyles.Bold;
            AddTextOutline(title, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

            statsText = CreateText(panel, "Stats", string.Empty, 30f, textCream, TextAlignmentOptions.Center);
            SetAnchored(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-150f, 48f), new Vector2(0f, -176f));
            AddTextOutline(statsText, new Color(0.12f, 0.055f, 0.012f, 0.48f), new Vector2(1f, -1f));

            closeButton = CreateCloseButton(panel);

            GameObject scrollRoot = CreateChild(panelObject, "LevelsScroll", typeof(Image), typeof(ScrollRect));
            RectTransform scrollRectTransform = scrollRoot.transform as RectTransform;
            SetAnchored(scrollRectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(680f, -276f), new Vector2(0f, -28f));
            Image scrollImage = scrollRoot.GetComponent<Image>();
            scrollImage.sprite = PremiumUISprites.CategorySlot;
            scrollImage.type = scrollImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            scrollImage.color = new Color(0.035f, 0.18f, 0.085f, 0.76f);

            GameObject viewport = CreateChild(scrollRoot, "Viewport", typeof(Image), typeof(Mask));
            RectTransform viewportRect = viewport.transform as RectTransform;
            StretchToFill(viewportRect, 18f, 18f, 18f, 18f);
            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = CreateChild(viewport, "Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentRoot = content.transform as RectTransform;
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 36f;
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

            int unlockedLevels = progressionManager.MaxUnlockedLevel;
            int displayedLevels = Mathf.Max(PreviewLevelRows, unlockedLevels);
            if (statsText != null)
            {
                statsText.text = $"Открыто уровней: {unlockedLevels}";
            }

            for (int level = 1; level <= displayedLevels; level++)
            {
                CreateLevelButton(level);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        private void CreateLevelButton(int level)
        {
            bool isUnlocked = progressionManager.CanPlayLevel(level);
            bool isCompleted = isUnlocked && level <= progressionManager.MaxCompletedLevel;
            bool isSelected = isUnlocked && level == progressionManager.LastSelectedLevel;

            Button button = CreateImageButton(contentRoot, $"Level_{level}", GetLevelButtonSprite(), Color.white);
            button.interactable = isUnlocked;

            RectTransform rectTransform = button.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(0f, 86f);

            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 86f;
            layoutElement.preferredHeight = 86f;

            Image image = button.GetComponent<Image>();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = isSelected
                ? Color.white
                : isCompleted
                    ? new Color(0.96f, 0.96f, 0.9f, 1f)
                    : isUnlocked
                        ? new Color(0.9f, 0.9f, 0.84f, 1f)
                        : new Color(0.54f, 0.56f, 0.46f, 0.72f);

            CanvasGroup rowGroup = button.gameObject.AddComponent<CanvasGroup>();
            rowGroup.alpha = isUnlocked
                ? isSelected || isCompleted ? 1f : 0.88f
                : 0.46f;

            string labelText = isUnlocked ? $"Уровень {level}" : "...";
            Color labelColor = isUnlocked ? textGold : new Color(0.86f, 0.8f, 0.62f, 0.64f);
            TextMeshProUGUI label = CreateText(rectTransform, "Label", labelText, 36f, labelColor, TextAlignmentOptions.Center);
            StretchToFill(label.rectTransform, 28f, 28f, 0f, 0f);
            label.fontStyle = FontStyles.Bold;
            AddTextOutline(label, new Color(0.12f, 0.055f, 0.012f, isUnlocked ? 0.62f : 0.32f), new Vector2(1f, -1f));

            if (isSelected)
            {
                AddOutline(button.gameObject, new Color(1f, 0.95f, 0.46f, 0.9f), new Vector2(2f, -2f));
            }

            if (isUnlocked)
            {
                int selectedLevel = level;
                button.onClick.AddListener(() => SelectLevel(selectedLevel));
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

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float startAlpha = show ? 0f : canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;
            Vector3 startScale = show ? new Vector3(0.92f, 0.92f, 1f) : panel.localScale;
            Vector3 endScale = show ? Vector3.one : new Vector3(0.92f, 0.92f, 1f);

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / FadeDuration);
                float eased = show ? 1f - Mathf.Pow(1f - progress, 2f) : progress * progress;

                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                }

                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            canvasGroup.blocksRaycasts = show;
            canvasGroup.interactable = show;
            if (panel != null)
            {
                panel.localScale = endScale;
            }

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
                panel.localScale = new Vector3(0.92f, 0.92f, 1f);
            }
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
            RectTransform closeRoot = CreateRect("CloseButton", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), new Vector2(-78f, -72f));
            Image image = closeRoot.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = true;

            Button button = closeRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateText(closeRoot, "Label", "X", 34f, textGold, TextAlignmentOptions.Center);
            StretchToFill(label.rectTransform, 0f, 0f, 0f, 0f);
            label.fontStyle = FontStyles.Bold;
            AddTextOutline(label, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

            button.gameObject.AddComponent<UIJuicyButton>();
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
    }
}
