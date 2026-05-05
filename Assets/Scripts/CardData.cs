using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class CardData
    {
        private readonly CardDefinition definition;
        private readonly string associationTitle;
        private int currentCount;

        public CardData(CardDefinition sourceDefinition, string sourceAssociationTitle)
        {
            definition = sourceDefinition;
            associationTitle = sourceAssociationTitle;
        }

        public CardDefinition Definition => definition;
        public string Id => definition != null ? definition.CardId : string.Empty;
        public string DisplayText => definition != null ? definition.Title : string.Empty;
        public string AssociationId => definition != null ? definition.AssociationId : string.Empty;
        public string AssociationTitle => associationTitle;
        public bool IsGold => definition != null && definition.IsGoalCard;
        public int RequiredCount => definition != null ? Mathf.Max(1, definition.RequiredCount) : 0;
        public int CurrentCount => currentCount;
        public Sprite Icon => definition != null ? definition.Icon : null;

        public void SetCurrentCount(int value)
        {
            currentCount = Mathf.Max(0, value);
        }
    }
}
