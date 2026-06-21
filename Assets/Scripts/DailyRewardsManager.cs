using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public enum DailyRewardType
    {
        Coins,
        Hint,
        ExtraMoves
    }

    [Serializable]
    public sealed class DailyReward
    {
        [SerializeField, Range(1, 7)] private int day = 1;
        [SerializeField] private DailyRewardType rewardType = DailyRewardType.Coins;
        [SerializeField, Min(1)] private int amount = 50;
        [SerializeField] private string title = "50 монет";
        [SerializeField] private string description = "Монеты пригодятся для будущих покупок.";

        public int Day => Mathf.Clamp(day, 1, 7);
        public DailyRewardType RewardType => rewardType;
        public int Amount => Mathf.Max(1, amount);
        public string Title => string.IsNullOrWhiteSpace(title) ? BuildFallbackTitle() : title;
        public string Description => string.IsNullOrWhiteSpace(description) ? BuildFallbackDescription() : description;

        public DailyReward(int day, DailyRewardType rewardType, int amount, string title, string description)
        {
            this.day = Mathf.Clamp(day, 1, 7);
            this.rewardType = rewardType;
            this.amount = Mathf.Max(1, amount);
            this.title = title;
            this.description = description;
        }

        private string BuildFallbackTitle()
        {
            return rewardType switch
            {
                DailyRewardType.Coins => $"{Amount} монет",
                DailyRewardType.Hint => Amount == 1 ? "1 подсказка" : $"{Amount} подсказки",
                DailyRewardType.ExtraMoves => $"+{Amount} ходов",
                _ => "Награда"
            };
        }

        private string BuildFallbackDescription()
        {
            return rewardType switch
            {
                DailyRewardType.Coins => "Монеты пригодятся для будущих покупок.",
                DailyRewardType.Hint => "Используй подсказку, если застрял на уровне.",
                DailyRewardType.ExtraMoves => "Дополнительные ходы помогут дожать сложный уровень.",
                _ => "Возвращайся каждый день за новой наградой."
            };
        }
    }

    public sealed class DailyRewardsManager : MonoBehaviour
    {
        private const string CurrentDayKey = "CardMesh.DailyRewards.CurrentDay";
        private const string ClaimedTodayKey = "CardMesh.DailyRewards.ClaimedToday";
        private const string LastClaimDateKey = "CardMesh.DailyRewards.LastClaimDate";
        private const string ExtraMovesKey = "CardMesh.Boosts.ExtraMoves";
        private const string DateFormat = "yyyyMMdd";

        [SerializeField] private List<DailyReward> rewards = new()
        {
            new DailyReward(1, DailyRewardType.Coins, 50, "50 монет", "Монеты пригодятся для будущих покупок."),
            new DailyReward(2, DailyRewardType.Coins, 75, "75 монет", "Небольшой ежедневный запас монет."),
            new DailyReward(3, DailyRewardType.Hint, 1, "1 подсказка", "Используй подсказку, если застрял на уровне."),
            new DailyReward(4, DailyRewardType.Coins, 100, "100 монет", "Больше монет для уверенного старта."),
            new DailyReward(5, DailyRewardType.ExtraMoves, 5, "+5 ходов", "Дополнительные ходы помогут дожать сложный уровень."),
            new DailyReward(6, DailyRewardType.Coins, 150, "150 монет", "Премиальная ежедневная награда."),
            new DailyReward(7, DailyRewardType.Coins, 300, "300 монет + 1 подсказка", "Большой финал серии: монеты и подсказка.")
        };

        [Header("Reward Services")]
        [SerializeField] private CoinsManager coinsManager;
        [SerializeField] private PlayerBoostInventory boostInventory;

        public int CurrentDay { get; private set; } = 1;
        public bool ClaimedToday { get; private set; }
        public IReadOnlyList<DailyReward> Rewards => rewards;
        public bool CanClaimToday => !ClaimedToday;

        public event Action StateChanged;
        public event Action<DailyReward> RewardClaimed;

        public static DailyRewardsManager GetOrCreate(Component context = null)
        {
            DailyRewardsManager manager = FindFirstObjectByType<DailyRewardsManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                return manager;
            }

            GameObject owner = context != null ? context.gameObject : new GameObject("DailyRewardsManager");
            manager = owner.GetComponent<DailyRewardsManager>();
            if (manager == null)
            {
                manager = owner.AddComponent<DailyRewardsManager>();
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
            rewards.Sort((left, right) => left.Day.CompareTo(right.Day));
        }

        public DailyReward GetReward(int day)
        {
            int safeDay = Mathf.Clamp(day, 1, 7);
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].Day == safeDay)
                {
                    return rewards[i];
                }
            }

            return rewards.Count > 0 ? rewards[Mathf.Clamp(safeDay - 1, 0, rewards.Count - 1)] : null;
        }

        public DailyReward GetCurrentReward() => GetReward(CurrentDay);

        public bool IsDayClaimed(int day)
        {
            int safeDay = Mathf.Clamp(day, 1, 7);
            return safeDay < CurrentDay || (ClaimedToday && safeDay == CurrentDay);
        }

        public bool IsDayCurrent(int day) => day == CurrentDay && !ClaimedToday;

        public bool IsDayLocked(int day)
        {
            int safeDay = Mathf.Clamp(day, 1, 7);
            return safeDay > CurrentDay;
        }

        public bool TryClaimToday()
        {
            Load();
            if (ClaimedToday)
            {
                return false;
            }

            DailyReward reward = GetCurrentReward();
            if (reward == null)
            {
                return false;
            }

            ApplyReward(reward);
            ClaimedToday = true;
            PlayerPrefs.SetInt(ClaimedTodayKey, 1);
            PlayerPrefs.SetString(LastClaimDateKey, TodayKey());
            PlayerPrefs.SetInt(CurrentDayKey, CurrentDay);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
            RewardClaimed?.Invoke(reward);
            return true;
        }

        [ContextMenu("Daily Rewards/Add Test Day")]
        public void DebugAddTestDay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CurrentDay = CurrentDay % 7 + 1;
            ClaimedToday = false;
            PlayerPrefs.SetInt(CurrentDayKey, CurrentDay);
            PlayerPrefs.SetInt(ClaimedTodayKey, 0);
            PlayerPrefs.SetString(LastClaimDateKey, string.Empty);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
