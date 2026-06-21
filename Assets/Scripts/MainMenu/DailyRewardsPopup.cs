using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [ExecuteAlways]
    public sealed class DailyRewardsPopup : MonoBehaviour
    {
        private static readonly Color TextDark = new(0.13f, 0.07f, 0.02f, 1f);
        private static readonly Color TextCream = new(1f, 0.92f, 0.7f, 1f);
        private static readonly Color TextGold = new(1f, 0.78f, 0.2f, 1f);
        private static readonly Color TextGreen = new(0.58f, 1f, 0.32f, 1f);
        private static readonly Color PanelGreen = new(0.045f, 0.28f, 0.135f, 0.92f);
        private static readonly Color DeepPanelGreen = new(0.032f, 0.22f, 0.105f, 0.9f);
        private static readonly Vector2 WindowSize = new(1700f, 1060f);
        private static readonly Vector2 RewardGridSize = new(1120f, 690f);
        private static readonly Vector2 RewardGridPosition = new(-350f, -88f);
        private static readonly Vector2 CurrentPanelSize = new(410f, 600f);
        private static readonly Vector2 CurrentPanelPosition = new(580f, -94f);
        private static readonly Vector2 StandardRewardCardSize = new(224f, 220f);
        private static readonly Vector2 FinalRewardCardSize = new(264f, 250f);
        private const float TopRewardRowY = 45f;
        private const float BottomRewardRowY = -285f;
        private const float CardStatusGap = 24f;
        private const float HeaderIconY = 452f;

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
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

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
                QueueEditorRefresh();
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
            SetText(currentTitleText, "ТЕКУЩАЯ НАГРАДА");
            SetText(currentRewardText, "1 ПОДСКАЗКА");
            SetText(currentDescriptionText, "Используй подсказку, если застрял на уровне!");
            SetText(seriesText, "День серии: 3 из 7");
            SetText(claimButtonText, "ЗАБРАТЬ");
            StyleClaimButtonState(RewardViewState.Available);
            RefreshCards();
            DisableExternalGlow(transform);
            DisableLegacyHeaderCrown(transform);
        }

        private void RefreshCards()
        {
            if (cardsRoot == null)
            {
                return;
            }

            for (int i = 0; i < cardsRoot.childCount; i++)
            {
                Transform card = cardsRoot.GetChild(i);
                int day = i + 1;
                DailyReward reward = manager != null ? manager.GetReward(day) : GetPreviewReward(day);
                bool claimed = manager != null ? manager.IsDayClaimed(day) : day <= 2;
                bool current = manager != null ? manager.IsDayCurrent(day) : day == 3;
                bool selected = selectedDay == day || (!Application.isPlaying && day == 3);
                bool locked = manager != null ? manager.IsDayLocked(day) : day > 3;

                Image image = card.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = RoyalEventSprites.RewardCard;
                    image.type = Image.Type.Sliced;
                    image.color = locked && !claimed ? new Color(0.68f, 0.68f, 0.68f, 0.72f) : Color.white;
                }

                SetText(card.Find("Day")?.GetComponent<TMP_Text>(), $"ДЕНЬ\n{day}");
                SetText(card.Find("Amount")?.GetComponent<TMP_Text>(), BuildAmount(reward));
                SetText(card.Find("Label")?.GetComponent<TMP_Text>(), BuildLabel(reward));
                TMP_Text statusText = card.Find("Status")?.GetComponent<TMP_Text>();
                SetText(statusText, claimed ? "ПОЛУЧЕНО" : current ? "СЕГОДНЯ" : "ЗАБЛОКИРОВАНО");
                if (statusText != null)
                {
                    statusText.color = claimed || current ? TextGreen : TextGold;
                }

                Image rewardIcon = card.Find("RewardIcon")?.GetComponent<Image>();
                if (rewardIcon != null)
                {
                    rewardIcon.sprite = GetRewardIcon(reward);
                    rewardIcon.preserveAspect = true;
                    rewardIcon.color = Color.white;
                }

                Transform glow = card.Find("ActiveGlow");
                if (glow != null)
                {
                    Image glowImage = glow.GetComponent<Image>();
                    if (glowImage != null)
                    {
                        Color color = glowImage.color;
                        color.a = 0f;
                        glowImage.color = color;
                    }

                    glow.gameObject.SetActive(false);
                }

                Transform check = card.Find("Check");
                if (check != null)
                {
#if UNITY_EDITOR
                    StyleCheckIcon(check as RectTransform);
#endif
                    check.gameObject.SetActive(claimed);
                }
            }
        }

        private void RefreshSelectedReward()
        {
            DailyReward reward = manager != null ? manager.GetReward(selectedDay) : GetPreviewReward(selectedDay <= 0 ? 3 : selectedDay);
            if (reward == null)
            {
                return;
            }

            bool isCurrentDay = selectedDay == (manager != null ? manager.CurrentDay : 3);
            bool isClaimed = manager != null ? manager.IsDayClaimed(selectedDay) : selectedDay <= 2;
            bool isAvailable = manager != null ? manager.IsDayCurrent(selectedDay) : isCurrentDay && !isClaimed;
            bool isLocked = manager != null ? manager.IsDayLocked(selectedDay) : selectedDay > 3;
            RewardViewState viewState = isClaimed ? RewardViewState.Claimed : isAvailable ? RewardViewState.Available : RewardViewState.Locked;

            SetText(currentTitleText, isClaimed ? "НАГРАДА ПОЛУЧЕНА" : isCurrentDay ? "ТЕКУЩАЯ НАГРАДА" : $"НАГРАДА ДНЯ {selectedDay}");
            SetText(currentRewardText, reward.Title.ToUpperInvariant());
            SetText(currentDescriptionText, isClaimed ? "Эта награда уже добавлена в твою серию." : isLocked ? "Эта награда откроется в один из следующих дней серии." : reward.Description);
            SetText(claimButtonText, viewState switch
            {
                RewardViewState.Available => "ЗАБРАТЬ",
                RewardViewState.Claimed => "ПОЛУЧЕНО",
                _ => "НЕДОСТУПНО"
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

            rect.sizeDelta = state switch
            {
                RewardViewState.Available => new Vector2(292f, 70f),
                RewardViewState.Claimed => new Vector2(246f, 52f),
                _ => new Vector2(268f, 56f)
            };

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
                image.type = Image.Type.Sliced;
                image.color = state switch
                {
                    RewardViewState.Available => Color.white,
                    RewardViewState.Claimed => new Color(0.72f, 0.62f, 0.28f, 0.72f),
                    _ => new Color(0.46f, 0.42f, 0.28f, 0.58f)
                };
            }

            TMP_Text label = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.rectTransform.anchoredPosition = state == RewardViewState.Claimed ? new Vector2(14f, 0f) : Vector2.zero;
                label.rectTransform.sizeDelta = rect.sizeDelta - new Vector2(state == RewardViewState.Claimed ? 78f : 54f, 18f);
                label.color = state switch
                {
                    RewardViewState.Available => TextDark,
                    RewardViewState.Claimed => new Color(0.08f, 0.31f, 0.08f, 1f),
                    _ => new Color(0.18f, 0.13f, 0.05f, 0.86f)
                };
                label.fontSizeMax = state == RewardViewState.Available ? 33f : 26f;
                label.fontSizeMin = 18f;
            }

            Transform check = rect.Find("Check");
            if (check != null)
            {
                RectTransform checkRect = check as RectTransform;
                if (checkRect != null)
                {
                    checkRect.anchoredPosition = new Vector2(state == RewardViewState.Claimed ? -84f : -100f, 0f);
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
            Transform flash = transform.Find("Panel/ClaimFlash");
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
            panel = transform.Find("Panel") as RectTransform;
            cardsRoot = transform.Find("Panel/RewardGrid") as RectTransform;
            currentTitleText = FindTMP("Panel/CurrentRewardPanel/Title");
            currentRewardText = FindTMP("Panel/CurrentRewardPanel/RewardTitle");
            currentDescriptionText = FindTMP("Panel/CurrentRewardPanel/Description");
            currentRewardIcon = transform.Find("Panel/CurrentRewardPanel/IconFrame")?.GetComponent<Image>();
            seriesText = FindTMP("Panel/SeriesText");
            claimButton = transform.Find("Panel/CurrentRewardPanel/ClaimButton")?.GetComponent<Button>();
            claimButtonText = FindTMP("Panel/CurrentRewardPanel/ClaimButton/Label");
            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            infoButton = transform.Find("Panel/InfoButton")?.GetComponent<Button>();
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

            for (int i = 0; i < cardsRoot.childCount; i++)
            {
                int day = i + 1;
                Button button = cardsRoot.GetChild(i).GetComponent<Button>();
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
                DailyRewardType.Hint => PremiumUISprites.HintBulbIcon,
                DailyRewardType.ExtraMoves => PremiumUISprites.BoosterRocketIcon != null ? PremiumUISprites.BoosterRocketIcon : PremiumUISprites.UndoIcon,
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
                return "300\n+1";
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
                return "МОНЕТ\nПОДСКАЗКА";
            }

            return reward.RewardType switch
            {
                DailyRewardType.Coins => "МОНЕТ",
                DailyRewardType.Hint => "ПОДСКАЗКА",
                DailyRewardType.ExtraMoves => "ХОДОВ",
                _ => "НАГРАДА"
            };
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

        private void EnsureAuthoringLayout()
        {
            if (transform.Find("Panel") != null)
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
            panelImage.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(1f, 0.68f, 0.14f, 0f), Vector2.zero);

            CreateHeaderIcon(panel);
            CreateText(panel, "Title", "ЕЖЕДНЕВНЫЕ НАГРАДЫ", 58f, TextGold, FontStyles.Bold, new Vector2(0f, 304f), new Vector2(1120f, 72f));
            CreateText(panel, "Subtitle", "Возвращайся каждый день и получай ценные награды", 29f, TextCream, FontStyles.Bold, new Vector2(0f, 238f), new Vector2(1080f, 44f));

            infoButton = CreateCircleButton(panel, "InfoButton", "i", new Vector2(-735f, 370f), new Vector2(74f, 74f), new Color(0.04f, 0.25f, 0.12f, 1f), TextGold);
            closeButton = CreateCircleButton(panel, "CloseButton", "X", new Vector2(735f, 370f), new Vector2(88f, 88f), new Color(0.87f, 0.56f, 0.08f, 1f), TextDark);

            cardsRoot = CreateRect(panel, "RewardGrid", Anchor.Center, Anchor.Center, RewardGridSize, RewardGridPosition);
            for (int i = 0; i < 7; i++)
            {
                CreateRewardCard(cardsRoot, i + 1);
            }

            RectTransform currentPanel = CreateRect(panel, "CurrentRewardPanel", Anchor.Center, Anchor.Center, CurrentPanelSize, CurrentPanelPosition);
            Image currentPanelImage = currentPanel.gameObject.AddComponent<Image>();
            currentPanelImage.sprite = PremiumUISprites.CategorySlot != null ? PremiumUISprites.CategorySlot : PremiumUISprites.EmeraldPanel;
            currentPanelImage.type = Image.Type.Sliced;
            currentPanelImage.color = DeepPanelGreen;
            AddShadow(currentPanel.gameObject, new Color(1f, 0.7f, 0.12f, 0f), Vector2.zero);
            Outline currentOutline = currentPanel.gameObject.AddComponent<Outline>();
            currentOutline.effectColor = new Color(1f, 0.72f, 0.18f, 0.46f);
            currentOutline.effectDistance = new Vector2(1f, -1f);

            CreateCurrentPanelInset(currentPanel);
            CreateCurrentPanelWash(currentPanel);
            CreatePanelCornerDecorations(currentPanel);
            currentTitleText = CreateText(currentPanel, "Title", "ТЕКУЩАЯ НАГРАДА", 28f, TextGold, FontStyles.Bold, new Vector2(0f, 228f), new Vector2(354f, 46f));
            RectTransform iconFrame = CreateRect(currentPanel, "IconFrame", Anchor.Center, Anchor.Center, new Vector2(214f, 214f), new Vector2(0f, 88f));
            currentRewardIcon = iconFrame.gameObject.AddComponent<Image>();
            currentRewardIcon.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
            currentRewardIcon.type = Image.Type.Sliced;
            currentRewardIcon.color = Color.white;
            TMP_Text oldCurrentIcon = CreateText(iconFrame, "Icon", string.Empty, 92f, TextGold, FontStyles.Bold, Vector2.zero, new Vector2(120f, 120f));
            oldCurrentIcon.gameObject.SetActive(false);
            currentRewardText = CreateText(currentPanel, "RewardTitle", "1 ПОДСКАЗКА", 34f, TextGreen, FontStyles.Bold, new Vector2(0f, -72f), new Vector2(350f, 64f));
            currentDescriptionText = CreateText(currentPanel, "Description", "Используй подсказку, если застрял на уровне!", 22f, TextCream, FontStyles.Normal, new Vector2(0f, -146f), new Vector2(350f, 82f));
            claimButton = CreateTextButton(currentPanel, "ClaimButton", "ЗАБРАТЬ", new Vector2(0f, -222f), new Vector2(292f, 70f));
            claimButtonText = claimButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            CreateCheckIcon(claimButton.transform, new Vector2(-100f, 0f), new Vector2(30f, 24f)).gameObject.SetActive(false);
            CreateText(currentPanel, "Hint", "Награду можно забрать 1 раз в день", 15f, TextCream, FontStyles.Normal, new Vector2(0f, -270f), new Vector2(340f, 30f));

            RectTransform flash = CreateRect(panel, "ClaimFlash", Anchor.Center, Anchor.Center, new Vector2(380f, 380f), new Vector2(492f, 72f));
            Image flashImage = flash.gameObject.AddComponent<Image>();
            flashImage.sprite = PremiumUISprites.IconCircle;
            flashImage.color = new Color(1f, 0.82f, 0.18f, 0f);
            flash.gameObject.AddComponent<CanvasGroup>();
            flash.gameObject.SetActive(false);

            seriesText = CreateText(panel, "SeriesText", "День серии: 3 из 7", 27f, TextCream, FontStyles.Bold, new Vector2(-165f, -485f), new Vector2(520f, 42f));

            UpgradeAuthoringLayout();
            BindSceneReferences();
            WireButtons();
        }

        private void QueueEditorRefresh()
        {
            sharedPrefabs.AssignDefaultsIfMissing();
            if (editorRefreshQueued)
            {
                return;
            }

            editorRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                editorRefreshQueued = false;
                if (Application.isPlaying)
                {
                    return;
                }

                EnsureAuthoringLayout();
                BindSceneReferences();
                ApplyFontsToAllTexts();
                RefreshEditorPreview();
            };
        }

        private void UpgradeAuthoringLayout()
        {
            sharedPrefabs.AssignDefaultsIfMissing();

            RectTransform existingPanel = transform.Find("Panel") as RectTransform;
            if (existingPanel != null)
            {
                existingPanel.sizeDelta = WindowSize;
                Image image = existingPanel.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }

                Shadow shadow = existingPanel.GetComponent<Shadow>() ?? existingPanel.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(1f, 0.68f, 0.14f, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            StyleSharedButton(transform.Find("Panel/CloseButton") as RectTransform, "X", new Vector2(86f, 86f), new Vector2(-78f, -80f), true);
            StyleSharedButton(transform.Find("Panel/InfoButton") as RectTransform, "i", new Vector2(66f, 66f), new Vector2(84f, -88f), false);
            StyleMainActionButton(transform.Find("Panel/CurrentRewardPanel/ClaimButton") as RectTransform);
            StyleCurrentRewardPanel();
            StyleHeader();
            StyleSeriesText();
            DisableLegacyHeaderCrown(transform);
            UpgradeDailyCards();
            UpgradeCurrentRewardIcon();
            DisableExternalGlow(transform);
        }

        private void StyleHeader()
        {
            RectTransform title = transform.Find("Panel/Title") as RectTransform;
            if (title != null)
            {
                title.sizeDelta = new Vector2(1160f, 72f);
                title.anchoredPosition = new Vector2(0f, 304f);
            }

            TMP_Text titleLabel = transform.Find("Panel/Title")?.GetComponent<TMP_Text>();
            if (titleLabel != null)
            {
                titleLabel.fontSizeMax = 58f;
                titleLabel.fontSizeMin = 34f;
                titleLabel.color = TextGold;
            }

            RectTransform subtitle = transform.Find("Panel/Subtitle") as RectTransform;
            if (subtitle != null)
            {
                subtitle.sizeDelta = new Vector2(1100f, 44f);
                subtitle.anchoredPosition = new Vector2(0f, 238f);
            }

            TMP_Text subtitleLabel = transform.Find("Panel/Subtitle")?.GetComponent<TMP_Text>();
            if (subtitleLabel != null)
            {
                subtitleLabel.fontSizeMax = 29f;
                subtitleLabel.fontSizeMin = 20f;
            }

            CreateHeaderIcon(transform.Find("Panel") as RectTransform);
        }

        private void StyleSeriesText()
        {
            RectTransform series = transform.Find("Panel/SeriesText") as RectTransform;
            if (series != null)
            {
                series.sizeDelta = new Vector2(520f, 42f);
                series.anchoredPosition = new Vector2(-165f, -485f);
            }

            TMP_Text label = series != null ? series.GetComponent<TMP_Text>() : null;
            if (label != null)
            {
                label.fontSizeMax = 27f;
                label.fontSizeMin = 18f;
                label.color = TextCream;
            }
        }

        private void UpgradeDailyCards()
        {
            RectTransform root = transform.Find("Panel/RewardGrid") as RectTransform;
            if (root == null)
            {
                return;
            }

            root.sizeDelta = RewardGridSize;
            root.anchoredPosition = RewardGridPosition;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform card = root.GetChild(i);
                RectTransform cardRect = card as RectTransform;
                if (cardRect != null)
                {
                    int day = i + 1;
                    cardRect.sizeDelta = GetRewardCardSize(day);
                    cardRect.anchoredPosition = GetRewardCardPosition(day);
                }

                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.sprite = RoyalEventSprites.RewardCard;
                    cardImage.type = Image.Type.Sliced;
                }

                TMP_Text oldIcon = card.Find("Icon")?.GetComponent<TMP_Text>();
                if (oldIcon != null)
                {
                    oldIcon.gameObject.SetActive(false);
                }

                Image icon = card.Find("RewardIcon")?.GetComponent<Image>();
                if (icon == null)
                {
                    icon = CreateImage(card, "RewardIcon", GetRewardIcon(GetPreviewReward(i + 1)), Color.white, new Vector2(0f, 24f), new Vector2(64f, 64f), Image.Type.Simple);
                }

                icon.sprite = GetRewardIcon(GetPreviewReward(i + 1));
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.rectTransform.anchoredPosition = GetRewardIconPosition(i + 1);
                icon.rectTransform.sizeDelta = i == 6 ? new Vector2(72f, 72f) : new Vector2(58f, 58f);

                StyleCardText(card, i + 1);

                Image glow = card.Find("ActiveGlow")?.GetComponent<Image>();
                if (glow != null)
                {
                    Color color = glow.color;
                    color.a = 0f;
                    glow.color = color;
                    glow.gameObject.SetActive(false);
                }
            }
        }

        private static void StyleCardText(Transform card, int day)
        {
            Vector2 size = GetRewardCardSize(day);
            float halfHeight = size.y * 0.5f;
            SetTextRect(card.Find("Day") as RectTransform, new Vector2(0f, halfHeight + 16f), new Vector2(size.x - 18f, 50f));
            SetTextRect(card.Find("Amount") as RectTransform, GetRewardAmountPosition(day), new Vector2(size.x - 30f, day == 7 ? 70f : 48f));
            SetTextRect(card.Find("Label") as RectTransform, GetRewardLabelPosition(day), new Vector2(size.x - 28f, day == 7 ? 58f : 42f));
            SetTextRect(card.Find("Status") as RectTransform, GetRewardStatusPosition(day), new Vector2(size.x + 18f, 32f));
            RectTransform checkRect = card.Find("Check") as RectTransform;
            Vector2 statusPosition = GetRewardStatusPosition(day);
            SetTextRect(checkRect, new Vector2(day == 7 ? -104f : -86f, statusPosition.y), new Vector2(26f, 22f));
            StyleCheckIcon(checkRect);

            TMP_Text dayText = card.Find("Day")?.GetComponent<TMP_Text>();
            if (dayText != null)
            {
                dayText.fontSizeMax = day == 7 ? 27f : 26f;
                dayText.fontSizeMin = 18f;
                dayText.lineSpacing = -13f;
                dayText.color = TextGold;
            }

            TMP_Text amountText = card.Find("Amount")?.GetComponent<TMP_Text>();
            if (amountText != null)
            {
                amountText.fontSizeMax = day == 7 ? 30f : 34f;
                amountText.fontSizeMin = 19f;
                amountText.color = TextCream;
                amountText.lineSpacing = day == 7 ? -6f : 0f;
            }

            TMP_Text labelText = card.Find("Label")?.GetComponent<TMP_Text>();
            if (labelText != null)
            {
                labelText.fontSizeMax = day == 7 ? 18f : 21f;
                labelText.fontSizeMin = 14f;
                labelText.lineSpacing = day == 7 ? -6f : -5f;
                labelText.color = TextCream;
            }

            TMP_Text statusText = card.Find("Status")?.GetComponent<TMP_Text>();
            if (statusText != null)
            {
                statusText.fontSizeMax = 15f;
                statusText.fontSizeMin = 10f;
            }

            ClearCheckText(card.Find("Check"));
        }

        private void UpgradeCurrentRewardIcon()
        {
            RectTransform frame = transform.Find("Panel/CurrentRewardPanel/IconFrame") as RectTransform;
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
                icon = CreateImage(frame, "RewardIcon", PremiumUISprites.HintBulbIcon, Color.white, Vector2.zero, new Vector2(132f, 132f), Image.Type.Simple);
            }

            icon.sprite = PremiumUISprites.HintBulbIcon;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.rectTransform.sizeDelta = new Vector2(132f, 132f);
            icon.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void StyleCurrentRewardPanel()
        {
            RectTransform rect = transform.Find("Panel/CurrentRewardPanel") as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = CurrentPanelSize;
            rect.anchoredPosition = CurrentPanelPosition;
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CategorySlot != null ? PremiumUISprites.CategorySlot : PremiumUISprites.EmeraldPanel;
                image.type = Image.Type.Sliced;
                image.color = DeepPanelGreen;
            }

            Outline outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.72f, 0.18f, 0.46f);
            outline.effectDistance = new Vector2(1f, -1f);

            CreateCurrentPanelInset(rect);
            CreateCurrentPanelWash(rect);
            CreatePanelCornerDecorations(rect);
            SetTextRect(rect.Find("Title") as RectTransform, new Vector2(0f, 228f), new Vector2(354f, 46f));
            SetTextRect(rect.Find("IconFrame") as RectTransform, new Vector2(0f, 88f), new Vector2(214f, 214f));
            SetTextRect(rect.Find("RewardTitle") as RectTransform, new Vector2(0f, -72f), new Vector2(350f, 64f));
            SetTextRect(rect.Find("Description") as RectTransform, new Vector2(0f, -146f), new Vector2(350f, 82f));
            SetTextRect(rect.Find("ClaimButton") as RectTransform, new Vector2(0f, -222f), new Vector2(292f, 70f));
            SetTextRect(rect.Find("Hint") as RectTransform, new Vector2(0f, -270f), new Vector2(340f, 30f));

            TMP_Text title = rect.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.fontSizeMax = 28f;
                title.fontSizeMin = 20f;
            }

            TMP_Text reward = rect.Find("RewardTitle")?.GetComponent<TMP_Text>();
            if (reward != null)
            {
                reward.fontSizeMax = 34f;
                reward.fontSizeMin = 22f;
            }

            TMP_Text description = rect.Find("Description")?.GetComponent<TMP_Text>();
            if (description != null)
            {
                description.fontSizeMax = 22f;
                description.fontSizeMin = 16f;
            }

            TMP_Text hint = rect.Find("Hint")?.GetComponent<TMP_Text>();
            if (hint != null)
            {
                hint.fontSizeMax = 15f;
                hint.fontSizeMin = 12f;
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

            rect.sizeDelta = new Vector2(316f, 78f);
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            RectTransform check = CreateCheckIcon(rect, new Vector2(-108f, 0f), new Vector2(30f, 24f));
            check.gameObject.SetActive(false);
            SetTextRect(rect.Find("Label") as RectTransform, Vector2.zero, rect.sizeDelta - new Vector2(58f, 20f));
        }

        private static void SetActiveIfExists(Transform parent, string childName, bool active)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static void CreateRewardCard(RectTransform parent, int day)
        {
            Vector2 position = GetRewardCardPosition(day);
            Vector2 size = GetRewardCardSize(day);
            RectTransform card = CreateRect(parent, $"Day{day}Card", Anchor.Center, Anchor.Center, size, position);
            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = RoyalEventSprites.RewardCard;
            image.type = Image.Type.Sliced;
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            card.gameObject.AddComponent<UIJuicyButton>();

            Image glow = CreateImage(card, "ActiveGlow", RoyalEventSprites.RewardCard, new Color(1f, 0.78f, 0.1f, 0f), Vector2.zero, size + new Vector2(24f, 28f), Image.Type.Sliced);
            glow.gameObject.SetActive(false);

            CreateText(card, "Day", $"ДЕНЬ\n{day}", day == 7 ? 27f : 26f, TextGold, FontStyles.Bold, new Vector2(0f, size.y * 0.5f + 16f), new Vector2(size.x - 18f, 50f));
            TMP_Text oldIcon = CreateText(card, "Icon", string.Empty, day == 7 ? 55f : 50f, TextGold, FontStyles.Bold, GetRewardIconPosition(day), new Vector2(size.x - 32f, 64f));
            oldIcon.gameObject.SetActive(false);
            CreateText(card, "Amount", day switch { 1 => "50", 2 => "75", 3 => "1", 4 => "100", 5 => "+5", 6 => "150", _ => "300\n+1" }, day == 7 ? 30f : 34f, TextCream, FontStyles.Bold, GetRewardAmountPosition(day), new Vector2(size.x - 30f, day == 7 ? 70f : 48f));
            CreateText(card, "Label", day switch { 3 => "ПОДСКАЗКА", 5 => "ХОДОВ", 7 => "МОНЕТ\nПОДСКАЗКА", _ => "МОНЕТ" }, day == 7 ? 18f : 21f, TextCream, FontStyles.Bold, GetRewardLabelPosition(day), new Vector2(size.x - 28f, day == 7 ? 58f : 42f));
            CreateText(card, "Status", day <= 2 ? "ПОЛУЧЕНО" : day == 3 ? "СЕГОДНЯ" : "ЗАБЛОКИРОВАНО", 15f, day <= 2 ? TextGreen : TextGold, FontStyles.Bold, GetRewardStatusPosition(day), new Vector2(size.x + 18f, 32f));
            Vector2 statusPosition = GetRewardStatusPosition(day);
            CreateCheckIcon(card, new Vector2(day == 7 ? -104f : -86f, statusPosition.y), new Vector2(26f, 22f));
        }

        private static Vector2 GetRewardCardPosition(int day)
        {
            return day switch
            {
                1 => new Vector2(-390f, TopRewardRowY),
                2 => new Vector2(-130f, TopRewardRowY),
                3 => new Vector2(130f, TopRewardRowY),
                4 => new Vector2(390f, TopRewardRowY),
                5 => new Vector2(-390f, BottomRewardRowY),
                6 => new Vector2(-130f, BottomRewardRowY),
                _ => new Vector2(245f, BottomRewardRowY)
            };
        }

        private static Vector2 GetRewardCardSize(int day)
        {
            return day == 7 ? FinalRewardCardSize : StandardRewardCardSize;
        }

        private static Vector2 GetRewardIconPosition(int day)
        {
            return day == 7 ? new Vector2(0f, 34f) : new Vector2(0f, 24f);
        }

        private static Vector2 GetRewardAmountPosition(int day)
        {
            return day == 7 ? new Vector2(0f, -24f) : new Vector2(0f, -24f);
        }

        private static Vector2 GetRewardLabelPosition(int day)
        {
            return day == 7 ? new Vector2(0f, -86f) : new Vector2(0f, -76f);
        }

        private static Vector2 GetRewardStatusPosition(int day)
        {
            float halfHeight = GetRewardCardSize(day).y * 0.5f;
            return new Vector2(0f, -halfHeight - CardStatusGap);
        }

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
                inset = CreateImage(panel, "InnerEmeraldPanel", PremiumUISprites.EmeraldPanel, new Color(0.06f, 0.34f, 0.16f, 0.42f), Vector2.zero, panel.sizeDelta - new Vector2(64f, 70f), Image.Type.Sliced);
            }

            inset.sprite = PremiumUISprites.EmeraldPanel;
            inset.type = Image.Type.Sliced;
            inset.color = new Color(0.06f, 0.34f, 0.16f, 0.42f);
            inset.raycastTarget = false;
            inset.rectTransform.anchoredPosition = Vector2.zero;
            inset.rectTransform.sizeDelta = panel.sizeDelta - new Vector2(64f, 70f);
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
                wash = CreateImage(panel, "InnerGreenWash", null, new Color(0.025f, 0.22f, 0.105f, 0.52f), new Vector2(0f, 8f), panel.sizeDelta - new Vector2(96f, 112f), Image.Type.Simple);
            }

            wash.sprite = null;
            wash.type = Image.Type.Simple;
            wash.color = new Color(0.025f, 0.22f, 0.105f, 0.52f);
            wash.raycastTarget = false;
            wash.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            wash.rectTransform.sizeDelta = panel.sizeDelta - new Vector2(96f, 112f);
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

            Image icon = panel.Find("HeaderIcon")?.GetComponent<Image>();
            if (icon == null)
            {
                icon = CreateImage(panel, "HeaderIcon", GetDailyRewardsHeaderSprite(), Color.white, new Vector2(0f, HeaderIconY), new Vector2(190f, 190f), Image.Type.Simple);
            }

            icon.sprite = GetDailyRewardsHeaderSprite();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.anchoredPosition = new Vector2(0f, HeaderIconY);
            icon.rectTransform.sizeDelta = new Vector2(190f, 190f);
            icon.transform.SetAsLastSibling();
        }

        private static Sprite GetDailyRewardsHeaderSprite()
        {
            Sprite sprite = null;
#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Button/QuestIcon.png");
#endif
            return sprite != null ? sprite : PremiumUISprites.CategoryLabel;
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
            image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, 38f, TextDark, FontStyles.Bold, Vector2.zero, size - new Vector2(48f, 24f));
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

                shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            foreach (Outline outline in root.GetComponentsInChildren<Outline>(true))
            {
                if (outline.gameObject.name == "CurrentRewardPanel")
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

        private static void DisableLegacyHeaderCrown(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "HeaderIcon")
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
