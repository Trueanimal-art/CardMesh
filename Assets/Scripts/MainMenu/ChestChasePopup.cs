using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class ChestChasePopup : MonoBehaviour
    {
        private static readonly Color ActiveMilestone = new(0.78f, 1f, 0.62f, 1f);
        private static readonly Color InactiveMilestone = Color.white;

        private ChestChaseManager manager;
        private CanvasGroup canvasGroup;
        private TMP_Text timerText;
        private TMP_Text progressCountText;
        private Image progressFill;
        private TMP_Text rewardToastText;
        private Coroutine timerRoutine;
        private bool isShowing;
        private bool referencesCached;

        private readonly List<StageBinding> stages = new();

        private sealed class StageBinding
        {
            public Image CardFrame;
            public Image ActiveGlow;
            public Image Milestone;
            public Image MilestoneGlow;
            public TMP_Text CoinValue;
            public TMP_Text HintValue;
            public TMP_Text UndoValue;
        }

        public static ChestChasePopup GetOrCreate()
        {
            ChestChasePopup fallback = null;
            ChestChasePopup[] popups = FindObjectsByType<ChestChasePopup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (ChestChasePopup popup in popups)
            {
                fallback ??= popup;
                if (popup.ResolveWindowRoot() != null)
                {
                    return popup;
                }
            }

            if (fallback == null)
            {
                throw new UnityException("ChestChasePopup must be authored in MainMenu.unity. Runtime UI creation is disabled.");
            }

            // Unity may briefly restore an outdated Temp/__Backupscenes copy while
            // scripts recompile. Keep that editor-only snapshot usable until the
            // authored MainMenu scene is reloaded; no UI is created at runtime.
            return fallback;
        }

        public void Show(ChestChaseManager targetManager)
        {
            MainMenuPopupExclusivity.OpenExclusive(this);
            manager = targetManager != null ? targetManager : ChestChaseManager.GetOrCreate(this);
            Subscribe();

            isShowing = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            CacheReferences();
            Refresh();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
            }

            timerRoutine = StartCoroutine(UpdateTimer());
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!isShowing)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDisable()
        {
            isShowing = false;
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private bool CacheReferences()
        {
            if (referencesCached)
            {
                return true;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            Transform window = ResolveWindowRoot();
            if (window == null)
            {
                return false;
            }

            Button closeButton = window.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            timerText = window.Find("RoyalContent/GoalBlock/Timer")?.GetComponent<TMP_Text>();
            progressCountText = window.Find("RoyalContent/ProgressBlock/ProgressBadge/ProgressCount")?.GetComponent<TMP_Text>();
            progressFill = window.Find("RoyalContent/ProgressBlock/ProgressBadge/ProgressFill")?.GetComponent<Image>();
            rewardToastText = window.Find("RoyalContent/RewardToast")?.GetComponent<TMP_Text>();

            stages.Clear();
            for (int i = 0; i < 5; i++)
            {
                Transform stage = window.Find($"RoyalContent/StageRow/Stage_{i + 1}");
                Transform milestone = window.Find($"RoyalContent/MilestoneTrack/Milestone_{i + 1}");
                stages.Add(new StageBinding
                {
                    CardFrame = stage?.GetComponent<Image>(),
                    ActiveGlow = stage?.Find("ActiveGlow")?.GetComponent<Image>(),
                    Milestone = milestone?.GetComponent<Image>(),
                    MilestoneGlow = milestone?.Find("ActiveGlow")?.GetComponent<Image>(),
                    CoinValue = stage?.Find("RewardRow_1/Value")?.GetComponent<TMP_Text>(),
                    HintValue = stage?.Find("RewardRow_2/Value")?.GetComponent<TMP_Text>(),
                    UndoValue = stage?.Find("RewardRow_3/Value")?.GetComponent<TMP_Text>()
                });
            }

            referencesCached = true;
            return true;
        }

        private Transform ResolveWindowRoot()
        {
            if (transform.name == "WindowRoot")
            {
                return transform;
            }

            Transform directChild = transform.Find("WindowRoot");
            if (directChild != null)
            {
                return directChild;
            }

            foreach (Transform descendant in GetComponentsInChildren<Transform>(true))
            {
                if (descendant.name == "WindowRoot")
                {
                    return descendant;
                }
            }

            return null;
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (manager == null)
            {
                return;
            }

            manager.StateChanged += Refresh;
            manager.RewardClaimed += ShowRewardToast;
        }

        private void Unsubscribe()
        {
            if (manager == null)
            {
                return;
            }

            manager.StateChanged -= Refresh;
            manager.RewardClaimed -= ShowRewardToast;
        }

        private void Refresh()
        {
            if (manager == null)
            {
                return;
            }

            int required = Mathf.Max(1, manager.KeysRequired);
            if (progressCountText != null)
            {
                progressCountText.text = $"{manager.CurrentKeys}/{required}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01((float)manager.CurrentKeys / required);
            }

            IReadOnlyList<ChestChaseRewardTier> tiers = manager.Tiers;
            for (int i = 0; i < stages.Count; i++)
            {
                ChestChaseRewardTier tier = tiers != null && i < tiers.Count ? tiers[i] : null;
                StageBinding stage = stages[i];
                SetText(stage.CoinValue, tier != null ? tier.Coins.ToString() : "-");
                SetText(stage.HintValue, tier != null ? tier.Hints.ToString() : "-");
                SetText(stage.UndoValue, tier != null ? tier.Undos.ToString() : "-");

                bool active = i == manager.CurrentStage;
                if (stage.ActiveGlow != null)
                {
                    stage.ActiveGlow.gameObject.SetActive(active);
                }

                if (stage.CardFrame != null)
                {
                    stage.CardFrame.color = active ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f);
                }

                if (stage.Milestone != null)
                {
                    stage.Milestone.color = active ? ActiveMilestone : InactiveMilestone;
                }

                if (stage.MilestoneGlow != null)
                {
                    stage.MilestoneGlow.gameObject.SetActive(active);
                }
            }
        }

        private IEnumerator UpdateTimer()
        {
            while (isShowing)
            {
                if (timerText != null && manager != null)
                {
                    TimeSpan remaining = manager.TimeRemaining;
                    timerText.text = remaining.TotalHours >= 1d
                        ? $"{Mathf.FloorToInt((float)remaining.TotalHours)}ч {remaining.Minutes:00}м"
                        : $"{remaining.Minutes:00}:{remaining.Seconds:00}";
                }

                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private void ShowRewardToast(ChestChaseRewardTier reward)
        {
            if (rewardToastText == null || reward == null)
            {
                return;
            }

            rewardToastText.text = $"Награда получена: {reward.Coins} монет";
            rewardToastText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideRewardToast));
            Invoke(nameof(HideRewardToast), 2.5f);
        }

        private void HideRewardToast()
        {
            if (rewardToastText != null)
            {
                rewardToastText.gameObject.SetActive(false);
            }
        }

        public void Close()
        {
            Unsubscribe();
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
