using System.Collections;
using TMPro;
using UnityEngine;

namespace CardMesh
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class HudValuePulse : MonoBehaviour
    {
        private TMP_Text target;
        private Coroutine pulseRoutine;
        private string lastValue;

        private void Awake()
        {
            target = GetComponent<TMP_Text>();
            lastValue = target != null ? target.text : string.Empty;
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            transform.localScale = Vector3.one;
        }

        private void HandleTextChanged(UnityEngine.Object changedObject)
        {
            if (target == null || changedObject != target || target.text == lastValue)
            {
                return;
            }

            lastValue = target.text;
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PlayPulse());
        }

        private IEnumerator PlayPulse()
        {
            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = Vector3.one * (1f + (wave * 0.08f));
                yield return null;
            }

            transform.localScale = Vector3.one;
            pulseRoutine = null;
        }
    }
}
