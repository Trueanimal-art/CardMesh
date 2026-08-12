using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class DrawSlot : MonoBehaviour, ICardContainer, IPointerClickHandler
    {
        private const int MaxVisibleRevealedCards = 3;
        private const float RevealedCardsBaseOffsetXFactor = -1.30f;
        // Keep the three-card reveal fan visibly above the crown targets without pushing it into the top HUD.
        private const float RevealedCardsBaseOffsetY = 40f;
        private const float RevealedCardsStepX = 12f;
        private const float RevealedCardsStepY = -20f;
        private const string EmptySlotRootName = "DeckEmptySlot";

        [SerializeField] private Image backgroundImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private CardVisualConfig visualConfig;
        [SerializeField] private RectTransform deckFaceClipRoot;
        [SerializeField] private Image deckFaceClipMaskImage;
        [SerializeField] private Image deckFaceImage;
        [SerializeField] private Mask deckFaceMask;

        private static Sprite roundedEmptySlotSprite;

        private readonly List<CardView> queuedCards = new();
        private readonly List<CardView> revealedCards = new();

        private GameManager gameManager;
        private Vector2 cardSize;
        private RectTransform emptySlotRoot;

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
                revealedCard.SetDrawHistoryTitleLayout(false);
                revealedCard.SetPlayerStackedOnPrevious(false);
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
                    cardView.SetDrawHistoryTitleLayout(false);
                    cardView.SetPlayerStackedOnPrevious(false);
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
            nextCard.SetPlayerStackedOnPrevious(false);
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
            cardView.SetPlayerStackedOnPrevious(false);
            cardView.BindToContainer(this);
            cardView.transform.SetParent(CardRoot, false);
            cardView.SetSize(cardSize);
            cardView.SetRevealHistoryLayout(false);
            cardView.SetDrawHistoryTitleLayout(false);
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
                    cardView.SetDrawHistoryTitleLayout(false);
                    cardView.SetPlayerStackedOnPrevious(false);
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
                    cardView.SetPlayerStackedOnPrevious(false);
                    cardView.BindToContainer(this);
                    cardView.transform.SetParent(CardRoot, false);
                    cardView.SetDragDetached(false);
                    cardView.SetSize(cardSize);
                    cardView.SetDrawHistoryTitleLayout(false);
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
            foreach (CardView cardView in queuedCards)
            {
                cardView?.SetPlayerStackedOnPrevious(false);
            }

            foreach (CardView cardView in revealedCards)
            {
                cardView?.SetPlayerStackedOnPrevious(false);
            }

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
                    cardView.SetDrawHistoryTitleLayout(false);
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
                cardView.SetRevealHistoryLayout(false);
                cardView.SetDrawHistoryTitleLayout(!isTopRevealedCard);
                cardView.SetFaceUp(true);
                cardView.SetVisible(true);
                cardView.SetRaycastBlocks(isTopRevealedCard);
            }

            if (backgroundImage != null)
            {
                backgroundImage.enabled = true;
                ApplyDeckBackVisual();
            }

            RefreshEmptySlotVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            gameManager?.ClearTouchSelection();
            gameManager?.DrawNextStockCard();
        }

        private void RefreshCounter()
        {
            if (countLabel != null)
            {
                bool hasQueuedCards = queuedCards.Count > 0;
                countLabel.text = queuedCards.Count.ToString();
                countLabel.gameObject.SetActive(hasQueuedCards);

                Transform badge = countLabel.transform.parent != null
                    ? countLabel.transform.parent.Find("DeckCounterBadgeBg")
                    : null;
                if (badge != null)
                {
                    badge.gameObject.SetActive(hasQueuedCards);
                }

                if (hasQueuedCards)
                {
                    countLabel.transform.SetAsLastSibling();
                }
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
                cardView.SetDrawHistoryTitleLayout(false);
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

            if (queuedCards.Count <= 0)
            {
                SetDeckFaceVisible(false);
                backgroundImage.sprite = GetRoundedEmptySlotSprite();
                backgroundImage.color = new Color(0.02f, 0.21f, 0.12f, 0.64f);
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.preserveAspect = false;
                return;
            }

            Sprite cardBackSprite = visualConfig != null ? visualConfig.CardBackSprite : null;
            backgroundImage.sprite = CardView.GetRoundedCardSprite();
            backgroundImage.color = cardBackSprite != null
                ? Color.white
                : visualConfig != null ? visualConfig.CardBackColor : new Color(0.2f, 0.62f, 0.98f, 1f);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.preserveAspect = false;
            ApplyDeckFaceSprite(cardBackSprite);
        }

        private void ApplyDeckFaceSprite(Sprite sprite)
        {
            EnsureDeckFaceClip();
            if (deckFaceClipRoot == null || deckFaceImage == null)
            {
                return;
            }

            bool isVisible = sprite != null;
            deckFaceClipRoot.gameObject.SetActive(isVisible);
            deckFaceImage.enabled = isVisible;
            deckFaceImage.sprite = sprite;
            deckFaceImage.color = Color.white;
            deckFaceImage.type = Image.Type.Simple;
            deckFaceImage.preserveAspect = false;
        }

        private void SetDeckFaceVisible(bool isVisible)
        {
            if (deckFaceClipRoot != null)
            {
                deckFaceClipRoot.gameObject.SetActive(isVisible);
            }

            if (deckFaceImage != null)
            {
                deckFaceImage.enabled = isVisible;
            }
        }

        private void EnsureDeckFaceClip()
        {
            if (backgroundImage == null || transform is not RectTransform deckRect)
            {
                return;
            }

            if (deckFaceClipRoot == null)
            {
                Transform existingClip = deckRect.Find("DeckFaceClip");
                if (existingClip is RectTransform existingClipRect)
                {
                    deckFaceClipRoot = existingClipRect;
                    deckFaceClipMaskImage = existingClip.GetComponent<Image>();
                    deckFaceMask = existingClip.GetComponent<Mask>();
                }
            }

            if (deckFaceClipRoot == null)
            {
                GameObject clipObject = new("DeckFaceClip", typeof(RectTransform), typeof(Image), typeof(Mask));
                deckFaceClipRoot = clipObject.GetComponent<RectTransform>();
                deckFaceClipRoot.SetParent(deckRect, false);
                deckFaceClipMaskImage = clipObject.GetComponent<Image>();
                deckFaceMask = clipObject.GetComponent<Mask>();
            }

            StretchToParent(deckFaceClipRoot, Vector2.zero, Vector2.zero);
            deckFaceClipRoot.SetAsFirstSibling();

            if (deckFaceClipMaskImage != null)
            {
                deckFaceClipMaskImage.sprite = CardView.GetRoundedCardSprite();
                deckFaceClipMaskImage.type = Image.Type.Sliced;
                deckFaceClipMaskImage.color = Color.white;
                deckFaceClipMaskImage.raycastTarget = false;
            }

            if (deckFaceMask != null)
            {
                deckFaceMask.showMaskGraphic = false;
            }

            if (deckFaceImage == null)
            {
                Transform existingFace = deckFaceClipRoot.Find("DeckFaceImage");
                deckFaceImage = existingFace != null ? existingFace.GetComponent<Image>() : null;
            }

            if (deckFaceImage == null)
            {
                GameObject faceObject = new("DeckFaceImage", typeof(RectTransform), typeof(Image));
                RectTransform faceRect = faceObject.GetComponent<RectTransform>();
                faceRect.SetParent(deckFaceClipRoot, false);
                deckFaceImage = faceObject.GetComponent<Image>();
            }

            StretchToParent(deckFaceImage.rectTransform, Vector2.zero, Vector2.zero);
            deckFaceImage.raycastTarget = false;
        }

        private void RefreshEmptySlotVisual()
        {
            EnsureEmptySlotVisual();

            if (emptySlotRoot == null)
            {
                return;
            }

            bool isEmptyStock = queuedCards.Count <= 0;
            emptySlotRoot.gameObject.SetActive(isEmptyStock);
            if (isEmptyStock)
            {
                emptySlotRoot.SetAsFirstSibling();
            }
        }

        private void EnsureEmptySlotVisual()
        {
            if (backgroundImage == null || transform is not RectTransform deckRect)
            {
                return;
            }

            if (emptySlotRoot == null)
            {
                Transform existing = deckRect.Find(EmptySlotRootName);
                emptySlotRoot = existing as RectTransform;
            }

            if (emptySlotRoot == null)
            {
                GameObject rootObject = new(EmptySlotRootName, typeof(RectTransform));
                emptySlotRoot = rootObject.GetComponent<RectTransform>();
                emptySlotRoot.SetParent(deckRect, false);
            }

            StretchToParent(emptySlotRoot, Vector2.zero, Vector2.zero);

            Image rim = EnsureLayer("DeckEmptySlotGoldRim", Vector2.zero, Vector2.zero);
            rim.sprite = GetRoundedEmptySlotSprite();
            rim.type = Image.Type.Sliced;
            rim.color = new Color(0.95f, 0.66f, 0.16f, 0.42f);

            Image baseImage = EnsureLayer("DeckEmptySlotBase", new Vector2(4f, 4f), new Vector2(-4f, -4f));
            baseImage.sprite = GetRoundedEmptySlotSprite();
            baseImage.type = Image.Type.Sliced;
            baseImage.color = new Color(0.02f, 0.18f, 0.1f, 0.72f);

            Image innerShadow = EnsureLayer("DeckEmptySlotInnerShadow", new Vector2(8f, 2f), new Vector2(-8f, -2f));
            innerShadow.sprite = GetRoundedEmptySlotSprite();
            innerShadow.type = Image.Type.Sliced;
            innerShadow.color = new Color(0f, 0.03f, 0.01f, 0.46f);

            Image innerPlate = EnsureLayer("DeckEmptySlotInnerPlate", new Vector2(12f, 10f), new Vector2(-12f, -10f));
            innerPlate.sprite = GetRoundedEmptySlotSprite();
            innerPlate.type = Image.Type.Sliced;
            innerPlate.color = new Color(0.03f, 0.29f, 0.15f, 0.38f);

            Image topGlint = EnsureLayer("DeckEmptySlotTopGlint", new Vector2(14f, 226f), new Vector2(-14f, -14f));
            topGlint.sprite = GetRoundedEmptySlotSprite();
            topGlint.type = Image.Type.Sliced;
            topGlint.color = new Color(1f, 0.88f, 0.34f, 0.18f);

            Image bottomShade = EnsureLayer("DeckEmptySlotBottomShade", new Vector2(14f, 14f), new Vector2(-14f, -220f));
            bottomShade.sprite = GetRoundedEmptySlotSprite();
            bottomShade.type = Image.Type.Sliced;
            bottomShade.color = new Color(0f, 0f, 0f, 0.22f);
        }

        private Image EnsureLayer(string layerName, Vector2 offsetMin, Vector2 offsetMax)
        {
            Transform existing = emptySlotRoot.Find(layerName);
            RectTransform rectTransform;
            Image image;
            if (existing == null)
            {
                GameObject layerObject = new(layerName, typeof(RectTransform), typeof(Image));
                rectTransform = layerObject.GetComponent<RectTransform>();
                rectTransform.SetParent(emptySlotRoot, false);
                image = layerObject.GetComponent<Image>();
            }
            else
            {
                rectTransform = (RectTransform)existing;
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
            }

            StretchToParent(rectTransform, offsetMin, offsetMax);
            image.raycastTarget = false;
            return image;
        }

        private static void StretchToParent(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private static Sprite GetRoundedEmptySlotSprite()
        {
            if (roundedEmptySlotSprite != null)
            {
                return roundedEmptySlotSprite;
            }

            const int width = 64;
            const int height = 96;
            const int radius = 8;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime_DeckEmptySlotRounded",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32 clear = new(255, 255, 255, 0);
            Color32 fill = new(255, 255, 255, 255);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dx = x < radius ? radius - x : x >= width - radius ? x - (width - radius - 1) : 0;
                    int dy = y < radius ? radius - y : y >= height - radius ? y - (height - radius - 1) : 0;
                    bool inside = dx == 0 && dy == 0 || (dx * dx) + (dy * dy) <= radius * radius;
                    pixels[(y * width) + x] = inside ? fill : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedEmptySlotSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            roundedEmptySlotSprite.name = "Runtime_DeckEmptySlotRoundedSprite";
            return roundedEmptySlotSprite;
        }
    }
}
