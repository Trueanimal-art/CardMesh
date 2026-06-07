using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SolitaireAssociations
{
    public sealed class UIJuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float hoverScale = 1.04f;
        [SerializeField] private float pressScale = 0.96f;
        [SerializeField] private float duration = 0.08f;

        private Coroutine scaleRoutine;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AnimateTo(baseScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateTo(baseScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            AnimateTo(baseScale * pressScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            AnimateTo(baseScale * hoverScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            MusicPlayer.Instance?.PlayButtonClick();
        }

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
    }
}
