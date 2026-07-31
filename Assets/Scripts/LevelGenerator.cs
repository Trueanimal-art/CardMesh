using System.Collections.Generic;
using UnityEngine;

namespace CardMesh
{
    public static class LevelGenerator
    {
        private const float ImageCardLowerTolerance = 0.8f;

        public static bool TryGenerate(LevelDefinition levelDefinition, CardDatabase cardDatabase, out GeneratedLevelData generatedLevelData)
        {
            return TryGenerate(levelDefinition, cardDatabase, null, 0, out generatedLevelData);
        }

        public static bool TryGenerate(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int? targetColumnsCount,
            int layoutVariantIndex,
            out GeneratedLevelData generatedLevelData)
        {
            return TryGenerateInternal(levelDefinition, cardDatabase, null, targetColumnsCount, layoutVariantIndex, Random.Range(1, int.MaxValue), 1, out generatedLevelData);
        }

        public static bool TryGenerate(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int? targetColumnsCount,
            int layoutVariantIndex,
            int seed,
            out GeneratedLevelData generatedLevelData)
        {
            return TryGenerate(levelDefinition, cardDatabase, targetColumnsCount, layoutVariantIndex, seed, 1, out generatedLevelData);
        }

        public static bool TryGenerate(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            string targetLayoutId,
            int? targetColumnsCount,
            int layoutVariantIndex,
            int seed,
            int levelNumber,
            out GeneratedLevelData generatedLevelData)
        {
            Random.State previousState = Random.state;
            Random.InitState(seed);

            try
            {
                return TryGenerateInternal(levelDefinition, cardDatabase, targetLayoutId, targetColumnsCount, layoutVariantIndex, seed, levelNumber, out generatedLevelData);
            }
            finally
            {
                Random.state = previousState;
            }
        }

        public static bool TryGenerate(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int? targetColumnsCount,
            int layoutVariantIndex,
            int seed,
            int levelNumber,
            out GeneratedLevelData generatedLevelData)
        {
            Random.State previousState = Random.state;
            Random.InitState(seed);

            try
            {
                return TryGenerateInternal(levelDefinition, cardDatabase, null, targetColumnsCount, layoutVariantIndex, seed, levelNumber, out generatedLevelData);
            }
            finally
            {
                Random.state = previousState;
            }
        }

