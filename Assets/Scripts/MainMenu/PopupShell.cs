using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PopupShell : MonoBehaviour
    {
        public enum SizePreset
        {
            Compact,
            Wide,
            Store
        }

        private static readonly Color BackdropColor = new(0.005f, 0.035f, 0.022f, 0.72f);
        private static readonly Color FrameShadowColor = new(0.02f, 0.01f, 0f, 0.34f);
        private static readonly Color InnerPanelColor = new(0.02f, 0.13f, 0.07f, 0.52f);
        private static readonly Color HeaderGold = new(1f, 0.82f, 0.28f, 1f);
        private static readonly Color HeaderCream = new(1f, 0.93f, 0.68f, 0.9f);
        private static readonly Color TextOutlineColor = new(0.1f, 0.045f, 0.012f, 0.54f);

        [Header("Configuration")]
        [SerializeField] private SizePreset sizePreset = SizePreset.Compact;
        [SerializeField] private string title = "Настройки";
        [SerializeField] private string subtitle;
        [SerializeField] private bool showIconMedallion;
        [SerializeField] private bool showInnerBackground = true;
        [SerializeField] private Vector2 titlePositionOffset;
        [SerializeField] private Vector2 closePositionOffset;
        [SerializeField] private Sprite iconSprite;

        [Header("Shell bindings")]
        [SerializeField] private RectTransform backdrop;
        [SerializeField] private Button backdropButton;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform innerBackground;
        [SerializeField] private RectTransform headerRoot;
        [SerializeField] private RectTransform iconMedallion;
        [SerializeField] private Image iconMedallionImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform contentRoot;

        private TMP_FontAsset fontAsset;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        public SizePreset Preset => sizePreset;
        public RectTransform Backdrop => backdrop;
        public Button BackdropButton => backdropButton;
        public RectTransform PanelRoot => panelRoot;
        public RectTransform Frame => frame;
        public RectTransform HeaderRoot => headerRoot;
        public RectTransform IconMedallion => iconMedallion;
        public TextMeshProUGUI TitleText => titleText;
        public TextMeshProUGUI SubtitleText => subtitleText;
        public Button CloseButton => closeButton;
        public RectTransform ContentRoot => contentRoot;

        public bool IsReady =>
            backdrop != null &&
            panelRoot != null &&
            frame != null &&
            headerRoot != null &&
            titleText != null &&
            closeButton != null &&
            contentRoot != null;

        public void Configure(
            SizePreset preset,
            string titleValue,
            string subtitleValue = null,
            Sprite icon = null,
            bool useIconMedallion = false,
            bool useInnerBackground = true,
            Vector2? customTitlePositionOffset = null,
            Vector2? customClosePositionOffset = null)
        {
            sizePreset = preset;
            title = titleValue ?? string.Empty;
            subtitle = subtitleValue ?? string.Empty;
            iconSprite = icon;
            showIconMedallion = useIconMedallion || iconSprite != null;
            showInnerBackground = useInnerBackground;
            titlePositionOffset = customTitlePositionOffset ?? Vector2.zero;
            closePositionOffset = customClosePositionOffset ?? Vector2.zero;
            EnsureHierarchy();
        }

        public void EnsureHierarchy()
        {
            fontAsset ??= ResolveFont();
            RectTransform root = transform as RectTransform;
            if (root != null)
            {
                StretchToFill(root, 0f, 0f, 0f, 0f);
            }

            EnsureBackdrop();
            EnsurePanel();
            EnsureFrame();
            EnsureInnerBackground();
            EnsureHeader();
            EnsureCloseButton();
            EnsureContentRoot();
            ApplyLayout();
            ApplyText();
            OrderShellChildren();
        }

        private void Reset()
        {
            EnsureHierarchy();
        }

        private void Awake()
        {
            EnsureHierarchy();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EnsureHierarchy();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || editorRefreshQueued)
            {
                return;
            }

            editorRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                editorRefreshQueued = false;
                if (!Application.isPlaying)
                {
                    EnsureHierarchy();
                }
            };
        }
