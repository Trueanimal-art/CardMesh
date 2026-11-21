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
            ApplyMobileLayout();

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

        // The scene-authored window was shorter than its content: "В меню" hung below the frame
        // and the title touched the top border. Lay the window out explicitly (reference 1080x1920).
        private void ApplyMobileLayout()
        {
            RectTransform box = titleText != null ? titleText.rectTransform.parent as RectTransform : null;
            if (box == null)
            {
                return;
            }

            // Same geometry as the authored "Начать заново?" popup, which uses the same frame
            // sprite: the sprite has wide transparent margins, so the rect is 920x900 while the
            // visible frame is much smaller. Title / body / two buttons sit where that popup has them.
            box.anchorMin = new Vector2(0.5f, 0.5f);
            box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(920f, 900f);
            box.anchoredPosition = Vector2.zero;

            PlaceCentered(titleText.rectTransform, new Vector2(0f, 202f), new Vector2(788f, 76f));
            StyleText(titleText, 52f, 44f);
            if (subtitleText != null)
            {
                PlaceCentered(subtitleText.rectTransform, new Vector2(0f, 50f), new Vector2(770f, 72f));
                StyleText(subtitleText, 38f, 32f);
            }

            PlaceCenteredButton(retryButton, new Vector2(0f, -50f));
            PlaceCenteredButton(menuButton, new Vector2(0f, -150f));
        }

        private static void PlaceCentered(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = center;
        }

        private static void PlaceCenteredButton(Button button, Vector2 center)
        {
            if (button == null || button.transform is not RectTransform rect)
            {
                return;
            }

            PlaceCentered(rect, center, new Vector2(560f, 96f));
            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = new Vector2(24f, 6f);
                labelRect.offsetMax = new Vector2(-24f, -6f);
                StyleText(label, 36f, 30f);
            }
        }

        private static void StyleText(TMP_Text text, float max, float min)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = min;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
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
