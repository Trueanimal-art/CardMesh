using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class CardColumn : MonoBehaviour, ICardContainer, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float FaceDownOffsetFactor = 0.65f;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private RectTransform blockedDropArea;

        private readonly List<CardView> cards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private float verticalOffset;
        private Color storedBackgroundColor;
        private bool hasStoredBackgroundColor;

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
            verticalOffset = profile.verticalOffset;

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

        public bool IsTopCard(CardView cardView)
        {
            return TopCard == cardView;
        }

        public bool CanAccept(CardView movingCard)
        {
            if (movingCard == null)
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
            float currentOffset = 0f;

            for (int index = 0; index < cards.Count; index++)
            {
                CardView card = cards[index];
                RectTransform cardRect = card.RectTransform;
                cardRect.SetParent(CardsRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 1f);
                cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.pivot = new Vector2(0.5f, 1f);
                cardRect.anchoredPosition = new Vector2(0f, -currentOffset);
                cardRect.SetSiblingIndex(index);
                card.SetSize(cardSize);
                bool isTopCard = index == cards.Count - 1;
                bool shouldFaceUp = isTopCard || card.IsFaceUp;
                card.SetFaceUp(shouldFaceUp);
                card.SetVisible(true);
                currentOffset += shouldFaceUp ? verticalOffset : verticalOffset * FaceDownOffsetFactor;
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
    }
}
