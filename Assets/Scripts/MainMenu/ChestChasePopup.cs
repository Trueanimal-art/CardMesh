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
        private static readonly Color ActiveMilestone = Color.white;
        private static readonly Color InactiveMilestone = new(0.52f, 0.76f, 0.56f, 0.88f);
        private static readonly Vector2 ActiveMilestoneSize = new(88f, 88f);
        private static readonly Vector2 InactiveMilestoneSize = new(20f, 20f);
        private static readonly Color StageCaptionColor = new(1f, 0.81f, 0.22f, 1f);
        private static readonly string[] StageRomanNumerals = { "I", "II", "III", "IV", "V" };

        // Readable layout on the authored 1020x1760 window grid (CanvasScaler
        // reference 1080x1920). All values are reference units; RoyalContent's
        // centre sits 920 units below the window top.
        private const float TitleFontSize = 54f;
        private const float BodyFontSize = 32f;
        private const float HeaderFontSize = 36f;
        private const float CurrencyFontSize = 36f;
        private const float CurrencyIconSize = 44f;
        private const float CurrencyIconGap = 10f;
        private const float CurrencyColumnSpacing = 240f;

        // Stage cards: royal_stage_* is sliced (56px border at 200 PPU), so on
        // a 248x362 card its inner gold line sits ~32 units in from the sides,
        // ~57 from the top and ~43 from the bottom. Content keeps 14 more.
        private const float StageCardWidth = 248f;
        private const float StageCardHeight = 362f;
        private const float StageColumnSpacing = 276f;
        private const float StageRowOffset = 172f;
        private const float StageContentWidth = 156f;
        private const float StageContentCenterY = -7f;
        private const float StageCaptionFontSize = 28f;
        private const float StageCaptionHeight = 36f;
        private const float StageCaptionGap = 12f;
        private const float StageRowHeight = 48f;
        private const float StageRowGap = 8f;
        private const float StageIconSize = 44f;
        private const float StageIconGap = 10f;
        private const float StageValueFontSize = 36f;
        private const float StageValueMinFontSize = 32f;

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
        private readonly List<IconValueBinding> currencyItems = new();
        private Transform windowRoot;

        private sealed class StageBinding
        {
            public RectTransform Card;
            public Image CardSurface;
            public Image Milestone;
            public TMP_Text CoinValue;
            public TMP_Text HintValue;
            public TMP_Text UndoValue;
            public TMP_Text Caption;
            public readonly IconValueBinding[] Rows = new IconValueBinding[3];
        }

        private sealed class IconValueBinding
        {
            public RectTransform Row;
            public RectTransform Icon;
            public TMP_Text Value;
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
            ApplyReadableLayout();
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

            windowRoot = window;
            stages.Clear();
            for (int i = 0; i < 5; i++)
            {
                Transform stage = window.Find($"RoyalContent/StageRow/Stage_{i + 1}");
                Transform milestone = window.Find($"RoyalContent/MilestoneTrack/Milestone_{i + 1}");
                StageBinding binding = new StageBinding
                {
                    Card = stage as RectTransform,
                    CardSurface = stage?.Find("CardFill")?.GetComponent<Image>(),
                    Milestone = milestone?.GetComponent<Image>(),
                    CoinValue = stage?.Find("RewardRow_1/Value")?.GetComponent<TMP_Text>(),
                    HintValue = stage?.Find("RewardRow_2/Value")?.GetComponent<TMP_Text>(),
                    UndoValue = stage?.Find("RewardRow_3/Value")?.GetComponent<TMP_Text>()
                };

                for (int row = 0; row < binding.Rows.Length; row++)
                {
                    binding.Rows[row] = BindIconValue(stage?.Find($"RewardRow_{row + 1}"));
                }

                stages.Add(binding);
            }

            currencyItems.Clear();
            for (int i = 0; i < 3; i++)
            {
                currencyItems.Add(BindIconValue(window.Find($"RoyalContent/GoalBlock/RewardPanel/Reward_{i + 1}")));
            }

            referencesCached = true;
            return true;
        }

        private static IconValueBinding BindIconValue(Transform row)
        {
            return new IconValueBinding
            {
                Row = row as RectTransform,
                Icon = row?.Find("Icon") as RectTransform,
                Value = row?.Find("Value")?.GetComponent<TMP_Text>()
            };
        }

        // The scene hierarchy is authored by RoyalSeriesSceneInstaller with
        // fine-print sizes (18-31 units). Re-apply a legible layout at runtime
        // so player builds do not depend on re-running the editor installer.
        private void ApplyReadableLayout()
        {
            Transform window = windowRoot;
            if (window == null)
            {
                return;
            }

            // Header: window top -> Header centre is 111 units down (pivot top at
            // -6, height 210). Title spans two lines so it can use the title size
            // without colliding with the close button (x >= 276, y <= 300).
            TMP_Text title = window.Find("Header/Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = "СУНДУКИ\nКОРОЛЕВСКОЙ СЕРИИ";
                ConfigureReadableText(title, TitleFontSize, TextAlignmentOptions.Center, false);
                title.lineSpacing = 0f;
                PlaceCentered(title.rectTransform, new Vector2(0f, -238f), new Vector2(826f, 130f));
            }

            TMP_Text subtitle = window.Find("Header/Subtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null)
            {
                subtitle.text = "Побеждай подряд и открывай\nвсё более ценные сундуки";
                ConfigureReadableText(subtitle, BodyFontSize, TextAlignmentOptions.Center, false);
                subtitle.lineSpacing = 0f;
                PlaceCentered(subtitle.rectTransform, new Vector2(0f, -350f), new Vector2(826f, 80f));
            }

            // Currency row: royal_goal_panel visual is 74 units tall on a 100 unit
            // rect (gold lines 12 from the top, 14 from the bottom).
            PlaceCentered(window.Find("RoyalContent/GoalBlock") as RectTransform, new Vector2(0f, 372f), new Vector2(840f, 120f));
            PlaceCentered(window.Find("RoyalContent/GoalBlock/RewardPanel") as RectTransform, Vector2.zero, new Vector2(720f, 100f));
            for (int i = 0; i < currencyItems.Count; i++)
            {
                IconValueBinding item = currencyItems[i];
                if (item.Row == null)
                {
                    continue;
                }

                PlaceCentered(item.Row, new Vector2((i - 1) * CurrencyColumnSpacing, 1f), item.Row.sizeDelta);
                ConfigureReadableText(item.Value, CurrencyFontSize, TextAlignmentOptions.Left, false);
                float width = MeasureWidth(item.Value);
                LayoutIconValue(item, CurrencyIconSize, CurrencyIconGap, width, 56f, 0f);
            }

            // Win counter: badge visual centre 655 units below the window top,
            // label 733, roman numerals 790, milestone jewels 850.
            PlaceCentered(window.Find("RoyalContent/ProgressBlock") as RectTransform, new Vector2(0f, 247f), new Vector2(520f, 150f));
            TMP_Text progressLabel = window.Find("RoyalContent/ProgressBlock/ProgressLabel")?.GetComponent<TMP_Text>();
            if (progressLabel != null)
            {
                ConfigureReadableText(progressLabel, HeaderFontSize, TextAlignmentOptions.Center, false);
                PlaceCentered(progressLabel.rectTransform, new Vector2(0f, -60f), new Vector2(420f, 46f));
            }

            PlaceCentered(window.Find("RoyalContent/MilestoneTrack") as RectTransform, new Vector2(0f, 73f), new Vector2(840f, 100f));
            for (int i = 0; i < stages.Count; i++)
            {
                Image milestone = stages[i].Milestone;
                TMP_Text roman = milestone != null ? milestone.transform.Find("Roman")?.GetComponent<TMP_Text>() : null;
                if (roman != null)
                {
                    ConfigureReadableText(roman, BodyFontSize, TextAlignmentOptions.Center, false);
                    // Keep the numeral clear of the active 88-unit jewel (visual +-30).
                    PlaceCentered(roman.rectTransform, new Vector2(0f, 60f), new Vector2(90f, 40f));
                }
            }

            // Stage cards: two centred rows (3 + 2), 276 units apart, starting
            // 44+ units below the active jewel.
            PlaceCentered(window.Find("RoyalContent/StageRow") as RectTransform, new Vector2(0f, -311f), new Vector2(840f, 2f * StageRowOffset + StageCardHeight));
            int topRowCount = Mathf.Min(3, stages.Count);
            int bottomRowCount = stages.Count - topRowCount;
            for (int i = 0; i < stages.Count; i++)
            {
                StageBinding stage = stages[i];
                if (stage.Card == null)
                {
                    continue;
                }

                bool topRow = i < topRowCount;
                int column = topRow ? i : i - topRowCount;
                int columns = topRow ? topRowCount : bottomRowCount;
                float x = (column - (columns - 1) * 0.5f) * StageColumnSpacing;
                float y = bottomRowCount > 0 ? (topRow ? StageRowOffset : -StageRowOffset) : 0f;
                Vector2 cardSize = new(StageCardWidth, StageCardHeight);
                PlaceCentered(stage.Card, new Vector2(x, y), cardSize);
                if (stage.CardSurface != null)
                {
                    PlaceCentered(stage.CardSurface.rectTransform, Vector2.zero, cardSize);
                }

                stage.Caption = EnsureStageCaption(stage.Card, stage.CoinValue);
                if (stage.Caption != null)
                {
                    stage.Caption.text = $"Сундук {StageRomanNumerals[Mathf.Min(i, StageRomanNumerals.Length - 1)]}";
                    stage.Caption.color = StageCaptionColor;
                    stage.Caption.raycastTarget = false;
                    ConfigureReadableText(stage.Caption, StageCaptionFontSize, TextAlignmentOptions.Center, false);
                }
            }

            TMP_Text toast = rewardToastText;
            if (toast != null)
            {
                ConfigureReadableText(toast, BodyFontSize, TextAlignmentOptions.Center, false);
                PlaceCentered(toast.rectTransform, new Vector2(0f, -702f), new Vector2(820f, 44f));
            }
        }

        // Centres the caption + three icon/value rows as one block inside the
        // card's inner gold frame. Called after values change because the
        // column width depends on the longest amount.
        private void LayoutStageCards()
        {
            float blockHeight = StageCaptionHeight + StageCaptionGap + 3f * StageRowHeight + 2f * StageRowGap;
            float top = StageContentCenterY + blockHeight * 0.5f;
            float captionY = top - StageCaptionHeight * 0.5f;
            float firstRowY = top - StageCaptionHeight - StageCaptionGap - StageRowHeight * 0.5f;

            foreach (StageBinding stage in stages)
            {
                if (stage.Card == null)
                {
                    continue;
                }

                if (stage.Caption != null)
                {
                    PlaceCentered(stage.Caption.rectTransform, new Vector2(0f, captionY), new Vector2(StageContentWidth, StageCaptionHeight));
                    stage.Caption.transform.SetAsLastSibling();
                }

                float maxValueWidth = StageContentWidth - StageIconSize - StageIconGap;
                float fontSize = StageValueFontSize;
                float textWidth = MeasureStageValues(stage, fontSize);
                if (textWidth > maxValueWidth)
                {
                    fontSize = Mathf.Max(StageValueMinFontSize, Mathf.Floor(fontSize * maxValueWidth / textWidth));
                    textWidth = Mathf.Min(maxValueWidth, MeasureStageValues(stage, fontSize));
                }

                for (int row = 0; row < stage.Rows.Length; row++)
                {
                    IconValueBinding binding = stage.Rows[row];
                    if (binding?.Row == null)
                    {
                        continue;
                    }

                    PlaceCentered(binding.Row, new Vector2(0f, firstRowY - row * (StageRowHeight + StageRowGap)), binding.Row.sizeDelta);
                    LayoutIconValue(binding, StageIconSize, StageIconGap, textWidth, StageRowHeight, 0f);
                }
            }
        }

        private static float MeasureStageValues(StageBinding stage, float fontSize)
        {
            float width = 0f;
            foreach (IconValueBinding binding in stage.Rows)
            {
                if (binding?.Value == null)
                {
                    continue;
                }

                ConfigureReadableText(binding.Value, fontSize, TextAlignmentOptions.Left, false);
                width = Mathf.Max(width, MeasureWidth(binding.Value));
            }

            return width;
        }

        private static TMP_Text EnsureStageCaption(RectTransform card, TMP_Text template)
        {
            Transform existing = card.Find("TierCaption");
            if (existing != null)
            {
                return existing.GetComponent<TMP_Text>();
            }

            if (template == null)
            {
                return null;
            }

            // A plain text clone of the card's own value label (same font and
            // material); the scene authors no caption object for the tiers.
            TMP_Text caption = Instantiate(template, card, false);
            caption.name = "TierCaption";
            caption.gameObject.SetActive(true);
            return caption;
        }

        // Lays out [icon][gap][value] as one group centred on the row's own
        // anchor. The value is left-aligned so stacked rows share one icon column.
        private static void LayoutIconValue(IconValueBinding binding, float iconSize, float gap, float textWidth, float rowHeight, float y)
        {
            if (binding?.Row == null)
            {
                return;
            }

            float groupWidth = iconSize + gap + textWidth;
            float left = -groupWidth * 0.5f;
            binding.Row.sizeDelta = new Vector2(groupWidth, rowHeight);

            if (binding.Icon != null)
            {
                PlaceCentered(binding.Icon, new Vector2(left + iconSize * 0.5f, y), new Vector2(iconSize, iconSize));
            }

            if (binding.Value != null)
            {
                RectTransform valueRect = binding.Value.rectTransform;
                valueRect.anchorMin = new Vector2(0.5f, 0.5f);
                valueRect.anchorMax = new Vector2(0.5f, 0.5f);
                valueRect.pivot = new Vector2(0f, 0.5f);
                valueRect.anchoredPosition = new Vector2(left + iconSize + gap, y);
                valueRect.sizeDelta = new Vector2(Mathf.Ceil(textWidth) + 2f, rowHeight);
                valueRect.localScale = Vector3.one;
                binding.Value.alignment = TextAlignmentOptions.Left;
            }
        }

        private static float MeasureWidth(TMP_Text text)
        {
            if (text == null)
            {
                return 0f;
            }

            string value = string.IsNullOrEmpty(text.text) ? "0" : text.text;
            return Mathf.Max(text.fontSize * 0.6f, text.GetPreferredValues(value).x);
        }

        private static void ConfigureReadableText(TMP_Text text, float size, TextAlignmentOptions alignment, bool wrap)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = false;
            text.fontSize = size;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = Vector4.zero;
        }

        private static void PlaceCentered(RectTransform rect, Vector2 position, Vector2 size)
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
                if (stage.CardSurface != null)
                {
                    Sprite cardSprite = active
                        ? CleanRoyalPopupSprites.RoyalStageActive
                        : CleanRoyalPopupSprites.RoyalStageIdle;
                    if (cardSprite != null)
                    {
                        stage.CardSurface.sprite = cardSprite;
                    }
                    stage.CardSurface.color = active
                        ? Color.white
                        : new Color(0.82f, 0.9f, 0.84f, 1f);
                }

                if (stage.Milestone != null)
                {
                    stage.Milestone.color = active ? ActiveMilestone : InactiveMilestone;
                    stage.Milestone.rectTransform.sizeDelta = active
                        ? ActiveMilestoneSize
                        : InactiveMilestoneSize;
                }
            }

            LayoutStageCards();
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