        private static bool TryGenerateInternal(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            string targetLayoutId,
            int? targetColumnsCount,
            int layoutVariantIndex,
            int seed,
            int levelNumber,
            out GeneratedLevelData generatedLevelData)
        {
            generatedLevelData = null;

            if (levelDefinition == null)
            {
                Debug.LogWarning("LevelGenerator: level definition is missing.");
                return false;
            }

            if (cardDatabase == null || !cardDatabase.Load())
            {
                Debug.LogWarning("LevelGenerator: card database is missing or failed to load.");
                return false;
            }

            LevelLayoutConfig layout = !string.IsNullOrWhiteSpace(targetLayoutId)
                ? levelDefinition.GetLayoutById(targetLayoutId)
                : null;
            layout ??= targetColumnsCount.HasValue
                ? levelDefinition.GetLayoutForColumns(targetColumnsCount.Value, layoutVariantIndex)
                : levelDefinition.GetRandomLayout();
            if (layout == null)
            {
                Debug.LogWarning($"LevelGenerator: no layout found in '{levelDefinition.name}'.");
                return false;
            }

            if (layout.ColumnCardCounts == null || layout.ColumnCardCounts.Count != layout.ColumnsCount)
            {
                Debug.LogWarning($"LevelGenerator: layout '{layout.Id}' has invalid columnCardCounts.");
                return false;
            }

            if (levelDefinition.GenerationMode == LevelGenerationMode.GuaranteedSolvable)
            {
                return TryGenerateGuaranteedSolvable(levelDefinition, cardDatabase, layout, seed, levelNumber, out generatedLevelData);
            }

            int expectedTotalCards = layout.DeckCount;
            foreach (int columnCardCount in layout.ColumnCardCounts)
            {
                expectedTotalCards += Mathf.Max(0, columnCardCount);
            }

            List<GoldCardData> availableGoldCards = new(cardDatabase.GetGoldCards());
            Shuffle(availableGoldCards);

            List<SelectedGoldCard> selectedGoldCards = new();
            Dictionary<string, int> goldRequiredCountsById = new();
            List<string> pool = new();

            List<GoldCardGroup> sortedGroups = new(layout.GoldCardGroups);
            sortedGroups.Sort((left, right) => right.OrdinaryCardsPerGold.CompareTo(left.OrdinaryCardsPerGold));
            int expectedGoldCards = CountGoldCards(sortedGroups);
            int expectedOrdinaryCards = Mathf.Max(0, expectedTotalCards - expectedGoldCards);
            int targetImageCardCount = GetTargetImageCardCount(levelDefinition, levelNumber, expectedOrdinaryCards);
            int preferredImageCardCount = 0;

            foreach (GoldCardGroup group in sortedGroups)
            {
                if (group == null)
                {
                    continue;
                }

                for (int goldIndex = 0; goldIndex < group.GoldCardsCount; goldIndex++)
                {
                    GoldCardData selectedGold = TakeEligibleGoldCard(
                        availableGoldCards,
                        group.OrdinaryCardsPerGold,
                        cardDatabase,
                        preferredImageCardCount < targetImageCardCount);
                    if (selectedGold == null)
                    {
                        Debug.LogWarning(
                            $"LevelGenerator: not enough gold cards with at least {group.OrdinaryCardsPerGold} ordinary cards for layout '{layout.Id}'.");
                        return false;
                    }

                    List<string> shuffledOrdinaryIds = new();
                    foreach (CardData ordinaryCard in selectedGold.Cards)
                    {
                        if (ordinaryCard != null && !string.IsNullOrWhiteSpace(ordinaryCard.Id))
                        {
                            shuffledOrdinaryIds.Add(ordinaryCard.Id);
                        }
                    }

                    Shuffle(shuffledOrdinaryIds);
                    if (shuffledOrdinaryIds.Count < group.OrdinaryCardsPerGold)
                    {
                        Debug.LogWarning(
                            $"LevelGenerator: gold card '{selectedGold.Id}' does not have enough ordinary cards for layout '{layout.Id}'.");
                        return false;
                    }

                    List<string> assignedOrdinaryIds = shuffledOrdinaryIds.GetRange(0, group.OrdinaryCardsPerGold);
                    List<string> remainingOrdinaryIds = shuffledOrdinaryIds.GetRange(group.OrdinaryCardsPerGold, shuffledOrdinaryIds.Count - group.OrdinaryCardsPerGold);

                    selectedGoldCards.Add(new SelectedGoldCard(selectedGold, assignedOrdinaryIds, remainingOrdinaryIds));
                    goldRequiredCountsById[selectedGold.Id] = group.OrdinaryCardsPerGold;
                    if (GoldCardCanBeImageGoal(selectedGold, cardDatabase))
                    {
                        preferredImageCardCount += assignedOrdinaryIds.Count;
                    }

                    pool.Add(selectedGold.Id);
                    pool.AddRange(assignedOrdinaryIds);
                }
            }

            AdjustPoolSize(layout.Id, expectedTotalCards, selectedGoldCards, pool);
            if (pool.Count != expectedTotalCards)
            {
                Debug.LogWarning(
                    $"LevelGenerator: layout '{layout.Id}' expected {expectedTotalCards} cards, but generated {pool.Count}.");
                return false;
            }

            if (HasDuplicateCardIds(pool, out string duplicateCardId))
            {
                Debug.LogWarning(
                    $"LevelGenerator: layout '{layout.Id}' generated duplicate card id '{duplicateCardId}'. Retrying with another seed.");
                return false;
            }

            HashSet<string> imageCardIds = SelectImageCardIds(levelDefinition, cardDatabase, levelNumber, selectedGoldCards, pool, out HashSet<string> imageGoalIds);
            Shuffle(pool);

            List<List<string>> columns = new();
            int cursor = 0;
            foreach (int columnCardCount in layout.ColumnCardCounts)
            {
                int safeCount = Mathf.Max(0, columnCardCount);
                List<string> column = new();
                for (int index = 0; index < safeCount && cursor < pool.Count; index++, cursor++)
                {
                    column.Add(pool[cursor]);
                }

                columns.Add(column);
            }

            List<string> stockCards = new();
            for (int index = 0; index < layout.DeckCount && cursor < pool.Count; index++, cursor++)
            {
                stockCards.Add(pool[cursor]);
            }

            if (cursor != pool.Count)
            {
                Debug.LogWarning(
                    $"LevelGenerator: layout '{layout.Id}' distribution mismatch. Distributed {cursor} of {pool.Count} cards.");
            }

            generatedLevelData = new GeneratedLevelData(
                layout.Id,
                layout.ColumnsCount,
                layout.ReferenceMoves,
                columns,
                stockCards,
                goldRequiredCountsById,
                imageCardIds,
                imageGoalIds,
                seed,
                1,
                false,
                0,
                0,
                "legacy_random_unverified");
            return true;
        }

