using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class CardColumn : MonoBehaviour, ICardContainer, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const float FaceDownOffsetFactor = 0.65f;
        private const float StackSpacingMultiplier = 1.2f;
        private const float MaxVisualStackOffset = 50f;
        private const float MinReadableStackOffset = 16f;
        private const float LinkedStackPeekHeight = 40f;
        private const float LinkedStackStepHeight = 40f;
        private const float PeekCornerMaskOverflow = 14f;
        private static readonly Color EmptyFrameFillColor = Color.white;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private RectTransform blockedDropArea;
        [SerializeField] private RectTransform emptyColumnFrame;

        private readonly List<CardView> cards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private float verticalOffset;

        public int CardCount => cards.Count;
        public CardView TopCard => cards.Count > 0 ? cards[^1] : null;
        public Transform CardsRoot => cardsRoot != null ? cardsRoot : transform;
        public RectTransform DropAreaRect => CardsRoot as RectTransform;
        public RectTransform BlockedDropAreaRect => blockedDropArea;

        public void ConfigureComponents(
            Image targetBackgroundImage,
            LayoutElement targetLayoutElement,
            Transform targetCardsRoot,
            RectTransform targetBlockedDropArea)
        {
            backgroundImage = targetBackgroundImage;
            layoutElement = targetLayoutElement;
            cardsRoot = targetCardsRoot;
            blockedDropArea = targetBlockedDropArea;
        }

        public void Initialize(GameManager owner)
        {
            gameManager = owner;
        }

        public void ApplyLayout(ColumnLayoutProfile profile)
        {
            cardSize = profile.cardSize;
            verticalOffset = Mathf.Min(profile.verticalOffset * StackSpacingMultiplier, MaxVisualStackOffset);

            if (layoutElement != null)
            {
                layoutElement.preferredWidth = cardSize.x;
                layoutElement.minWidth = cardSize.x;
                layoutElement.preferredHeight = cardSize.y + (verticalOffset * 4f);
                layoutElement.minHeight = layoutElement.preferredHeight;
            }

            RefreshLayout();
        }

        public void SetBackgroundAlpha(float alpha)
        {
            if (backgroundImage == null)
            {
                return;
            }

            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            cardView.SetPlayerStackedOnPrevious(false);
            cards.Add(cardView);
            cardView.BindToContainer(this);
            cardView.transform.SetParent(CardsRoot, false);
            cardView.SetSize(cardSize);
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
                if (cardView == null)
                {
                    continue;
                }

                cardView.SetPlayerStackedOnPrevious(false);
                cards.Add(cardView);
                cardView.BindToContainer(this);
                cardView.transform.SetParent(CardsRoot, false);
                cardView.SetSize(cardSize);
            }

            RefreshLayout();
        }

        public void AddCardsFromPlayerMove(IEnumerable<CardView> cardViews)
        {
            if (cardViews == null)
            {
                return;
            }

            List<CardView> movingCards = new();
            foreach (CardView cardView in cardViews)
            {
                if (cardView != null)
                {
                    movingCards.Add(cardView);
                }
            }

            if (movingCards.Count == 0)
            {
                return;
            }

            bool linksToExistingTop = TopCard != null;
            movingCards[0].SetPlayerStackedOnPrevious(linksToExistingTop);
            foreach (CardView cardView in movingCards)
            {
                cards.Add(cardView);
                cardView.BindToContainer(this);
                cardView.transform.SetParent(CardsRoot, false);
                cardView.SetSize(cardSize);
            }

            RefreshLayout();
        }

        public void RemoveCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            if (cards.Remove(cardView))
            {
                cardView.SetPlayerStackedOnPrevious(false);
                RefreshLayout();
            }
        }

        public List<CardView> RemoveStackStartingAt(CardView firstCard)
        {
            List<CardView> stack = GetMovableStack(firstCard);
            if (stack.Count == 0)
            {
                return stack;
            }

            foreach (CardView cardView in stack)
            {
                cards.Remove(cardView);
            }

            stack[0].SetPlayerStackedOnPrevious(false);
            RefreshLayout();
            return stack;
        }

        public List<CardView> GetMovableStack(CardView firstCard)
        {
            List<CardView> stack = new();
            if (firstCard == null || firstCard.Data == null || !firstCard.IsFaceUp)
            {
                return stack;
            }

            int firstIndex = cards.IndexOf(firstCard);
            if (firstIndex < 0)
            {
                return stack;
            }

            string associationId = firstCard.Data.AssociationId;
            bool expectsImageCard = firstCard.Data.IsGold
                ? firstCard.Data.IsImageGoal
                : firstCard.Data.IsImageCard;
            int stackStartIndex = firstIndex;

            if (firstCard.Data.IsGold)
            {
                if (firstIndex != cards.Count - 1)
                {
                    return stack;
                }

                while (stackStartIndex - 1 >= 0)
                {
                    CardView upperCard = cards[stackStartIndex];
                    if (upperCard == null || !upperCard.IsPlayerStackedOnPrevious)
                    {
                        break;
                    }

                    CardView previousCard = cards[stackStartIndex - 1];
                    if (!CanJoinRegularStack(previousCard, associationId, expectsImageCard))
                    {
                        break;
                    }

                    stackStartIndex--;
                }

                for (int index = stackStartIndex; index <= firstIndex; index++)
                {
                    CardView cardView = cards[index];
                    bool isGoldHead = index == firstIndex;
                    if (isGoldHead)
                    {
                        if (cardView == null
                            || !cardView.IsFaceUp
                            || cardView.Data == null
                            || !cardView.Data.IsGold
                            || cardView.Data.AssociationId != associationId
                            || cardView.Data.IsImageGoal != expectsImageCard)
                        {
                            stack.Clear();
                            return stack;
                        }
                    }
                    else if (!CanJoinRegularStack(cardView, associationId, expectsImageCard))
                    {
                        stack.Clear();
                        return stack;
                    }

                    stack.Add(cardView);
                }

                return stack;
            }

            while (stackStartIndex - 1 >= 0)
            {
                CardView upperCard = cards[stackStartIndex];
                if (upperCard == null || !upperCard.IsPlayerStackedOnPrevious)
                {
                    break;
                }

                CardView previousCard = cards[stackStartIndex - 1];
                if (!CanJoinRegularStack(previousCard, associationId, expectsImageCard))
                {
                    break;
                }

                stackStartIndex--;
            }

            for (int index = stackStartIndex; index < cards.Count; index++)
            {
                CardView cardView = cards[index];
                if (!CanJoinRegularStack(cardView, associationId, expectsImageCard)
                    || (index > stackStartIndex && !cardView.IsPlayerStackedOnPrevious))
                {
                    stack.Clear();
                    return stack;
                }

                stack.Add(cardView);
            }

            return stack;
        }

        private static bool CanJoinRegularStack(CardView cardView, string associationId, bool expectsImageCard)
        {
            return cardView != null
                && cardView.IsFaceUp
                && cardView.Data != null
                && !cardView.Data.IsGold
                && cardView.Data.AssociationId == associationId
                && cardView.Data.IsImageCard == expectsImageCard;
        }

        public CardView GetCardAt(int index)
        {
            return index >= 0 && index < cards.Count ? cards[index] : null;
        }

        public List<CardView> GetCardsSnapshot()
        {
            return new List<CardView>(cards);
        }

        public void RestoreCards(IEnumerable<CardView> cardViews)
        {
            cards.Clear();

            if (cardViews != null)
            {
                foreach (CardView cardView in cardViews)
                {
                    if (cardView == null)
                    {
                        continue;
                    }

                    cards.Add(cardView);
                    cardView.gameObject.SetActive(true);
                    cardView.BindToContainer(this);
                    cardView.transform.SetParent(CardsRoot, false);
                    cardView.SetDragDetached(false);
                    cardView.SetSize(cardSize);
                }
            }

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

            CardView topCard = TopCard;
            if (topCard == null)
            {
                return true;
            }

            if (topCard.Data == null || topCard.Data.IsGold)
            {
                return false;
            }

            if (topCard.Data.AssociationId != movingCard.Data.AssociationId)
            {
                return false;
            }

            bool movingUsesImages = movingCard.Data.IsGold
                ? movingCard.Data.IsImageGoal
                : movingCard.Data.IsImageCard;
            return topCard.Data.IsImageCard == movingUsesImages;
        }

        public void ClearCards()
        {
            foreach (CardView cardView in cards)
            {
                cardView?.SetPlayerStackedOnPrevious(false);
            }

            cards.Clear();
            RefreshLayout();
        }

        public void RevealOnlyTopCard()
        {
            for (int index = 0; index < cards.Count; index++)
            {
                CardView card = cards[index];
                if (card == null)
                {
                    continue;
                }

                card.SetFaceUp(index == cards.Count - 1);
                card.SetVisible(true);
            }

            RefreshLayout();
        }

        public void RefreshLayout()
        {
            RefreshLayout(true);
        }

        public void RefreshLayoutForDragPreview()
        {
            RefreshLayout(false);
        }

        private void RefreshLayout(bool revealTopCard)
        {
            RefreshEmptyColumnFrame();
            int topLayoutCardIndex = -1;

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                CardView card = cards[index];
                if (card != null && !card.IsDragDetached)
                {
                    topLayoutCardIndex = index;
                    break;
                }
            }

            float resolvedVerticalOffset = ResolveSafeVerticalOffset(topLayoutCardIndex, revealTopCard);
            float currentOffset = 0f;

            for (int index = 0; index < cards.Count; index++)
            {
                CardView card = cards[index];
                if (card == null || card.IsDragDetached)
                {
                    continue;
                }

                RectTransform cardRect = card.RectTransform;
                cardRect.SetParent(CardsRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 1f);
                cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.pivot = new Vector2(0.5f, 1f);
                cardRect.localScale = Vector3.one;
                cardRect.anchoredPosition = new Vector2(0f, -currentOffset);
                cardRect.SetSiblingIndex(index);
                card.SetSize(cardSize);
                bool isTopCard = index == topLayoutCardIndex;
                bool shouldFaceUp = (revealTopCard && isTopCard) || card.IsFaceUp;
                bool isPeekCard = !isTopCard;
                bool isLinkedStackPeek = IsLinkedStackPeek(index, topLayoutCardIndex);
                float stepHeight = isLinkedStackPeek
                    ? LinkedStackStepHeight
                    : shouldFaceUp ? resolvedVerticalOffset : resolvedVerticalOffset * FaceDownOffsetFactor;
                float visibleHeight = isLinkedStackPeek ? LinkedStackPeekHeight : stepHeight;
                float maskOverflowHeight = isPeekCard && index + 1 <= topLayoutCardIndex
                    ? PeekCornerMaskOverflow
                    : 0f;
                card.SetFaceUp(shouldFaceUp);
                card.SetRevealHistoryLayout(isPeekCard, visibleHeight, shouldFaceUp && isPeekCard, maskOverflowHeight);
                card.SetVisible(true);
                card.SetRaycastBlocks(true);
                currentOffset += stepHeight;
            }
        }

        private bool IsLinkedStackPeek(int index, int topLayoutCardIndex)
        {
            if (index < 0 || index >= topLayoutCardIndex || index + 1 >= cards.Count)
            {
                return false;
            }

            CardView nextCard = cards[index + 1];
            return nextCard != null
                && !nextCard.IsDragDetached
                && nextCard.IsPlayerStackedOnPrevious;
        }

        private void RefreshEmptyColumnFrame()
        {
            EnsureEmptyColumnFrame();
            if (emptyColumnFrame == null)
            {
                return;
            }

            emptyColumnFrame.gameObject.SetActive(cards.Count == 0);
            emptyColumnFrame.anchorMin = new Vector2(0.5f, 1f);
            emptyColumnFrame.anchorMax = new Vector2(0.5f, 1f);
            emptyColumnFrame.pivot = new Vector2(0.5f, 1f);
            emptyColumnFrame.anchoredPosition = Vector2.zero;
            emptyColumnFrame.sizeDelta = cardSize;
            emptyColumnFrame.localScale = Vector3.one;
            emptyColumnFrame.SetAsFirstSibling();
        }

        private void EnsureEmptyColumnFrame()
        {
            if (CardsRoot == null)
            {
                return;
            }

            if (emptyColumnFrame == null)
            {
                Transform existing = CardsRoot.Find("EmptyColumnFrame");
                if (existing is RectTransform existingRect)
                {
                    emptyColumnFrame = existingRect;
                }
                else
                {
                    GameObject frameObject = new("EmptyColumnFrame", typeof(RectTransform), typeof(Image));
                    emptyColumnFrame = frameObject.GetComponent<RectTransform>();
                    emptyColumnFrame.SetParent(CardsRoot, false);
                }
            }

            Image image = emptyColumnFrame.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.EmptyColumnSlot;
                image.type = Image.Type.Sliced;
                image.material = null;
                image.color = EmptyFrameFillColor;
                image.raycastTarget = false;
            }

            CanvasGroup group = emptyColumnFrame.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            Outline outline = emptyColumnFrame.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0f, 0f, 0f, 0f);
                outline.effectDistance = Vector2.zero;
                outline.useGraphicAlpha = true;
            }
        }

        private float ResolveSafeVerticalOffset(int topLayoutCardIndex, bool revealTopCard)
        {
            if (cards.Count <= 1)
            {
                return verticalOffset;
            }

            float availableHeight = ResolveAvailableStackHeight();
            float offsetBudget = Mathf.Max(0f, availableHeight - cardSize.y);
            if (offsetBudget <= 0f)
            {
                return 0f;
            }

            float weightedSteps = 0f;
            for (int index = 0; index < cards.Count - 1; index++)
            {
                CardView card = cards[index];
                if (card == null || card.IsDragDetached)
                {
                    continue;
                }

                if (IsLinkedStackPeek(index, topLayoutCardIndex))
                {
                    weightedSteps += LinkedStackStepHeight / Mathf.Max(1f, verticalOffset);
                    continue;
                }

                bool shouldFaceUp = (revealTopCard && index == topLayoutCardIndex) || card.IsFaceUp;
                weightedSteps += shouldFaceUp ? 1f : FaceDownOffsetFactor;
            }

            if (weightedSteps <= 0f)
            {
                return verticalOffset;
            }

            float safeOffset = offsetBudget / weightedSteps;
            if (safeOffset < MinReadableStackOffset)
            {
                return Mathf.Max(6f, safeOffset);
            }

            return Mathf.Min(safeOffset, verticalOffset);
        }

        private float ResolveAvailableStackHeight()
        {
            RectTransform rootRect = CardsRoot as RectTransform;
            if (rootRect != null && rootRect.rect.height > cardSize.y)
            {
                return rootRect.rect.height;
            }

            RectTransform ownRect = transform as RectTransform;
            if (ownRect != null && ownRect.rect.height > cardSize.y)
            {
                return ownRect.rect.height;
            }

            return cardSize.y + (verticalOffset * 5f);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                gameManager?.TryMoveSelectedCardTo(this);
            }
        }

        private bool IsPointerOverDropArea(PointerEventData eventData)
        {
            RectTransform dropArea = CardsRoot as RectTransform;
            if (dropArea == null || eventData == null)
            {
                return false;
            }

            if (blockedDropArea != null
                && RectTransformUtility.RectangleContainsScreenPoint(blockedDropArea, eventData.position, eventData.pressEventCamera))
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(dropArea, eventData.position, eventData.pressEventCamera);
        }

        private void SetHighlight(bool value)
        {
            if (backgroundImage == null)
            {
                return;
            }

            Color color = backgroundImage.color;
            color.r = value ? 1f : color.r;
            color.g = value ? 0.84f : color.g;
            color.b = value ? 0.22f : color.b;
            color.a = value ? 0.28f : 0f;
            backgroundImage.color = color;
        }
    }
}
