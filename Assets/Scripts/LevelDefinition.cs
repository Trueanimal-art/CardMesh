using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Solitaire Associations/Уровни/Уровень")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelName = "Уровень";
        [SerializeField] private string defaultLayoutId = "layout_4_cols_v1";
        [SerializeField] private List<LevelLayoutConfig> layouts = new();
        [Header("Image Cards")]
        [SerializeField, Min(1)] private int imageCardsStartFromLevel = 30;
        [SerializeField, Range(0, 100)] private int imageCardPercentage = 25;

        public string LevelName => levelName;
        public string DefaultLayoutId => defaultLayoutId;
        public IReadOnlyList<LevelLayoutConfig> Layouts => layouts;
        public int ImageCardsStartFromLevel => Mathf.Max(1, imageCardsStartFromLevel);
        public int ImageCardPercentage => Mathf.Clamp(imageCardPercentage, 0, 100);

        public bool ShouldUseImageCards(int levelNumber)
        {
            return Mathf.Max(1, levelNumber) >= ImageCardsStartFromLevel && ImageCardPercentage > 0;
        }

        public int MovesCount
        {
            get
            {
                LevelLayoutConfig layout = GetActiveLayout();
                return layout != null ? layout.ReferenceMoves : 1;
            }
        }

        public int ColumnsCount
        {
            get
            {
                LevelLayoutConfig layout = GetActiveLayout();
                return layout != null ? layout.ColumnsCount : 1;
            }
        }

        public LevelLayoutConfig GetActiveLayout()
        {
            if (!string.IsNullOrWhiteSpace(defaultLayoutId))
            {
                foreach (LevelLayoutConfig layout in layouts)
                {
                    if (layout != null && string.Equals(layout.Id, defaultLayoutId, StringComparison.Ordinal))
                    {
                        return layout;
                    }
                }
            }

            foreach (LevelLayoutConfig layout in layouts)
            {
                if (layout != null)
                {
                    return layout;
                }
            }

            return null;
        }

        public LevelLayoutConfig GetRandomLayout()
        {
            List<LevelLayoutConfig> availableLayouts = new();
            foreach (LevelLayoutConfig layout in layouts)
            {
                if (layout != null)
                {
                    availableLayouts.Add(layout);
                }
            }

            if (availableLayouts.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, availableLayouts.Count);
            return availableLayouts[randomIndex];
        }

        public LevelLayoutConfig GetLayoutForColumns(int columnsCount, int variantIndex)
        {
            List<LevelLayoutConfig> availableLayouts = new();
            foreach (LevelLayoutConfig layout in layouts)
            {
                if (layout != null && layout.ColumnsCount == columnsCount)
                {
                    availableLayouts.Add(layout);
                }
            }

            if (availableLayouts.Count == 0)
            {
                return GetActiveLayout();
            }

            int safeIndex = Mathf.Abs(variantIndex) % availableLayouts.Count;
            return availableLayouts[safeIndex];
        }
    }

    [Serializable]
    public sealed class LevelLayoutConfig
    {
        [SerializeField] private string id = "layout_id";
        [SerializeField, Min(1)] private int columnsCount = 4;
        [SerializeField] private List<int> columnCardCounts = new();
        [SerializeField, Min(0)] private int deckCount;
        [SerializeField, Min(1)] private int referenceMoves = 100;
        [SerializeField] private List<GoldCardGroup> goldCardGroups = new();

        public string Id => id ?? string.Empty;
        public int ColumnsCount => Mathf.Max(1, columnsCount);
        public IReadOnlyList<int> ColumnCardCounts => columnCardCounts;
        public int DeckCount => Mathf.Max(0, deckCount);
        public int ReferenceMoves => Mathf.Max(1, referenceMoves);
        public IReadOnlyList<GoldCardGroup> GoldCardGroups => goldCardGroups;
    }

    [Serializable]
    public sealed class GoldCardGroup
    {
        [SerializeField, Min(0)] private int goldCardsCount;
        [SerializeField, Min(0)] private int ordinaryCardsPerGold;

        public int GoldCardsCount => Mathf.Max(0, goldCardsCount);
        public int OrdinaryCardsPerGold => Mathf.Max(0, ordinaryCardsPerGold);
    }

    public sealed class GeneratedLevelData
    {
        private readonly Dictionary<string, int> goldRequiredCountsById;
        private readonly HashSet<string> imageCardIds;

        public GeneratedLevelData(
            string layoutId,
            int columnsCount,
            int referenceMoves,
            List<List<string>> columns,
            List<string> stockCards,
            Dictionary<string, int> sourceGoldRequiredCountsById,
            IEnumerable<string> sourceImageCardIds = null)
        {
            LayoutId = layoutId;
            ColumnsCount = Mathf.Max(1, columnsCount);
            ReferenceMoves = Mathf.Max(1, referenceMoves);
            Columns = columns ?? new List<List<string>>();
            StockCards = stockCards ?? new List<string>();
            goldRequiredCountsById = sourceGoldRequiredCountsById ?? new Dictionary<string, int>();
            imageCardIds = sourceImageCardIds != null
                ? new HashSet<string>(sourceImageCardIds)
                : new HashSet<string>();
        }

        public string LayoutId { get; }
        public int ColumnsCount { get; }
        public int ReferenceMoves { get; }
        public IReadOnlyList<List<string>> Columns { get; }
        public IReadOnlyList<string> StockCards { get; }

        public bool TryGetGoldRequiredCount(string goldCardId, out int requiredCount)
        {
            return goldRequiredCountsById.TryGetValue(goldCardId, out requiredCount);
        }

        public bool IsImageCard(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && imageCardIds.Contains(cardId);
        }
    }
}