        private static bool TryGenerateGuaranteedSolvable(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            LevelLayoutConfig layout,
            int seed,
            int levelNumber,
            out GeneratedLevelData generatedLevelData)
        {
            generatedLevelData = null;
            int maxAttempts = levelDefinition.MaxGenerationAttempts;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                int attemptSeed = seed + (attempt * 7919);
                Random.InitState(attemptSeed);

                if (!TryBuildSelectedCards(levelDefinition, cardDatabase, layout, levelNumber, out List<SelectedGoldCard> selectedGoldCards, out Dictionary<string, int> goldRequiredCountsById, out List<string> pool))
                {
                    continue;
                }

                HashSet<string> imageCardIds = SelectImageCardIds(levelDefinition, cardDatabase, levelNumber, selectedGoldCards, pool, out HashSet<string> imageGoalIds);
                if (!TryBuildSolvableDeal(layout, selectedGoldCards, pool, out List<List<string>> columns, out List<string> stockCards, out int solverMoves))
                {
                    continue;
                }

                int totalCards = CountCards(columns) + stockCards.Count;
                int imageCards = CountCardsInSet(columns, stockCards, imageCardIds);
                int textCards = Mathf.Max(0, totalCards - imageCards - goldRequiredCountsById.Count);
                generatedLevelData = new GeneratedLevelData(
                    layout.Id,
                    layout.ColumnsCount,
                    layout.ReferenceMoves,
                    columns,
                    stockCards,
                    goldRequiredCountsById,
                    imageCardIds,
                    imageGoalIds,
                    attemptSeed,
                    attempt,
                    true,
                    solverMoves,
                    0,
                    "guaranteed_by_construction");

                Debug.Log(
                    $"LevelGenerator: generated verified level. seed={attemptSeed}, layout={layout.Id}, cards={totalCards}, textCards={textCards}, imageCards={imageCards}, goalSlots={layout.ColumnsCount}, goldCards={goldRequiredCountsById.Count}, attempts={attempt}, solver=guaranteed_by_construction, solutionMoves={solverMoves}.");
                return true;
            }

            Debug.LogError(
                $"LevelGenerator: failed to generate a verified solvable level after {maxAttempts} attempts. layout={layout.Id}, seed={seed}.");
            return false;
        }

