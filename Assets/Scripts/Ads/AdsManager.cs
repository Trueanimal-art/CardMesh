using System;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    private const string LastInterstitialTimeKey = "CardMesh.Ads.LastInterstitialRealtime";
    private const string ReasonAddMoves = "AddMoves";
    public const string ReasonLevelComplete = "LevelComplete";
    public const string ReasonRestart = "Restart";

    public static AdsManager Instance { get; private set; }

    [Header("Interstitial")]
    [SerializeField, Min(0f)] private float interstitialCooldownSeconds = 120f;
    [SerializeField, Min(0)] private int restartRequestsBeforeInterstitial = 2;

    [Header("Editor Mock")]
    [SerializeField] private bool mockRewardedShouldFail;
    [SerializeField] private bool mockInterstitialShouldFail;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private IAdsProvider provider;
    private int restartRequestCountThisLevel;
    private int trackedLevelNumber = -1;
    private bool adInProgress;
    private float audioVolumeBeforeAd = 1f;
    private bool audioPauseBeforeAd;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        provider = CreateProvider();
        Log($"Provider selected: {provider.ProviderName}");
    }

    public void ShowRewardedAddMoves(Action onCompleted, Action onFailed = null)
    {
        ShowRewarded(ReasonAddMoves, onCompleted, onFailed);
    }

    public void ShowRewarded(string reason, Action onCompleted, Action onFailed = null)
    {
        string safeReason = NormalizeReason(reason, ReasonAddMoves);
        Log($"Rewarded requested: {safeReason}");

        if (adInProgress)
        {
            Log("Rewarded failed");
            onFailed?.Invoke();
            return;
        }

        BeginAdAudioMute();
        adInProgress = true;

        bool started = provider.ShowRewarded(
            onCompleted: () =>
            {
                Log($"Rewarded completed: {safeReason}");
                FinishAdAudioRestore();
                adInProgress = false;
                onCompleted?.Invoke();
            },
            onFailedOrClosed: () =>
            {
                Log("Rewarded failed");
                FinishAdAudioRestore();
                adInProgress = false;
                onFailed?.Invoke();
            });

        if (!started)
        {
            Log("Rewarded failed");
            FinishAdAudioRestore();
            adInProgress = false;
            onFailed?.Invoke();
        }
    }

    public bool ShowInterstitialIfAllowed(string reason, Action onCompleted = null, Action onSkippedOrFailed = null)
    {
        string safeReason = NormalizeReason(reason, "Unknown");
        Log($"Interstitial requested: {safeReason}");

        if (safeReason == ReasonRestart)
        {
            restartRequestCountThisLevel++;
            if (restartRequestCountThisLevel <= restartRequestsBeforeInterstitial)
            {
                Log("Interstitial skipped: RestartCount");
                onSkippedOrFailed?.Invoke();
                return false;
            }
        }

        if (!CanShowInterstitial())
        {
            Log("Interstitial skipped: Cooldown");
            onSkippedOrFailed?.Invoke();
            return false;
        }

        if (adInProgress)
        {
            Log("Interstitial failed");
            onSkippedOrFailed?.Invoke();
            return false;
        }

        BeginAdAudioMute();
        adInProgress = true;

        bool started = provider.ShowInterstitial(
            onCompleted: () =>
            {
                MarkInterstitialShown();
                Log("Interstitial completed");
                FinishAdAudioRestore();
                adInProgress = false;
                onCompleted?.Invoke();
            },
            onFailed: () =>
            {
                Log("Interstitial failed");
                FinishAdAudioRestore();
                adInProgress = false;
                onSkippedOrFailed?.Invoke();
            });

        if (!started)
        {
            Log("Interstitial failed");
            FinishAdAudioRestore();
            adInProgress = false;
            onSkippedOrFailed?.Invoke();
        }

        return started;
    }

    public bool CanShowInterstitial()
    {
        return GetSecondsSinceLastInterstitial() >= interstitialCooldownSeconds;
    }

    public void MarkInterstitialShown()
    {
        PlayerPrefs.SetString(LastInterstitialTimeKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    public void ResetRestartCounterForLevel(int levelNumber)
    {
        if (trackedLevelNumber == levelNumber)
        {
            return;
        }

        trackedLevelNumber = levelNumber;
        restartRequestCountThisLevel = 0;
        Log($"Restart counter reset for level {levelNumber}");
    }

    public void TryShowRewarded(Action onSuccess, Action onFailOrClosed)
    {
        ShowRewarded("Rewarded", onSuccess, onFailOrClosed);
    }

    public void TryShowInterstitial(Action onClosedOrShown = null, Action onFail = null)
    {
        ShowInterstitialIfAllowed("Interstitial", onClosedOrShown, onFail);
    }

    public bool TryShowInterstitialWithResult(Action onClosedOrShown = null, Action onFail = null)
    {
        return ShowInterstitialIfAllowed("Interstitial", onClosedOrShown, onFail);
    }

    public void ShowInterstitial()
    {
        ShowInterstitialIfAllowed("Interstitial");
    }

    public void ShowRewarded(Action onSuccess)
    {
        ShowRewarded("Rewarded", onSuccess, null);
    }

    private IAdsProvider CreateProvider()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new YandexAdsProvider();
#else
        return new MockAdsProvider(() => mockRewardedShouldFail, () => mockInterstitialShouldFail);
#endif
    }

    private double GetSecondsSinceLastInterstitial()
    {
        string stored = PlayerPrefs.GetString(LastInterstitialTimeKey, string.Empty);
        if (string.IsNullOrEmpty(stored))
        {
            return double.PositiveInfinity;
        }

        if (!long.TryParse(stored, out long lastShown))
        {
            return double.PositiveInfinity;
        }

        return Math.Max(0d, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastShown);
    }

    private void BeginAdAudioMute()
    {
        audioVolumeBeforeAd = AudioListener.volume;
        audioPauseBeforeAd = AudioListener.pause;
        AudioListener.volume = 0f;
        AudioListener.pause = true;
        Log("Audio muted for ad");
    }

    private void FinishAdAudioRestore()
    {
        AudioListener.volume = audioVolumeBeforeAd;
        AudioListener.pause = audioPauseBeforeAd;
        Log("Audio restored after ad");
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[ADS] {message}");
        }
    }

    private static string NormalizeReason(string reason, string fallback)
    {
        return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
    }

    private interface IAdsProvider
    {
        string ProviderName { get; }
        bool ShowRewarded(Action onCompleted, Action onFailedOrClosed);
        bool ShowInterstitial(Action onCompleted, Action onFailed);
    }

    private sealed class MockAdsProvider : IAdsProvider
    {
        private readonly Func<bool> shouldFailRewarded;
        private readonly Func<bool> shouldFailInterstitial;

        public MockAdsProvider(Func<bool> shouldFailRewarded, Func<bool> shouldFailInterstitial)
        {
            this.shouldFailRewarded = shouldFailRewarded;
            this.shouldFailInterstitial = shouldFailInterstitial;
        }

        public string ProviderName => "MockAdsProvider";

        public bool ShowRewarded(Action onCompleted, Action onFailedOrClosed)
        {
            if (shouldFailRewarded?.Invoke() == true)
            {
                onFailedOrClosed?.Invoke();
                return true;
            }

            onCompleted?.Invoke();
            return true;
        }

        public bool ShowInterstitial(Action onCompleted, Action onFailed)
        {
            if (shouldFailInterstitial?.Invoke() == true)
            {
                onFailed?.Invoke();
                return true;
            }

            onCompleted?.Invoke();
            return true;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private sealed class YandexAdsProvider : IAdsProvider
    {
        public string ProviderName => "YandexAdsProvider";

        public bool ShowRewarded(Action onCompleted, Action onFailedOrClosed)
        {
            YandexAdsBridge bridge = EnsureBridge();
            if (bridge == null)
            {
                onFailedOrClosed?.Invoke();
                return false;
            }

            bool rewardCompleted = false;
            return bridge.ShowRewarded(
                onSuccess: () =>
                {
                    rewardCompleted = true;
                    onCompleted?.Invoke();
                },
                onClosed: () =>
                {
                    if (!rewardCompleted)
                    {
                        onFailedOrClosed?.Invoke();
                    }
                });
        }

        public bool ShowInterstitial(Action onCompleted, Action onFailed)
        {
            YandexAdsBridge bridge = EnsureBridge();
            if (bridge == null)
            {
                onFailed?.Invoke();
                return false;
            }

            return bridge.ShowInterstitial(onCompleted);
        }

        private static YandexAdsBridge EnsureBridge()
        {
            if (YandexAdsBridge.Instance != null)
            {
                return YandexAdsBridge.Instance;
            }

            GameObject bridgeObject = new("YandexAdsBridge");
            return bridgeObject.AddComponent<YandexAdsBridge>();
        }
    }
#endif
}
