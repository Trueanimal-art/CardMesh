using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CardMesh.OneTapGarage
{
    [DisallowMultipleComponent]
    public sealed class OneTapGarageSplashController : MonoBehaviour
    {
        private const string SfxVolumeKey = "SfxVolume";
        private const string AudioMutedKey = "AudioMuted";
        private const string EditorSkipKey = "CardMesh.OneTapGarage.SkipInEditor";
        private const float DefaultSfxVolume = 0.8f;
        private const float SafeFirstLaunchVolume = 0.65f;

        [Header("Approved timing")]
        [SerializeField, Min(0.1f)] private float splashDuration = 2.1f;
        [SerializeField, Min(0f)] private float mainMenuActivationTime = 0.75f;
        [SerializeField, Min(0f)] private float liveMenuWipeStartTime = 1.25f;
        [SerializeField, Range(0.2f, 0.4f)] private float liveMenuWipeDuration = 0.4f;

        [Header("Scene loading")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Approved playback")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoSurface;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioListener splashAudioListener;
        [SerializeField] private EventSystem splashEventSystem;
        [SerializeField] private CanvasGroup splashCanvasGroup;
        [SerializeField] private GameObject staticFallback;
        [SerializeField] private GameObject fallbackLogo;
        [SerializeField] private RectTransform safeAreaRoot;

        private static bool playedThisProcess;

        private AsyncOperation mainMenuLoad;
        private AudioSource[] pausedMenuAudio;
        private bool[] previousMenuAudioMute;
        private bool pausedVideo;
        private bool pausedAudio;
        private bool playbackStarted;
        private bool transitionFinished;
        private bool menuAudioCaptured;
        private float liveMenuWipeStartedAt = -1f;
        private Rect baseVideoUv = new(0f, 0f, 1f, 1f);
        private float elapsed;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;

        public float Elapsed => elapsed;
        public bool MainMenuLoadStarted => mainMenuLoad != null;
        public bool TransitionFinished => transitionFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProcessState()
        {
            playedThisProcess = false;
        }

        private void Awake()
        {
            ForceLandscape();
            ApplySafeArea(true);

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 1f;
                splashCanvasGroup.interactable = true;
                splashCanvasGroup.blocksRaycasts = true;
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
                audioSource.volume = ResolveSplashVolume();
            }

            if (videoPlayer != null)
            {
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.skipOnDrop = false;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.timeReference = VideoTimeReference.ExternalTime;
                videoPlayer.externalReferenceTime = 0d;
            }
        }

        private void Start()
        {
            if (ShouldSkipSplash())
            {
                Debug.Log("OneTapGarageSplash: skipped by the Editor developer toggle or because it already played in this process.");
                playedThisProcess = true;
                SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
                return;
            }

            playedThisProcess = true;
            Debug.Log("OneTapGarageSplash: starting approved animation from frame zero.");
            StartCoroutine(RunSplash());
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth
                || Screen.height != lastScreenHeight
                || Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea(false);
            }
        }

        private IEnumerator RunSplash()
        {
            mainMenuLoad = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
            if (mainMenuLoad == null)
            {
                Debug.LogError($"OneTapGarageSplash: scene '{mainMenuSceneName}' is not in Build Settings.");
                yield break;
            }

            mainMenuLoad.allowSceneActivation = false;

            if (videoPlayer != null && videoPlayer.clip != null)
            {
                videoPlayer.Prepare();
                float prepareElapsed = 0f;
                while (!videoPlayer.isPrepared && prepareElapsed < 2f)
                {
                    prepareElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                elapsed = 0f;

                if (videoPlayer.isPrepared)
                {
                    videoPlayer.Pause();
                    videoPlayer.frame = 0;
                    videoPlayer.time = 0d;
                    yield return null;

                    if (staticFallback != null)
                    {
                        staticFallback.SetActive(false);
                    }

                    if (videoSurface != null)
                    {
                        ConfigureVideoSurface();
                        videoSurface.enabled = true;
                    }

                    videoPlayer.Play();
                    playbackStarted = true;
                    Debug.Log(
                        $"OneTapGarageSplash: video playback started after "
                        + $"{prepareElapsed:0.000}s preparation.");
                }
                else if (fallbackLogo != null)
                {
                    fallbackLogo.SetActive(true);
                    Debug.LogError("OneTapGarageSplash: approved video could not be prepared; static approved lockup fallback is active.");
                }
            }

            if (audioSource != null && audioSource.clip != null && audioSource.volume > 0f)
            {
                audioSource.time = 0f;
                audioSource.Play();
            }

            while (elapsed < splashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (playbackStarted && videoPlayer != null)
                {
                    videoPlayer.externalReferenceTime = elapsed;
                }

                if (!mainMenuLoad.allowSceneActivation && elapsed >= mainMenuActivationTime)
                {
                    Debug.Log(
                        $"OneTapGarageSplash: MainMenu activation allowed at "
                        + $"{elapsed:0.000}s of splash playback.");
                    DisableSplashEventSystem();

                    if (splashAudioListener != null)
                    {
                        splashAudioListener.enabled = false;
                    }

                    mainMenuLoad.allowSceneActivation = true;
                }

                if (mainMenuLoad.isDone && !menuAudioCaptured)
                {
                    PauseMenuAudioUntilReveal();
                }

                UpdateLiveMenuWipe();
                yield return null;
            }

            if (!mainMenuLoad.allowSceneActivation)
            {
                DisableSplashEventSystem();

                if (splashAudioListener != null)
                {
                    splashAudioListener.enabled = false;
                }

                mainMenuLoad.allowSceneActivation = true;
            }

            while (!mainMenuLoad.isDone)
            {
                yield return null;
            }

            PauseMenuAudioUntilReveal();

            Scene mainMenu = SceneManager.GetSceneByName(mainMenuSceneName);
            if (mainMenu.IsValid() && mainMenu.isLoaded)
            {
                SceneManager.SetActiveScene(mainMenu);
            }

            while (videoSurface != null && videoSurface.enabled)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateLiveMenuWipe();
                yield return null;
            }

            FinishTransition();
        }

        private void ConfigureVideoSurface()
        {
            if (videoSurface == null)
            {
                return;
            }

            AspectRatioFitter fitter =
                videoSurface.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            RectTransform rect = videoSurface.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            float sourceWidth =
                videoPlayer != null && videoPlayer.clip != null
                    ? videoPlayer.clip.width
                    : 1920f;
            float sourceHeight =
                videoPlayer != null && videoPlayer.clip != null
                    ? videoPlayer.clip.height
                    : 1080f;
            float sourceAspect = sourceWidth / Mathf.Max(1f, sourceHeight);
            float screenAspect = Screen.width / Mathf.Max(1f, Screen.height);

            if (screenAspect < sourceAspect)
            {
                float visibleWidth = screenAspect / sourceAspect;
                baseVideoUv = new Rect(
                    (1f - visibleWidth) * 0.5f,
                    0f,
                    visibleWidth,
                    1f);
            }
            else
            {
                float visibleHeight = sourceAspect / screenAspect;
                baseVideoUv = new Rect(
                    0f,
                    (1f - visibleHeight) * 0.5f,
                    1f,
                    visibleHeight);
            }

            videoSurface.uvRect = baseVideoUv;
        }

        private void UpdateLiveMenuWipe()
        {
            if (videoSurface == null
                || !videoSurface.enabled
                || mainMenuLoad == null
                || !mainMenuLoad.isDone)
            {
                return;
            }

            if (liveMenuWipeStartedAt < 0f)
            {
                if (elapsed < liveMenuWipeStartTime)
                {
                    return;
                }

                liveMenuWipeStartedAt = elapsed;
                Debug.Log(
                    $"OneTapGarageSplash: live MainMenu Light Pass wipe "
                    + $"started at {elapsed:0.000}s.");
            }

            float duration = Mathf.Max(0.001f, liveMenuWipeDuration);
            float linearProgress = Mathf.Clamp01(
                (elapsed - liveMenuWipeStartedAt) / duration);
            float wavePhase = Mathf.Sin(linearProgress * Mathf.PI);
            float opticalWaveOffset =
                0.08f
                * wavePhase
                * wavePhase
                * (1f - linearProgress);
            float progress = Mathf.Clamp01(
                (Mathf.SmoothStep(0f, 1f, linearProgress) * 1.12f)
                + 0.027f
                + opticalWaveOffset);

            RectTransform rect = videoSurface.rectTransform;
            rect.anchorMin = new Vector2(progress, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(50f, 0f);
            rect.offsetMax = Vector2.zero;

            float remaining = 1f - progress;
            videoSurface.uvRect = new Rect(
                baseVideoUv.x + (baseVideoUv.width * progress),
                baseVideoUv.y,
                baseVideoUv.width * remaining,
                baseVideoUv.height);

            if (progress >= 1f)
            {
                videoSurface.enabled = false;
                Debug.Log(
                    $"OneTapGarageSplash: live MainMenu fully revealed at "
                    + $"{elapsed:0.000}s without a baked-menu swap.");
            }
        }

        private void FinishTransition()
        {
            if (transitionFinished)
            {
                return;
            }

            transitionFinished = true;
            Debug.Log(
                $"OneTapGarageSplash: transition finished at "
                + $"{elapsed:0.000}s of splash playback.");

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.blocksRaycasts = false;
                splashCanvasGroup.interactable = false;
                splashCanvasGroup.alpha = 0f;
            }

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            ResumeMenuAudio();
            Scene splashScene = gameObject.scene;
            if (splashScene.IsValid() && splashScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(splashScene);
            }
        }

        private void DisableSplashEventSystem()
        {
            if (splashEventSystem != null)
            {
                splashEventSystem.gameObject.SetActive(false);
            }
        }

        private void PauseMenuAudioUntilReveal()
        {
            if (menuAudioCaptured)
            {
                return;
            }

            menuAudioCaptured = true;
            MusicPlayer musicPlayer = MusicPlayer.Instance;
            if (musicPlayer == null)
            {
                return;
            }

            pausedMenuAudio = musicPlayer.GetComponents<AudioSource>();
            previousMenuAudioMute = new bool[pausedMenuAudio.Length];
            for (int index = 0; index < pausedMenuAudio.Length; index++)
            {
                AudioSource source = pausedMenuAudio[index];
                if (source != null)
                {
                    previousMenuAudioMute[index] = source.mute;
                    source.mute = true;
                }
            }
        }

        private void ResumeMenuAudio()
        {
            if (pausedMenuAudio == null)
            {
                return;
            }

            for (int index = 0; index < pausedMenuAudio.Length; index++)
            {
                AudioSource source = pausedMenuAudio[index];
                if (source != null)
                {
                    bool wasMuted =
                        previousMenuAudioMute != null
                        && index < previousMenuAudioMute.Length
                        && previousMenuAudioMute[index];
                    source.mute = wasMuted;
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!playbackStarted || transitionFinished)
            {
                return;
            }

            if (paused)
            {
                pausedVideo = videoPlayer != null && videoPlayer.isPlaying;
                pausedAudio = audioSource != null && audioSource.isPlaying;

                if (pausedVideo)
                {
                    videoPlayer.Pause();
                }

                if (pausedAudio)
                {
                    audioSource.Pause();
                }
            }
            else
            {
                if (pausedVideo && videoPlayer != null)
                {
                    videoPlayer.Play();
                }

                if (pausedAudio && audioSource != null)
                {
                    audioSource.UnPause();
                }

                pausedVideo = false;
                pausedAudio = false;
            }
        }

        private void ApplySafeArea(bool force)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;

            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if (!force
                && safeAreaRoot.anchorMin == anchorMin
                && safeAreaRoot.anchorMax == anchorMax)
            {
                return;
            }

            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private static float ResolveSplashVolume()
        {
            if (PlayerPrefs.GetInt(AudioMutedKey, 0) == 1)
            {
                return 0f;
            }

            float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
            return Mathf.Min(saved, SafeFirstLaunchVolume);
        }

        private static bool ShouldSkipSplash()
        {
            if (playedThisProcess)
            {
                return true;
            }

#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EditorSkipKey, false);
#else
            return false;
#endif
        }

        private static void ForceLandscape()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