        private static bool TryBuildSelectedCards(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            LevelLayoutConfig layout,
            int levelNumber,
            out List<SelectedGoldCard> selectedGoldCards,
            out Dictionary<string, int> goldRequiredCountsById,
            out List<string> pool)
        {
            selectedGoldCards = new List<SelectedGoldCard>();
            goldRequiredCountsById = new Dictionary<string, int>();
            pool = new List<string>();

            int expectedTotalCards = layout.DeckCount;
            foreach (int columnCardCount in layout.ColumnCardCounts)
            {
                expectedTotalCards += Mathf.Max(0, columnCardCount);
            }

            List<GoldCardData> availableGoldCards = new(cardDatabase.GetGoldCards());
            Shuffle(availableGoldCards);

            List<GoldCardGroup> sortedGroups = new(layout.GoldCardGroups);
            sortedGroups.Sort((left, right) => right.OrdinaryCardsPerGold.CompareTo(left.OrdinaryCardsPerGold));
            int expectedGoldCards = CountGoldCards(sortedGroups);
            int expectedOrdinaryCards = Mathf.Max(0, expectedTotalCards - expectedGoldCards);
            int targetImageCardCount = GetTargetImageCardCount(levelDefinition, levelNumber, expectedOrdinaryCards);
            int preferredImageCardCount = 0;

            foreach (GoldCardGroup group in sortedGroups)
            {
                if (group == null)
                {
                    continue;
                }

                for (int goldIndex = 0; goldIndex < group.GoldCardsCount; goldIndex++)
                {
                    GoldCardData selectedGold = TakeEligibleGoldCard(
                        availableGoldCards,
                        group.OrdinaryCardsPerGold,
                        cardDatabase,
                        preferredImageCardCount < targetImageCardCount);
                    if (selectedGold == null)
                    {
                        return false;
                    }

                    List<string> shuffledOrdinaryIds = new();
                    foreach (CardData ordinaryCard in selectedGold.Cards)
                    {
                        if (ordinaryCard != null && !string.IsNullOrWhiteSpace(ordinaryCard.Id))
                        {
                            shuffledOrdinaryIds.Add(ordinaryCard.Id);
                        }
                    }

                    Shuffle(shuffledOrdinaryIds);
                    if (shuffledOrdinaryIds.Count < group.OrdinaryCardsPerGold)
                    {
                        return false;
                    }

                    List<string> assignedOrdinaryIds = shuffledOrdinaryIds.GetRange(0, group.OrdinaryCardsPerGold);
                    List<string> remainingOrdinaryIds = shuffledOrdinaryIds.GetRange(group.OrdinaryCardsPerGold, shuffledOrdinaryIds.Count - group.OrdinaryCardsPerGold);

                    selectedGoldCards.Add(new SelectedGoldCard(selectedGold, assignedOrdinaryIds, remainingOrdinaryIds));
                    goldRequiredCountsById[selectedGold.Id] = group.OrdinaryCardsPerGold;
                    if (GoldCardCanBeImageGoal(selectedGold, cardDatabase))
                    {
                        preferredImageCardCount += assignedOrdinaryIds.Count;
                    }

                    pool.Add(selectedGold.Id);
                    pool.AddRange(assignedOrdinaryIds);
                }
            }

            AdjustPoolSize(layout.Id, expectedTotalCards, selectedGoldCards, pool);
            return pool.Count == expectedTotalCards && !HasDuplicateCardIds(pool, out _);
        }

        private static bool TryBuildSolvableDeal(
            LevelLayoutConfig layout,
            List<SelectedGoldCard> selectedGoldCards,
            List<string> pool,
            out List<List<string>> columns,
            out List<string> stockCards,
            out int solutionMoves)
        {
            columns = new List<List<string>>();
            stockCards = new List<string>();
            solutionMoves = 0;
            if (layout == null || selectedGoldCards == null || pool == null)
            {
                return false;
            }

            List<SelectedGoldCard> orderedGoldCards = new(selectedGoldCards);
            Shuffle(orderedGoldCards);
            orderedGoldCards.Sort((left, right) =>
            {
                int leftCount = left?.AssignedOrdinaryIds?.Count ?? 0;
                int rightCount = right?.AssignedOrdinaryIds?.Count ?? 0;
                return rightCount.CompareTo(leftCount);
            });

            HashSet<string> solutionIds = new();
            List<string> solutionOrder = new();
            int activeGoalBatchSize = Mathf.Max(1, Mathf.Min(layout.ColumnsCount, layout.ColumnsCount >= 4 ? 3 : layout.ColumnsCount));
            foreach (List<SelectedGoldCard> batch in MakeGoalBatches(orderedGoldCards, activeGoalBatchSize))
            {
                foreach (SelectedGoldCard selectedGoldCard in batch)
                {
                    if (selectedGoldCard?.GoldCard == null)
                    {
                        return false;
                    }

                    solutionOrder.Add(selectedGoldCard.GoldCard.Id);
                    solutionIds.Add(selectedGoldCard.GoldCard.Id);
                }

                int maxAssignedCards = 0;
                foreach (SelectedGoldCard selectedGoldCard in batch)
                {
                    if (selectedGoldCard?.AssignedOrdinaryIds == null)
                    {
                        continue;
                    }

                    maxAssignedCards = Mathf.Max(maxAssignedCards, selectedGoldCard.AssignedOrdinaryIds.Count);
                }

                for (int cardIndex = 0; cardIndex < maxAssignedCards; cardIndex++)
                {
                    List<SelectedGoldCard> wave = new(batch);
                    Shuffle(wave);
                    foreach (SelectedGoldCard selectedGoldCard in wave)
                    {
                        if (selectedGoldCard?.AssignedOrdinaryIds == null || cardIndex >= selectedGoldCard.AssignedOrdinaryIds.Count)
                        {
                            continue;
                        }

                        string cardId = selectedGoldCard.AssignedOrdinaryIds[cardIndex];
                        if (string.IsNullOrWhiteSpace(cardId))
                        {
                            continue;
                        }

                        solutionOrder.Add(cardId);
                        solutionIds.Add(cardId);
                    }
                }
            }

            List<string> extras = new();
            foreach (string cardId in pool)
            {
                if (!solutionIds.Contains(cardId))
                {
                    extras.Add(cardId);
                }
            }

            Shuffle(extras);

            int columnCapacity = 0;
            foreach (int columnCardCount in layout.ColumnCardCounts)
            {
                columnCapacity += Mathf.Max(0, columnCardCount);
            }

            int columnSolutionCards = Mathf.Min(columnCapacity, solutionOrder.Count);
            List<string> solutionPrefix = solutionOrder.GetRange(0, columnSolutionCards);
            columns = DealColumnsRoundRobin(solutionPrefix, layout.ColumnCardCounts);

            List<string> solutionSuffix = solutionOrder.GetRange(columnSolutionCards, solutionOrder.Count - columnSolutionCards);
            stockCards = WeaveStock(solutionSuffix, extras);
            LimitInitialGoldTops(columns, stockCards, GetGoldIds(selectedGoldCards));

            if (CountCards(columns) + stockCards.Count != pool.Count)
            {
                return false;
            }

            int stockSolutionCards = Mathf.Max(0, solutionOrder.Count - columnSolutionCards);
            solutionMoves = columnSolutionCards + (stockSolutionCards * 2);
            return true;
        }

