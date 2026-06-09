using System.Reflection;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const string RuntimeFontResourcePath = "Fonts/Roboto-Bold";
        private static readonly Color RegularColor = new(0.98f, 0.98f, 0.98f, 1f);
        private static readonly Color GoldColor = new(0.98f, 0.84f, 0.33f, 1f);
        private static readonly Color BackColor = new(0.2f, 0.62f, 0.98f, 1f);
        private static readonly Color RegularOutlineColor = new(0f, 0f, 0f, 0.12f);
        private static readonly Color GoldOutlineColor = new(0.55f, 0.38f, 0f, 0.35f);
        private static readonly Color BackOutlineColor = new(0.05f, 0.27f, 0.5f, 0.45f);
        private const float GoldHeaderSidePadding = 10f;
        private const float GoldHeaderTopPadding = 10f;
        private const float GoldHeaderHeight = 32f;
        private const float GoldProgressWidth = 46f;
        private const float ImageCardTitleHeight = 54f;
        private const float ImageCardTitleSidePadding = 12f;
        private static readonly MethodInfo TryAddCharactersMethod = typeof(TMP_FontAsset).GetMethod(
            "TryAddCharacters",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(string) },
            null);

        private static TMP_FontAsset runtimeCyrillicFontAsset;

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

        private GameManager gameManager;
        private RectTransform dragLayer;
        private Coroutine feedbackCoroutine;
        private Coroutine juiceCoroutine;
        private bool isFaceUp = true;
        private bool useRevealHistoryLayout;
        private bool showRevealHistoryTitle;
        private float revealHistoryVisibleHeight = 46f;
        private bool showTitle = true;
        private bool showFooter = true;

        public RectTransform RectTransform { get; private set; }
        public CardData Data { get; private set; }
        public ICardContainer Container { get; private set; }
        public bool IsFaceUp => isFaceUp;
        public bool IsDragDetached { get; private set; }

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

        public void SetDragDetached(bool value)
        {
            IsDragDetached = value;
        }

        public void SetDraggingVisual(bool value)
        {
            StopJuiceCoroutine();
            HideGlow();

            if (!isActiveAndEnabled)
            {
                transform.localScale = value ? Vector3.one * 1.035f : Vector3.one;
                return;
            }

            if (value)
            {
                transform.localScale = Vector3.one * 1.035f;
                if (outline != null)
                {
                    outline.effectColor = new Color(1f, 0.88f, 0.22f, 0.58f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                return;
            }

            transform.localScale = Vector3.one;
            RefreshVisual();
        }

        public void PlayAcceptedFeedback()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            PlayGlow(new Color(1f, 0.82f, 0.18f, Data != null && Data.IsGold ? 0.34f : 0.22f), 1.08f, 0.22f, true);
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
            visibleHeight = Mathf.Clamp(visibleHeight, 22f, 52f);
            if (useRevealHistoryLayout == value
                && Mathf.Approximately(revealHistoryVisibleHeight, visibleHeight)
                && showRevealHistoryTitle == showTitleInPeek)
            {
                return;
            }

            useRevealHistoryLayout = value;
            showRevealHistoryTitle = showTitleInPeek;
            revealHistoryVisibleHeight = visibleHeight;
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
                SetFooterBadgeVisible(false);

                if (outline != null)
                {
                    outline.effectColor = visualConfig != null ? visualConfig.BackOutlineColor : BackOutlineColor;
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

            if (backgroundImage != null)
            {
                Sprite faceSprite = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldCardSprite : visualConfig.NormalCardSprite)
                    : null;
                Color faceColor = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldCardColor : visualConfig.NormalCardColor)
                    : (Data.IsGold ? GoldColor : RegularColor);
                ApplyBackground(faceSprite, faceColor);
            }

            if (outline != null)
            {
                outline.effectColor = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldOutlineColor : visualConfig.NormalOutlineColor)
                    : (Data.IsGold ? GoldOutlineColor : RegularOutlineColor);
            }

            if (titleLabel != null)
            {
                ApplyConfiguredFont(titleLabel);
                titleLabel.color = visualConfig != null
                    ? (Data.IsGold ? visualConfig.GoldTextColor : visualConfig.TextColor)
                    : new Color(0.26f, 0.2f, 0.16f, 1f);
                EnsureTextFontSupports(titleLabel, Data.DisplayText);
                titleLabel.text = showTitle && !Data.IsImageCard ? Data.DisplayText : string.Empty;
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
                string footerText = showFooter && !useRevealHistoryLayout && shouldShowProgress
                    ? $"{progressSource.CurrentCount}/{progressSource.RequiredCount}"
                    : string.Empty;
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

            backgroundImage.sprite = sprite;
            backgroundImage.color = sprite != null ? Color.white : fallbackColor;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;

            if (glowImage != null)
            {
                glowImage.sprite = sprite;
                glowImage.type = Image.Type.Simple;
                glowImage.preserveAspect = false;
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
            if (runtimeCyrillicFontAsset != null)
            {
                return runtimeCyrillicFontAsset;
            }

            Font sourceFont = Resources.Load<Font>(RuntimeFontResourcePath);
            if (sourceFont == null)
            {
                sourceFont = LoadBuiltinRuntimeFont();
            }

            if (sourceFont == null)
            {
                return null;
            }

            runtimeCyrillicFontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (runtimeCyrillicFontAsset != null)
            {
                runtimeCyrillicFontAsset.name = $"Runtime_{sourceFont.name}_CardCyrillic";
            }

            return runtimeCyrillicFontAsset;
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
            bool isGoldCard = Data != null && Data.IsGold && !useRevealHistoryLayout;
            bool isImageCard = Data != null && Data.IsImageCard && Data.Icon != null && !useRevealHistoryLayout;
            bool isGoalSlotCard = Container is GoalSlot && !useRevealHistoryLayout;

            if (titleRoot != null)
            {
                titleRoot.gameObject.SetActive(showTitle && !isImageCard && (!useRevealHistoryLayout || showRevealHistoryTitle));

                if (useRevealHistoryLayout)
                {
                    titleRoot.anchorMin = new Vector2(0f, 1f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 1f);
                    titleRoot.sizeDelta = new Vector2(-16f, revealHistoryVisibleHeight);
                    titleRoot.anchoredPosition = new Vector2(0f, -2f);
                }
                else if (isGoldCard)
                {
                    titleRoot.anchorMin = new Vector2(0f, 0f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 0.5f);
                    titleRoot.sizeDelta = new Vector2(-24f, -64f);
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
                    titleRoot.sizeDelta = new Vector2(-24f, -64f);
                    titleRoot.anchoredPosition = Vector2.zero;
                }
            }

            if (titleLabel != null)
            {
                if (useRevealHistoryLayout && !showRevealHistoryTitle)
                {
                    titleLabel.text = string.Empty;
                }

                titleLabel.alignment = useRevealHistoryLayout && showRevealHistoryTitle
                    ? TextAlignmentOptions.Center
                    : isGoldCard
                    ? TextAlignmentOptions.Center
                    : TextAlignmentOptions.Center;
                float fontSizeMax = visualConfig != null ? visualConfig.TitleFontSize : 34f;
                float fontSizeMin = visualConfig != null ? visualConfig.TitleFontSizeMin : 18f;
                float peekFontSize = Mathf.Clamp(revealHistoryVisibleHeight * 0.58f, 13f, 19f);
                titleLabel.fontSizeMax = useRevealHistoryLayout
                    ? Mathf.Min(peekFontSize, fontSizeMax)
                    : isGoldCard ? Mathf.Min(28f, fontSizeMax) : isImageCard ? Mathf.Min(20f, fontSizeMax) : fontSizeMax;
                titleLabel.fontSizeMin = useRevealHistoryLayout
                    ? Mathf.Min(11f, fontSizeMin)
                    : isGoldCard ? Mathf.Min(18f, fontSizeMin) : isImageCard ? Mathf.Min(11f, fontSizeMin) : fontSizeMin;
                titleLabel.enableWordWrapping = !useRevealHistoryLayout && !isGoldCard && !isImageCard;
                titleLabel.overflowMode = useRevealHistoryLayout || isGoldCard || isImageCard
                    ? TextOverflowModes.Ellipsis
                    : TextOverflowModes.Overflow;
            }

            if (footerRoot != null)
            {
                footerRoot.gameObject.SetActive(showFooter && !useRevealHistoryLayout);
                if (showFooter && !useRevealHistoryLayout)
                {
                    footerRoot.anchorMin = new Vector2(1f, 1f);
                    footerRoot.anchorMax = new Vector2(1f, 1f);
                    footerRoot.pivot = new Vector2(1f, 1f);
                    footerRoot.sizeDelta = isGoldCard || isGoalSlotCard
                        ? new Vector2(GoldProgressWidth, GoldHeaderHeight)
                        : new Vector2(78f, 30f);
                    footerRoot.anchoredPosition = isGoldCard || isGoalSlotCard
                        ? new Vector2(-GoldHeaderSidePadding, -GoldHeaderTopPadding)
                        : new Vector2(-10f, -10f);

                    SetFooterBadgeVisible(false);
                }
            }

            if (footerLabel != null)
            {
                footerLabel.alignment = isGoldCard || isGoalSlotCard
                    ? TextAlignmentOptions.Right
                    : TextAlignmentOptions.TopRight;
                footerLabel.enableWordWrapping = false;
                footerLabel.overflowMode = TextOverflowModes.Ellipsis;
                if (isGoldCard || isGoalSlotCard)
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
            if (gameManager == null || !gameManager.BeginDrag(this))
            {
                return;
            }

            global::MusicPlayer.Instance?.PlayCardPick();
            gameManager.BeginDragVisual(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!gameManager.IsDragging(this))
            {
                return;
            }

            gameManager.UpdateDragVisual(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            gameManager?.EndDrag(this, eventData);
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
            if (glowImage == null)
            {
                return;
            }

            glowImage.enabled = false;
            glowImage.color = new Color(1f, 0.82f, 0.18f, 0f);
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

            shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
            shadow.effectDistance = new Vector2(0f, -7f);
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
