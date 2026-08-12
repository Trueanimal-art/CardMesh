using UnityEngine;

public static class AdMobConfiguration
{
    private const string GoogleTestPublisherId = "3940256099942544";
    public const string TestAndroidAppId =
        "ca-app-pub-3940256099942544~3347511713";
    public const string TestAndroidInterstitialAdUnitId =
        "ca-app-pub-3940256099942544/1033173712";
    public const string TestAndroidRewardedAdUnitId =
        "ca-app-pub-3940256099942544/5224354917";
    public const string TestAndroidAnchoredAdaptiveBannerAdUnitId =
        "ca-app-pub-3940256099942544/9214589741";

    // Create these identifiers in the Card Mesh app inside the owner's AdMob
    // account. Release builds are blocked while any value remains empty.
    public const string ProductionAndroidAppId =
        "ca-app-pub-4396102534113317~2713880516";
    public const string ProductionAndroidInterstitialAdUnitId =
        "ca-app-pub-4396102534113317/2174752056";
    public const string ProductionAndroidRewardedAdUnitId =
        "ca-app-pub-4396102534113317/8115871690";
    public const string ProductionAndroidBannerAdUnitId =
        "ca-app-pub-4396102534113317/8530804667";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public const string AndroidInterstitialAdUnitId =
        TestAndroidInterstitialAdUnitId;
    public const string AndroidRewardedAdUnitId =
        TestAndroidRewardedAdUnitId;
    public const string AndroidBannerAdUnitId =
        TestAndroidAnchoredAdaptiveBannerAdUnitId;
#else
    public const string AndroidInterstitialAdUnitId =
        ProductionAndroidInterstitialAdUnitId;
    public const string AndroidRewardedAdUnitId =
        ProductionAndroidRewardedAdUnitId;
    public const string AndroidBannerAdUnitId =
        ProductionAndroidBannerAdUnitId;
#endif

    public static bool ProductionIdsConfigured =>
        IsAdMobAppId(ProductionAndroidAppId)
        && !IsGoogleTestId(ProductionAndroidAppId)
        && IsAdUnitId(ProductionAndroidInterstitialAdUnitId)
        && !IsGoogleTestId(ProductionAndroidInterstitialAdUnitId)
        && IsAdUnitId(ProductionAndroidRewardedAdUnitId)
        && !IsGoogleTestId(ProductionAndroidRewardedAdUnitId)
        && IsAdUnitId(ProductionAndroidBannerAdUnitId)
        && !IsGoogleTestId(ProductionAndroidBannerAdUnitId);

    private static bool IsAdMobAppId(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("ca-app-pub-", System.StringComparison.Ordinal)
            && value.Contains("~");
    }

    private static bool IsAdUnitId(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("ca-app-pub-", System.StringComparison.Ordinal)
            && value.Contains("/");
    }

    private static bool IsGoogleTestId(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(GoogleTestPublisherId);
    }
}
