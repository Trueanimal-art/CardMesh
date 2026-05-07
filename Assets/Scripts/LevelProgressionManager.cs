using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class LevelProgressionManager : MonoBehaviour
    {
        private const string CurrentLevelNumberKey = "CardMesh.CurrentLevelNumber";

        [SerializeField, Min(1)] private int currentLevelNumber = 1;
        [SerializeField] private int[] layoutCycle = { 3, 4, 4, 4, 5, 5 };
        [SerializeField] private bool saveProgress = true;

        public int CurrentLevelNumber => Mathf.Max(1, currentLevelNumber);
        public int CurrentCycleIndex => ResolveCycleIndex(CurrentLevelNumber);
        public int CurrentTargetColumns => GetColumnsForLevel(CurrentLevelNumber);
        public int CurrentLayoutVariantIndex => Mathf.Max(0, CurrentLevelNumber - 1);

        private void Awake()
        {
            Load();
        }

        public void AdvanceToNextLevel()
        {
            currentLevelNumber = Mathf.Max(1, currentLevelNumber + 1);
            Save();
        }

        public void ResetProgress()
        {
            currentLevelNumber = 1;
            Save();
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
                return;
            }

            currentLevelNumber = Mathf.Max(1, PlayerPrefs.GetInt(CurrentLevelNumberKey, currentLevelNumber));
        }

        private void Save()
        {
            if (!saveProgress)
            {
                return;
            }

            PlayerPrefs.SetInt(CurrentLevelNumberKey, CurrentLevelNumber);
            PlayerPrefs.Save();
        }
    }
}
