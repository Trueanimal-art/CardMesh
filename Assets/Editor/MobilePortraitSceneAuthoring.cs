using CardMesh;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh.EditorTools
{
    public static class MobilePortraitSceneAuthoring
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string MainMenuScenePath = "Assets/MainMenu.unity";
        private const string GameplayBackgroundPath =
            "Assets/Art/Backgrounds/BG_Game_Mobile_9x20.png";
        private const string MainMenuBackgroundPath =
            "Assets/Art/Backgrounds/BG_MainMenu_Mobile_9x20.png";

        [MenuItem("Tools/Card Mesh/Mobile/Apply Portrait Scene Layouts")]
        public static void ApplyPortraitSceneLayouts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Portrait scene layouts can only be authored in Edit Mode.");
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            string previousActivePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                ApplyGameplayScene();
                ApplyMainMenuScene();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                else if (!string.IsNullOrEmpty(previousActivePath))
                {
                    EditorSceneManager.OpenScene(previousActivePath, OpenSceneMode.Single);
                }
            }

            Debug.Log(
                "Mobile portrait layouts authored directly into Main and MainMenu scenes.");
        }

        [MenuItem("Tools/Card Mesh/Mobile/Apply Gameplay Portrait Layout")]
        public static void ApplyGameplayPortraitLayout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Gameplay portrait layout can only be authored in Edit Mode.");
                return;
            }

            ApplyGameplayScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Gameplay portrait layout authored directly into Main scene.");
        }

        private static void ApplyGameplayScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ConfigurePortraitCanvases(scene);
            AssignBackground(scene, GameplayBackgroundPath);
            MobileDisplayBootstrap.ApplyGameplayLayoutToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            ConfigurePortraitCanvases(scene);
            AssignBackground(scene, MainMenuBackgroundPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePortraitCanvases(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler == null)
                    {
                        scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                    }

                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080f, 1920f);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0f;
                    EditorUtility.SetDirty(scaler);
                }
            }
        }

        private static void AssignBackground(Scene scene, string assetPath)
        {
            ConfigureBackgroundImporter(assetPath);
            Sprite backgroundSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (backgroundSprite == null)
            {
                Debug.LogError($"Missing mobile background: {assetPath}");
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.name != "Background")
                    {
                        continue;
                    }

                    image.sprite = backgroundSprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    EditorUtility.SetDirty(image);
                    return;
                }
            }
        }

        private static void ConfigureBackgroundImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;

            TextureImporterPlatformSettings android =
                importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 4096;
            android.format = TextureImporterFormat.ASTC_8x8;
            android.textureCompression =
                TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }
}