        private static List<List<SelectedGoldCard>> MakeGoalBatches(List<SelectedGoldCard> orderedGoldCards, int batchSize)
        {
            List<List<SelectedGoldCard>> batches = new();
            int cursor = 0;
            int safeBatchSize = Mathf.Max(1, batchSize);
            while (cursor < orderedGoldCards.Count)
            {
                int remaining = orderedGoldCards.Count - cursor;
                int currentSize = safeBatchSize > 1 && remaining == safeBatchSize + 1
                    ? 2
                    : Mathf.Min(safeBatchSize, remaining);
                batches.Add(orderedGoldCards.GetRange(cursor, currentSize));
                cursor += currentSize;
            }

            return batches;
        }

        private static List<List<string>> DealColumnsRoundRobin(List<string> solutionPrefix, IReadOnlyList<int> columnCardCounts)
        {
            List<List<string>> dealtColumns = new();
            List<int> remainingCapacity = new();
            foreach (int columnCardCount in columnCardCounts)
            {
                dealtColumns.Add(new List<string>());
                remainingCapacity.Add(Mathf.Max(0, columnCardCount));
            }

            int columnIndex = 0;
            foreach (string cardId in solutionPrefix)
            {
                int checkedColumns = 0;
                while (checkedColumns < remainingCapacity.Count && remainingCapacity[columnIndex] <= 0)
                {
                    columnIndex = (columnIndex + 1) % remainingCapacity.Count;
                    checkedColumns++;
                }

                if (checkedColumns >= remainingCapacity.Count)
                {
                    break;
                }

                dealtColumns[columnIndex].Add(cardId);
                remainingCapacity[columnIndex]--;
                columnIndex = (columnIndex + 1) % remainingCapacity.Count;
            }

            foreach (List<string> column in dealtColumns)
            {
                column.Reverse();
            }

            return dealtColumns;
        }

