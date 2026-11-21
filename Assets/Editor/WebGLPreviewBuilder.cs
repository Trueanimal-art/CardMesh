using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardMesh.EditorTools
{
    /// <summary>
    /// Browser preview builds (to play from a phone without installing an APK).
    /// QA build adds CARDMESH_QA (layout audit bridge); both add
    /// CARDMESH_WEB_PREVIEW (rewarded ads are granted instantly, no ad SDK).
    /// </summary>
    public static class WebGLPreviewBuilder
    {
        private const string PreviewPath = "Builds/WebGL/Preview";
        private const string QaPath = "Builds/WebGL/QA";

        [MenuItem("Tools/Card Mesh/WebGL/Build Preview")]
        public static void BuildPreview()
        {
            Build(PreviewPath, qa: false);
        }

        [MenuItem("Tools/Card Mesh/WebGL/Build QA")]
        public static void BuildQa()
        {
            Build(QaPath, qa: true);
        }

        public static void BatchBuildPreview()
        {
            BuildOrExit(() => Build(PreviewPath, qa: false));
        }

        public static void BatchBuildQa()
        {
            BuildOrExit(() => Build(QaPath, qa: true));
        }

        private static void BuildOrExit(Action build)
        {
            try
            {
                build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Build(string outputPath, bool qa)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            }

            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            ApplyWebGLTextureOverrides();

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "PROJECT:CardMeshMobile";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            string[] defines = qa
                ? new[] { "CARDMESH_WEB_PREVIEW", "CARDMESH_QA" }
                : new[] { "CARDMESH_WEB_PREVIEW" };

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
                extraScriptingDefines = defines
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"WebGL build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            Debug.Log($"WebGL build OK: {outputPath}, {report.summary.totalSize / (1024 * 1024)} MB");
        }

        // Most UI art is imported uncompressed (fine for the Android APK, far too heavy
        // for a phone browser). WebGL gets its own ASTC override; Android settings stay as is.
        private static void ApplyWebGLTextureOverrides()
        {
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    {
                        continue;
                    }

                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    int largest = Mathf.Max(width, height);
                    int maxSize = Mathf.Min(importer.maxTextureSize, 2048);
                    TextureImporterFormat format = largest >= 1024
                        ? TextureImporterFormat.ASTC_6x6
                        : TextureImporterFormat.ASTC_4x4;

                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("WebGL");
                    if (settings.overridden && settings.format == format && settings.maxTextureSize == maxSize)
                    {
                        continue;
                    }

                    settings.overridden = true;
                    settings.format = format;
                    settings.maxTextureSize = maxSize;
                    settings.compressionQuality = 50;
                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"WebGL texture overrides applied to {changed} textures.");
        }
    }
}
