using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardMesh
{
    public sealed class UIJuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float pressScale = 0.96f;
        [SerializeField] private float duration = 0.08f;
        [SerializeField] private bool hoverFeedback = true;
        [SerializeField] private bool mobileHaptics = true;

        private Coroutine scaleRoutine;
        private Coroutine glowRoutine;
        private Vector3 baseScale = Vector3.one;
        private CanvasGroup hoverGlow;

        private void Awake()
        {
            baseScale = transform.localScale;
            RefreshGlow();
            if (hoverGlow != null)
            {
                hoverGlow.alpha = 0f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ShouldUseHoverFeedback(eventData))
            {
                return;
            }

            AnimateTo(baseScale * hoverScale);
            AnimateGlow(1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ShouldUseHoverFeedback(eventData))
            {
                return;
            }

            AnimateTo(baseScale);
            AnimateGlow(0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(baseScale * pressScale);
            if (mobileHaptics)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                PlayLightHaptic();
#endif
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(ShouldUseHoverFeedback(eventData) ? baseScale * hoverScale : baseScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            MusicPlayer.Instance?.PlayButtonClick();
        }

        public void ConfigureMobileFeedback(float pressedScale, float animationDuration)
        {
            pressScale = Mathf.Clamp(pressedScale, 0.85f, 1f);
            duration = Mathf.Clamp(animationDuration, 0.04f, 0.25f);
            hoverFeedback = false;
            mobileHaptics = true;
        }

        private bool ShouldUseHoverFeedback(PointerEventData eventData)
        {
            if (!hoverFeedback || Application.isMobilePlatform)
            {
                return false;
            }

            return eventData == null || eventData.pointerId < 0;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void PlayLightHaptic()
        {
            try
            {
                using AndroidJavaClass version = new("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") < 26)
                {
                    return;
                }

                using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject vibrator =
                    activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using AndroidJavaClass vibrationEffect =
                    new("android.os.VibrationEffect");
                using AndroidJavaObject effect =
                    vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", 12L, 32);
                vibrator.Call("vibrate", effect);
            }
            catch (System.Exception)
            {
                // Haptics are optional and must never block the button action.
            }
        }
#endif

        private void AnimateTo(Vector3 targetScale)
        {
            if (!isActiveAndEnabled)
            {
                transform.localScale = targetScale;
                return;
            }

            if (scaleRoutine != null)
            {
                StopCoroutine(scaleRoutine);
            }

            scaleRoutine = StartCoroutine(ScaleTo(targetScale));
        }

        private void AnimateGlow(float targetAlpha)
        {
            if (hoverGlow == null)
            {
                RefreshGlow();
                if (hoverGlow == null)
                {
                    return;
                }
            }

            if (!isActiveAndEnabled)
            {
                hoverGlow.alpha = targetAlpha;
                return;
            }

            if (glowRoutine != null)
            {
                StopCoroutine(glowRoutine);
            }

            glowRoutine = StartCoroutine(FadeGlow(targetAlpha));
        }

        private void RefreshGlow()
        {
            hoverGlow = transform.Find("HoverGlow")?.GetComponent<CanvasGroup>();
        }

        private IEnumerator ScaleTo(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, 1f - Mathf.Pow(1f - progress, 2f));
                yield return null;
            }

            transform.localScale = targetScale;
            scaleRoutine = null;
        }

        private IEnumerator FadeGlow(float targetAlpha)
        {
            float startAlpha = hoverGlow.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                hoverGlow.alpha = Mathf.Lerp(startAlpha, targetAlpha, 1f - Mathf.Pow(1f - progress, 2f));
                yield return null;
            }

            hoverGlow.alpha = targetAlpha;
            glowRoutine = null;
        }
    }
}
