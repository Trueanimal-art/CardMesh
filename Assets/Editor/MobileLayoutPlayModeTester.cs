using CardMesh;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh.EditorTools
{
    public static class MobileLayoutPlayModeTester
    {
        private static int pendingColumnsCount;

        [MenuItem("Tools/Card Mesh/Mobile/Open Main Scene")]
        private static void OpenMainScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before opening the Main scene.");
                return;
            }

            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        }

        [MenuItem("Tools/Card Mesh/Mobile/Open Main Menu Scene")]
        private static void OpenMainMenuScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before opening the MainMenu scene.");
                return;
            }

            EditorSceneManager.OpenScene("Assets/MainMenu.unity");
        }

        [MenuItem("Tools/Card Mesh/Mobile/Audit Main Menu Navigation")]
        private static void AuditMainMenuNavigation()
        {
            string[] names =
            {
                "DailyRewardsButton",
                "ChestChaseButton",
                "LevelsButton",
                "CollectionBookButton",
                "PlayButton",
                "ShopButton",
                "SettingsButton"
            };

            foreach (string objectName in names)
            {
                GameObject target = GameObject.Find(objectName);
                WriteToolbarState(objectName, target != null
                    ? target.GetComponent<RectTransform>()
                    : null);
            }

            GameObject contact = GameObject.Find("ContactButton");
            GameObject rate = GameObject.Find("RateUsButton");
            if (contact != null || rate != null)
            {
                Debug.LogError("Main menu audit: legacy Contact/Rate openers must not be active on the main surface.");
            }
        }

        [MenuItem("Tools/Card Mesh/Mobile/Test 3 Columns")]
        private static void TestThreeColumns()
        {
            LoadColumns(3);
        }

        [MenuItem("Tools/Card Mesh/Mobile/Test 4 Columns")]
        private static void TestFourColumns()
        {
            LoadColumns(4);
        }

        [MenuItem("Tools/Card Mesh/Mobile/Test 5 Columns")]
        private static void TestFiveColumns()
        {
            LoadColumns(5);
        }

        [MenuItem("Tools/Card Mesh/Mobile/Reveal 3 Draw Cards")]
        private static void RevealThreeDrawCards()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Start Play Mode before revealing draw cards.");
                return;
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("Open the Main gameplay scene before revealing draw cards.");
                return;
            }

            for (int index = 0; index < 3; index++)
            {
                gameManager.DrawNextStockCard();
            }

            Debug.Log("Mobile layout test revealed three draw cards.");
        }

        [MenuItem("Tools/Card Mesh/Mobile/Test 3 Columns", true)]
        [MenuItem("Tools/Card Mesh/Mobile/Test 4 Columns", true)]
        [MenuItem("Tools/Card Mesh/Mobile/Test 5 Columns", true)]
        private static bool CanTestColumns()
        {
            return true;
        }

        private static void LoadColumns(int columnsCount)
        {
            if (!Application.isPlaying)
            {
                CardMeshSceneRefs sceneRefs =
                    Object.FindFirstObjectByType<CardMeshSceneRefs>(FindObjectsInactive.Include);
                if (sceneRefs == null)
                {
                    Debug.LogError(
                        "Mobile layout preview requires the Main scene in Edit Mode.");
                    return;
                }

                Undo.RecordObject(sceneRefs, $"Preview {columnsCount} columns");
                sceneRefs.SetEditorPreviewColumns(columnsCount);
                EditorUtility.SetDirty(sceneRefs);
                EditorSceneManager.MarkSceneDirty(sceneRefs.gameObject.scene);
                Debug.Log(
                    $"Mobile layout preview authored for {columnsCount} columns. "
                    + "Save the scene to keep this editor preview.");
                return;
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                pendingColumnsCount = columnsCount;
                EditorApplication.update -= TryLoadPendingColumns;
                EditorApplication.update += TryLoadPendingColumns;
                SceneManager.LoadScene("Main");
                Debug.Log(
                    $"Mobile layout test is opening Main for {columnsCount} columns.");
                return;
            }

            gameManager.LoadDevLevelWithColumns(columnsCount);
            Debug.Log($"Mobile layout test loaded {columnsCount} columns.");
        }

        private static void TryLoadPendingColumns()
        {
            if (!Application.isPlaying)
            {
                pendingColumnsCount = 0;
                EditorApplication.update -= TryLoadPendingColumns;
                return;
            }

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return;
            }

            int columnsCount = pendingColumnsCount;
            pendingColumnsCount = 0;
            EditorApplication.update -= TryLoadPendingColumns;
            gameManager.LoadDevLevelWithColumns(columnsCount);
            Debug.Log($"Mobile layout test loaded {columnsCount} columns.");
        }

        private static void WriteToolbarState(string label, RectTransform rect)
        {
            if (rect == null)
            {
                Debug.LogError($"Main menu navigation audit: {label} is missing.");
                return;
            }

            Image image = rect.GetComponent<Image>();
            Debug.Log(
                $"Main menu navigation audit: {label}; parent={rect.parent?.name}; active={rect.gameObject.activeInHierarchy}; " +
                $"anchor={rect.anchorMin}; position={rect.anchoredPosition}; size={rect.sizeDelta}; " +
                $"scale={rect.localScale}; imageEnabled={image != null && image.enabled}; " +
                $"sprite={(image != null && image.sprite != null ? image.sprite.name : "<none>")}; " +
                $"color={(image != null ? image.color.ToString() : "<none>")}.");
        }
    }
}
