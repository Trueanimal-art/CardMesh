using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class WinScreenView : MonoBehaviour
    {
        private const string CompletedTitle = "\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u043f\u0440\u043e\u0439\u0434\u0435\u043d";
        private const string MovesPrefix = "\u0425\u043e\u0434\u044b";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI movesText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;
        [SerializeField] private Vector3 hiddenScale = new(0.92f, 0.92f, 1f);

        private Coroutine showRoutine;
        private bool isShowing;

        private void Awake()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            panel ??= transform as RectTransform;

            if (!isShowing)
            {
                ApplyHiddenVisuals();
            }
        }

        public void Configure(UnityAction onRetry, UnityAction onMenu)
        {
            WireButton(retryButton, onRetry);
            WireButton(menuButton, onMenu);
        }

        public void ConfigureComponents(
            CanvasGroup targetCanvasGroup,
            RectTransform targetPanel,
            TextMeshProUGUI targetTitleText,
            TextMeshProUGUI targetMovesText,
            Button targetRetryButton,
            Button targetMenuButton)
        {
            canvasGroup = targetCanvasGroup;
            panel = targetPanel;
            titleText = targetTitleText;
            movesText = targetMovesText;
            retryButton = targetRetryButton;
            menuButton = targetMenuButton;
            ApplyHiddenVisuals();
        }

        public void Show(int usedMoves)
        {
            if (titleText != null)
            {
                titleText.text = CompletedTitle;
            }

            if (movesText != null)
            {
                movesText.text = $"{MovesPrefix}: {usedMoves}";
            }

            isShowing = true;
            gameObject.SetActive(true);

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
            }

            showRoutine = StartCoroutine(ShowAnimated());
        }

        public void HideInstant()
        {
            isShowing = false;

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }

            ApplyHiddenVisuals();
            gameObject.SetActive(false);
        }

        private void ApplyHiddenVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (panel != null)
            {
                panel.localScale = hiddenScale;
            }
        }

        private IEnumerator ShowAnimated()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (panel != null)
            {
                panel.localScale = hiddenScale;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 2f);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }

                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(hiddenScale, Vector3.one, eased);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }

            showRoutine = null;
        }

        private static void WireButton(Button button, UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
