using System;
using UnityEngine;

namespace CardMesh
{
    public enum CardType
    {
        Text = 0,
        Image = 1
    }

    [Serializable]
    public sealed class CardData
    {
        [SerializeField] private string id = "card_id";
        [SerializeField] private string title = "Новая карта";
        [SerializeField] private string imageKey;
        [SerializeField] private CardType cardType = CardType.Text;

        private string associationId = string.Empty;
        private string associationTitle = string.Empty;
        private bool isGold;
        private bool isImageGoal;
        private int requiredCount;
        private int currentCount;
        private Sprite icon;

        public string Id => id ?? string.Empty;
        public string Title => title ?? string.Empty;
        public string ImageKey => imageKey;
        public string DisplayText => Title;
        public CardType CardType => cardType;
        public bool IsImageCard => cardType == CardType.Image && !isGold;
        public bool IsImageGoal => isGold && isImageGoal;
        public string AssociationId => associationId ?? string.Empty;
        public string AssociationTitle => associationTitle ?? string.Empty;
        public bool IsGold => isGold;
        public int RequiredCount => Mathf.Max(0, requiredCount);
        public int CurrentCount => currentCount;
        public Sprite Icon => icon;

        public static CardData CreateRegularCard(CardData sourceCard, GoldCardData goldCard)
        {
            return CreateRegularCard(sourceCard, goldCard, CardType.Text);
        }

        public static CardData CreateRegularCard(CardData sourceCard, GoldCardData goldCard, CardType sourceCardType)
        {
            if (sourceCard == null || goldCard == null)
            {
                return null;
            }

            return new CardData
            {
                id = sourceCard.Id,
                title = sourceCard.Title,
                imageKey = sourceCard.ImageKey,
                cardType = sourceCardType,
                associationId = goldCard.Id,
                associationTitle = goldCard.Title,
                isGold = false,
                requiredCount = 0,
                currentCount = 0,
                icon = null
            };
        }

        public static CardData CreateGoldCard(GoldCardData sourceGoldCard, int sourceRequiredCount)
        {
            if (sourceGoldCard == null)
            {
                return null;
            }

            return new CardData
            {
                id = sourceGoldCard.Id,
                title = sourceGoldCard.Title,
                imageKey = null,
                cardType = CardType.Text,
                associationId = sourceGoldCard.Id,
                associationTitle = sourceGoldCard.Title,
                isGold = true,
                requiredCount = Mathf.Max(1, sourceRequiredCount),
                currentCount = 0,
                icon = null
            };
        }

        public void SetCurrentCount(int value)
        {
            currentCount = Mathf.Max(0, value);
        }

        public void SetImageGoal(bool value)
        {
            isImageGoal = isGold && value;
        }

        public void SetIcon(Sprite value)
        {
            icon = value;
        }
    }
}
