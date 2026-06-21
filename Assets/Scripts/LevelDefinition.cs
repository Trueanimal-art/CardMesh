using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public enum LevelGenerationMode
    {
        Random = 0,
        GuaranteedSolvable = 1
    }

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Solitaire Associations/Уровни/Уровень")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelName = "Уровень";
        [SerializeField] private string defaultLayoutId = "layout_4_cols_v1";
        [SerializeField] private LevelGenerationMode generationMode = LevelGenerationMode.GuaranteedSolvable;
        [SerializeField, Min(1)] private int maxGenerationAttempts = 20;
        [SerializeField, Min(1)] private int solverMaxStates = 120000;
        [SerializeField] private List<LevelLayoutConfig> layouts = new();
        [Header("Image Cards")]
        [SerializeField, Min(1)] private int imageCardsStartFromLevel = 30;
        [SerializeField, Range(0, 100)] private int imageCardPercentage = 25;

        public string LevelName => levelName;
        public string DefaultLayoutId => defaultLayoutId;
        public LevelGenerationMode GenerationMode => generationMode;
        public int MaxGenerationAttempts => Mathf.Max(1, maxGenerationAttempts);
        public int SolverMaxStates => Mathf.Max(1, solverMaxStates);
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

        public LevelLayoutConfig GetLayoutById(string layoutId)
        {
            if (string.IsNullOrWhiteSpace(layoutId))
            {
                return null;
            }

            foreach (LevelLayoutConfig layout in layouts)
            {
                if (layout != null && string.Equals(layout.Id, layoutId, StringComparison.Ordinal))
                {
                    return layout;
                }
            }

            return null;
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
        private readonly HashSet<string> imageGoalIds;

        public GeneratedLevelData(
            string layoutId,
            int columnsCount,
            int referenceMoves,
            List<List<string>> columns,
            List<string> stockCards,
            Dictionary<string, int> sourceGoldRequiredCountsById,
            IEnumerable<string> sourceImageCardIds = null,
            IEnumerable<string> sourceImageGoalIds = null,
            int generationSeed = 0,
            int generationAttempts = 1,
            bool solverVerified = false,
            int solverMoves = 0,
            int solverStates = 0,
            string solverResult = "")
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
            imageGoalIds = sourceImageGoalIds != null
                ? new HashSet<string>(sourceImageGoalIds)
                : new HashSet<string>();
            GenerationSeed = generationSeed;
            GenerationAttempts = Mathf.Max(1, generationAttempts);
            SolverVerified = solverVerified;
            SolverMoves = Mathf.Max(0, solverMoves);
            SolverStates = Mathf.Max(0, solverStates);
            SolverResult = solverResult ?? string.Empty;
        }

        public string LayoutId { get; }
        public int ColumnsCount { get; }
        public int ReferenceMoves { get; }
        public IReadOnlyList<List<string>> Columns { get; }
        public IReadOnlyList<string> StockCards { get; }
        public int GenerationSeed { get; }
        public int GenerationAttempts { get; }
        public bool SolverVerified { get; }
        public int SolverMoves { get; }
        public int SolverStates { get; }
        public string SolverResult { get; }

        public bool TryGetGoldRequiredCount(string goldCardId, out int requiredCount)
        {
            return goldRequiredCountsById.TryGetValue(goldCardId, out requiredCount);
        }

        public bool IsImageCard(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && imageCardIds.Contains(cardId);
        }

        public bool IsImageGoal(string goldCardId)
        {
            return !string.IsNullOrWhiteSpace(goldCardId) && imageGoalIds.Contains(goldCardId);
        }
    }
}
