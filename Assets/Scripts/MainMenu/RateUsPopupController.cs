using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class RateUsPopupController : MonoBehaviour
    {
        private const string RatingPrefsKey = "CardMesh.Player.Rating";

        // Runtime layout (reference units, Panel-centre space). The panel uses the
        // pause-window frame sprite stretched (Image.Type.Simple). At 900x820 its
        // inner rails sit ~158 below the top / ~175 above the bottom, the baked
        // divider at +108 (diamond +90..+126), the bottom jewel tip at -215 and
        // the side inner rails ~107 from each side.
        private const string SubtitleText = "Понравилась игра?\nПоддержите нас оценкой!";
        private static readonly Vector2 PanelSize = new(900f, 820f);
        private static readonly Vector2 TitleSize = new(640f, 76f);
        private static readonly Vector2 TitlePosition = new(0f, 185f);
        private static readonly Vector2 SubtitleSize = new(600f, 84f);
        private static readonly Vector2 SubtitlePosition = new(0f, 34f);
        private const float StarRowY = -70f;
        private const float StarSpacing = 116f;
        private static readonly Vector2 ActionButtonSize = new(270f, 66f);
        private const float ActionButtonX = 160f;
        private const float ActionButtonY = -168f;
        private const float ActionLabelHorizontalPadding = 24f;
        private const float TitleFontSize = 54f;
        private const float SubtitleFontSize = 32f;
        private const float ActionLabelFontSize = 36f;

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Image[] starImages = new Image[5];
        [SerializeField] private Sprite filledStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private RateUsStarInteraction[] starInteractions = new RateUsStarInteraction[5];
        [SerializeField] private CanvasGroup submitGlow;
        [SerializeField] private CanvasGroup submitShine;
        [SerializeField] private RectTransform[] sparkleRects;
        [SerializeField] private CanvasGroup[] sparkleGroups;
        [SerializeField] private int selectedRating;

        private int previewRating;
        private Coroutine submitReadyRoutine;
        private Coroutine confirmationRoutine;
        private Vector2[] sparkleStartPositions;

        private void OnEnable()
        {
            popupRoot ??= gameObject;
            ApplyPolishedLayout();
            selectedRating = 0;
            previewRating = 0;
            submitReadyRoutine = null;
            confirmationRoutine = null;
            CacheSparklePositions();
            ResetEffects();
            RefreshStars(true);
            UpdateSubmitState(false);
        }

        public void SelectOne() => SetRating(1);
        public void SelectTwo() => SetRating(2);
        public void SelectThree() => SetRating(3);
        public void SelectFour() => SetRating(4);
        public void SelectFive() => SetRating(5);

        public void Open()
        {
            MainMenuPopupExclusivity.OpenExclusive(this);
            GameObject root = popupRoot != null ? popupRoot : gameObject;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            ApplyPolishedLayout();
        }

        /// <summary>
        /// Final geometry and font sizes. The prefab and MainMenu.unity overrides put
        /// the action buttons on the frame's bottom rail and used a 21-unit subtitle;
        /// this runs on every open so the serialized values cannot win.
        /// </summary>
        private void ApplyPolishedLayout()
        {
            Transform root = popupRoot != null ? popupRoot.transform : transform;
            RectTransform panel = root.Find("Panel") as RectTransform;
            if (panel == null)
            {
                return;
            }

            panel.sizeDelta = PanelSize;

            TextMeshProUGUI title = FindDirectText(panel, "Title");
            if (title != null)
            {
                SetCentered(title.rectTransform, TitleSize, TitlePosition);
                title.alignment = TextAlignmentOptions.Center;
                ApplyFixedFont(title, TitleFontSize, false);
            }

            TextMeshProUGUI subtitle = FindDirectText(panel, "Subtitle");
            if (subtitle != null)
            {
                SetCentered(subtitle.rectTransform, SubtitleSize, SubtitlePosition);
                subtitle.text = SubtitleText;
                subtitle.alignment = TextAlignmentOptions.Center;
                ApplyFixedFont(subtitle, SubtitleFontSize, true);
            }

            for (int i = 0; i < 5; i++)
            {
                RectTransform star = panel.Find($"StarButton{i + 1}") as RectTransform;
                if (star != null)
                {
                    star.anchoredPosition = new Vector2((i - 2) * StarSpacing, StarRowY);
                }
            }

            RectTransform sparkles = panel.Find("RatingSparkles") as RectTransform;
            if (sparkles != null)
            {
                sparkles.anchoredPosition = new Vector2(0f, StarRowY);
            }

            LayoutActionButton(panel.Find("CancelButton") as RectTransform, -ActionButtonX);
            LayoutActionButton(panel.Find("SubmitButton") as RectTransform, ActionButtonX);
        }

        private static void LayoutActionButton(RectTransform button, float x)
        {
            if (button == null)
            {
                return;
            }

            SetCentered(button, ActionButtonSize, new Vector2(x, ActionButtonY));

            RectTransform glow = button.Find("SelectionGlow") as RectTransform;
            if (glow != null)
            {
                glow.sizeDelta = ActionButtonSize + new Vector2(12f, 8f);
                glow.anchoredPosition = Vector2.zero;
            }

            TextMeshProUGUI label = FindDirectText(button, "Label");
            if (label != null)
            {
                SetCentered(
                    label.rectTransform,
                    new Vector2(ActionButtonSize.x - ActionLabelHorizontalPadding * 2f, 50f),
                    Vector2.zero);
                label.alignment = TextAlignmentOptions.Center;
                ApplyFixedFont(label, ActionLabelFontSize, false);
            }
        }

        private static TextMeshProUGUI FindDirectText(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void ApplyFixedFont(TMP_Text text, float size, bool allowWrap)
        {
            text.enableAutoSizing = false;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            text.fontSize = size;
            text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        public void Submit()
        {
            if (selectedRating <= 0 || confirmationRoutine != null)
            {
                return;
            }

            PlayerPrefs.SetInt(RatingPrefsKey, Mathf.Clamp(selectedRating, 1, 5));
            PlayerPrefs.Save();
            confirmationRoutine = StartCoroutine(PlayConfirmation());
        }

        public void Close()
        {
            if (submitReadyRoutine != null)
            {
                StopCoroutine(submitReadyRoutine);
                submitReadyRoutine = null;
            }

            confirmationRoutine = null;
            (popupRoot != null ? popupRoot : gameObject).SetActive(false);
        }

        public void PreviewRating(int rating)
        {
            previewRating = Mathf.Clamp(rating, 1, 5);
            RefreshStars(false);
        }

        public void ClearPreviewRating()
        {
            previewRating = 0;
            RefreshStars(false);
        }

        private void SetRating(int rating)
        {
            selectedRating = Mathf.Clamp(rating, 1, 5);
            previewRating = 0;
            RefreshStars(false);
            UpdateSubmitState(true);
        }

        private void RefreshStars(bool immediate)
        {
            int displayedRating = previewRating > 0 ? previewRating : selectedRating;
            for (int i = 0; i < starImages.Length; i++)
            {
                Image image = starImages[i];
                if (image == null)
                {
                    continue;
                }

                bool active = i < displayedRating;
                image.sprite = active ? filledStarSprite : emptyStarSprite;
                image.color = Color.white;

                if (starInteractions != null
                    && i < starInteractions.Length
                    && starInteractions[i] != null)
                {
                    starInteractions[i].SetSelected(active, immediate);
                }
            }
        }

        private void UpdateSubmitState(bool playReadyEffect)
        {
            bool ready = selectedRating > 0;
            if (submitButton != null)
            {
                // Keep the approved button artwork fully opaque. Submit() still
                // guards against sending before a rating has been selected.
                submitButton.interactable = true;
            }

            if (submitGlow != null)
            {
                submitGlow.alpha = ready ? 0.2f : 0f;
            }

            if (playReadyEffect && ready)
            {
                if (submitReadyRoutine != null)
                {
                    StopCoroutine(submitReadyRoutine);
                }

                submitReadyRoutine = StartCoroutine(PlaySubmitReadyEffect());
            }
        }

        private IEnumerator PlaySubmitReadyEffect()
        {
            float elapsed = 0f;
            const float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                if (submitGlow != null)
                {
                    submitGlow.alpha = Mathf.Lerp(0.2f, 0.48f, pulse);
                }

                if (submitShine != null)
                {
                    submitShine.alpha = pulse * 0.72f;
                }

                yield return null;
            }

            if (submitGlow != null)
            {
                submitGlow.alpha = 0.2f;
            }

            if (submitShine != null)
            {
                submitShine.alpha = 0f;
            }

            submitReadyRoutine = null;
        }

        private IEnumerator PlayConfirmation()
        {
            if (submitButton != null)
            {
                submitButton.interactable = false;
            }

            for (int i = 0; starInteractions != null && i < starInteractions.Length; i++)
            {
                RateUsStarInteraction interaction = starInteractions[i];
                if (interaction == null)
                {
                    continue;
                }

                interaction.SetInteractable(false);
                if (i < selectedRating)
                {
                    interaction.PlayConfirmationFlash(i * 0.035f);
                }
            }

            StartCoroutine(PlaySparkles());
            yield return new WaitForSecondsRealtime(0.5f);

            OpenGooglePlay();
            Close();
        }

        private IEnumerator PlaySparkles()
        {
            CacheSparklePositions();
            float elapsed = 0f;
            const float duration = 0.48f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Sin(t * Mathf.PI);
                float eased = 1f - Mathf.Pow(1f - t, 2f);

                for (int i = 0; sparkleRects != null && i < sparkleRects.Length; i++)
                {
                    RectTransform rect = sparkleRects[i];
                    if (rect == null)
                    {
                        continue;
                    }

                    Vector2 start = sparkleStartPositions[i];
                    float drift = i % 2 == 0 ? -12f : 12f;
                    rect.anchoredPosition =
                        start + new Vector2(drift * eased, Mathf.Lerp(0f, 54f, eased));
                    rect.localRotation = Quaternion.Euler(0f, 0f, t * (i % 2 == 0 ? 150f : -150f));

                    if (sparkleGroups != null
                        && i < sparkleGroups.Length
                        && sparkleGroups[i] != null)
                    {
                        sparkleGroups[i].alpha = alpha * 0.82f;
                    }
                }

                yield return null;
            }

            ResetSparkles();
        }

        private void OpenGooglePlay()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string packageId = Application.identifier;
            if (!string.IsNullOrWhiteSpace(packageId))
            {
                Application.OpenURL($"market://details?id={packageId}");
            }
#endif
        }

        private void CacheSparklePositions()
        {
            if (sparkleRects == null)
            {
                sparkleRects = new RectTransform[0];
            }

            if (sparkleStartPositions == null || sparkleStartPositions.Length != sparkleRects.Length)
            {
                sparkleStartPositions = new Vector2[sparkleRects.Length];
            }

            for (int i = 0; i < sparkleRects.Length; i++)
            {
                if (sparkleRects[i] != null)
                {
                    sparkleStartPositions[i] = sparkleRects[i].anchoredPosition;
                }
            }
        }

        private void ResetEffects()
        {
            if (submitGlow != null)
            {
                submitGlow.alpha = 0f;
            }

            if (submitShine != null)
            {
                submitShine.alpha = 0f;
            }

            ResetSparkles();

            for (int i = 0; starInteractions != null && i < starInteractions.Length; i++)
            {
                starInteractions[i]?.SetInteractable(true);
            }
        }

        private void ResetSparkles()
        {
            if (sparkleStartPositions == null)
            {
                return;
            }

            for (int i = 0; sparkleRects != null && i < sparkleRects.Length; i++)
            {
                if (sparkleRects[i] != null)
                {
                    sparkleRects[i].anchoredPosition = sparkleStartPositions[i];
                    sparkleRects[i].localRotation = Quaternion.identity;
                }

                if (sparkleGroups != null
                    && i < sparkleGroups.Length
                    && sparkleGroups[i] != null)
                {
                    sparkleGroups[i].alpha = 0f;
                }
            }
        }
    }
}
