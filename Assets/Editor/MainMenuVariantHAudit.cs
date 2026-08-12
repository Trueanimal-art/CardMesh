using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuVariantHAudit
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string SessionKey = "CardMesh.MainMenuVariantH.AuditRunning";
    private const string CaptureIndexKey =
        "CardMesh.MainMenuVariantH.CaptureIndex";
    private const string TransitionPendingKey =
        "CardMesh.MainMenuVariantH.TransitionPending";
    private const string EditorSplashSkipKey =
        "CardMesh.OneTapGarage.SkipInEditor";
    private const string PreviousSplashSkipKey =
        "CardMesh.MainMenuVariantH.PreviousSplashSkip";
    private const string SplashOverrideActiveKey =
        "CardMesh.MainMenuVariantH.SplashOverrideActive";
    private const string ArtifactDirectory = "Artifacts/MainMenuVariantH";

    private static readonly string[] ButtonNames =
    {
        "DailyRewardsButton",
        "ChestChaseButton",
        "PlayButton",
        "CollectionBookButton",
        "ShopButton"
    };

    private static readonly string[] ExpectedSprites =
    {
        "h4_nav_medallion_base",
        "h4_nav_medallion_base",
        "h5_play_cta_clean",
        "h4_nav_medallion_base",
        "h4_nav_medallion_base"
    };

    private static readonly string[] ExpectedGlyphs =
    {
        "icon_reward_royal_seal",
        "icon_chests_royal_seal",
        null,
        "icon_collection_royal_seal",
        "icon_shop_royal_seal"
    };

    private static readonly string[] TopButtonNames =
    {
        "LevelsButton",
        "SettingsButton"
    };

    private static readonly string[] ExpectedTopSprites =
    {
        "h4_nav_medallion_base",
        "h4_nav_medallion_base"
    };

    private static readonly string[] ExpectedTopGlyphs =
    {
        "icon_levels_royal_seal",
        "icon_settings_royal_seal"
    };

    private static readonly Vector2Int[] CaptureSizes =
    {
        new(1080, 1920),
        new(1080, 2160),
        new(1080, 2400)
    };

    private static double playStartedAt;
    private static double stageStartedAt;
    private static int captureStage;
    private static GameObject hiddenDevTools;
    private static MainMenuVariantHCaptureRunner captureRunner;
    private static string pendingCapturePath;
    private static int pendingCaptureWidth;
    private static int pendingCaptureHeight;

    static MainMenuVariantHAudit()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    [MenuItem("Tools/Card Mesh/Mobile/Audit Variant H Navigation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Stop Play Mode before auditing Variant H.");
        }

        AuditEditModeScene();
        Directory.CreateDirectory(
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ArtifactDirectory)));
        SessionState.SetBool(
            PreviousSplashSkipKey,
            EditorPrefs.GetBool(EditorSplashSkipKey, false));
        SessionState.SetBool(SplashOverrideActiveKey, true);
        EditorPrefs.SetBool(EditorSplashSkipKey, true);
        EditorPrefs.SetBool(SessionKey, true);
        EditorPrefs.SetInt(CaptureIndexKey, 0);
        EditorPrefs.SetBool(TransitionPendingKey, false);
        StartCaptureSession();
    }

    private static void AuditEditModeScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(
            FindObjectsInactive.Include);
        RectTransform safeArea = FindNamedRect(canvas?.transform, "SafeAreaRoot");
        RectTransform chromeRoot = FindNamedRect(safeArea, "MainMenuChromeRoot");
        RectTransform topActivities = FindNamedRect(chromeRoot, "TopActivities");
        RectTransform bottomMenu = FindNamedRect(chromeRoot, "BottomNavigation");
        Require(
            canvas != null
            && safeArea != null
            && chromeRoot != null
            && topActivities != null
            && bottomMenu != null,
            "main hierarchy");
        Require(
            chromeRoot.parent == safeArea,
            "MainMenuChromeRoot is authored directly under SafeAreaRoot");
        RectTransform backgroundOffset =
            canvas.transform.Find("BackgroundOffsetLayer") as RectTransform;
        RectTransform background =
            backgroundOffset?.Find("Background") as RectTransform;
        Require(
            backgroundOffset != null && background != null,
            "BackgroundOffsetLayer hierarchy");
        Require(
            Approximately(backgroundOffset.anchorMin, Vector2.zero)
            && Approximately(backgroundOffset.anchorMax, Vector2.one)
            && Approximately(backgroundOffset.sizeDelta, new Vector2(0f, 216f))
            && Approximately(backgroundOffset.anchoredPosition, new Vector2(0f, -104f)),
            $"BackgroundOffsetLayer geometry anchors "
            + $"{backgroundOffset.anchorMin}/{backgroundOffset.anchorMax}, size "
            + $"{backgroundOffset.sizeDelta}, position {backgroundOffset.anchoredPosition}");
        AspectRatioFitter backgroundFitter =
            background.GetComponent<AspectRatioFitter>();
        Require(
            backgroundFitter != null
            && backgroundFitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent
            && Mathf.Abs(backgroundFitter.aspectRatio - 0.45f) < 0.0001f,
            $"Background AspectRatioFitter mode {backgroundFitter?.aspectMode}, "
            + $"aspect {backgroundFitter?.aspectRatio}");
        Require(canvas.transform.Find("BottomRailVisual") == null, "legacy BottomRailVisual removed");
        Require(
            Approximately(topActivities.sizeDelta, new Vector2(1080f, 250f)),
            $"TopActivities size {topActivities.sizeDelta}");
        AuditTopUtilityMedallions(topActivities);
        Require(
            Approximately(bottomMenu.sizeDelta, new Vector2(0f, 490f)),
            $"BottomNavigation size {bottomMenu.sizeDelta}");
        Require(
            bottomMenu.GetComponentInChildren<LayoutGroup>(true) == null,
            "no runtime-sensitive LayoutGroup in BottomNavigation");
        Require(
            bottomMenu.GetComponentInChildren<ContentSizeFitter>(true) == null,
            "no runtime-sensitive ContentSizeFitter in BottomNavigation");

        List<Rect> hitRects = new();
        for (int index = 0; index < ButtonNames.Length; index++)
        {
            RectTransform root = FindNamedRect(bottomMenu, ButtonNames[index]);
            Require(root != null, $"{ButtonNames[index]} exists under BottomNavigation");

            bool primary = ButtonNames[index] == "PlayButton";
            Vector2 expectedSize = primary
                ? new Vector2(780f, 300f)
                : new Vector2(236f, 238f);
            Vector2 expectedPosition = primary
                ? new Vector2(0f, 340f)
                : new Vector2(0f, 12f);
            Require(
                Approximately(root.sizeDelta, expectedSize),
                $"{root.name} size {root.sizeDelta}, expected {expectedSize}");
            Require(
                Approximately(root.anchoredPosition, expectedPosition),
                $"{root.name} position {root.anchoredPosition}, expected {expectedPosition}");

            Button button = root.GetComponent<Button>()
                ?? root.GetComponentInChildren<Button>(true);
            Require(button != null && button.interactable, $"{root.name} Button");
            Image target = button.targetGraphic as Image;
            Require(
                target != null && target.raycastTarget && target.sprite == null,
                $"{root.name} transparent hit target");

            Image visual = FindNamedRect(root, "Visual")?.GetComponent<Image>();
            Require(visual != null, $"{root.name} Visual");
            Require(!visual.raycastTarget, $"{root.name} Visual raycast disabled");
            Require(
                visual.sprite != null && visual.sprite.name == ExpectedSprites[index],
                $"{root.name} sprite {visual.sprite?.name}");

            RectTransform glyphRect = FindNamedRect(root, "Glyph");
            if (primary)
            {
                Require(
                    glyphRect == null || !glyphRect.gameObject.activeSelf,
                    $"{root.name} has no separate glyph");
            }
            else
            {
                Image glyph = glyphRect?.GetComponent<Image>();
                Require(
                    glyphRect != null
                    && glyphRect.gameObject.activeSelf
                    && glyph != null
                    && glyph.sprite != null
                    && glyph.sprite.name == ExpectedGlyphs[index],
                    $"{root.name} glyph {glyph?.sprite?.name}");

                Vector2 expectedGlyphSize = root.name == "CollectionBookButton"
                    ? new Vector2(158.08f, 158.08f)
                    : new Vector2(152f, 152f);
                Vector2 expectedGlyphPosition = root.name == "CollectionBookButton"
                    ? new Vector2(0f, -21.96f)
                    : new Vector2(0f, -25f);
                Require(
                    Approximately(glyphRect.sizeDelta, expectedGlyphSize)
                    && Approximately(glyphRect.anchoredPosition, expectedGlyphPosition),
                    $"{root.name} glyph geometry size {glyphRect.sizeDelta}, "
                    + $"position {glyphRect.anchoredPosition}");
            }

            if (root.name == "DailyRewardsButton")
            {
                RectTransform rewardBadge = FindNamedRect(root, "NotificationDot");
                Require(
                    rewardBadge != null
                    && Approximately(rewardBadge.anchorMin, new Vector2(0.5f, 1f))
                    && Approximately(rewardBadge.anchorMax, new Vector2(0.5f, 1f))
                    && Approximately(rewardBadge.sizeDelta, new Vector2(52f, 52f))
                    && Approximately(rewardBadge.anchoredPosition, new Vector2(72f, -30f)),
                    $"Daily reward rim badge geometry anchors "
                    + $"{rewardBadge?.anchorMin}/{rewardBadge?.anchorMax}, size "
                    + $"{rewardBadge?.sizeDelta}, position {rewardBadge?.anchoredPosition}");
            }

            TMP_Text label = FindNamedRect(root, "Label")?.GetComponent<TMP_Text>();
            Require(label != null, $"{root.name} Label");
            Require(!label.enableAutoSizing, $"{root.name} fixed typography");
            Require(
                label.preferredWidth <= label.rectTransform.rect.width + 0.5f,
                $"{root.name} label width {label.preferredWidth:F1}/"
                + $"{label.rectTransform.rect.width:F1}");
            Require(!label.raycastTarget, $"{root.name} Label raycast disabled");
            if (primary)
            {
                Require(
                    label.fontStyle.HasFlag(FontStyles.Bold)
                    && label.font != null
                    && label.font.name.Contains("Prata")
                    && label.fontSharedMaterial != null
                    && label.fontSharedMaterial.name.Contains("Prata Cyrillic CTA"),
                    $"{root.name} uses the premium Prata CTA TMP preset");
                Require(
                    Approximately(
                        label.rectTransform.anchoredPosition,
                        new Vector2(0f, 5f)),
                    $"{root.name} optically centered label position "
                    + $"{label.rectTransform.anchoredPosition}");
            }

            hitRects.Add(CalculateRectRelativeTo(bottomMenu, root));
        }

        for (int left = 0; left < hitRects.Count; left++)
        {
            for (int right = left + 1; right < hitRects.Count; right++)
            {
                Require(
                    !hitRects[left].Overlaps(hitRects[right]),
                    $"{ButtonNames[left]} and {ButtonNames[right]} hit rectangles overlap");
            }
        }

        try
        {
            CardMesh.MainMenuNavigationVisibility.SetBottomNavigationVisible(
                canvas,
                false);
            Require(
                !chromeRoot.gameObject.activeSelf,
                "modal visibility hides the complete MainMenuChromeRoot");
        }
        finally
        {
            CardMesh.MainMenuNavigationVisibility.SetBottomNavigationVisible(
                canvas,
                true);
        }
        Require(
            chromeRoot.gameObject.activeSelf,
            "modal visibility restores the complete MainMenuChromeRoot");

        Require(scene.isLoaded, "MainMenu scene loaded");
        Debug.Log(
            "Variant H6 edit-mode audit passed: Levels and Settings are top utility medallions; "
            + "Reward, Chests, Collection and Shop are bottom actions; the bold TMP Play label "
            + "is optically centered; the complete chrome is scene-authored with independent "
            + "hit targets, live labels, raycasts, no rail, and no layout builders.");
    }

    private static void AuditTopUtilityMedallions(RectTransform topActivities)
    {
        for (int index = 0; index < TopButtonNames.Length; index++)
        {
            RectTransform root = FindNamedRect(topActivities, TopButtonNames[index]);
            Require(root != null, $"{TopButtonNames[index]} exists under TopActivities");
            Require(
                Approximately(root.sizeDelta, new Vector2(236f, 238f)),
                $"{root.name} size {root.sizeDelta}");
            Require(
                Approximately(root.anchoredPosition, new Vector2(0f, 12f)),
                $"{root.name} position {root.anchoredPosition}");

            Button button = root.GetComponent<Button>()
                ?? root.GetComponentInChildren<Button>(true);
            Require(button != null && button.interactable, $"{root.name} Button");
            Image surface = button.targetGraphic as Image;
            Require(
                surface != null
                && surface.sprite == null
                && surface.raycastTarget,
                $"{root.name} transparent utility hit target");

            Image visual = FindNamedRect(root, "Visual")?.GetComponent<Image>();
            Require(
                visual != null
                && visual.sprite != null
                && visual.sprite.name == ExpectedTopSprites[index]
                && !visual.raycastTarget,
                $"{root.name} utility medallion {visual?.sprite?.name}");

            Image glyph = FindNamedRect(root, "Glyph")?.GetComponent<Image>();
            Require(
                glyph != null
                && glyph.sprite != null
                && glyph.sprite.name == ExpectedTopGlyphs[index]
                && !glyph.raycastTarget,
                $"{root.name} utility glyph {glyph?.sprite?.name}");
            RectTransform glyphRect = glyph?.rectTransform;
            Vector2 expectedGlyphSize = root.name == "SettingsButton"
                ? new Vector2(147.44f, 147.44f)
                : new Vector2(152f, 152f);
            Vector2 expectedGlyphPosition = root.name == "SettingsButton"
                ? new Vector2(0f, -27.28f)
                : new Vector2(0f, -25f);
            Require(
                glyphRect != null
                && Approximately(glyphRect.sizeDelta, expectedGlyphSize)
                && Approximately(glyphRect.anchoredPosition, expectedGlyphPosition),
                $"{root.name} utility glyph geometry size {glyphRect?.sizeDelta}, "
                + $"position {glyphRect?.anchoredPosition}");

            TMP_Text label = FindNamedRect(root, "Label")?.GetComponent<TMP_Text>();
            Require(label != null, $"{root.name} live utility label");
            Require(
                label.preferredWidth <= label.rectTransform.rect.width + 0.5f,
                $"{root.name} label fits");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (!EditorPrefs.GetBool(SessionKey, false))
            {
                RestoreEditorSplashPreference();
                return;
            }

            if (EditorPrefs.GetBool(TransitionPendingKey, false))
            {
                EditorPrefs.SetBool(TransitionPendingKey, false);
                EditorApplication.delayCall += StartCaptureSession;
            }
            else
            {
                EditorPrefs.SetBool(SessionKey, false);
                RestoreEditorSplashPreference();
            }
        }

        if (!EditorPrefs.GetBool(SessionKey, false)
            || state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        BeginPlayAudit();
    }

    private static void OnEditorUpdate()
    {
        if (!EditorPrefs.GetBool(SessionKey, false) || !EditorApplication.isPlaying)
        {
            return;
        }

        // Domain reload can complete after EnteredPlayMode was dispatched. Keep the
        // audit self-starting so a test run can never remain stuck in Play Mode.
        if (playStartedAt <= 0d)
        {
            BeginPlayAudit();
        }

        int captureIndex = EditorPrefs.GetInt(CaptureIndexKey, 0);
        if (captureIndex < 0 || captureIndex >= CaptureSizes.Length)
        {
            throw new InvalidOperationException(
                $"Variant H audit has an invalid capture index {captureIndex}.");
        }

        Vector2Int captureSize = CaptureSizes[captureIndex];
        double stageElapsed = EditorApplication.timeSinceStartup - stageStartedAt;
        if (captureStage == 0
            && stageElapsed >= 2d
            && IsMainMenuReady()
            && IsResolutionReady(captureSize.x, captureSize.y))
        {
            HideDevTools();
            BeginCapture(
                $"VariantH_{captureSize.x}x{captureSize.y}.png",
                captureSize.x,
                captureSize.y);
            captureStage = 1;
            stageStartedAt = EditorApplication.timeSinceStartup;
        }
        else if (captureStage == 1 && IsPendingCaptureValid())
        {
            int nextCaptureIndex = captureIndex + 1;
            EditorPrefs.SetInt(CaptureIndexKey, nextCaptureIndex);
            if (nextCaptureIndex < CaptureSizes.Length)
            {
                EditorPrefs.SetBool(TransitionPendingKey, true);
                EditorApplication.isPlaying = false;
                return;
            }

            EditorPrefs.SetBool(SessionKey, false);
            RestoreEditorSplashPreference();
            EditorApplication.isPlaying = false;
            Debug.Log(
                "Variant H play-mode audit captured 1080x1920, 1080x2160 and 1080x2400.");
        }
    }

    private static void BeginPlayAudit()
    {
        playStartedAt = EditorApplication.timeSinceStartup;
        stageStartedAt = playStartedAt;
        captureStage = 0;
        HideDevTools();
        pendingCapturePath = null;
        EnsureCaptureRunner();
    }

    private static void StartCaptureSession()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        int captureIndex = EditorPrefs.GetInt(CaptureIndexKey, 0);
        Require(
            captureIndex >= 0 && captureIndex < CaptureSizes.Length,
            $"capture index {captureIndex}");
        Vector2Int captureSize = CaptureSizes[captureIndex];
        ApplyResolution(captureSize.x, captureSize.y);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static bool IsMainMenuReady()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            return false;
        }

        RectTransform[] rects = UnityEngine.Object.FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (RectTransform rect in rects)
        {
            if (rect != null
                && rect.name == "MainMenuChromeRoot"
                && rect.gameObject.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private static void HideDevTools()
    {
        hiddenDevTools = GameObject.Find("RuntimeDevTools");
        if (hiddenDevTools != null)
        {
            hiddenDevTools.SetActive(false);
        }
    }

    private static void RestoreEditorSplashPreference()
    {
        if (!SessionState.GetBool(SplashOverrideActiveKey, false))
        {
            return;
        }

        EditorPrefs.SetBool(
            EditorSplashSkipKey,
            SessionState.GetBool(PreviousSplashSkipKey, false));
        SessionState.SetBool(SplashOverrideActiveKey, false);
    }

    private static void ApplyResolution(int width, int height)
    {
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
        MethodInfo setCustomResolution = gameViewType.GetMethod(
            "SetCustomResolution",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (setCustomResolution == null)
        {
            throw new MissingMethodException(
                "UnityEditor.GameView.SetCustomResolution was not found.");
        }

        setCustomResolution.Invoke(
            gameView,
            new object[]
            {
                new Vector2(width, height),
                $"CardMesh Audit {width}x{height}"
            });
        Screen.SetResolution(width, height, false);
        gameView.Repaint();
        Canvas.ForceUpdateCanvases();
        Debug.Log(
            $"Variant H audit applied Game View {width}x{height}; "
            + $"Screen reports {Screen.width}x{Screen.height}.");
    }

    private static bool IsResolutionReady(int width, int height)
    {
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = EditorWindow.GetWindow(gameViewType, false, "Game", false);
        PropertyInfo targetRenderSizeProperty = gameViewType.GetProperty(
            "targetRenderSize",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Vector2 targetRenderSize = targetRenderSizeProperty != null
            ? (Vector2)targetRenderSizeProperty.GetValue(gameView)
            : new Vector2(Screen.width, Screen.height);
        if (Mathf.RoundToInt(targetRenderSize.x) != width
            || Mathf.RoundToInt(targetRenderSize.y) != height)
        {
            return false;
        }

        Canvas.ForceUpdateCanvases();
        return true;
    }

    private static void EnsureCaptureRunner()
    {
        if (captureRunner != null)
        {
            return;
        }

        GameObject runnerObject = new("VariantH Capture Runner");
        runnerObject.hideFlags = HideFlags.HideAndDontSave;
        captureRunner = runnerObject.AddComponent<MainMenuVariantHCaptureRunner>();
    }

    private static void BeginCapture(string fileName, int width, int height)
    {
        string path = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", ArtifactDirectory, fileName));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        pendingCapturePath = path;
        pendingCaptureWidth = width;
        pendingCaptureHeight = height;
        Canvas.ForceUpdateCanvases();
        EnsureCaptureRunner();
        captureRunner.Capture(path, width, height);
    }

    private static bool IsPendingCaptureValid()
    {
        if (string.IsNullOrEmpty(pendingCapturePath)
            || !File.Exists(pendingCapturePath))
        {
            return false;
        }

        using FileStream stream = File.OpenRead(pendingCapturePath);
        if (stream.Length < 24)
        {
            return false;
        }

        byte[] header = new byte[24];
        int bytesRead = stream.Read(header, 0, header.Length);
        if (bytesRead != header.Length)
        {
            return false;
        }

        int width =
            (header[16] << 24)
            | (header[17] << 16)
            | (header[18] << 8)
            | header[19];
        int height =
            (header[20] << 24)
            | (header[21] << 16)
            | (header[22] << 8)
            | header[23];
        Require(
            width == pendingCaptureWidth && height == pendingCaptureHeight,
            $"capture dimensions {width}x{height}, expected "
            + $"{pendingCaptureWidth}x{pendingCaptureHeight}");
        Debug.Log($"Variant H audit verified PNG {width}x{height}.");
        pendingCapturePath = null;
        return true;
    }

    private static RectTransform FindNamedRect(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root as RectTransform;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            RectTransform match = FindNamedRect(root.GetChild(index), name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Abs(left.x - right.x) < 0.1f
            && Mathf.Abs(left.y - right.y) < 0.1f;
    }

    private static Rect CalculateRectRelativeTo(RectTransform parent, RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 bottomLeft = parent.InverseTransformPoint(corners[0]);
        Vector3 topRight = parent.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x,
            topRight.y);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Variant H audit failed: {message}.");
        }
    }
}

public sealed class MainMenuVariantHCaptureRunner : MonoBehaviour
{
    public void Capture(string path, int width, int height)
    {
        StartCoroutine(CaptureAtEndOfFrame(path, width, height));
    }

    private static IEnumerator CaptureAtEndOfFrame(
        string path,
        int width,
        int height)
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        Texture2D texture = new(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
        texture.Apply(false, false);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Destroy(texture);
        Debug.Log($"Variant H audit wrote PNG {width}x{height}: {path}");
    }
}
