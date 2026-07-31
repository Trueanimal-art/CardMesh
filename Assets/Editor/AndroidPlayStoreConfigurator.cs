using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SolitaireAssociations.EditorTools
{
    public static class AndroidPlayStoreConfigurator
    {
        public const string PackageId = "com.onetapgarage.cardmesh";
        private const string AabPath = "Builds/Android/CardMesh.aab";
        private const string TestApkPath = "Builds/Android/CardMesh-test.apk";

        [MenuItem("Tools/CardMesh/Android/Apply Google Play Settings")]
        public static void ApplyGooglePlaySettings()
        {
            PlayerSettings.companyName = "One Tap Garage";
            PlayerSettings.productName = "CardMesh";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.minifyRelease = false;
            PlayerSettings.Android.minifyDebug = false;

            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                BuildTargetGroup.Android,
                ManagedStrippingLevel.Low);

            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"CardMesh Google Play settings applied: package={PackageId}, "
                + "portrait, target API 36, IL2CPP, ARM64, AAB.");
        }

        [MenuItem("Tools/CardMesh/Android/Build Test APK")]
        public static void BuildTestApk()
        {
            ApplyGooglePlaySettings();
            AndroidSdkVersions releaseTarget = PlayerSettings.Android.targetSdkVersion;
            try
            {
                // Unity 2022.3.43 ships AGP/aapt2 that cannot link the Android 36
                // platform. API 34 is used only for a local device smoke APK;
                // the source-controlled release target remains API 36.
                PlayerSettings.Android.targetSdkVersion =
                    AndroidSdkVersions.AndroidApiLevel34;
                EditorUserBuildSettings.buildAppBundle = false;
                Build(TestApkPath, BuildOptions.Development, requireUploadKeystore: false);
            }
            finally
            {
                PlayerSettings.Android.targetSdkVersion = releaseTarget;
                EditorUserBuildSettings.buildAppBundle = true;
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/CardMesh/Android/Build Release AAB")]
        public static void BuildReleaseAab()
        {
            ApplyGooglePlaySettings();
            EditorUserBuildSettings.buildAppBundle = true;
            Build(AabPath, BuildOptions.CompressWithLz4HC, requireUploadKeystore: true);
        }

        public static void BatchBuildTestApk()
        {
            BuildTestApk();
        }

        public static void BatchBuildReleaseAab()
        {
            BuildReleaseAab();
        }

        private static void Build(
            string outputPath,
            BuildOptions options,
            bool requireUploadKeystore)
        {
            if (requireUploadKeystore
                && (!PlayerSettings.Android.useCustomKeystore
                    || string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName)
                    || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)))
            {
                throw new BuildFailedException(
                    "Release AAB requires an upload keystore. Configure it locally in "
                    + "Project Settings > Player > Android > Publishing Settings. "
                    + "Do not commit keystore files or passwords.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                throw new BuildFailedException("Unity could not switch to Android.");
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes in Build Settings.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Android build failed: {report.summary.result}; "
                    + $"errors={report.summary.totalErrors}.");
            }

            Debug.Log(
                $"Android build succeeded: {outputPath}; "
                + $"size={report.summary.totalSize} bytes.");
        }
    }
}
