using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardMesh.EditorTools
{
    [InitializeOnLoad]
    public static class AdMobSettingsInstaller
    {
        private const string SettingsDirectory =
            "Assets/GoogleMobileAds/Resources";
        private const string SettingsPath =
            SettingsDirectory + "/GoogleMobileAdsSettings.asset";
        private const string SettingsTypeName =
            "GoogleMobileAds.Editor.GoogleMobileAdsSettings";

        static AdMobSettingsInstaller()
        {
            EditorApplication.delayCall += EnsureDevelopmentSettings;
        }

        [MenuItem("Tools/Card Mesh/Android/Configure AdMob Test IDs")]
        public static void EnsureDevelopmentSettings()
        {
            EnsureSettings(AdMobConfiguration.TestAndroidAppId);
        }

        internal static void EnsureSettings(string androidAppId)
        {
            if (string.IsNullOrWhiteSpace(androidAppId))
            {
                throw new BuildFailedException(
                    "The Android AdMob app ID is empty.");
            }

            Type settingsType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(SettingsTypeName, false))
                .FirstOrDefault(type => type != null);
            if (settingsType == null)
            {
                Debug.LogWarning(
                    "[ADS] Google Mobile Ads settings type is not available yet.");
                return;
            }

            Directory.CreateDirectory(SettingsDirectory);
            ScriptableObject settings =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance(settingsType);
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty androidId =
                serializedSettings.FindProperty("adMobAndroidAppId");
            if (androidId == null)
            {
                throw new BuildFailedException(
                    "Google Mobile Ads settings format is unsupported.");
            }

            if (androidId.stringValue != androidAppId)
            {
                androidId.stringValue = androidAppId;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[ADS] Android AdMob app ID synchronized.");
            }
        }
    }

    internal sealed class AdMobBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            bool isDevelopment =
                (report.summary.options & BuildOptions.Development) != 0;
            if (!isDevelopment && !AdMobConfiguration.ProductionIdsConfigured)
            {
                throw new BuildFailedException(
                    "Release build blocked: create the Card Mesh app and "
                    + "interstitial/rewarded/banner units in AdMob, then set all "
                    + "ProductionAndroid* IDs in AdMobConfiguration.cs. "
                    + "Google test IDs must never ship to production.");
            }

            string appId = isDevelopment
                ? AdMobConfiguration.TestAndroidAppId
                : AdMobConfiguration.ProductionAndroidAppId;
            AdMobSettingsInstaller.EnsureSettings(appId);
        }
    }
}
