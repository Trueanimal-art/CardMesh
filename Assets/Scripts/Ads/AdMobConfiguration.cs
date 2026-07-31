using UnityEngine;

public static class AdMobConfiguration
{
    public const string TestAndroidAppId =
        "ca-app-pub-3940256099942544~3347511713";
    public const string TestAndroidInterstitialAdUnitId =
        "ca-app-pub-3940256099942544/1033173712";
    public const string TestAndroidRewardedAdUnitId =
        "ca-app-pub-3940256099942544/5224354917";

    // Create these identifiers in the Card Mesh app inside the owner's AdMob
    // account. Release builds are blocked while any value remains empty.
    public const string ProductionAndroidAppId = "";
    public const string ProductionAndroidInterstitialAdUnitId = "";
    public const string ProductionAndroidRewardedAdUnitId = "";

    public static string AndroidInterstitialAdUnitId =>
        Debug.isDebugBuild
            ? TestAndroidInterstitialAdUnitId
            : ProductionAndroidInterstitialAdUnitId;

    public static string AndroidRewardedAdUnitId =>
        Debug.isDebugBuild
            ? TestAndroidRewardedAdUnitId
            : ProductionAndroidRewardedAdUnitId;

    public static bool ProductionIdsConfigured =>
        IsAdMobAppId(ProductionAndroidAppId)
        && IsAdUnitId(ProductionAndroidInterstitialAdUnitId)
        && IsAdUnitId(ProductionAndroidRewardedAdUnitId);

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
}
