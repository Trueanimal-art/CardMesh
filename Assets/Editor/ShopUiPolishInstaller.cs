using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh.Editor
{
    public static class ShopUiPolishInstaller
    {
        private static readonly string[] ShopSpritePaths =
        {
            "Assets/Resources/UI/Shop/treasury_hero_niche.png",
            "Assets/Resources/UI/Shop/treasury_card_niche.png",
            "Assets/Resources/UI/Shop/royal_chest_open.png"
        };

        [MenuItem("Tools/CardMesh/Polish Shop Popup")]
        public static void PolishShopPopup()
        {
            PrepareShopSprites();

            ShopPopup popup = Object.FindFirstObjectByType<ShopPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Shop UI Polish: ShopPopup was not found in the open scene.");
                return;
            }

            popup.MaterializeAuthoringLayout();
            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Shop UI Polish: layout, cards and purchase buttons updated.");
        }

        public static void RebuildShopPopupAssets()
        {
            EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            PolishShopPopup();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }

        private static void PrepareShopSprites()
        {
            foreach (string path in ShopSpritePaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled ||
                               importer.alphaIsTransparency == false;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        [MenuItem("Tools/CardMesh/Preview Shop Popup")]
        public static void PreviewShopPopup()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Shop UI Preview: enter Play Mode first.");
                return;
            }

            ShopPopup popup = Object.FindFirstObjectByType<ShopPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Shop UI Preview: ShopPopup was not found.");
                return;
            }

            popup.Show();
        }
    }
}
