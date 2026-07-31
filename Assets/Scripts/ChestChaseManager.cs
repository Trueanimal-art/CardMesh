using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardMesh
{
    public sealed class ChestChaseManager : MonoBehaviour
    {
        private const string CurrentStreakKey = "CardMesh.ChestChase.CurrentStreak";
        private const string CurrentStageKey = "CardMesh.ChestChase.CurrentStage";
        private const string CurrentKeysKey = "CardMesh.ChestChase.CurrentKeys";
        private const string EndsAtTicksKey = "CardMesh.ChestChase.EndsAtTicks";
        private const int DefaultDurationHours = 41;

        [SerializeField] private ChestChaseRewardConfig rewardConfig;
        [SerializeField] private CoinsManager coinsManager;
        [SerializeField] private PlayerBoostInventory boostInventory;

        private readonly List<ChestChaseRewardTier> defaultTiers = new()
        {
            new ChestChaseRewardTier(300, 1, 0, 20),
            new ChestChaseRewardTier(500, 1, 1, 20),
            new ChestChaseRewardTier(700, 2, 2, 20),
            new ChestChaseRewardTier(1000, 3, 3, 20),
            new ChestChaseRewardTier(1500, 5, 5, 20)
        };

        public int CurrentStreak { get; private set; }
        public int CurrentStage { get; private set; }
        public int CurrentKeys { get; private set; }
        public DateTime EndsAtUtc { get; private set; }
        public ChestChaseRewardTier LastClaimedReward { get; private set; }

        public event Action StateChanged;
        public event Action<ChestChaseRewardTier> RewardClaimed;

        public IReadOnlyList<ChestChaseRewardTier> Tiers =>
            rewardConfig != null && rewardConfig.Tiers != null && rewardConfig.Tiers.Count > 0
                ? rewardConfig.Tiers
                : defaultTiers;

        public ChestChaseRewardTier CurrentTier => GetTier(CurrentStage);
        public int KeysRequired => CurrentTier != null ? CurrentTier.KeysRequired : 20;
        public TimeSpan TimeRemaining => EndsAtUtc > DateTime.UtcNow ? EndsAtUtc - DateTime.UtcNow : TimeSpan.Zero;

        private void Awake()
        {
            EnsureDependencies();
            Load();
            RefreshExpiredEvent();
        }

        public static ChestChaseManager GetOrCreate(Component context = null)
        {
            ChestChaseManager manager = FindFirstObjectByType<ChestChaseManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                manager.EnsureDependencies();
                manager.RefreshExpiredEvent();
                return manager;
            }

            GameObject owner = context != null ? context.gameObject : new GameObject("ChestChaseManager");
            manager = owner.GetComponent<ChestChaseManager>();
            if (manager == null)
            {
                manager = owner.AddComponent<ChestChaseManager>();
            }

            manager.EnsureDependencies();
            manager.Load();
            manager.RefreshExpiredEvent();
            return manager;
        }

        public ChestChaseRewardTier GetTier(int index)
        {
            IReadOnlyList<ChestChaseRewardTier> tiers = Tiers;
            if (tiers == null || tiers.Count == 0)
            {
                return null;
            }

            return tiers[Mathf.Clamp(index, 0, tiers.Count - 1)];
        }

        public void RegisterLevelWon()
        {
            RefreshExpiredEvent();
            CurrentStreak++;
            CurrentKeys++;

            while (CurrentKeys >= KeysRequired)
            {
                CurrentKeys -= KeysRequired;
                ClaimCurrentStageReward();
                CurrentStage = Mathf.Min(CurrentStage + 1, Tiers.Count - 1);
            }

            Save();
            StateChanged?.Invoke();
        }

        public void RegisterLevelLost()
        {
            RefreshExpiredEvent();
            if (CurrentStreak == 0 && CurrentKeys == 0)
            {
                return;
            }

            CurrentStreak = 0;
            CurrentKeys = 0;
            Save();
            StateChanged?.Invoke();
        }

        public void DebugAddWin()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterLevelWon();
#endif
        }

        public void DebugResetStreak()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CurrentStreak = 0;
            CurrentKeys = 0;
            CurrentStage = 0;
            Save();
            StateChanged?.Invoke();
#endif
        }

        public void DebugOpenNextChest()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CurrentKeys = KeysRequired - 1;
            RegisterLevelWon();
#endif
        }

        private void ClaimCurrentStageReward()
        {
            ChestChaseRewardTier reward = CurrentTier;
            if (reward == null)
            {
                return;
            }

            LastClaimedReward = reward;
            if (coinsManager != null)
            {
                coinsManager.AddCoins(reward.Coins);
            }

            if (boostInventory != null)
            {
                // Chest rewards feed the shared consumable inventory used by gameplay buttons.
                boostInventory.AddHints(reward.Hints);
                boostInventory.AddUndos(reward.Undos);
            }

            RewardClaimed?.Invoke(reward);
        }

        private void EnsureDependencies()
        {
            if (coinsManager == null)
            {
                coinsManager = FindFirstObjectByType<CoinsManager>(FindObjectsInactive.Include);
                if (coinsManager == null)
                {
                    coinsManager = gameObject.AddComponent<CoinsManager>();
                }
            }

            boostInventory ??= PlayerBoostInventory.GetOrCreate(this);
        }

        private void RefreshExpiredEvent()
        {
            if (EndsAtUtc > DateTime.UtcNow)
            {
                return;
            }

            CurrentStreak = 0;
            CurrentStage = 0;
            CurrentKeys = 0;
            EndsAtUtc = DateTime.UtcNow.AddHours(rewardConfig != null ? rewardConfig.EventDurationHours : DefaultDurationHours);
            Save();
        }

        private void Load()
        {
            CurrentStreak = Mathf.Max(0, PlayerPrefs.GetInt(CurrentStreakKey, 0));
            CurrentStage = Mathf.Clamp(PlayerPrefs.GetInt(CurrentStageKey, 0), 0, Mathf.Max(0, Tiers.Count - 1));
            CurrentKeys = Mathf.Max(0, PlayerPrefs.GetInt(CurrentKeysKey, 0));

            string ticksValue = PlayerPrefs.GetString(EndsAtTicksKey, string.Empty);
            EndsAtUtc = long.TryParse(ticksValue, out long ticks)
                ? new DateTime(ticks, DateTimeKind.Utc)
                : DateTime.UtcNow.AddHours(rewardConfig != null ? rewardConfig.EventDurationHours : DefaultDurationHours);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(CurrentStreakKey, CurrentStreak);
            PlayerPrefs.SetInt(CurrentStageKey, CurrentStage);
            PlayerPrefs.SetInt(CurrentKeysKey, CurrentKeys);
            PlayerPrefs.SetString(EndsAtTicksKey, EndsAtUtc.Ticks.ToString());
            PlayerPrefs.Save();
        }
    }
}