#endif

        private void EnsureBackdrop()
        {
            backdrop = FindDirectRect(transform, "Backdrop") ?? FindDirectRect(transform, "Dim") ?? backdrop;
            if (backdrop == null)
            {
                backdrop = CreateRect(transform, "Backdrop");
            }

            backdrop.name = "Backdrop";
            Image image = backdrop.GetComponent<Image>() ?? backdrop.gameObject.AddComponent<Image>();
            image.color = BackdropColor;
            image.raycastTarget = true;
            backdropButton = backdrop.GetComponent<Button>() ?? backdrop.gameObject.AddComponent<Button>();
            backdropButton.targetGraphic = image;
            ColorBlock colors = backdropButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            backdropButton.colors = colors;
            StretchToFill(backdrop, 0f, 0f, 0f, 0f);
        }

        private void EnsurePanel()
        {
            panelRoot = FindDirectRect(transform, "Panel") ?? panelRoot;
            if (panelRoot == null)
            {
                panelRoot = CreateRect(transform, "Panel");
            }

            Image legacyImage = panelRoot.GetComponent<Image>();
            if (legacyImage != null)
            {
                legacyImage.color = Color.clear;
                legacyImage.raycastTarget = false;
            }
        }

        private void EnsureFrame()
        {
            frame = FindDirectRect(panelRoot, "Frame") ?? frame;
            if (frame == null)
            {
                frame = CreateRect(panelRoot, "Frame");
            }

            frameImage = frame.GetComponent<Image>() ?? frame.gameObject.AddComponent<Image>();
            frameImage.sprite = GetFrameSprite();
            frameImage.type = Image.Type.Simple;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;

            Shadow shadow = frame.GetComponent<Shadow>() ?? frame.gameObject.AddComponent<Shadow>();
            shadow.effectColor = FrameShadowColor;
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;
        }

        private void EnsureInnerBackground()
        {
            innerBackground = FindDirectRect(panelRoot, "InnerBackground") ?? innerBackground;
            if (innerBackground == null)
            {
                innerBackground = CreateRect(panelRoot, "InnerBackground");
            }

            Image image = innerBackground.GetComponent<Image>() ?? innerBackground.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = InnerPanelColor;
            image.raycastTarget = false;
            image.enabled = showInnerBackground;
        }

        private void EnsureHeader()
        {
            headerRoot = FindDirectRect(panelRoot, "HeaderRoot") ?? headerRoot;
            if (headerRoot == null)
            {
                headerRoot = CreateRect(panelRoot, "HeaderRoot");
            }

            iconMedallion = FindDirectRect(headerRoot, "IconMedallion") ?? iconMedallion;
            if (iconMedallion == null)
            {
                iconMedallion = CreateRect(headerRoot, "IconMedallion");
            }

            iconMedallionImage = iconMedallion.GetComponent<Image>() ?? iconMedallion.gameObject.AddComponent<Image>();
            iconMedallionImage.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
            iconMedallionImage.type = Image.Type.Simple;
            iconMedallionImage.color = Color.white;
            iconMedallionImage.raycastTarget = false;

            RectTransform icon = FindDirectRect(iconMedallion, "Icon");
            if (icon == null)
            {
                icon = CreateRect(iconMedallion, "Icon");
            }

            iconImage = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            titleText = FindDirectComponent<TextMeshProUGUI>(headerRoot, "Title") ?? titleText;
            if (titleText == null)
            {
                titleText = CreateText(headerRoot, "Title", 54f, HeaderGold, TextAlignmentOptions.Center);
            }

            subtitleText = FindDirectComponent<TextMeshProUGUI>(headerRoot, "Subtitle") ?? subtitleText;
            if (subtitleText == null)
            {
                subtitleText = CreateText(headerRoot, "Subtitle", 28f, HeaderCream, TextAlignmentOptions.Center);
            }
        }

        private void EnsureCloseButton()
        {
            closeButton = EnsureIconCloseButton(panelRoot, "CloseButton");
        }

        private void EnsureContentRoot()
        {
            contentRoot = FindDirectRect(panelRoot, "ContentRoot") ?? contentRoot;
            if (contentRoot == null)
            {
                contentRoot = CreateRect(panelRoot, "ContentRoot");
            }
        }

        private void ApplyLayout()
        {
            ShellLayout layout = GetLayout(sizePreset);
            SetAnchored(panelRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), layout.PanelSize, Vector2.zero);
            StretchToFill(frame, 0f, 0f, 0f, 0f);
            StretchToFill(innerBackground, layout.InnerLeft, layout.InnerRight, layout.InnerTop, layout.InnerBottom);

            SetAnchored(headerRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, layout.HeaderHeight), Vector2.zero);
            SetAnchored(iconMedallion, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), layout.IconSize, layout.IconPosition);
            StretchToFill(iconImage != null ? iconImage.rectTransform : null, layout.IconInset, layout.IconInset, layout.IconInset, layout.IconInset);
            iconMedallion.gameObject.SetActive(showIconMedallion);

            SetAnchored(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), layout.TitleSize, layout.TitlePosition + titlePositionOffset);
            SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), layout.SubtitleSize, layout.SubtitlePosition);
            subtitleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitle));

            RectTransform closeRect = closeButton.transform as RectTransform;
            SetAnchored(
                closeRect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                layout.CloseSize,
                layout.ClosePosition + closePositionOffset);
            StyleIconCloseButton(closeButton);

            StretchToFill(contentRoot, layout.ContentLeft, layout.ContentRight, layout.ContentTop, layout.ContentBottom);
        }

        private void ApplyText()
        {
            if (titleText != null)
            {
                titleText.text = title;
                titleText.fontStyle = FontStyles.Bold;
                RuntimeCyrillicFontProvider.Apply(titleText, titleText.text);
                EnsureTextOutline(titleText, TextOutlineColor, new Vector2(1f, -1f));
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
                subtitleText.fontStyle = FontStyles.Bold;
                RuntimeCyrillicFontProvider.Apply(subtitleText, subtitleText.text);
                EnsureTextOutline(subtitleText, new Color(0f, 0.06f, 0.02f, 0.38f), new Vector2(1f, -1f));
            }
        }

        private void OrderShellChildren()
        {
            if (backdrop != null)
            {
                backdrop.SetAsFirstSibling();
            }

            if (panelRoot != null)
            {
                panelRoot.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
            }

            if (frame != null)
            {
                frame.SetAsFirstSibling();
            }

            if (innerBackground != null)
            {
                innerBackground.SetSiblingIndex(Mathf.Min(1, panelRoot.childCount - 1));
            }

            if (headerRoot != null)
            {
                headerRoot.SetAsLastSibling();
            }

            if (contentRoot != null)
            {
                contentRoot.SetAsLastSibling();
            }

            if (closeButton != null)
            {
                closeButton.transform.SetAsLastSibling();
            }
        }

        public static Button EnsureIconCloseButton(RectTransform parent, string objectName)
        {
            RectTransform rect = FindDirectRect(parent, objectName);
            if (rect == null)
            {
                rect = CreateRect(parent, objectName);
            }

            Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.66f, 1f);
            colors.pressedColor = new Color(0.92f, 0.72f, 0.22f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.48f, 0.7f);
            button.colors = colors;

            if (rect.GetComponent<UIJuicyButton>() == null)
            {
                rect.gameObject.AddComponent<UIJuicyButton>();
            }

            StyleIconCloseButton(button);
            return button;
        }

        private static void StyleIconCloseButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            RectTransform root = button.transform as RectTransform;
            if (root == null)
            {
                return;
            }

            DisableTextLabels(root);
            EnsureCloseStroke(root, "StrokeA", 45f);
            EnsureCloseStroke(root, "StrokeB", -45f);
        }

        private static void EnsureCloseStroke(RectTransform parent, string objectName, float rotation)
        {
            RectTransform stroke = FindDirectRect(parent, objectName);
            if (stroke == null)
            {
                stroke = CreateRect(parent, objectName);
            }

            Image image = stroke.GetComponent<Image>() ?? stroke.gameObject.AddComponent<Image>();
            image.color = HeaderGold;
            image.raycastTarget = false;
            stroke.anchorMin = new Vector2(0.5f, 0.5f);
            stroke.anchorMax = new Vector2(0.5f, 0.5f);
            stroke.pivot = new Vector2(0.5f, 0.5f);
            stroke.anchoredPosition = Vector2.zero;
            stroke.sizeDelta = new Vector2(30f, 5.2f);
            stroke.localScale = Vector3.one;
            stroke.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void DisableTextLabels(RectTransform root)
        {
            TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].gameObject.SetActive(false);
            }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(parent, objectName);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ResolveFont();
            label.font = font != null ? font : label.font;
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(14f, fontSize * 0.58f);
            label.enableWordWrapping = false;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_FontAsset ResolveFont()
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

        private static Sprite GetFrameSprite()
        {
            if (PremiumUISprites.SettingsWindowCardMesh != null)
            {
                return PremiumUISprites.SettingsWindowCardMesh;
            }

            if (RoyalEventSprites.Panel != null)
            {
                return RoyalEventSprites.Panel;
            }

            return PremiumUISprites.EmeraldPanel;
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject child = new(objectName, typeof(RectTransform));
            if (parent != null)
            {
                child.layer = parent.gameObject.layer;
                child.transform.SetParent(parent, false);
            }

            return child.transform as RectTransform;
        }

        private static RectTransform FindDirectRect(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform child = parent.Find(objectName);
            return child as RectTransform;
        }

        private static T FindDirectComponent<T>(Transform parent, string objectName) where T : Component
        {
            return FindDirectRect(parent, objectName)?.GetComponent<T>();
        }

        private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
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

        private static void EnsureTextOutline(TMP_Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static ShellLayout GetLayout(SizePreset preset)
        {
            return preset switch
            {
                SizePreset.Wide => new ShellLayout(
                    new Vector2(1510f, 980f),
                    278f,
                    new Vector2(166f, 166f),
                    new Vector2(0f, -74f),
                    24f,
                    new Vector2(1040f, 72f),
                    new Vector2(0f, -150f),
                    new Vector2(1080f, 42f),
                    new Vector2(0f, -210f),
                    new Vector2(82f, 82f),
                    new Vector2(-82f, -82f),
                    92f,
                    92f,
                    280f,
                    84f,
                    68f,
                    68f,
                    206f,
                    54f),
                SizePreset.Store => new ShellLayout(
                    new Vector2(1510f, 980f),
                    326f,
                    new Vector2(178f, 178f),
                    new Vector2(0f, -68f),
                    24f,
                    new Vector2(900f, 74f),
                    new Vector2(0f, -160f),
                    new Vector2(1040f, 44f),
                    new Vector2(0f, -220f),
                    new Vector2(82f, 82f),
                    new Vector2(-82f, -82f),
                    98f,
                    98f,
                    324f,
                    72f,
                    72f,
                    72f,
                    232f,
                    44f),
                _ => new ShellLayout(
                    new Vector2(920f, 780f),
                    178f,
                    new Vector2(126f, 126f),
                    new Vector2(0f, -58f),
                    18f,
                    new Vector2(720f, 68f),
                    new Vector2(0f, -82f),
                    new Vector2(690f, 36f),
                    new Vector2(0f, -132f),
                    new Vector2(62f, 62f),
                    new Vector2(-66f, -76f),
                    78f,
                    78f,
                    220f,
                    86f,
                    56f,
                    56f,
                    174f,
                    50f)
            };
        }

        private readonly struct ShellLayout
        {
            public ShellLayout(
                Vector2 panelSize,
                float headerHeight,
                Vector2 iconSize,
                Vector2 iconPosition,
                float iconInset,
                Vector2 titleSize,
                Vector2 titlePosition,
                Vector2 subtitleSize,
                Vector2 subtitlePosition,
                Vector2 closeSize,
                Vector2 closePosition,
                float contentLeft,
                float contentRight,
                float contentTop,
                float contentBottom,
                float innerLeft,
                float innerRight,
                float innerTop,
                float innerBottom)
            {
                PanelSize = panelSize;
                HeaderHeight = headerHeight;
                IconSize = iconSize;
                IconPosition = iconPosition;
                IconInset = iconInset;
                TitleSize = titleSize;
                TitlePosition = titlePosition;
                SubtitleSize = subtitleSize;
                SubtitlePosition = subtitlePosition;
                CloseSize = closeSize;
                ClosePosition = closePosition;
                ContentLeft = contentLeft;
                ContentRight = contentRight;
                ContentTop = contentTop;
                ContentBottom = contentBottom;
                InnerLeft = innerLeft;
                InnerRight = innerRight;
                InnerTop = innerTop;
                InnerBottom = innerBottom;
            }

            public Vector2 PanelSize { get; }
            public float HeaderHeight { get; }
            public Vector2 IconSize { get; }
            public Vector2 IconPosition { get; }
            public float IconInset { get; }
            public Vector2 TitleSize { get; }
            public Vector2 TitlePosition { get; }
            public Vector2 SubtitleSize { get; }
            public Vector2 SubtitlePosition { get; }
            public Vector2 CloseSize { get; }
            public Vector2 ClosePosition { get; }
            public float ContentLeft { get; }
            public float ContentRight { get; }
            public float ContentTop { get; }
            public float ContentBottom { get; }
            public float InnerLeft { get; }
            public float InnerRight { get; }
            public float InnerTop { get; }
            public float InnerBottom { get; }
        }
    }
}
