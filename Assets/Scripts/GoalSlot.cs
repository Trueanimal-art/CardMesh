using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class GoalSlot : MonoBehaviour, ICardContainer, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HeaderTopInset = 10f;
        private const float HeaderHeight = 34f;
        private const float HeaderProgressWidth = 58f;
        private const string RuntimeFontResourcePath = "Fonts/Roboto-Bold";
        private const float LayoutEpsilon = 0.01f;
        private static TMP_FontAsset headerFontAsset;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private RectTransform headerRoot;
        [SerializeField] private TextMeshProUGUI categoryTitleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private CardVisualConfig visualConfig;

        private readonly List<CardView> cards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private Color storedBackgroundColor;
        private bool hasStoredBackgroundColor;
        private Coroutine feedbackRoutine;
        public int CardCount => cards.Count;
        public CardView TopCard => cards.Count > 0 ? cards[^1] : null;
        public CardView GoalCard => cards.Count > 0 ? cards[0] : null;
        public int RegularCardsCount => Mathf.Max(0, cards.Count - 1);
        public int RemainingRegularCapacity => GoalCard != null && GoalCard.Data != null && GoalCard.Data.IsGold
            ? Mathf.Max(0, GoalCard.Data.RequiredCount - RegularCardsCount)
            : 0;
        public Transform CardsRoot => cardsRoot != null ? cardsRoot : transform;
        public RectTransform DropAreaRect => CardsRoot as RectTransform;

        public void ConfigureComponents(Image targetBackgroundImage, LayoutElement targetLayoutElement, Transform targetCardsRoot)
        {
            backgroundImage = targetBackgroundImage;
            layoutElement = targetLayoutElement;
            cardsRoot = targetCardsRoot;
            EnsureHeader();
        }

        public void Initialize(GameManager owner)
        {
            gameManager = owner;
        }

        public void SetVisualConfig(CardVisualConfig config)
        {
            visualConfig = config;
            ApplySlotVisual();
        }

        public void ApplyLayout(ColumnLayoutProfile profile)
        {
            cardSize = profile.cardSize;

            if (transform is RectTransform rectTransform)
            {
                rectTransform.sizeDelta = cardSize;
            }

            if (layoutElement != null)
            {
                layoutElement.preferredWidth = cardSize.x;
                layoutElement.minWidth = layoutElement.preferredWidth;
                layoutElement.preferredHeight = cardSize.y;
                layoutElement.minHeight = cardSize.y;
            }

            if (backgroundImage != null)
            {
                ApplySlotVisual();
            }

            NormalizeCardsRoot();
            RefreshLayout();
        }

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            AddCardWithoutRefresh(cardView);
            RefreshLayout();
        }

        public void AddCards(IEnumerable<CardView> cardViews)
        {
            if (cardViews == null)
            {
                return;
            }

            foreach (CardView cardView in cardViews)
            {
                AddCardWithoutRefresh(cardView);
            }

            RefreshLayout();
        }

        private void AddCardWithoutRefresh(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            cards.Add(cardView);
            cardView.BindToContainer(this);
            RectTransform cardRect = cardView.RectTransform;
            cardRect.SetParent(CardsRoot, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localPosition = new Vector3(0f, 0f, cardRect.localPosition.z);
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;
            cardView.SetSize(cardSize);
            cardView.SetRevealHistoryLayout(false);
            cardView.SetTextVisibility(true, true);
            cardView.SetRaycastBlocks(false);
        }

        public void RemoveCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            cards.Remove(cardView);
            RefreshLayout();
        }

        public bool IsTopCard(CardView cardView)
        {
            return TopCard == cardView;
        }

        public bool CanAccept(CardView movingCard)
        {
            if (movingCard == null || movingCard.Data == null)
            {
                return false;
            }

            if (cards.Count == 0)
            {
                return movingCard.Data.IsGold;
            }

            CardView goalCard = GoalCard;
            if (goalCard == null || !goalCard.Data.IsGold || movingCard.Data.IsGold)
            {
                return false;
            }

            return movingCard.Data.AssociationId == goalCard.Data.AssociationId;
        }

        public bool CanAcceptStack(IReadOnlyList<CardView> movingCards)
        {
            if (movingCards == null || movingCards.Count == 0)
            {
                return false;
            }

            CardView firstCard = movingCards[0];
            if (!CanAccept(firstCard))
            {
                return false;
            }

            if (firstCard.Data != null && firstCard.Data.IsGold)
            {
                return movingCards.Count == 1;
            }

            if (movingCards.Count > RemainingRegularCapacity)
            {
                return false;
            }

            string associationId = firstCard.Data != null ? firstCard.Data.AssociationId : string.Empty;
            foreach (CardView movingCard in movingCards)
            {
                if (movingCard == null
                    || movingCard.Data == null
                    || movingCard.Data.IsGold
                    || movingCard.Data.AssociationId != associationId)
                {
                    return false;
                }
            }

            return true;
        }

        public void ClearCards()
        {
            cards.Clear();
            RefreshLayout();
        }

        public void PlayAcceptFeedback(bool strong = false)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            StartFeedback(strong ? 1.08f : 1.045f, strong ? 0.32f : 0.2f, new Color(1f, 0.82f, 0.18f, strong ? 0.48f : 0.3f));
        }

        public void PlayCompleteFeedback()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            StartFeedback(1.12f, 0.34f, new Color(1f, 0.88f, 0.22f, 0.62f));
        }

        public void RefreshLayout()
        {
            NormalizeCardsRoot();
            EnsureHeader();

            RefreshHeader();

            for (int index = 0; index < cards.Count; index++)
            {
                CardView cardView = cards[index];
                bool isTopCard = index == cards.Count - 1;
                RectTransform cardRect = cardView.RectTransform;
                cardRect.SetParent(CardsRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.localScale = Vector3.one;
                Vector2 targetPosition = Vector2.zero;
                cardRect.anchoredPosition = targetPosition;
                cardRect.SetSiblingIndex(index);
                cardView.SetSize(cardSize);
                cardRect.anchoredPosition = targetPosition;
                cardRect.localPosition = new Vector3(0f, cardRect.localPosition.y, cardRect.localPosition.z);
                cardView.SetRevealHistoryLayout(false);
                cardView.SetTextVisibility(true, true);
                cardView.SetFaceUp(true);
                cardView.SetVisible(isTopCard);
                cardView.SetRaycastBlocks(false);
                LogLayoutFlow("RefreshLayout:card", cardView);
            }

            if (headerRoot != null)
            {
                headerRoot.SetAsLastSibling();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            SetHighlight(false);

            if (!IsPointerOverDropArea(eventData))
            {
                return;
            }

            gameManager?.TryMoveDraggedCardTo(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (gameManager != null && gameManager.CanDropDraggedCardTo(this))
            {
                SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private bool IsPointerOverDropArea(PointerEventData eventData)
        {
            RectTransform dropArea = CardsRoot as RectTransform;
            if (dropArea == null || eventData == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(dropArea, eventData.position, eventData.pressEventCamera);
        }

        private void ApplySlotVisual()
        {
            if (backgroundImage == null)
            {
                return;
            }

            Sprite slotSprite = visualConfig != null ? visualConfig.GoalSlotEmblemSprite : null;
            backgroundImage.enabled = true;
            backgroundImage.sprite = slotSprite;
            backgroundImage.raycastTarget = true;
            backgroundImage.color = slotSprite != null
                ? visualConfig.GoalSlotColor
                : new Color(0.25f, 0.32f, 0.28f, 0.35f);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;

            Shadow shadow = backgroundImage.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = backgroundImage.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        private void NormalizeCardsRoot()
        {
            if (CardsRoot is not RectTransform cardsRoot || cardsRoot == transform)
            {
                return;
            }

            RectTransform slotRect = transform as RectTransform;
            cardsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRoot.pivot = new Vector2(0.5f, 0.5f);
            cardsRoot.sizeDelta = cardSize.sqrMagnitude > 0f
                ? cardSize
                : slotRect != null ? slotRect.rect.size : cardsRoot.sizeDelta;
            cardsRoot.anchoredPosition = Vector2.zero;
            cardsRoot.localPosition = new Vector3(0f, 0f, cardsRoot.localPosition.z);
            cardsRoot.localScale = Vector3.one;
            cardsRoot.localRotation = Quaternion.identity;
            LogContainerLayout("NormalizeCardsRoot", slotRect, cardsRoot);
        }

        private void EnsureHeader()
        {
            if (headerRoot == null)
            {
                Transform existingHeader = transform.Find("HeaderRoot");
                headerRoot = existingHeader as RectTransform;
            }

            if (headerRoot == null)
            {
                GameObject headerObject = new("HeaderRoot", typeof(RectTransform));
                headerRoot = headerObject.GetComponent<RectTransform>();
                headerRoot.SetParent(transform, false);
            }

            RectTransform slotRect = transform as RectTransform;
            headerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            headerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            headerRoot.pivot = new Vector2(0.5f, 1f);
            float headerWidth = cardSize.x > 0f
                ? cardSize.x - 18f
                : slotRect != null ? slotRect.rect.width - 18f : 160f;
            headerRoot.sizeDelta = new Vector2(Mathf.Max(120f, headerWidth), HeaderHeight);
            headerRoot.anchoredPosition = new Vector2(0f, (cardSize.y * 0.5f) - HeaderTopInset);
            headerRoot.localScale = Vector3.one;
            headerRoot.localRotation = Quaternion.identity;

            categoryTitleText = EnsureHeaderText(categoryTitleText, "CategoryTitleText", TextAlignmentOptions.Center);
            progressText = EnsureHeaderText(progressText, "ProgressText", TextAlignmentOptions.Right);

            RectTransform titleRect = categoryTitleText != null ? categoryTitleText.rectTransform : null;
            if (titleRect != null)
            {
                titleRect.anchorMin = Vector2.zero;
                titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.offsetMin = new Vector2(8f, 0f);
                titleRect.offsetMax = new Vector2(-(HeaderProgressWidth + 8f), 0f);
                titleRect.localScale = Vector3.one;
            }

            RectTransform progressRect = progressText != null ? progressText.rectTransform : null;
            if (progressRect != null)
            {
                progressRect.anchorMin = new Vector2(1f, 0f);
                progressRect.anchorMax = Vector2.one;
                progressRect.pivot = new Vector2(1f, 0.5f);
                progressRect.sizeDelta = new Vector2(HeaderProgressWidth, 0f);
                progressRect.anchoredPosition = Vector2.zero;
                progressRect.localScale = Vector3.one;
            }

            headerRoot.SetAsLastSibling();
        }

        private TextMeshProUGUI EnsureHeaderText(TextMeshProUGUI current, string name, TextAlignmentOptions alignment)
        {
            if (current == null)
            {
                Transform existing = headerRoot != null ? headerRoot.Find(name) : null;
                current = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            }

            if (current == null)
            {
                GameObject textObject = new(name, typeof(RectTransform));
                textObject.transform.SetParent(headerRoot, false);
                current = textObject.AddComponent<TextMeshProUGUI>();
            }

            current.font = GetOrCreateHeaderFont();
            current.fontStyle = FontStyles.Bold;
            current.enableAutoSizing = true;
            current.fontSizeMax = 20f;
            current.fontSizeMin = 11f;
            current.enableWordWrapping = false;
            current.overflowMode = TextOverflowModes.Ellipsis;
            current.alignment = alignment;
            current.color = new Color(0.24f, 0.16f, 0.06f, 1f);
            current.raycastTarget = false;
            EnsureHeaderTextShadow(current);
            return current;
        }

        private static void EnsureHeaderTextShadow(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(1f, 0.92f, 0.64f, 0.72f);
            shadow.effectDistance = new Vector2(0f, -1.4f);
            shadow.useGraphicAlpha = true;
        }

        private void RefreshHeader()
        {
            if (headerRoot != null)
            {
                headerRoot.gameObject.SetActive(false);
            }

            if (categoryTitleText != null)
            {
                categoryTitleText.text = string.Empty;
            }

            if (progressText != null)
            {
                progressText.text = string.Empty;
            }
        }

        private static TMP_FontAsset GetOrCreateHeaderFont()
        {
            if (headerFontAsset != null)
            {
                return headerFontAsset;
            }

            Font sourceFont = Resources.Load<Font>(RuntimeFontResourcePath);
            if (sourceFont == null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            headerFontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (headerFontAsset != null)
            {
                headerFontAsset.name = $"Runtime_{sourceFont.name}_GoalHeader";
            }

            return headerFontAsset != null ? headerFontAsset : TMP_Settings.defaultFontAsset;
        }

        private void LogLayoutFlow(string reason, CardView cardView)
        {
#if UNITY_EDITOR
            if (cardView == null || cardView.RectTransform == null)
            {
                return;
            }

            RectTransform rect = cardView.RectTransform;
            if (Mathf.Abs(rect.anchoredPosition.x) <= LayoutEpsilon && Mathf.Abs(rect.localPosition.x) <= LayoutEpsilon)
            {
                return;
            }

            Debug.LogWarning(
                $"GoalSlot layout drift ({reason}): " +
                $"card={cardView.name}, parent={rect.parent?.name}, " +
                $"anchored={rect.anchoredPosition}, local={rect.localPosition}, " +
                $"sibling={rect.GetSiblingIndex()}, scale={rect.localScale}, " +
                $"anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, pivot={rect.pivot}");
#endif
        }

        private void LogContainerLayout(string reason, RectTransform crownSlot, RectTransform goalCardsRoot)
        {
#if UNITY_EDITOR
            if (crownSlot == null || goalCardsRoot == null)
            {
                return;
            }

            if (Mathf.Abs(goalCardsRoot.anchoredPosition.x) <= LayoutEpsilon && Mathf.Abs(goalCardsRoot.localPosition.x) <= LayoutEpsilon)
            {
                return;
            }

            Debug.LogWarning(
                $"GoalSlot container drift ({reason}): " +
                $"crown={crownSlot.name}, crownAnchored={crownSlot.anchoredPosition}, crownLocal={crownSlot.localPosition}, " +
                $"crownSize={crownSlot.sizeDelta}, crownAnchorMin={crownSlot.anchorMin}, crownAnchorMax={crownSlot.anchorMax}, " +
                $"crownPivot={crownSlot.pivot}, crownScale={crownSlot.localScale}; " +
                $"root={goalCardsRoot.name}, rootAnchored={goalCardsRoot.anchoredPosition}, rootLocal={goalCardsRoot.localPosition}, " +
                $"rootSize={goalCardsRoot.sizeDelta}, rootAnchorMin={goalCardsRoot.anchorMin}, rootAnchorMax={goalCardsRoot.anchorMax}, " +
                $"rootPivot={goalCardsRoot.pivot}, rootScale={goalCardsRoot.localScale}");
#endif
        }

        private void SetHighlight(bool value)
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (value)
            {
                if (!hasStoredBackgroundColor)
                {
                    storedBackgroundColor = backgroundImage.color;
                    hasStoredBackgroundColor = true;
                }

                backgroundImage.color = gameManager != null ? gameManager.HighlightColor : new Color(1f, 0.92f, 0.34f, 0.45f);
                return;
            }

            if (!hasStoredBackgroundColor)
            {
                return;
            }

            backgroundImage.color = storedBackgroundColor;
            hasStoredBackgroundColor = false;
        }

        private void StartFeedback(float peakScale, float duration, Color flashColor)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = StartCoroutine(FeedbackRoutine(peakScale, duration, flashColor));
        }

        private IEnumerator FeedbackRoutine(float peakScale, float duration, Color flashColor)
        {
            Color originalColor = backgroundImage != null ? backgroundImage.color : Color.white;
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * Mathf.Max(1f, peakScale);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);

                transform.localScale = Vector3.LerpUnclamped(originalScale, targetScale, wave);
                if (backgroundImage != null)
                {
                    backgroundImage.color = Color.Lerp(originalColor, flashColor, wave);
                }

                yield return null;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = originalColor;
            }

            transform.localScale = originalScale;
            RefreshLayout();
            feedbackRoutine = null;
        }
    }
}
