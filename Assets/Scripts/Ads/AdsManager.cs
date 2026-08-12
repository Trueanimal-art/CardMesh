using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool bannerDesiredVisible;
    private bool applicationPaused;
    private readonly HashSet<string> bannerBlockers = new();
    private float nextBannerHeartbeatAt;
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
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        UpdateBannerVisibility(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        provider?.Dispose();
        provider = null;
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        Instance = null;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextBannerHeartbeatAt)
        {
            return;
        }

        nextBannerHeartbeatAt = Time.unscaledTime + 1f;
        RestoreBannerVisibility();
    }

    public void SetBannerBlocker(string reason, bool active)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        if (active)
        {
            bannerBlockers.Add(reason);
        }
        else
        {
            bannerBlockers.Remove(reason);
        }

        RestoreBannerVisibility();
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
        provider.SetBannerVisible(false);
        bool resolved = false;

        bool started = provider.ShowRewarded(
            onCompleted: () =>
            {
                if (resolved)
                {
                    return;
                }

                resolved = true;
                Log($"Rewarded completed: {safeReason}");
                FinishAdAudioRestore();
                adInProgress = false;
                RestoreBannerVisibility();
                onCompleted?.Invoke();
            },
            onFailedOrClosed: () =>
            {
                if (resolved)
                {
                    return;
                }

                resolved = true;
                Log("Rewarded failed");
                FinishAdAudioRestore();
                adInProgress = false;
                RestoreBannerVisibility();
                onFailed?.Invoke();
            });

        if (!started)
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            Log("Rewarded failed");
            FinishAdAudioRestore();
            adInProgress = false;
            RestoreBannerVisibility();
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
        provider.SetBannerVisible(false);
        bool resolved = false;

        bool started = provider.ShowInterstitial(
            onCompleted: () =>
            {
                if (resolved)
                {
                    return;
                }

                resolved = true;
                MarkInterstitialShown();
                Log("Interstitial completed");
                FinishAdAudioRestore();
                adInProgress = false;
                RestoreBannerVisibility();
                onCompleted?.Invoke();
            },
            onFailed: () =>
            {
                if (resolved)
                {
                    return;
                }

                resolved = true;
                Log("Interstitial failed");
                FinishAdAudioRestore();
                adInProgress = false;
                RestoreBannerVisibility();
                onSkippedOrFailed?.Invoke();
            });

        if (!started)
        {
            if (resolved)
            {
                return false;
            }

            resolved = true;
            Log("Interstitial failed");
            FinishAdAudioRestore();
            adInProgress = false;
            RestoreBannerVisibility();
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

    public bool PrivacyOptionsRequired =>
        provider != null && provider.PrivacyOptionsRequired;

    public void ShowPrivacyOptions(Action onClosed = null, Action onFailed = null)
    {
        if (provider == null || !provider.ShowPrivacyOptions(onClosed, onFailed))
        {
            onFailed?.Invoke();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        applicationPaused = pauseStatus;
        if (pauseStatus)
        {
            provider?.SetBannerVisible(false);
            return;
        }

        RestoreBannerVisibility();
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        UpdateBannerVisibility(nextScene);
    }

    private void UpdateBannerVisibility(Scene scene)
    {
        bannerDesiredVisible =
            scene.IsValid()
            && string.Equals(scene.name, "Main", StringComparison.Ordinal);
        if (!bannerDesiredVisible)
        {
            bannerBlockers.Clear();
        }
        RestoreBannerVisibility();
    }

    private void RestoreBannerVisibility()
    {
        provider?.SetBannerVisible(
            bannerDesiredVisible
            && !adInProgress
            && !applicationPaused
            && bannerBlockers.Count == 0);
    }

    private IAdsProvider CreateProvider()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new GoogleMobileAdsProvider();
#elif UNITY_EDITOR
        return new MockAdsProvider(() => mockRewardedShouldFail, () => mockInterstitialShouldFail);
#else
        return new UnavailableAdsProvider();
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
        public bool PrivacyOptionsRequired => false;

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

        public bool ShowPrivacyOptions(Action onClosed, Action onFailed)
        {
            onClosed?.Invoke();
            return true;
        }

        public void SetBannerVisible(bool visible)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class UnavailableAdsProvider : IAdsProvider
    {
        public string ProviderName => "UnavailableAdsProvider";
        public bool PrivacyOptionsRequired => false;

        public bool ShowRewarded(
            Action onCompleted,
            Action onFailedOrClosed)
        {
            return false;
        }

        public bool ShowInterstitial(Action onCompleted, Action onFailed)
        {
            return false;
        }

        public bool ShowPrivacyOptions(Action onClosed, Action onFailed)
        {
            return false;
        }

        public void SetBannerVisible(bool visible)
        {
        }

        public void Dispose()
        {
        }
    }
}

internal interface IAdsProvider : IDisposable
{
    string ProviderName { get; }
    bool PrivacyOptionsRequired { get; }
    bool ShowRewarded(Action onCompleted, Action onFailedOrClosed);
    bool ShowInterstitial(Action onCompleted, Action onFailed);
    bool ShowPrivacyOptions(Action onClosed, Action onFailed);
    void SetBannerVisible(bool visible);
}
