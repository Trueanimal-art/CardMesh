using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class CardColumn : MonoBehaviour, ICardContainer, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float FaceDownOffsetFactor = 0.65f;
        private const float StackSpacingMultiplier = 1.2f;
        private const float MaxVisualStackOffset = 50f;
        private const float MinReadableStackOffset = 16f;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private RectTransform blockedDropArea;

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

            cards.Remove(cardView);
            RefreshLayout();
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

            RefreshLayout();
            return stack;
        }

        public List<CardView> GetMovableStack(CardView firstCard)
        {
            List<CardView> stack = new();
            int firstIndex = cards.IndexOf(firstCard);
            if (firstIndex < 0)
            {
                return stack;
            }

            for (int index = firstIndex; index < cards.Count; index++)
            {
                CardView cardView = cards[index];
                if (cardView == null || !cardView.IsFaceUp || cardView.Data == null || cardView.Data.IsGold)
                {
                    stack.Clear();
                    return stack;
                }

                if (index > firstIndex && cardView.Data.AssociationId != firstCard.Data.AssociationId)
                {
                    stack.Clear();
                    return stack;
                }

                stack.Add(cardView);
            }

            return stack;
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
            if (movingCard == null || movingCard.Data == null || movingCard.Data.IsGold)
            {
                return false;
            }

            CardView topCard = TopCard;
            if (topCard == null)
            {
                return true;
            }

            if (topCard.Data.IsGold)
            {
                return false;
            }

            return topCard.Data.AssociationId == movingCard.Data.AssociationId;
        }

        public void ClearCards()
        {
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
            int topLayoutCardIndex = -1;

            for (int index = cards.Count - 1; index >= 0; index--)
            {
                CardView card = cards[index];
                if (card != null)
                {
                    topLayoutCardIndex = index;
                    break;
                }
            }

            float resolvedVerticalOffset = ResolveSafeVerticalOffset(topLayoutCardIndex);
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
                bool shouldFaceUp = isTopCard || card.IsFaceUp;
                card.SetFaceUp(shouldFaceUp);
                card.SetRevealHistoryLayout(shouldFaceUp && !isTopCard, resolvedVerticalOffset, shouldFaceUp && !isTopCard);
                card.SetVisible(true);
                card.SetRaycastBlocks(true);
                currentOffset += shouldFaceUp ? resolvedVerticalOffset : resolvedVerticalOffset * FaceDownOffsetFactor;
            }
        }

        private float ResolveSafeVerticalOffset(int topLayoutCardIndex)
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

                bool shouldFaceUp = index == topLayoutCardIndex || card.IsFaceUp;
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
        }
    }
}