#endif
        }

        [ContextMenu("Daily Rewards/Reset Progress")]
        public void DebugResetProgress()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CurrentDay = 1;
            ClaimedToday = false;
            PlayerPrefs.DeleteKey(CurrentDayKey);
            PlayerPrefs.DeleteKey(ClaimedTodayKey);
            PlayerPrefs.DeleteKey(LastClaimDateKey);
            PlayerPrefs.Save();
            StateChanged?.Invoke();
#endif
        }

        private void Load()
        {
            CurrentDay = Mathf.Clamp(PlayerPrefs.GetInt(CurrentDayKey, 1), 1, 7);
            ClaimedToday = PlayerPrefs.GetInt(ClaimedTodayKey, 0) == 1;

            string lastClaimDate = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
            string today = TodayKey();
            if (ClaimedToday && !string.IsNullOrEmpty(lastClaimDate) && lastClaimDate != today)
            {
                CurrentDay = CurrentDay % 7 + 1;
                ClaimedToday = false;
                PlayerPrefs.SetInt(CurrentDayKey, CurrentDay);
                PlayerPrefs.SetInt(ClaimedTodayKey, 0);
                PlayerPrefs.Save();
            }
        }

        private void ApplyReward(DailyReward reward)
        {
            EnsureServices();

            switch (reward.RewardType)
            {
                case DailyRewardType.Coins:
                    coinsManager?.AddCoins(reward.Amount);
                    if (reward.Day == 7)
                    {
                        boostInventory?.AddHints(1);
                    }
                    break;
                case DailyRewardType.Hint:
                    boostInventory?.AddHints(reward.Amount);
                    break;
                case DailyRewardType.ExtraMoves:
                    PlayerPrefs.SetInt(ExtraMovesKey, PlayerPrefs.GetInt(ExtraMovesKey, 0) + reward.Amount);
                    PlayerPrefs.Save();
                    break;
            }
        }

        private void EnsureServices()
        {
            coinsManager ??= FindFirstObjectByType<CoinsManager>(FindObjectsInactive.Include);
            if (coinsManager == null)
            {
                coinsManager = gameObject.AddComponent<CoinsManager>();
            }

            boostInventory ??= PlayerBoostInventory.GetOrCreate(this);
        }

        private static string TodayKey() => DateTime.Now.ToString(DateFormat);
    }
}
