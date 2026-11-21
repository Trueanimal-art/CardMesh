using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh.Editor
{
    public static class CollectionBookUiPolishInstaller
    {
        private const string PopupPrefabPath = "Assets/Prefabs/UI/MainMenu/CollectionBookPopup.prefab";

        [MenuItem("Tools/CardMesh/Polish Collection Book")]
        public static void PolishCollectionBook()
        {
            CollectionBookPopup popup = Object.FindFirstObjectByType<CollectionBookPopup>(FindObjectsInactive.Include);
            if (popup != null)
            {
                popup.RestoreClassicAuthoringLayout();
                EditorUtility.SetDirty(popup);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }
            else
            {
                Debug.LogWarning("Collection Book polish: no scene instance was found; prefab will still be updated.");
            }

            PolishPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("Collection Book polish: classic scene and prefab layout restored.");
        }

        // Batch-mode entry point used by CI and isolated authoring runs. Opening
        // the scene explicitly makes the generated hierarchy deterministic even
        // when Unity starts without the editor's last-open scene state.
        public static void PolishCollectionBookAssets()
        {
            EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            PolishCollectionBook();
        }

        private static void PolishPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PopupPrefabPath);
            try
            {
                CollectionBookPopup popup = prefabRoot.GetComponent<CollectionBookPopup>()
                    ?? prefabRoot.GetComponentInChildren<CollectionBookPopup>(true);
                if (popup == null)
                {
                    Debug.LogError($"Collection Book polish: CollectionBookPopup is missing in {PopupPrefabPath}.");
                    return;
                }

                popup.RestoreClassicAuthoringLayout();
                EditorUtility.SetDirty(popup);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PopupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
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
