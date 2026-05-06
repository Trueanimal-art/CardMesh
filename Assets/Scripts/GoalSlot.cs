using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class GoalSlot : MonoBehaviour, ICardContainer, IDropHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private CardVisualConfig visualConfig;

        private readonly List<CardView> cards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        public int CardCount => cards.Count;
        public CardView TopCard => cards.Count > 0 ? cards[^1] : null;
        public CardView GoalCard => cards.Count > 0 ? cards[0] : null;
        public int RegularCardsCount => Mathf.Max(0, cards.Count - 1);
        public Transform CardsRoot => cardsRoot != null ? cardsRoot : transform;
        public RectTransform DropAreaRect => CardsRoot as RectTransform;

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

        public void SetVisualConfig(CardVisualConfig config)
        {
            visualConfig = config;
            ApplySlotVisual();
        }

        public void ApplyLayout(ColumnLayoutProfile profile)
        {
            cardSize = profile.cardSize;

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

            RefreshLayout();
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

        public void ClearCards()
        {
            cards.Clear();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            int visibleCardIndex = cards.Count > 1 ? cards.Count - 1 : 0;
            for (int index = 0; index < cards.Count; index++)
            {
                CardView cardView = cards[index];
                bool isVisibleCard = index == visibleCardIndex;
                RectTransform cardRect = cardView.RectTransform;
                cardRect.SetParent(CardsRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.SetSiblingIndex(isVisibleCard ? cards.Count - 1 : index);
                cardView.SetSize(cardSize);
                cardView.SetFaceUp(true);
                cardView.SetVisible(isVisibleCard);
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

        private void ApplySlotVisual()
        {
            if (backgroundImage == null)
            {
                return;
            }

            Sprite slotSprite = visualConfig != null ? visualConfig.GoalSlotEmblemSprite : null;
            backgroundImage.enabled = true;
            backgroundImage.sprite = slotSprite;
            backgroundImage.color = slotSprite != null
                ? visualConfig.GoalSlotColor
                : new Color(0.25f, 0.32f, 0.28f, 0.35f);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }
    }
}
