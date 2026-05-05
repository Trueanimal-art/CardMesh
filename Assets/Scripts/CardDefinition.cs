using UnityEngine;

namespace SolitaireAssociations
{
    [CreateAssetMenu(fileName = "CardDefinition", menuName = "Solitaire Associations/Карты/Карта")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId = "card_id";
        [SerializeField] private string title = "Новая карта";
        [SerializeField] private string associationId = "association_id";
        [SerializeField] private bool isGoalCard;
        [SerializeField, Min(0)] private int requiredCount;
        [SerializeField] private Sprite icon;

        public string CardId => cardId;
        public string Title => title;
        public string AssociationId => associationId;
        public bool IsGoalCard => isGoalCard;
        public int RequiredCount => Mathf.Max(0, requiredCount);
        public Sprite Icon => icon;
    }
}
