using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Solitaire Associations/Уровни/Уровень")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelName = "Уровень";
        [SerializeField, Min(1)] private int movesCount = 100;
        [SerializeField, Min(1)] private int columnsCount = 4;
        [SerializeField] private List<ColumnDefinition> columns = new();
        [SerializeField] private List<CardDefinition> stockCards = new();

        public string LevelName => levelName;
        public int MovesCount => Mathf.Max(1, movesCount);
        public int ColumnsCount => Mathf.Max(1, columnsCount);
        public IReadOnlyList<ColumnDefinition> Columns => columns;
        public IReadOnlyList<CardDefinition> StockCards => stockCards;
    }

    [Serializable]
    public sealed class ColumnDefinition
    {
        [SerializeField] private List<CardDefinition> cardsInColumn = new();

        public IReadOnlyList<CardDefinition> CardsInColumn => cardsInColumn;
    }
}
