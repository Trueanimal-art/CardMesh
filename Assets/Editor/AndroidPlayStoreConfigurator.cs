using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardMesh.EditorTools
{
    public static class AndroidPlayStoreConfigurator
    {
        public const string PackageId = "com.onetapgarage.cardmesh";
        private const string AabPath = "Builds/Android/CardMesh.aab";
        private const string TestApkPath = "Builds/Android/CardMesh-test.apk";

        [MenuItem("Tools/Card Mesh/Android/Apply Google Play Settings")]
        public static void ApplyGooglePlaySettings()
        {
            PlayerSettings.companyName = "One Tap Garage";
            PlayerSettings.productName = "Card Mesh";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.appCategory = "game";
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.minifyRelease = false;
            PlayerSettings.Android.minifyDebug = false;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android,
                ManagedStrippingLevel.Low);

            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Card Mesh Google Play settings applied: package={PackageId}, "
                + "portrait, target API 36, IL2CPP, ARM64, AAB.");
        }

        [MenuItem("Tools/Card Mesh/Android/Build Test APK")]
        public static void BuildTestApk()
        {
            ApplyGooglePlaySettings();
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                Build(TestApkPath, BuildOptions.Development, requireUploadKeystore: false);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = true;
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/Card Mesh/Android/Build Release AAB")]
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
