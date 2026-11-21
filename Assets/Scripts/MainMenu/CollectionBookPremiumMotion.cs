using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    /// <summary>
    /// Calm runtime motion for scene-authored Collection Book visuals.
    /// This component never creates or positions layout objects.
    /// </summary>
    public sealed class CollectionBookPremiumMotion : MonoBehaviour
    {
        private readonly List<RectTransform> cardRects = new();
        private readonly List<CanvasGroup> cardGroups = new();
        private readonly List<Image> rewardIcons = new();
        private readonly List<Image> activeGlows = new();

        private Image progressFill;
        private RectTransform progressMarker;
        private Vector3 markerBaseScale = Vector3.one;
        private Coroutine revealRoutine;
        private Coroutine progressRoutine;
        private Coroutine rewardFlashRoutine;
        private int activeRewardIndex = -1;
        private float pulseTime;

        private void OnEnable()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            revealRoutine = null;
            progressRoutine = null;
            rewardFlashRoutine = null;
            ResetVisuals();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            pulseTime += Time.unscaledDeltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(pulseTime * Mathf.PI * 2f / 2.6f);

            if (progressMarker != null)
            {
                progressMarker.localScale = markerBaseScale * Mathf.Lerp(1f, 1.045f, wave);
            }

            if (activeRewardIndex >= 0 && activeRewardIndex < activeGlows.Count)
            {
                Image glow = activeGlows[activeRewardIndex];
                if (glow != null && glow.gameObject.activeSelf)
                {
                    Color color = glow.color;
                    color.a = Mathf.Lerp(0.12f, 0.2f, wave);
                    glow.color = color;
                }
            }
        }

        public void PlayOpenSequence()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            ResolveReferences();
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                ResetCards();
            }

            revealRoutine = StartCoroutine(RevealCards());
        }

        public void AnimateProgress(float targetFill, int claimedRewardIndex)
        {
            if (progressFill == null)
            {
                ResolveReferences();
            }

            if (progressFill == null)
            {
                return;
            }

            if (!Application.isPlaying || targetFill <= progressFill.fillAmount)
            {
                progressFill.fillAmount = targetFill;
            }
            else
            {
                if (progressRoutine != null)
                {
                    StopCoroutine(progressRoutine);
                }

                progressRoutine = StartCoroutine(FillProgress(targetFill));
            }

            if (claimedRewardIndex >= 0 && claimedRewardIndex < rewardIcons.Count)
            {
                if (rewardFlashRoutine != null)
                {
                    StopCoroutine(rewardFlashRoutine);
                }

                rewardFlashRoutine = StartCoroutine(FlashReward(claimedRewardIndex));
            }
        }

        public void SetActiveReward(int index)
        {
            activeRewardIndex = index >= 0 && index < activeGlows.Count ? index : -1;
            for (int i = 0; i < activeGlows.Count; i++)
            {
                Image glow = activeGlows[i];
                if (glow == null)
                {
                    continue;
                }

                bool active = i == activeRewardIndex;
                glow.gameObject.SetActive(active);
                if (active)
                {
                    Color color = glow.color;
                    color.a = 0.16f;
                    glow.color = color;
                }
            }
        }

        private void ResolveReferences()
        {
            cardRects.Clear();
            cardGroups.Clear();
            rewardIcons.Clear();
            activeGlows.Clear();

            progressFill = transform.Find("Panel/ProgressBlock/ProgressTrack/ProgressFill")?.GetComponent<Image>();
            progressMarker = transform.Find("Panel/RewardStages/CurrentProgressMarker") as RectTransform;
            if (progressMarker != null)
            {
                markerBaseScale = progressMarker.localScale;
            }

            Transform root = transform.Find("Panel/RewardStages");
            if (root == null)
            {
                return;
            }

            for (int index = 0; index < 6; index++)
            {
                Transform card = root.Find($"RewardStage_{index + 1}");
                cardRects.Add(card as RectTransform);
                cardGroups.Add(card?.GetComponent<CanvasGroup>());
                rewardIcons.Add(card?.Find("RewardIcon")?.GetComponent<Image>());
                activeGlows.Add(card?.Find("ActiveGlow")?.GetComponent<Image>());
            }
        }

        private IEnumerator RevealCards()
        {
            for (int index = 0; index < cardRects.Count; index++)
            {
                RectTransform card = cardRects[index];
                CanvasGroup group = cardGroups[index];
                if (card != null)
                {
                    card.localScale = Vector3.one * 0.975f;
                }

                if (group != null)
                {
                    group.alpha = 0f;
                }
            }

            // Overlapping stagger: the whole grid is visible ~0.4s after opening
            // (the old one-card-at-a-time reveal took ~1s and was easy to
            // capture half-done, which read as a left-aligned two-tile row).
            const float duration = 0.16f;
            const float stagger = 0.045f;
            float total = duration + stagger * Mathf.Max(0, cardRects.Count - 1);
            float elapsed = 0f;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int index = 0; index < cardRects.Count; index++)
                {
                    float t = Mathf.Clamp01((elapsed - index * stagger) / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    CanvasGroup group = cardGroups[index];
                    RectTransform card = cardRects[index];
                    if (group != null)
                    {
                        group.alpha = eased;
                    }

                    if (card != null)
                    {
                        card.localScale = Vector3.LerpUnclamped(Vector3.one * 0.975f, Vector3.one, eased);
                    }
                }

                yield return null;
            }

            ResetCards();
            revealRoutine = null;
        }

        private IEnumerator FillProgress(float target)
        {
            float start = progressFill.fillAmount;
            const float duration = 0.38f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                progressFill.fillAmount = Mathf.Lerp(start, target, eased);
                yield return null;
            }

            progressFill.fillAmount = target;
            progressRoutine = null;
        }

        private IEnumerator FlashReward(int index)
        {
            Image icon = rewardIcons[index];
            if (icon == null)
            {
                yield break;
            }

            RectTransform rect = icon.rectTransform;
            Vector3 baseScale = rect.localScale;
            Color baseColor = icon.color;
            const float duration = 0.42f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                rect.localScale = baseScale * Mathf.Lerp(1f, 1.08f, wave);
                icon.color = Color.Lerp(baseColor, new Color(1f, 0.96f, 0.62f, baseColor.a), wave * 0.7f);
                yield return null;
            }

            rect.localScale = baseScale;
            icon.color = baseColor;
            rewardFlashRoutine = null;
        }

        private void ResetVisuals()
        {
            if (progressMarker != null)
            {
                progressMarker.localScale = markerBaseScale;
            }

            ResetCards();
        }

        private void ResetCards()
        {
            for (int index = 0; index < cardRects.Count; index++)
            {
                if (cardRects[index] != null)
                {
                    cardRects[index].localScale = Vector3.one;
                }

                if (cardGroups[index] != null)
                {
                    cardGroups[index].alpha = 1f;
                }
            }
        }
    }
}
