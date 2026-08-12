using System.Reflection;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace CardMesh
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private const string RuntimeFontResourcePath = "Fonts/Roboto-Bold";
        private static readonly Color RegularColor = new(0.98f, 0.98f, 0.98f, 1f);
        private static readonly Color GoldColor = new(0.98f, 0.84f, 0.33f, 1f);
        private static readonly Color BackColor = new(0.2f, 0.62f, 0.98f, 1f);
        private static readonly Color RegularOutlineColor = new(0f, 0f, 0f, 0.12f);
        private static readonly Color GoldOutlineColor = new(0.55f, 0.38f, 0f, 0.35f);
        private static readonly Color BackOutlineColor = new(0.05f, 0.27f, 0.5f, 0.45f);
        private const float GoldHeaderSidePadding = 26f;
        private const float GoldHeaderTopPadding = 22f;
        private const float GoldHeaderHeight = 32f;
        private const float GoldProgressWidth = 46f;
        private const float RegularTitleFontSizeMax = 30f;
        private const float RegularTitleFontSizeMin = 16f;
        private const float GoldTitleFontSizeMax = 24f;
        private const float GoldTitleFontSizeMin = 11f;
        private const float RegularTitleHorizontalPadding = 22f;
        private const float GoldTitleHorizontalPadding = 24f;
        private const float ImageCardTitleHeight = 54f;
        private const float ImageCardTitleSidePadding = 12f;
        private const float PremiumFrameInset = 8f;
        private const float PremiumFrameMainThickness = 1.35f;
        private const float PremiumFrameHighlightThickness = 0.75f;
        private const float PremiumFrameShadowThickness = 2.6f;
        private const float PremiumFrameCornerSize = 18f;
        private const float PremiumFrameCornerCut = 6f;
        private const float PremiumFrameCornerThickness = 1.45f;
        private const float RoundedCardCornerRadius = 18f;
        private const float PeekEdgeThickness = 1.2f;
        private const float PeekDividerThickness = 2.2f;
        private const float StackedTopDividerThickness = 2.2f;
        private static readonly Color PremiumFrameGold = new(0.86f, 0.66f, 0.30f, 0.46f);
        private static readonly Color PremiumFrameHighlight = new(1f, 0.96f, 0.72f, 0.38f);
        private static readonly Color PremiumFrameShadow = new(0.20f, 0.12f, 0.04f, 0.18f);
        private static readonly Color PeekEdgeColor = new(0.57f, 0.50f, 0.42f, 0.36f);
        private static readonly Color PeekDividerColor = new(0.45f, 0.39f, 0.31f, 0.68f);
        private static readonly Color StackedTopDividerColor = new(0.52f, 0.45f, 0.36f, 0.62f);
        private static readonly Color PeekCardBorderFillColor = new(0.98f, 0.95f, 0.88f, 1f);
        private static readonly MethodInfo TryAddCharactersMethod = typeof(TMP_FontAsset).GetMethod(
            "TryAddCharacters",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(string) },
            null);

        private static TMP_FontAsset runtimeCyrillicFontAsset;
        private static Sprite roundedCardSprite;
        private static Sprite premiumRegularFrameSprite;
        private static Sprite premiumGoldFrameSprite;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Outline outline;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform titleRoot;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private RectTransform footerRoot;
        [SerializeField] private TextMeshProUGUI footerLabel;
        [SerializeField] private Image iconImage;
        [SerializeField] private CardVisualConfig visualConfig;
        [SerializeField] private Shadow shadow;
        [SerializeField] private Image glowImage;
        [SerializeField] private RectTransform peekMaskRoot;
        [SerializeField] private Image peekSolidFillImage;
        [SerializeField] private Image peekBackgroundImage;
        [SerializeField] private Outline peekOutline;
        [SerializeField] private RectTransform premiumFrameRoot;
        [SerializeField] private Image premiumFrameArtImage;
        [SerializeField] private Image[] premiumFrameImages;
        [SerializeField] private RectTransform faceClipRoot;
        [SerializeField] private Image faceClipMaskImage;
        [SerializeField] private Image faceImage;
        [SerializeField] private Mask faceMask;
        [SerializeField] private RectTransform peekFaceClipRoot;
        [SerializeField] private Image peekFaceClipMaskImage;
        [SerializeField] private Image peekFaceImage;
        [SerializeField] private Mask peekFaceMask;
        [SerializeField] private Image peekEdgeTop;
        [SerializeField] private Image peekEdgeRight;
        [SerializeField] private Image peekEdgeBottom;
        [SerializeField] private Image peekEdgeLeft;
        [SerializeField] private Image stackedTopDivider;

        private GameManager gameManager;
        private RectTransform dragLayer;
        private Coroutine feedbackCoroutine;
        private Coroutine juiceCoroutine;
        private bool isFaceUp = true;
        private bool useRevealHistoryLayout;
        private bool useDrawHistoryTitleLayout;
        private bool showRevealHistoryTitle;
        private float revealHistoryVisibleHeight = 46f;
        private float revealHistoryMaskHeight = 46f;
        private bool showTitle = true;
        private bool showFooter = true;
        private bool hasFooterTextOverride;
        private string footerTextOverride = string.Empty;

        private enum FrameSide
        {
            Top,
            Right,
            Bottom,
            Left
        }

        public RectTransform RectTransform { get; private set; }
        public CardData Data { get; private set; }
        public ICardContainer Container { get; private set; }
        public bool IsFaceUp => isFaceUp;
        public bool IsDragDetached { get; private set; }
        public bool IsPlayerStackedOnPrevious { get; private set; }

        public void ConfigureComponents(
            Image targetBackgroundImage,
            Outline targetOutline,
            LayoutElement targetLayoutElement,
            CanvasGroup targetCanvasGroup,
            RectTransform targetTitleRoot,
            TextMeshProUGUI targetTitleLabel,
            RectTransform targetFooterRoot,
            TextMeshProUGUI targetFooterLabel,
            Image targetIconImage)
        {
            backgroundImage = targetBackgroundImage;
            outline = targetOutline;
            layoutElement = targetLayoutElement;
            canvasGroup = targetCanvasGroup;
            titleRoot = targetTitleRoot;
            titleLabel = targetTitleLabel;
            footerRoot = targetFooterRoot;
            footerLabel = targetFooterLabel;
            iconImage = targetIconImage;
        }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;
            EnsureDepthVisuals();
        }

        public void Setup(CardData data, GameManager owner, RectTransform dragParent)
        {
            Data = data;
            gameManager = owner;
            dragLayer = dragParent;
            SetPlayerStackedOnPrevious(false);
            EnsureDepthVisuals();
            RefreshVisual();
        }

        public void SetVisualConfig(CardVisualConfig config)
        {
            visualConfig = config;
            RefreshVisual();
        }

        public void BindToContainer(ICardContainer container)
        {
            Container = container;
            if (container is not GoalSlot)
            {
                showTitle = true;
                showFooter = true;
            }

            if (container is not DrawSlot)
            {
                useDrawHistoryTitleLayout = false;
            }

            RefreshVisual();
        }

        public void SetTextVisibility(bool titleVisible, bool footerVisible)
        {
            if (showTitle == titleVisible && showFooter == footerVisible)
            {
                return;
            }

            showTitle = titleVisible;
            showFooter = footerVisible;
            RefreshVisual();
        }

        public void SetFooterTextOverride(string value)
        {
            string normalizedValue = value ?? string.Empty;
            bool hasOverride = !string.IsNullOrEmpty(normalizedValue);
            if (hasFooterTextOverride == hasOverride && footerTextOverride == normalizedValue)
            {
                return;
            }

            hasFooterTextOverride = hasOverride;
            footerTextOverride = normalizedValue;
            RefreshVisual();
        }

        public void SetSize(Vector2 size)
        {
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = size.x;
                layoutElement.preferredHeight = size.y;
                layoutElement.minWidth = size.x;
                layoutElement.minHeight = size.y;
            }

            if (RectTransform == null)
            {
                RectTransform = (RectTransform)transform;
            }

            RectTransform.sizeDelta = size;
            ApplyPeekBackgroundLayout();
            LayoutPremiumFrame();
        }

        public void SetFaceUp(bool value)
        {
            isFaceUp = value;
            RefreshVisual();
        }

        public void SetVisible(bool value)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = value ? 1f : 0f;
                canvasGroup.blocksRaycasts = value;
                canvasGroup.interactable = value;
            }
        }

        public void SetRaycastBlocks(bool value)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = value;
            }
        }

        public void SetTouchSelected(bool value)
        {
            if (glowImage == null)
            {
                return;
            }

            glowImage.enabled = value;
            glowImage.color = value
                ? new Color(1f, 0.84f, 0.22f, 0.72f)
                : new Color(1f, 0.82f, 0.18f, 0f);
        }

        public void SetDragDetached(bool value)
        {
            IsDragDetached = value;
        }

        public void SetPlayerStackedOnPrevious(bool value)
        {
            if (IsPlayerStackedOnPrevious == value)
            {
                return;
            }

            IsPlayerStackedOnPrevious = value;
            RefreshVisual();
        }

        public void SetDraggingVisual(bool value)
        {
            StopJuiceCoroutine();
            HideGlow();
            DisableExternalCardEffects();

            if (!isActiveAndEnabled)
            {
                transform.localScale = value ? Vector3.one * 1.035f : Vector3.one;
                return;
            }

            if (value)
            {
                transform.localScale = Vector3.one * 1.035f;
                return;
            }

            transform.localScale = Vector3.one;
            RefreshVisual();
        }

        public void PlayAcceptedFeedback()
        {
            HideGlow();
        }

        public void SetRevealHistoryLayout(bool value)
        {
            SetRevealHistoryLayout(value, 46f);
        }

        public void SetRevealHistoryLayout(bool value, float visibleHeight)
        {
            SetRevealHistoryLayout(value, visibleHeight, false);
        }

        public void SetRevealHistoryLayout(bool value, float visibleHeight, bool showTitleInPeek)
        {
            SetRevealHistoryLayout(value, visibleHeight, showTitleInPeek, 0f);
        }

        public void SetRevealHistoryLayout(bool value, float visibleHeight, bool showTitleInPeek, float maskOverflowHeight)
        {
            visibleHeight = Mathf.Clamp(visibleHeight, 22f, 52f);
            float maskHeight = Mathf.Clamp(
                visibleHeight + Mathf.Max(0f, maskOverflowHeight),
                visibleHeight,
                Mathf.Max(visibleHeight, GetCurrentCardHeight()));
            if (useRevealHistoryLayout == value
                && Mathf.Approximately(revealHistoryVisibleHeight, visibleHeight)
                && Mathf.Approximately(revealHistoryMaskHeight, maskHeight)
                && showRevealHistoryTitle == showTitleInPeek)
            {
                return;
            }

            useRevealHistoryLayout = value;
            showRevealHistoryTitle = showTitleInPeek;
            revealHistoryVisibleHeight = visibleHeight;
            revealHistoryMaskHeight = maskHeight;
            RefreshVisual();
        }

        public void SetDrawHistoryTitleLayout(bool value)
        {
            if (useDrawHistoryTitleLayout == value)
            {
                return;
            }

            useDrawHistoryTitleLayout = value;
            RefreshVisual();
        }

        public void AnimateFromWorldPosition(Vector3 startWorldPosition, float duration)
        {
            if (!isActiveAndEnabled || RectTransform == null || duration <= 0f)
            {
                return;
            }

            Vector3 targetWorldPosition = RectTransform.position;
            StartFeedbackCoroutine(AnimateWorldPosition(startWorldPosition, targetWorldPosition, duration));
        }

        public void PlayShake(float duration, float strength)
        {
            if (!isActiveAndEnabled || RectTransform == null || duration <= 0f || strength <= 0f)
            {
                return;
            }

            StartFeedbackCoroutine(Shake(duration, strength));
        }

        public void RefreshVisual()
        {
            if (Data == null)
            {
                return;
            }

            ApplyTextLayout();
            EnsureDepthVisuals();

            if (!isFaceUp)
            {
                ApplyBackground(visualConfig != null ? visualConfig.CardBackSprite : null, visualConfig != null ? visualConfig.CardBackColor : BackColor);
                ApplyPremiumFrame(false, Data != null && Data.IsGold);
                SetFooterBadgeVisible(false);

                if (outline != null)
                {
                    ApplyOutlineColor(visualConfig != null ? visualConfig.BackOutlineColor : BackOutlineColor);
                }

                if (titleLabel != null)
                {
                    titleLabel.text = string.Empty;
                }

                if (footerLabel != null)
                {
                    footerLabel.text = string.Empty;
                }

                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                return;
            }

            Sprite faceSprite = null;
            if (backgroundImage != null)
            {
                faceSprite = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldCardSprite : visualConfig.NormalCardSprite)
                    : null;
                Color faceColor = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldCardColor : visualConfig.NormalCardColor)
                    : (Data.IsGold ? GoldColor : RegularColor);
                ApplyBackground(faceSprite, faceColor);
            }
            ApplyPremiumFrame(!useRevealHistoryLayout && !useDrawHistoryTitleLayout && faceSprite == null, Data.IsGold);

            if (outline != null)
            {
                ApplyOutlineColor(visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldOutlineColor : visualConfig.NormalOutlineColor)
                    : (Data.IsGold ? GoldOutlineColor : RegularOutlineColor));
            }

            if (titleLabel != null)
            {
                ApplyConfiguredFont(titleLabel);
                titleLabel.color = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldTextColor : visualConfig.TextColor)
                    : new Color(0.26f, 0.2f, 0.16f, 1f);
                EnsureTextFontSupports(titleLabel, Data.DisplayText);
                bool shouldShowTitleText = showTitle && (!Data.IsImageCard || useRevealHistoryLayout || useDrawHistoryTitleLayout);
                titleLabel.text = shouldShowTitleText ? Data.DisplayText : string.Empty;
            }

            if (footerLabel != null)
            {
                GoalSlot goalSlot = Container as GoalSlot;
                bool isTopGoalCard = goalSlot != null && goalSlot.IsTopCard(this);
                CardData progressSource = goalSlot != null
                    ? goalSlot.GoalCard?.Data
                    : Data;
                bool shouldShowProgress = Data != null
                    && progressSource != null
                    && (Data.IsGold || isTopGoalCard);
                bool canShowFooterText = showFooter && !useRevealHistoryLayout && !useDrawHistoryTitleLayout;
                string footerText = string.Empty;
                if (canShowFooterText && hasFooterTextOverride)
                {
                    footerText = footerTextOverride;
                }
                else if (canShowFooterText && shouldShowProgress)
                {
                    footerText = $"{progressSource.CurrentCount}/{progressSource.RequiredCount}";
                }

                if (!string.IsNullOrEmpty(footerText))
                {
                    ApplyConfiguredFont(footerLabel);
                    footerLabel.color = visualConfig != null ? visualConfig.GoldTextColor : new Color(0.35f, 0.28f, 0.14f, 0.9f);
                    EnsureTextFontSupports(footerLabel, footerText);
                }

                footerLabel.text = footerText;
            }

            if (iconImage != null)
            {
                bool hasIcon = Data.Icon != null && !useRevealHistoryLayout;
                iconImage.enabled = hasIcon;
                iconImage.sprite = Data.Icon;
                ApplyIconLayout(hasIcon && Data.IsImageCard);
            }
        }

        private void ApplyBackground(Sprite sprite, Color fallbackColor)
        {
            if (backgroundImage == null)
            {
                return;
            }

            bool hasVisibleFaceSprite = sprite != null && !useRevealHistoryLayout;
            backgroundImage.sprite = GetRoundedCardSprite();
            backgroundImage.color = useRevealHistoryLayout
                ? new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0f)
                : hasVisibleFaceSprite
                    ? new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0f)
                : fallbackColor;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = true;

            ApplyFaceSprite(sprite, hasVisibleFaceSprite);

            if (glowImage != null)
            {
                glowImage.sprite = GetRoundedCardSprite();
                glowImage.type = Image.Type.Sliced;
                glowImage.preserveAspect = false;
                glowImage.enabled = false;
                glowImage.color = new Color(1f, 0.82f, 0.18f, 0f);
            }

            if (outline != null)
            {
                outline.enabled = false;
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
            }

            if (shadow != null)
            {
                shadow.enabled = false;
                shadow.effectColor = Color.clear;
                shadow.effectDistance = Vector2.zero;
            }

            ApplyStackedTopDivider(false);

            if (useRevealHistoryLayout)
            {
                ApplyPeekBackground(sprite, fallbackColor);
            }
            else if (peekBackgroundImage != null)
            {
                SetPeekBackgroundVisible(false);
            }
        }

        private void ApplyStackedTopDivider(bool isVisible)
        {
            EnsureStackedTopDivider();
            if (stackedTopDivider == null)
            {
                return;
            }

            stackedTopDivider.enabled = isVisible;
            if (!isVisible)
            {
                return;
            }

            RectTransform rect = stackedTopDivider.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -StackedTopDividerThickness);
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            stackedTopDivider.transform.SetAsLastSibling();
        }

        private void EnsureStackedTopDivider()
        {
            if (stackedTopDivider == null)
            {
                Transform existing = transform.Find("StackedTopDivider");
                stackedTopDivider = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (stackedTopDivider == null)
            {
                GameObject dividerObject = new("StackedTopDivider", typeof(RectTransform), typeof(Image));
                dividerObject.transform.SetParent(transform, false);
                stackedTopDivider = dividerObject.GetComponent<Image>();
            }

            stackedTopDivider.raycastTarget = false;
            stackedTopDivider.sprite = null;
            stackedTopDivider.type = Image.Type.Simple;
            stackedTopDivider.color = StackedTopDividerColor;
        }

        private void ApplyOutlineColor(Color color)
        {
            if (outline != null)
            {
                outline.enabled = false;
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
            }

            if (peekOutline != null)
            {
                peekOutline.enabled = false;
                peekOutline.effectColor = Color.clear;
                peekOutline.effectDistance = Vector2.zero;
            }
        }

        private void ApplyPremiumFrame(bool isVisible, bool isGoldCard)
        {
            EnsurePremiumFrame();
            if (premiumFrameRoot == null)
            {
                return;
            }

            premiumFrameRoot.gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            SetLegacyPremiumFrameSegmentsVisible(false);
            EnsurePremiumFrameArt();
            if (premiumFrameArtImage != null)
            {
                premiumFrameArtImage.enabled = true;
                premiumFrameArtImage.sprite = GetPremiumFrameSprite(isGoldCard);
                premiumFrameArtImage.color = Color.white;
                premiumFrameArtImage.type = Image.Type.Simple;
                premiumFrameArtImage.preserveAspect = false;
                premiumFrameArtImage.raycastTarget = false;
            }

            LayoutPremiumFrame();
        }

        private void EnsurePremiumFrame()
        {
            if (premiumFrameRoot == null)
            {
                Transform existingFrame = transform.Find("PremiumInnerFrame");
                if (existingFrame is RectTransform existingRect)
                {
                    premiumFrameRoot = existingRect;
                }
            }

            if (premiumFrameRoot == null)
            {
                GameObject frameObject = new("PremiumInnerFrame", typeof(RectTransform));
                premiumFrameRoot = frameObject.GetComponent<RectTransform>();
                premiumFrameRoot.SetParent(transform, false);
            }

            premiumFrameRoot.anchorMin = Vector2.zero;
            premiumFrameRoot.anchorMax = Vector2.one;
            premiumFrameRoot.offsetMin = Vector2.zero;
            premiumFrameRoot.offsetMax = Vector2.zero;
            premiumFrameRoot.pivot = new Vector2(0.5f, 0.5f);
            premiumFrameRoot.localScale = Vector3.one;
            premiumFrameRoot.localRotation = Quaternion.identity;
            premiumFrameRoot.SetSiblingIndex(2);

            EnsurePremiumFrameArt();
            SetLegacyPremiumFrameSegmentsVisible(false);

            LayoutPremiumFrame();
        }

        private void EnsurePremiumFrameArt()
        {
            if (premiumFrameRoot == null)
            {
                return;
            }

            if (premiumFrameArtImage == null)
            {
                Transform existing = premiumFrameRoot.Find("PremiumFrameArt");
                premiumFrameArtImage = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (premiumFrameArtImage == null)
            {
                GameObject artObject = new("PremiumFrameArt", typeof(RectTransform), typeof(Image));
                artObject.transform.SetParent(premiumFrameRoot, false);
                premiumFrameArtImage = artObject.GetComponent<Image>();
            }

            premiumFrameArtImage.raycastTarget = false;
            premiumFrameArtImage.transform.SetAsLastSibling();
        }

        private void SetLegacyPremiumFrameSegmentsVisible(bool isVisible)
        {
            if (premiumFrameRoot == null)
            {
                return;
            }

            for (int i = 0; i < premiumFrameRoot.childCount; i++)
            {
                Transform child = premiumFrameRoot.GetChild(i);
                if (child == null || child.name == "PremiumFrameArt")
                {
                    continue;
                }

                if (child.TryGetComponent(out Image image))
                {
                    image.enabled = isVisible;
                }
            }
        }

        private Image EnsureFrameImage(string imageName)
        {
            Transform existing = premiumFrameRoot.Find(imageName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject imageObject = new(imageName, typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(premiumFrameRoot, false);
                image = imageObject.GetComponent<Image>();
            }

            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        private void SetFrameImageColor(string imageName, Color color)
        {
            if (premiumFrameRoot == null)
            {
                return;
            }

            Transform child = premiumFrameRoot.Find(imageName);
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = color;
            }
        }

        private void ApplyPremiumCornerColors(Color highlight, Color gold)
        {
            Color softenedGold = new(gold.r, gold.g, gold.b, gold.a * 0.86f);
            SetFrameImageColor("PremiumFrameTopLeftH", highlight);
            SetFrameImageColor("PremiumFrameTopLeftV", highlight);
            SetFrameImageColor("PremiumFrameTopLeftD", softenedGold);
            SetFrameImageColor("PremiumFrameTopRightH", highlight);
            SetFrameImageColor("PremiumFrameTopRightV", softenedGold);
            SetFrameImageColor("PremiumFrameTopRightD", softenedGold);
            SetFrameImageColor("PremiumFrameBottomRightH", softenedGold);
            SetFrameImageColor("PremiumFrameBottomRightV", softenedGold);
            SetFrameImageColor("PremiumFrameBottomRightD", softenedGold);
            SetFrameImageColor("PremiumFrameBottomLeftH", softenedGold);
            SetFrameImageColor("PremiumFrameBottomLeftV", highlight);
            SetFrameImageColor("PremiumFrameBottomLeftD", softenedGold);
        }

        private void LayoutPremiumFrame()
        {
            if (premiumFrameRoot == null)
            {
                return;
            }

            if (premiumFrameArtImage == null)
            {
                return;
            }

            RectTransform artRect = premiumFrameArtImage.rectTransform;
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.localScale = Vector3.one;
            artRect.localRotation = Quaternion.identity;
        }

        private void ApplyFaceSprite(Sprite sprite, bool isVisible)
        {
            EnsureFaceClip();
            if (faceClipRoot == null || faceImage == null)
            {
                return;
            }

            faceClipRoot.gameObject.SetActive(isVisible && sprite != null);
            faceImage.enabled = isVisible && sprite != null;
            faceImage.sprite = sprite;
            faceImage.color = Color.white;
            faceImage.type = Image.Type.Simple;
            faceImage.preserveAspect = false;
            BringCardContentToFront();
        }

        private void BringCardContentToFront()
        {
            if (titleRoot != null)
            {
                titleRoot.SetAsLastSibling();
            }

            if (footerRoot != null)
            {
                footerRoot.SetAsLastSibling();
            }

            if (iconImage != null)
            {
                iconImage.transform.SetAsLastSibling();
            }
        }

        private void EnsureFaceClip()
        {
            if (faceClipRoot == null)
            {
                Transform existingClip = transform.Find("CardFaceClip");
                if (existingClip is RectTransform existingClipRect)
                {
                    faceClipRoot = existingClipRect;
                    faceClipMaskImage = existingClip.GetComponent<Image>();
                    faceMask = existingClip.GetComponent<Mask>();
                }
            }

            if (faceClipRoot == null)
            {
                GameObject clipObject = new("CardFaceClip", typeof(RectTransform), typeof(Image), typeof(Mask));
                faceClipRoot = clipObject.GetComponent<RectTransform>();
                faceClipRoot.SetParent(transform, false);
                faceClipMaskImage = clipObject.GetComponent<Image>();
                faceMask = clipObject.GetComponent<Mask>();
            }

            faceClipRoot.anchorMin = Vector2.zero;
            faceClipRoot.anchorMax = Vector2.one;
            faceClipRoot.offsetMin = Vector2.zero;
            faceClipRoot.offsetMax = Vector2.zero;
            faceClipRoot.pivot = new Vector2(0.5f, 0.5f);
            faceClipRoot.localScale = Vector3.one;
            faceClipRoot.localRotation = Quaternion.identity;
            faceClipRoot.SetSiblingIndex(glowImage != null ? 1 : 0);

            if (faceClipMaskImage != null)
            {
                faceClipMaskImage.sprite = GetRoundedCardSprite();
                faceClipMaskImage.type = Image.Type.Sliced;
                faceClipMaskImage.color = Color.white;
                faceClipMaskImage.raycastTarget = false;
            }

            if (faceMask != null)
            {
                faceMask.showMaskGraphic = false;
            }

            if (faceImage == null)
            {
                Transform existingFace = faceClipRoot.Find("CardFaceImage");
                faceImage = existingFace != null ? existingFace.GetComponent<Image>() : null;
            }

            if (faceImage == null)
            {
                GameObject faceObject = new("CardFaceImage", typeof(RectTransform), typeof(Image));
                RectTransform faceRect = faceObject.GetComponent<RectTransform>();
                faceRect.SetParent(faceClipRoot, false);
                faceImage = faceObject.GetComponent<Image>();
            }

            RectTransform imageRect = faceImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.localScale = Vector3.one;
            imageRect.localRotation = Quaternion.identity;
            faceImage.raycastTarget = false;
        }

        private void LayoutFrameSide(string imageName, FrameSide side, float inset, float thickness, float cornerGap = PremiumFrameCornerSize)
        {
            Transform child = premiumFrameRoot.Find(imageName);
            if (child is not RectTransform rect)
            {
                return;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            switch (side)
            {
                case FrameSide.Top:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.offsetMin = new Vector2(inset + cornerGap, -inset - thickness);
                    rect.offsetMax = new Vector2(-inset - cornerGap, -inset);
                    break;
                case FrameSide.Right:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.offsetMin = new Vector2(-inset - thickness, inset + cornerGap);
                    rect.offsetMax = new Vector2(-inset, -inset - cornerGap);
                    break;
                case FrameSide.Bottom:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.offsetMin = new Vector2(inset + cornerGap, inset);
                    rect.offsetMax = new Vector2(-inset - cornerGap, inset + thickness);
                    break;
                case FrameSide.Left:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.offsetMin = new Vector2(inset, inset + cornerGap);
                    rect.offsetMax = new Vector2(inset + thickness, -inset - cornerGap);
                    break;
            }
        }

        private void LayoutPremiumFrameCorners()
        {
            float inset = PremiumFrameInset;
            float run = PremiumFrameCornerSize - PremiumFrameCornerCut;
            float diagonalLength = PremiumFrameCornerCut * 1.42f;
            float thickness = PremiumFrameCornerThickness;

            LayoutFrameSegment("PremiumFrameTopLeftH", new Vector2(0f, 1f), new Vector2(inset + PremiumFrameCornerCut + (run * 0.5f), -inset), new Vector2(run, thickness), 0f);
            LayoutFrameSegment("PremiumFrameTopLeftV", new Vector2(0f, 1f), new Vector2(inset, -inset - PremiumFrameCornerCut - (run * 0.5f)), new Vector2(thickness, run), 0f);
            LayoutFrameSegment("PremiumFrameTopLeftD", new Vector2(0f, 1f), new Vector2(inset + (PremiumFrameCornerCut * 0.5f), -inset - (PremiumFrameCornerCut * 0.5f)), new Vector2(diagonalLength, thickness), 45f);

            LayoutFrameSegment("PremiumFrameTopRightH", new Vector2(1f, 1f), new Vector2(-inset - PremiumFrameCornerCut - (run * 0.5f), -inset), new Vector2(run, thickness), 0f);
            LayoutFrameSegment("PremiumFrameTopRightV", new Vector2(1f, 1f), new Vector2(-inset, -inset - PremiumFrameCornerCut - (run * 0.5f)), new Vector2(thickness, run), 0f);
            LayoutFrameSegment("PremiumFrameTopRightD", new Vector2(1f, 1f), new Vector2(-inset - (PremiumFrameCornerCut * 0.5f), -inset - (PremiumFrameCornerCut * 0.5f)), new Vector2(diagonalLength, thickness), -45f);

            LayoutFrameSegment("PremiumFrameBottomRightH", new Vector2(1f, 0f), new Vector2(-inset - PremiumFrameCornerCut - (run * 0.5f), inset), new Vector2(run, thickness), 0f);
            LayoutFrameSegment("PremiumFrameBottomRightV", new Vector2(1f, 0f), new Vector2(-inset, inset + PremiumFrameCornerCut + (run * 0.5f)), new Vector2(thickness, run), 0f);
            LayoutFrameSegment("PremiumFrameBottomRightD", new Vector2(1f, 0f), new Vector2(-inset - (PremiumFrameCornerCut * 0.5f), inset + (PremiumFrameCornerCut * 0.5f)), new Vector2(diagonalLength, thickness), 45f);

            LayoutFrameSegment("PremiumFrameBottomLeftH", new Vector2(0f, 0f), new Vector2(inset + PremiumFrameCornerCut + (run * 0.5f), inset), new Vector2(run, thickness), 0f);
            LayoutFrameSegment("PremiumFrameBottomLeftV", new Vector2(0f, 0f), new Vector2(inset, inset + PremiumFrameCornerCut + (run * 0.5f)), new Vector2(thickness, run), 0f);
            LayoutFrameSegment("PremiumFrameBottomLeftD", new Vector2(0f, 0f), new Vector2(inset + (PremiumFrameCornerCut * 0.5f), inset + (PremiumFrameCornerCut * 0.5f)), new Vector2(diagonalLength, thickness), -45f);
        }

        private void LayoutFrameSegment(string imageName, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, float rotationZ)
        {
            Transform child = premiumFrameRoot.Find(imageName);
            if (child is not RectTransform rect)
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }

        private void ApplyPeekBackground(Sprite sprite, Color fallbackColor)
        {
            EnsurePeekBackground();
            if (peekBackgroundImage == null)
            {
                return;
            }

            bool shouldUseFaceSprite = sprite != null;
            Color backingColor = shouldUseFaceSprite ? PeekCardBorderFillColor : fallbackColor;
            SetPeekBackgroundVisible(true);
            ApplyPeekSolidFill(backingColor);
            if (shouldUseFaceSprite && peekSolidFillImage != null)
            {
                peekSolidFillImage.enabled = false;
            }

            peekBackgroundImage.sprite = GetRoundedCardSprite();
            peekBackgroundImage.color = shouldUseFaceSprite
                ? new Color(backingColor.r, backingColor.g, backingColor.b, 0f)
                : backingColor;
            peekBackgroundImage.type = Image.Type.Sliced;
            peekBackgroundImage.preserveAspect = false;
            peekBackgroundImage.raycastTarget = false;
            ApplyPeekFaceSprite(sprite, shouldUseFaceSprite);
            SetPeekEdgesVisible(false);

            if (peekOutline != null)
            {
                peekOutline.enabled = false;
            }

            ApplyPeekBackgroundLayout();
        }

        private void EnsurePeekBackground()
        {
            if (peekMaskRoot == null)
            {
                Transform existingMask = transform.Find("CardPeekMask");
                if (existingMask is RectTransform existingMaskRect)
                {
                    peekMaskRoot = existingMaskRect;
                }
            }

            if (peekMaskRoot == null)
            {
                GameObject maskObject = new("CardPeekMask", typeof(RectTransform), typeof(RectMask2D));
                peekMaskRoot = maskObject.GetComponent<RectTransform>();
                peekMaskRoot.SetParent(transform, false);
            }

            if (peekBackgroundImage == null)
            {
                Transform existingPeek = peekMaskRoot.Find("CardPeekBackground");
                if (existingPeek != null)
                {
                    peekBackgroundImage = existingPeek.GetComponent<Image>();
                    peekOutline = existingPeek.GetComponent<Outline>();
                }
            }

            if (peekBackgroundImage == null)
            {
                GameObject peekObject = new("CardPeekBackground", typeof(RectTransform), typeof(Image), typeof(Outline));
                RectTransform peekRect = peekObject.GetComponent<RectTransform>();
                peekRect.SetParent(peekMaskRoot, false);
                peekBackgroundImage = peekObject.GetComponent<Image>();
                peekOutline = peekObject.GetComponent<Outline>();
            }
            else if (peekBackgroundImage.transform.parent != peekMaskRoot)
            {
                peekBackgroundImage.rectTransform.SetParent(peekMaskRoot, false);
            }

            EnsurePeekSolidFill();

            if (peekOutline != null)
            {
                peekOutline.effectColor = outline != null ? outline.effectColor : RegularOutlineColor;
                peekOutline.useGraphicAlpha = true;
                peekOutline.enabled = false;
            }

            EnsurePeekEdges();

            peekMaskRoot.SetAsFirstSibling();
            ApplyPeekBackgroundLayout();
        }

        private void EnsurePeekSolidFill()
        {
            if (peekMaskRoot == null)
            {
                return;
            }

            if (peekSolidFillImage == null)
            {
                Transform existingFill = peekMaskRoot.Find("CardPeekSolidFill");
                peekSolidFillImage = existingFill != null ? existingFill.GetComponent<Image>() : null;
            }

            if (peekSolidFillImage == null)
            {
                GameObject fillObject = new("CardPeekSolidFill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(peekMaskRoot, false);
                peekSolidFillImage = fillObject.GetComponent<Image>();
            }

            peekSolidFillImage.sprite = null;
            peekSolidFillImage.type = Image.Type.Simple;
            peekSolidFillImage.raycastTarget = false;
        }

        private void ApplyPeekSolidFill(Color color)
        {
            EnsurePeekSolidFill();
            if (peekSolidFillImage == null)
            {
                return;
            }

            peekSolidFillImage.enabled = true;
            peekSolidFillImage.color = color;
        }

        private void ApplyPeekBackgroundLayout()
        {
            if (peekMaskRoot == null || peekBackgroundImage == null)
            {
                return;
            }

            peekMaskRoot.anchorMin = new Vector2(0f, 1f);
            peekMaskRoot.anchorMax = new Vector2(1f, 1f);
            peekMaskRoot.pivot = new Vector2(0.5f, 1f);
            peekMaskRoot.sizeDelta = new Vector2(0f, revealHistoryMaskHeight);
            peekMaskRoot.anchoredPosition = Vector2.zero;
            peekMaskRoot.localScale = Vector3.one;
            peekMaskRoot.localRotation = Quaternion.identity;

            if (peekSolidFillImage != null)
            {
                RectTransform fillRect = peekSolidFillImage.rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillRect.pivot = new Vector2(0.5f, 0.5f);
                fillRect.localScale = Vector3.one;
                fillRect.localRotation = Quaternion.identity;
                peekSolidFillImage.transform.SetAsFirstSibling();
            }

            RectTransform peekRect = peekBackgroundImage.rectTransform;
            peekRect.anchorMin = new Vector2(0f, 1f);
            peekRect.anchorMax = new Vector2(1f, 1f);
            peekRect.pivot = new Vector2(0.5f, 1f);
            peekRect.sizeDelta = new Vector2(0f, GetCurrentCardHeight());
            peekRect.anchoredPosition = Vector2.zero;
            peekRect.localScale = Vector3.one;
            peekRect.localRotation = Quaternion.identity;
            peekBackgroundImage.transform.SetSiblingIndex(peekSolidFillImage != null ? 1 : 0);

            if (peekFaceClipRoot != null)
            {
                peekFaceClipRoot.anchorMin = new Vector2(0f, 1f);
                peekFaceClipRoot.anchorMax = new Vector2(1f, 1f);
                peekFaceClipRoot.pivot = new Vector2(0.5f, 1f);
                peekFaceClipRoot.sizeDelta = new Vector2(0f, GetCurrentCardHeight());
                peekFaceClipRoot.anchoredPosition = Vector2.zero;
                peekFaceClipRoot.localScale = Vector3.one;
                peekFaceClipRoot.localRotation = Quaternion.identity;
                peekFaceClipRoot.SetSiblingIndex(peekSolidFillImage != null ? 2 : 1);
            }

            LayoutPeekEdges();
        }

        private void ApplyPeekFaceSprite(Sprite sprite, bool isVisible)
        {
            EnsurePeekFaceClip();
            if (peekFaceClipRoot == null || peekFaceImage == null)
            {
                return;
            }

            bool shouldShow = isVisible && sprite != null;
            peekFaceClipRoot.gameObject.SetActive(shouldShow);
            peekFaceImage.enabled = shouldShow;
            peekFaceImage.sprite = sprite;
            peekFaceImage.color = Color.white;
            peekFaceImage.type = Image.Type.Simple;
            peekFaceImage.preserveAspect = false;
            ApplyPeekBackgroundLayout();
        }

        private void EnsurePeekFaceClip()
        {
            if (peekMaskRoot == null)
            {
                return;
            }

            if (peekFaceClipRoot == null)
            {
                Transform existingClip = peekMaskRoot.Find("CardPeekFaceClip");
                if (existingClip is RectTransform existingClipRect)
                {
                    peekFaceClipRoot = existingClipRect;
                    peekFaceClipMaskImage = existingClip.GetComponent<Image>();
                    peekFaceMask = existingClip.GetComponent<Mask>();
                }
            }

            if (peekFaceClipRoot == null)
            {
                GameObject clipObject = new("CardPeekFaceClip", typeof(RectTransform), typeof(Image), typeof(Mask));
                peekFaceClipRoot = clipObject.GetComponent<RectTransform>();
                peekFaceClipRoot.SetParent(peekMaskRoot, false);
                peekFaceClipMaskImage = clipObject.GetComponent<Image>();
                peekFaceMask = clipObject.GetComponent<Mask>();
            }

            if (peekFaceClipMaskImage != null)
            {
                peekFaceClipMaskImage.sprite = GetRoundedCardSprite();
                peekFaceClipMaskImage.type = Image.Type.Sliced;
                peekFaceClipMaskImage.color = Color.white;
                peekFaceClipMaskImage.raycastTarget = false;
            }

            if (peekFaceMask != null)
            {
                peekFaceMask.showMaskGraphic = false;
            }

            if (peekFaceImage == null)
            {
                Transform existingFace = peekFaceClipRoot.Find("CardPeekFaceImage");
                peekFaceImage = existingFace != null ? existingFace.GetComponent<Image>() : null;
            }

            if (peekFaceImage == null)
            {
                GameObject faceObject = new("CardPeekFaceImage", typeof(RectTransform), typeof(Image));
                RectTransform faceRect = faceObject.GetComponent<RectTransform>();
                faceRect.SetParent(peekFaceClipRoot, false);
                peekFaceImage = faceObject.GetComponent<Image>();
            }

            RectTransform imageRect = peekFaceImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.localScale = Vector3.one;
            imageRect.localRotation = Quaternion.identity;
            peekFaceImage.raycastTarget = false;
            ApplyPeekBackgroundLayout();
        }

        private float GetCurrentCardHeight()
        {
            if (RectTransform != null && RectTransform.rect.height > 1f)
            {
                return RectTransform.rect.height;
            }

            if (RectTransform != null && RectTransform.sizeDelta.y > 1f)
            {
                return RectTransform.sizeDelta.y;
            }

            if (layoutElement != null && layoutElement.preferredHeight > 1f)
            {
                return layoutElement.preferredHeight;
            }

            return 374f;
        }

        private static Sprite GetPremiumFrameSprite(bool isGoldCard)
        {
            if (isGoldCard)
            {
                premiumGoldFrameSprite ??= CreatePremiumFrameSprite(
                    "RuntimePremiumGoldInnerFrame",
                    new Color32(122, 78, 0, 64),
                    new Color32(190, 121, 12, 178),
                    new Color32(255, 225, 100, 170));
                return premiumGoldFrameSprite;
            }

            premiumRegularFrameSprite ??= CreatePremiumFrameSprite(
                "RuntimePremiumRegularInnerFrame",
                new Color32(86, 54, 18, 46),
                new Color32(160, 112, 48, 142),
                new Color32(255, 233, 182, 116));
            return premiumRegularFrameSprite;
        }

        private static Sprite CreatePremiumFrameSprite(string spriteName, Color32 shadowColor, Color32 mainColor, Color32 highlightColor)
        {
            const int width = 128;
            const int height = 192;
            const int inset = 12;
            const int corner = 20;
            const int notch = 7;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 0);
            }

            Vector2Int[] path =
            {
                new(inset + corner, height - inset),
                new(width - inset - corner, height - inset),
                new(width - inset - corner, height - inset - notch),
                new(width - inset - notch, height - inset - notch),
                new(width - inset, height - inset - corner),
                new(width - inset, inset + corner),
                new(width - inset - notch, inset + notch),
                new(width - inset - corner, inset + notch),
                new(width - inset - corner, inset),
                new(inset + corner, inset),
                new(inset + corner, inset + notch),
                new(inset + notch, inset + notch),
                new(inset, inset + corner),
                new(inset, height - inset - corner),
                new(inset + notch, height - inset - notch),
                new(inset + corner, height - inset - notch),
                new(inset + corner, height - inset),
            };

            DrawFramePath(pixels, width, height, path, new Vector2Int(2, -2), shadowColor, 2);
            DrawFramePath(pixels, width, height, path, Vector2Int.zero, mainColor, 2);
            DrawFramePath(pixels, width, height, path, new Vector2Int(0, 1), highlightColor, 1);

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = spriteName;
            return sprite;
        }

        private static void DrawFramePath(Color32[] pixels, int width, int height, Vector2Int[] path, Vector2Int offset, Color32 color, int radius)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                DrawLine(pixels, width, height, path[i] + offset, path[i + 1] + offset, color, radius);
            }
        }

        private static void DrawLine(Color32[] pixels, int width, int height, Vector2Int start, Vector2Int end, Color32 color, int radius)
        {
            int dx = end.x - start.x;
            int dy = end.y - start.y;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (steps <= 0)
            {
                BlendBrush(pixels, width, height, start.x, start.y, color, radius);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
                BlendBrush(pixels, width, height, x, y, color, radius);
            }
        }

        private static void BlendBrush(Color32[] pixels, int width, int height, int centerX, int centerY, Color32 color, int radius)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= height)
                {
                    continue;
                }

                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= width)
                    {
                        continue;
                    }

                    int dx = x - centerX;
                    int dy = y - centerY;
                    if ((dx * dx) + (dy * dy) > radiusSquared)
                    {
                        continue;
                    }

                    int index = (y * width) + x;
                    pixels[index] = AlphaBlend(pixels[index], color);
                }
            }
        }

        private static Color32 AlphaBlend(Color32 destination, Color32 source)
        {
            float sourceAlpha = source.a / 255f;
            float destinationAlpha = destination.a / 255f;
            float outAlpha = sourceAlpha + (destinationAlpha * (1f - sourceAlpha));
            if (outAlpha <= 0f)
            {
                return new Color32(255, 255, 255, 0);
            }

            byte r = (byte)Mathf.RoundToInt(((source.r * sourceAlpha) + (destination.r * destinationAlpha * (1f - sourceAlpha))) / outAlpha);
            byte g = (byte)Mathf.RoundToInt(((source.g * sourceAlpha) + (destination.g * destinationAlpha * (1f - sourceAlpha))) / outAlpha);
            byte b = (byte)Mathf.RoundToInt(((source.b * sourceAlpha) + (destination.b * destinationAlpha * (1f - sourceAlpha))) / outAlpha);
            byte a = (byte)Mathf.RoundToInt(outAlpha * 255f);
            return new Color32(r, g, b, a);
        }

        public static Sprite GetRoundedCardSprite()
        {
            if (roundedCardSprite != null)
            {
                return roundedCardSprite;
            }

            const int size = 96;
            const int radius = 13;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedCardMask"
            };
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            float centerMin = radius - 0.5f;
            float centerMax = size - radius - 0.5f;
            float radiusSquared = radius * radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x;
                    float py = y;
                    float nearestX = Mathf.Clamp(px, centerMin, centerMax);
                    float nearestY = Mathf.Clamp(py, centerMin, centerMax);
                    float distanceSquared = ((px - nearestX) * (px - nearestX)) + ((py - nearestY) * (py - nearestY));
                    texture.SetPixel(x, y, distanceSquared <= radiusSquared ? solid : clear);
                }
            }

            texture.Apply(false, true);
            roundedCardSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedCardSprite.name = "RuntimeRoundedCardSprite";
            return roundedCardSprite;
        }

        private void SetPeekBackgroundVisible(bool isVisible)
        {
            if (peekMaskRoot != null)
            {
                peekMaskRoot.gameObject.SetActive(isVisible);
            }

            if (peekBackgroundImage != null)
            {
                peekBackgroundImage.enabled = isVisible;
            }

            if (peekSolidFillImage != null)
            {
                peekSolidFillImage.enabled = isVisible;
            }

            if (!isVisible && peekFaceClipRoot != null)
            {
                peekFaceClipRoot.gameObject.SetActive(false);
            }

            if (!isVisible && peekFaceImage != null)
            {
                peekFaceImage.enabled = false;
            }

            if (peekOutline != null)
            {
                peekOutline.enabled = false;
            }

            SetPeekEdgesVisible(isVisible && peekBackgroundImage != null && peekBackgroundImage.sprite == GetRoundedCardSprite());
        }

        private void EnsurePeekEdges()
        {
            if (peekMaskRoot == null)
            {
                return;
            }

            peekEdgeTop = EnsurePeekEdge("PeekEdgeTop", peekEdgeTop);
            peekEdgeRight = EnsurePeekEdge("PeekEdgeRight", peekEdgeRight);
            peekEdgeBottom = EnsurePeekEdge("PeekEdgeBottom", peekEdgeBottom);
            peekEdgeLeft = EnsurePeekEdge("PeekEdgeLeft", peekEdgeLeft);
            LayoutPeekEdges();
        }

        private Image EnsurePeekEdge(string edgeName, Image current)
        {
            if (current != null)
            {
                current.raycastTarget = false;
                current.color = GetPeekEdgeColor(edgeName);
                return current;
            }

            Transform existing = peekMaskRoot.Find(edgeName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                GameObject edgeObject = new(edgeName, typeof(RectTransform), typeof(Image));
                edgeObject.transform.SetParent(peekMaskRoot, false);
                image = edgeObject.GetComponent<Image>();
            }

            image.raycastTarget = false;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = GetPeekEdgeColor(edgeName);
            return image;
        }

        private static Color GetPeekEdgeColor(string edgeName)
        {
            return edgeName == "PeekEdgeBottom" ? PeekDividerColor : PeekEdgeColor;
        }

        private void LayoutPeekEdges()
        {
            if (peekMaskRoot == null)
            {
                return;
            }

            LayoutPeekEdge(peekEdgeTop, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -PeekEdgeThickness), new Vector2(0f, 0f));
            LayoutPeekEdge(peekEdgeRight, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-PeekEdgeThickness, 0f), new Vector2(0f, 0f));
            LayoutPeekEdge(peekEdgeBottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, PeekDividerThickness));
            LayoutPeekEdge(peekEdgeLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(PeekEdgeThickness, 0f));
        }

        private static void LayoutPeekEdge(Image edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (edge == null)
            {
                return;
            }

            RectTransform rect = edge.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void SetPeekEdgesVisible(bool isVisible)
        {
            if (peekEdgeTop != null)
            {
                peekEdgeTop.enabled = isVisible;
            }

            if (peekEdgeRight != null)
            {
                peekEdgeRight.enabled = isVisible;
            }

            if (peekEdgeBottom != null)
            {
                peekEdgeBottom.enabled = isVisible;
            }

            if (peekEdgeLeft != null)
            {
                peekEdgeLeft.enabled = isVisible;
            }
        }

        private void ApplyConfiguredFont(TMP_Text textComponent)
        {
            if (textComponent == null || visualConfig == null || visualConfig.Font == null)
            {
                return;
            }

            textComponent.font = visualConfig.Font;
            textComponent.fontSharedMaterial = visualConfig.Font.material;
        }

        private static void EnsureTextFontSupports(TMP_Text textComponent, string text)
        {
            if (textComponent == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            TMP_FontAsset currentFont = textComponent.font;
            if (currentFont != null)
            {
                WarmUpFont(currentFont, text);
                if (currentFont.HasCharacters(text))
                {
                    return;
                }
            }

            TMP_FontAsset fallbackFont = GetOrCreateRuntimeCyrillicFont();
            if (fallbackFont == null)
            {
                return;
            }

            WarmUpFont(fallbackFont, text);
            textComponent.font = fallbackFont;
            textComponent.fontSharedMaterial = fallbackFont.material;
        }

        private static TMP_FontAsset GetOrCreateRuntimeCyrillicFont()
        {
            return runtimeCyrillicFontAsset ??=
                RuntimeCyrillicFontProvider.FontAsset;
        }

        private static Font LoadBuiltinRuntimeFont()
        {
            try
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        private static void WarmUpFont(TMP_FontAsset fontAsset, string text)
        {
            if (fontAsset == null || string.IsNullOrEmpty(text) || TryAddCharactersMethod == null)
            {
                return;
            }

            TryAddCharactersMethod.Invoke(fontAsset, new object[] { text });
        }

        private void ApplyTextLayout()
        {
            bool useTopHistoryTitle = useRevealHistoryLayout || useDrawHistoryTitleLayout;
            bool isGoldCard = Data != null && Data.IsGold && !useTopHistoryTitle;
            bool isImageCard = Data != null && Data.IsImageCard && Data.Icon != null && !useTopHistoryTitle;
            bool isGoalSlotCard = Container is GoalSlot && !useTopHistoryTitle;
            bool useProgressFooterLayout = isGoldCard || isGoalSlotCard || hasFooterTextOverride;

            if (titleRoot != null)
            {
                titleRoot.gameObject.SetActive(showTitle && !isImageCard && (!useRevealHistoryLayout || showRevealHistoryTitle));

                if (useTopHistoryTitle)
                {
                    float titleHeight = useRevealHistoryLayout ? revealHistoryVisibleHeight : 46f;
                    titleRoot.anchorMin = new Vector2(0f, 1f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 1f);
                    titleRoot.sizeDelta = new Vector2(-42f, Mathf.Max(16f, titleHeight - 12f));
                    titleRoot.anchoredPosition = new Vector2(0f, useDrawHistoryTitleLayout ? -13f : -11f);
                }
                else if (isGoldCard)
                {
                    titleRoot.anchorMin = new Vector2(0f, 0f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 0.5f);
                    titleRoot.sizeDelta = new Vector2(-GoldTitleHorizontalPadding * 2f, -76f);
                    titleRoot.anchoredPosition = Vector2.zero;
                }
                else if (isImageCard)
                {
                    titleRoot.anchorMin = new Vector2(0f, 0f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 0.5f);
                    titleRoot.sizeDelta = new Vector2(-ImageCardTitleSidePadding * 2f, -ImageCardTitleHeight);
                    titleRoot.anchoredPosition = Vector2.zero;
                }
                else
                {
                    titleRoot.anchorMin = new Vector2(0f, 0f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 0.5f);
                    titleRoot.sizeDelta = new Vector2(-RegularTitleHorizontalPadding * 2f, -76f);
                    titleRoot.anchoredPosition = Vector2.zero;
                }
            }

            if (titleLabel != null)
            {
                if (useRevealHistoryLayout && !showRevealHistoryTitle)
                {
                    titleLabel.text = string.Empty;
                }

                titleLabel.alignment = useTopHistoryTitle
                    ? TextAlignmentOptions.Center
                    : isGoldCard
                    ? TextAlignmentOptions.Center
                    : TextAlignmentOptions.Center;
                float fontSizeMax = visualConfig != null ? visualConfig.TitleFontSize : 34f;
                float fontSizeMin = visualConfig != null ? visualConfig.TitleFontSizeMin : 18f;
                float historyTitleHeight = useRevealHistoryLayout ? revealHistoryVisibleHeight : 46f;
                float peekFontSize = Mathf.Clamp(historyTitleHeight * 0.58f, 13f, 19f);
                titleLabel.fontSizeMax = useTopHistoryTitle
                    ? Mathf.Min(peekFontSize, fontSizeMax)
                    : isGoldCard ? Mathf.Min(GoldTitleFontSizeMax, fontSizeMax) : isImageCard ? Mathf.Min(20f, fontSizeMax) : Mathf.Min(RegularTitleFontSizeMax, fontSizeMax);
                titleLabel.fontSizeMin = useTopHistoryTitle
                    ? Mathf.Min(11f, fontSizeMin)
                    : isGoldCard ? Mathf.Min(GoldTitleFontSizeMin, fontSizeMin) : isImageCard ? Mathf.Min(11f, fontSizeMin) : Mathf.Min(RegularTitleFontSizeMin, fontSizeMin);
                titleLabel.textWrappingMode =
                    !useTopHistoryTitle && !isImageCard
                        ? TextWrappingModes.Normal
                        : TextWrappingModes.NoWrap;
                titleLabel.overflowMode = TextOverflowModes.Truncate;
            }

            if (footerRoot != null)
            {
                footerRoot.gameObject.SetActive(showFooter && !useRevealHistoryLayout && !useDrawHistoryTitleLayout);
                if (showFooter && !useRevealHistoryLayout && !useDrawHistoryTitleLayout)
                {
                    footerRoot.anchorMin = new Vector2(1f, 1f);
                    footerRoot.anchorMax = new Vector2(1f, 1f);
                    footerRoot.pivot = new Vector2(1f, 1f);
                    footerRoot.sizeDelta = useProgressFooterLayout
                        ? new Vector2(GoldProgressWidth, GoldHeaderHeight)
                        : new Vector2(78f, 30f);
                    footerRoot.anchoredPosition = useProgressFooterLayout
                        ? new Vector2(-GoldHeaderSidePadding, -GoldHeaderTopPadding)
                        : new Vector2(-10f, -10f);

                    SetFooterBadgeVisible(false);
                }
            }

            if (footerLabel != null)
            {
                footerLabel.alignment = useProgressFooterLayout
                    ? TextAlignmentOptions.Right
                    : TextAlignmentOptions.TopRight;
                footerLabel.textWrappingMode = TextWrappingModes.NoWrap;
                footerLabel.overflowMode = TextOverflowModes.Truncate;
                if (useProgressFooterLayout)
                {
                    footerLabel.fontSizeMax = 22f;
                    footerLabel.fontSizeMin = 14f;
                    footerLabel.margin = new Vector4(4f, 0f, 4f, 1f);
                    footerLabel.color = new Color(0.35f, 0.25f, 0.11f, 1f);
                }
            }
        }

        private void ApplyIconLayout(bool useLargeImageLayout)
        {
            if (iconImage == null)
            {
                return;
            }

            RectTransform iconRoot = iconImage.rectTransform;
            if (useLargeImageLayout)
            {
                iconRoot.anchorMin = new Vector2(0.1f, 0.12f);
                iconRoot.anchorMax = new Vector2(0.9f, 0.88f);
                iconRoot.pivot = new Vector2(0.5f, 0.5f);
                iconRoot.offsetMin = Vector2.zero;
                iconRoot.offsetMax = Vector2.zero;
                iconImage.preserveAspect = true;
                return;
            }

            iconRoot.anchorMin = new Vector2(1f, 1f);
            iconRoot.anchorMax = new Vector2(1f, 1f);
            iconRoot.pivot = new Vector2(1f, 1f);
            iconRoot.sizeDelta = new Vector2(40f, 40f);
            iconRoot.anchoredPosition = new Vector2(-12f, -12f);
            iconImage.preserveAspect = true;
        }

        private void SetFooterBadgeVisible(bool isVisible)
        {
            if (footerRoot == null)
            {
                return;
            }

            Transform badgeParent = footerRoot.parent != null ? footerRoot.parent : footerRoot;
            SetNamedChildVisible(badgeParent, "FooterBadgeBg", isVisible);
            SetNamedChildVisible(footerRoot, "FooterBadgeBg", isVisible);
        }

        private static void SetNamedChildVisible(Transform parent, string childName, bool isVisible)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(isVisible);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null
                || eventData.button != PointerEventData.InputButton.Left
                || gameManager == null
                || !gameManager.BeginDrag(this, eventData.pointerId))
            {
                return;
            }

            global::MusicPlayer.Instance?.PlayCardPick();
            gameManager.BeginDragVisual(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null || !gameManager.IsDragging(this, eventData.pointerId))
            {
                return;
            }

            gameManager.UpdateDragVisual(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            gameManager?.EndDrag(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            gameManager?.HandleCardTap(this, eventData.clickCount);
        }

        private void StartFeedbackCoroutine(IEnumerator routine)
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }

            feedbackCoroutine = StartCoroutine(routine);
        }

        private void StartJuiceCoroutine(IEnumerator routine)
        {
            if (juiceCoroutine != null)
            {
                StopCoroutine(juiceCoroutine);
            }

            juiceCoroutine = StartCoroutine(routine);
        }

        private void StopJuiceCoroutine()
        {
            if (juiceCoroutine == null)
            {
                return;
            }

            StopCoroutine(juiceCoroutine);
            juiceCoroutine = null;
        }

        private void HideGlow()
        {
            DisableExternalCardEffects();

            if (glowImage == null)
            {
                return;
            }

            glowImage.enabled = false;
            glowImage.color = new Color(1f, 0.82f, 0.18f, 0f);
        }

        private void DisableExternalCardEffects()
        {
            if (outline != null)
            {
                outline.enabled = false;
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
            }

            if (peekOutline != null)
            {
                peekOutline.enabled = false;
                peekOutline.effectColor = Color.clear;
                peekOutline.effectDistance = Vector2.zero;
            }

            if (shadow != null)
            {
                shadow.enabled = false;
                shadow.effectColor = Color.clear;
                shadow.effectDistance = Vector2.zero;
            }

            SetPeekEdgesVisible(false);
        }

        private void EnsureDepthVisuals()
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (shadow == null)
            {
                shadow = gameObject.GetComponent<Shadow>();
                if (shadow == null)
                {
                    shadow = gameObject.AddComponent<Shadow>();
                }
            }

            shadow.enabled = false;
            shadow.effectColor = Color.clear;
            shadow.effectDistance = Vector2.zero;
            shadow.useGraphicAlpha = true;

            if (glowImage == null)
            {
                Transform existingGlow = transform.Find("CardGlow");
                if (existingGlow != null)
                {
                    glowImage = existingGlow.GetComponent<Image>();
                }
            }

            if (glowImage == null)
            {
                GameObject glowObject = new("CardGlow", typeof(RectTransform), typeof(Image));
                RectTransform glowRect = glowObject.GetComponent<RectTransform>();
                glowRect.SetParent(transform, false);
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = new Vector2(-8f, -8f);
                glowRect.offsetMax = new Vector2(8f, 8f);
                glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowImage = glowObject.GetComponent<Image>();
            }

            glowImage.raycastTarget = false;
            glowImage.color = new Color(1f, 0.82f, 0.18f, 0f);
            glowImage.enabled = false;
            glowImage.transform.SetAsFirstSibling();
        }

        private void PlayGlow(Color targetColor, float targetScale, float duration, bool fadeOut)
        {
            EnsureDepthVisuals();
            StartJuiceCoroutine(GlowScaleRoutine(targetColor, targetScale, duration, fadeOut));
        }

        private IEnumerator GlowScaleRoutine(Color targetColor, float targetScale, float duration, bool fadeOut)
        {
            if (glowImage != null)
            {
                glowImage.enabled = targetColor.a > 0f;
                glowImage.color = targetColor;
            }

            Vector3 startScale = transform.localScale;
            Vector3 peakScale = Vector3.one * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = Vector3.LerpUnclamped(startScale, peakScale, eased);
                yield return null;
            }

            if (fadeOut)
            {
                elapsed = 0f;
                Color startColor = glowImage != null ? glowImage.color : targetColor;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float wave = Mathf.Sin(progress * Mathf.PI);
                    transform.localScale = Vector3.one * (1f + wave * 0.035f);
                    if (glowImage != null)
                    {
                        Color color = startColor;
                        color.a = Mathf.Lerp(startColor.a, 0f, progress);
                        glowImage.color = color;
                    }

                    yield return null;
                }

                if (glowImage != null)
                {
                    glowImage.enabled = false;
                }
            }

            transform.localScale = fadeOut ? Vector3.one : peakScale;
            juiceCoroutine = null;
        }

        private IEnumerator AnimateWorldPosition(Vector3 startWorldPosition, Vector3 targetWorldPosition, float duration)
        {
            RectTransform.position = startWorldPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                RectTransform.position = Vector3.LerpUnclamped(startWorldPosition, targetWorldPosition, easedProgress);
                yield return null;
            }

            RectTransform.position = targetWorldPosition;
            Container?.RefreshLayout();
            feedbackCoroutine = null;
        }

        private IEnumerator Shake(float duration, float strength)
        {
            Vector2 originalPosition = RectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float fade = 1f - progress;
                float offsetX = Mathf.Sin(progress * Mathf.PI * 8f) * strength * fade;
                RectTransform.anchoredPosition = originalPosition + new Vector2(offsetX, 0f);
                yield return null;
            }

            RectTransform.anchoredPosition = originalPosition;
            feedbackCoroutine = null;
        }
    }
}
