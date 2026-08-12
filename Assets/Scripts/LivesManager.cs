using System;
using UnityEngine;

namespace CardMesh
{
    public sealed class LivesManager : MonoBehaviour
    {
        private const string LivesKey = "CardMesh.Lives.Current";
        private const string LastRestoreTimestampKey = "CardMesh.Lives.LastRestoreUtc";

        public const int MaxLives = 5;
        public const int RestoreIntervalSeconds = 3600;

        [SerializeField, Range(0, 5)] private int currentLives = MaxLives;
        [SerializeField] private long lastRestoreUtcTimestamp;

        private float nextRefreshAt;

        public int CurrentLives => Mathf.Clamp(currentLives, 0, MaxLives);

        public event Action<int, int> OnLivesChanged;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLivesForEditorPlayMode()
        {
            long now = GetUtcNowTimestamp();
            PlayerPrefs.SetInt(LivesKey, MaxLives);
            PlayerPrefs.SetString(LastRestoreTimestampKey, now.ToString());
            PlayerPrefs.Save();
        }
#endif

        private void Awake()
        {
            Load();
            RecalculateLivesByTime();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + 1f;
            RecalculateLivesByTime();
        }

        public bool CanSpendLife()
        {
            RecalculateLivesByTime();
            return CurrentLives > 0;
        }

        public bool SpendLife()
        {
            RecalculateLivesByTime();
            if (CurrentLives <= 0)
            {
                return false;
            }

            bool timerStartsNow = CurrentLives >= MaxLives;
            currentLives = Mathf.Max(0, CurrentLives - 1);
            if (timerStartsNow)
            {
                lastRestoreUtcTimestamp = GetUtcNowTimestamp();
            }

            Save();
            NotifyLivesChanged();
            return true;
        }

        public TimeSpan GetTimeToNextLife()
        {
            RecalculateLivesByTime();
            if (CurrentLives >= MaxLives)
            {
                return TimeSpan.Zero;
            }

            long elapsed = Math.Max(0L, GetUtcNowTimestamp() - lastRestoreUtcTimestamp);
            long remainingSeconds = RestoreIntervalSeconds - (elapsed % RestoreIntervalSeconds);
            if (remainingSeconds <= 0L || remainingSeconds > RestoreIntervalSeconds)
            {
                remainingSeconds = RestoreIntervalSeconds;
            }

            return TimeSpan.FromSeconds(remainingSeconds);
        }

        public void RecalculateLivesByTime()
        {
            long now = GetUtcNowTimestamp();
            bool timestampWasInvalid = lastRestoreUtcTimestamp <= 0L || lastRestoreUtcTimestamp > now;
            if (lastRestoreUtcTimestamp <= 0L || lastRestoreUtcTimestamp > now)
            {
                lastRestoreUtcTimestamp = now;
            }

            int beforeLives = CurrentLives;
            currentLives = beforeLives;

            if (currentLives >= MaxLives)
            {
                currentLives = MaxLives;
                if (timestampWasInvalid)
                {
                    Save();
                }

                return;
            }

            long elapsedSeconds = now - lastRestoreUtcTimestamp;
            if (elapsedSeconds < RestoreIntervalSeconds)
            {
                return;
            }

            int restoredLives = (int)(elapsedSeconds / RestoreIntervalSeconds);
            currentLives = Mathf.Min(MaxLives, currentLives + restoredLives);
            lastRestoreUtcTimestamp = currentLives >= MaxLives
                ? now
                : lastRestoreUtcTimestamp + (restoredLives * RestoreIntervalSeconds);

            Save();
            if (CurrentLives != beforeLives)
            {
                NotifyLivesChanged();
            }
        }

        public bool TryAddLife()
        {
            RecalculateLivesByTime();
            int beforeLives = CurrentLives;
            currentLives = Mathf.Min(MaxLives, CurrentLives + 1);
            if (currentLives >= MaxLives)
            {
                lastRestoreUtcTimestamp = GetUtcNowTimestamp();
            }

            Save();
            if (CurrentLives != beforeLives)
            {
                NotifyLivesChanged();
                return true;
            }

            return false;
        }

        public void AddLifeForRewardedAd()
        {
            TryAddLife();
        }

        private void Load()
        {
            long now = GetUtcNowTimestamp();
            if (!PlayerPrefs.HasKey(LivesKey))
            {
                currentLives = MaxLives;
                lastRestoreUtcTimestamp = now;
                Save();
                return;
            }

            currentLives = Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, MaxLives), 0, MaxLives);
            string storedTimestamp = PlayerPrefs.GetString(LastRestoreTimestampKey, now.ToString());
            lastRestoreUtcTimestamp = long.TryParse(storedTimestamp, out long parsedTimestamp) ? parsedTimestamp : now;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(LivesKey, CurrentLives);
            PlayerPrefs.SetString(LastRestoreTimestampKey, lastRestoreUtcTimestamp.ToString());
            PlayerPrefs.Save();
        }

        private void NotifyLivesChanged()
        {
            OnLivesChanged?.Invoke(CurrentLives, MaxLives);
        }

        private static long GetUtcNowTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
