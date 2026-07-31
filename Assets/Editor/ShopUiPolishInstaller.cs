using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SolitaireAssociations.Editor
{
    public static class ShopUiPolishInstaller
    {
        [MenuItem("Tools/CardMesh/Polish Shop Popup")]
        public static void PolishShopPopup()
        {
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
