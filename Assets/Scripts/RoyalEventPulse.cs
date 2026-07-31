using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [DisallowMultipleComponent]
    public sealed class RoyalEventPulse : MonoBehaviour
    {
        [SerializeField] private float scaleAmplitude = 0.02f;
        [SerializeField] private float alphaAmplitude;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float phase;

        private RectTransform rectTransform;
        private Graphic graphic;
        private Vector3 baseScale;
        private Color baseColor;

        public void Configure(float scale, float alpha, float pulseSpeed, float startPhase)
        {
            scaleAmplitude = Mathf.Max(0f, scale);
            alphaAmplitude = Mathf.Max(0f, alpha);
            speed = Mathf.Max(0.05f, pulseSpeed);
            phase = startPhase;
            CaptureBaseState();
        }

        private void OnEnable()
        {
            CaptureBaseState();
        }

        private void Update()
        {
            float wave = (Mathf.Sin((Time.unscaledTime * speed) + phase) + 1f) * 0.5f;
            if (rectTransform != null && scaleAmplitude > 0f)
            {
                float scale = 1f + ((wave - 0.5f) * 2f * scaleAmplitude);
                rectTransform.localScale = baseScale * scale;
            }

            if (graphic != null && alphaAmplitude > 0f)
            {
                Color color = baseColor;
                color.a = Mathf.Clamp01(baseColor.a * (1f - alphaAmplitude + (wave * alphaAmplitude)));
                graphic.color = color;
            }
        }

        private void CaptureBaseState()
        {
            rectTransform = transform as RectTransform;
            graphic = GetComponent<Graphic>();
            if (rectTransform != null)
            {
                baseScale = rectTransform.localScale;
            }

            if (graphic != null)
            {
                baseColor = graphic.color;
            }
        }
    }
}
