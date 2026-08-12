using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class WinScreenView : MonoBehaviour
    {
        private const string CompletedTitle = "\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u043f\u0440\u043e\u0439\u0434\u0435\u043d!";
        private const string CompletedSubtitle = "\u041c\u043e\u043b\u043e\u0434\u0435\u0446!";
        private const int ConfettiCount = 46;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [FormerlySerializedAs("movesText")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField, Min(0f)] private float fadeDuration = 0.34f;
        [SerializeField] private Vector3 hiddenScale = new(0.84f, 0.84f, 1f);

        private Coroutine showRoutine;
        private Coroutine confettiRoutine;
        private RectTransform confettiLayer;
        private readonly Color[] confettiColors =
        {
            new(1f, 0.82f, 0.16f, 1f),
            new(1f, 0.96f, 0.58f, 1f),
            new(0.25f, 0.94f, 0.58f, 1f),
            new(0.98f, 0.35f, 0.27f, 1f),
            new(0.52f, 0.86f, 1f, 1f)
        };
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
            TextMeshProUGUI targetSubtitleText,
            Button targetRetryButton,
            Button targetMenuButton)
        {
            canvasGroup = targetCanvasGroup;
            panel = targetPanel;
            titleText = targetTitleText;
            subtitleText = targetSubtitleText;
            retryButton = targetRetryButton;
            menuButton = targetMenuButton;
            ApplyHiddenVisuals();
        }

        public void Show(int usedMoves)
        {
            AdsManager.Instance?.SetBannerBlocker("WinScreen", true);
            if (titleText != null)
            {
                titleText.text = CompletedTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = CompletedSubtitle;
            }

            isShowing = true;
            gameObject.SetActive(true);

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
            }

            if (confettiRoutine != null)
            {
                StopCoroutine(confettiRoutine);
            }

            showRoutine = StartCoroutine(ShowAnimated());
        }

        public void HideInstant()
        {
            AdsManager.Instance?.SetBannerBlocker("WinScreen", false);
            isShowing = false;

            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }

            if (confettiRoutine != null)
            {
                StopCoroutine(confettiRoutine);
                confettiRoutine = null;
            }

            ClearConfetti();
            ApplyHiddenVisuals();
            gameObject.SetActive(false);
        }

        private void EnsureConfettiLayer()
        {
            Transform existing = transform.Find("ConfettiLayer");
            if (existing != null)
            {
                confettiLayer = existing as RectTransform;
                return;
            }

            GameObject layerObject = new("ConfettiLayer", typeof(RectTransform));
            confettiLayer = layerObject.GetComponent<RectTransform>();
            confettiLayer.SetParent(transform, false);
            confettiLayer.anchorMin = Vector2.zero;
            confettiLayer.anchorMax = Vector2.one;
            confettiLayer.pivot = new Vector2(0.5f, 0.5f);
            confettiLayer.offsetMin = Vector2.zero;
            confettiLayer.offsetMax = Vector2.zero;
            confettiLayer.SetAsLastSibling();
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
            EnsureConfettiLayer();
            ClearConfetti();

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

            confettiRoutine = StartCoroutine(PlayConfetti());
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);

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

        private IEnumerator PlayConfetti()
        {
            EnsureConfettiLayer();
            if (confettiLayer == null)
            {
                yield break;
            }

            Rect rect = confettiLayer.rect;
            float halfWidth = Mathf.Max(420f, rect.width * 0.5f);
            float halfHeight = Mathf.Max(260f, rect.height * 0.5f);

            for (int index = 0; index < ConfettiCount; index++)
            {
                Image piece = CreateConfettiPiece(index);
                RectTransform pieceRect = piece.rectTransform;
                pieceRect.SetParent(confettiLayer, false);
                pieceRect.anchoredPosition = new Vector2(Random.Range(-halfWidth * 0.72f, halfWidth * 0.72f), Random.Range(halfHeight * 0.12f, halfHeight * 0.42f));
                pieceRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f));
                pieceRect.sizeDelta = new Vector2(Random.Range(8f, 15f), Random.Range(14f, 24f));
                StartCoroutine(AnimateConfetti(pieceRect, piece, halfHeight));

                if (index % 5 == 0)
                {
                    yield return null;
                }
            }
        }

        private Image CreateConfettiPiece(int index)
        {
            GameObject pieceObject = new($"Confetti_{index}", typeof(RectTransform), typeof(Image));
            Image image = pieceObject.GetComponent<Image>();
            image.color = confettiColors[index % confettiColors.Length];
            image.raycastTarget = false;
            return image;
        }

        private static IEnumerator AnimateConfetti(RectTransform piece, Image image, float halfHeight)
        {
            Vector2 start = piece.anchoredPosition;
            Vector2 end = start + new Vector2(Random.Range(-120f, 120f), -Random.Range(260f, halfHeight + 180f));
            Quaternion startRotation = piece.localRotation;
            Quaternion endRotation = startRotation * Quaternion.Euler(0f, 0f, Random.Range(180f, 520f));
            float duration = Random.Range(1.05f, 1.65f);
            float elapsed = 0f;
            Color startColor = image.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 2f);
                piece.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                piece.localRotation = Quaternion.LerpUnclamped(startRotation, endRotation, progress);
                image.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, Mathf.Max(0f, progress - 0.58f) / 0.42f));
                yield return null;
            }

            Destroy(piece.gameObject);
        }

        private void ClearConfetti()
        {
            if (confettiLayer == null)
            {
                return;
            }

            for (int index = confettiLayer.childCount - 1; index >= 0; index--)
            {
                Destroy(confettiLayer.GetChild(index).gameObject);
            }
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
