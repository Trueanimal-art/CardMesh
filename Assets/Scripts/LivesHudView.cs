using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [DisallowMultipleComponent]
    public sealed class LivesHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image heartIcon;
        [SerializeField] private Image backplate;
        [SerializeField] private Button addLifeButton;

        private LivesManager livesManager;
        private float nextTimerRefreshAt;
        private Coroutine heartFeedbackRoutine;
        private int lastRenderedLives = -1;

        public event Action AddLifeRequested;

        private void Awake()
        {
            ResolveReferences();
            NormalizeVisuals();
        }

        private void OnEnable()
        {
            ResolveReferences();
            NormalizeVisuals();

            if (livesManager == null)
            {
                LivesManager sceneLivesManager = UnityEngine.Object.FindFirstObjectByType<LivesManager>();
                if (sceneLivesManager != null)
                {
                    Configure(sceneLivesManager, countText, heartIcon);
                }
            }
            else
            {
                RefreshDisplay();
            }
        }

        private void Update()
        {
            if (livesManager == null || livesManager.CurrentLives >= LivesManager.MaxLives)
            {
                return;
            }

            if (Time.unscaledTime < nextTimerRefreshAt)
            {
                return;
            }

            nextTimerRefreshAt = Time.unscaledTime + 1f;
            RefreshDisplay();
        }

        public void Configure(
            LivesManager targetLivesManager,
            TMP_Text targetCountText,
            Image targetHeartIcon,
            TMP_Text targetStatusText = null,
            Button targetAddLifeButton = null,
            Image targetBackplate = null)
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= HandleLivesChanged;
            }

            if (addLifeButton != null)
            {
                addLifeButton.onClick.RemoveListener(HandleAddLifeClicked);
            }

            livesManager = targetLivesManager;
            countText = targetCountText;
            heartIcon = targetHeartIcon;
            statusText = targetStatusText != null ? targetStatusText : statusText;
            addLifeButton = targetAddLifeButton != null ? targetAddLifeButton : addLifeButton;
            backplate = targetBackplate != null ? targetBackplate : backplate;

            ResolveReferences();
            NormalizeVisuals();
            gameObject.SetActive(livesManager != null);

            if (addLifeButton != null)
            {
                addLifeButton.onClick.RemoveListener(HandleAddLifeClicked);
                addLifeButton.onClick.AddListener(HandleAddLifeClicked);
            }

            if (livesManager != null)
            {
                livesManager.OnLivesChanged += HandleLivesChanged;
                livesManager.RecalculateLivesByTime();
                RefreshDisplay();
            }
        }

        private void OnDestroy()
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= HandleLivesChanged;
            }

            if (addLifeButton != null)
            {
                addLifeButton.onClick.RemoveListener(HandleAddLifeClicked);
            }
        }

        private void OnDisable()
        {
            if (heartFeedbackRoutine != null)
            {
                StopCoroutine(heartFeedbackRoutine);
                heartFeedbackRoutine = null;
            }

            if (heartIcon != null)
            {
                heartIcon.rectTransform.localScale = Vector3.one;
            }
        }

        private void HandleLivesChanged(int currentLives, int maxLives)
        {
            RefreshDisplay(currentLives, maxLives);
        }

        private void RefreshDisplay()
        {
            if (livesManager == null)
            {
                return;
            }

            livesManager.RecalculateLivesByTime();
            RefreshDisplay(livesManager.CurrentLives, LivesManager.MaxLives);
        }

        private void RefreshDisplay(int currentLives, int maxLives)
        {
            int safeMaxLives = Mathf.Max(0, maxLives);
            int safeCurrentLives = Mathf.Clamp(currentLives, 0, safeMaxLives);
            bool isFull = safeMaxLives > 0 && safeCurrentLives >= safeMaxLives;
            if (lastRenderedLives >= 0 && safeCurrentLives != lastRenderedLives && heartIcon != null && isActiveAndEnabled)
            {
                if (heartFeedbackRoutine != null)
                {
                    StopCoroutine(heartFeedbackRoutine);
                }

                heartFeedbackRoutine = StartCoroutine(PlayHeartFeedback(safeCurrentLives > lastRenderedLives));
            }
            lastRenderedLives = safeCurrentLives;

            if (countText != null)
            {
                ApplyCountTextStyle(countText);
                countText.SetText(safeCurrentLives.ToString());
                countText.ForceMeshUpdate(true, true);
            }

            if (statusText != null)
            {
                ApplyStatusTextStyle(statusText);
                statusText.SetText(isFull ? "ЗАПОЛНЕНО" : FormatTimeToNextLife());
                statusText.ForceMeshUpdate(true, true);
            }

            if (addLifeButton != null)
            {
                addLifeButton.gameObject.SetActive(!isFull);
                addLifeButton.interactable = !isFull;
            }

            if (heartIcon != null)
            {
                heartIcon.color = safeCurrentLives > 0
                    ? Color.white
                    : new Color(0.64f, 0.46f, 0.44f, 1f);
            }

            if (backplate != null)
            {
                backplate.color = new Color(1f, 1f, 1f, 0.92f);
            }
        }

        private string FormatTimeToNextLife()
        {
            TimeSpan remaining = livesManager != null
                ? livesManager.GetTimeToNextLife()
                : TimeSpan.FromSeconds(LivesManager.RestoreIntervalSeconds);
            int totalMinutes = Mathf.Max(0, Mathf.FloorToInt((float)remaining.TotalMinutes));
            int seconds = Mathf.Clamp(remaining.Seconds, 0, 59);
            return $"{totalMinutes:00}м {seconds:00}с";
        }

        private IEnumerator PlayHeartFeedback(bool restored)
        {
            const float duration = 0.24f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                float scale = restored ? 1f + (wave * 0.08f) : 1f - (wave * 0.14f);
                heartIcon.rectTransform.localScale = Vector3.one * scale;
                heartIcon.color = Color.Lerp(
                    Color.white,
                    restored ? new Color(1f, 0.9f, 0.48f, 1f) : new Color(0.86f, 0.62f, 0.62f, 1f),
                    wave);
                yield return null;
            }

            heartIcon.rectTransform.localScale = Vector3.one;
            heartIcon.color = Color.white;
            heartFeedbackRoutine = null;
        }

        private void HandleAddLifeClicked()
        {
            AddLifeRequested?.Invoke();
        }

        private void ResolveReferences()
        {
            if (countText == null)
            {
                countText = FindText("LivesCount") ?? FindText("Value");
            }

            if (heartIcon == null)
            {
                Transform heart = transform.Find("HeartIcon");
                if (heart != null)
                {
                    heartIcon = heart.GetComponent<Image>();
                }
            }

            statusText ??= FindText("LivesStatus");

            if (backplate == null)
            {
                Transform panel = transform.Find("LivesBackplate");
                if (panel != null)
                {
                    backplate = panel.GetComponent<Image>();
                }
            }

            if (addLifeButton == null)
            {
                Transform addButton = transform.Find("AddLifeButton");
                if (addButton != null)
                {
                    addLifeButton = addButton.GetComponent<Button>();
                }
            }
        }

        private void NormalizeVisuals()
        {
            if (countText != null)
            {
                TrimExtraTextComponents(countText.gameObject, countText);
                ApplyCountTextStyle(countText);
            }

            if (statusText != null)
            {
                TrimExtraTextComponents(statusText.gameObject, statusText);
                ApplyStatusTextStyle(statusText);
            }

            if (heartIcon != null)
            {
                heartIcon.raycastTarget = false;
            }
        }

        private TMP_Text FindText(string targetName)
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.name == targetName)
                {
                    return text;
                }
            }

            return null;
        }

        private static void ApplyCountTextStyle(TMP_Text text)
        {
            text.gameObject.SetActive(true);
            text.enabled = true;
            text.canvasRenderer.SetAlpha(1f);
            text.enableAutoSizing = false;
            text.fontSize = 36f;
            text.fontSizeMin = 36f;
            text.fontSizeMax = 36f;
            text.fontStyle = FontStyles.Bold;
            text.fontWeight = FontWeight.Bold;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.margin = Vector4.zero;
            text.extraPadding = false;
            text.raycastTarget = false;
            text.maxVisibleCharacters = int.MaxValue;

            if (text.font != null && text.font.material != null)
            {
                text.fontSharedMaterial = text.font.material;
            }

            RemoveUiEffects(text.gameObject);
        }

        private static void ApplyStatusTextStyle(TMP_Text text)
        {
            text.gameObject.SetActive(true);
            text.enabled = true;
            text.canvasRenderer.SetAlpha(1f);
            text.enableAutoSizing = true;
            text.fontSize = 19f;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 19f;
            text.fontStyle = FontStyles.Bold;
            text.fontWeight = FontWeight.Bold;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.96f, 0.72f, 0.22f, 1f);
            text.margin = Vector4.zero;
            text.extraPadding = false;
            text.raycastTarget = false;
            text.maxVisibleCharacters = int.MaxValue;

            if (text.font != null && text.font.material != null)
            {
                text.fontSharedMaterial = text.font.material;
            }

            RemoveUiEffects(text.gameObject);
        }

        private static void TrimExtraTextComponents(GameObject target, TMP_Text keep)
        {
            foreach (TMP_Text text in target.GetComponents<TMP_Text>())
            {
                if (text == keep)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(text);
                }
                else
                {
                    DestroyImmediate(text);
                }
            }
        }

        private static void RemoveUiEffects(GameObject target)
        {
            foreach (Outline outline in target.GetComponents<Outline>())
            {
                if (Application.isPlaying)
                {
                    Destroy(outline);
                }
                else
                {
                    DestroyImmediate(outline);
                }
            }

            foreach (Shadow shadow in target.GetComponents<Shadow>())
            {
                if (shadow is Outline)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(shadow);
                }
                else
                {
                    DestroyImmediate(shadow);
                }
            }
        }
    }
}
