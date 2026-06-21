using SolitaireAssociations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations.Editor
{
    public static class MainSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string LevelAssetPath = "Assets/Resources/Levels/Level_1.asset";

        [MenuItem("Tools/Solitaire Associations/Build Main Scene")]
        public static void BuildMainScene()
        {
            BuildSceneInternal();
        }

        public static void BuildMainSceneFromBatchmode()
        {
            BuildSceneInternal();
        }

        private static void BuildSceneInternal()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateCanvas();
            CreateEventSystem();
            CreateGameRoot();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";

            Camera cameraComponent = cameraObject.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 5f;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.08f, 0.18f, 0.14f, 1f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateCanvas()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rectTransform = (RectTransform)canvas.transform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void CreateGameRoot()
        {
            GameObject gameRoot = new("GameRoot");
            gameRoot.AddComponent<StubPlatformService>();
            GameManager gameManager = gameRoot.AddComponent<GameManager>();
            new GameObject("AdsManager").AddComponent<AdsManager>();

            LevelDefinition levelDefinition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelAssetPath);
            SerializedObject serializedObject = new(gameManager);
            serializedObject.FindProperty("levelDefinition").objectReferenceValue = levelDefinition;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
