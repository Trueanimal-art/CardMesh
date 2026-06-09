using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class ChestChasePopup : MonoBehaviour
    {
        private static readonly Color TextDark = new(0.16f, 0.09f, 0.03f, 1f);
        private static readonly Color TextCream = new(1f, 0.95f, 0.76f, 1f);
        private static readonly Color TextGold = new(1f, 0.82f, 0.24f, 1f);

        private ChestChaseManager manager;
        private CanvasGroup canvasGroup;
        private RectTransform panel;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI progressCountText;
        private TextMeshProUGUI progressLabelText;
        private Image progressFill;
        private RectTransform progressTrack;
        private RectTransform stageRow;
        private RectTransform tooltip;
        private TextMeshProUGUI tooltipTitleText;
        private TextMeshProUGUI tooltipCoinText;
        private TextMeshProUGUI tooltipHintText;
        private TextMeshProUGUI tooltipUndoText;
        private TextMeshProUGUI rewardToastText;
        private Coroutine timerRoutine;

        public static ChestChasePopup GetOrCreate()
        {
            RuntimeCyrillicFontProvider.WarmUp("КОРОЛЕВСКАЯ СЕРИЯ Побеждай уровни подряд и получай всё больше наград. При проигрыше серия сбрасывается. Побед подряд Монеты Подсказки Отмены хода");

            ChestChasePopup existing = FindFirstObjectByType<ChestChasePopup>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = new("ChestChaseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 1f;
            }

            GameObject root = new("ChestChasePopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            Stretch(rect);

            ChestChasePopup popup = root.AddComponent<ChestChasePopup>();
            popup.BuildLayout();
            root.SetActive(false);
            return popup;
        }

        public void Show(ChestChaseManager targetManager)
        {
            manager = targetManager != null ? targetManager : ChestChaseManager.GetOrCreate(this);

            if (manager != null)
            {
                manager.StateChanged -= Refresh;
                manager.StateChanged += Refresh;
                manager.RewardClaimed -= ShowRewardToast;
                manager.RewardClaimed += ShowRewardToast;
            }

            Refresh();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartShowAnimation();

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
            }

            timerRoutine = StartCoroutine(UpdateTimer());
        }

        private void OnDisable()
        {
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private void BuildLayout()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            Image overlay = GetComponent<Image>();
            overlay.color = new Color(0f, 0.035f, 0.019f, 0.9f);
            overlay.raycastTarget = true;

            Vector2 panelSize = CalculatePanelSize();
            bool landscapeLayout = panelSize.x > panelSize.y;
            panel = CreateRect("Panel", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), panelSize, new Vector2(0f, 10f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
            panelImage.type = panelImage.sprite != null && panelImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(0f, 0.01f, 0f, 0.58f), new Vector2(0f, -14f));

            TextMeshProUGUI title = CreateText(panel, "Title", "КОРОЛЕВСКАЯ СЕРИЯ", 58f, TextGold, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(980f, 82f), new Vector2(0f, -86f));
            AddTextOutline(title, new Color(0.25f, 0.1f, 0f, 0.62f), new Vector2(1.5f, -1.5f));
            AddTextShadow(title.gameObject, new Color(1f, 0.72f, 0.12f, 0.18f), new Vector2(0f, 2f));

            TextMeshProUGUI description = CreateText(panel, "Description", "Побеждай уровни подряд и получай всё больше наград.\nПри проигрыше серия сбрасывается.", 28f, TextCream, TextAlignmentOptions.Center);
            description.fontStyle = FontStyles.Bold;
            SetAnchored(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 80f), new Vector2(0f, -152f));
            AddTextOutline(description, new Color(0f, 0.02f, 0f, 0.62f), new Vector2(1f, -1f));

            Button closeButton = CreateCircleButton(panel, "CloseButton", "X", new Color(0.95f, 0.58f, 0.05f, 1f), TextDark, Close);
            SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), new Vector2(-70f, -64f));

            Button infoButton = CreateCircleButton(panel, "InfoButton", "i", new Color(0.06f, 0.31f, 0.14f, 1f), TextGold, ShowRules);
            SetAnchored(infoButton.transform as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f), new Vector2(82f, -82f));

            RectTransform timer = CreateRect("Timer", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(222f, 64f), new Vector2(0f, -236f));
            Image timerImage = timer.gameObject.AddComponent<Image>();
            timerImage.sprite = PremiumUISprites.PrimaryButton != null ? PremiumUISprites.PrimaryButton : RoyalEventSprites.Tooltip;
            timerImage.type = timerImage.sprite != null && timerImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            timerImage.color = Color.white;
            timerText = CreateText(timer, "TimerText", string.Empty, 30f, TextDark, TextAlignmentOptions.Center);
            timerText.fontStyle = FontStyles.Bold;
            Stretch(timerText.rectTransform, 16f, 16f, 0f, 1f);

            RectTransform hero = CreateRect("Hero", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), landscapeLayout ? new Vector2(800f, 382f) : new Vector2(700f, 336f), new Vector2(0f, landscapeLayout ? -392f : -486f));
            Image heroImage = hero.gameObject.AddComponent<Image>();
            heroImage.sprite = RoyalEventSprites.Hero;
            heroImage.color = Color.white;
            heroImage.raycastTarget = false;
            AddPulse(hero.gameObject, 0.012f, 0f, 0.85f, 0f);

            RectTransform progressBadge = CreateRect("ProgressBadge", hero, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(274f, 78f), new Vector2(0f, 78f));
            Image badgeImage = progressBadge.gameObject.AddComponent<Image>();
            badgeImage.sprite = PremiumUISprites.SecondaryButton != null ? PremiumUISprites.SecondaryButton : PremiumUISprites.EmeraldPanel;
            badgeImage.type = badgeImage.sprite != null && badgeImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            badgeImage.color = Color.white;
            AddShadow(progressBadge.gameObject, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -5f));

            progressCountText = CreateText(progressBadge, "ProgressCount", string.Empty, 42f, TextCream, TextAlignmentOptions.Center);
            progressCountText.fontStyle = FontStyles.Bold;
            Stretch(progressCountText.rectTransform, 18f, 18f, 2f, 8f);
            AddTextOutline(progressCountText, new Color(0f, 0.02f, 0f, 0.55f), new Vector2(1.2f, -1.2f));

            progressLabelText = CreateText(panel, "ProgressLabel", "ПОБЕД ПОДРЯД", 24f, TextCream, TextAlignmentOptions.Center);
            progressLabelText.fontStyle = FontStyles.Bold;
            SetAnchored(progressLabelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(340f, 44f), new Vector2(0f, landscapeLayout ? -548f : -652f));
            AddTextOutline(progressLabelText, new Color(0f, 0.02f, 0f, 0.5f), new Vector2(1f, -1f));

            float progressWidth = landscapeLayout ? 1010f : 700f;
            RectTransform progressBack = CreateRect("ProgressBack", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(progressWidth, 46f), new Vector2(0f, landscapeLayout ? -604f : -720f));
            progressTrack = progressBack;
            Image progressBackImage = progressBack.gameObject.AddComponent<Image>();
            progressBackImage.sprite = RoyalEventSprites.ProgressBack != null ? RoyalEventSprites.ProgressBack : PremiumUISprites.HudCounterGold;
            progressBackImage.type = progressBackImage.sprite != null && progressBackImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            progressBackImage.color = Color.white;

            progressFill = CreateRect("ProgressFill", progressBack, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 38f), Vector2.zero).gameObject.AddComponent<Image>();
            progressFill.sprite = RoyalEventSprites.ProgressFill != null ? RoyalEventSprites.ProgressFill : PremiumUISprites.SecondaryButton;
            progressFill.type = progressFill.sprite != null && progressFill.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            progressFill.color = Color.white;

            CreateProgressMilestones(progressBack);

            CreateSparkles(panel, landscapeLayout);

            stageRow = CreateRect("StageRow", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), landscapeLayout ? new Vector2(1160f, 252f) : new Vector2(800f, 224f), new Vector2(0f, landscapeLayout ? 138f : 218f));

            tooltip = CreateRect("RewardTooltip", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), landscapeLayout ? new Vector2(322f, 96f) : new Vector2(340f, 108f), new Vector2(0f, landscapeLayout ? 294f : 348f));
            Image tooltipImage = tooltip.gameObject.AddComponent<Image>();
            tooltipImage.sprite = RoyalEventSprites.Tooltip != null ? RoyalEventSprites.Tooltip : PremiumUISprites.CreamPanel;
            tooltipImage.type = tooltipImage.sprite != null && tooltipImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            tooltipImage.color = Color.white;
            AddShadow(tooltip.gameObject, new Color(0.1f, 0.04f, 0f, 0.34f), new Vector2(0f, -6f));

            tooltipTitleText = CreateText(tooltip, "Title", "Награда", 18f, TextDark, TextAlignmentOptions.Center);
            tooltipTitleText.fontStyle = FontStyles.Bold;
            SetAnchored(tooltipTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(276f, 24f), new Vector2(0f, -12f));
            tooltipCoinText = CreateTooltipRow(tooltip, "Coins", RoyalEventSprites.CoinIcon, new Vector2(-94f, -55f));
            tooltipHintText = CreateTooltipRow(tooltip, "Hints", PremiumUISprites.HintBulbIcon, new Vector2(0f, -55f));
            tooltipUndoText = CreateTooltipRow(tooltip, "Undos", PremiumUISprites.UndoIcon, new Vector2(94f, -55f));

            rewardToastText = CreateText(panel, "RewardToast", string.Empty, 28f, TextGold, TextAlignmentOptions.Center);
            rewardToastText.fontStyle = FontStyles.Bold;
            SetAnchored(rewardToastText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(720f, 64f), new Vector2(0f, 72f));
            AddTextOutline(rewardToastText, new Color(0f, 0.02f, 0f, 0.55f), new Vector2(1.2f, -1.2f));
            rewardToastText.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (manager == null)
            {
                return;
            }

            int required = Mathf.Max(1, manager.KeysRequired);
            int current = Mathf.Clamp(manager.CurrentKeys, 0, required);

            SetText(progressCountText, $"{current}/{required}");
            SetText(progressLabelText, "ПОБЕД ПОДРЯД");

            if (progressFill != null)
            {
                RectTransform track = progressFill.transform.parent as RectTransform;
                float trackWidth = track != null ? Mathf.Max(track.rect.width, track.sizeDelta.x) : 720f;
                float trackHeight = track != null ? Mathf.Max(track.rect.height, track.sizeDelta.y) : 62f;
                float width = Mathf.Lerp(10f, Mathf.Max(10f, trackWidth - 12f), current / (float)required);
                progressFill.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(28f, trackHeight - 8f));
            }

            RefreshProgressMarkers(current, required);
            RebuildStages();
            ShowTooltipForStage(manager.CurrentStage);
            UpdateTimerText();
        }

        private void RebuildStages()
        {
            if (stageRow == null || manager == null)
            {
                return;
            }

            foreach (Transform child in stageRow)
            {
                Destroy(child.gameObject);
            }

            IReadOnlyList<ChestChaseRewardTier> tiers = manager.Tiers;
            int count = tiers != null ? tiers.Count : 0;
            if (count <= 0)
            {
                return;
            }

            float rowWidth = Mathf.Max(stageRow.rect.width, stageRow.sizeDelta.x);
            bool wide = rowWidth > 1000f;
            Vector2 stageSize = wide ? new Vector2(194f, 254f) : new Vector2(148f, 208f);
            float usableWidth = wide ? Mathf.Min(1010f, rowWidth - 150f) : 660f;
            float step = count > 1 ? usableWidth / (count - 1) : 0f;
            float startX = -step * (count - 1) * 0.5f;
            for (int index = 0; index < count; index++)
            {
                int stageIndex = index;
                RectTransform stage = CreateRect($"Stage_{index + 1}", stageRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), stageSize, new Vector2(startX + (step * index), 0f));
                Button button = stage.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => ShowTooltipForStage(stageIndex));
                if (button.GetComponent<UIJuicyButton>() == null)
                {
                    button.gameObject.AddComponent<UIJuicyButton>();
                }

                bool isCompleted = index < manager.CurrentStage;
                bool isCurrent = index == manager.CurrentStage;
                CreateRewardStageCard(stage, tiers[index], index + 1, isCompleted, isCurrent);
            }
        }

        private void ShowTooltipForStage(int stageIndex)
        {
            if (manager == null || tooltip == null)
            {
                return;
            }

            ChestChaseRewardTier reward = manager.GetTier(stageIndex);
            if (reward == null)
            {
                tooltip.gameObject.SetActive(false);
                return;
            }

            tooltip.gameObject.SetActive(true);
            SetText(tooltipTitleText, "Награда");
            SetText(tooltipCoinText, $"x{reward.Coins}");
            SetText(tooltipHintText, $"x{reward.Hints}");
            SetText(tooltipUndoText, reward.Undos > 0 ? $"x{reward.Undos}" : "-");

            IReadOnlyList<ChestChaseRewardTier> tiers = manager.Tiers;
            int count = tiers != null ? Mathf.Max(1, tiers.Count) : 1;
            float normalized = count <= 1 ? 0.5f : stageIndex / (float)(count - 1);
            float rowWidth = stageRow != null ? Mathf.Max(stageRow.rect.width, stageRow.sizeDelta.x) : 820f;
            float x = Mathf.Lerp(rowWidth > 1000f ? -480f : -306f, rowWidth > 1000f ? 480f : 306f, normalized);
            tooltip.anchoredPosition = new Vector2(x, tooltip.anchoredPosition.y);
        }

        private IEnumerator UpdateTimer()
        {
            while (gameObject.activeSelf)
            {
                UpdateTimerText();
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private void UpdateTimerText()
        {
            if (timerText == null || manager == null)
            {
                return;
            }

            TimeSpan remaining = manager.TimeRemaining;
            string value = remaining.TotalDays >= 1d
                ? $"{Mathf.FloorToInt((float)remaining.TotalDays)}д {remaining.Hours}ч"
                : $"{remaining.Hours}ч {remaining.Minutes}м";
            SetText(timerText, value);
        }

        private void ShowRewardToast(ChestChaseRewardTier reward)
        {
            if (rewardToastText == null || reward == null)
            {
                return;
            }

            SetText(rewardToastText, $"Получено: монеты {reward.Coins}, подсказки {reward.Hints}, отмены {reward.Undos}");
            rewardToastText.gameObject.SetActive(true);
            StopCoroutine(nameof(HideRewardToastAfterDelay));
            StartCoroutine(nameof(HideRewardToastAfterDelay));
        }

        private IEnumerator HideRewardToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.6f);
            if (rewardToastText != null)
            {
                rewardToastText.gameObject.SetActive(false);
            }
        }

        private void ShowRules()
        {
            ShowTooltipForStage(manager != null ? manager.CurrentStage : 0);
            if (rewardToastText != null)
            {
                SetText(rewardToastText, "1 победа продвигает серию. При поражении прогресс серии сбрасывается.");
                rewardToastText.gameObject.SetActive(true);
                StopCoroutine(nameof(HideRewardToastAfterDelay));
                StartCoroutine(nameof(HideRewardToastAfterDelay));
            }
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void StartShowAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }

        private static void CreateProgressMilestones(RectTransform parent)
        {
            for (int index = 0; index < 5; index++)
            {
                float normalized = index / 4f;
                RectTransform mark = CreateRect($"Milestone_{index + 1}", parent, new Vector2(normalized, 0.5f), new Vector2(normalized, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(42f, 42f), Vector2.zero);
                mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image image = mark.gameObject.AddComponent<Image>();
                image.sprite = RoyalEventSprites.ProgressMarker != null ? RoyalEventSprites.ProgressMarker : PremiumUISprites.IconCircle;
                image.color = new Color(1f, 1f, 1f, 0.62f);
                image.raycastTarget = false;
            }
        }

        private void RefreshProgressMarkers(int current, int required)
        {
            if (progressTrack == null)
            {
                return;
            }

            for (int index = 0; index < 5; index++)
            {
                Image marker = progressTrack.Find($"Milestone_{index + 1}")?.GetComponent<Image>();
                if (marker == null)
                {
                    continue;
                }

                int threshold = Mathf.RoundToInt(required * (index / 4f));
                bool active = current >= threshold;
                marker.color = active
                    ? new Color(1f, 1f, 1f, index == 0 || current == threshold ? 0.95f : 0.82f)
                    : new Color(1f, 0.9f, 0.52f, 0.42f);
            }
        }

        private static void CreateRewardStageCard(RectTransform root, ChestChaseRewardTier reward, int stageNumber, bool completed, bool current)
        {
            bool large = root.sizeDelta.x > 180f;
            RectTransform glow = CreateRect("Glow", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), large ? new Vector2(36f, 36f) : new Vector2(24f, 24f), Vector2.zero);
            Image glowImage = glow.gameObject.AddComponent<Image>();
            glowImage.sprite = RoyalEventSprites.RewardCardActive != null ? RoyalEventSprites.RewardCardActive : RoyalEventSprites.RewardCard;
            glowImage.color = current ? new Color(1f, 1f, 1f, 0.34f) : new Color(1f, 1f, 1f, 0f);
            glowImage.raycastTarget = false;
            if (current)
            {
                AddPulse(glow.gameObject, 0.028f, 0.22f, 1.05f, stageNumber * 0.4f);
            }

            RectTransform card = CreateRect("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(180f, 236f) : new Vector2(142f, 198f), new Vector2(0f, large ? -4f : -8f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = current ? RoyalEventSprites.RewardCardActive : RoyalEventSprites.RewardCard;
            cardImage.color = current ? Color.white : completed ? new Color(0.9f, 1f, 0.82f, 1f) : new Color(0.74f, 0.82f, 0.7f, 1f);
            cardImage.raycastTarget = false;

            TextMeshProUGUI stageLabel = CreateText(root, "StageLabel", Roman(stageNumber), large ? 30f : 26f, TextGold, TextAlignmentOptions.Center);
            stageLabel.fontStyle = FontStyles.Bold;
            SetAnchored(stageLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(68f, 42f), new Vector2(0f, large ? -8f : -12f));
            AddTextOutline(stageLabel, new Color(0.18f, 0.06f, 0f, 0.65f), new Vector2(1.2f, -1.2f));

            CreateRewardLine(card, "Coins", RoyalEventSprites.CoinIcon, reward != null ? reward.Coins.ToString() : "0", large ? 64f : 46f, large);
            CreateRewardLine(card, "Hints", PremiumUISprites.HintBulbIcon, reward != null ? reward.Hints.ToString() : "0", large ? 2f : -2f, large);
            if (reward != null && reward.Undos > 0)
            {
                CreateRewardLine(card, "Undo", PremiumUISprites.UndoIcon, reward.Undos.ToString(), large ? -60f : -50f, large);
            }

            if (completed)
            {
                RectTransform done = CreateRect("CompletedGem", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), large ? new Vector2(54f, 54f) : new Vector2(46f, 46f), new Vector2(0f, large ? 18f : 14f));
                Image doneImage = done.gameObject.AddComponent<Image>();
                doneImage.sprite = PremiumUISprites.IconCircle != null ? PremiumUISprites.IconCircle : RoyalEventSprites.CoinIcon;
                doneImage.color = new Color(0.18f, 1f, 0.28f, 1f);
                doneImage.raycastTarget = false;
            }
        }

        private static void CreateRewardLine(RectTransform parent, string name, Sprite iconSprite, string value, float y, bool large)
        {
            RectTransform row = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(142f, 46f) : new Vector2(112f, 38f), new Vector2(0f, y));
            RectTransform icon = CreateRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(42f, 42f) : new Vector2(34f, 34f), new Vector2(large ? 26f : 22f, 0f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = iconSprite != null ? iconSprite : PremiumUISprites.IconCircle;
            image.color = Color.white;
            image.raycastTarget = false;

            TextMeshProUGUI label = CreateText(row, "Value", value, large ? 36f : 30f, TextCream, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            SetAnchored(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(large ? -58f : -48f, 0f), new Vector2(large ? 68f : 58f, 0f));
            AddTextOutline(label, new Color(0f, 0.02f, 0f, 0.6f), new Vector2(1f, -1f));
        }

        private static TextMeshProUGUI CreateTooltipRow(RectTransform parent, string name, Sprite iconSprite, Vector2 position)
        {
            RectTransform row = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(86f, 36f), position);
            RectTransform icon = CreateRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(25f, 25f), new Vector2(16f, 0f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = iconSprite != null ? iconSprite : PremiumUISprites.IconCircle;
            image.color = Color.white;
            image.raycastTarget = false;

            TextMeshProUGUI text = CreateText(row, "Text", string.Empty, 20f, TextDark, TextAlignmentOptions.Left);
            text.fontStyle = FontStyles.Bold;
            SetAnchored(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(-34f, 0f), new Vector2(36f, 0f));
            text.lineSpacing = 0f;
            return text;
        }

        private static string Roman(int number)
        {
            return number switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                _ => number.ToString()
            };
        }

        private static Button CreateCircleButton(Transform parent, string name, string label, Color color, Color textColor, UnityAction action)
        {
            Button button = CreateImageButton(parent, name, label, PremiumUISprites.IconCircle, color, textColor, 36f);
            button.onClick.AddListener(action);
            return button;
        }

        private static Button CreateImageButton(Transform parent, string name, string label, Sprite sprite, Color color, Color textColor, float fontSize)
        {
            RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 72f), Vector2.zero);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : PremiumUISprites.CreamPanel;
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            AddShadow(root.gameObject, new Color(0f, 0.02f, 0f, 0.34f), new Vector2(0f, -6f));

            TextMeshProUGUI text = CreateText(root, "Label", label, fontSize, textColor, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            Stretch(text.rectTransform, 8f, 8f, 0f, 2f);
            AddTextOutline(text, new Color(0.18f, 0.06f, 0f, 0.5f), new Vector2(1f, -1f));

            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(text, value);
            SetText(text, value);
            return text;
        }

        private static Vector2 CalculatePanelSize()
        {
            float screenWidth = Mathf.Max(Screen.width, 1080f);
            float screenHeight = Mathf.Max(Screen.height, 1080f);
            bool landscape = screenWidth >= screenHeight;
            if (landscape)
            {
                return new Vector2(
                    Mathf.Clamp(screenWidth * 0.78f, 1220f, 1520f),
                    Mathf.Clamp(screenHeight * 0.78f, 820f, 940f));
            }

            return new Vector2(
                Mathf.Clamp(screenWidth * 0.9f, 860f, 980f),
                Mathf.Clamp(screenHeight * 0.74f, 1120f, 1260f));
        }

        private static void CreateSparkles(RectTransform parent, bool landscapeLayout)
        {
            Vector2[] positions = landscapeLayout
                ? new[]
                {
                    new Vector2(-480f, 160f),
                    new Vector2(-360f, -10f),
                    new Vector2(-210f, 210f),
                    new Vector2(260f, 186f),
                    new Vector2(430f, 24f),
                    new Vector2(540f, -122f),
                    new Vector2(-560f, -128f)
                }
                : new[]
                {
                    new Vector2(-300f, 260f),
                    new Vector2(300f, 220f),
                    new Vector2(-260f, -40f),
                    new Vector2(250f, -110f)
                };

            for (int index = 0; index < positions.Length; index++)
            {
                RectTransform sparkle = CreateRect($"Sparkle_{index + 1}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * (index % 2 == 0 ? 24f : 16f), positions[index]);
                Image image = sparkle.gameObject.AddComponent<Image>();
                image.sprite = RoyalEventSprites.ProgressMarker != null ? RoyalEventSprites.ProgressMarker : RoyalEventSprites.CoinIcon;
                image.color = new Color(1f, 0.9f, 0.28f, index % 2 == 0 ? 0.28f : 0.2f);
                image.raycastTarget = false;
                AddPulse(sparkle.gameObject, 0.12f, 0.25f, 0.75f + (index * 0.11f), index * 0.9f);
            }
        }

        private static void AddPulse(GameObject target, float scaleAmplitude, float alphaAmplitude, float speed, float phase)
        {
            if (target == null)
            {
                return;
            }

            RoyalEventPulse pulse = target.GetComponent<RoyalEventPulse>();
            if (pulse == null)
            {
                pulse = target.AddComponent<RoyalEventPulse>();
            }

            pulse.Configure(scaleAmplitude, alphaAmplitude, speed, phase);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            RuntimeCyrillicFontProvider.Apply(text, value);
            text.text = value ?? string.Empty;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject target = new(name, typeof(RectTransform));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchored(rect, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return rect;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddTextShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddTextOutline(TMP_Text text, Color color, Vector2 distance)
        {
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
