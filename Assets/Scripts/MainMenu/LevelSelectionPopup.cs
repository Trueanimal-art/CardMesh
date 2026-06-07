using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class LevelSelectionPopup : MonoBehaviour
    {
        private const string PopupName = "LevelSelectionPopup";
        private const float FadeDuration = 0.18f;
        private const int MinDisplayedLevels = 30;
        private const int ExtraLockedLevels = 12;

        private readonly Color textGold = new(1f, 0.83f, 0.34f, 1f);
        private readonly Color textDark = new(0.16f, 0.09f, 0.02f, 1f);
        private readonly Color lockedTint = new(0.42f, 0.44f, 0.36f, 0.62f);

        private CanvasGroup canvasGroup;
        private RectTransform panel;
        private RectTransform contentRoot;
        private TextMeshProUGUI statsText;
        private TMP_FontAsset fontAsset;
        private LevelProgressionManager progressionManager;
        private Action<int> onLevelSelected;
        private Coroutine animationRoutine;
        private bool isBusy;

        public static LevelSelectionPopup GetOrCreate()
        {
            LevelSelectionPopup existing = FindFirstObjectByType<LevelSelectionPopup>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            EnsureEventSystem();

            GameObject root = new(PopupName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            StretchToFill(root.transform as RectTransform, 0f, 0f, 0f, 0f);

            LevelSelectionPopup popup = root.AddComponent<LevelSelectionPopup>();
            popup.BuildUi();
            root.SetActive(false);
            return popup;
        }

        public void Show(LevelProgressionManager targetProgressionManager, Action<int> selectedCallback)
        {
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

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (panel == null)
            {
                panel = transform.Find("Panel") as RectTransform;
            }

            ApplyHiddenVisuals();
        }

        private void BuildUi()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            fontAsset = ResolveFont();

            GameObject dim = CreateChild(gameObject, "Dim", typeof(Image), typeof(Button));
            StretchToFill(dim.transform as RectTransform, 0f, 0f, 0f, 0f);
            Image dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0.03f, 0.01f, 0.72f);
            Button dimButton = dim.GetComponent<Button>();
            dimButton.onClick.AddListener(Hide);

            GameObject panelObject = CreateChild(gameObject, "Panel", typeof(Image));
            panel = panelObject.transform as RectTransform;
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 780f), new Vector2(0f, 120f));

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = PremiumUISprites.EmeraldPanel;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.93f, 1f, 0.86f, 1f);
            AddShadow(panelObject, new Color(0f, 0.03f, 0.01f, 0.58f), new Vector2(0f, -12f));
            AddOutline(panelObject, new Color(1f, 0.78f, 0.22f, 0.86f), new Vector2(3f, -3f));

            TextMeshProUGUI title = CreateText(panel, "Title", "\u0412\u044b\u0431\u043e\u0440 \u0443\u0440\u043e\u0432\u043d\u044f", 54f, textGold, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 78f), new Vector2(0f, -42f));
            title.fontStyle = FontStyles.Bold;
            AddTextOutline(title, new Color(0.02f, 0.08f, 0.03f, 0.8f), new Vector2(1.6f, -1.6f));

            statsText = CreateText(panel, "Stats", string.Empty, 30f, new Color(1f, 0.93f, 0.64f, 1f), TextAlignmentOptions.Center);
            SetAnchored(statsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 46f), new Vector2(0f, -108f));
            AddTextOutline(statsText, new Color(0f, 0.06f, 0.02f, 0.58f), new Vector2(1f, -1f));

            Button closeButton = CreateImageButton(panel, "CloseButton", PremiumUISprites.IconCircle, new Color(1f, 0.82f, 0.18f, 1f));
            SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(72f, 72f), new Vector2(-56f, -54f));
            closeButton.onClick.AddListener(Hide);
            TextMeshProUGUI closeLabel = CreateText(closeButton.transform as RectTransform, "Label", "X", 30f, textDark, TextAlignmentOptions.Center);
            StretchToFill(closeLabel.rectTransform, 0f, 0f, 0f, 2f);
            closeLabel.fontStyle = FontStyles.Bold;

            GameObject scrollRoot = CreateChild(panelObject, "LevelsScroll", typeof(Image), typeof(ScrollRect));
            RectTransform scrollRectTransform = scrollRoot.transform as RectTransform;
            SetAnchored(scrollRectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(690f, -210f), new Vector2(0f, -26f));
            Image scrollImage = scrollRoot.GetComponent<Image>();
            scrollImage.sprite = PremiumUISprites.CategorySlot;
            scrollImage.type = Image.Type.Sliced;
            scrollImage.color = new Color(0.04f, 0.22f, 0.1f, 0.7f);

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
            layout.padding = new RectOffset(16, 16, 16, 16);
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

            if (statsText != null)
            {
                statsText.text = $"\u041e\u0442\u043a\u0440\u044b\u0442\u043e \u0443\u0440\u043e\u0432\u043d\u0435\u0439: {progressionManager.MaxUnlockedLevel}";
            }

            int displayedLevels = Mathf.Max(MinDisplayedLevels, progressionManager.MaxUnlockedLevel + ExtraLockedLevels);
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
            bool isCompleted = level <= progressionManager.MaxCompletedLevel;
            bool isSelected = level == progressionManager.LastSelectedLevel && isCompleted;

            Button button = CreateImageButton(contentRoot, $"Level_{level}", isSelected ? PremiumUISprites.PrimaryButton : PremiumUISprites.SecondaryButton, Color.white);
            button.interactable = isUnlocked;

            RectTransform rectTransform = button.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(0f, 76f);

            LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 76f;
            layoutElement.preferredHeight = 76f;

            Image image = button.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = isCompleted
                ? Color.white
                : isUnlocked
                    ? new Color(0.2f, 0.47f, 0.26f, 0.58f)
                    : lockedTint;

            CanvasGroup rowGroup = button.gameObject.AddComponent<CanvasGroup>();
            rowGroup.alpha = isCompleted ? 1f : isUnlocked ? 0.72f : 0.42f;

            string labelText = isCompleted ? $"\u0423\u0440\u043e\u0432\u0435\u043d\u044c {level}" : "...";
            Color labelColor = isCompleted
                ? textDark
                : new Color(0.86f, 0.8f, 0.62f, isUnlocked ? 0.82f : 0.56f);
            TextMeshProUGUI label = CreateText(rectTransform, "Label", labelText, 32f, labelColor, TextAlignmentOptions.Center);
            StretchToFill(label.rectTransform, 28f, 28f, 0f, 2f);
            label.fontStyle = FontStyles.Bold;
            AddTextOutline(label, isCompleted ? new Color(1f, 0.88f, 0.34f, 0.34f) : new Color(0f, 0f, 0f, 0.26f), new Vector2(1f, -1f));

            if (isSelected)
            {
                AddOutline(button.gameObject, new Color(1f, 0.97f, 0.58f, 1f), new Vector2(3f, -3f));
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
            image.type = Image.Type.Sliced;
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.76f, 1f);
            colors.pressedColor = new Color(0.9f, 0.68f, 0.18f, 1f);
            colors.disabledColor = new Color(0.48f, 0.48f, 0.42f, 0.65f);
            button.colors = colors;

            buttonObject.AddComponent<UIJuicyButton>();
            AddShadow(buttonObject, new Color(0.02f, 0.01f, 0f, 0.32f), new Vector2(0f, -5f));
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
            return label;
        }

        private static GameObject CreateChild(GameObject parent, string objectName, params Type[] components)
        {
            GameObject child = new(objectName, components);
            child.transform.SetParent(parent.transform, false);
            return child;
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
