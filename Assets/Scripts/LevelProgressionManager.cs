using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class LevelProgressionManager : MonoBehaviour
    {
        private const string CurrentLevelNumberKey = "CardMesh.CurrentLevelNumber";
        private const string MaxUnlockedLevelKey = "CardMesh.MaxUnlockedLevel";
        private const string MaxCompletedLevelKey = "CardMesh.MaxCompletedLevel";
        private const string LastSelectedLevelKey = "CardMesh.LastSelectedLevel";
        private const string LevelSeedPrefix = "CardMesh.LevelSeed.";
        private const int MaxClearedSeedLevels = 500;

        [SerializeField, Min(1)] private int currentLevelNumber = 1;
        [SerializeField, Min(1)] private int maxUnlockedLevel = 1;
        [SerializeField, Min(0)] private int maxCompletedLevel;
        [SerializeField] private int[] layoutCycle = { 3, 4, 4, 4, 5, 5 };
        [SerializeField] private bool saveProgress = true;

        public int CurrentLevelNumber => Mathf.Max(1, currentLevelNumber);
        public int LastSelectedLevel => CurrentLevelNumber;
        public int MaxUnlockedLevel => Mathf.Max(1, maxUnlockedLevel);
        public int MaxCompletedLevel => Mathf.Max(0, maxCompletedLevel);
        public int CurrentCycleIndex => ResolveCycleIndex(CurrentLevelNumber);
        public int CurrentTargetColumns => GetColumnsForLevel(CurrentLevelNumber);
        public int CurrentLayoutVariantIndex => Mathf.Max(0, CurrentLevelNumber - 1);

        private void Awake()
        {
            Load();
            EnsureLevelSeed(CurrentLevelNumber);
        }

        public void AdvanceToNextLevel()
        {
            MarkLevelCompleted(CurrentLevelNumber);
            SelectLevel(Mathf.Min(MaxUnlockedLevel, CurrentLevelNumber + 1));
        }

        public void MarkLevelCompleted(int levelNumber)
        {
            int safeLevel = Mathf.Max(1, levelNumber);
            maxCompletedLevel = Mathf.Max(MaxCompletedLevel, safeLevel);
            maxUnlockedLevel = Mathf.Max(MaxUnlockedLevel, safeLevel + 1);
            EnsureLevelSeed(safeLevel + 1);
            Save();
        }

        public void ResetProgress()
        {
            ClearSavedProgress();
            currentLevelNumber = 1;
            maxUnlockedLevel = 1;
            maxCompletedLevel = 0;
            EnsureLevelSeed(1);
            Save();
        }

        public static void ClearSavedProgress()
        {
            PlayerPrefs.DeleteKey(CurrentLevelNumberKey);
            PlayerPrefs.DeleteKey(MaxUnlockedLevelKey);
            PlayerPrefs.DeleteKey(MaxCompletedLevelKey);
            PlayerPrefs.DeleteKey(LastSelectedLevelKey);

            for (int level = 1; level <= MaxClearedSeedLevels; level++)
            {
                PlayerPrefs.DeleteKey(GetSeedKey(level));
            }

            PlayerPrefs.Save();
        }

        public bool CanPlayLevel(int levelNumber)
        {
            return levelNumber >= 1 && levelNumber <= MaxUnlockedLevel;
        }

        public bool SelectLevel(int levelNumber)
        {
            if (!CanPlayLevel(levelNumber))
            {
                return false;
            }

            currentLevelNumber = Mathf.Max(1, levelNumber);
            EnsureLevelSeed(currentLevelNumber);
            Save();
            return true;
        }

        public int GetOrCreateSeedForCurrentLevel()
        {
            return EnsureLevelSeed(CurrentLevelNumber);
        }

        public int GetOrCreateSeed(int levelNumber)
        {
            return EnsureLevelSeed(Mathf.Max(1, levelNumber));
        }

        public bool HasSeed(int levelNumber)
        {
            return saveProgress && PlayerPrefs.HasKey(GetSeedKey(Mathf.Max(1, levelNumber)));
        }

        private int GetColumnsForLevel(int levelNumber)
        {
            if (layoutCycle == null || layoutCycle.Length == 0)
            {
                return 3;
            }

            int cycleIndex = ResolveCycleIndex(levelNumber);
            return Mathf.Clamp(layoutCycle[cycleIndex], 3, 5);
        }

        private int ResolveCycleIndex(int levelNumber)
        {
            if (layoutCycle == null || layoutCycle.Length == 0)
            {
                return 0;
            }

            return Mathf.Abs(Mathf.Max(1, levelNumber) - 1) % layoutCycle.Length;
        }

        private void Load()
        {
            if (!saveProgress)
            {
                currentLevelNumber = Mathf.Max(1, currentLevelNumber);
                maxUnlockedLevel = Mathf.Max(1, Mathf.Max(maxUnlockedLevel, currentLevelNumber));
                maxCompletedLevel = Mathf.Clamp(maxCompletedLevel, 0, maxUnlockedLevel);
                return;
            }

            int legacyCurrentLevel = Mathf.Max(1, PlayerPrefs.GetInt(CurrentLevelNumberKey, currentLevelNumber));
            maxUnlockedLevel = Mathf.Max(1, PlayerPrefs.GetInt(MaxUnlockedLevelKey, legacyCurrentLevel));
            maxCompletedLevel = Mathf.Max(0, PlayerPrefs.GetInt(MaxCompletedLevelKey, Mathf.Max(0, maxUnlockedLevel - 1)));
            currentLevelNumber = Mathf.Max(1, PlayerPrefs.GetInt(LastSelectedLevelKey, legacyCurrentLevel));

            if (currentLevelNumber > maxUnlockedLevel)
            {
                currentLevelNumber = maxUnlockedLevel;
            }
        }

        private void Save()
        {
            if (!saveProgress)
            {
                return;
            }

            PlayerPrefs.SetInt(CurrentLevelNumberKey, CurrentLevelNumber);
            PlayerPrefs.SetInt(MaxUnlockedLevelKey, MaxUnlockedLevel);
            PlayerPrefs.SetInt(MaxCompletedLevelKey, MaxCompletedLevel);
            PlayerPrefs.SetInt(LastSelectedLevelKey, CurrentLevelNumber);
            PlayerPrefs.Save();
        }

        private int EnsureLevelSeed(int levelNumber)
        {
            int safeLevel = Mathf.Max(1, levelNumber);

            if (!saveProgress)
            {
                return CreateStableSeed(safeLevel);
            }

            string key = GetSeedKey(safeLevel);
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key);
            }

            int seed = CreateStableSeed(safeLevel);
            PlayerPrefs.SetInt(key, seed);
            PlayerPrefs.Save();
            return seed;
        }

        private static string GetSeedKey(int levelNumber)
        {
            return $"{LevelSeedPrefix}{Mathf.Max(1, levelNumber)}";
        }

        private static int CreateStableSeed(int levelNumber)
        {
            unchecked
            {
                int seed = 173_389_921;
                seed = (seed * 31) ^ Mathf.Max(1, levelNumber);
                seed = (seed * 31) ^ Random.Range(1, int.MaxValue);
                return seed == int.MinValue ? Mathf.Max(1, levelNumber) : Mathf.Abs(seed == 0 ? levelNumber : seed);
            }
        }
    }
}
