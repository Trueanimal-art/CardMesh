using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class RateUsStarInteraction : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        [SerializeField] private RateUsPopupController controller;
        [SerializeField, Range(1, 5)] private int ratingValue = 1;
        [SerializeField] private RectTransform starVisual;
        [SerializeField] private CanvasGroup glow;
        [SerializeField] private float hoverScale = 1.13f;
        [SerializeField] private float selectedScale = 1.04f;
        [SerializeField] private float animationDuration = 0.09f;

        private Coroutine animationRoutine;
        private bool pointerInside;
        private bool selected;

        public int RatingValue => ratingValue;

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            controller?.PreviewRating(ratingValue);
            AnimateTo(hoverScale, 0.62f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            controller?.ClearPreviewRating();
            AnimateTo(selected ? selectedScale : 1f, selected ? 0.3f : 0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            controller?.PreviewRating(ratingValue);
            AnimateTo(1.08f, 0.72f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(pointerInside ? hoverScale : (selected ? selectedScale : 1f),
                selected || pointerInside ? 0.56f : 0f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            MusicPlayer.Instance?.PlayButtonClick();
        }

        public void SetSelected(bool value, bool immediate)
        {
            selected = value;
            if (pointerInside && !immediate)
            {
                return;
            }

            float scale = selected ? selectedScale : 1f;
            float glowAlpha = selected ? 0.3f : 0f;
            if (immediate || !isActiveAndEnabled)
            {
                ApplyVisual(scale, glowAlpha);
            }
            else
            {
                AnimateTo(scale, glowAlpha);
            }
        }

        public void SetInteractable(bool value)
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.interactable = value;
            }
        }

        public void PlayConfirmationFlash(float delay)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(ConfirmationFlash(delay));
        }

        private void AnimateTo(float scale, float glowAlpha)
        {
            if (starVisual == null)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                ApplyVisual(scale, glowAlpha);
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(AnimateVisual(scale, glowAlpha));
        }

        private IEnumerator AnimateVisual(float targetScale, float targetGlow)
        {
            Vector3 startScale = starVisual.localScale;
            float startGlow = glow != null ? glow.alpha : 0f;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                starVisual.localScale =
                    Vector3.LerpUnclamped(startScale, Vector3.one * targetScale, eased);
                if (glow != null)
                {
                    glow.alpha = Mathf.Lerp(startGlow, targetGlow, eased);
                }

                yield return null;
            }

            ApplyVisual(targetScale, targetGlow);
            animationRoutine = null;
        }

        private IEnumerator ConfirmationFlash(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            Vector3 baseScale = Vector3.one * selectedScale;
            float elapsed = 0f;
            const float duration = 0.34f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                starVisual.localScale = baseScale * Mathf.Lerp(1f, 1.13f, pulse);
                if (glow != null)
                {
                    glow.alpha = Mathf.Lerp(0.3f, 0.92f, pulse);
                }

                yield return null;
            }

            ApplyVisual(selectedScale, 0.3f);
            animationRoutine = null;
        }

        private void ApplyVisual(float scale, float glowAlpha)
        {
            if (starVisual != null)
            {
                starVisual.localScale = Vector3.one * scale;
            }

            if (glow != null)
            {
                glow.alpha = glowAlpha;
            }
        }
    }
}
