using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color RegularColor = new(0.98f, 0.98f, 0.98f, 1f);
        private static readonly Color GoldColor = new(0.98f, 0.84f, 0.33f, 1f);
        private static readonly Color BackColor = new(0.2f, 0.62f, 0.98f, 1f);
        private static readonly Color RegularOutlineColor = new(0f, 0f, 0f, 0.12f);
        private static readonly Color GoldOutlineColor = new(0.55f, 0.38f, 0f, 0.35f);
        private static readonly Color BackOutlineColor = new(0.05f, 0.27f, 0.5f, 0.45f);

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Outline outline;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI footerLabel;
        [SerializeField] private Image iconImage;

        private GameManager gameManager;
        private RectTransform dragLayer;
        private bool isFaceUp = true;

        public RectTransform RectTransform { get; private set; }
        public CardData Data { get; private set; }
        public ICardContainer Container { get; private set; }
        public bool IsFaceUp => isFaceUp;

        public void ConfigureComponents(
            Image targetBackgroundImage,
            Outline targetOutline,
            LayoutElement targetLayoutElement,
            CanvasGroup targetCanvasGroup,
            TextMeshProUGUI targetTitleLabel,
            TextMeshProUGUI targetFooterLabel,
            Image targetIconImage)
        {
            backgroundImage = targetBackgroundImage;
            outline = targetOutline;
            layoutElement = targetLayoutElement;
            canvasGroup = targetCanvasGroup;
            titleLabel = targetTitleLabel;
            footerLabel = targetFooterLabel;
            iconImage = targetIconImage;
        }

        private void Awake()
        {
            RectTransform = (RectTransform)transform;
        }

        public void Setup(CardData data, GameManager owner, RectTransform dragParent)
        {
            Data = data;
            gameManager = owner;
            dragLayer = dragParent;
            RefreshVisual();
        }

        public void BindToContainer(ICardContainer container)
        {
            Container = container;
            RefreshVisual();
        }

        public void SetSize(Vector2 size)
        {
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = size.x;
                layoutElement.preferredHeight = size.y;
                layoutElement.minWidth = size.x;
                layoutElement.minHeight = size.y;
            }

            if (RectTransform == null)
            {
                RectTransform = (RectTransform)transform;
            }

            RectTransform.sizeDelta = size;
        }

        public void SetFaceUp(bool value)
        {
            isFaceUp = value;
            RefreshVisual();
        }

        public void SetVisible(bool value)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = value ? 1f : 0f;
                canvasGroup.blocksRaycasts = value;
                canvasGroup.interactable = value;
            }
        }

        public void RefreshVisual()
        {
            if (Data == null)
            {
                return;
            }

            if (!isFaceUp)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.color = BackColor;
                }

                if (outline != null)
                {
                    outline.effectColor = BackOutlineColor;
                }

                if (titleLabel != null)
                {
                    titleLabel.text = string.Empty;
                }

                if (footerLabel != null)
                {
                    footerLabel.text = string.Empty;
                }

                if (iconImage != null)
                {
                    iconImage.enabled = false;
                }

                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = Data.IsGold ? GoldColor : RegularColor;
            }

            if (outline != null)
            {
                outline.effectColor = Data.IsGold ? GoldOutlineColor : RegularOutlineColor;
            }

            if (titleLabel != null)
            {
                titleLabel.text = Data.DisplayText;
            }

            if (footerLabel != null)
            {
                footerLabel.text = Data.IsGold && Container is GoalSlot
                    ? $"{Data.CurrentCount}/{Data.RequiredCount}"
                    : string.Empty;
            }

            if (iconImage != null)
            {
                bool hasIcon = Data.Icon != null;
                iconImage.enabled = hasIcon;
                iconImage.sprite = Data.Icon;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gameManager == null || !gameManager.BeginDrag(this))
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            transform.SetParent(dragLayer, true);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!gameManager.IsDragging(this))
            {
                return;
            }

            RectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            gameManager?.EndDrag(this);
        }
    }
}
