using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class DrawSlot : MonoBehaviour, ICardContainer, IPointerClickHandler
    {
        private const int MaxVisibleRevealedCards = 3;
        private const float RevealedCardsBaseOffsetXFactor = 1.08f;
        private const float RevealedCardsBaseOffsetY = 24f;
        private const float RevealedCardsStepX = 20f;
        private const float RevealedCardsStepY = -36f;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private CardVisualConfig visualConfig;

        private readonly List<CardView> queuedCards = new();
        private readonly List<CardView> revealedCards = new();

        private GameManager gameManager;
        private Vector2 cardSize;

        public int CardCount => queuedCards.Count + revealedCards.Count;
        public CardView TopCard => revealedCards.Count > 0 ? revealedCards[^1] : null;
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

        public void SetVisualConfig(CardVisualConfig config)
        {
            visualConfig = config;
            ApplyDeckBackVisual();
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
            foreach (CardView revealedCard in revealedCards)
            {
                if (revealedCard == null)
                {
                    continue;
                }

                revealedCard.SetRevealHistoryLayout(false);
                revealedCard.BindToContainer(null);
                revealedCard.gameObject.SetActive(false);
            }

            revealedCards.Clear();
            queuedCards.Clear();

            if (cardViews != null)
            {
                foreach (CardView cardView in cardViews)
                {
                    if (cardView == null)
                    {
                        continue;
                    }

                    cardView.SetRevealHistoryLayout(false);
                    cardView.BindToContainer(null);
                    cardView.gameObject.SetActive(false);
                    queuedCards.Add(cardView);
                }
            }

            RefreshLayout();
            RefreshCounter();
        }

        public bool DrawNextCard()
        {
            if (queuedCards.Count <= 0)
            {
                return false;
            }

            CardView nextCard = queuedCards[0];
            queuedCards.RemoveAt(0);

            if (nextCard == null)
            {
                RefreshLayout();
                RefreshCounter();
                return false;
            }

            Vector3 startPosition = backgroundImage != null
                ? backgroundImage.rectTransform.position
                : transform.position;

            nextCard.gameObject.SetActive(true);
            nextCard.BindToContainer(this);
            revealedCards.Add(nextCard);

            RefreshLayout();
            nextCard.AnimateFromWorldPosition(startPosition, gameManager != null ? gameManager.MoveDuration : 0f);
            RefreshCounter();
            return true;
        }

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            revealedCards.Add(cardView);
            cardView.BindToContainer(this);
            cardView.transform.SetParent(CardRoot, false);
            cardView.SetSize(cardSize);
            cardView.SetRevealHistoryLayout(false);
            RefreshLayout();
            RefreshCounter();
        }

        public void RemoveCard(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            if (!revealedCards.Remove(cardView))
            {
                return;
            }

            RefreshLayout();
            RefreshCounter();
        }

        public bool IsTopCard(CardView cardView)
        {
            return TopCard == cardView;
        }

        public bool CanAccept(CardView movingCard)
        {
            return false;
        }

        public void ClearCards()
        {
            queuedCards.Clear();
            revealedCards.Clear();
            RefreshLayout();
            RefreshCounter();
        }

        public void RefreshLayout()
        {
            int firstVisibleIndex = Mathf.Max(0, revealedCards.Count - MaxVisibleRevealedCards);
            Vector2 basePosition = ResolveBasePosition();

            for (int index = 0; index < revealedCards.Count; index++)
            {
                CardView cardView = revealedCards[index];
                if (cardView == null)
                {
                    continue;
                }

                bool isVisible = index >= firstVisibleIndex;
                cardView.gameObject.SetActive(isVisible);

                if (!isVisible)
                {
                    cardView.SetRevealHistoryLayout(false);
                    continue;
                }

                int visibleIndex = index - firstVisibleIndex;
                bool isTopRevealedCard = index == revealedCards.Count - 1;
                RectTransform cardRect = cardView.RectTransform;
                cardRect.SetParent(CardRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2(
                    basePosition.x + (visibleIndex * RevealedCardsStepX),
                    basePosition.y + (visibleIndex * RevealedCardsStepY));
                cardRect.SetAsLastSibling();
                cardView.SetSize(cardSize);
                cardView.SetRevealHistoryLayout(!isTopRevealedCard);
                cardView.SetFaceUp(true);
                cardView.SetVisible(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.enabled = queuedCards.Count > 0 || revealedCards.Count == 0;
                ApplyDeckBackVisual();
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

        private Vector2 ResolveBasePosition()
        {
            RectTransform drawRect = transform as RectTransform;
            RectTransform rootRect = CardRoot as RectTransform;

            if (drawRect == null || rootRect == null)
            {
                return new Vector2(cardSize.x * RevealedCardsBaseOffsetXFactor, RevealedCardsBaseOffsetY);
            }

            if (drawRect.parent == rootRect)
            {
                return new Vector2(
                    drawRect.anchoredPosition.x + (cardSize.x * RevealedCardsBaseOffsetXFactor),
                    drawRect.anchoredPosition.y + RevealedCardsBaseOffsetY);
            }

            return new Vector2(cardSize.x * RevealedCardsBaseOffsetXFactor, RevealedCardsBaseOffsetY);
        }

        private void ApplyDeckBackVisual()
        {
            if (backgroundImage == null)
            {
                return;
            }

            Sprite cardBackSprite = visualConfig != null ? visualConfig.CardBackSprite : null;
            backgroundImage.sprite = cardBackSprite;
            backgroundImage.color = cardBackSprite != null
                ? Color.white
                : visualConfig != null ? visualConfig.CardBackColor : new Color(0.2f, 0.62f, 0.98f, 1f);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }
    }
}
