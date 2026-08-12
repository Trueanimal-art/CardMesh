using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh
{
    [DefaultExecutionOrder(-1000)]
    public sealed class MobileDisplayBootstrap : MonoBehaviour
    {
        private static readonly Vector2 PortraitReferenceResolution = new(1080f, 1920f);
        private const float GameplayVerticalOffset = 480f;
        private const float DeckLiftAboveTargets = 225f;
        private readonly Dictionary<int, Rect> lastSafeAreas = new();
        private int lastWidth;
        private int lastHeight;
        private Rect lastScreenSafeArea = new(-1f, -1f, -1f, -1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<MobileDisplayBootstrap>() != null)
            {
                return;
            }

            GameObject root = new(nameof(MobileDisplayBootstrap));
            DontDestroyOnLoad(root);
            root.AddComponent<MobileDisplayBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ApplyPortraitOrientation();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            StartCoroutine(ApplyAfterLayout());
        }

        private void Update()
        {
            Rect currentSafeArea = Screen.safeArea;
            if (Screen.width == lastWidth
                && Screen.height == lastHeight
                && currentSafeArea == lastScreenSafeArea)
            {
                return;
            }

            ApplyToLoadedScenes();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(ApplyAfterLayout());
        }

        private IEnumerator ApplyAfterLayout()
        {
            yield return null;
            ApplyToLoadedScenes();
            yield return null;
            ApplyToLoadedScenes();
        }

        private void ApplyToLoadedScenes()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastScreenSafeArea = Screen.safeArea;
            ApplyPortraitOrientation();
            ConfigureEventSystems();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    ConfigureCanvases(sceneRoot);
                    ConfigureCameraBackgrounds(sceneRoot);
                }

            }
        }

        private static void ApplyPortraitOrientation()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
        }

        private void ConfigureCanvases(GameObject root)
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    continue;
                }

                RectTransform safeRoot = FindNamedRect(canvas.transform, "SafeAreaRoot")
                    ?? FindNamedRect(canvas.transform, "SafeArea");
                if (safeRoot != null)
                {
                    ApplySafeArea(safeRoot, canvas);
                }
            }
        }

        private static void ConfigureCameraBackgrounds(GameObject root)
        {
            Color safeAreaColor = new(0.006f, 0.055f, 0.027f, 1f);
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                camera.backgroundColor = safeAreaColor;
            }
        }

        private void ApplySafeArea(RectTransform safeRoot, Canvas canvas)
        {
            if (safeRoot == null || canvas == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            int canvasId = canvas.GetInstanceID();
            Rect safe = Screen.safeArea;
            if (lastSafeAreas.TryGetValue(canvasId, out Rect previous)
                && previous == safe
                && safeRoot.anchorMin != Vector2.zero)
            {
                return;
            }

            lastSafeAreas[canvasId] = safe;
            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            safeRoot.anchorMin = anchorMin;
            safeRoot.anchorMax = anchorMax;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private static void ConfigureEventSystems()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            int threshold = Mathf.Clamp(Mathf.RoundToInt(14f * dpi / 160f), 14, 48);
            foreach (EventSystem eventSystem in FindObjectsByType<EventSystem>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                eventSystem.pixelDragThreshold = threshold;
            }
        }

#if UNITY_EDITOR
        // These methods are authoring commands. Runtime only applies orientation and safe areas;
        // all stable mobile geometry is saved directly into the Unity scenes.
        public static void ApplyGameplayLayoutToScene(Scene scene)
        {
            CardMeshSceneRefs sceneRefs = FindInScene<CardMeshSceneRefs>(scene);
            if (sceneRefs == null || sceneRefs.SafeAreaRoot == null)
            {
                return;
            }

            RectTransform root = sceneRefs.SafeAreaRoot;
            Canvas canvas = root.GetComponentInParent<Canvas>();
            RectTransform canvasRoot = canvas != null
                ? canvas.transform as RectTransform
                : null;
            RectTransform background = FindNamedRect(canvasRoot, "Background");
            if (canvas != null && background != null)
            {
                background.SetParent(canvas.transform, false);
                Stretch(background);
                background.SetAsFirstSibling();
                ConfigureFullBleedBackground(background);
            }

            SetRect(FindNamedRect(root, "TopHUD"), 0f, 1f, 1f, 1f, 0f, 224f, 0f, 0f);
            RectTransform livesHud = FindNamedRect(root, "LivesHud");
            RectTransform coinsPanel = FindNamedRect(root, "CoinsPanel");
            RectTransform movesPanel = FindNamedRect(root, "MovesPanel");
            RectTransform statusPlates = FindNamedRect(root, "GameplayStatusPlates");
            if (statusPlates != null)
            {
                SetRect(statusPlates, 0.5f, 1f, 0.5f, 1f, 960f, 96f, 35f, -162f);
                livesHud?.SetParent(statusPlates, false);
                coinsPanel?.SetParent(statusPlates, false);
                movesPanel?.SetParent(statusPlates, false);
                SetRect(livesHud, 0.5f, 0.5f, 0.5f, 0.5f, 280f, 96f, -330f, 0f);
                SetRect(coinsPanel, 0.5f, 0.5f, 0.5f, 0.5f, 190f, 96f, 0f, 0f);
                SetRect(movesPanel, 0.5f, 0.5f, 0.5f, 0.5f, 240f, 96f, 280f, 0f);
            }
            else
            {
                SetRect(livesHud, 0f, 1f, 0f, 1f, 238f, 66f, 125f, -220f);
                SetRect(coinsPanel, 0f, 1f, 0f, 1f, 238f, 60f, 365f, -220f);
                SetRect(movesPanel, 0f, 1f, 0f, 1f, 238f, 60f, 605f, -220f);
            }
            SetRect(
                FindNamedRect(root, "DeckArea"),
                1f,
                1f,
                1f,
                1f,
                520f,
                310f,
                -283f,
                -220f - GameplayVerticalOffset + DeckLiftAboveTargets);
            SetRect(
                FindNamedRect(root, "BoardArea"),
                0f,
                0f,
                1f,
                1f,
                -80f,
                -980f,
                0f,
                40f - GameplayVerticalOffset);
            SetBottomStretch(FindNamedRect(root, "BottomHUD"), 224f, 208f);

            RectTransform ad = FindNamedRect(root, "AdBannerPlaceholder");
            if (ad != null)
            {
                ad.gameObject.SetActive(true);
                SetBottomStretch(ad, 240f, 0f);
                Image adImage = ad.GetComponent<Image>();
                if (adImage != null)
                {
                    adImage.color = new Color(0f, 0f, 0f, 0f);
                    adImage.raycastTarget = false;
                }
            }

            RectTransform columnsRoot = sceneRefs.ColumnSlotsRoot;
            if (columnsRoot != null)
            {
                Stretch(columnsRoot);
                HorizontalLayoutGroup group = columnsRoot.GetComponent<HorizontalLayoutGroup>();
                if (group != null)
                {
                    group.padding = new RectOffset(12, 12, 0, 0);
                    group.childAlignment = TextAnchor.UpperCenter;
                    group.childControlWidth = true;
                    group.childForceExpandWidth = false;
                }
            }

            sceneRefs.SetEditorPreviewColumns(sceneRefs.EditorPreviewColumns);

            ArrangeBottomButton(root, "HintButton", -250f, 0f);
            ArrangeBottomButton(root, "UndoButton", 0f, 14f);
            ArrangeBottomButton(root, "BoosterDeckButton", 250f, 0f);
        }

        public static void ApplyMainMenuLayoutToScene(Scene scene)
        {
            Debug.LogWarning(
                "MobileDisplayBootstrap.ApplyMainMenuLayoutToScene is intentionally disabled. "
                + "MainMenu geometry is authored and saved by "
                + "MainMenuMobileNavigationInstaller.RebuildMainMenuNavigation.");
        }

        private static void FitLargeWindows(RectTransform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            foreach (RectTransform rect in canvasRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == canvasRoot
                    || (rect.name != "WindowRoot" && rect.name != "Panel")
                    || rect.rect.width < 820f
                    || rect.rect.height < 650f)
                {
                    continue;
                }

                float scale = Mathf.Min(
                    1f,
                    (PortraitReferenceResolution.x - 72f) / Mathf.Max(1f, rect.rect.width),
                    (PortraitReferenceResolution.y - 160f) / Mathf.Max(1f, rect.rect.height));
                rect.localScale = Vector3.one * scale;
            }
        }

        private static void ArrangeBottomButton(Transform root, string name, float x, float y)
        {
            SetRect(FindNamedRect(root, name), 0.5f, 0.5f, 0.5f, 0.5f, 224f, 224f, x, y);
        }

        private static RectTransform EnsureMainMenuSafeAreaRoot(
            RectTransform canvasRoot,
            RectTransform background)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            RectTransform safeRoot = null;
            for (int index = 0; index < canvasRoot.childCount; index++)
            {
                if (canvasRoot.GetChild(index) is RectTransform child
                    && child.name == "SafeAreaRoot")
                {
                    safeRoot = child;
                    break;
                }
            }

            if (safeRoot == null)
            {
                GameObject safeObject = new("SafeAreaRoot", typeof(RectTransform));
                safeRoot = safeObject.GetComponent<RectTransform>();
                safeRoot.SetParent(canvasRoot, false);
            }

            Stretch(safeRoot);

            List<RectTransform> content = new();
            for (int index = 0; index < canvasRoot.childCount; index++)
            {
                if (canvasRoot.GetChild(index) is RectTransform child
                    && child != background
                    && child != safeRoot)
                {
                    if (child.name == "BottomRailVisual")
                    {
                        continue;
                    }

                    content.Add(child);
                }
            }

            foreach (RectTransform child in content)
            {
                child.SetParent(safeRoot, false);
            }

            safeRoot.SetAsLastSibling();
            return safeRoot;
        }

        private static void ConfigureFullBleedBackground(RectTransform background)
        {
            if (background == null)
            {
                return;
            }

            Image image = background.GetComponent<Image>();
            if (image != null)
            {
                image.preserveAspect = false;
            }

            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            }

            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (image != null && image.sprite != null)
            {
                float aspectRatio = image.sprite.rect.height > 0f
                    ? image.sprite.rect.width / image.sprite.rect.height
                    : 1f;
                string assetPath =
                    UnityEditor.AssetDatabase.GetAssetPath(image.sprite);
                UnityEditor.TextureImporter importer =
                    UnityEditor.AssetImporter.GetAtPath(assetPath)
                    as UnityEditor.TextureImporter;
                if (importer != null)
                {
                    importer.GetSourceTextureWidthAndHeight(
                        out int sourceWidth,
                        out int sourceHeight);
                    if (sourceHeight > 0)
                    {
                        aspectRatio = (float)sourceWidth / sourceHeight;
                    }
                }

                fitter.aspectRatio = aspectRatio;
            }
        }
#endif

        private static void SetRect(
            RectTransform rect,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float width,
            float height,
            float x,
            float y)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetBottomStretch(
            RectTransform rect,
            float height,
            float bottomInset)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, bottomInset);
            rect.localScale = Vector3.one;
        }

        private static RectTransform FindNamedRect(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName)
                {
                    return rect;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T match = root.GetComponentInChildren<T>(true);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
