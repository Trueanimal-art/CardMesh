using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public static class LevelGenerator
    {
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
            return TryGenerateInternal(levelDefinition, cardDatabase, targetColumnsCount, layoutVariantIndex, out generatedLevelData);
        }

        public static bool TryGenerate(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int? targetColumnsCount,
            int layoutVariantIndex,
            int seed,
            out GeneratedLevelData generatedLevelData)
        {
            Random.State previousState = Random.state;
            Random.InitState(seed);

            try
            {
                return TryGenerateInternal(levelDefinition, cardDatabase, targetColumnsCount, layoutVariantIndex, out generatedLevelData);
            }
            finally
            {
                Random.state = previousState;
            }
        }

        private static bool TryGenerateInternal(
            LevelDefinition levelDefinition,
            CardDatabase cardDatabase,
            int? targetColumnsCount,
            int layoutVariantIndex,
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

            LevelLayoutConfig layout = targetColumnsCount.HasValue
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

            foreach (GoldCardGroup group in sortedGroups)
            {
                if (group == null)
                {
                    continue;
                }

                for (int goldIndex = 0; goldIndex < group.GoldCardsCount; goldIndex++)
                {
                    GoldCardData selectedGold = TakeEligibleGoldCard(availableGoldCards, group.OrdinaryCardsPerGold);
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
                goldRequiredCountsById);
            return true;
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

        private static GoldCardData TakeEligibleGoldCard(List<GoldCardData> availableGoldCards, int requiredOrdinaryCards)
        {
            for (int index = 0; index < availableGoldCards.Count; index++)
            {
                GoldCardData goldCard = availableGoldCards[index];
                if (goldCard == null || goldCard.Cards == null || goldCard.Cards.Count < requiredOrdinaryCards)
                {
                    continue;
                }

                availableGoldCards.RemoveAt(index);
                return goldCard;
            }

            return null;
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
