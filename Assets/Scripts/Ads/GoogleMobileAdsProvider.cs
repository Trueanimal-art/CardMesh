#if UNITY_ANDROID
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

internal sealed class GoogleMobileAdsProvider : IAdsProvider
{
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;
    private BannerView bannerView;
    private bool consentFlowStarted;
    private bool initializing;
    private bool initialized;
    private bool interstitialLoading;
    private bool rewardedLoading;
    private bool bannerLoading;
    private bool bannerDesiredVisible;
    private bool disposed;
    private bool privacyOptionsRequired;
    private int bannerRetryAttempt;
    private float nextBannerRetryAt;
    private int bannerScreenWidth;
    private int bannerScreenHeight;
    private Rect bannerSafeArea;

    internal GoogleMobileAdsProvider()
    {
        MobileAdsEventExecutor.Initialize();
        var requestConfiguration = new RequestConfiguration
        {
            MaxAdContentRating = MaxAdContentRating.G,
            AgeRestrictedTreatment =
                GoogleMobileAds.Api.AgeRestrictedTreatment.Unspecified
        };
#if DEVELOPMENT_BUILD
        requestConfiguration.TestDeviceIds = new System.Collections.Generic.List<string>
        {
            AdRequest.TestDeviceSimulator
        };
#endif
        MobileAds.SetRequestConfiguration(requestConfiguration);
        BeginConsentFlow();
    }

    public string ProviderName => "GoogleMobileAdsProvider";
    public bool PrivacyOptionsRequired => privacyOptionsRequired;

    public void SetBannerVisible(bool visible)
    {
        bannerDesiredVisible = visible;
        if (disposed)
        {
            return;
        }

        if (!visible)
        {
            bannerView?.Hide();
            return;
        }

        TryInitializeIfAllowed();
        if (initialized)
        {
            if (bannerView != null && BannerGeometryChanged())
            {
                DestroyBanner();
            }

            LoadBanner();
            bannerView?.Show();
        }
    }

    public bool ShowInterstitial(Action onCompleted, Action onFailed)
    {
        if (!initialized || interstitialAd == null || !interstitialAd.CanShowAd())
        {
            TryInitializeIfAllowed();
            LoadInterstitial();
            return false;
        }

        InterstitialAd ad = interstitialAd;
        interstitialAd = null;
        bool finished = false;

        void Finish(bool succeeded)
        {
            RunOnMainThread(() =>
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                ad.Destroy();
                LoadInterstitial();
                if (succeeded)
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onFailed?.Invoke();
                }
            });
        }

        ad.OnAdFullScreenContentClosed += () => Finish(true);
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"[ADS] Interstitial failed to open: {error}");
            Finish(false);
        };

        try
        {
            ad.Show();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(false);
            return true;
        }
    }

    public bool ShowRewarded(Action onCompleted, Action onFailedOrClosed)
    {
        if (!initialized || rewardedAd == null || !rewardedAd.CanShowAd())
        {
            TryInitializeIfAllowed();
            LoadRewarded();
            return false;
        }

        RewardedAd ad = rewardedAd;
        rewardedAd = null;
        bool rewardEarned = false;
        bool finished = false;

        void Finish()
        {
            RunOnMainThread(() =>
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                ad.Destroy();
                LoadRewarded();
                if (rewardEarned)
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    onFailedOrClosed?.Invoke();
                }
            });
        }

        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"[ADS] Rewarded ad failed to open: {error}");
            Finish();
        };

        try
        {
            ad.Show(reward =>
            {
                // This is the only event that authorizes the in-game reward.
                rewardEarned = true;
                Debug.Log(
                    $"[ADS] Reward earned: {reward.Amount} {reward.Type}");
            });
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish();
            return true;
        }
    }

    public bool ShowPrivacyOptions(Action onClosed, Action onFailed)
    {
        if (disposed || !consentFlowStarted)
        {
            return false;
        }

        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            RunOnMainThread(() =>
            {
                UpdatePrivacyRequirement();
                DestroyLoadedAds();

                if (error != null)
                {
                    Debug.LogWarning(
                        $"[ADS] Privacy options form failed: {error.Message}");
                    onFailed?.Invoke();
                    return;
                }

                if (ConsentInformation.CanRequestAds())
                {
                    TryInitializeIfAllowed();
                    LoadAds();
                }

                onClosed?.Invoke();
            });
        });
        return true;
    }

    public void Dispose()
    {
        disposed = true;
        DestroyLoadedAds();
    }

    private void BeginConsentFlow()
    {
        if (disposed || consentFlowStarted)
        {
            return;
        }

        consentFlowStarted = true;
        // Do not assert an age-consent status before the player supplies one.
        // Play Console target-audience settings and the final consent design
        // must remain the source of truth for the 13+ release.
        var parameters = new ConsentRequestParameters();

        ConsentInformation.Update(parameters, updateError =>
        {
            RunOnMainThread(() =>
            {
                UpdatePrivacyRequirement();

                if (updateError != null)
                {
                    Debug.LogWarning(
                        $"[ADS] Consent update failed: {updateError.Message}");
                    TryInitializeIfAllowed();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    RunOnMainThread(() =>
                    {
                        UpdatePrivacyRequirement();
                        if (formError != null)
                        {
                            Debug.LogWarning(
                                $"[ADS] Consent form failed: {formError.Message}");
                        }

                        TryInitializeIfAllowed();
                    });
                });
            });
        });
    }

    private void TryInitializeIfAllowed()
    {
        if (disposed || initialized || initializing
            || !ConsentInformation.CanRequestAds())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                AdMobConfiguration.AndroidInterstitialAdUnitId)
            || string.IsNullOrWhiteSpace(
                AdMobConfiguration.AndroidRewardedAdUnitId)
            || string.IsNullOrWhiteSpace(
                AdMobConfiguration.AndroidBannerAdUnitId))
        {
            Debug.LogError(
                "[ADS] Production AdMob ad unit IDs are not configured.");
            return;
        }

        initializing = true;
