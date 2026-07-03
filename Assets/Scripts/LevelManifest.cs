using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [Serializable]
    public sealed class LevelManifestData
    {
        public int totalLevels;
        public string generationDate;
        public string generatorVersion;
        public string solverVersion;
        public List<LevelManifestEntry> levels = new();
        public LevelManifestSolverStats solverStats;
        public List<LevelManifestFailedSeed> failedSeeds = new();
    }

    [Serializable]
    public sealed class LevelManifestEntry
    {
        public string levelId;
        public int levelNumber;
        public int seed;
        public string layoutId;
        public string difficulty;
        public int columns;
        public int goalSlots;
        public int cardCount;
        public int deckCount;
        public int goldenCards;
        public int imageCards;
        public int textCards;
        public int referenceMoves;
        public int startingMoves;
        public int generationAttempts;
        public bool solverSolved;
        public int solverMoves;
        public int solverStates;
        public string solverResult;
        public List<string> cards = new();
        public List<LevelManifestColumn> columnsData = new();
        public List<string> stockCards = new();
        public List<string> goldCards = new();
        public List<LevelManifestGoldGoal> goldGoals = new();
        public List<string> imageCardIds = new();
        public List<string> imageGoalIds = new();
    }

    [Serializable]
    public sealed class LevelManifestColumn
    {
        public int index;
        public List<string> cards = new();
    }

    [Serializable]
    public sealed class LevelManifestGoldGoal
    {
        public string id;
        public int requiredCount;
        public bool isImageGoal;
    }

    [Serializable]
    public sealed class LevelManifestSolverStats
    {
        public int solved;
        public int total;
        public float solvedRate;
        public float averageSolverMoves;
        public float averageSolverStates;
        public float averageGenerationAttempts;
    }

    [Serializable]
    public sealed class LevelManifestFailedSeed
    {
        public int levelNumber;
        public int seed;
        public string layoutId;
        public string reason;
    }

    public static class LevelManifestDatabase
    {
        private const string ManifestResourcePath = "GeneratedLevels/level_manifest";

        private static readonly Dictionary<int, LevelManifestEntry> LevelsByNumber = new();
        private static LevelManifestData cachedManifest;
        private static bool loadAttempted;

        public static bool TryGetLevel(int levelNumber, out LevelManifestEntry entry)
        {
            EnsureLoaded();
            return LevelsByNumber.TryGetValue(Mathf.Max(1, levelNumber), out entry);
        }

        public static bool TryGetManifest(out LevelManifestData manifest)
        {
            EnsureLoaded();
            manifest = cachedManifest;
            return manifest != null;
        }

        public static bool TryCreateGeneratedLevelData(LevelManifestEntry entry, out GeneratedLevelData generatedLevelData)
        {
            generatedLevelData = null;
            if (entry == null || string.IsNullOrWhiteSpace(entry.layoutId) || entry.columns <= 0)
            {
                return false;
            }

            List<List<string>> columns = new();
            if (entry.columnsData != null)
            {
                entry.columnsData.Sort((left, right) => left.index.CompareTo(right.index));
                foreach (LevelManifestColumn column in entry.columnsData)
                {
                    columns.Add(column?.cards != null ? new List<string>(column.cards) : new List<string>());
                }
            }

            if (columns.Count == 0)
            {
                return false;
            }

            Dictionary<string, int> requiredCountsByGold = new();
            List<string> imageGoalIds = new();
            if (entry.goldGoals != null && entry.goldGoals.Count > 0)
            {
                foreach (LevelManifestGoldGoal goal in entry.goldGoals)
                {
                    if (goal == null || string.IsNullOrWhiteSpace(goal.id))
                    {
                        continue;
                    }

                    requiredCountsByGold[goal.id] = Mathf.Max(1, goal.requiredCount);
                    if (goal.isImageGoal)
                    {
                        imageGoalIds.Add(goal.id);
                    }
                }
            }
            else if (entry.goldCards != null)
            {
                foreach (string goldCardId in entry.goldCards)
                {
                    if (!string.IsNullOrWhiteSpace(goldCardId))
                    {
                        requiredCountsByGold[goldCardId] = 1;
                    }
                }
            }

            if (requiredCountsByGold.Count == 0)
            {
                return false;
            }

            if (imageGoalIds.Count == 0 && entry.imageGoalIds != null)
            {
                imageGoalIds.AddRange(entry.imageGoalIds);
            }

            generatedLevelData = new GeneratedLevelData(
                entry.layoutId,
                entry.columns,
                Mathf.Max(1, entry.startingMoves > 0 ? entry.startingMoves : entry.referenceMoves),
                columns,
                entry.stockCards != null ? new List<string>(entry.stockCards) : new List<string>(),
                requiredCountsByGold,
                entry.imageCardIds,
                imageGoalIds,
                entry.seed,
                Mathf.Max(1, entry.generationAttempts),
                entry.solverSolved,
                Mathf.Max(0, entry.solverMoves),
                Mathf.Max(0, entry.solverStates),
                entry.solverResult);
            return true;
        }

        private static void EnsureLoaded()
        {
            if (loadAttempted)
            {
                return;
            }

            loadAttempted = true;
            TextAsset manifestAsset = Resources.Load<TextAsset>(ManifestResourcePath);
            if (manifestAsset == null)
            {
                return;
            }

            try
            {
                cachedManifest = JsonUtility.FromJson<LevelManifestData>(manifestAsset.text);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"LevelManifestDatabase: failed to parse level manifest. {exception.Message}");
                cachedManifest = null;
                return;
            }

            if (cachedManifest?.levels == null)
            {
                cachedManifest = null;
                return;
            }

            LevelsByNumber.Clear();
            foreach (LevelManifestEntry level in cachedManifest.levels)
            {
                if (level == null || level.levelNumber <= 0)
                {
                    continue;
                }

                LevelsByNumber[level.levelNumber] = level;
            }

            Debug.Log($"LevelManifestDatabase: loaded {LevelsByNumber.Count} generated levels.");
        }
    }
}
