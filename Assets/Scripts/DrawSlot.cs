using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class DrawSlot : MonoBehaviour, ICardContainer, IPointerClickHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Transform cardRoot;

        private readonly List<CardView> queuedCards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private CardView visibleCard;

        public int CardCount => queuedCards.Count + (visibleCard != null ? 1 : 0);
        public CardView TopCard => visibleCard;
        public int QueuedCardsCount => queuedCards.Count;
        public Transform CardRoot => cardRoot != null ? cardRoot : transform;

        public void ConfigureComponents(Image targetBackgroundImage, LayoutElement targetLayoutElement, TMP_Text targetCountLabel, Transform targetCardRoot)
        {
            backgroundImage = targetBackgroundImage;
            layoutElement = targetLayoutElement;
            countLabel = targetCountLabel;
            cardRoot = targetCardRoot;
        }

        public void Initialize(GameManager owner)
        {
            gameManager = owner;
        }

        public void ApplyLayout(ColumnLayoutProfile profile)
        {
            cardSize = profile.cardSize;

            if (layoutElement != null)
            {
                layoutElement.preferredWidth = cardSize.x;
                layoutElement.minWidth = cardSize.x;
                layoutElement.preferredHeight = cardSize.y;
                layoutElement.minHeight = cardSize.y;
            }

            RefreshLayout();
            RefreshCounter();
        }

        public void SetStockCards(IEnumerable<CardView> cardViews)
        {
            if (visibleCard != null)
            {
                visibleCard.BindToContainer(null);
                visibleCard.gameObject.SetActive(false);
                visibleCard = null;
            }

            queuedCards.Clear();

            if (cardViews != null)
            {
                foreach (CardView cardView in cardViews)
                {
                    if (cardView == null)
                    {
                        continue;
                    }

                    cardView.BindToContainer(null);
                    cardView.gameObject.SetActive(false);
                    queuedCards.Add(cardView);
                }
            }

            RefreshLayout();
            RefreshCounter();
        }

        public void DrawNextCard()
        {
            if (visibleCard != null)
            {
                visibleCard.BindToContainer(null);
                visibleCard.gameObject.SetActive(false);
                queuedCards.Add(visibleCard);
                visibleCard = null;
            }

            if (queuedCards.Count > 0)
            {
                visibleCard = queuedCards[0];
                queuedCards.RemoveAt(0);
                visibleCard.gameObject.SetActive(true);
                visibleCard.BindToContainer(this);
            }

            RefreshLayout();
            RefreshCounter();
        }

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            visibleCard = cardView;
            cardView.BindToContainer(this);
            cardView.transform.SetParent(CardRoot, false);
            cardView.SetSize(cardSize);
            RefreshLayout();
            RefreshCounter();
        }

        public void RemoveCard(CardView cardView)
        {
            if (visibleCard != cardView)
            {
                return;
            }

            visibleCard = null;
            RefreshLayout();
            RefreshCounter();
        }

        public bool IsTopCard(CardView cardView)
        {
            return visibleCard == cardView;
        }

        public bool CanAccept(CardView movingCard)
        {
            return false;
        }

        public void ClearCards()
        {
            queuedCards.Clear();
            visibleCard = null;
            RefreshLayout();
            RefreshCounter();
        }

        public void RefreshLayout()
        {
            if (visibleCard != null)
            {
                RectTransform cardRect = visibleCard.RectTransform;
                cardRect.SetParent(CardRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.SetSiblingIndex(0);
                visibleCard.SetSize(cardSize);
                visibleCard.SetFaceUp(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.enabled = visibleCard == null || queuedCards.Count > 0;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            gameManager?.DrawNextStockCard();
        }

        private void RefreshCounter()
        {
            if (countLabel != null)
            {
                countLabel.text = queuedCards.Count.ToString();
            }
        }
    }
}
