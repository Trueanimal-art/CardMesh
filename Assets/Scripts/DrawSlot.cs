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
        private const float RevealedCardsBaseOffsetXFactor = -1.42f;
        private const float RevealedCardsBaseOffsetY = -8f;
        private const float RevealedCardsStepX = 12f;
        private const float RevealedCardsStepY = -42f;

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
        public bool HasDrawableCards => queuedCards.Count > 0 || revealedCards.Count > 0;
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

            if (transform is RectTransform rectTransform)
            {
                rectTransform.sizeDelta = cardSize;
            }

            NormalizeCardRoot();

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
                if (revealedCards.Count <= 0)
                {
                    return false;
                }

                RecycleRevealedCardsToStock();
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

        public List<CardView> GetQueuedCardsSnapshot()
        {
            return new List<CardView>(queuedCards);
        }

        public List<CardView> GetRevealedCardsSnapshot()
        {
            return new List<CardView>(revealedCards);
        }

        public void RestoreCards(IEnumerable<CardView> queuedSnapshot, IEnumerable<CardView> revealedSnapshot)
        {
            queuedCards.Clear();
            revealedCards.Clear();

            if (queuedSnapshot != null)
            {
                foreach (CardView cardView in queuedSnapshot)
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

            if (revealedSnapshot != null)
            {
                foreach (CardView cardView in revealedSnapshot)
                {
                    if (cardView == null)
                    {
                        continue;
                    }

                    cardView.gameObject.SetActive(true);
                    cardView.BindToContainer(this);
                    cardView.transform.SetParent(CardRoot, false);
                    cardView.SetDragDetached(false);
                    cardView.SetSize(cardSize);
                    revealedCards.Add(cardView);
                }
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
            NormalizeCardRoot();

            List<int> visibleRevealedIndices = new();
            for (int index = 0; index < revealedCards.Count; index++)
            {
                CardView cardView = revealedCards[index];
                if (cardView != null && !cardView.IsDragDetached)
                {
                    visibleRevealedIndices.Add(index);
                }
            }

            int firstVisiblePosition = Mathf.Max(0, visibleRevealedIndices.Count - MaxVisibleRevealedCards);
            int topVisibleIndex = visibleRevealedIndices.Count > 0 ? visibleRevealedIndices[^1] : -1;
            Vector2 basePosition = ResolveBasePosition();

            for (int index = 0; index < revealedCards.Count; index++)
            {
                CardView cardView = revealedCards[index];
                if (cardView == null)
                {
                    continue;
                }

                if (cardView.IsDragDetached)
                {
                    continue;
                }

                int visiblePosition = visibleRevealedIndices.IndexOf(index);
                bool isVisible = visiblePosition >= firstVisiblePosition;
                cardView.gameObject.SetActive(isVisible);

                if (!isVisible)
                {
                    cardView.SetRevealHistoryLayout(false);
                    continue;
                }

                int visibleIndex = visiblePosition - firstVisiblePosition;
                bool isTopRevealedCard = index == topVisibleIndex;
                RectTransform cardRect = cardView.RectTransform;
                cardRect.SetParent(CardRoot, false);
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.localScale = Vector3.one;
                cardRect.anchoredPosition = new Vector2(
                    basePosition.x + (visibleIndex * RevealedCardsStepX),
                    basePosition.y + (visibleIndex * RevealedCardsStepY));
                cardRect.SetAsLastSibling();
                cardView.SetSize(cardSize);
                cardView.SetRevealHistoryLayout(!isTopRevealedCard, Mathf.Abs(RevealedCardsStepY) + 8f, !isTopRevealedCard);
                cardView.SetFaceUp(true);
                cardView.SetVisible(true);
            }

            if (backgroundImage != null)
            {
                backgroundImage.enabled = HasDrawableCards;
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
                countLabel.transform.SetAsLastSibling();
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

        private void NormalizeCardRoot()
        {
            if (CardRoot is not RectTransform rootRect)
            {
                return;
            }

            rootRect.localScale = Vector3.one;
            rootRect.localRotation = Quaternion.identity;
        }

        private void RecycleRevealedCardsToStock()
        {
            foreach (CardView cardView in revealedCards)
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

            revealedCards.Clear();
            RefreshLayout();
            RefreshCounter();
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
