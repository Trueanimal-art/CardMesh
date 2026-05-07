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

        private GameManager gameManager;
        private RectTransform dragLayer;
        private Coroutine feedbackCoroutine;
        private bool isFaceUp = true;
        private bool useRevealHistoryLayout;

        public RectTransform RectTransform { get; private set; }
        public CardData Data { get; private set; }
        public ICardContainer Container { get; private set; }
        public bool IsFaceUp => isFaceUp;

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
        }

        public void Setup(CardData data, GameManager owner, RectTransform dragParent)
        {
            Data = data;
            gameManager = owner;
            dragLayer = dragParent;
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

        public void SetRevealHistoryLayout(bool value)
        {
            if (useRevealHistoryLayout == value)
            {
                return;
            }

            useRevealHistoryLayout = value;
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

            if (!isFaceUp)
            {
                ApplyBackground(visualConfig != null ? visualConfig.CardBackSprite : null, visualConfig != null ? visualConfig.CardBackColor : BackColor);

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
                titleLabel.text = Data.DisplayText;
            }

            if (footerLabel != null)
            {
                CardData progressSource = Container is GoalSlot goalSlot
                    ? goalSlot.GoalCard?.Data
                    : Data;
                string footerText = !useRevealHistoryLayout && Container is GoalSlot && progressSource != null
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
                sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            if (titleRoot != null)
            {
                if (useRevealHistoryLayout)
                {
                    titleRoot.anchorMin = new Vector2(0f, 1f);
                    titleRoot.anchorMax = new Vector2(1f, 1f);
                    titleRoot.pivot = new Vector2(0.5f, 1f);
                    titleRoot.sizeDelta = new Vector2(-18f, 46f);
                    titleRoot.anchoredPosition = new Vector2(0f, -10f);
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
                titleLabel.alignment = useRevealHistoryLayout
                    ? TextAlignmentOptions.TopLeft
                    : TextAlignmentOptions.Center;
                float fontSizeMax = visualConfig != null ? visualConfig.TitleFontSize : 34f;
                float fontSizeMin = visualConfig != null ? visualConfig.TitleFontSizeMin : 18f;
                titleLabel.fontSizeMax = useRevealHistoryLayout ? Mathf.Min(24f, fontSizeMax) : fontSizeMax;
                titleLabel.fontSizeMin = useRevealHistoryLayout ? Mathf.Min(14f, fontSizeMin) : fontSizeMin;
            }

            if (footerRoot != null)
            {
                footerRoot.gameObject.SetActive(!useRevealHistoryLayout);
                if (!useRevealHistoryLayout)
                {
                    footerRoot.anchorMin = new Vector2(1f, 1f);
                    footerRoot.anchorMax = new Vector2(1f, 1f);
                    footerRoot.pivot = new Vector2(1f, 1f);
                    footerRoot.sizeDelta = new Vector2(78f, 30f);
                    footerRoot.anchoredPosition = Container is GoalSlot
                        ? new Vector2(-12f, -26f)
                        : new Vector2(-10f, -10f);
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gameManager == null || !gameManager.BeginDrag(this))
            {
                return;
            }

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
