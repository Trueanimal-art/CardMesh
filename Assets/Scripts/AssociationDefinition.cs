using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [CreateAssetMenu(fileName = "AssociationDefinition", menuName = "Solitaire Associations/Карты/Ассоциация")]
    public sealed class AssociationDefinition : ScriptableObject
    {
        [SerializeField] private string associationId = "association_id";
        [SerializeField] private string displayName = "Новая ассоциация";
        [SerializeField] private List<CardDefinition> cards = new();

        public string AssociationId => associationId;
        public string DisplayName => displayName;
        public IReadOnlyList<CardDefinition> Cards => cards;
    }
}
