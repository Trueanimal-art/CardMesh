using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh.Editor
{
    public static class CollectionBookUiPolishInstaller
    {
        [MenuItem("Tools/CardMesh/Polish Collection Book")]
        public static void PolishCollectionBook()
        {
            CollectionBookPopup popup = Object.FindFirstObjectByType<CollectionBookPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Collection Book polish: CollectionBookPopup was not found in the open scene.");
                return;
            }

            popup.MaterializeAuthoringLayout();
            EditorUtility.SetDirty(popup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Collection Book polish: scene-authored layout and visual states updated.");
        }

        [MenuItem("Tools/CardMesh/Preview Collection Book")]
        public static void PreviewCollectionBook()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Collection Book preview: enter Play Mode first.");
                return;
            }

            CollectionBookPopup popup = Object.FindFirstObjectByType<CollectionBookPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                Debug.LogError("Collection Book preview: CollectionBookPopup was not found.");
                return;
            }

            popup.Show(CollectionBookManager.GetOrCreate(popup));
        }
    }
}