#if DEVELOPMENT_BUILD
        Debug.Log(
            "[ADS] Development IDs: "
            + $"interstitial={AdMobConfiguration.AndroidInterstitialAdUnitId}, "
            + $"rewarded={AdMobConfiguration.AndroidRewardedAdUnitId}, "
            + $"banner={AdMobConfiguration.AndroidBannerAdUnitId}.");
#endif
        MobileAds.Initialize(status =>
        {
            RunOnMainThread(() =>
            {
                initializing = false;
                if (status == null)
                {
                    Debug.LogError("[ADS] Google Mobile Ads initialization failed.");
                    return;
                }

                initialized = true;
                Debug.Log("[ADS] Google Mobile Ads initialization complete.");
                LoadAds();
            });
        });
    }

    private void LoadAds()
    {
        LoadInterstitial();
        LoadRewarded();
        if (bannerDesiredVisible)
        {
            LoadBanner();
        }
    }

    private void LoadBanner()
    {
        if (disposed || !initialized || bannerLoading || bannerView != null
            || Time.unscaledTime < nextBannerRetryAt)
        {
            return;
        }

        int safeWidthDp = MobileAds.Utils.GetDeviceSafeWidth();
        AdSize bannerSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                safeWidthDp);
        bannerView = new BannerView(
            AdMobConfiguration.AndroidBannerAdUnitId,
            bannerSize,
            AdPosition.Bottom);
        bannerLoading = true;

        bannerView.OnBannerAdLoaded += () =>
        {
            RunOnMainThread(() =>
            {
                bannerLoading = false;
                bannerRetryAttempt = 0;
                nextBannerRetryAt = 0f;
                if (disposed || bannerView == null)
                {
                    return;
                }

                RememberBannerGeometry();
                PositionBannerInsideSafeArea();
                if (bannerDesiredVisible)
                {
                    bannerView.Show();
                }
                else
                {
                    bannerView.Hide();
                }

                Debug.Log(
                    $"[ADS] Banner loaded. "
                    + $"safeWidthDp={safeWidthDp}, safeArea={Screen.safeArea}, "
                    + $"sizePx={bannerView.GetWidthInPixels()}x"
                    + $"{bannerView.GetHeightInPixels()}.");
            });
        };
        bannerView.OnBannerAdLoadFailed += error =>
        {
            RunOnMainThread(() =>
            {
                bannerLoading = false;
                Debug.LogWarning($"[ADS] Banner load failed: {error}");
                DestroyBanner();
                bannerRetryAttempt++;
                nextBannerRetryAt = Time.unscaledTime
                    + Mathf.Min(60f, Mathf.Pow(2f, bannerRetryAttempt));
            });
        };
        bannerView.LoadAd(new AdRequest());
    }

    private void PositionBannerInsideSafeArea()
    {
        if (bannerView == null)
        {
            return;
        }

        float widthPx = bannerView.GetWidthInPixels();
        float heightPx = bannerView.GetHeightInPixels();
        Rect safeArea = Screen.safeArea;
        int x = Mathf.RoundToInt(
            safeArea.xMin + Mathf.Max(0f, (safeArea.width - widthPx) * 0.5f));
        int y = Mathf.RoundToInt(
            Screen.height - safeArea.yMin - heightPx);
        bannerView.SetPosition(x, y);
    }

    private void LoadInterstitial()
    {
        if (disposed || !initialized || interstitialLoading
            || (interstitialAd != null && interstitialAd.CanShowAd()))
        {
            return;
        }

        interstitialAd?.Destroy();
        interstitialAd = null;
        interstitialLoading = true;

        InterstitialAd.Load(
            AdMobConfiguration.AndroidInterstitialAdUnitId,
            new AdRequest(),
            (ad, error) =>
            {
                RunOnMainThread(() =>
                {
                    interstitialLoading = false;
                    if (disposed)
                    {
                        ad?.Destroy();
                        return;
                    }

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning(
                            $"[ADS] Interstitial load failed: {error}");
                        return;
                    }

                    interstitialAd = ad;
                    Debug.Log("[ADS] Interstitial loaded.");
                });
            });
    }

    private void LoadRewarded()
    {
        if (disposed || !initialized || rewardedLoading
            || (rewardedAd != null && rewardedAd.CanShowAd()))
        {
            return;
        }

        rewardedAd?.Destroy();
        rewardedAd = null;
        rewardedLoading = true;

        RewardedAd.Load(
            AdMobConfiguration.AndroidRewardedAdUnitId,
            new AdRequest(),
            (ad, error) =>
            {
                RunOnMainThread(() =>
                {
                    rewardedLoading = false;
                    if (disposed)
                    {
                        ad?.Destroy();
                        return;
                    }

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning(
                            $"[ADS] Rewarded load failed: {error}");
                        return;
                    }

                    rewardedAd = ad;
                    Debug.Log("[ADS] Rewarded loaded.");
                });
            });
    }

    private void DestroyLoadedAds()
    {
        interstitialAd?.Destroy();
        interstitialAd = null;
        rewardedAd?.Destroy();
        rewardedAd = null;
        DestroyBanner();
        bannerRetryAttempt = 0;
        nextBannerRetryAt = 0f;
    }

    private bool BannerGeometryChanged()
    {
        return bannerScreenWidth != Screen.width
            || bannerScreenHeight != Screen.height
            || bannerSafeArea != Screen.safeArea;
    }

    private void RememberBannerGeometry()
    {
        bannerScreenWidth = Screen.width;
        bannerScreenHeight = Screen.height;
        bannerSafeArea = Screen.safeArea;
    }

    private void DestroyBanner()
    {
        bannerView?.Destroy();
        bannerView = null;
        bannerLoading = false;
        bannerScreenWidth = 0;
        bannerScreenHeight = 0;
        bannerSafeArea = default;
    }

    private void UpdatePrivacyRequirement()
    {
        privacyOptionsRequired =
            ConsentInformation.PrivacyOptionsRequirementStatus
            == PrivacyOptionsRequirementStatus.Required;
    }

    private static void RunOnMainThread(Action action)
    {
        if (action != null)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }
}
#endif