        private static List<string> WeaveStock(List<string> solutionSuffix, List<string> extras)
        {
            List<string> stock = new();
            List<string> remainingExtras = new(extras);
            int solutionIndex = 0;
            while (solutionIndex < solutionSuffix.Count || remainingExtras.Count > 0)
            {
                bool shouldPlaceExtra = remainingExtras.Count > 0
                    && (solutionIndex >= solutionSuffix.Count || stock.Count % 4 == 2 || Random.value < 0.18f);
                if (shouldPlaceExtra)
                {
                    int lastIndex = remainingExtras.Count - 1;
                    stock.Add(remainingExtras[lastIndex]);
                    remainingExtras.RemoveAt(lastIndex);
                    continue;
                }

                stock.Add(solutionSuffix[solutionIndex]);
                solutionIndex++;
            }

            return stock;
        }

        private static void LimitInitialGoldTops(List<List<string>> columns, List<string> stockCards, HashSet<string> goldIds, int maxGoldTops = 1)
        {
            if (columns == null || stockCards == null || goldIds == null)
            {
                return;
            }

            List<int> goldTopColumns = new();
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                List<string> column = columns[columnIndex];
                if (column != null && column.Count > 0 && goldIds.Contains(column[^1]))
                {
                    goldTopColumns.Add(columnIndex);
                }
            }

            if (goldTopColumns.Count <= maxGoldTops)
            {
                return;
            }

