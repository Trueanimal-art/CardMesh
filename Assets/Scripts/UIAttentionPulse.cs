using System.Collections;
using UnityEngine;

namespace CardMesh
{
    public sealed class UIAttentionPulse : MonoBehaviour
    {
        [SerializeField] private float interval = 6f;
        [SerializeField] private float peakScale = 1.08f;
        [SerializeField] private float pulseDuration = 0.5f;

        private Vector3 baseScale;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            baseScale = transform.localScale;
            pulseRoutine = StartCoroutine(PulseLoop());
        }

        private void OnDisable()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            transform.localScale = baseScale;
        }

        private IEnumerator PulseLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(interval);
                yield return Animate(baseScale, baseScale * peakScale, pulseDuration * 0.4f);
                yield return Animate(baseScale * peakScale, baseScale, pulseDuration * 0.6f);
            }
        }

        private IEnumerator Animate(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            transform.localScale = to;
        }
    }
}
