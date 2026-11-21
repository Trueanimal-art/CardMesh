using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class DailyRewardsPopup : MonoBehaviour
    {
        private static readonly Color TextDark = new(0.13f, 0.07f, 0.02f, 1f);
        private static readonly Color TextCream = new(1f, 0.92f, 0.7f, 1f);
        private static readonly Color TextGold = new(1f, 0.78f, 0.2f, 1f);
        private static readonly Color TextGreen = new(0.58f, 1f, 0.32f, 1f);
        private static readonly Color PanelGreen = new(0.045f, 0.28f, 0.135f, 0.96f);
        private static readonly Color DeepPanelGreen = new(0.032f, 0.22f, 0.105f, 0.98f);
        private static readonly Vector2 WindowSize = new(960f, 1540f);
        private static readonly Vector2 WindowAnchorMin = new(0.074f, 0.088f);
        private static readonly Vector2 WindowAnchorMax = new(0.926f, 0.912f);
        private static readonly Vector2 HeaderSize = new(840f, 150f);
        private static readonly Vector2 HeaderPosition = new(0f, 555f);
        private static readonly Vector2 ContentSize = new(840f, 1370f);
        private static readonly Vector2 ContentPosition = new(0f, -10f);
        private static readonly Vector2 RewardsColumnSize = new(840f, 760f);
        private static readonly Vector2 RewardsColumnPosition = new(0f, 170f);
        private static readonly Vector2 RewardDetailsPanelSize = new(790f, 350f);
        private static readonly Vector2 RewardDetailsPanelPosition = new(0f, -415f);
        private static readonly Vector2 RewardRowSize = new(840f, 280f);
        // Four cards at 195px plus three 20px gaps fit the 840px row exactly.
        // Keeping one shared size prevents Day 7 from drifting into a bespoke card.
        private static readonly Vector2 StandardRewardCardSize = new(195f, 250f);
        // The source icon contains transparent breathing room. A larger rect lets the
        // gift itself fill the frame's top medallion instead of looking like a second,
        // undersized medallion nested inside it.
        private static readonly Vector2 TopBadgeSize = new(118f, 118f);
        private const float TopRewardRowY = 125f;
        private const float BottomRewardRowY = -165f;
        private const float TopRowSpacing = 20f;
        private const float BottomRowSpacing = 48f;
        private const float CardStatusY = -82f;
        private const float TopBadgeY = -168f;
        private const float PopupWidth = 1020f;
        private const int EditorPreviewDay = 5;
        private const string AuthoredAssetFolder = "Assets/Resources/UI/DailyRewards";
        private const string AuthoredResourceFolder = "UI/DailyRewards/";

        [Header("Runtime bindings")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cardsRoot;
        [SerializeField] private TMP_Text currentTitleText;
        [SerializeField] private TMP_Text currentRewardText;
        [SerializeField] private TMP_Text currentDescriptionText;
        [SerializeField] private TMP_Text seriesText;
        [SerializeField] private TMP_Text claimButtonText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private Image currentRewardIcon;

        [Header("Shared prefab references")]
        [SerializeField] private PremiumEventPrefabReferences sharedPrefabs = new();

        private DailyRewardsManager manager;
        private Coroutine openRoutine;
        private bool hasShownRuntime;
        private int selectedDay = 1;

        private enum RewardViewState
        {
            Available,
            Claimed,
            Locked
        }

        public static DailyRewardsPopup SceneInstance =>
            FindFirstObjectByType<DailyRewardsPopup>(FindObjectsInactive.Include);

        private void Reset()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
            }
#endif
            BindSceneReferences();
        }

        private void Awake()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();

            if (Application.isPlaying && !hasShownRuntime)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
            }
#endif

            if (Application.isPlaying)
            {
                BindSceneReferences();
                ApplyFontsToAllTexts();
                WireButtons();
                Refresh();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
                BindSceneReferences();
            }
        }
