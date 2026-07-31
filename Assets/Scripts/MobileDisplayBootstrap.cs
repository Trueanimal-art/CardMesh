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
        private readonly Dictionary<int, Rect> lastSafeAreas = new();
        private int lastWidth;
        private int lastHeight;

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
            if (Screen.width == lastWidth && Screen.height == lastHeight)
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

                if (scene.name == "Main")
                {
                    ApplyGameplayLayout(scene);
                }
                else if (scene.name == "MainMenu")
                {
                    ApplyMainMenuLayout(scene);
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

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = PortraitReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                RectTransform safeRoot = FindNamedRect(canvas.transform, "SafeAreaRoot")
                    ?? FindNamedRect(canvas.transform, "SafeArea");
                if (safeRoot != null)
                {
                    ApplySafeArea(safeRoot, canvas);
                }

                FitLargeWindows(canvas.transform as RectTransform);
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

        private static void ApplyGameplayLayout(Scene scene)
        {
            CardMeshSceneRefs sceneRefs = FindInScene<CardMeshSceneRefs>(scene);
            if (sceneRefs == null || sceneRefs.SafeAreaRoot == null)
            {
                return;
            }

            RectTransform root = sceneRefs.SafeAreaRoot;
            SetRect(FindNamedRect(root, "TopHUD"), 0f, 1f, 1f, 1f, 0f, 190f, 0f, -20f);
            SetRect(FindNamedRect(root, "MovesPanel"), 0f, 1f, 0f, 1f, 190f, 170f, 125f, -285f);
            SetRect(FindNamedRect(root, "DeckArea"), 1f, 1f, 1f, 1f, 330f, 300f, -175f, -330f);
            SetRect(FindNamedRect(root, "BoardArea"), 0f, 0f, 1f, 1f, -80f, -760f, 0f, -70f);
            SetRect(FindNamedRect(root, "BottomHUD"), 0f, 0f, 1f, 0f, 0f, 250f, 0f, 42f);

            RectTransform ad = FindNamedRect(root, "AdBannerPlaceholder");
            if (ad != null)
            {
                ad.gameObject.SetActive(false);
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
                }
            }

            foreach (ColumnRef column in sceneRefs.Columns)
            {
                if (column?.Root is not RectTransform columnRect)
                {
                    continue;
                }

                LayoutElement element = columnRect.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredHeight = 1040f;
                    element.minHeight = 1040f;
                }

                if (column.CardsStackRoot is RectTransform stackRect)
                {
                    stackRect.sizeDelta = new Vector2(stackRect.sizeDelta.x, 820f);
                }
            }

            ArrangeBottomButton(root, "HintButton", -330f);
            ArrangeBottomButton(root, "UndoButton", 0f);
            ArrangeBottomButton(root, "BoosterDeckButton", 330f);
        }

        private static void ApplyMainMenuLayout(Scene scene)
        {
            Canvas canvas = FindInScene<Canvas>(scene);
            if (canvas == null)
            {
                return;
            }

            RectTransform root = canvas.transform as RectTransform;
            RectTransform background = FindNamedRect(root, "Background");
            if (background != null)
            {
                Stretch(background);
                Image image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.preserveAspect = false;
                }
            }

            SetRect(FindNamedRect(root, "PlayButton"), 0.5f, 0f, 0.5f, 0f, 500f, 260f, 0f, 430f);
            SetRect(FindNamedRect(root, "LevelsButton"), 0.5f, 0f, 0.5f, 0f, 330f, 220f, -205f, 180f);
            SetRect(FindNamedRect(root, "SettingsButton"), 0.5f, 0f, 0.5f, 0f, 330f, 220f, 205f, 180f);
            SetRect(FindNamedRect(root, "ChestChaseButton"), 0f, 1f, 0f, 1f, 144f, 144f, 105f, -115f);
            SetRect(FindNamedRect(root, "DailyRewardsButton"), 0.5f, 1f, 0.5f, 1f, 144f, 144f, 0f, -115f);
            SetRect(FindNamedRect(root, "ShopButton"), 1f, 1f, 1f, 1f, 144f, 144f, -105f, -115f);
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

        private static void ArrangeBottomButton(Transform root, string name, float x)
        {
            SetRect(FindNamedRect(root, name), 0.5f, 0.5f, 0.5f, 0.5f, 280f, 108f, x, 0f);
        }

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
