using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class SettingsSliderFeedback : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private Image handleImage;

        private Coroutine feedbackRoutine;
        private Color baseColor = Color.white;

        private void Awake()
        {
            if (handleImage != null)
            {
                baseColor = handleImage.color;
            }
        }

        public void OnPointerDown(PointerEventData eventData) =>
            Animate(true);

        public void OnBeginDrag(PointerEventData eventData) =>
            Animate(true);

        public void OnPointerUp(PointerEventData eventData) =>
            Animate(false);

        public void OnEndDrag(PointerEventData eventData) =>
            Animate(false);

        private void Animate(bool active)
        {
            if (!isActiveAndEnabled || handle == null)
            {
                return;
            }

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = StartCoroutine(AnimateRoutine(active));
        }

        private IEnumerator AnimateRoutine(bool active)
        {
            Vector3 startScale = handle.localScale;
            Vector3 targetScale = Vector3.one * (active ? 1.12f : 1f);
            Color startColor =
                handleImage != null ? handleImage.color : baseColor;
            Color targetColor = active
                ? new Color(1f, 0.91f, 0.48f, 1f)
                : baseColor;

            float elapsed = 0f;
            const float duration = 0.09f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                handle.localScale =
                    Vector3.LerpUnclamped(startScale, targetScale, eased);
                if (handleImage != null)
                {
                    handleImage.color =
                        Color.Lerp(startColor, targetColor, eased);
                }

                yield return null;
            }

            handle.localScale = targetScale;
            if (handleImage != null)
            {
                handleImage.color = targetColor;
            }

            feedbackRoutine = null;
        }
    }
}