#endif

        private void OnDestroy()
        {
            UnwireButtons();
            if (manager != null)
            {
                manager.StateChanged -= Refresh;
                manager.RewardClaimed -= OnRewardClaimed;
            }
        }

        public void Show(DailyRewardsManager targetManager)
        {
            MainMenuPopupExclusivity.OpenExclusive(this);
            manager = targetManager != null ? targetManager : DailyRewardsManager.GetOrCreate(this);
            manager.StateChanged -= Refresh;
            manager.StateChanged += Refresh;
            manager.RewardClaimed -= OnRewardClaimed;
            manager.RewardClaimed += OnRewardClaimed;
            selectedDay = manager.CurrentDay;

            hasShownRuntime = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
            Refresh();
            DisableExternalGlow(transform);
            StartOpenAnimation();
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        public void SelectDay(int day)
        {
            selectedDay = Mathf.Clamp(day, 1, 7);
            RefreshSelectedReward();
            DisableExternalGlow(transform);
        }

        private void Claim()
        {
            manager ??= DailyRewardsManager.GetOrCreate(this);
            manager.TryClaimToday();
            Refresh();
        }

        private void ShowInfo()
        {
            SetText(currentTitleText, "ПРАВИЛА");
            SetText(currentRewardText, "1 раз в день");
            SetText(currentDescriptionText, "Забирай ежедневную награду. Если пропустил день, серия не сбрасывается.");
        }

        private void Refresh()
        {
            if (!Application.isPlaying && manager == null)
            {
                RefreshEditorPreview();
                return;
            }

            manager ??= DailyRewardsManager.GetOrCreate(this);
            selectedDay = Mathf.Clamp(selectedDay <= 0 ? manager.CurrentDay : selectedDay, 1, 7);

            if (seriesText != null)
            {
                SetText(seriesText, $"День серии: {manager.CurrentDay} из 7");
            }

            RefreshCards();
            RefreshSelectedReward();
            DisableExternalGlow(transform);
            DisableLegacyHeaderCrown(transform);
        }

        private void RefreshEditorPreview()
        {
            selectedDay = EditorPreviewDay;
            SetText(seriesText, $"День серии: {EditorPreviewDay} из 7");
            RefreshCards();
            RefreshSelectedReward();
            DisableExternalGlow(transform);
            DisableLegacyHeaderCrown(transform);
        }

        private void RefreshCards()
        {
            if (cardsRoot == null)
            {
                return;
            }

            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindRewardCard(day);
                if (card == null)
                {
                    continue;
                }

                DailyReward reward = manager != null ? manager.GetReward(day) : GetPreviewReward(day);
                bool claimed = manager != null ? manager.IsDayClaimed(day) : day < EditorPreviewDay;
                bool current = manager != null ? manager.IsDayCurrent(day) : day == EditorPreviewDay;
                bool selected = selectedDay == day || (!Application.isPlaying && day == 3);
                bool locked = manager != null ? manager.IsDayLocked(day) : day > 3;

                Image image = card.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = current || selected
                            ? CleanRoyalPopupSprites.DailyCardAvailable
                            : CleanRoyalPopupSprites.DailyCard;
                    image.type = Image.Type.Sliced;
                    image.color = locked && !claimed ? new Color(0.62f, 0.68f, 0.64f, 0.9f) : Color.white;
                    image.material = null;
                }

                Image cardFill = FindRewardCardPart(card, "CardFill")?.GetComponent<Image>();
                if (cardFill != null)
                {
                    cardFill.gameObject.SetActive(false);
                }

                SetText(FindRewardCardPart(card, "RewardAmount", "Amount")?.GetComponent<TMP_Text>(), BuildAmount(reward));
                SetText(FindRewardCardPart(card, "RewardType", "Label")?.GetComponent<TMP_Text>(), BuildLabel(reward));
                TMP_Text statusText = FindRewardCardPart(card, "Status")?.GetComponent<TMP_Text>();
                SetText(statusText, claimed ? "ПОЛУЧЕНО" : current ? "ДОСТУПНО" : "ЗАБЛОКИРОВАНО");
                if (statusText != null)
                {
                    statusText.color = claimed || current ? TextGreen : TextGold;
                }

                Image statusBacking = FindRewardCardPart(card, "StatusArea", "StatusBacking")?.GetComponent<Image>();
                if (statusBacking != null)
                {
                    statusBacking.color = claimed || current
                        ? new Color(0.05f, 0.28f, 0.1f, 0.74f)
                        : new Color(0.16f, 0.11f, 0.03f, 0.62f);
                }

                Image rewardIcon = FindRewardCardPart(card, "RewardIcon")?.GetComponent<Image>();
                if (rewardIcon != null)
                {
                    rewardIcon.sprite = GetRewardIcon(reward);
                    rewardIcon.preserveAspect = true;
                    rewardIcon.color = Color.white;
                }

                Transform glow = FindRewardCardPart(card, "ActiveGlow");
                if (glow != null)
                {
                    Image glowImage = glow.GetComponent<Image>();
                    if (glowImage != null)
                    {
                        glowImage.sprite = GetRewardCardVisualSprite(true);
                        glowImage.type = Image.Type.Sliced;
                        glowImage.material = null;
                        glowImage.color = current && !claimed
                            ? new Color(1f, 0.82f, 0.18f, 0.18f)
                            : new Color(1f, 0.82f, 0.18f, 0f);
                    }

                    glow.gameObject.SetActive(false);
                }

                Transform check = FindRewardCardPart(card, "Check");
                if (check != null)
                {
#if UNITY_EDITOR
                    StyleCheckIcon(check as RectTransform);
#endif
                    check.gameObject.SetActive(claimed);
                }
                NormalizeClaimCheck(card, claimed);
            }
        }

        private static void NormalizeClaimCheck(Transform card, bool visible)
        {
            if (card == null) return;

            RectTransform preferred = null;
            foreach (RectTransform candidate in card.GetComponentsInChildren<RectTransform>(true))
            {
                if (candidate.name != "Check") continue;
                bool insideStatus = candidate.parent != null && candidate.parent.name == "StatusArea";
                if (insideStatus && preferred == null) preferred = candidate;
                candidate.gameObject.SetActive(false);
            }

            preferred ??= FindRewardCardPart(card, "Check") as RectTransform;
            if (preferred == null) return;
            preferred.anchorMin = preferred.anchorMax = preferred.pivot = new Vector2(0.5f, 0.5f);
            // Keep the tick immediately before the centred status word without
            // touching either the glyphs or the left card rim.
            preferred.anchoredPosition = new Vector2(-51f, 0f);
            preferred.sizeDelta = new Vector2(23f, 18f);
            preferred.gameObject.SetActive(visible);
        }

        private void RefreshSelectedReward()
        {
            DailyReward reward = manager != null ? manager.GetReward(selectedDay) : GetPreviewReward(selectedDay <= 0 ? EditorPreviewDay : selectedDay);
            if (reward == null)
            {
                return;
            }

            bool isCurrentDay = selectedDay == (manager != null ? manager.CurrentDay : EditorPreviewDay);
            bool isClaimed = manager != null ? manager.IsDayClaimed(selectedDay) : selectedDay < EditorPreviewDay;
            bool isAvailable = manager != null ? manager.IsDayCurrent(selectedDay) : isCurrentDay && !isClaimed;
            bool isLocked = manager != null ? manager.IsDayLocked(selectedDay) : selectedDay > EditorPreviewDay;
            RewardViewState viewState = isClaimed ? RewardViewState.Claimed : isAvailable ? RewardViewState.Available : RewardViewState.Locked;

            SetText(currentTitleText, isClaimed ? "НАГРАДА ПОЛУЧЕНА" : isAvailable ? "НАГРАДА ДОСТУПНА" : $"НАГРАДА ДНЯ {selectedDay}");
            SetText(currentRewardText, reward.Title.ToUpperInvariant());
            SetText(currentDescriptionText, isClaimed ? "Эта награда уже добавлена в твою серию." : isLocked ? "Эта награда откроется в один из следующих дней серии." : reward.Description);
            SetText(claimButtonText, viewState switch
            {
                RewardViewState.Available => "ЗАБРАТЬ",
                RewardViewState.Claimed => "ПОЛУЧЕНО",
                _ => "ЗАБЛОКИРОВАНО"
            });
            StyleClaimButtonState(viewState);
            Image rewardIcon = currentRewardIcon != null ? currentRewardIcon.transform.Find("RewardIcon")?.GetComponent<Image>() : null;
            if (rewardIcon != null)
            {
                rewardIcon.sprite = GetRewardIcon(reward);
                rewardIcon.preserveAspect = true;
                rewardIcon.color = Color.white;
            }
        }

        private void StyleClaimButtonState(RewardViewState state)
        {
            bool canClaim = state == RewardViewState.Available;
            if (claimButton != null)
            {
                claimButton.interactable = canClaim;
                claimButton.transition = canClaim ? Selectable.Transition.ColorTint : Selectable.Transition.None;
            }

            RectTransform rect = claimButton != null ? claimButton.transform as RectTransform : null;
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(280f, 56f);

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.GameplayActionButton != null
                    ? PremiumUISprites.GameplayActionButton
                    : PremiumUISprites.CleanGoldButton;
                image.type = Image.Type.Simple;
                image.color = state switch
                {
                    RewardViewState.Available => Color.white,
                    RewardViewState.Claimed => new Color(0.82f, 0.86f, 0.72f, 0.68f),
                    _ => new Color(0.72f, 0.75f, 0.66f, 0.52f)
                };
                image.material = null;
            }

            RemoveButtonGlow(rect);
            ConfigureDropShadow(rect.gameObject, new Color(0f, 0.02f, 0.01f, 0.18f), new Vector2(0f, -2f));

            TMP_Text label = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.rectTransform.anchoredPosition = state == RewardViewState.Claimed ? new Vector2(8f, 0f) : Vector2.zero;
                label.rectTransform.sizeDelta = rect.sizeDelta - new Vector2(state == RewardViewState.Claimed ? 82f : 48f, 22f);
                label.color = state switch
                {
                    RewardViewState.Available => TextCream,
                    RewardViewState.Claimed => TextGreen,
                    _ => new Color(TextGold.r, TextGold.g, TextGold.b, 0.62f)
                };
                label.fontSizeMax = state == RewardViewState.Available ? 28f : 25f;
                label.fontSizeMin = 16f;
            }

            Transform check = rect.Find("Check");
            if (check != null)
            {
                RectTransform checkRect = check as RectTransform;
                if (checkRect != null)
                {
                    checkRect.anchoredPosition = new Vector2(state == RewardViewState.Claimed ? -34f : -96f, 0f);
                    checkRect.sizeDelta = new Vector2(28f, 22f);
                }
#if UNITY_EDITOR
                StyleCheckIcon(check as RectTransform);
#endif
                check.gameObject.SetActive(state == RewardViewState.Claimed);
            }
        }

        private void OnRewardClaimed(DailyReward reward)
        {
            if (panel == null)
            {
                return;
            }

            StartCoroutine(PlayClaimFlash());
        }

        private IEnumerator PlayClaimFlash()
        {
            Transform flash = transform.Find("WindowRoot/ClaimFlash")
                ?? transform.Find("Panel/ClaimFlash");
            if (flash == null)
            {
                yield break;
            }

            flash.gameObject.SetActive(true);
            CanvasGroup group = flash.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = flash.gameObject.AddComponent<CanvasGroup>();
            }

            float duration = 0.36f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = 1f - t;
                flash.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.16f, t);
                yield return null;
            }

            flash.gameObject.SetActive(false);
            group.alpha = 1f;
            flash.localScale = Vector3.one;
        }

        private void StartOpenAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(PlayOpenAnimation());
        }

        private IEnumerator PlayOpenAnimation()
        {
            float duration = 0.18f;
            float elapsed = 0f;
            Vector3 from = Vector3.one * 0.97f;
            Vector3 to = Vector3.one;
            if (panel != null)
            {
                panel.localScale = from;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }

                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(from, to, eased);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = to;
            }

            openRoutine = null;
        }

        private void BindSceneReferences()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panel = (transform.Find("WindowRoot") ?? transform.Find("Panel")) as RectTransform;
            cardsRoot = (transform.Find("WindowRoot/Content/RewardsColumn") ?? transform.Find("Panel/RewardGrid")) as RectTransform;
            currentTitleText = FindTMP("WindowRoot/Content/RewardDetailsPanel/Title") ?? FindTMP("Panel/CurrentRewardPanel/Title");
            currentRewardText = FindTMP("WindowRoot/Content/RewardDetailsPanel/RewardName") ?? FindTMP("Panel/CurrentRewardPanel/RewardTitle");
            currentDescriptionText = FindTMP("WindowRoot/Content/RewardDetailsPanel/Description") ?? FindTMP("Panel/CurrentRewardPanel/Description");
            currentRewardIcon = (transform.Find("WindowRoot/Content/RewardDetailsPanel/RewardIconFrame") ?? transform.Find("Panel/CurrentRewardPanel/IconFrame"))?.GetComponent<Image>();
            seriesText = FindTMP("WindowRoot/Content/RewardsColumn/StreakText") ?? FindTMP("Panel/SeriesText");
            claimButton = (transform.Find("WindowRoot/Content/RewardDetailsPanel/Status") ?? transform.Find("Panel/CurrentRewardPanel/ClaimButton"))?.GetComponent<Button>();
            claimButtonText = FindTMP("WindowRoot/Content/RewardDetailsPanel/Status/Label") ?? FindTMP("Panel/CurrentRewardPanel/ClaimButton/Label");
            closeButton = (transform.Find("WindowRoot/CloseButton") ?? transform.Find("Panel/CloseButton"))?.GetComponent<Button>();
            infoButton = (transform.Find("WindowRoot/InfoButton") ?? transform.Find("Panel/InfoButton"))?.GetComponent<Button>();
        }

        private TMP_Text FindTMP(string path)
        {
            TMP_Text text = transform.Find(path)?.GetComponent<TMP_Text>();
            if (text != null)
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }

            return text;
        }

        private void WireButtons()
        {
            BindButton(closeButton, Hide);
            BindButton(claimButton, Claim);
            BindButton(infoButton, ShowInfo);

            if (cardsRoot == null)
            {
                return;
            }

            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindRewardCard(day);
                Button button = card != null ? card.GetComponent<Button>() : null;
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectDay(day));
            }
        }

        private static void BindButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void UnwireButtons()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (claimButton != null) claimButton.onClick.RemoveListener(Claim);
            if (infoButton != null) infoButton.onClick.RemoveListener(ShowInfo);
        }

        private void ApplyFontsToAllTexts()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                RuntimeCyrillicFontProvider.Apply(texts[i], texts[i].text);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
            RuntimeCyrillicFontProvider.Apply(text, value);
        }

        private static DailyReward GetPreviewReward(int day)
        {
            return day switch
            {
                1 => new DailyReward(1, DailyRewardType.Coins, 50, "50 монет", "Монеты пригодятся для будущих покупок."),
                2 => new DailyReward(2, DailyRewardType.Coins, 75, "75 монет", "Небольшой ежедневный запас монет."),
                3 => new DailyReward(3, DailyRewardType.Hint, 1, "1 подсказка", "Используй подсказку, если застрял на уровне!"),
                4 => new DailyReward(4, DailyRewardType.Coins, 100, "100 монет", "Больше монет для уверенного старта."),
                5 => new DailyReward(5, DailyRewardType.ExtraMoves, 5, "+5 ходов", "Дополнительные ходы помогут дожать сложный уровень."),
                6 => new DailyReward(6, DailyRewardType.Coins, 150, "150 монет", "Премиальная ежедневная награда."),
                _ => new DailyReward(7, DailyRewardType.Coins, 300, "300 монет + 1 подсказка", "Большой финал серии: монеты и подсказка.")
            };
        }

        private static Sprite GetRewardIcon(DailyReward reward)
        {
            if (reward == null)
            {
                return RoyalEventSprites.CoinIcon;
            }

            return reward.RewardType switch
            {
                DailyRewardType.Hint => CleanRoyalPopupSprites.GameplayHint != null ? CleanRoyalPopupSprites.GameplayHint : PremiumUISprites.HintBulbIcon,
                DailyRewardType.ExtraMoves => CleanRoyalPopupSprites.GameplayMoves != null ? CleanRoyalPopupSprites.GameplayMoves : PremiumUISprites.BoosterRocketIcon,
                _ => RoyalEventSprites.CoinIcon
            };
        }

        private static string BuildAmount(DailyReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.Day == 7)
            {
                return "300";
            }

            return reward.RewardType == DailyRewardType.ExtraMoves ? $"+{reward.Amount}" : reward.Amount.ToString();
        }

        private static string BuildLabel(DailyReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.Day == 7)
            {
                return "МОНЕТ\n+1 ПОДСКАЗКА";
            }

            return reward.RewardType switch
            {
                DailyRewardType.Coins => "МОНЕТ",
                DailyRewardType.Hint => "ПОДСКАЗКА",
                DailyRewardType.ExtraMoves => "ХОДОВ",
                _ => "НАГРАДА"
            };
        }

        private void ApplyAuthoringLayout()
        {
            EnsurePopupCanvas();
            EnsureModalHierarchy();
            BindSceneReferences();

            RectTransform root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            if (panel != null)
            {
                RectTransform badge = transform.Find("WindowRoot/TopBadge") as RectTransform;
                Button close = transform.Find("WindowRoot/CloseButton")?.GetComponent<Button>();
                LevelFrameChrome chrome = panel.GetComponent<LevelFrameChrome>() ?? panel.gameObject.AddComponent<LevelFrameChrome>();
                chrome.Configure(LevelFrameChrome.Preset.Tall, badge, close);
            }

            StyleHeaderRuntime();
            Transform info = transform.Find("WindowRoot/InfoButton");
            if (info != null)
            {
                info.gameObject.SetActive(false);
            }

            RectTransform content = transform.Find("WindowRoot/Content") as RectTransform;
            SetCenteredRect(content, ContentSize, ContentPosition);
            StyleRewardGridRuntime();
            StyleCurrentRewardPanelRuntime();

            RectTransform flash = transform.Find("WindowRoot/ClaimFlash") as RectTransform;
            if (flash != null)
            {
                SetCenteredRect(flash, new Vector2(300f, 300f), ContentPosition + RewardDetailsPanelPosition + new Vector2(0f, 120f));
            }

            StyleSeriesTextRuntime();
            ApplyDailyRewardsBadgeLayout();
        }

        private void ApplyDailyRewardsBadgeLayout()
        {
            RectTransform window = transform.Find("WindowRoot") as RectTransform;
            if (window != null)
            {
                LevelFrameChrome chrome = window.GetComponent<LevelFrameChrome>() ?? window.gameObject.AddComponent<LevelFrameChrome>();
                chrome.SetPanelLayoutOverride(PopupWidth, LevelFrameChrome.GetMetrics(LevelFrameChrome.Preset.Tall).PanelPosition);
                chrome.SetBadgeLayoutOverride(TopBadgeSize, new Vector2(0f, TopBadgeY));
            }

            RectTransform badge = transform.Find("WindowRoot/TopBadge") as RectTransform;
            if (badge != null)
            {
                badge.anchorMin = new Vector2(0.5f, 1f);
                badge.anchorMax = new Vector2(0.5f, 1f);
                badge.pivot = new Vector2(0.5f, 0.5f);
                badge.sizeDelta = TopBadgeSize;
                badge.anchoredPosition = new Vector2(0f, TopBadgeY);
                badge.localScale = Vector3.one;
            }
        }

        private void EnsurePopupCanvas()
        {
            Canvas popupCanvas = GetComponent<Canvas>();
            if (popupCanvas == null)
            {
                popupCanvas = gameObject.AddComponent<Canvas>();
            }

            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 500;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = true;

            Image rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = false;
                rootImage.raycastTarget = false;
            }
        }

        private void EnsureModalHierarchy()
        {
            RectTransform root = transform as RectTransform;
            if (root != null)
            {
                SetStretchRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            RectTransform overlay = EnsureChildRect(transform, "ModalOverlay");
            SetStretchRect(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsFirstSibling();
            Image overlayImage = EnsureImage(overlay);
            overlayImage.sprite = null;
            overlayImage.type = Image.Type.Simple;
            overlayImage.color = new Color(0f, 0.02f, 0.01f, 0.48f);
            overlayImage.raycastTarget = true;

            RectTransform windowRoot = GetDirectRect(transform, "WindowRoot")
                ?? GetDirectRect(transform, "Panel")
                ?? EnsureChildRect(transform, "WindowRoot");
            windowRoot.name = "WindowRoot";
            windowRoot.SetAsLastSibling();
            SetStretchRect(windowRoot, WindowAnchorMin, WindowAnchorMax, Vector2.zero, Vector2.zero);
            Image windowImage = windowRoot.GetComponent<Image>();
            if (windowImage != null)
            {
                windowImage.enabled = false;
                windowImage.raycastTarget = false;
            }

            RectTransform frame = EnsureChildRect(windowRoot, "Frame");
            LayoutPopupFrame(frame);
            frame.SetAsFirstSibling();
            Image frameImage = EnsureImage(frame);
            ApplyPopupPanelVisual(frameImage);
            frameImage.raycastTarget = false;

            RectTransform topBadge = MoveOrCreate(windowRoot, "TopBadge", windowRoot.Find("TopBadge") ?? windowRoot.Find("HeaderIcon"));
            topBadge.SetAsLastSibling();
            Image topBadgeImage = EnsureImage(topBadge);
            topBadgeImage.sprite = GetDailyRewardsTopBadgeSprite();
            topBadgeImage.type = Image.Type.Simple;
            topBadgeImage.color = Color.white;
            topBadgeImage.preserveAspect = true;
            topBadgeImage.raycastTarget = false;

            RectTransform header = EnsureChildRect(windowRoot, "Header");
            MoveOrCreate(header, "Title", windowRoot.Find("Title"));
            MoveOrCreate(header, "Subtitle", windowRoot.Find("Subtitle"));

            RectTransform content = EnsureChildRect(windowRoot, "Content");
            RectTransform rewardsColumn = MoveOrCreate(content, "RewardsColumn", windowRoot.Find("RewardsColumn") ?? windowRoot.Find("RewardGrid"));
            RectTransform topRow = MoveOrCreate(rewardsColumn, "RewardsRowTop", rewardsColumn.Find("RewardsRowTop") ?? rewardsColumn.Find("TopRewardRow"));
            RectTransform bottomRow = MoveOrCreate(rewardsColumn, "RewardsRowBottom", rewardsColumn.Find("RewardsRowBottom") ?? rewardsColumn.Find("BottomRewardRow"));
            RectTransform streakText = MoveOrCreate(rewardsColumn, "StreakText", windowRoot.Find("StreakText") ?? windowRoot.Find("SeriesText"));

            RectTransform details = MoveOrCreate(content, "RewardDetailsPanel", windowRoot.Find("RewardDetailsPanel") ?? windowRoot.Find("CurrentRewardPanel"));
            MoveOrCreate(details, "RewardIconFrame", details.Find("RewardIconFrame") ?? details.Find("IconFrame"));
            MoveOrCreate(details, "RewardName", details.Find("RewardName") ?? details.Find("RewardTitle"));
            MoveOrCreate(details, "Status", details.Find("Status") ?? details.Find("ClaimButton"));
            MoveOrCreate(details, "Footnote", details.Find("Footnote") ?? details.Find("Hint"));

            MoveOrCreate(windowRoot, "CloseButton", windowRoot.Find("CloseButton"));
            RectTransform info = MoveOrCreate(windowRoot, "InfoButton", windowRoot.Find("InfoButton"));
            if (info != null)
            {
                info.gameObject.SetActive(false);
            }

            RectTransform flash = MoveOrCreate(windowRoot, "ClaimFlash", windowRoot.Find("ClaimFlash"));
            if (flash != null)
            {
                flash.gameObject.SetActive(false);
            }

            EnsureRewardRowsInHierarchy(rewardsColumn);
            topRow.SetAsLastSibling();
            bottomRow.SetAsLastSibling();
            streakText.SetAsLastSibling();
            DisableLegacyFrameDecorations(windowRoot);
            Button closeButton = PopupShell.EnsureIconCloseButton(windowRoot, "CloseButton");
            LevelFrameChrome chrome = windowRoot.GetComponent<LevelFrameChrome>() ?? windowRoot.gameObject.AddComponent<LevelFrameChrome>();
            chrome.Configure(LevelFrameChrome.Preset.Tall, topBadge, closeButton);
        }

        private void StyleHeaderRuntime()
        {
            RectTransform header = transform.Find("WindowRoot/Header") as RectTransform;
            SetCenteredRect(header, HeaderSize, HeaderPosition);
            SetCenteredRect(transform.Find("WindowRoot/Header/Title") as RectTransform, new Vector2(840f, 64f), new Vector2(0f, -18f));
            SetCenteredRect(transform.Find("WindowRoot/Header/Subtitle") as RectTransform, new Vector2(840f, 58f), new Vector2(0f, -74f));

            TMP_Text titleLabel = transform.Find("WindowRoot/Header/Title")?.GetComponent<TMP_Text>();
            StyleText(titleLabel, 48f, 32f, TextGold, false, 0f);

            TMP_Text subtitleLabel = transform.Find("WindowRoot/Header/Subtitle")?.GetComponent<TMP_Text>();
            StyleText(subtitleLabel, 23f, 17f, TextCream, true, 0f);

            RectTransform topBadge = transform.Find("WindowRoot/TopBadge") as RectTransform;
            if (topBadge != null)
            {
                topBadge.anchorMin = new Vector2(0.5f, 1f);
                topBadge.anchorMax = new Vector2(0.5f, 1f);
                topBadge.pivot = new Vector2(0.5f, 0.5f);
                topBadge.sizeDelta = TopBadgeSize;
                topBadge.anchoredPosition = new Vector2(0f, TopBadgeY);
                topBadge.localScale = Vector3.one;
                Image icon = topBadge.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = GetDailyRewardsTopBadgeSprite();
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    icon.color = Color.white;
                }

                topBadge.SetAsLastSibling();
            }
        }

        private void StyleRewardGridRuntime()
        {
            if (cardsRoot == null)
            {
                return;
            }

            SetCenteredRect(cardsRoot, RewardsColumnSize, RewardsColumnPosition);

            RectTransform topRow = cardsRoot.Find("RewardsRowTop") as RectTransform;
            RectTransform bottomRow = cardsRoot.Find("RewardsRowBottom") as RectTransform;
            StyleRewardRow(topRow, TopRewardRowY, TopRowSpacing);
            StyleRewardRow(bottomRow, BottomRewardRowY, BottomRowSpacing);

            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindRewardCard(day);
                if (card == null)
                {
                    continue;
                }

                RectTransform cardRect = card as RectTransform;
                if (cardRect != null)
                {
                    SetCenteredRect(cardRect, GetRewardCardSize(day), card.parent == cardsRoot ? GetRewardCardPosition(day) : Vector2.zero);
                    LayoutElement cardLayout = cardRect.GetComponent<LayoutElement>() ?? cardRect.gameObject.AddComponent<LayoutElement>();
                    cardLayout.minWidth = cardRect.sizeDelta.x;
                    cardLayout.preferredWidth = cardRect.sizeDelta.x;
                    cardLayout.minHeight = cardRect.sizeDelta.y;
                    cardLayout.preferredHeight = cardRect.sizeDelta.y;
                }

                EnsureRewardCardHierarchy(card);

                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.sprite = GetRewardCardVisualSprite(false);
                    cardImage.type = Image.Type.Sliced;
                    cardImage.raycastTarget = true;
                    cardImage.material = null;
                }

                Button button = card.GetComponent<Button>();
                if (button != null)
                {
                    button.targetGraphic = cardImage;
                }

                StyleRewardCardChildren(card, day);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsRoot);
        }

        private static void StyleRewardRow(RectTransform row, float y, float spacing)
        {
            if (row == null)
            {
                return;
            }

            SetCenteredRect(row, RewardRowSize, new Vector2(0f, y));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
        }

        private static void StyleRewardCardChildren(Transform card, int day)
        {
            Vector2 cardSize = GetRewardCardSize(day);
            SetCenteredRect(FindRewardCardPart(card, "CardFill") as RectTransform, cardSize - new Vector2(22f, 22f), new Vector2(0f, -1f));
            SetCenteredRect(FindRewardCardPart(card, "ActiveGlow") as RectTransform, cardSize + new Vector2(18f, 22f), Vector2.zero);
            SetCenteredRect(FindRewardCardPart(card, "RewardIcon") as RectTransform, new Vector2(78f, 78f), GetRewardIconPosition(day));
            SetCenteredRect(FindRewardCardPart(card, "RewardAmount", "Amount") as RectTransform, new Vector2(cardSize.x - 34f, 46f), GetRewardAmountPosition(day));
            SetCenteredRect(FindRewardCardPart(card, "RewardType", "Label") as RectTransform, new Vector2(day == 7 ? cardSize.x - 20f : cardSize.x - 34f, day == 7 ? 38f : 34f), GetRewardLabelPosition(day));
            SetCenteredRect(FindRewardCardPart(card, "StatusArea", "StatusBacking") as RectTransform, new Vector2(cardSize.x - 34f, day == 7 ? 24f : 29f), GetRewardStatusPosition(day));
            SetCenteredRect(FindRewardCardPart(card, "Status") as RectTransform, new Vector2(cardSize.x - 34f, day == 7 ? 24f : 29f), Vector2.zero);
            SetCenteredRect(FindRewardCardPart(card, "Check") as RectTransform, new Vector2(23f, 18f), new Vector2(-51f, 0f));

            TMP_Text amountText = FindRewardCardPart(card, "RewardAmount", "Amount")?.GetComponent<TMP_Text>();
            StyleText(amountText, day == 7 ? 32f : 35f, 19f, TextCream, true, day == 7 ? -8f : 0f);

            TMP_Text labelText = FindRewardCardPart(card, "RewardType", "Label")?.GetComponent<TMP_Text>();
            StyleText(labelText, day == 7 ? 15f : 19f, day == 7 ? 11f : 13f, TextCream, day != 7, day == 7 ? -3f : -3f);

            TMP_Text statusText = FindRewardCardPart(card, "Status")?.GetComponent<TMP_Text>();
            StyleText(statusText, 15f, 11f, TextGreen, false, 0f);
            if (statusText != null)
            {
                statusText.enableAutoSizing = false;
                statusText.fontSize = 13f;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.margin = Vector4.zero;
            }

            Image statusBacking = FindRewardCardPart(card, "StatusArea", "StatusBacking")?.GetComponent<Image>();
            if (statusBacking != null)
            {
                statusBacking.sprite = PremiumUISprites.SecondaryButton;
                statusBacking.type = Image.Type.Sliced;
                statusBacking.raycastTarget = false;
                LayoutElement statusLayout = statusBacking.GetComponent<LayoutElement>() ?? statusBacking.gameObject.AddComponent<LayoutElement>();
                statusLayout.minHeight = 29f;
                statusLayout.preferredHeight = 29f;
            }

            Image rewardIcon = FindRewardCardPart(card, "RewardIcon")?.GetComponent<Image>();
            if (rewardIcon != null)
            {
                rewardIcon.preserveAspect = true;
                rewardIcon.raycastTarget = false;
            }

            Transform activeGlow = FindRewardCardPart(card, "ActiveGlow");
            if (activeGlow != null)
            {
                activeGlow.SetAsFirstSibling();
            }

            foreach (Image checkPart in FindRewardCardPart(card, "Check")?.GetComponentsInChildren<Image>(true) ?? System.Array.Empty<Image>())
            {
                checkPart.color = TextGreen;
                checkPart.raycastTarget = false;
            }
        }

        private void StyleCurrentRewardPanelRuntime()
        {
            RectTransform rect = transform.Find("WindowRoot/Content/RewardDetailsPanel") as RectTransform;
            if (rect == null)
            {
                return;
            }

            SetCenteredRect(rect, RewardDetailsPanelSize, RewardDetailsPanelPosition);
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                Sprite cleanPanel = CleanRoyalPopupSprites.DailyHero;
                image.sprite = cleanPanel != null ? cleanPanel : PremiumUISprites.CategorySlotEmerald != null ? PremiumUISprites.CategorySlotEmerald : PremiumUISprites.EmeraldPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = false;
                image.material = null;
            }

            ConfigureDropShadow(rect.gameObject, new Color(0f, 0.025f, 0.012f, 0.32f), new Vector2(0f, -3f));

            Outline outline = rect.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(1f, 0.72f, 0.18f, 0.5f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            DisableLegacyDetailsDecorations(rect);
            SetCenteredRect(rect.Find("Title") as RectTransform, new Vector2(620f, 38f), new Vector2(0f, 96f));
            SetCenteredRect(rect.Find("RewardIconFrame") as RectTransform, new Vector2(124f, 124f), new Vector2(-220f, -5f));
            SetCenteredRect(rect.Find("RewardName") as RectTransform, new Vector2(400f, 44f), new Vector2(100f, 32f));
            SetCenteredRect(rect.Find("Description") as RectTransform, new Vector2(400f, 54f), new Vector2(100f, -10f));
            SetCenteredRect(rect.Find("Status") as RectTransform, new Vector2(280f, 52f), new Vector2(100f, -64f));
            SetCenteredRect(rect.Find("Footnote") as RectTransform, new Vector2(400f, 24f), new Vector2(100f, -108f));

            TMP_Text title = rect.Find("Title")?.GetComponent<TMP_Text>();
            StyleText(title, 25f, 17f, TextGold, false, 0f);

            TMP_Text reward = rect.Find("RewardName")?.GetComponent<TMP_Text>();
            StyleText(reward, 32f, 19f, TextGold, true, 0f);

            TMP_Text description = rect.Find("Description")?.GetComponent<TMP_Text>();
            StyleText(description, 19f, 13f, TextCream, true, 12f);

            TMP_Text hint = rect.Find("Footnote")?.GetComponent<TMP_Text>();
            StyleText(hint, 13f, 10f, new Color(TextCream.r, TextCream.g, TextCream.b, 0.86f), true, 0f);

            Image iconFrame = rect.Find("RewardIconFrame")?.GetComponent<Image>();
            if (iconFrame != null)
            {
                iconFrame.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
                iconFrame.type = Image.Type.Sliced;
                iconFrame.color = Color.white;
                iconFrame.raycastTarget = false;
            }

            Image rewardIcon = rect.Find("RewardIconFrame/RewardIcon")?.GetComponent<Image>();
            if (rewardIcon != null)
            {
                rewardIcon.preserveAspect = true;
                rewardIcon.raycastTarget = false;
                SetCenteredRect(rewardIcon.rectTransform, new Vector2(92f, 92f), Vector2.zero);
            }

            Image divider = rect.Find("Divider")?.GetComponent<Image>();
            if (divider != null)
            {
                divider.color = new Color(1f, 0.78f, 0.2f, 0.34f);
                divider.raycastTarget = false;
            }
        }

        private void StyleSeriesTextRuntime()
        {
            RectTransform series = transform.Find("WindowRoot/Content/RewardsColumn/StreakText") as RectTransform;
            SetCenteredRect(series, new Vector2(650f, 34f), new Vector2(0f, -350f));

            TMP_Text label = series != null ? series.GetComponent<TMP_Text>() : null;
            StyleText(label, 22f, 15f, TextCream, false, 0f);
        }

        private static void StyleSharedButtonRuntime(RectTransform rect, string label, Vector2 size, Vector2 position, bool rightTop)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 anchor = rightTop ? Vector2.one : new Vector2(0f, 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = true;
            }

            TMP_Text text = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (text != null)
            {
                SetText(text, label);
                SetCenteredRect(text.rectTransform, size * 0.68f, Vector2.zero);
                StyleText(text, size.y * 0.42f, size.y * 0.26f, TextGold, false, 0f);
            }
        }

        private static RectTransform GetDirectRect(Transform parent, string name)
        {
            return parent != null ? parent.Find(name) as RectTransform : null;
        }

        private static RectTransform EnsureChildRect(Transform parent, string name)
        {
            RectTransform rect = GetDirectRect(parent, name);
            if (rect != null)
            {
                return rect;
            }

            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform MoveOrCreate(RectTransform parent, string newName, Transform candidate)
        {
            RectTransform rect = parent != null ? parent.Find(newName) as RectTransform : null;
            if (rect == null && candidate != null)
            {
                rect = candidate as RectTransform;
            }

            if (rect == null)
            {
                rect = EnsureChildRect(parent, newName);
            }
            else if (rect.parent != parent)
            {
                rect.SetParent(parent, false);
            }

            rect.name = newName;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image EnsureImage(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            return image;
        }

        private static void EnsureRewardRowsInHierarchy(RectTransform root)
        {
            RectTransform topRow = EnsureChildRect(root, "RewardsRowTop");
            RectTransform bottomRow = EnsureChildRect(root, "RewardsRowBottom");
            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindChildRecursive(root, $"Day{day}Card");
                RectTransform targetRow = day <= 4 ? topRow : bottomRow;
                if (card == null || targetRow == null || card.parent == targetRow)
                {
                    if (card != null)
                    {
                        EnsureRewardCardHierarchy(card);
                    }

                    continue;
                }

                card.SetParent(targetRow, false);
                EnsureRewardCardHierarchy(card);
            }
        }

        private static void EnsureRewardCardHierarchy(Transform card)
        {
            if (card is not RectTransform cardRect)
            {
                return;
            }

            RectTransform cardFill = MoveOrCreate(cardRect, "CardFill", cardRect.Find("CardFill"));
            Image cardFillImage = EnsureImage(cardFill);
            cardFillImage.sprite = PremiumUISprites.CategorySlotEmerald;
            cardFillImage.type = Image.Type.Simple;
            cardFillImage.color = new Color(0.88f, 1f, 0.94f, 1f);
            cardFillImage.raycastTarget = false;
            SetCenteredRect(cardFill, cardRect.sizeDelta - new Vector2(22f, 22f), new Vector2(0f, -1f));
            cardFill.SetAsFirstSibling();

            ConfigureDropShadow(cardRect.gameObject, new Color(0f, 0.02f, 0.01f, 0.28f), new Vector2(0f, -3f));

            MoveOrCreate(cardRect, "DayBadge", cardRect.Find("DayBadge") ?? cardRect.Find("Day"));
            RectTransform rewardIcon = MoveOrCreate(cardRect, "RewardIcon", cardRect.Find("RewardIcon"));
            Image rewardIconImage = rewardIcon.GetComponent<Image>();
            if (rewardIconImage == null)
            {
                rewardIconImage = rewardIcon.gameObject.AddComponent<Image>();
            }

            rewardIconImage.preserveAspect = true;
            rewardIconImage.raycastTarget = false;
            MoveOrCreate(cardRect, "RewardAmount", cardRect.Find("RewardAmount") ?? cardRect.Find("Amount"));
            MoveOrCreate(cardRect, "RewardType", cardRect.Find("RewardType") ?? cardRect.Find("Label"));
            RectTransform statusArea = MoveOrCreate(cardRect, "StatusArea", cardRect.Find("StatusArea") ?? cardRect.Find("StatusBacking"));
            Image statusImage = EnsureImage(statusArea);
            statusImage.sprite = PremiumUISprites.SecondaryButton;
            statusImage.type = Image.Type.Sliced;
            statusImage.raycastTarget = false;

            MoveOrCreate(statusArea, "Status", statusArea.Find("Status") ?? cardRect.Find("Status"));
            MoveOrCreate(statusArea, "Check", statusArea.Find("Check") ?? cardRect.Find("Check"));
        }

        private static void SetStretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Sprite GetDailyRewardsTopBadgeSprite()
        {
            Sprite sprite = Resources.Load<Sprite>("UI/Premium/icon_reward_royal_seal");
            return sprite != null ? sprite : PremiumUISprites.CategoryLabel;
        }

        private static void DisableLegacyFrameDecorations(Transform windowRoot)
        {
            SetActiveIfExists(windowRoot, "DividerStrip", false);
            SetActiveIfExists(windowRoot, "TopFrameJewel", false);
            SetActiveIfExists(windowRoot, "BottomFrameJewel", false);
        }

        private static void DisableLegacyDetailsDecorations(Transform details)
        {
            SetActiveIfExists(details, "InnerEmeraldPanel", false);
            SetActiveIfExists(details, "InnerGreenWash", false);
            SetActiveIfExists(details, "CornerTopLeft", false);
            SetActiveIfExists(details, "CornerTopRight", false);
            SetActiveIfExists(details, "CornerBottomLeft", false);
            SetActiveIfExists(details, "CornerBottomRight", false);
            SetActiveIfExists(details, "Divider", false);
        }

        private Transform FindRewardCard(int day)
        {
            return cardsRoot != null ? FindChildRecursive(cardsRoot, $"Day{Mathf.Clamp(day, 1, 7)}Card") : null;
        }

        private static Transform FindRewardCardPart(Transform card, string name, string legacyName = null)
        {
            if (card == null)
            {
                return null;
            }

            return card.Find(name)
                ?? (legacyName != null ? card.Find(legacyName) : null)
                ?? FindChildRecursive(card, name)
                ?? (legacyName != null ? FindChildRecursive(card, legacyName) : null);
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildRecursive(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
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
            rect.localScale = Vector3.one;
        }

        private static void StyleText(TMP_Text text, float maxSize, float minSize, Color color, bool wrap, float lineSpacing)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.textWrappingMode =
                wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.lineSpacing = lineSpacing;
            text.raycastTarget = false;
        }

#if UNITY_EDITOR
        public void MaterializeAuthoringLayout()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureAuthoringLayout();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
            RefreshEditorPreview();
        }

        [UnityEditor.MenuItem("Tools/CardMesh/Rebuild Daily Rewards Popup")]
        public static void RebuildMainMenuDailyRewardsPopup()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            DailyRewardsPopup popup = FindFirstObjectByType<DailyRewardsPopup>(FindObjectsInactive.Include);
            if (popup == null)
            {
                throw new UnityException("DailyRewardsPopup is missing from Assets/MainMenu.unity.");
            }

            popup.MaterializeAuthoringLayout();
            popup.gameObject.SetActive(false);
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI/MainMenu");
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(popup.gameObject, "Assets/Prefabs/UI/MainMenu/DailyRewardsPopup.prefab");
            UnityEditor.EditorUtility.SetDirty(popup.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        private void EnsureAuthoringLayout()
        {
            if (transform.Find("Panel") != null || transform.Find("WindowRoot") != null)
            {
                UpgradeAuthoringLayout();
                return;
            }

            sharedPrefabs.AssignDefaultsIfMissing();

            Image dim = GetComponent<Image>();
            if (dim != null)
            {
                dim.color = new Color(0f, 0.03f, 0.014f, 0.84f);
                dim.raycastTarget = true;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            panel = CreateRect(transform, "Panel", Anchor.Center, Anchor.Center, WindowSize, new Vector2(0f, 0f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            ApplyPopupPanelVisual(panelImage);
            AddShadow(panel.gameObject, new Color(1f, 0.68f, 0.14f, 0f), Vector2.zero);

            CreateHeaderIcon(panel);
            CreateText(panel, "Title", "ЕЖЕДНЕВНЫЕ НАГРАДЫ", 50f, TextGold, FontStyles.Bold, new Vector2(0f, 272f), new Vector2(1060f, 62f));
            CreateText(panel, "Subtitle", "Возвращайся каждый день и получай ценные награды", 25f, TextCream, FontStyles.Bold, new Vector2(0f, 218f), new Vector2(1020f, 38f));

            infoButton = CreateCircleButton(panel, "InfoButton", "i", new Vector2(-708f, 346f), new Vector2(58f, 58f), new Color(0.04f, 0.25f, 0.12f, 1f), TextGold);
            closeButton = CreateCircleButton(panel, "CloseButton", "X", new Vector2(710f, 350f), new Vector2(70f, 70f), new Color(0.87f, 0.56f, 0.08f, 1f), TextDark);

            cardsRoot = CreateRect(panel, "RewardGrid", Anchor.Center, Anchor.Center, RewardsColumnSize, RewardsColumnPosition);
            CreateRewardRows(cardsRoot);
            for (int i = 0; i < 7; i++)
            {
                CreateRewardCard(cardsRoot, i + 1);
            }

            RectTransform currentPanel = CreateRect(panel, "CurrentRewardPanel", Anchor.Center, Anchor.Center, RewardDetailsPanelSize, RewardDetailsPanelPosition);
            Image currentPanelImage = currentPanel.gameObject.AddComponent<Image>();
            currentPanelImage.sprite = PremiumUISprites.CategorySlot != null ? PremiumUISprites.CategorySlot : PremiumUISprites.EmeraldPanel;
            currentPanelImage.type = Image.Type.Sliced;
            currentPanelImage.color = Color.white;
            AddShadow(currentPanel.gameObject, new Color(1f, 0.7f, 0.12f, 0f), Vector2.zero);
            Outline currentOutline = currentPanel.gameObject.AddComponent<Outline>();
            currentOutline.effectColor = new Color(1f, 0.72f, 0.18f, 0.46f);
            currentOutline.effectDistance = new Vector2(1f, -1f);

            CreateCurrentPanelInset(currentPanel);
            CreateCurrentPanelWash(currentPanel);
            CreatePanelCornerDecorations(currentPanel);
            currentTitleText = CreateText(currentPanel, "Title", "НАГРАДА ДОСТУПНА", 24f, TextGold, FontStyles.Bold, new Vector2(0f, 238f), new Vector2(318f, 48f));
            RectTransform iconFrame = CreateRect(currentPanel, "IconFrame", Anchor.Center, Anchor.Center, new Vector2(178f, 178f), new Vector2(0f, 100f));
            currentRewardIcon = iconFrame.gameObject.AddComponent<Image>();
            currentRewardIcon.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
            currentRewardIcon.type = Image.Type.Sliced;
            currentRewardIcon.color = Color.white;
            TMP_Text oldCurrentIcon = CreateText(iconFrame, "Icon", string.Empty, 92f, TextGold, FontStyles.Bold, Vector2.zero, new Vector2(120f, 120f));
            oldCurrentIcon.gameObject.SetActive(false);
            currentRewardText = CreateText(currentPanel, "RewardTitle", "1 ПОДСКАЗКА", 30f, TextGold, FontStyles.Bold, new Vector2(0f, -54f), new Vector2(318f, 58f));
            currentDescriptionText = CreateText(currentPanel, "Description", "Используй подсказку, если застрял на уровне!", 19f, TextCream, FontStyles.Normal, new Vector2(0f, -128f), new Vector2(312f, 78f));
            claimButton = CreateTextButton(currentPanel, "ClaimButton", "ЗАБРАТЬ", new Vector2(0f, -204f), new Vector2(262f, 58f));
            claimButtonText = claimButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            CreateCheckIcon(claimButton.transform, new Vector2(-92f, 0f), new Vector2(26f, 21f)).gameObject.SetActive(false);
            CreateCurrentPanelDivider(currentPanel);
            CreateText(currentPanel, "Hint", "Награду можно забрать 1 раз в день", 13f, TextCream, FontStyles.Normal, new Vector2(0f, -274f), new Vector2(310f, 30f));

            RectTransform flash = CreateRect(panel, "ClaimFlash", Anchor.Center, Anchor.Center, new Vector2(380f, 380f), new Vector2(492f, 72f));
            Image flashImage = flash.gameObject.AddComponent<Image>();
            flashImage.sprite = PremiumUISprites.IconCircle;
            flashImage.color = new Color(1f, 0.82f, 0.18f, 0f);
            flash.gameObject.AddComponent<CanvasGroup>();
            flash.gameObject.SetActive(false);

            seriesText = CreateText(panel, "SeriesText", "День серии: 3 из 7", 24f, TextCream, FontStyles.Bold, new Vector2(-260f, -360f), new Vector2(520f, 38f));

            UpgradeAuthoringLayout();
            BindSceneReferences();
            WireButtons();
        }

        private void BakePersistentVisualAssets()
        {
            Sprite source = PremiumUISprites.PauseWindowCardMesh;
            Shader shader = Resources.Load<Shader>("Shaders/CardMeshLightFrame");
            if (source == null || shader == null || panel == null)
            {
                throw new UnityException("Daily Rewards authoring assets cannot be baked: source frame or shader is missing.");
            }

            System.IO.Directory.CreateDirectory(AuthoredAssetFolder);
            Rect sourceRect = source.rect;
            Sprite frameSprite = CreatePersistentSpriteAsset(source, new Rect(
                sourceRect.x + 72f,
                sourceRect.y + 90f,
                sourceRect.width - 144f,
                sourceRect.height - 180f), "DailyRewardsPopupFrame");
            Sprite topSprite = CreatePersistentSpriteAsset(source, new Rect(710f, 794f, 116f, 130f), "DailyRewardsTopFrameJewel");
            Sprite bottomSprite = CreatePersistentSpriteAsset(source, new Rect(710f, 140f, 116f, 130f), "DailyRewardsBottomFrameJewel");
            Sprite dividerSprite = CreatePersistentSpriteAsset(source, new Rect(240f, 600f, 1056f, 88f), "DailyRewardsDividerStrip");

            Material frameMaterial = CreatePersistentMaterialAsset(shader, frameSprite, "DailyRewardsFrame", false, false);
            Material topMaterial = CreatePersistentMaterialAsset(shader, topSprite, "DailyRewardsTopFrameJewel", true, false);
            Material bottomMaterial = CreatePersistentMaterialAsset(shader, bottomSprite, "DailyRewardsBottomFrameJewel", true, false);
            Material dividerMaterial = CreatePersistentMaterialAsset(shader, dividerSprite, "DailyRewardsDividerStrip", false, true);

            Image frameImage = panel.Find("Frame")?.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.sprite = frameSprite;
                frameImage.type = Image.Type.Simple;
                frameImage.color = Color.white;
                frameImage.material = frameMaterial;
                frameImage.raycastTarget = false;
                UnityEditor.EditorUtility.SetDirty(frameImage);
            }

            ApplyFrameDecorationAssets(panel, topSprite, bottomSprite, dividerSprite, topMaterial, bottomMaterial, dividerMaterial);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        private static Sprite CreatePersistentSpriteAsset(Sprite source, Rect textureRect, string assetName)
        {
            string path = $"{AuthoredAssetFolder}/{assetName}.asset";
            UnityEditor.AssetDatabase.DeleteAsset(path);

            Rect clamped = new(
                Mathf.Clamp(textureRect.x, 0f, source.texture.width - 1f),
                Mathf.Clamp(textureRect.y, 0f, source.texture.height - 1f),
                Mathf.Min(textureRect.width, source.texture.width - textureRect.x),
                Mathf.Min(textureRect.height, source.texture.height - textureRect.y));
            Sprite sprite = Sprite.Create(source.texture, clamped, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
            sprite.name = assetName;
            UnityEditor.AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        private static Material CreatePersistentMaterialAsset(Shader shader, Sprite sprite, string assetName, bool jewelOverlay, bool dividerOverlay)
        {
            string path = $"{AuthoredAssetFolder}/{assetName}.mat";
            Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = assetName };
                UnityEditor.AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            Rect rect = sprite.textureRect;
            material.SetVector("_UvRect", new Vector4(
                rect.x / sprite.texture.width,
                rect.y / sprite.texture.height,
                rect.width / sprite.texture.width,
                rect.height / sprite.texture.height));
            material.SetFloat("_JewelOverlay", jewelOverlay ? 1f : 0f);
            material.SetFloat("_DividerOverlay", dividerOverlay ? 1f : 0f);
            material.hideFlags = HideFlags.None;
            UnityEditor.EditorUtility.SetDirty(material);
            return material;
        }

        private void UpgradeAuthoringLayout()
        {
            sharedPrefabs.AssignDefaultsIfMissing();
            EnsurePopupCanvas();
            EnsureModalHierarchy();
            BindSceneReferences();

            RectTransform existingPanel = transform.Find("WindowRoot") as RectTransform;
            if (existingPanel != null)
            {
                SetStretchRect(existingPanel, WindowAnchorMin, WindowAnchorMax, Vector2.zero, Vector2.zero);
                Image image = existingPanel.Find("Frame")?.GetComponent<Image>();
                if (image != null)
                {
                    ApplyPopupPanelVisual(image);
                }

                Shadow shadow = existingPanel.GetComponent<Shadow>() ?? existingPanel.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(1f, 0.68f, 0.14f, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            StyleSharedButton(transform.Find("WindowRoot/CloseButton") as RectTransform, "X", new Vector2(66f, 66f), new Vector2(-48f, -48f), true);
            SetActiveIfExists(transform.Find("WindowRoot"), "InfoButton", false);
            UpgradeClaimButtonVisual();
            StyleCurrentRewardPanel();
            StyleHeader();
            StyleSeriesText();
            DisableLegacyHeaderCrown(transform);
            UpgradeDailyCards();
            UpgradeCurrentRewardIcon();
            ApplyAuthoringLayout();
            DisableExternalGlow(transform);
        }

        private void StyleHeader()
        {
            StyleHeaderRuntime();
        }

        private void StyleSeriesText()
        {
            StyleSeriesTextRuntime();
        }

        private void UpgradeDailyCards()
        {
            RectTransform root = transform.Find("WindowRoot/Content/RewardsColumn") as RectTransform;
            if (root == null)
            {
                return;
            }

            root.sizeDelta = RewardsColumnSize;
            root.anchoredPosition = RewardsColumnPosition;
            cardsRoot = root;
            EnsureRewardRows(root);

            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindRewardCard(day);
                if (card == null)
                {
                    continue;
                }

                RectTransform cardRect = card as RectTransform;
                if (cardRect != null)
                {
                    cardRect.sizeDelta = GetRewardCardSize(day);
                    cardRect.anchoredPosition = card.parent == root ? GetRewardCardPosition(day) : Vector2.zero;
                }

                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.sprite = GetRewardCardVisualSprite(false);
                    cardImage.type = Image.Type.Sliced;
                    cardImage.material = null;
                }

                EnsureRewardCardHierarchy(card);

                TMP_Text oldIcon = card.Find("Icon")?.GetComponent<TMP_Text>();
                if (oldIcon != null)
                {
                    oldIcon.gameObject.SetActive(false);
                }

                Image icon = FindRewardCardPart(card, "RewardIcon")?.GetComponent<Image>();
                if (icon == null)
                {
                    icon = CreateImage(card, "RewardIcon", GetRewardIcon(GetPreviewReward(day)), Color.white, GetRewardIconPosition(day), new Vector2(58f, 58f), Image.Type.Simple);
                }

                icon.sprite = GetRewardIcon(GetPreviewReward(day));
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.rectTransform.anchoredPosition = GetRewardIconPosition(day);
                icon.rectTransform.sizeDelta = new Vector2(66f, 66f);

                EnsureCardStatusBacking(card, day);
                StyleCardText(card, day);

                Image glow = FindRewardCardPart(card, "ActiveGlow")?.GetComponent<Image>();
                if (glow != null)
                {
                    Color color = glow.color;
                    color.a = 0f;
                    glow.color = color;
                    glow.rectTransform.sizeDelta = GetRewardCardSize(day) + new Vector2(18f, 22f);
                    glow.gameObject.SetActive(false);
                }
            }
        }

        private void UpgradeClaimButtonVisual()
        {
            RectTransform details = transform.Find("WindowRoot/Content/RewardDetailsPanel") as RectTransform;
            RectTransform existing = details != null ? details.Find("Status") as RectTransform : null;
            if (details == null || existing == null)
            {
                StyleMainActionButton(existing);
                return;
            }

            if (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(existing.gameObject))
            {
                UnityEditor.PrefabUtility.UnpackPrefabInstance(
                    existing.gameObject,
                    UnityEditor.PrefabUnpackMode.OutermostRoot,
                    UnityEditor.InteractionMode.AutomatedAction);
            }

            SetTextRect(existing, new Vector2(105f, -78f), new Vector2(300f, 64f));
            StyleMainActionButton(existing);
            TMP_Text label = existing != null ? existing.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label != null)
            {
                SetText(label, "ЗАБРАТЬ");
                label.color = TextCream;
                label.fontSizeMax = 27f;
                label.fontSizeMin = 18f;
                SetTextRect(label.rectTransform, Vector2.zero, new Vector2(252f, 42f));
            }

            if (existing != null && existing.Find("Check") == null)
            {
                CreateCheckIcon(existing, new Vector2(-78f, 0f), new Vector2(24f, 19f)).gameObject.SetActive(false);
            }
        }

        private static void StyleCardText(Transform card, int day)
        {
            Vector2 size = GetRewardCardSize(day);
            SetTextRect(FindRewardCardPart(card, "DayBadge", "Day") as RectTransform, new Vector2(0f, 143f), new Vector2(150f, 32f));
            SetTextRect(FindRewardCardPart(card, "RewardAmount", "Amount") as RectTransform, GetRewardAmountPosition(day), new Vector2(size.x - 34f, 46f));
            SetTextRect(FindRewardCardPart(card, "RewardType", "Label") as RectTransform, GetRewardLabelPosition(day), new Vector2(day == 7 ? size.x - 20f : size.x - 34f, day == 7 ? 38f : 34f));
            SetTextRect(FindRewardCardPart(card, "StatusArea", "StatusBacking") as RectTransform, GetRewardStatusPosition(day), new Vector2(size.x - 34f, day == 7 ? 24f : 29f));
            SetCenteredRect(FindRewardCardPart(card, "Status") as RectTransform, new Vector2(size.x - 34f, day == 7 ? 24f : 29f), Vector2.zero);
            RectTransform checkRect = FindRewardCardPart(card, "Check") as RectTransform;
            SetTextRect(checkRect, new Vector2(-34f, 0f), new Vector2(23f, 18f));
            StyleCheckIcon(checkRect);

            TMP_Text dayText = FindRewardCardPart(card, "DayBadge", "Day")?.GetComponent<TMP_Text>();
            if (dayText != null)
            {
                SetText(dayText, $"ДЕНЬ {day}");
                dayText.fontSizeMax = 22f;
                dayText.fontSizeMin = 18f;
                dayText.lineSpacing = 0f;
                dayText.color = TextCream;
            }

            TMP_Text amountText = FindRewardCardPart(card, "RewardAmount", "Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
            {
                amountText.fontSizeMax = day == 7 ? 32f : 35f;
                amountText.fontSizeMin = 19f;
                amountText.color = TextCream;
                amountText.lineSpacing = day == 7 ? -8f : 0f;
            }

            TMP_Text labelText = FindRewardCardPart(card, "RewardType", "Label")?.GetComponent<TMP_Text>();
            if (labelText != null)
            {
                labelText.fontSizeMax = day == 7 ? 15f : 19f;
                labelText.fontSizeMin = day == 7 ? 11f : 13f;
                labelText.lineSpacing = day == 7 ? -3f : -3f;
                labelText.textWrappingMode = day == 7 ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
                labelText.color = TextCream;
            }

            TMP_Text statusText = FindRewardCardPart(card, "Status")?.GetComponent<TMP_Text>();
            if (statusText != null)
            {
                statusText.enableAutoSizing = false;
                statusText.fontSize = 13f;
                statusText.fontSizeMax = 13f;
                statusText.fontSizeMin = 13f;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.margin = Vector4.zero;
            }

            ClearCheckText(FindRewardCardPart(card, "Check"));
        }

        private void UpgradeCurrentRewardIcon()
        {
            RectTransform frame = transform.Find("WindowRoot/Content/RewardDetailsPanel/RewardIconFrame") as RectTransform;
            if (frame == null)
            {
                return;
            }

            TMP_Text oldIcon = frame.Find("Icon")?.GetComponent<TMP_Text>();
            if (oldIcon != null)
            {
                oldIcon.gameObject.SetActive(false);
            }

            Image icon = frame.Find("RewardIcon")?.GetComponent<Image>();
            if (icon == null)
            {
                icon = CreateImage(frame, "RewardIcon", PremiumUISprites.HintBulbIcon, Color.white, Vector2.zero, new Vector2(104f, 104f), Image.Type.Simple);
            }

            icon.sprite = PremiumUISprites.HintBulbIcon;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.rectTransform.sizeDelta = new Vector2(104f, 104f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void StyleCurrentRewardPanel()
        {
            RectTransform rect = transform.Find("WindowRoot/Content/RewardDetailsPanel") as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = RewardDetailsPanelSize;
            rect.anchoredPosition = RewardDetailsPanelPosition;
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CategorySlot != null ? PremiumUISprites.CategorySlot : PremiumUISprites.EmeraldPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            ConfigureDropShadow(rect.gameObject, new Color(0f, 0.025f, 0.012f, 0.32f), new Vector2(0f, -3f));

            Outline outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.72f, 0.18f, 0.46f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateCurrentPanelInset(rect);
            CreateCurrentPanelWash(rect);
            CreatePanelCornerDecorations(rect);
            CreateCurrentPanelDivider(rect);
            SetTextRect(rect.Find("Title") as RectTransform, new Vector2(0f, 172f), new Vector2(270f, 42f));
            SetTextRect(rect.Find("RewardIconFrame") as RectTransform, new Vector2(0f, 72f), new Vector2(140f, 140f));
            SetTextRect(rect.Find("RewardName") as RectTransform, new Vector2(0f, -18f), new Vector2(266f, 44f));
            SetTextRect(rect.Find("Description") as RectTransform, new Vector2(0f, -75f), new Vector2(190f, 68f));
            SetTextRect(rect.Find("Status") as RectTransform, new Vector2(0f, -136f), new Vector2(208f, 50f));
            SetTextRect(rect.Find("Divider") as RectTransform, new Vector2(0f, -169f), new Vector2(248f, 2f));
            SetTextRect(rect.Find("Footnote") as RectTransform, new Vector2(0f, -185f), new Vector2(248f, 24f));

            TMP_Text title = rect.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.fontSizeMax = 25f;
                title.fontSizeMin = 17f;
            }

            TMP_Text reward = rect.Find("RewardName")?.GetComponent<TMP_Text>();
            if (reward != null)
            {
                reward.fontSizeMax = 32f;
                reward.fontSizeMin = 19f;
                reward.color = TextGold;
            }

            TMP_Text description = rect.Find("Description")?.GetComponent<TMP_Text>();
            if (description != null)
            {
                description.fontSizeMax = 19f;
                description.fontSizeMin = 13f;
                description.alignment = TextAlignmentOptions.Center;
                description.textWrappingMode = TextWrappingModes.Normal;
                description.lineSpacing = 12f;
            }

            TMP_Text hint = rect.Find("Footnote")?.GetComponent<TMP_Text>();
            if (hint != null)
            {
                hint.fontSizeMax = 13f;
                hint.fontSizeMin = 10f;
                hint.color = new Color(TextCream.r, TextCream.g, TextCream.b, 0.86f);
            }
        }

        private static void StyleSharedButton(RectTransform rect, string label, Vector2 size, Vector2 position, bool rightTop)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rightTop ? Vector2.one : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
                image.type = Image.Type.Sliced;
                image.color = new Color(1f, 1f, 1f, 0.94f);
            }

            Shadow shadow = rect.GetComponent<Shadow>();
            if (shadow != null && shadow is not Outline)
            {
                shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            TMP_Text text = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (text != null)
            {
                SetText(text, label);
                text.fontSizeMax = size.y * 0.46f;
                text.color = new Color(1f, 0.82f, 0.24f, 1f);
            }
        }

        private static void StyleMainActionButton(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(300f, 64f);
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.GameplayActionButton != null
                    ? PremiumUISprites.GameplayActionButton
                    : PremiumUISprites.CleanGoldButton;
                image.type = Image.Type.Simple;
                image.color = Color.white;
                image.material = null;
            }

            RemoveButtonGlow(rect);

            RectTransform check = CreateCheckIcon(rect, new Vector2(-126f, 0f), new Vector2(28f, 22f));
            check.gameObject.SetActive(false);
            SetTextRect(rect.Find("Label") as RectTransform, Vector2.zero, new Vector2(252f, 42f));
        }

        private static void SetActiveIfExists(Transform parent, string childName, bool active)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static void CreateRewardRows(RectTransform root)
        {
            EnsureRewardRow(root, "RewardsRowTop", TopRewardRowY, TopRowSpacing);
            EnsureRewardRow(root, "RewardsRowBottom", BottomRewardRowY, BottomRowSpacing);
        }

        private static void EnsureRewardRows(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            RectTransform topRow = EnsureRewardRow(root, "RewardsRowTop", TopRewardRowY, TopRowSpacing);
            RectTransform bottomRow = EnsureRewardRow(root, "RewardsRowBottom", BottomRewardRowY, BottomRowSpacing);
            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindChildRecursive(root, $"Day{day}Card");
                RectTransform targetRow = day <= 4 ? topRow : bottomRow;
                if (card == null || targetRow == null || card.parent == targetRow)
                {
                    continue;
                }

                card.SetParent(targetRow, false);
            }
        }

        private static RectTransform EnsureRewardRow(RectTransform root, string name, float y, float spacing)
        {
            if (root == null)
            {
                return null;
            }

            RectTransform row = root.Find(name) as RectTransform;
            if (row == null)
            {
                row = CreateRect(root, name, Anchor.Center, Anchor.Center, RewardRowSize, new Vector2(0f, y));
            }

            row.SetAsLastSibling();
            SetTextRect(row, new Vector2(0f, y), RewardRowSize);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static RectTransform GetRewardRow(RectTransform root, int day)
        {
            if (root == null)
            {
                return null;
            }

            return root.Find(day <= 4 ? "RewardsRowTop" : "RewardsRowBottom") as RectTransform;
        }

        private static void EnsureCardStatusBacking(Transform card, int day)
        {
            if (card == null)
            {
                return;
            }

            Vector2 size = card is RectTransform rect ? rect.sizeDelta : StandardRewardCardSize;
            Image backing = FindRewardCardPart(card, "StatusArea", "StatusBacking")?.GetComponent<Image>();
            if (backing == null)
            {
                backing = CreateImage(card, "StatusArea", PremiumUISprites.SecondaryButton, new Color(0.05f, 0.28f, 0.1f, 0.74f), GetRewardStatusPosition(day), new Vector2(size.x - 34f, day == 7 ? 24f : 29f), Image.Type.Sliced);
            }

            backing.gameObject.name = "StatusArea";
            backing.sprite = PremiumUISprites.SecondaryButton;
            backing.type = Image.Type.Sliced;
            backing.color = new Color(0.05f, 0.28f, 0.1f, 0.74f);
            backing.raycastTarget = false;
            backing.rectTransform.anchoredPosition = GetRewardStatusPosition(day);
            backing.rectTransform.sizeDelta = new Vector2(size.x - 34f, day == 7 ? 24f : 29f);
            LayoutElement backingLayout = backing.GetComponent<LayoutElement>() ?? backing.gameObject.AddComponent<LayoutElement>();
            backingLayout.minHeight = day == 7 ? 24f : 29f;
            backingLayout.preferredHeight = day == 7 ? 24f : 29f;

            Transform status = FindRewardCardPart(card, "Status");
            if (status != null)
            {
                status.SetParent(backing.transform, false);
            }
        }

        private static void CreateCurrentPanelDivider(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Image divider = panel.Find("Divider")?.GetComponent<Image>();
            if (divider == null)
            {
                divider = CreateImage(panel, "Divider", null, new Color(1f, 0.78f, 0.2f, 0.34f), new Vector2(0f, -260f), new Vector2(318f, 2f), Image.Type.Simple);
            }

            divider.sprite = null;
            divider.type = Image.Type.Simple;
            divider.color = new Color(1f, 0.78f, 0.2f, 0.34f);
            divider.raycastTarget = false;
            divider.rectTransform.anchoredPosition = new Vector2(0f, -260f);
            divider.rectTransform.sizeDelta = new Vector2(318f, 2f);

            Transform hint = panel.Find("Footnote") ?? panel.Find("Hint");
            if (hint != null)
            {
                divider.transform.SetSiblingIndex(Mathf.Max(0, hint.GetSiblingIndex()));
            }
        }

        private static void CreateRewardCard(RectTransform parent, int day)
        {
            RectTransform targetParent = GetRewardRow(parent, day);
            Vector2 position = GetRewardCardPosition(day);
            Vector2 size = GetRewardCardSize(day);
            RectTransform card = CreateRect(targetParent != null ? targetParent : parent, $"Day{day}Card", Anchor.Center, Anchor.Center, size, targetParent != null ? Vector2.zero : position);
            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = GetRewardCardVisualSprite(false);
            image.type = Image.Type.Sliced;
            image.material = null;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            card.gameObject.AddComponent<UIJuicyButton>();

            Image glow = CreateImage(card, "ActiveGlow", GetRewardCardVisualSprite(false), new Color(1f, 0.78f, 0.1f, 0f), Vector2.zero, size + new Vector2(24f, 28f), Image.Type.Sliced);
            glow.gameObject.SetActive(false);

            CreateText(card, "DayBadge", $"ДЕНЬ {day}", 22f, TextCream, FontStyles.Bold, new Vector2(0f, 143f), new Vector2(150f, 32f));
            TMP_Text oldIcon = CreateText(card, "Icon", string.Empty, day == 7 ? 52f : 48f, TextGold, FontStyles.Bold, GetRewardIconPosition(day), new Vector2(size.x - 34f, 66f));
            oldIcon.gameObject.SetActive(false);
            RectTransform statusArea = CreateImage(card, "StatusArea", PremiumUISprites.SecondaryButton, new Color(0.05f, 0.28f, 0.1f, 0.74f), GetRewardStatusPosition(day), new Vector2(size.x - 34f, day == 7 ? 24f : 29f), Image.Type.Sliced).rectTransform;
            CreateText(card, "RewardAmount", day switch { 1 => "50", 2 => "75", 3 => "1", 4 => "100", 5 => "+5", 6 => "150", _ => "300" }, day == 7 ? 30f : 33f, TextCream, FontStyles.Bold, GetRewardAmountPosition(day), new Vector2(size.x - 34f, 46f));
            CreateText(card, "RewardType", day switch { 3 => "ПОДСКАЗКА", 5 => "ХОДОВ", 7 => "МОНЕТ\n+1 ПОДСКАЗКА", _ => "МОНЕТ" }, day == 7 ? 15f : 18f, TextCream, FontStyles.Bold, GetRewardLabelPosition(day), new Vector2(day == 7 ? size.x - 20f : size.x - 34f, day == 7 ? 38f : 34f));
            CreateText(statusArea, "Status", day <= 2 ? "ПОЛУЧЕНО" : day == 3 ? "ДОСТУПНО" : "ЗАБЛОКИРОВАНО", 14f, day <= 3 ? TextGreen : TextGold, FontStyles.Bold, Vector2.zero, new Vector2(size.x - 34f, day == 7 ? 24f : 31f));
            CreateCheckIcon(statusArea, new Vector2(-34f, 0f), new Vector2(25f, 20f));
            StyleCardText(card, day);
        }

#endif

        private static Vector2 GetRewardCardPosition(int day)
        {
            return day switch
            {
                1 => new Vector2(-375f, TopRewardRowY),
                2 => new Vector2(-125f, TopRewardRowY),
                3 => new Vector2(125f, TopRewardRowY),
                4 => new Vector2(375f, TopRewardRowY),
                5 => new Vector2(-268f, BottomRewardRowY),
                6 => new Vector2(0f, BottomRewardRowY),
                _ => new Vector2(268f, BottomRewardRowY)
            };
        }

        private static Vector2 GetRewardCardSize(int day)
        {
            return StandardRewardCardSize;
        }

        private static Vector2 GetRewardIconPosition(int day)
        {
            return day == 7 ? new Vector2(0f, 52f) : new Vector2(0f, 50f);
        }

        private static Vector2 GetRewardAmountPosition(int day)
        {
            return day == 7 ? new Vector2(0f, -7f) : new Vector2(0f, -21f);
        }

        private static Vector2 GetRewardLabelPosition(int day)
        {
            return day == 7
                ? new Vector2(0f, -50f)
                : new Vector2(0f, -59f);
        }

        private static Vector2 GetRewardStatusPosition(int day)
        {
            return new Vector2(0f, day == 7 ? -80f : CardStatusY);
        }

#if UNITY_EDITOR
        private static void SetTextRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static RectTransform CreateCheckIcon(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform root = parent.Find("Check") as RectTransform;
            if (root == null)
            {
                root = CreateRect(parent, "Check", Anchor.Center, Anchor.Center, size, position);
            }

            SetTextRect(root, position, size);
            ClearCheckText(root);
            CreateCheckSegment(root, "Stem", new Vector2(-6f, -2f), new Vector2(size.x * 0.34f, size.y * 0.18f), -42f);
            CreateCheckSegment(root, "Arm", new Vector2(5f, 1f), new Vector2(size.x * 0.72f, size.y * 0.18f), 42f);
            root.gameObject.SetActive(false);
            return root;
        }

        private static void StyleCheckIcon(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            ClearCheckText(root);
            Vector2 size = root.sizeDelta;
            CreateCheckSegment(root, "Stem", new Vector2(-6f, -2f), new Vector2(size.x * 0.34f, size.y * 0.18f), -42f);
            CreateCheckSegment(root, "Arm", new Vector2(5f, 1f), new Vector2(size.x * 0.72f, size.y * 0.18f), 42f);
        }

        private static void ClearCheckText(Transform root)
        {
            TMP_Text text = root != null ? root.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                return;
            }

            text.text = string.Empty;
            text.enabled = false;
            text.raycastTarget = false;
        }

        private static void CreateCheckSegment(RectTransform parent, string name, Vector2 position, Vector2 size, float angle)
        {
            Image segment = parent.Find(name)?.GetComponent<Image>();
            if (segment == null)
            {
                segment = CreateImage(parent, name, null, TextGreen, position, size, Image.Type.Simple);
            }

            segment.color = TextGreen;
            segment.raycastTarget = false;
            segment.rectTransform.anchoredPosition = position;
            segment.rectTransform.sizeDelta = size;
            segment.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private static void CreatePanelCornerDecorations(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Vector2 inset = new(28f, 28f);
            CreateCornerDecoration(panel, "CornerTopLeft", new Vector2(-panel.sizeDelta.x * 0.5f + inset.x, panel.sizeDelta.y * 0.5f - inset.y), 45f);
            CreateCornerDecoration(panel, "CornerTopRight", new Vector2(panel.sizeDelta.x * 0.5f - inset.x, panel.sizeDelta.y * 0.5f - inset.y), -45f);
            CreateCornerDecoration(panel, "CornerBottomLeft", new Vector2(-panel.sizeDelta.x * 0.5f + inset.x, -panel.sizeDelta.y * 0.5f + inset.y), -45f);
            CreateCornerDecoration(panel, "CornerBottomRight", new Vector2(panel.sizeDelta.x * 0.5f - inset.x, -panel.sizeDelta.y * 0.5f + inset.y), 45f);
        }

        private static void CreateCurrentPanelInset(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Image inset = panel.Find("InnerEmeraldPanel")?.GetComponent<Image>();
            if (inset == null)
            {
                inset = CreateImage(panel, "InnerEmeraldPanel", PremiumUISprites.CategorySlotEmerald, new Color(0.88f, 1f, 0.94f, 0.46f), Vector2.zero, panel.sizeDelta - new Vector2(54f, 60f), Image.Type.Simple);
            }

            inset.sprite = PremiumUISprites.CategorySlotEmerald;
            inset.type = Image.Type.Simple;
            inset.color = new Color(0.88f, 1f, 0.94f, 0.46f);
            inset.raycastTarget = false;
            inset.rectTransform.anchoredPosition = Vector2.zero;
            inset.rectTransform.sizeDelta = panel.sizeDelta - new Vector2(54f, 60f);
            inset.transform.SetAsFirstSibling();
        }

        private static void CreateCurrentPanelWash(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Image wash = panel.Find("InnerGreenWash")?.GetComponent<Image>();
            if (wash == null)
            {
                wash = CreateImage(panel, "InnerGreenWash", null, new Color(0.025f, 0.22f, 0.105f, 0.7f), new Vector2(0f, 6f), panel.sizeDelta - new Vector2(84f, 100f), Image.Type.Simple);
            }

            wash.sprite = null;
            wash.type = Image.Type.Simple;
            wash.color = new Color(0.025f, 0.22f, 0.105f, 0.36f);
            wash.raycastTarget = false;
            wash.rectTransform.anchoredPosition = new Vector2(0f, 6f);
            wash.rectTransform.sizeDelta = panel.sizeDelta - new Vector2(84f, 100f);
            wash.transform.SetSiblingIndex(Mathf.Min(1, panel.childCount - 1));
        }

        private static void CreateCornerDecoration(RectTransform parent, string name, Vector2 position, float angle)
        {
            Image corner = parent.Find(name)?.GetComponent<Image>();
            if (corner == null)
            {
                corner = CreateImage(parent, name, PremiumUISprites.IconCircle, new Color(1f, 0.72f, 0.18f, 0.5f), position, new Vector2(26f, 26f), Image.Type.Sliced);
            }

            corner.sprite = PremiumUISprites.IconCircle;
            corner.type = Image.Type.Sliced;
            corner.color = new Color(1f, 0.72f, 0.18f, 0.5f);
            corner.raycastTarget = false;
            corner.rectTransform.anchoredPosition = position;
            corner.rectTransform.sizeDelta = new Vector2(26f, 26f);
            corner.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private static void CreateHeaderIcon(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            Image icon = (panel.Find("TopBadge") ?? panel.Find("HeaderIcon"))?.GetComponent<Image>();
            if (icon == null)
            {
                icon = CreateImage(panel, "TopBadge", GetDailyRewardsTopBadgeSprite(), Color.white, new Vector2(0f, TopBadgeY), TopBadgeSize, Image.Type.Simple);
            }

            icon.gameObject.name = "TopBadge";
            icon.sprite = GetDailyRewardsTopBadgeSprite();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, TopBadgeY);
            icon.rectTransform.sizeDelta = TopBadgeSize;
            icon.transform.SetAsLastSibling();
        }

        private static Sprite GetDailyRewardsHeaderSprite()
        {
            return GetDailyRewardsTopBadgeSprite();
        }

        private static RectTransform CreateRect(Transform parent, string name, Anchor anchor, Anchor ignored, Vector2 size, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Vector2 point = anchor == Anchor.Center ? new Vector2(0.5f, 0.5f) : Vector2.zero;
            rect.anchorMin = point;
            rect.anchorMax = point;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, Image.Type type)
        {
            RectTransform rect = CreateRect(parent, name, Anchor.Center, Anchor.Center, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, FontStyles style, Vector2 position, Vector2 rectSize)
        {
            RectTransform rect = CreateRect(parent, name, Anchor.Center, Anchor.Center, rectSize, position);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(11f, size * 0.5f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
            return label;
        }

        private static Button CreateCircleButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Color textColor)
        {
            RectTransform rect = CreateRect(parent, name, Anchor.Center, Anchor.Center, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.IconCircle;
            image.type = Image.Type.Sliced;
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, size.y * 0.46f, textColor, FontStyles.Bold, Vector2.zero, size * 0.7f);
            rect.gameObject.AddComponent<UIJuicyButton>();
            return button;
        }

        private static Button CreateTextButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, Anchor.Center, Anchor.Center, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.GameplayActionButton != null ? PremiumUISprites.GameplayActionButton : PremiumUISprites.PrimaryButton;
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, 28f, TextDark, FontStyles.Bold, Vector2.zero, size - new Vector2(48f, 16f));
            rect.gameObject.AddComponent<UIJuicyButton>();
            return button;
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            Shadow shadow = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, 0f);
            shadow.effectDistance = Vector2.zero;
            shadow.useGraphicAlpha = true;
        }

        private enum Anchor
        {
            Center
        }
#endif

        private void ApplyPopupPanelVisual()
        {
            Image panelImage = panel != null ? panel.Find("Frame")?.GetComponent<Image>() ?? panel.GetComponent<Image>() : null;
            if (panelImage != null)
            {
                ApplyPopupPanelVisual(panelImage);
            }
        }

        private static void ApplyPopupPanelVisual(Image panelImage)
        {
            panelImage.sprite = GetPopupPanelSprite();
            panelImage.type = PremiumUISprites.MobilePopupFrame != null ? Image.Type.Simple : Image.Type.Sliced;
            panelImage.color = Color.white;
            panelImage.material = null;
        }

        private static Material GetPopupFrameMaterial(Sprite sprite)
        {
            return sprite != null ? Resources.Load<Material>(AuthoredResourceFolder + "DailyRewardsFrame") : null;
        }

        private static Sprite GetPopupPanelSprite()
        {
            if (PremiumUISprites.MobilePopupFrame != null)
            {
                return PremiumUISprites.MobilePopupFrame;
            }

            Sprite authored = Resources.Load<Sprite>(AuthoredResourceFolder + "DailyRewardsPopupFrame");
            if (authored != null)
            {
                return authored;
            }

            return PremiumUISprites.PauseWindowCardMesh != null
                ? PremiumUISprites.PauseWindowCardMesh
                : RoyalEventSprites.Panel != null
                    ? RoyalEventSprites.Panel
                    : PremiumUISprites.EmeraldPanel;
        }

        private static void ApplyFrameDecorationAssets(
            RectTransform windowRoot,
            Sprite topSprite,
            Sprite bottomSprite,
            Sprite dividerSprite,
            Material topMaterial,
            Material bottomMaterial,
            Material dividerMaterial)
        {
            Image top = EnsureFrameJewelImage(windowRoot, "TopFrameJewel", topSprite, true);
            Image bottom = EnsureFrameJewelImage(windowRoot, "BottomFrameJewel", bottomSprite, false);
            Image divider = EnsureDividerStripImage(windowRoot, dividerSprite);
            top.material = topMaterial;
            bottom.material = bottomMaterial;
            divider.material = dividerMaterial;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(top);
            UnityEditor.EditorUtility.SetDirty(bottom);
            UnityEditor.EditorUtility.SetDirty(divider);
#endif
        }

        private static Image EnsureFrameJewelImage(RectTransform windowRoot, string name, Sprite sprite, bool top)
        {
            RectTransform rect = windowRoot.Find(name) as RectTransform ?? EnsureChildRect(windowRoot, name);
            rect.anchorMin = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, top ? -20f : 37f);
            rect.sizeDelta = new Vector2(76f, 84f);
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(Mathf.Min(1, windowRoot.childCount - 1));

            Image image = EnsureImage(rect);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Image EnsureDividerStripImage(RectTransform windowRoot, Sprite sprite)
        {
            RectTransform rect = windowRoot.Find("DividerStrip") as RectTransform ?? EnsureChildRect(windowRoot, "DividerStrip");
            rect.anchorMin = new Vector2(0.5f, 0.817f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -2f);
            rect.sizeDelta = new Vector2(1056f, 88f);
            rect.localScale = Vector3.one;
            rect.SetSiblingIndex(Mathf.Min(2, windowRoot.childCount - 1));

            Image image = EnsureImage(rect);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private void LayoutPopupFrame()
        {
            LayoutPopupFrame(panel != null ? panel.Find("Frame") as RectTransform : null);
        }

        private static void LayoutPopupFrame(RectTransform frame)
        {
            if (frame == null)
            {
                return;
            }

            SetStretchRect(frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Sprite GetRewardCardVisualSprite(bool active)
        {
            Sprite clean = active ? CleanRoyalPopupSprites.DailyCardAvailable : CleanRoyalPopupSprites.DailyCard;
            return clean != null ? clean : PremiumUISprites.CategorySlot != null
                ? PremiumUISprites.CategorySlot
                : active ? RoyalEventSprites.RewardCardActive : RoyalEventSprites.RewardCard;
        }

        private static void ConfigureDropShadow(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return;
            }

            Shadow dropShadow = null;
            foreach (Shadow shadow in target.GetComponents<Shadow>())
            {
                if (shadow is not Outline)
                {
                    dropShadow = shadow;
                    break;
                }
            }

            dropShadow ??= target.AddComponent<Shadow>();
            dropShadow.effectColor = color;
            dropShadow.effectDistance = distance;
            dropShadow.useGraphicAlpha = true;
        }

        private static void DisableExternalGlow(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Shadow shadow in root.GetComponentsInChildren<Shadow>(true))
            {
                if (shadow is Outline)
                {
                    continue;
                }

                if (shadow.gameObject.name == "RewardDetailsPanel"
                    || shadow.gameObject.name == "Status"
                    || shadow.gameObject.name.StartsWith("Day"))
                {
                    continue;
                }

                shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            foreach (Outline outline in root.GetComponentsInChildren<Outline>(true))
            {
                if (outline.gameObject.name == "RewardDetailsPanel")
                {
                    continue;
                }

                if (outline.GetComponent<TMP_Text>() != null)
                {
                    continue;
                }

                outline.effectColor = new Color(outline.effectColor.r, outline.effectColor.g, outline.effectColor.b, 0f);
                outline.effectDistance = Vector2.zero;
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "ActiveGlow"
                    && image.transform.parent != null
                    && image.transform.parent.name.StartsWith("Day"))
                {
                    continue;
                }

                if (!IsExternalGlowName(image.gameObject.name))
                {
                    continue;
                }

                Color color = image.color;
                color.a = 0f;
                image.color = color;
                image.gameObject.SetActive(false);
            }
        }

        private static void RemoveButtonGlow(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            foreach (Shadow effect in rect.GetComponents<Shadow>())
            {
                effect.effectColor = Color.clear;
                effect.effectDistance = Vector2.zero;
            }

            foreach (Transform child in rect.GetComponentsInChildren<Transform>(true))
            {
                if (child == rect || !IsExternalGlowName(child.name))
                {
                    continue;
                }

                child.gameObject.SetActive(false);
            }
        }

        private static void DisableLegacyHeaderCrown(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "TopBadge")
                {
                    continue;
                }

                string name = child.name.ToLowerInvariant();
                if (name == "topgem"
                    || name.Contains("crown")
                    || name.Contains("emperor"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static bool IsExternalGlowName(string objectName)
        {
            string name = objectName.ToLowerInvariant();
            return name.Contains("glow")
                || name.Contains("halo")
                || name.Contains("flash")
                || name.Contains("shine")
                || name.Contains("sparkle")
                || name.Contains("aura")
                || name.Contains("bloom")
                || name.Contains("outer")
                || name.Contains("highlight")
                || name.Contains("backlight");
        }

    }
}
