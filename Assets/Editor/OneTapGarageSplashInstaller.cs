using System;
using System.IO;
using CardMesh.OneTapGarage;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class OneTapGarageSplashInstaller
{
    public const string ScenePath = "Assets/Scenes/OneTapGarageSplash.unity";
    public const string MainMenuPath = "Assets/MainMenu.unity";
    public const string VideoPath =
        "Assets/Video/OneTapGarageSplash/ONE_TAP_GARAGE_Final_Logo_Master_Silent.mp4";
    public const string AudioPath =
        "Assets/Audio/OneTapGarageSplash/ONE_TAP_GARAGE_Final_Splash.wav";
    public const string BackgroundPath =
        "Assets/Art/OneTapGarageSplash/Final_Background_Mobile_v2.png";
    public const string OneTapPath =
        "Assets/Art/OneTapGarageSplash/Final_OneTap_2048.png";
    public const string GaragePath =
        "Assets/Art/OneTapGarageSplash/Final_Garage_1024.png";
    public const string TapPointPath =
        "Assets/Art/OneTapGarageSplash/Final_TapPoint_512.png";
    public const string FullLogoPath =
        "Assets/Art/OneTapGarageSplash/Final_FullLogo_2048.png";
    public const string RenderTexturePath =
        "Assets/Art/OneTapGarageSplash/OneTapGarageSplash.renderTexture";

    private const string EditorSkipKey = "CardMesh.OneTapGarage.SkipInEditor";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string AudioMutedKey = "AudioMuted";
    private const string AndroidAuditBuildPath =
        "Artifacts/OneTapGarageSplash/UnityIntegration/Android/CardMesh-OneTapGarageSplash.apk";

    [InitializeOnLoadMethod]
    private static void EnsureSplashStartsEditorPlayMode()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SceneAsset splashScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (splashScene != null
                && EditorSceneManager.playModeStartScene != splashScene)
            {
                EditorSceneManager.playModeStartScene = splashScene;
            }
        };
    }

    [MenuItem("Tools/ONE TAP GARAGE/Rebuild Approved Splash Scene")]
    public static void Rebuild()
    {
        ValidateApprovedFiles();
        ConfigureTexture(BackgroundPath, false);
        ConfigureTexture(OneTapPath, true);
        ConfigureTexture(GaragePath, true);
        ConfigureTexture(TapPointPath, true);
        ConfigureTexture(FullLogoPath, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        RenderTexture targetTexture = CreateOrReplaceRenderTexture();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "OneTapGarageSplash";

        GameObject root = new("OneTapGarageSplash");
        OneTapGarageSplashController controller =
            root.AddComponent<OneTapGarageSplashController>();

        AudioListener splashAudioListener = CreateCamera(root.transform);
        EventSystem splashEventSystem = CreateEventSystem(root.transform);

        GameObject canvasObject = new(
            "CanvasGroup",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        scaler.referencePixelsPerUnit = 100f;

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        GameObject staticFallback = CreateRectObject("StaticFallback", canvasObject.transform);
        Stretch(staticFallback.GetComponent<RectTransform>());

        Image background = CreateImage(
            "Background",
            staticFallback.transform,
            LoadSprite(BackgroundPath),
            Vector2.zero,
            new Vector2(1080f, 1920f));
        Stretch(background.rectTransform);
        background.preserveAspect = false;

        GameObject safeArea = CreateRectObject("SafeArea", staticFallback.transform);
        Stretch(safeArea.GetComponent<RectTransform>());

        GameObject fallbackLogo = CreateRectObject("FallbackLogo", safeArea.transform);
        Stretch(fallbackLogo.GetComponent<RectTransform>());

        Image ambientGlow = CreateImage(
            "AmbientGlow",
            fallbackLogo.transform,
            LoadSprite(TapPointPath),
            new Vector2(214f, 132f),
            new Vector2(300f, 300f));
        ambientGlow.color = new Color(0.32f, 0.82f, 0.62f, 0.08f);
        ambientGlow.preserveAspect = true;
        ambientGlow.raycastTarget = false;
        CanvasGroup ambientGlowGroup =
            ambientGlow.gameObject.AddComponent<CanvasGroup>();
        ambientGlowGroup.alpha = 0f;

        GameObject oneTapRevealMaskObject =
            CreateRectObject("OneTapRevealMask", fallbackLogo.transform);
        RectTransform oneTapRevealMask =
            oneTapRevealMaskObject.GetComponent<RectTransform>();
        oneTapRevealMask.anchoredPosition = new Vector2(0f, 92f);
        oneTapRevealMask.sizeDelta = new Vector2(880f, 210f);
        oneTapRevealMaskObject.AddComponent<RectMask2D>();
        Image oneTapLogo = CreateImage(
            "OneTapLogo",
            oneTapRevealMask,
            LoadSprite(OneTapPath),
            Vector2.zero,
            new Vector2(880f, 210f));
        oneTapLogo.preserveAspect = true;
        oneTapLogo.raycastTarget = false;
        CanvasGroup oneTapLogoGroup =
            oneTapLogo.gameObject.AddComponent<CanvasGroup>();
        oneTapLogoGroup.alpha = 0f;

        GameObject garageRevealMaskObject =
            CreateRectObject("GarageRevealMask", fallbackLogo.transform);
        RectTransform garageRevealMask =
            garageRevealMaskObject.GetComponent<RectTransform>();
        garageRevealMask.anchoredPosition = new Vector2(0f, -82f);
        garageRevealMask.sizeDelta = new Vector2(560f, 104f);
        garageRevealMaskObject.AddComponent<RectMask2D>();
        Image garageModule = CreateImage(
            "GarageModule",
            garageRevealMask,
            LoadSprite(GaragePath),
            Vector2.zero,
            new Vector2(560f, 104f));
        garageModule.preserveAspect = true;
        garageModule.raycastTarget = false;
        CanvasGroup garageModuleGroup =
            garageModule.gameObject.AddComponent<CanvasGroup>();
        garageModuleGroup.alpha = 0f;

        Image tapPoint = CreateImage(
            "TapPoint",
            fallbackLogo.transform,
            LoadSprite(TapPointPath),
            new Vector2(214f, 134f),
            new Vector2(88f, 88f));
        tapPoint.preserveAspect = true;
        tapPoint.raycastTarget = false;
        CanvasGroup tapPointGroup =
            tapPoint.gameObject.AddComponent<CanvasGroup>();
        tapPointGroup.alpha = 0f;
        fallbackLogo.SetActive(true);

        GameObject transitionMask =
            CreateRectObject("TransitionMask", canvasObject.transform);
        Stretch(transitionMask.GetComponent<RectTransform>());
        transitionMask.AddComponent<RectMask2D>();

        GameObject lightPassObject =
            CreateRectObject("LightPass", transitionMask.transform);
        RectTransform lightPassRect = lightPassObject.GetComponent<RectTransform>();
        Stretch(lightPassRect);
        RawImage lightPass = lightPassObject.AddComponent<RawImage>();
        lightPass.texture = targetTexture;
        lightPass.color = Color.white;
        lightPass.raycastTarget = true;
        lightPass.enabled = false;

        GameObject audioObject = new("AudioSource", typeof(AudioSource));
        audioObject.transform.SetParent(root.transform, false);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.65f;

        GameObject videoObject = new("VideoPlayer", typeof(VideoPlayer));
        videoObject.transform.SetParent(root.transform, false);
        VideoPlayer videoPlayer = videoObject.GetComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = targetTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;

        BindController(
            controller,
            videoPlayer,
            lightPass,
            audioSource,
            splashAudioListener,
            splashEventSystem,
            canvasGroup,
            staticFallback,
            fallbackLogo,
            safeArea.GetComponent<RectTransform>(),
            oneTapRevealMask,
            garageRevealMask,
            oneTapLogo.rectTransform,
            garageModule.rectTransform,
            tapPoint.rectTransform,
            ambientGlow.rectTransform,
            oneTapLogoGroup,
            garageModuleGroup,
            tapPointGroup,
            ambientGlowGroup);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        PutSplashFirstInBuildSettings();
        EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "ONE TAP GARAGE approved splash scene rebuilt and placed first in Build Settings. "
            + "Existing MainMenu scene and scripts were not opened or modified.");
    }

    [MenuItem("Tools/ONE TAP GARAGE/Skip Splash In Editor")]
    private static void ToggleEditorSkip()
    {
        bool enabled = !EditorPrefs.GetBool(EditorSkipKey, false);
        EditorPrefs.SetBool(EditorSkipKey, enabled);
        Menu.SetChecked("Tools/ONE TAP GARAGE/Skip Splash In Editor", enabled);
        Debug.Log($"ONE TAP GARAGE Editor skip: {(enabled ? "ON" : "OFF")}");
    }

    [MenuItem("Tools/ONE TAP GARAGE/Skip Splash In Editor", true)]
    private static bool ValidateEditorSkip()
    {
        Menu.SetChecked(
            "Tools/ONE TAP GARAGE/Skip Splash In Editor",
            EditorPrefs.GetBool(EditorSkipKey, false));
        return true;
    }

    [MenuItem("Tools/ONE TAP GARAGE/Restore Editor Audio Defaults")]
    private static void RestoreEditorAudioDefaults()
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, 0.7f);
        PlayerPrefs.SetFloat(SfxVolumeKey, 0.8f);
        PlayerPrefs.SetInt(AudioMutedKey, 0);
        PlayerPrefs.Save();
        Debug.Log(
            "ONE TAP GARAGE Editor audio restored: "
            + "MusicVolume=0.7, SfxVolume=0.8, AudioMuted=0.");
    }

    [MenuItem("Tools/ONE TAP GARAGE/Build Android Audit APK")]
    public static void BuildAndroidAuditApk()
    {
        ValidateApprovedFiles();

        if (EditorBuildSettings.scenes.Length == 0
            || !string.Equals(
                EditorBuildSettings.scenes[0].path,
                ScenePath,
                StringComparison.Ordinal))
        {
            throw new BuildFailedException(
                "OneTapGarageSplash must be the first enabled scene before the Android audit build.");
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
            && !EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android))
        {
            throw new BuildFailedException("Unity could not switch to the Android build target.");
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(AndroidAuditBuildPath)
            ?? "Artifacts/OneTapGarageSplash/UnityIntegration/Android");

        BuildPlayerOptions options = new()
        {
            scenes = Array.ConvertAll(
                EditorBuildSettings.scenes,
                scene => scene.path),
            locationPathName = AndroidAuditBuildPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.Development,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Android audit build failed: {report.summary.result}; "
                + $"{report.summary.totalErrors} error(s), "
                + $"{report.summary.totalWarnings} warning(s).");
        }

        Debug.Log(
            $"ONE TAP GARAGE Android audit APK built: {AndroidAuditBuildPath}; "
            + $"{report.summary.totalSize} bytes; "
            + $"{report.summary.totalWarnings} warning(s).");
    }

    private static void ValidateApprovedFiles()
    {
        string[] required =
        {
            VideoPath,
            AudioPath,
            BackgroundPath,
            OneTapPath,
            GaragePath,
            TapPointPath,
            FullLogoPath,
            MainMenuPath,
        };

        for (int index = 0; index < required.Length; index++)
        {
            if (!File.Exists(required[index]))
            {
                throw new FileNotFoundException(
                    $"ONE TAP GARAGE approved asset is missing: {required[index]}");
            }
        }
    }

    private static void ConfigureTexture(string path, bool alpha)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            throw new UnityException($"Texture importer was not found for {path}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = path == BackgroundPath ? 2048 : 4096;
        importer.SaveAndReimport();
    }

    private static RenderTexture CreateOrReplaceRenderTexture()
    {
        RenderTexture existing =
            AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(RenderTexturePath);
        }

        RenderTexture renderTexture = new(1920, 1080, 0, RenderTextureFormat.ARGB32)
        {
            name = "OneTapGarageSplash",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
        return renderTexture;
    }

    private static AudioListener CreateCamera(Transform parent)
    {
        GameObject cameraObject =
            new("SplashCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(parent, false);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.003f, 0.012f, 0.009f, 1f);
        camera.orthographic = true;
        camera.depth = -100f;
        return cameraObject.GetComponent<AudioListener>();
    }

    private static EventSystem CreateEventSystem(Transform parent)
    {
        GameObject eventSystemObject = new(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(parent, false);
        return eventSystemObject.GetComponent<EventSystem>();
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject gameObject = CreateRectObject(name, parent);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new UnityException($"Sprite import failed: {path}");
        }

        return sprite;
    }

    private static void BindController(
        OneTapGarageSplashController controller,
        VideoPlayer videoPlayer,
        RawImage videoSurface,
        AudioSource audioSource,
        AudioListener splashAudioListener,
        EventSystem splashEventSystem,
        CanvasGroup canvasGroup,
        GameObject staticFallback,
        GameObject fallbackLogo,
        RectTransform safeArea,
        RectTransform oneTapRevealMask,
        RectTransform garageRevealMask,
        RectTransform oneTapLogo,
        RectTransform garageModule,
        RectTransform tapPoint,
        RectTransform ambientGlow,
        CanvasGroup oneTapLogoGroup,
        CanvasGroup garageModuleGroup,
        CanvasGroup tapPointGroup,
        CanvasGroup ambientGlowGroup)
    {
        SerializedObject serialized = new(controller);
        serialized.FindProperty("splashDuration").floatValue = 2.1f;
        serialized.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
        serialized.FindProperty("videoPlayer").objectReferenceValue = videoPlayer;
        serialized.FindProperty("videoSurface").objectReferenceValue = videoSurface;
        serialized.FindProperty("audioSource").objectReferenceValue = audioSource;
        serialized.FindProperty("splashAudioListener").objectReferenceValue =
            splashAudioListener;
        serialized.FindProperty("splashEventSystem").objectReferenceValue =
            splashEventSystem;
        serialized.FindProperty("splashCanvasGroup").objectReferenceValue = canvasGroup;
        serialized.FindProperty("staticFallback").objectReferenceValue = staticFallback;
        serialized.FindProperty("fallbackLogo").objectReferenceValue = fallbackLogo;
        serialized.FindProperty("safeAreaRoot").objectReferenceValue = safeArea;
        serialized.FindProperty("oneTapRevealMask").objectReferenceValue =
            oneTapRevealMask;
        serialized.FindProperty("garageRevealMask").objectReferenceValue =
            garageRevealMask;
        serialized.FindProperty("oneTapLogo").objectReferenceValue = oneTapLogo;
        serialized.FindProperty("garageModule").objectReferenceValue = garageModule;
        serialized.FindProperty("tapPoint").objectReferenceValue = tapPoint;
        serialized.FindProperty("ambientGlow").objectReferenceValue = ambientGlow;
        serialized.FindProperty("oneTapLogoGroup").objectReferenceValue =
            oneTapLogoGroup;
        serialized.FindProperty("garageModuleGroup").objectReferenceValue =
            garageModuleGroup;
        serialized.FindProperty("tapPointGroup").objectReferenceValue =
            tapPointGroup;
        serialized.FindProperty("ambientGlowGroup").objectReferenceValue =
            ambientGlowGroup;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PutSplashFirstInBuildSettings()
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] next =
            new EditorBuildSettingsScene[current.Length + 1];
        next[0] = new EditorBuildSettingsScene(ScenePath, true);

        int writeIndex = 1;
        for (int index = 0; index < current.Length; index++)
        {
            if (string.Equals(current[index].path, ScenePath, StringComparison.Ordinal))
            {
                continue;
            }

            next[writeIndex++] = current[index];
        }

        if (writeIndex != next.Length)
        {
            Array.Resize(ref next, writeIndex);
        }

        EditorBuildSettings.scenes = next;
    }
}
