using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public enum CollectionRewardType
    {
        Coins,
        Undo,
        Hint,
        ExtraMove,
        SmallChest,
        BigChest,
        Booster,
        EpicReward
    }

    [Serializable]
    public sealed class CollectionBookReward
    {
        [SerializeField, Min(1)] private int requiredPoints = 10;
        [SerializeField] private CollectionRewardType rewardType = CollectionRewardType.Undo;
        [SerializeField, Min(0)] private int amount = 1;
        [SerializeField] private string title = "+1 Отмена";
        [SerializeField] private string shortLabel = "+1";

        public int RequiredPoints => Mathf.Max(1, requiredPoints);
        public CollectionRewardType RewardType => rewardType;
        public int Amount => Mathf.Max(0, amount);
        public string Title => string.IsNullOrWhiteSpace(title) ? BuildFallbackTitle() : title;
        public string ShortLabel => string.IsNullOrWhiteSpace(shortLabel) ? Title : shortLabel;

        public CollectionBookReward(int requiredPoints, CollectionRewardType rewardType, int amount, string title, string shortLabel)
        {
            this.requiredPoints = Mathf.Max(1, requiredPoints);
            this.rewardType = rewardType;
            this.amount = Mathf.Max(0, amount);
            this.title = title;
            this.shortLabel = shortLabel;
        }

        private string BuildFallbackTitle()
        {
            return rewardType switch
            {
                CollectionRewardType.Coins => $"{Amount} монет",
                CollectionRewardType.Undo => $"+{Amount} Отмена",
                CollectionRewardType.Hint => $"+{Amount} подсказка",
                CollectionRewardType.ExtraMove => $"+{Amount} хода",
                CollectionRewardType.SmallChest => "Малый сундук",
                CollectionRewardType.BigChest => "Большой сундук",
                CollectionRewardType.Booster => "Бустер",
                CollectionRewardType.EpicReward => "Эпическая награда",
                _ => "Награда"
            };
        }
    }

    public sealed class CollectionBookManager : MonoBehaviour
    {
        private const string PointsKey = "CardMesh.CollectionBook.Points";
        private const string ClaimedCountKey = "CardMesh.CollectionBook.ClaimedCount";

        [Header("Scoring")]
        [SerializeField, Min(0)] private int normalCategoryPoints = 1;
        [SerializeField, Min(0)] private int imageCategoryPoints = 2;
        [SerializeField, Min(0)] private int noHintBonusPoints = 1;
        [SerializeField, Min(0)] private int noUndoBonusPoints = 1;

        [Header("Progression")]
        [SerializeField] private List<string> levelTitles = new()
        {
            "Коллекционер I",
            "Коллекционер II",
            "Коллекционер III",
            "Исследователь категорий",
            "Мастер коллекций",
            "Хранитель архива",
            "Легенда ассоциаций"
        };

        [SerializeField] private List<CollectionBookReward> rewards = new()
        {
            new CollectionBookReward(10, CollectionRewardType.Undo, 1, "+1 Отмена", "+1"),
            new CollectionBookReward(25, CollectionRewardType.Hint, 1, "+1 Подсказка", "+1"),
            new CollectionBookReward(50, CollectionRewardType.Coins, 50, "50 монет", "50"),
            new CollectionBookReward(75, CollectionRewardType.Undo, 2, "+2 Отмены", "+2"),
            new CollectionBookReward(100, CollectionRewardType.Hint, 2, "+2 Подсказки", "+2"),
            new CollectionBookReward(150, CollectionRewardType.ExtraMove, 3, "+3 хода", "+3"),
            new CollectionBookReward(250, CollectionRewardType.BigChest, 1, "Большой сундук", "сундук"),
            new CollectionBookReward(400, CollectionRewardType.Booster, 1, "Редкий бустер", "x1"),
            new CollectionBookReward(600, CollectionRewardType.EpicReward, 1, "Эпическая награда", "x1")
        };

        [Header("Reward Services")]
        [SerializeField] private CoinsManager coinsManager;
        [SerializeField] private PlayerBoostInventory boostInventory;

        public int CurrentPoints { get; private set; }
        public int ClaimedRewardsCount { get; private set; }
        public IReadOnlyList<CollectionBookReward> Rewards => rewards;
        public int NormalCategoryPoints => Mathf.Max(0, normalCategoryPoints);
        public int ImageCategoryPoints => Mathf.Max(0, imageCategoryPoints);
        public int NoHintBonusPoints => Mathf.Max(0, noHintBonusPoints);
        public int NoUndoBonusPoints => Mathf.Max(0, noUndoBonusPoints);
        public bool HasAvailableReward => GetNextReward() != null && CurrentPoints >= GetNextReward().RequiredPoints;

        public event Action StateChanged;

        public static CollectionBookManager GetOrCreate(Component context = null)
        {
            CollectionBookManager manager = FindFirstObjectByType<CollectionBookManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                return manager;
            }

            GameObject owner = context != null ? context.gameObject : new GameObject("CollectionBookManager");
            manager = owner.GetComponent<CollectionBookManager>();
            if (manager == null)
            {
                manager = owner.AddComponent<CollectionBookManager>();
            }

            return manager;
        }

        private void Awake()
        {
            Load();
            EnsureServices();
        }

        private void OnValidate()
        {
            rewards.Sort((left, right) => left.RequiredPoints.CompareTo(right.RequiredPoints));
        }

        public CollectionBookReward GetNextReward()
        {
            if (rewards == null || rewards.Count == 0 || ClaimedRewardsCount >= rewards.Count)
            {
                return null;
            }

            return rewards[Mathf.Clamp(ClaimedRewardsCount, 0, rewards.Count - 1)];
        }

        public CollectionBookReward GetReward(int index)
        {
            if (rewards == null || index < 0 || index >= rewards.Count)
            {
                return null;
            }

            return rewards[index];
        }

        public string GetLevelTitle()
        {
            if (levelTitles == null || levelTitles.Count == 0)
            {
                return "Коллекционер";
            }

            int index = Mathf.Clamp(ClaimedRewardsCount / 2, 0, levelTitles.Count - 1);
            return levelTitles[index];
        }

        public int GetPreviousMilestonePoints()
        {
            if (ClaimedRewardsCount <= 0 || rewards == null || rewards.Count == 0)
            {
                return 0;
            }

            int index = Mathf.Clamp(ClaimedRewardsCount - 1, 0, rewards.Count - 1);
            return rewards[index].RequiredPoints;
        }

        public int AddCategoryCompletion(bool isImageCategory, bool usedHint, bool usedUndo)
        {
            int amount = isImageCategory ? ImageCategoryPoints : NormalCategoryPoints;
            if (!usedHint)
            {
                amount += NoHintBonusPoints;
            }

            if (!usedUndo)
            {
                amount += NoUndoBonusPoints;
            }

            return AddPoints(amount);
        }

        public int AddPoints(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (safeAmount <= 0)
            {
                return CurrentPoints;
            }

            CurrentPoints += safeAmount;
            Save();
            StateChanged?.Invoke();
            return CurrentPoints;
        }

        public bool TryClaimNextReward()
        {
            CollectionBookReward reward = GetNextReward();
            if (reward == null || CurrentPoints < reward.RequiredPoints)
            {
                return false;
            }

            ApplyReward(reward);
            ClaimedRewardsCount = Mathf.Clamp(ClaimedRewardsCount + 1, 0, rewards.Count);
            Save();
            StateChanged?.Invoke();
            return true;
        }

        public void DebugAddOnePoint() => AddPoints(1);
        public void DebugAddTenPoints() => AddPoints(10);
        public void DebugAddHundredPoints() => AddPoints(100);

        public void DebugResetProgress()
        {
            CurrentPoints = 0;
            ClaimedRewardsCount = 0;
            Save();
            StateChanged?.Invoke();
        }

        public void DebugUnlockAllRewards()
        {
            if (rewards == null || rewards.Count == 0)
            {
                return;
            }

            CurrentPoints = rewards[^1].RequiredPoints;
            ClaimedRewardsCount = rewards.Count;
            Save();
            StateChanged?.Invoke();
        }

        private void ApplyReward(CollectionBookReward reward)
        {
            EnsureServices();

            switch (reward.RewardType)
            {
                case CollectionRewardType.Coins:
                    coinsManager?.AddCoins(reward.Amount);
                    break;
                case CollectionRewardType.Hint:
                    boostInventory?.AddHints(reward.Amount);
                    break;
                case CollectionRewardType.Undo:
                    boostInventory?.AddUndos(reward.Amount);
                    break;
                case CollectionRewardType.ExtraMove:
                case CollectionRewardType.SmallChest:
                case CollectionRewardType.BigChest:
                case CollectionRewardType.Booster:
                case CollectionRewardType.EpicReward:
                    PlayerPrefs.SetInt($"CardMesh.CollectionBook.Reward.{reward.RewardType}", PlayerPrefs.GetInt($"CardMesh.CollectionBook.Reward.{reward.RewardType}", 0) + reward.Amount);
                    PlayerPrefs.Save();
                    break;
            }
        }

        private void EnsureServices()
        {
            if (coinsManager == null)
            {
                coinsManager = FindFirstObjectByType<CoinsManager>(FindObjectsInactive.Include);
            }

            if (coinsManager == null)
            {
                coinsManager = gameObject.AddComponent<CoinsManager>();
            }

            boostInventory ??= PlayerBoostInventory.GetOrCreate(this);
        }

        private void Load()
        {
            CurrentPoints = Mathf.Max(0, PlayerPrefs.GetInt(PointsKey, 0));
            ClaimedRewardsCount = Mathf.Clamp(PlayerPrefs.GetInt(ClaimedCountKey, 0), 0, rewards != null ? rewards.Count : 0);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PointsKey, CurrentPoints);
            PlayerPrefs.SetInt(ClaimedCountKey, ClaimedRewardsCount);
            PlayerPrefs.Save();
        }
    }
}
