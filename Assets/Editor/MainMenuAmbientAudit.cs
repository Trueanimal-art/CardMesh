using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuAmbientAudit
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string SessionKey = "CardMesh.MainMenuAmbientAudit.Running";
    private const string ScreenshotRelativePath = "Artifacts/MainMenuAmbient_GameView.png";

    private static double playStartedAt;
    private static int frameStartedAt;
    private static bool toggleChecked;
    private static bool motionChecked;
    private static bool shineObserved;
    private static bool screenshotRequested;
    private static bool screenshotCleanupRestored;
    private static MainMenuAmbientController controller;
    private static RectTransform runtimeFog;
    private static RectTransform runtimeRays;
    private static RectTransform runtimeParticle;
    private static Image runtimeShine;
    private static Vector2 initialFogPosition;
    private static Vector2 initialRaysPosition;
    private static Vector2 initialParticlePosition;
    private static GameObject hiddenDevTools;

    static MainMenuAmbientAudit()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    [MenuItem("Tools/CardMesh/Audit Main Menu Ambient Animation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Stop Play Mode before starting the ambient audit.");
        }

        AuditEditModeScene();
        SessionState.SetBool(SessionKey, true);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void AuditEditModeScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        MainMenuAmbientController ambient = UnityEngine.Object.FindFirstObjectByType<MainMenuAmbientController>(FindObjectsInactive.Include);
        if (canvas == null || ambient == null)
        {
            throw new UnityException("Ambient audit: Canvas or MainMenuAmbientController is missing.");
        }

        Transform root = ambient.transform;
        Transform background = canvas.transform.Find("Background");
        Transform playButton = canvas.transform.Find("PlayButton");
        Transform fog = root.Find("AmbientFog");
        Transform rays = root.Find("LightRays");
        Transform particles = root.Find("AmbientParticles");
        Transform shine = FindChildRecursive(playButton, "AmbientShine");

        Require(root.name == "AmbientAnimation", "ambient root name");
        Require(background != null && root.GetSiblingIndex() == background.GetSiblingIndex() + 1, "ambient root must follow Background");
        Require(playButton != null && root.GetSiblingIndex() < playButton.GetSiblingIndex(), "ambient root must render below PlayButton");
        Require(fog != null && rays != null && particles != null && shine != null, "ambient hierarchy");
        Require(particles.childCount == 15, "particle pool must contain 15 objects");
        Require(shine.parent.GetComponent<Image>() != null, "shine must be attached to the actual PlayButton Image");

        Graphic[] ambientGraphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < ambientGraphics.Length; i++)
        {
            Require(!ambientGraphics[i].raycastTarget, $"{ambientGraphics[i].name} Raycast Target");
        }

        Require(!shine.GetComponent<Graphic>().raycastTarget, "AmbientShine Raycast Target");
        Require(ambient.enabled && ambient.gameObject.activeInHierarchy, "ambient controller enabled");
        Require(scene.isLoaded, "MainMenu scene loaded");
        Debug.Log("MainMenu ambient edit-mode audit passed: hierarchy, ordering, pool, shine alignment, and raycasts are valid.");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Screen.SetResolution(1920, 1080, false);
            controller = UnityEngine.Object.FindFirstObjectByType<MainMenuAmbientController>();
            if (controller == null)
            {
                FailAndStop("MainMenuAmbientController was not found in Play Mode.");
                return;
            }

            playStartedAt = EditorApplication.timeSinceStartup;
            frameStartedAt = Time.frameCount;
            toggleChecked = false;
            motionChecked = false;
            shineObserved = false;
            screenshotRequested = false;
            screenshotCleanupRestored = false;
            runtimeFog = controller.transform.Find("AmbientFog") as RectTransform;
            runtimeRays = controller.transform.Find("LightRays") as RectTransform;
            RectTransform runtimeParticleRoot = controller.transform.Find("AmbientParticles") as RectTransform;
            runtimeParticle = runtimeParticleRoot != null && runtimeParticleRoot.childCount > 0
                ? runtimeParticleRoot.GetChild(0) as RectTransform
                : null;
            runtimeShine = FindChildRecursive(GameObject.Find("PlayButton")?.transform, "AmbientShine")?.GetComponent<Image>();
            Require(runtimeFog != null && runtimeRays != null && runtimeParticle != null && runtimeShine != null, "runtime ambient references");
            initialFogPosition = runtimeFog.anchoredPosition;
            initialRaysPosition = runtimeRays.anchoredPosition;
            initialParticlePosition = runtimeParticle.anchoredPosition;
            hiddenDevTools = null;
        }
    }

    private static void OnEditorUpdate()
    {
        if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying || controller == null)
        {
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
        shineObserved |= runtimeShine != null && runtimeShine.enabled;

        if (!motionChecked && elapsed >= 3d)
        {
            Require((runtimeFog.anchoredPosition - initialFogPosition).sqrMagnitude > 1f, "fog must move");
            Require((runtimeRays.anchoredPosition - initialRaysPosition).sqrMagnitude > 1f, "light rays must move");
            Require((runtimeParticle.anchoredPosition - initialParticlePosition).sqrMagnitude > 25f, "particles must move");
            motionChecked = true;
            Debug.Log("MainMenu ambient motion audit passed: fog, rays, and particles changed position.");
        }

        if (!toggleChecked && elapsed >= 4d)
        {
            controller.EnableAmbientAnimation = false;
            Require(!controller.EditorVisualLayersActive, "master toggle must disable all visual layers");
            controller.EnableAmbientAnimation = true;
            Require(controller.EditorVisualLayersActive, "master toggle must restore all visual layers");
            toggleChecked = true;
            Debug.Log("MainMenu ambient master toggle audit passed.");
        }

        if (!screenshotRequested && elapsed >= 19.5d)
        {
            hiddenDevTools = GameObject.Find("RuntimeDevTools");
            if (hiddenDevTools != null)
            {
                hiddenDevTools.SetActive(false);
            }

            string screenshotPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScreenshotRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath) ?? ".");
            ScreenCapture.CaptureScreenshot(screenshotPath, 1);
            screenshotRequested = true;
        }

        if (screenshotRequested && !screenshotCleanupRestored && elapsed >= 21d)
        {
            if (hiddenDevTools != null)
            {
                hiddenDevTools.SetActive(true);
            }

            screenshotCleanupRestored = true;
        }

        if (elapsed < 30d)
        {
            return;
        }

        int frames = Mathf.Max(1, Time.frameCount - frameStartedAt);
        double fps = frames / Math.Max(0.001d, elapsed);
        double bytesPerAmbientFrame = controller.EditorSampleFrames > 0
            ? (double)controller.EditorAllocatedBytes / controller.EditorSampleFrames
            : 0d;

        Require(motionChecked, "motion sampling must complete");
        Require(shineObserved, "PlayButton shine must become visible during the audit");
        Require(toggleChecked, "master toggle check must complete");
        Require(screenshotRequested && screenshotCleanupRestored, "screenshot capture must complete");
        Debug.Log(
            $"MainMenu ambient 30s audit passed: {frames} frames, {fps:F1} average FPS in Editor, "
            + $"{controller.EditorAllocatedBytes} managed bytes across {controller.EditorSampleFrames} ambient frames "
            + $"({bytesPerAmbientFrame:F2} B/frame), motion/shine/toggle passed, screenshot saved to {ScreenshotRelativePath}.");

        SessionState.SetBool(SessionKey, false);
        controller = null;
        EditorApplication.isPlaying = false;
    }

    private static void FailAndStop(string message)
    {
        SessionState.SetBool(SessionKey, false);
        controller = null;
        Debug.LogError($"MainMenu ambient audit failed: {message}");
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new UnityException($"MainMenu ambient audit failed: {message}.");
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
