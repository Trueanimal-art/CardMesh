using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class CardColumn : MonoBehaviour, ICardContainer, IDropHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;

        private readonly List<CardView> cards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private float verticalOffset;

        public int CardCount => cards.Count;
        public CardView TopCard => cards.Count > 0 ? cards[^1] : null;
        public Transform CardsRoot => cardsRoot != null ? cardsRoot : transform;

        public void ConfigureComponents(Image targetBackgroundImage, LayoutElement targetLayoutElement, Transform targetCardsRoot)
        {
            backgroundImage = targetBackgroundImage;
            layoutElement = targetLayoutElement;
            cardsRoot = targetCardsRoot;
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

        public void RemoveCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            cards.Remove(cardView);
            RefreshLayout();
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

        public void RefreshLayout()
        {
            bool keepCardUnderGoldFaceUp = cards.Count >= 2 && cards[^1].Data.IsGold;

            for (int index = 0; index < cards.Count; index++)
            {
                CardView card = cards[index];
                RectTransform cardRect = card.RectTransform;
                cardRect.SetParent(CardsRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 1f);
                cardRect.anchorMax = new Vector2(0.5f, 1f);
                cardRect.pivot = new Vector2(0.5f, 1f);
                cardRect.anchoredPosition = new Vector2(0f, -(index * verticalOffset));
                cardRect.SetSiblingIndex(index);
                card.SetSize(cardSize);
                bool isTopCard = index == cards.Count - 1;
                bool isCardUnderGold = keepCardUnderGoldFaceUp && index == cards.Count - 2;
                bool shouldStayFaceUp = card.IsFaceUp;
                card.SetFaceUp(isTopCard || isCardUnderGold || shouldStayFaceUp);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!IsPointerOverDropArea(eventData))
            {
                return;
            }

            gameManager?.TryMoveDraggedCardTo(this);
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
    }
}
