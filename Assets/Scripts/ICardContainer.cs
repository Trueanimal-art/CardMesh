namespace SolitaireAssociations
{
    public interface ICardContainer
    {
        int CardCount { get; }
        CardView TopCard { get; }

        void Initialize(GameManager owner);
        void ApplyLayout(ColumnLayoutProfile profile);
        void AddCard(CardView cardView);
        void RemoveCard(CardView cardView);
        bool IsTopCard(CardView cardView);
        bool CanAccept(CardView movingCard);
        void ClearCards();
        void RefreshLayout();
    }
}