            Shuffle(goldTopColumns);
            for (int index = maxGoldTops; index < goldTopColumns.Count; index++)
            {
                List<string> column = columns[goldTopColumns[index]];
                int hiddenNonGoldIndex = FindHiddenNonGoldIndex(column, goldIds);
                if (hiddenNonGoldIndex >= 0)
                {
                    (column[^1], column[hiddenNonGoldIndex]) = (column[hiddenNonGoldIndex], column[^1]);
                    continue;
                }

                int stockNonGoldIndex = FindStockNonGoldIndex(stockCards, goldIds);
                if (stockNonGoldIndex < 0)
                {
                    continue;
                }

                (column[^1], stockCards[stockNonGoldIndex]) = (stockCards[stockNonGoldIndex], column[^1]);
            }
        }

        private static int FindHiddenNonGoldIndex(List<string> column, HashSet<string> goldIds)
        {
            if (column == null)
            {
                return -1;
            }

            for (int index = column.Count - 2; index >= 0; index--)
            {
                if (!goldIds.Contains(column[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindStockNonGoldIndex(List<string> stockCards, HashSet<string> goldIds)
        {
            List<int> candidates = new();
            for (int index = 0; index < stockCards.Count; index++)
            {
                if (!goldIds.Contains(stockCards[index]))
                {
                    candidates.Add(index);
                }
            }

            if (candidates.Count == 0)
            {
                return -1;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private static HashSet<string> SelectImageCardIds(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int levelNumber,
            List<SelectedGoldCard> selectedGoldCards,
            List<string> pool,
            out HashSet<string> imageGoalIds)
        {
            HashSet<string> imageCardIds = new();
            imageGoalIds = new HashSet<string>();
            if (levelDefinition == null || cardDatabase == null || !levelDefinition.ShouldUseImageCards(levelNumber))
            {
                return imageCardIds;
            }

            HashSet<string> poolIds = new(pool);
            HashSet<string> goldIds = GetGoldIds(selectedGoldCards);
            int totalOrdinaryCards = 0;
            foreach (string cardId in poolIds)
            {
                if (string.IsNullOrWhiteSpace(cardId) || goldIds.Contains(cardId))
                {
                    continue;
                }

                totalOrdinaryCards++;
            }

            if (totalOrdinaryCards == 0)
            {
                return imageCardIds;
            }

            List<SelectedGoldCard> imageGoalCandidates = new();
            foreach (SelectedGoldCard selectedGoldCard in selectedGoldCards)
            {
                if (GoldCardCanBeImageGoal(selectedGoldCard?.GoldCard, cardDatabase)
                    && CountPoolCardsForGold(selectedGoldCard, poolIds) > 0)
                {
                    imageGoalCandidates.Add(selectedGoldCard);
                }
            }

            if (imageGoalCandidates.Count == 0)
            {
                return imageCardIds;
            }

            imageGoalCandidates.Sort((left, right) =>
                CountPoolCardsForGold(right, poolIds).CompareTo(CountPoolCardsForGold(left, poolIds)));
            int targetImageCardCount = GetTargetImageCardCount(levelDefinition, levelNumber, totalOrdinaryCards);

            int selectedImageCardCount = 0;
            foreach (SelectedGoldCard imageGoalCandidate in imageGoalCandidates)
            {
                int added = AddPoolCardsForImageGoal(imageGoalCandidate, poolIds, imageCardIds);
                if (added <= 0)
                {
                    continue;
                }

                imageGoalIds.Add(imageGoalCandidate.GoldCard.Id);
                selectedImageCardCount += added;
                if (selectedImageCardCount >= targetImageCardCount)
                {
                    break;
                }
            }

            return imageCardIds;
        }

        private static int CountGoldCards(IReadOnlyList<GoldCardGroup> groups)
        {
            int count = 0;
            if (groups == null)
            {
                return count;
            }

            foreach (GoldCardGroup group in groups)
            {
                if (group != null)
                {
                    count += group.GoldCardsCount;
                }
            }

            return count;
        }

        private static int GetTargetImageCardCount(LevelDefinition levelDefinition, int levelNumber, int ordinaryCardCount)
        {
            if (levelDefinition == null || ordinaryCardCount <= 0 || !levelDefinition.ShouldUseImageCards(levelNumber))
            {
                return 0;
            }

            int percentage = levelDefinition.ImageCardPercentage;
            int targetCount = Mathf.RoundToInt(ordinaryCardCount * (percentage / 100f));
            int toleratedTargetCount = Mathf.CeilToInt(targetCount * ImageCardLowerTolerance);
            return percentage > 0 ? Mathf.Max(1, toleratedTargetCount) : 0;
        }

        private static int CountCards(List<List<string>> columns)
        {
            int count = 0;
            if (columns == null)
            {
                return count;
            }

            foreach (List<string> column in columns)
            {
                if (column != null)
                {
                    count += column.Count;
                }
            }

            return count;
        }

        private static int CountCardsInSet(List<List<string>> columns, List<string> stockCards, HashSet<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return 0;
            }

            int count = 0;
            if (columns != null)
            {
                foreach (List<string> column in columns)
                {
                    if (column == null)
                    {
                        continue;
                    }

                    foreach (string cardId in column)
                    {
                        if (cardIds.Contains(cardId))
                        {
                            count++;
                        }
                    }
                }
            }

            if (stockCards != null)
            {
                foreach (string cardId in stockCards)
                {
                    if (cardIds.Contains(cardId))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static HashSet<string> GetGoldIds(List<SelectedGoldCard> selectedGoldCards)
        {
            HashSet<string> goldIds = new();
            if (selectedGoldCards == null)
            {
                return goldIds;
            }

            foreach (SelectedGoldCard selectedGoldCard in selectedGoldCards)
            {
                if (selectedGoldCard?.GoldCard == null)
                {
                    continue;
                }

                goldIds.Add(selectedGoldCard.GoldCard.Id);
            }

            return goldIds;
        }

        private static bool HasDuplicateCardIds(IEnumerable<string> cardIds, out string duplicateCardId)
        {
            duplicateCardId = string.Empty;
            HashSet<string> seen = new();
            if (cardIds == null)
            {
                return false;
            }

            foreach (string cardId in cardIds)
            {
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    continue;
                }

                if (!seen.Add(cardId))
                {
                    duplicateCardId = cardId;
                    return true;
                }
            }

            return false;
        }

        private static void AdjustPoolSize(
            string layoutId,
            int expectedTotalCards,
            List<SelectedGoldCard> selectedGoldCards,
            List<string> pool)
        {
            if (pool.Count < expectedTotalCards)
            {
                List<string> extraOrdinaryCards = new();
                foreach (SelectedGoldCard selectedGoldCard in selectedGoldCards)
                {
                    extraOrdinaryCards.AddRange(selectedGoldCard.RemainingOrdinaryIds);
                }

                Shuffle(extraOrdinaryCards);
                int neededCards = expectedTotalCards - pool.Count;
                int cardsToTake = Mathf.Min(neededCards, extraOrdinaryCards.Count);
                for (int index = 0; index < cardsToTake; index++)
                {
                    pool.Add(extraOrdinaryCards[index]);
                }

                Debug.LogWarning(
                    $"LevelGenerator: layout '{layoutId}' gold groups produced too few cards. Added {cardsToTake} extra ordinary cards.");
                return;
            }

            if (pool.Count > expectedTotalCards)
            {
                List<int> ordinaryCardIndices = new();
                HashSet<string> goldIds = new();
                foreach (SelectedGoldCard selectedGoldCard in selectedGoldCards)
                {
                    goldIds.Add(selectedGoldCard.GoldCard.Id);
                }

                for (int index = 0; index < pool.Count; index++)
                {
                    if (!goldIds.Contains(pool[index]))
                    {
                        ordinaryCardIndices.Add(index);
                    }
                }

                Shuffle(ordinaryCardIndices);
                int cardsToRemove = Mathf.Min(pool.Count - expectedTotalCards, ordinaryCardIndices.Count);
                ordinaryCardIndices.Sort((left, right) => right.CompareTo(left));
                for (int index = 0; index < cardsToRemove; index++)
                {
                    pool.RemoveAt(ordinaryCardIndices[index]);
                }

                Debug.LogWarning(
                    $"LevelGenerator: layout '{layoutId}' gold groups produced too many cards. Removed {cardsToRemove} ordinary cards.");
            }
        }

        private static GoldCardData TakeEligibleGoldCard(
            List<GoldCardData> availableGoldCards,
            int requiredOrdinaryCards,
            CardDatabase cardDatabase,
            bool preferImageGoal)
        {
            if (preferImageGoal)
            {
                GoldCardData imageGoalCard = TakeEligibleGoldCard(
                    availableGoldCards,
                    requiredOrdinaryCards,
                    goldCard => GoldCardCanBeImageGoal(goldCard, cardDatabase));
                if (imageGoalCard != null)
                {
                    return imageGoalCard;
                }
            }

            return TakeEligibleGoldCard(availableGoldCards, requiredOrdinaryCards, null);
        }

        private static GoldCardData TakeEligibleGoldCard(
            List<GoldCardData> availableGoldCards,
            int requiredOrdinaryCards,
            System.Predicate<GoldCardData> predicate)
        {
            for (int index = 0; index < availableGoldCards.Count; index++)
            {
                GoldCardData goldCard = availableGoldCards[index];
                if (goldCard == null || goldCard.Cards == null || goldCard.Cards.Count < requiredOrdinaryCards)
                {
                    continue;
                }

                if (predicate != null && !predicate(goldCard))
                {
                    continue;
                }

                availableGoldCards.RemoveAt(index);
                return goldCard;
            }

            return null;
        }

        private static bool GoldCardCanBeImageGoal(GoldCardData goldCard, CardDatabase cardDatabase)
        {
            if (goldCard?.Cards == null || cardDatabase == null)
            {
                return false;
            }

            foreach (CardData card in goldCard.Cards)
            {
                if (card == null || !cardDatabase.HasImageForCard(card.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountPoolCardsForGold(SelectedGoldCard selectedGoldCard, HashSet<string> poolIds)
        {
            if (selectedGoldCard?.GoldCard?.Cards == null || poolIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (CardData card in selectedGoldCard.GoldCard.Cards)
            {
                if (card != null && poolIds.Contains(card.Id))
                {
                    count++;
                }
            }

            return count;
        }

        private static int AddPoolCardsForImageGoal(SelectedGoldCard selectedGoldCard, HashSet<string> poolIds, HashSet<string> imageCardIds)
        {
            if (selectedGoldCard?.GoldCard?.Cards == null || poolIds == null || imageCardIds == null)
            {
                return 0;
            }

            int added = 0;
            foreach (CardData card in selectedGoldCard.GoldCard.Cards)
            {
                if (card != null && poolIds.Contains(card.Id) && imageCardIds.Add(card.Id))
                {
                    added++;
                }
            }

            return added;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                T temp = list[index];
                list[index] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        private sealed class SelectedGoldCard
        {
            public SelectedGoldCard(GoldCardData goldCard, List<string> assignedOrdinaryIds, List<string> remainingOrdinaryIds)
            {
                GoldCard = goldCard;
                AssignedOrdinaryIds = assignedOrdinaryIds;
                RemainingOrdinaryIds = remainingOrdinaryIds;
            }

            public GoldCardData GoldCard { get; }
            public List<string> AssignedOrdinaryIds { get; }
            public List<string> RemainingOrdinaryIds { get; }
        }
    }
}
