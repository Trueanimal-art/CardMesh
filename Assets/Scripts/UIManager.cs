using System.Collections.Generic;
using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class UIManager : MonoBehaviour
    {
        private const float HudTopInset = 100f;
        private const float MovesTopInset = 242f;
        private const float LivesLeftInsetX = 154f;
        private const float LivesUnderMovesTopInset = 379f;
        private const float RightDeckInset = 269f;
        private const float DeckCardBackInsetX = 239f;
        private static readonly Vector2 GameplayCardSize = new(196f, 286f);
        private const float BottomButtonMaxWidth = 392f;
        private const float BottomButtonMinWidth = 210f;
        private const float BottomHudHeight = 178f;
        private const float BottomHudBottomInset = 40f;
        private const float BottomButtonYOffset = 3f;
        private const int HintCost = 900;

        [SerializeField] private TMP_Text movesLabel;
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text deckCounterLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject boosterBadge;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button rewardBonusButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button boosterDeckButton;

        private static Sprite roundedPanelSprite;
        private static Sprite roundedButtonSprite;
        private static Sprite roundSprite;
        private static Sprite vignetteSprite;
        private static Sprite heartSprite;

        private readonly HashSet<Button> wiredButtons = new();

        private SolitaireSceneRefs sceneRefs;
        private RectTransform safeAreaRoot;
        private RectTransform coinsPanel;
        private RectTransform movesPanel;
        private GameObject pausePopup;
        private GameObject restartConfirmationPopup;
        private GameObject livesEmptyPopup;
        private GameObject levelFailedPopup;
        private LivesHudView livesHudView;
        private TextMeshProUGUI livesEmptyTimerText;
        private TextMeshProUGUI movesBonusText;
        private CanvasGroup movesBonusGroup;
        private Coroutine movesBonusTextRoutine;
        private LivesManager activeLivesManager;
        private Coroutine livesEmptyTimerRoutine;
        private Action pauseRestartAction;
        private Action pauseMainMenuAction;
        private Coroutine coinsPulseRoutine;
        private RectTransform hintBadgeRoot;
        private TextMeshProUGUI hintBadgeText;
        private RectTransform undoBadgeRoot;
        private TextMeshProUGUI undoBadgeText;
        private RectTransform boosterBadgeRoot;
        private TextMeshProUGUI boosterBadgeText;
        private Coroutine hintBadgePulseRoutine;
        private Coroutine undoBadgePulseRoutine;
        private Coroutine boosterBadgePulseRoutine;
        private int hintCount;
        private int undoCount;
        private int boosterCount = 1;
        private int lastDisplayedMoves = 140;
        private bool hasDisplayedMoves;
        private TMP_FontAsset movesLabelFontAsset;
        private Material movesLabelFontMaterial;
        private TMP_FontAsset movesValueFontAsset;
        private Material movesValueFontMaterial;
        private TMP_FontAsset livesCountFontAsset;
        private Material livesCountFontMaterial;

        public void Configure(
            SolitaireSceneRefs sceneRefs,
            TextMeshProUGUI targetStatusLabel,
            Button targetRestartButton)
        {
            this.sceneRefs = sceneRefs;
            safeAreaRoot = sceneRefs != null ? sceneRefs.SafeAreaRoot : null;
            movesLabel = sceneRefs != null ? sceneRefs.MovesValue : null;
            coinsLabel = sceneRefs != null ? sceneRefs.CoinsText : null;
            deckCounterLabel = sceneRefs != null ? sceneRefs.DeckCounter : null;
            statusLabel = targetStatusLabel;
            restartButton = targetRestartButton;
            boosterBadge = sceneRefs != null ? sceneRefs.Badge : null;
            menuButton = sceneRefs != null ? sceneRefs.MenuButton : null;
            plusButton = sceneRefs != null ? sceneRefs.PlusButton : null;
            rewardBonusButton = sceneRefs != null ? sceneRefs.RewardBonusButton : null;
            rewardBonusButton ??= FindChildRecursive<Button>(safeAreaRoot, "RewardBonusButton");
            menuButton ??= rewardBonusButton;
            rewardBonusButton ??= menuButton;
            hintButton = sceneRefs != null ? sceneRefs.HintButton : null;
            undoButton = sceneRefs != null ? sceneRefs.UndoButton : null;
            boosterDeckButton = sceneRefs != null ? sceneRefs.BoosterDeckButton : null;
            CacheHudTextStyles();
        }

        public void Initialize(GameManager gameManager, IReadOnlyList<GameManager.ObjectiveState> objectiveStates)
        {
            ResetPauseState();
            ApplyGameplayPolish();

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                if (gameManager != null)
                {
                    restartButton.onClick.AddListener(gameManager.RequestRestartWithConfirmation);
                }
            }

            WirePauseButton(gameManager);
            SetButtonInteractable(rewardBonusButton, true);
            if (menuButton != rewardBonusButton)
            {
                WireButton(menuButton, "MenuButton", ShowPausePopup);
            }
            WireButton(plusButton, "PlusButton");
            WireButton(hintButton, "HintButton", gameManager != null ? (Action)gameManager.UseHintForTesting : null);
            WireButton(undoButton, "UndoButton", gameManager != null ? (Action)gameManager.TryUseUndo : null);
            WireButton(boosterDeckButton, "AddMovesButton", gameManager != null ? (Action)gameManager.ShowRewardedAddMoves : null);
            EnsureLivesHud(gameManager != null ? gameManager.LivesManager : null);
            ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            UpdateDeckCounter(0);
            SetBoosterBadgeVisible(false);
        }

        public void ResetPauseState()
        {
            Time.timeScale = 1f;

            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (restartConfirmationPopup != null)
            {
                restartConfirmationPopup.SetActive(false);
            }

            if (livesEmptyPopup != null)
            {
                livesEmptyPopup.SetActive(false);
            }

            if (levelFailedPopup != null)
            {
                levelFailedPopup.SetActive(false);
            }

            if (livesEmptyTimerRoutine != null)
            {
                StopCoroutine(livesEmptyTimerRoutine);
                livesEmptyTimerRoutine = null;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void UpdateMoves(int remainingMoves)
        {
            lastDisplayedMoves = remainingMoves;
            hasDisplayedMoves = true;

            if (safeAreaRoot == null && sceneRefs != null)
            {
                safeAreaRoot = sceneRefs.SafeAreaRoot;
            }

            if (movesLabel == null || movesLabel.name != "MovesValue" || !movesLabel.gameObject.activeInHierarchy)
            {
                RectTransform movesRoot = FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
                movesLabel = FindDirectText(movesRoot, "MovesValue") ?? FindChildRecursive<TMP_Text>(safeAreaRoot, "MovesValue");
            }

            if (movesLabel != null)
            {
                movesLabel.gameObject.SetActive(true);
                movesLabel.enabled = true;
                movesLabel.canvasRenderer.SetAlpha(1f);
                ApplyCachedTextStyle(movesLabel, movesValueFontAsset, movesValueFontMaterial);
                ApplyMovesValueStyle(movesLabel);
                movesLabel.SetText(remainingMoves.ToString());
                ApplyMovesValueStyle(movesLabel);
                movesLabel.ForceMeshUpdate(true, true);
            }
        }

        public void UpdateGoalsSummary(int completedGoals, int totalGoals)
        {
        }

        public void UpdateProgress(IReadOnlyList<GameManager.ObjectiveState> objectiveStates)
        {
        }

        public void UpdateCoins(int coins)
        {
            if (coinsLabel != null)
            {
                coinsLabel.text = coins.ToString();
            }
        }

        public void PlayCoinsAward(int totalCoins, int amount)
        {
            UpdateCoins(totalCoins);

            if (!isActiveAndEnabled || coinsLabel == null)
            {
                return;
            }

            if (coinsPulseRoutine != null)
            {
                StopCoroutine(coinsPulseRoutine);
            }

            coinsPulseRoutine = StartCoroutine(PulseCoins());
            SpawnFloatingText(coinsPanel != null ? coinsPanel : coinsLabel.rectTransform, $"+{Mathf.Max(0, amount)}");
        }

        public void SetGameplayButtonsInteractable(bool isInteractable)
        {
            SetButtonInteractable(restartButton, isInteractable);
            SetButtonInteractable(hintButton, isInteractable);
            SetButtonInteractable(undoButton, isInteractable);
            SetButtonInteractable(boosterDeckButton, isInteractable);
        }

        public void SetAddMovesButtonInteractable(bool isInteractable)
        {
            SetButtonInteractable(boosterDeckButton, isInteractable);
        }

        public void ShowMovesBonusText(int amount)
        {
            EnsureMovesBonusText();
            if (movesBonusText == null || movesBonusGroup == null)
            {
                return;
            }

            if (movesBonusTextRoutine != null)
            {
                StopCoroutine(movesBonusTextRoutine);
            }

            movesBonusText.text = $"Moves Bonus +{Mathf.Max(0, amount)}";
            movesBonusText.rectTransform.SetAsLastSibling();
            movesBonusText.gameObject.SetActive(true);
            movesBonusTextRoutine = StartCoroutine(FadeCanvasGroup(movesBonusGroup, 0f, 1f, 0.22f, false));
        }

        public IEnumerator HideMovesBonusText()
        {
            EnsureMovesBonusText();
            if (movesBonusGroup == null || movesBonusText == null)
            {
                yield break;
            }

            if (movesBonusTextRoutine != null)
            {
                StopCoroutine(movesBonusTextRoutine);
            }

            yield return FadeCanvasGroup(movesBonusGroup, movesBonusGroup.alpha, 0f, 0.28f, true);
            movesBonusTextRoutine = null;
        }

        public void HideMovesBonusTextInstant()
        {
            if (movesBonusTextRoutine != null)
            {
                StopCoroutine(movesBonusTextRoutine);
                movesBonusTextRoutine = null;
            }

            if (movesBonusGroup != null)
            {
                movesBonusGroup.alpha = 0f;
            }

            if (movesBonusText != null)
            {
                movesBonusText.gameObject.SetActive(false);
            }
        }

        public IEnumerator PlayMovesBonusCoin(float duration)
        {
            EnsureMovesBonusAnchors();
            if (safeAreaRoot == null || movesPanel == null || coinsPanel == null)
            {
                yield break;
            }

            Image coin = CreateBonusCoin();
            RectTransform coinRect = coin.rectTransform;
            coinRect.SetParent(safeAreaRoot, false);
            coinRect.SetAsLastSibling();
            coinRect.position = movesPanel.position;
            coinRect.sizeDelta = new Vector2(34f, 34f);

            Vector3 start = movesPanel.position;
            Vector3 end = coinsPanel.position;
            Vector3 lift = Vector3.up * 54f;
            float safeDuration = Mathf.Max(0.025f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                Vector3 arc = Vector3.LerpUnclamped(start, end, eased) + (lift * Mathf.Sin(progress * Mathf.PI));
                coinRect.position = arc;
                coinRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, Mathf.Sin(progress * Mathf.PI));

                Color color = coin.color;
                color.a = Mathf.Lerp(0.95f, 1f, Mathf.Sin(progress * Mathf.PI));
                coin.color = color;
                yield return null;
            }

            Destroy(coin.gameObject);
        }

        public void PlayGoalCompletedFeedback(RectTransform source, int amount)
        {
            if (source == null || safeAreaRoot == null || !isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(SpawnGoalSparkles(source, amount));
        }

        public void UpdateDeckCounter(int remainingCards)
        {
            if (deckCounterLabel != null)
            {
                deckCounterLabel.text = remainingCards.ToString();
            }
        }

        public void SetBoosterBadgeVisible(bool isVisible)
        {
            if (boosterBadge != null)
            {
                if (boosterBadgeText != null)
                {
                    boosterBadgeText.text = $"x{boosterCount}";
                }

                boosterBadge.SetActive(isVisible);
            }
        }

        public void UpdateUndoCount(int count)
        {
            undoCount = Mathf.Max(0, count);
            if (undoBadgeText != null)
            {
                undoBadgeText.text = $"x{undoCount}";
            }

            if (undoBadgeRoot != null)
            {
                undoBadgeRoot.gameObject.SetActive(undoCount > 0);
            }

            PlayBadgePulse(undoBadgeRoot, ref undoBadgePulseRoutine);
        }

        public void UpdateHintCount(int count)
        {
            hintCount = Mathf.Max(0, count);
            if (hintBadgeText != null)
            {
                hintBadgeText.text = $"x{hintCount}";
            }

            if (hintBadgeRoot != null)
            {
                hintBadgeRoot.gameObject.SetActive(hintCount > 0);
            }

            RectTransform hintRoot = hintButton != null ? hintButton.GetComponent<RectTransform>() : null;
            RectTransform costRow = hintRoot != null ? FindChildRecursive<RectTransform>(hintRoot, "HintButtonSubRow") : null;
            if (costRow != null)
            {
                costRow.gameObject.SetActive(hintCount <= 0);
            }

            PlayBadgePulse(hintBadgeRoot, ref hintBadgePulseRoutine);
        }

        public void UpdateBoosterCount(int count)
        {
            boosterCount = Mathf.Max(0, count);
            if (boosterBadgeText != null)
            {
                boosterBadgeText.text = $"x{boosterCount}";
            }

            SetBoosterBadgeVisible(boosterCount > 0);
            PlayBadgePulse(boosterBadgeRoot, ref boosterBadgePulseRoutine);
        }

        public void ShowStatus(string message, Color color)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            statusLabel.color = color;
        }

        public void ShowRestartConfirmation(Func<bool> onConfirmed, Action onClosed = null)
        {
            EnsureRestartConfirmationPopup(onConfirmed, onClosed);
            ClosePausePopup(false);

            if (restartConfirmationPopup != null)
            {
                RestartConfirmationPopupController controller = restartConfirmationPopup.GetComponent<RestartConfirmationPopupController>();
                if (controller != null)
                {
                    controller.Show();
                }
                else
                {
                    restartConfirmationPopup.transform.SetAsLastSibling();
                    restartConfirmationPopup.SetActive(true);
                    Time.timeScale = 0f;
                }
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void ShowLevelFailedPopup(Func<bool> onRetry)
        {
            EnsureLevelFailedPopup(onRetry);

            if (levelFailedPopup != null)
            {
                levelFailedPopup.transform.SetAsLastSibling();
                levelFailedPopup.SetActive(true);
                Time.timeScale = 0f;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void ShowLivesEmptyPopup(LivesManager livesManager)
        {
            activeLivesManager = livesManager;
            EnsureLivesEmptyPopup();
            UpdateLivesEmptyTimerText();

            if (livesEmptyPopup != null)
            {
                livesEmptyPopup.transform.SetAsLastSibling();
                livesEmptyPopup.SetActive(true);
                Time.timeScale = 0f;
            }

            if (livesEmptyTimerRoutine != null)
            {
                StopCoroutine(livesEmptyTimerRoutine);
            }

            livesEmptyTimerRoutine = StartCoroutine(UpdateLivesEmptyTimer());
        }

        private void ApplyGameplayPolish()
        {
            if (sceneRefs != null)
            {
                safeAreaRoot = sceneRefs.SafeAreaRoot;
            }

            if (safeAreaRoot == null)
            {
                return;
            }

            if (HasPrefabAuthoredGameplayUi())
            {
                BindAuthoredGameplayButtonBadges();
            }
            else
            {
                EnsureBackgroundVignette();
            }

            StyleTopHud();
            StyleMovesPanel();
            StyleDeckArea();
            StyleBottomHudZone();
            StyleBottomButtons();
            HideAdPlaceholder();
        }

        private bool HasPrefabAuthoredGameplayUi()
        {
            return safeAreaRoot != null
                && FindChildRecursive<RectTransform>(safeAreaRoot, "HintButtonIcon") != null
                && FindChildRecursive<RectTransform>(safeAreaRoot, "UndoButtonIcon") != null
                && FindChildRecursive<RectTransform>(safeAreaRoot, "BoosterDeckButtonIcon") != null
                && FindChildRecursive<RectTransform>(safeAreaRoot, "LivesHud") != null;
        }

        private void BindAuthoredGameplayButtonBadges()
        {
            if (hintButton != null)
            {
                BindAuthoredBadge(hintButton.transform, "HintButtonBadge", out hintBadgeRoot, out hintBadgeText);
            }

            if (undoButton != null)
            {
                BindAuthoredBadge(undoButton.transform, "UndoButtonBadge", out undoBadgeRoot, out undoBadgeText);
            }

            if (boosterDeckButton != null)
            {
                BindAuthoredBadge(boosterDeckButton.transform, "BoosterDeckButtonBadge", out boosterBadgeRoot, out boosterBadgeText);
                boosterBadge = boosterBadgeRoot != null ? boosterBadgeRoot.gameObject : boosterBadge;
            }
        }

        private static void BindAuthoredBadge(Transform buttonRoot, string badgeName, out RectTransform badgeRoot, out TextMeshProUGUI badgeText)
        {
            badgeRoot = FindChildRecursive<RectTransform>(buttonRoot, badgeName);
            badgeText = badgeRoot != null ? FindChildRecursive<TextMeshProUGUI>(badgeRoot, "BadgeText") : null;
        }

        private void EnsureLivesHud(LivesManager livesManager)
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            RectTransform root = FindChildRecursive<RectTransform>(safeAreaRoot, "LivesHud");
            if (root == null)
            {
                root = CreateRect("LivesHud", safeAreaRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(214f, 64f), new Vector2(LivesLeftInsetX, -LivesUnderMovesTopInset));
            }
            else
            {
                SetAnchored(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(214f, 64f), new Vector2(LivesLeftInsetX, -LivesUnderMovesTopInset));
            }

            Image panelImage = EnsureGraphic<Image>(root.gameObject);
            panelImage.sprite = null;
            panelImage.color = new Color(1f, 1f, 1f, 0f);
            panelImage.raycastTarget = false;

            RectTransform heart = EnsureDirectImageLayer(root, "HeartIcon", GetHeartSprite(), Image.Type.Simple, Color.white);
            TMP_Text heartText = heart.GetComponent<TMP_Text>();
            if (heartText != null)
            {
                heartText.enabled = false;
            }

            SetAnchored(heart, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), new Vector2(42f, 0f));
            Image heartImage = heart.GetComponent<Image>();
            heartImage.preserveAspect = true;
            AddShadow(heart.gameObject, new Color(0.25f, 0f, 0f, 0.32f), new Vector2(0f, -2f));

            TextMeshProUGUI count = EnsureDirectText(root, "LivesCount");
            RemoveUiEffects(count.gameObject);
            ConfigureText(count, 32f, 32f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.66f, 1f));
            ApplyCachedTextStyle(count, livesCountFontAsset, null);
            ApplyLivesCountStyle(count);
            SetAnchored(count.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(126f, 56f), new Vector2(82f, 0f));
            count.transform.SetAsLastSibling();
            NormalizeLivesHudChildren(root, heart, count);

            LivesHudView[] hudViews = root.GetComponents<LivesHudView>();
            if (hudViews.Length > 0)
            {
                livesHudView = hudViews[0];
                for (int index = 1; index < hudViews.Length; index++)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(hudViews[index]);
                    }
                    else
                    {
                        DestroyImmediate(hudViews[index]);
                    }
                }
            }
            else
            {
                livesHudView = root.gameObject.AddComponent<LivesHudView>();
            }

            livesHudView.Configure(livesManager, count, heart.GetComponent<Image>());
            root.SetAsLastSibling();
        }

        private static void NormalizeLivesHudChildren(RectTransform root, RectTransform heart, TMP_Text count)
        {
            if (root == null)
            {
                return;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);
                bool keep = child == heart || (count != null && child == count.transform);
                child.gameObject.SetActive(keep);
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == count)
                {
                    TrimExtraTextComponents(text.gameObject, text);
                    ApplyLivesCountStyle(text);
                    continue;
                }

                text.enabled = false;
                text.canvasRenderer.SetAlpha(0f);
            }
        }

        private void CacheHudTextStyles()
        {
            RectTransform movesRoot = FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
            TMP_Text authoredMovesLabel = FindDirectText(movesRoot, "MovesLabel") ?? FindChildRecursive<TMP_Text>(movesRoot, "MovesLabel");
            TMP_Text authoredMovesValue = FindDirectText(movesRoot, "MovesValue") ?? FindChildRecursive<TMP_Text>(movesRoot, "MovesValue") ?? movesLabel;
            CacheTextStyle(authoredMovesLabel, ref movesLabelFontAsset, ref movesLabelFontMaterial);
            CacheTextStyle(authoredMovesValue, ref movesValueFontAsset, ref movesValueFontMaterial);

            RectTransform livesRoot = FindChildRecursive<RectTransform>(safeAreaRoot, "LivesHud");
            TMP_Text authoredLivesCount = FindDirectText(livesRoot, "LivesCount") ?? FindChildRecursive<TMP_Text>(livesRoot, "LivesCount");
            CacheTextStyle(authoredLivesCount, ref livesCountFontAsset, ref livesCountFontMaterial);
            livesCountFontMaterial = null;
        }

        private static void CacheTextStyle(TMP_Text source, ref TMP_FontAsset fontAsset, ref Material fontMaterial)
        {
            if (source == null)
            {
                return;
            }

            if (fontAsset == null && source.font != null)
            {
                fontAsset = source.font;
            }

            if (fontMaterial == null && source.fontSharedMaterial != null)
            {
                fontMaterial = source.fontSharedMaterial;
            }
        }

        private static void ApplyCachedTextStyle(TMP_Text target, TMP_FontAsset fontAsset, Material fontMaterial)
        {
            if (target == null)
            {
                return;
            }

            if (fontAsset != null)
            {
                target.font = fontAsset;
            }

            if (fontMaterial != null)
            {
                target.fontSharedMaterial = fontMaterial;
            }
        }

        private static void ApplyCleanFontMaterial(TMP_Text text)
        {
            if (text == null || text.font == null || text.font.material == null)
            {
                return;
            }

            text.fontSharedMaterial = text.font.material;
        }

        private static void ApplyMovesValueStyle(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.gameObject.SetActive(true);
            text.enabled = true;
            text.canvasRenderer.SetAlpha(1f);
            text.enableAutoSizing = false;
            text.fontSize = 38f;
            text.fontSizeMin = 38f;
            text.fontSizeMax = 38f;
            text.fontStyle = FontStyles.Normal;
            text.fontWeight = FontWeight.Regular;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.31f, 0.17f, 0.04f, 1f);
            text.margin = Vector4.zero;
            text.extraPadding = false;
            text.raycastTarget = false;
            text.ForceMeshUpdate(true, true);
        }

        private static void ApplyLivesCountStyle(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.gameObject.SetActive(true);
            text.enabled = true;
            text.canvasRenderer.SetAlpha(1f);
            text.enableAutoSizing = false;
            text.fontSize = 32f;
            text.fontSizeMin = 32f;
            text.fontSizeMax = 32f;
            text.fontStyle = FontStyles.Normal;
            text.fontWeight = FontWeight.Regular;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(1f, 0.9f, 0.66f, 1f);
            text.margin = Vector4.zero;
            text.extraPadding = false;
            text.raycastTarget = false;
            ApplyCleanFontMaterial(text);
            text.ForceMeshUpdate(true, true);
        }

        private void StyleTopHud()
        {
            RectTransform topHud = FindChildRecursive<RectTransform>(safeAreaRoot, "TopHUD");
            if (topHud != null)
            {
                SetAnchored(topHud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 128f), new Vector2(0f, -18f));
            }

            if (menuButton != null && menuButton != rewardBonusButton)
            {
                menuButton.gameObject.SetActive(false);
            }

            coinsPanel = FindChildRecursive<RectTransform>(safeAreaRoot, "CoinsPanel");
            if (coinsPanel != null)
            {
                SetAnchored(coinsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(236f, 70f), new Vector2(196f, -HudTopInset));
                Image panelImage = EnsureGraphic<Image>(coinsPanel.gameObject);
                panelImage.sprite = GetHudCounterGoldSprite();
                panelImage.type = Image.Type.Sliced;
                panelImage.color = new Color(1f, 1f, 1f, 0.96f);
                AddShadow(coinsPanel.gameObject, new Color(0.12f, 0.06f, 0.01f, 0.22f), new Vector2(0f, -4f));

                RectTransform coinGloss = EnsureImageLayer(coinsPanel, "CoinsTopGloss", GetRoundedButtonSprite(), Image.Type.Sliced, new Color(1f, 0.96f, 0.74f, 0.16f));
                SetAnchored(coinGloss, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-54f, 18f), new Vector2(10f, -19f));
                coinGloss.SetAsFirstSibling();

                RectTransform coinIcon = FindChildRecursive<RectTransform>(coinsPanel, "CoinIcon")
                    ?? EnsureImageLayer(coinsPanel, "CoinIcon", GetCoinIconSprite(), Image.Type.Simple, Color.white);
                if (coinIcon != null)
                {
                    SetAnchored(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), new Vector2(40f, 0f));
                    Image coinImage = EnsureGraphic<Image>(coinIcon.gameObject);
                    coinImage.sprite = GetCoinIconSprite();
                    coinImage.type = Image.Type.Simple;
                    coinImage.preserveAspect = true;
                    coinImage.color = Color.white;
                    AddShadow(coinIcon.gameObject, new Color(0.32f, 0.14f, 0f, 0.24f), new Vector2(0f, -2f));

                    foreach (TMP_Text text in coinIcon.GetComponentsInChildren<TMP_Text>(true))
                    {
                        text.gameObject.SetActive(false);
                    }
                }

                if (coinsLabel != null)
                {
                    RectTransform coinsRect = coinsLabel.rectTransform;
                    SetAnchored(coinsRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 44f), new Vector2(24f, 0f));
                    RemoveUiEffects(coinsLabel.gameObject);
                    ApplyCachedTextStyle(coinsLabel, movesValueFontAsset, movesValueFontMaterial);
                    ConfigureText(coinsLabel, 30f, 30f, TextAlignmentOptions.Center, new Color(0.31f, 0.17f, 0.04f, 1f));
                    coinsLabel.enableAutoSizing = false;
                    coinsLabel.fontSize = 30f;
                    coinsLabel.fontSizeMin = 30f;
                    coinsLabel.fontSizeMax = 30f;
                    coinsLabel.fontStyle = FontStyles.Normal;
                    coinsLabel.fontWeight = FontWeight.Regular;
                    coinsLabel.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            if (plusButton != null)
            {
                plusButton.gameObject.SetActive(false);
            }

            if (rewardBonusButton != null)
            {
                RectTransform pauseRoot = rewardBonusButton.GetComponent<RectTransform>();
                Image pauseImage = rewardBonusButton.GetComponent<Image>();
                if (pauseImage != null && rewardBonusButton.targetGraphic == null)
                {
                    rewardBonusButton.targetGraphic = pauseImage;
                }

                RemoveChildIfExists(pauseRoot, "PauseIcon");
                RemoveChildIfExists(pauseRoot, "MenuBarTop");
                RemoveChildIfExists(pauseRoot, "MenuBarMiddle");
                RemoveChildIfExists(pauseRoot, "MenuBarBottom");
            }
        }

        private void StyleMovesPanel()
        {
            movesPanel = FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
            if (movesPanel == null)
            {
                return;
            }

            SetAnchored(movesPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(164f, 108f), new Vector2(154f, -MovesTopInset));
            Image panelImage = EnsureGraphic<Image>(movesPanel.gameObject);
            panelImage.sprite = GetHudPanelCreamGoldSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(1f, 1f, 1f, 0.92f);
            AddShadow(movesPanel.gameObject, new Color(0.12f, 0.06f, 0.01f, 0.2f), new Vector2(0f, -4f));

            RectTransform movesGloss = EnsureImageLayer(movesPanel, "MovesTopGloss", GetRoundedButtonSprite(), Image.Type.Sliced, new Color(1f, 0.96f, 0.74f, 0.14f));
            SetAnchored(movesGloss, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-36f, 18f), new Vector2(0f, -20f));
            movesGloss.SetAsFirstSibling();

            EnsureMovesPanelText(movesPanel);
        }

        private void EnsureMovesPanelText(RectTransform panel)
        {
            string value = hasDisplayedMoves
                ? lastDisplayedMoves.ToString()
                : (movesLabel != null && !string.IsNullOrWhiteSpace(movesLabel.text)
                    ? movesLabel.text
                    : lastDisplayedMoves.ToString());

            TextMeshProUGUI label = EnsureDirectText(panel, "MovesLabel");
            label.text = "Ходы";
            label.gameObject.SetActive(true);
            label.enabled = true;
            label.canvasRenderer.SetAlpha(1f);
            SetAnchored(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-26f, 28f), new Vector2(0f, -17f));
            ConfigureText(label, 25f, 17f, TextAlignmentOptions.Center, new Color(0.37f, 0.22f, 0.06f, 1f));
            ApplyCachedTextStyle(label, movesLabelFontAsset, movesLabelFontMaterial);
            label.fontStyle = FontStyles.Normal;
            label.fontWeight = FontWeight.Regular;
            RemoveUiEffects(label.gameObject);
            AddTextOutline(label, new Color(1f, 0.93f, 0.62f, 0.22f), new Vector2(0.6f, -0.6f));

            movesLabel = EnsureDirectText(panel, "MovesValue");
            movesLabel.text = value;
            movesLabel.gameObject.SetActive(true);
            movesLabel.enabled = true;
            movesLabel.canvasRenderer.SetAlpha(1f);
            SetAnchored(movesLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-24f, 54f), new Vector2(0f, 10f));
            ConfigureText(movesLabel, 38f, 38f, TextAlignmentOptions.Center, new Color(0.31f, 0.17f, 0.04f, 1f));
            ApplyCachedTextStyle(movesLabel, movesValueFontAsset, movesValueFontMaterial);
            ApplyMovesValueStyle(movesLabel);
            RemoveUiEffects(movesLabel.gameObject);
            label.transform.SetAsLastSibling();
            movesLabel.transform.SetAsLastSibling();
            NormalizeMovesPanelText(panel, label, movesLabel);
        }

        private static void NormalizeMovesPanelText(RectTransform panel, TMP_Text label, TMP_Text value)
        {
            if (panel == null)
            {
                return;
            }

            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == label || text == value)
                {
                    TrimExtraTextComponents(text.gameObject, text);
                    text.gameObject.SetActive(true);
                    text.enabled = true;
                    text.canvasRenderer.SetAlpha(1f);
                    if (text == value)
                    {
                        ApplyMovesValueStyle(text);
                    }

                    continue;
                }

                text.enabled = false;
                text.canvasRenderer.SetAlpha(0f);
            }
        }

        private static void TrimExtraTextComponents(GameObject target, TMP_Text keep)
        {
            if (target == null)
            {
                return;
            }

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
            if (target == null)
            {
                return;
            }

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

        private void StyleDeckArea()
        {
            RectTransform deckArea = FindChildRecursive<RectTransform>(safeAreaRoot, "DeckArea");
            if (deckArea != null)
            {
                float deckAreaY = ResolveDeckAreaYAlignedToGoalRow(deckArea, -470f);
                SetAnchored(deckArea, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(520f, 310f), new Vector2(-RightDeckInset, deckAreaY));
                Image deckAreaImage = EnsureGraphic<Image>(deckArea.gameObject);
                deckAreaImage.color = new Color(1f, 1f, 1f, 0f);
                deckAreaImage.raycastTarget = false;
            }

            RectTransform deckCardBack = sceneRefs != null ? sceneRefs.DeckCardBack : null;
            if (deckCardBack != null)
            {
                SetAnchored(deckCardBack, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), GameplayCardSize, new Vector2(-DeckCardBackInsetX, -2f));
                Image deckImage = EnsureGraphic<Image>(deckCardBack.gameObject);
                deckImage.raycastTarget = true;
            }

            if (deckCounterLabel != null)
            {
                RectTransform counterRect = deckCounterLabel.rectTransform;
                SetAnchored(counterRect, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), new Vector2(52f, 40f), new Vector2(-165f, 120f));
                ConfigureText(deckCounterLabel, 32f, 22f, TextAlignmentOptions.Center, new Color(0.12f, 0.18f, 0.25f, 1f));
                deckCounterLabel.fontStyle = FontStyles.Bold;
                EnsureDeckCounterBadge(counterRect);
                counterRect.SetAsLastSibling();
            }
        }

        private void EnsureDeckCounterBadge(RectTransform counterRect)
        {
            if (counterRect == null || counterRect.parent == null)
            {
                return;
            }

            RectTransform badgeRect = FindChildRecursive<RectTransform>(counterRect.parent, "DeckCounterBadgeBg");
            if (badgeRect == null)
            {
                badgeRect = CreateRect("DeckCounterBadgeBg", counterRect.parent, counterRect.anchorMin, counterRect.anchorMax, counterRect.pivot, counterRect.sizeDelta, counterRect.anchoredPosition);
            }
            else
            {
                SetAnchored(badgeRect, counterRect.anchorMin, counterRect.anchorMax, counterRect.pivot, counterRect.sizeDelta, counterRect.anchoredPosition);
            }

            Image counterImage = EnsureGraphic<Image>(badgeRect.gameObject);
            counterImage.sprite = GetRoundedPanelSprite();
            counterImage.type = Image.Type.Sliced;
            counterImage.color = new Color(1f, 1f, 0.96f, 0.96f);
            counterImage.raycastTarget = false;
            AddShadow(badgeRect.gameObject, new Color(0f, 0.04f, 0.1f, 0.28f), new Vector2(0f, -2f));
            badgeRect.SetSiblingIndex(counterRect.GetSiblingIndex());
        }

        private float ResolveDeckAreaYAlignedToGoalRow(RectTransform deckArea, float fallbackY)
        {
            if (sceneRefs == null || deckArea == null || deckArea.parent is not RectTransform deckParent)
            {
                return fallbackY;
            }

            IReadOnlyList<ColumnRef> columns = sceneRefs.Columns;
            if (columns == null || columns.Count == 0)
            {
                return fallbackY;
            }

            float sumY = 0f;
            int count = 0;
            foreach (ColumnRef column in columns)
            {
                RectTransform crownSlot = column?.CrownSlot as RectTransform;
                if (crownSlot == null || !crownSlot.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 goalCenterWorld = crownSlot.TransformPoint(crownSlot.rect.center);
                float goalCenterParentY = deckParent.InverseTransformPoint(goalCenterWorld).y;
                sumY += goalCenterParentY;
                count++;
            }

            if (count <= 0)
            {
                return fallbackY;
            }

            float goalCenterY = sumY / count;
            float deckParentTopY = deckParent.rect.yMax;
            float deckCardLocalY = ((0.54f - 0.5f) * 310f) - 2f;
            return goalCenterY - deckParentTopY - deckCardLocalY;
        }

        private void StyleBottomButtons()
        {
            Vector2 buttonSize = ResolveBottomButtonSize();
            float buttonGap = Mathf.Clamp(buttonSize.x * 0.1f, 22f, 48f);
            float buttonStep = buttonSize.x + buttonGap;

            StyleActionButton(
                hintButton,
                "HintButton",
                "Подсказка",
                PremiumUISprites.HintBulbIcon,
                hintCount > 0 ? string.Empty : HintCost.ToString(),
                $"x{hintCount}",
                new Vector2(-buttonStep, BottomButtonYOffset),
                buttonSize,
                hintCount > 0,
                out hintBadgeRoot,
                out hintBadgeText);

            StyleActionButton(
                undoButton,
                "UndoButton",
                "Отмена",
                PremiumUISprites.UndoIcon,
                string.Empty,
                $"x{undoCount}",
                new Vector2(0f, BottomButtonYOffset),
                buttonSize,
                true,
                out undoBadgeRoot,
                out undoBadgeText);

            StyleActionButton(
                boosterDeckButton,
                "BoosterDeckButton",
                "+10 ходов",
                PremiumUISprites.BoosterRocketIcon,
                string.Empty,
                string.Empty,
                new Vector2(buttonStep, BottomButtonYOffset),
                buttonSize,
                false,
                out boosterBadgeRoot,
                out boosterBadgeText);
        }

        private void StyleBottomHudZone()
        {
            RectTransform bottomHud = FindChildRecursive<RectTransform>(safeAreaRoot, "BottomHUD");
            if (bottomHud == null)
            {
                return;
            }

            SetAnchored(
                bottomHud,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, BottomHudHeight),
                new Vector2(0f, BottomHudBottomInset));
        }

        private Vector2 ResolveBottomButtonSize()
        {
            RectTransform bottomHud = FindChildRecursive<RectTransform>(safeAreaRoot, "BottomHUD");
            float availableWidth = bottomHud != null && bottomHud.rect.width > 0f
                ? bottomHud.rect.width
                : safeAreaRoot != null && safeAreaRoot.rect.width > 0f
                    ? safeAreaRoot.rect.width
                    : 1920f;

            float horizontalPadding = Mathf.Clamp(availableWidth * 0.045f, 24f, 72f);
            float idealGap = Mathf.Clamp(availableWidth * 0.035f, 22f, 48f);
            float maxFittingWidth = (availableWidth - (horizontalPadding * 2f) - (idealGap * 2f)) / 3f;
            float width = Mathf.Clamp(maxFittingWidth, BottomButtonMinWidth, BottomButtonMaxWidth);
            float height = Mathf.Round(width * 0.295f);
            return new Vector2(width, height);
        }

        private void StyleActionButton(
            Button button,
            string name,
            string label,
            Sprite iconSprite,
            string subValue,
            string badgeValue,
            Vector2 anchoredPosition,
            Vector2 buttonSize,
            bool showBadge,
            out RectTransform badgeRoot,
            out TextMeshProUGUI badgeText)
        {
            badgeRoot = null;
            badgeText = null;

            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.transition = Selectable.Transition.None;
            RectTransform root = button.GetComponent<RectTransform>();
            SetAnchored(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), buttonSize, anchoredPosition);

            Image image = EnsureGraphic<Image>(button.gameObject);
            image.sprite = GetGameplayActionButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            button.targetGraphic = image;
            AddShadow(button.gameObject, new Color(0.08f, 0.03f, 0f, 0.32f), new Vector2(0f, -8f));
            Outline imageOutline = button.GetComponent<Outline>();
            if (imageOutline != null)
            {
                imageOutline.enabled = false;
            }

            RectTransform hoverGlow = EnsureImageLayer(root, "HoverGlow", GetGameplayActionButtonSprite(), Image.Type.Sliced, new Color(1f, 0.74f, 0.2f, 0.16f));
            StretchToFill(hoverGlow, -8f, -8f, -7f, -7f);
            hoverGlow.SetAsFirstSibling();
            CanvasGroup hoverGroup = hoverGlow.GetComponent<CanvasGroup>();
            if (hoverGroup == null)
            {
                hoverGroup = hoverGlow.gameObject.AddComponent<CanvasGroup>();
            }

            hoverGroup.alpha = 0f;

            RectTransform gloss = EnsureImageLayer(root, "TopGloss", GetRoundedButtonSprite(), Image.Type.Sliced, new Color(0.82f, 1f, 0.74f, 0.13f));
            SetAnchored(gloss, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-104f, 23f), new Vector2(10f, -30f));
            gloss.SetAsLastSibling();

            float iconHaloSize = Mathf.Clamp(buttonSize.y * 0.82f, 50f, 92f);
            float iconSize = Mathf.Clamp(iconHaloSize * 0.78f, 42f, 74f);
            float iconX = Mathf.Clamp(buttonSize.x * 0.17f, 38f, 66f);
            RectTransform iconHalo = EnsureImageLayer(root, $"{name}IconHalo", GetGoldIconSprite(), Image.Type.Sliced, new Color(0.78f, 0.62f, 0.28f, 0.92f));
            SetAnchored(iconHalo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(iconHaloSize, iconHaloSize), new Vector2(iconX, -1f));
            AddShadow(iconHalo.gameObject, new Color(0.32f, 0.13f, 0f, 0.26f), new Vector2(0f, -4f));

            RectTransform icon = EnsureImageLayer(root, $"{name}Icon", iconSprite != null ? iconSprite : GetEmeraldIconSprite(), Image.Type.Simple, new Color(0.9f, 0.9f, 0.78f, 1f));
            SetAnchored(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(iconSize, iconSize), new Vector2(iconX, 2f));

            TextMeshProUGUI labelText = EnsureText(root, $"{name}Label");
            if (labelText != null)
            {
                labelText.text = label;
                ConfigureText(labelText, 34f, 18f, TextAlignmentOptions.Center, new Color(0.96f, 0.78f, 0.32f, 1f));
                ApplyGameplayButtonTextStyle(labelText, 0.105f);
                Vector2 labelSize = string.IsNullOrEmpty(subValue) ? new Vector2(-84f, 50f) : new Vector2(-84f, 42f);
                Vector2 labelOffset = GetGameplayLabelOffset(name);
                Vector2 labelPosition = string.IsNullOrEmpty(subValue) ? labelOffset : labelOffset + new Vector2(0f, 12f);
                SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), labelSize, labelPosition);
                AddTextOutline(labelText, new Color(0.17f, 0.078f, 0.016f, 0.68f), new Vector2(1.05f, -1.05f));
                Shadow labelShadow = labelText.GetComponent<Shadow>();
                if (labelShadow == null)
                {
                    labelShadow = labelText.gameObject.AddComponent<Shadow>();
                }

                labelShadow.enabled = true;
                labelShadow.effectColor = new Color(0.03f, 0.012f, 0f, 0.28f);
                labelShadow.effectDistance = new Vector2(1f, -1.4f);
                labelShadow.useGraphicAlpha = true;
            }

            RectTransform subRow = null;
            if (!string.IsNullOrEmpty(subValue))
            {
                subRow = EnsureHintCostRow(root, name, subValue);
            }
            else
            {
                RectTransform existingSubRow = FindChildRecursive<RectTransform>(root, $"{name}SubRow");
                if (existingSubRow != null)
                {
                    existingSubRow.gameObject.SetActive(false);
                }
            }

            badgeRoot = EnsureBadge(root, name, badgeValue, showBadge, out badgeText);

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                bool isManagedText =
                    text == labelText ||
                    text == badgeText ||
                    text.name == $"{name}SubText" ||
                    text.name == "CoinGlyph";

                if (isManagedText)
                {
                    continue;
                }

                text.gameObject.SetActive(false);
            }

            hoverGlow.SetAsFirstSibling();
            gloss.SetAsLastSibling();
            subRow?.SetAsLastSibling();
            badgeRoot?.SetAsLastSibling();
        }

        private RectTransform EnsureImageLayer(RectTransform parent, string name, Sprite sprite, Image.Type imageType, Color color)
        {
            RectTransform layer = FindChildRecursive<RectTransform>(parent, name);
            if (layer == null)
            {
                layer = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            Image image = EnsureGraphic<Image>(layer.gameObject);
            image.sprite = sprite;
            image.type = imageType;
            image.color = color;
            image.raycastTarget = false;
            return layer;
        }

        private RectTransform EnsureDirectImageLayer(RectTransform parent, string name, Sprite sprite, Image.Type imageType, Color color)
        {
            RectTransform layer = null;
            if (parent != null)
            {
                for (int index = 0; index < parent.childCount; index++)
                {
                    if (parent.GetChild(index) is not RectTransform child || child.name != name)
                    {
                        continue;
                    }

                    if (child.TryGetComponent(out TMP_Text text))
                    {
                        text.enabled = false;
                        child.gameObject.SetActive(false);
                        continue;
                    }

                    layer = child;
                    break;
                }
            }

            if (layer == null)
            {
                layer = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            layer.gameObject.SetActive(true);
            Image image = EnsureGraphic<Image>(layer.gameObject);
            image.sprite = sprite;
            image.type = imageType;
            image.color = color;
            image.raycastTarget = false;
            return layer;
        }

        private static Vector2 GetGameplayLabelOffset(string buttonName)
        {
            return buttonName switch
            {
                "HintButton" => new Vector2(20f, 1f),
                "UndoButton" => new Vector2(-10f, 2f),
                "BoosterDeckButton" => new Vector2(-10f, 1f),
                _ => new Vector2(-10f, 0f)
            };
        }

        private RectTransform EnsureBadge(
            RectTransform buttonRoot,
            string buttonName,
            string value,
            bool isVisible,
            out TextMeshProUGUI text)
        {
            string badgeName = $"{buttonName}Badge";
            RectTransform badge = FindChildRecursive<RectTransform>(buttonRoot, badgeName);
            if (badge == null)
            {
                badge = CreateRect(badgeName, buttonRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(68f, 38f), new Vector2(-42f, -24f));
            }

            if (buttonName == "BoosterDeckButton" && boosterBadge != null && boosterBadge != badge.gameObject)
            {
                boosterBadge.SetActive(false);
            }

            if (buttonName == "BoosterDeckButton")
            {
                boosterBadge = badge.gameObject;
            }

            bool isUndoCount = buttonName == "UndoButton";
            Vector2 badgeSize = new(58f, 24f);
            Vector2 badgePosition = new(-82f, -16f);
            SetAnchored(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), badgeSize, badgePosition);

            Image badgeImage = EnsureGraphic<Image>(badge.gameObject);
            badgeImage.sprite = GetHudCounterGoldSprite();
            badgeImage.type = Image.Type.Sliced;
            badgeImage.color = isUndoCount ? new Color(0.72f, 0.45f, 0.13f, 0.72f) : new Color(0.42f, 0.14f, 0.07f, 0.76f);
            badgeImage.raycastTarget = false;
            AddShadow(badge.gameObject, new Color(0.09f, 0.035f, 0f, 0.24f), new Vector2(0f, -1.4f));

            RectTransform badgeGloss = EnsureImageLayer(badge, "BadgeGloss", GetRoundedButtonSprite(), Image.Type.Sliced, new Color(1f, 0.86f, 0.42f, 0.09f));
            SetAnchored(badgeGloss, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-16f, 7f), new Vector2(0f, -7f));

            text = EnsureText(badge, "BadgeText");
            text.text = value;
            ConfigureText(text, 20f, 14f, TextAlignmentOptions.Center, new Color(0.9f, 0.7f, 0.32f, 1f));
            ApplyGameplayButtonTextStyle(text, 0.08f);
            StretchToFill(text.rectTransform, 7f, 7f, 0f, 2f);

            AddTextOutline(text, new Color(0.18f, 0.055f, 0f, 0.68f), new Vector2(0.75f, -0.75f));
            badgeGloss.SetAsFirstSibling();
            text.rectTransform.SetAsLastSibling();
            badge.gameObject.SetActive(isVisible);
            return badge;
        }

        private RectTransform EnsureHintCostRow(RectTransform buttonRoot, string buttonName, string value)
        {
            RectTransform row = FindChildRecursive<RectTransform>(buttonRoot, $"{buttonName}SubRow");
            if (row == null)
            {
                row = CreateRect($"{buttonName}SubRow", buttonRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(112f, 28f), new Vector2(0f, -19f));
            }
            else
            {
                SetAnchored(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(112f, 28f), new Vector2(0f, -19f));
            }

            Image rowImage = EnsureGraphic<Image>(row.gameObject);
            rowImage.sprite = GetHudCounterGoldSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = new Color(0.64f, 0.32f, 0.09f, 0.62f);
            rowImage.raycastTarget = false;
            AddShadow(row.gameObject, new Color(0.12f, 0.045f, 0f, 0.2f), new Vector2(0f, -1f));

            RectTransform coin = EnsureImageLayer(row, "CoinDot", GetCoinIconSprite(), Image.Type.Simple, Color.white);
            SetAnchored(coin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f), new Vector2(19f, 0f));
            Image coinImage = coin.GetComponent<Image>();
            if (coinImage != null)
            {
                coinImage.preserveAspect = true;
            }

            TextMeshProUGUI subText = EnsureText(row, $"{buttonName}SubText");
            subText.text = value;
            ConfigureText(subText, 21f, 14f, TextAlignmentOptions.Center, new Color(0.88f, 0.68f, 0.3f, 1f));
            ApplyGameplayButtonTextStyle(subText, 0.08f);
            SetAnchored(subText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -2f), new Vector2(13f, 0f));
            AddTextOutline(subText, new Color(0.18f, 0.055f, 0f, 0.62f), new Vector2(0.65f, -0.65f));
            row.gameObject.SetActive(true);
            return row;
        }

        private void PlayBadgePulse(RectTransform badge, ref Coroutine routine)
        {
            if (badge == null || !badge.gameObject.activeInHierarchy || !isActiveAndEnabled)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(PulseBadge(badge));
        }

        private IEnumerator PulseBadge(RectTransform badge)
        {
            Vector3 baseScale = Vector3.one;
            float duration = 0.26f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                badge.localScale = baseScale * (1f + (wave * 0.16f));
                yield return null;
            }

            badge.localScale = baseScale;
        }

        private void EnsureBackgroundVignette()
        {
            RectTransform background = FindChildRecursive<RectTransform>(safeAreaRoot, "Background");
            RectTransform tableWash = FindChildRecursive<RectTransform>(safeAreaRoot, "GameplayTableWash");
            if (tableWash == null)
            {
                GameObject washObject = new("GameplayTableWash", typeof(RectTransform), typeof(Image));
                tableWash = washObject.GetComponent<RectTransform>();
                tableWash.SetParent(safeAreaRoot, false);
            }

            StretchToFill(tableWash, 0f, 0f, 0f, 0f);
            Image washImage = EnsureGraphic<Image>(tableWash.gameObject);
            washImage.sprite = null;
            washImage.color = new Color(0.04f, 0.78f, 0.34f, 0.24f);
            washImage.raycastTarget = false;

            if (background != null)
            {
                tableWash.SetSiblingIndex(background.GetSiblingIndex() + 1);
            }

            RectTransform existing = FindChildRecursive<RectTransform>(safeAreaRoot, "GameplaySoftVignette");
            if (existing == null)
            {
                GameObject overlayObject = new("GameplaySoftVignette", typeof(RectTransform), typeof(Image));
                existing = overlayObject.GetComponent<RectTransform>();
                existing.SetParent(safeAreaRoot, false);
            }

            StretchToFill(existing, 0f, 0f, 0f, 0f);
            Image image = EnsureGraphic<Image>(existing.gameObject);
            image.sprite = GetVignetteSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.58f);
            image.raycastTarget = false;

            if (tableWash != null)
            {
                existing.SetSiblingIndex(tableWash.GetSiblingIndex() + 1);
            }
        }

        private void HideAdPlaceholder()
        {
            RectTransform adPlaceholder = FindChildRecursive<RectTransform>(safeAreaRoot, "AdBannerPlaceholder");
            if (adPlaceholder != null)
            {
                adPlaceholder.gameObject.SetActive(false);
            }
        }

        private void WireButton(Button button, string buttonName, System.Action onClick = null)
        {
            if (button == null)
            {
                return;
            }

            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            if (wiredButtons.Add(button))
            {
                button.onClick.AddListener(() =>
                {
                    Debug.Log($"UIManager: {buttonName} clicked.");
                    onClick?.Invoke();
                });
            }
        }

        private void WirePauseButton(GameManager gameManager)
        {
            rewardBonusButton ??= FindChildRecursive<Button>(safeAreaRoot, "RewardBonusButton");
            rewardBonusButton ??= menuButton;
            if (rewardBonusButton == null)
            {
                return;
            }

            pauseRestartAction = gameManager != null ? (Action)gameManager.RequestRestartWithConfirmation : null;
            pauseMainMenuAction = gameManager != null ? (Action)gameManager.RequestReturnToMainMenu : null;
            rewardBonusButton.gameObject.SetActive(true);
            rewardBonusButton.interactable = true;

            Image rewardImage = rewardBonusButton.GetComponent<Image>();
            if (rewardImage != null)
            {
                rewardImage.raycastTarget = true;
                rewardBonusButton.targetGraphic = rewardImage;
            }

            if (rewardBonusButton.GetComponent<UIJuicyButton>() == null)
            {
                rewardBonusButton.gameObject.AddComponent<UIJuicyButton>();
            }

            ScenePauseMenuButton scenePauseButton = rewardBonusButton.GetComponent<ScenePauseMenuButton>();
            if (scenePauseButton == null)
            {
                scenePauseButton = rewardBonusButton.gameObject.AddComponent<ScenePauseMenuButton>();
            }
            EnsurePausePopup();
            scenePauseButton.SetPopup(pausePopup);
            scenePauseButton.enabled = false;

            rewardBonusButton.onClick.RemoveAllListeners();
            rewardBonusButton.onClick.AddListener(() =>
            {
                Debug.Log("UIManager: RewardBonusButton clicked.");
                ShowPausePopup();
            });
            wiredButtons.Add(rewardBonusButton);
        }

        private void ShowPausePopup()
        {
            EnsurePausePopup();
            if (pausePopup != null)
            {
                PauseMenuPopupController controller = pausePopup.GetComponent<PauseMenuPopupController>();
                if (controller != null)
                {
                    controller.SetRestartAction(pauseRestartAction);
                    controller.SetMainMenuAction(pauseMainMenuAction);
                    controller.Show();
                    return;
                }

                if (safeAreaRoot != null && pausePopup.transform.parent != safeAreaRoot)
                {
                    pausePopup.transform.SetParent(safeAreaRoot, false);
                }

                pausePopup.SetActive(true);
                pausePopup.transform.SetAsLastSibling();
                if (pausePopup.transform is RectTransform popupRect)
                {
                    StretchToFill(popupRect, 0f, 0f, 0f, 0f);
                }

                CanvasGroup popupGroup = pausePopup.GetComponent<CanvasGroup>();
                if (popupGroup != null)
                {
                    popupGroup.alpha = 1f;
                    popupGroup.interactable = true;
                    popupGroup.blocksRaycasts = true;
                }

                Debug.Log($"UIManager: Pause menu opened via authored {GetHierarchyPath(pausePopup.transform)}.");
                Time.timeScale = 0f;
                return;
            }

            Debug.LogWarning("UIManager: Pause menu click received, but authored PauseSettingsPopup was not found in the scene.");
        }

        private void ClosePausePopup()
        {
            ClosePausePopup(true);
        }

        private void ClosePausePopup(bool resumeGame)
        {
            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (resumeGame && !HasActiveModal())
            {
                Time.timeScale = 1f;
            }
        }

        private void EnsurePausePopup()
        {
            if (pausePopup != null)
            {
                WireAuthoredPausePopupButtons();
                return;
            }

            safeAreaRoot ??= sceneRefs != null ? sceneRefs.SafeAreaRoot : null;
            if (safeAreaRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
                safeAreaRoot = canvas != null ? canvas.transform as RectTransform : null;
            }

            if (safeAreaRoot == null)
            {
                return;
            }

            Transform existing = safeAreaRoot.Find("PauseSettingsPopup") ?? FindChildRecursive<RectTransform>(safeAreaRoot, "PauseSettingsPopup");
            if (existing == null)
            {
                foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate != null && candidate.name == "PauseSettingsPopup")
                    {
                        existing = candidate;
                        break;
                    }
                }
            }

            if (existing != null)
            {
                pausePopup = existing.gameObject;
                WireAuthoredPausePopupButtons();
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            Stack<string> path = new();
            Transform current = transform;
            while (current != null)
            {
                path.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        private void WireAuthoredPausePopupButtons()
        {
            if (pausePopup == null)
            {
                return;
            }

            PauseMenuPopupController controller = pausePopup.GetComponent<PauseMenuPopupController>();
            if (controller != null)
            {
                controller.SetRestartAction(pauseRestartAction);
                controller.SetMainMenuAction(pauseMainMenuAction);
                controller.WireButtons();
                return;
            }

            Button continueButton = FindChildRecursive<Button>(pausePopup.transform, "ContinueButton");
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(ClosePausePopup);
                if (continueButton.GetComponent<UIJuicyButton>() == null)
                {
                    continueButton.gameObject.AddComponent<UIJuicyButton>();
                }
            }

            Button restartButton = FindChildRecursive<Button>(pausePopup.transform, "RestartButton");
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => pauseRestartAction?.Invoke());
                if (restartButton.GetComponent<UIJuicyButton>() == null)
                {
                    restartButton.gameObject.AddComponent<UIJuicyButton>();
                }
            }

            Button mainMenuButton = FindChildRecursive<Button>(pausePopup.transform, "MainMenuButton");
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() => pauseMainMenuAction?.Invoke());
                if (mainMenuButton.GetComponent<UIJuicyButton>() == null)
                {
                    mainMenuButton.gameObject.AddComponent<UIJuicyButton>();
                }
            }
        }

        private void EnsureRestartConfirmationPopup(Func<bool> onConfirmed, Action onClosed)
        {
            if (restartConfirmationPopup == null)
            {
                restartConfirmationPopup = FindAuthoredModal("RestartConfirmationPopup");
                if (restartConfirmationPopup == null)
                {
                    Debug.LogWarning("UIManager: authored RestartConfirmationPopup was not found in the scene.");
                    return;
                }
            }

            bool confirmConsumed = false;
            bool closeNotified = false;
            void NotifyClosed()
            {
                if (closeNotified)
                {
                    return;
                }

                closeNotified = true;
                onClosed?.Invoke();
            }

            Action cancel = () =>
            {
                HideModal(restartConfirmationPopup);
                NotifyClosed();
            };

            Action confirm = () =>
            {
                if (confirmConsumed)
                {
                    return;
                }

                confirmConsumed = true;
                bool restarted = onConfirmed?.Invoke() == true;
                if (restarted)
                {
                    HideModal(restartConfirmationPopup);
                    NotifyClosed();
                }
                else if (restartConfirmationPopup != null)
                {
                    restartConfirmationPopup.SetActive(false);
                    if (!HasActiveModal())
                    {
                        Time.timeScale = 1f;
                    }

                    NotifyClosed();
                }
            };

            RestartConfirmationPopupController controller = restartConfirmationPopup.GetComponent<RestartConfirmationPopupController>();
            if (controller != null)
            {
                controller.SetActions(confirm, cancel);
                return;
            }

            Button primary = FindChildRecursive<Button>(restartConfirmationPopup.transform, "ConfirmButton")
                ?? FindChildRecursive<Button>(restartConfirmationPopup.transform, "ContinueButton");
            if (primary != null)
            {
                primary.onClick.RemoveAllListeners();
                primary.onClick.AddListener(() => confirm());
            }

            Button cancelButton = FindChildRecursive<Button>(restartConfirmationPopup.transform, "CancelButton");
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => cancel());
            }

            Button closeButton = FindChildRecursive<Button>(restartConfirmationPopup.transform, "CloseButton");
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => cancel());
            }
        }

        private GameObject FindAuthoredModal(string modalName)
        {
            safeAreaRoot ??= sceneRefs != null ? sceneRefs.SafeAreaRoot : null;
            if (safeAreaRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
                safeAreaRoot = canvas != null ? canvas.transform as RectTransform : null;
            }

            Transform existing = safeAreaRoot != null
                ? safeAreaRoot.Find(modalName) ?? FindChildRecursive<RectTransform>(safeAreaRoot, modalName)
                : null;
            if (existing != null)
            {
                return existing.gameObject;
            }

            foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.name == modalName)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private void EnsureLevelFailedPopup(Func<bool> onRetry)
        {
            if (levelFailedPopup == null)
            {
                levelFailedPopup = CreateModalRoot("LevelFailedPopup", out RectTransform panel, new Vector2(430f, 286f));
                CreateModalTitle(panel, "Уровень не пройден", new Vector2(-44f, 72f), new Vector2(0f, -30f), 33f);

                TextMeshProUGUI body = CreateText("Body", panel, "Попробуйте еще раз", 27f, new Color(0.38f, 0.25f, 0.12f, 1f));
                body.fontStyle = FontStyles.Bold;
                SetAnchored(body.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-58f, 48f), new Vector2(0f, 12f));

                Button retryButton = CreateButton("RetryButton", panel, "Попробовать снова");
                SetAnchored(retryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 64f), new Vector2(0f, 34f));
                ConfigureModalButton(retryButton, true);
            }

            Button retry = FindChildRecursive<Button>(levelFailedPopup.transform, "RetryButton");
            if (retry != null)
            {
                retry.onClick.RemoveAllListeners();
                retry.onClick.AddListener(() =>
                {
                    bool restarted = onRetry?.Invoke() == true;
                    if (restarted)
                    {
                        HideModal(levelFailedPopup);
                    }
                });
            }
        }

        private void EnsureLivesEmptyPopup()
        {
            if (livesEmptyPopup != null)
            {
                return;
            }

            livesEmptyPopup = CreateModalRoot("LivesEmptyPopup", out RectTransform panel, new Vector2(420f, 280f));
            CreateModalTitle(panel, "Жизни закончились", new Vector2(0f, 72f), new Vector2(0f, -30f), 34f);

            livesEmptyTimerText = CreateText("Timer", panel, string.Empty, 26f, new Color(0.38f, 0.25f, 0.12f, 1f));
            livesEmptyTimerText.fontStyle = FontStyles.Bold;
            SetAnchored(livesEmptyTimerText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-52f, 52f), new Vector2(0f, 18f));

            Button okButton = CreateButton("OkButton", panel, "Понятно");
            SetAnchored(okButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280f, 62f), new Vector2(0f, 34f));
            ConfigureModalButton(okButton, true);
            okButton.onClick.AddListener(() => HideModal(livesEmptyPopup));
        }

        private IEnumerator UpdateLivesEmptyTimer()
        {
            while (livesEmptyPopup != null && livesEmptyPopup.activeSelf)
            {
                UpdateLivesEmptyTimerText();
                yield return new WaitForSecondsRealtime(0.25f);
            }

            livesEmptyTimerRoutine = null;
        }

        private void UpdateLivesEmptyTimerText()
        {
            if (livesEmptyTimerText == null)
            {
                return;
            }

            TimeSpan timeToNextLife = activeLivesManager != null
                ? activeLivesManager.GetTimeToNextLife()
                : TimeSpan.FromSeconds(LivesManager.RestoreIntervalSeconds);
            int minutes = Mathf.FloorToInt((float)timeToNextLife.TotalMinutes);
            int seconds = Mathf.Clamp(timeToNextLife.Seconds, 0, 59);
            livesEmptyTimerText.text = $"Следующая жизнь через {minutes:00}:{seconds:00}";
        }

        private GameObject CreateModalRoot(string name, out RectTransform panel, Vector2 panelSize)
        {
            GameObject overlayObject = new(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform overlay = overlayObject.GetComponent<RectTransform>();
            overlay.SetParent(safeAreaRoot, false);
            StretchToFill(overlay, 0f, 0f, 0f, 0f);

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0.01f, 0.045f, 0.028f, 0.66f);
            overlayImage.raycastTarget = true;

            panel = CreateRect("Panel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), panelSize, Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = GetPauseWindowSprite();
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

            overlayObject.SetActive(false);
            return overlayObject;
        }

        private TextMeshProUGUI CreateModalTitle(RectTransform panel, string value, Vector2 sizeDelta, Vector2 anchoredPosition, float fontSize)
        {
            TextMeshProUGUI title = CreateText("Title", panel, value, fontSize, new Color(0.98f, 0.86f, 0.52f, 1f));
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), sizeDelta, anchoredPosition);
            AddTextOutline(title, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
            return title;
        }

        private Button CreateCloseButton(RectTransform panel, Action onClick)
        {
            RectTransform closeRoot = CreateRect("CloseButton", panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(48f, 48f), new Vector2(-34f, -34f));
            Image image = closeRoot.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            Button button = closeRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI label = CreateText("Label", closeRoot, "X", 36f, new Color(0.95f, 0.16f, 0.16f, 1f));
            label.fontStyle = FontStyles.Bold;
            StretchToFill(label.rectTransform, 0f, 0f, 0f, 0f);
            button.onClick.AddListener(() => onClick?.Invoke());
            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            return button;
        }

        private void HideModal(GameObject popup)
        {
            if (popup != null)
            {
                popup.SetActive(false);
            }

            if (popup == livesEmptyPopup && livesEmptyTimerRoutine != null)
            {
                StopCoroutine(livesEmptyTimerRoutine);
                livesEmptyTimerRoutine = null;
            }

            if (!HasActiveModal())
            {
                Time.timeScale = 1f;
            }
        }

        private bool HasActiveModal()
        {
            return (pausePopup != null && pausePopup.activeSelf) ||
                (restartConfirmationPopup != null && restartConfirmationPopup.activeSelf) ||
                (livesEmptyPopup != null && livesEmptyPopup.activeSelf) ||
                (levelFailedPopup != null && levelFailedPopup.activeSelf);
        }

        private static void SetButtonInteractable(Button button, bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
            }
        }

        private void EnsureMovesBonusAnchors()
        {
            if (safeAreaRoot == null && sceneRefs != null)
            {
                safeAreaRoot = sceneRefs.SafeAreaRoot;
            }

            if (safeAreaRoot == null)
            {
                return;
            }

            movesPanel ??= FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
            coinsPanel ??= FindChildRecursive<RectTransform>(safeAreaRoot, "CoinsPanel");
        }

        private void EnsureMovesBonusText()
        {
            EnsureMovesBonusAnchors();
            if (safeAreaRoot == null)
            {
                return;
            }

            if (movesBonusText == null)
            {
                movesBonusText = CreateText("MovesBonusText", safeAreaRoot, string.Empty, 34f, new Color(1f, 0.84f, 0.24f, 1f));
                movesBonusText.fontStyle = FontStyles.Bold;
                movesBonusText.enableWordWrapping = false;
                SetAnchored(movesBonusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(360f, 54f), new Vector2(0f, -198f));
                AddTextOutline(movesBonusText, new Color(0.22f, 0.1f, 0f, 0.46f), new Vector2(1.2f, -1.2f));
                AddShadow(movesBonusText.gameObject, new Color(0.13f, 0.06f, 0f, 0.22f), new Vector2(0f, -2f));
                movesBonusText.gameObject.SetActive(false);
            }

            movesBonusGroup = movesBonusText.GetComponent<CanvasGroup>();
            if (movesBonusGroup == null)
            {
                movesBonusGroup = movesBonusText.gameObject.AddComponent<CanvasGroup>();
            }

            movesBonusGroup.blocksRaycasts = false;
            movesBonusGroup.interactable = false;
        }

        private Image CreateBonusCoin()
        {
            Image coin = CreateSparkle("MovesBonusCoin");
            coin.sprite = GetCoinIconSprite();
            coin.type = Image.Type.Simple;
            coin.preserveAspect = true;
            coin.color = Color.white;
            AddShadow(coin.gameObject, new Color(0.25f, 0.12f, 0f, 0.24f), new Vector2(0f, -2f));
            return coin;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, bool deactivateWhenDone)
        {
            if (group == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                group.alpha = to;
                if (deactivateWhenDone)
                {
                    group.gameObject.SetActive(false);
                }

                yield break;
            }

            group.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - (2f * progress));
                group.alpha = Mathf.Lerp(from, to, eased);
                yield return null;
            }

            group.alpha = to;
            if (deactivateWhenDone)
            {
                group.gameObject.SetActive(false);
            }
        }

        private IEnumerator PulseCoins()
        {
            Transform target = coinsPanel != null ? coinsPanel : coinsLabel.transform;
            Vector3 baseScale = Vector3.one;
            float duration = 0.24f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                target.localScale = baseScale * (1f + (wave * 0.1f));
                yield return null;
            }

            target.localScale = baseScale;
            coinsPulseRoutine = null;
        }

        private void SpawnFloatingText(RectTransform source, string text)
        {
            if (source == null || safeAreaRoot == null)
            {
                return;
            }

            TextMeshProUGUI popup = CreateText("CoinsAwardPopup", safeAreaRoot, text, 34f, new Color(1f, 0.83f, 0.18f, 1f));
            popup.fontStyle = FontStyles.Bold;
            popup.raycastTarget = false;
            popup.rectTransform.position = source.position;
            popup.rectTransform.sizeDelta = new Vector2(120f, 48f);
            StartCoroutine(FloatAndFade(popup.rectTransform, popup, new Vector2(0f, 66f), 0.72f));
        }

        private IEnumerator SpawnGoalSparkles(RectTransform source, int amount)
        {
            Vector3 center = source.position;
            for (int index = 0; index < 8; index++)
            {
                Image sparkle = CreateSparkle($"GoalSparkle_{index}");
                RectTransform sparkleRect = sparkle.rectTransform;
                sparkleRect.SetParent(safeAreaRoot, false);
                sparkleRect.position = center;
                sparkleRect.sizeDelta = new Vector2(18f + (index % 3) * 4f, 18f + (index % 3) * 4f);
                float angle = (Mathf.PI * 2f * index) / 8f;
                Vector2 offset = new(Mathf.Cos(angle) * 74f, Mathf.Sin(angle) * 48f);
                StartCoroutine(FloatAndFade(sparkleRect, sparkle, offset, 0.58f));
            }

            if (amount > 0)
            {
                SpawnFloatingText(source, $"+{amount}");
            }
            yield return null;
        }

        private IEnumerator FloatAndFade(RectTransform rectTransform, Graphic graphic, Vector2 offset, float duration)
        {
            Vector2 start = rectTransform.anchoredPosition;
            Vector2 target = start + offset;
            Color startColor = graphic.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, progress);
                graphic.color = color;
                yield return null;
            }

            Destroy(rectTransform.gameObject);
        }

        private static Image CreateSparkle(string name)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            Image image = gameObject.GetComponent<Image>();
            image.sprite = GetRoundSprite();
            image.color = new Color(1f, 0.86f, 0.25f, 0.88f);
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 62f), Vector2.zero);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = GetGameplayActionButtonSprite();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddShadow(root.gameObject, new Color(0.03f, 0.015f, 0f, 0.34f), new Vector2(0f, -5f));

            TextMeshProUGUI text = CreateText("Label", root, label, 26f, new Color(0.98f, 0.73f, 0.2f, 1f));
            text.fontStyle = FontStyles.Bold;
            StretchToFill(text.rectTransform, 12f, 12f, 0f, 0f);
            AddTextOutline(text, new Color(0.15f, 0.07f, 0.015f, 0.62f), new Vector2(1f, -1f));
            return button;
        }

        private static void ConfigureModalButton(Button button, bool isPrimary)
        {
            if (button == null)
            {
                return;
            }

            Image image = EnsureGraphic<Image>(button.gameObject);
            image.sprite = GetGameplayActionButtonSprite();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = isPrimary ? Color.white : new Color(0.92f, 0.92f, 0.88f, 1f);
            button.targetGraphic = image;
            AddShadow(button.gameObject, new Color(0.03f, 0.015f, 0f, 0.32f), new Vector2(0f, -5f));

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                ConfigureText(text, 30f, 20f, TextAlignmentOptions.Center, new Color(0.98f, 0.73f, 0.2f, 1f));
                text.fontStyle = FontStyles.Bold;
                AddTextOutline(text, new Color(0.15f, 0.07f, 0.015f, 0.62f), new Vector2(1f, -1f));
            }
        }

        private static void ConfigurePauseMenuButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = EnsureGraphic<Image>(button.gameObject);
            image.sprite = GetPrimaryButtonSprite();
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(1f, 0.93f, 0.74f, 1f);
            button.targetGraphic = image;
            AddShadow(button.gameObject, new Color(0.18f, 0.09f, 0f, 0.28f), new Vector2(0f, -5f));

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                ConfigureText(text, 30f, 20f, TextAlignmentOptions.Center, new Color(0.3f, 0.16f, 0.04f, 1f));
                text.fontStyle = FontStyles.Bold;
                AddTextOutline(text, new Color(1f, 0.98f, 0.82f, 0.34f), new Vector2(0f, -0.7f));
            }
        }

        private static TextMeshProUGUI EnsureText(RectTransform parent, string name)
        {
            RectTransform existing = FindChildRecursive<RectTransform>(parent, name);
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI text))
            {
                return text;
            }

            return CreateText(name, parent, string.Empty, 24f, Color.white);
        }

        private static TextMeshProUGUI EnsureDirectText(RectTransform parent, string name)
        {
            TextMeshProUGUI existing = FindDirectText(parent, name);
            if (existing != null)
            {
                return existing;
            }

            return CreateText(name, parent, string.Empty, 24f, Color.white);
        }

        private static TextMeshProUGUI FindDirectText(RectTransform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            TextMeshProUGUI inactiveMatch = null;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name != name || !child.TryGetComponent(out TextMeshProUGUI text))
                {
                    continue;
                }

                if (child.gameObject.activeSelf)
                {
                    return text;
                }

                inactiveMatch ??= text;
            }

            return inactiveMatch;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10f, fontSize * 0.62f);
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            SetAnchored(rectTransform, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return rectTransform;
        }

        private static void ConfigureText(TMP_Text text, float fontSizeMax, float fontSizeMin, TextAlignmentOptions alignment, Color color)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSizeMax;
            text.fontSizeMin = fontSizeMin;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = alignment;
            text.color = color;
        }

        private static void ApplyGameplayButtonTextStyle(TMP_Text text, float outlineWidth)
        {
            if (text == null)
            {
                return;
            }

            text.fontStyle = FontStyles.Bold;
            text.fontWeight = FontWeight.Bold;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(
                new Color(0.98f, 0.88f, 0.48f, 1f),
                new Color(0.94f, 0.74f, 0.28f, 1f),
                new Color(0.58f, 0.32f, 0.06f, 1f),
                new Color(0.72f, 0.44f, 0.09f, 1f));

            Material material = text.fontMaterial;
            if (material == null)
            {
                return;
            }

            material = new Material(material);
            if (material.HasProperty(ShaderUtilities.ID_FaceColor))
            {
                material.SetColor(ShaderUtilities.ID_FaceColor, new Color(0.96f, 0.77f, 0.32f, 1f));
            }

            if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.11f, 0.052f, 0.012f, 1f));
            }

            if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            }

            if (material.HasProperty(ShaderUtilities.ID_GlowColor))
            {
                material.SetColor(ShaderUtilities.ID_GlowColor, Color.clear);
            }

            text.fontMaterial = material;
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

        private static void AddTextOutline(TMP_Text target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddImageOutline(Image target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void EnsureMenuBar(RectTransform parent, string name, Vector2 anchoredPosition)
        {
            RectTransform bar = FindChildRecursive<RectTransform>(parent, name);
            if (bar == null)
            {
                bar = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(36f, 6f), anchoredPosition);
            }
            else
            {
                SetAnchored(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(36f, 6f), anchoredPosition);
            }

            Image image = EnsureGraphic<Image>(bar.gameObject);
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.9f, 1f, 0.78f, 1f);
            image.raycastTarget = false;
        }

        private static void RemoveChildIfExists(RectTransform parent, string name)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        private static T EnsureGraphic<T>(GameObject target) where T : Graphic
        {
            T graphic = target.GetComponent<T>();
            if (graphic == null)
            {
                graphic = target.AddComponent<T>();
            }

            return graphic;
        }

        private static T FindChildRecursive<T>(Transform root, string objectName) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.GetComponent<T>();
                }
            }

            return null;
        }

        private static void StretchToFill(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
        }

        private static Sprite GetRoundedPanelSprite()
        {
            return roundedPanelSprite != null
                ? roundedPanelSprite
                : roundedPanelSprite = CreateRoundedSprite(64, 14);
        }

        private static Sprite GetRoundedButtonSprite()
        {
            return roundedButtonSprite != null
                ? roundedButtonSprite
                : roundedButtonSprite = CreateRoundedSprite(64, 18);
        }

        private static Sprite GetPrimaryButtonSprite()
        {
            return PremiumUISprites.PrimaryButton != null
                ? PremiumUISprites.PrimaryButton
                : GetRoundedButtonSprite();
        }

        private static Sprite GetGameplayActionButtonSprite()
        {
            return PremiumUISprites.GameplayActionButton != null
                ? PremiumUISprites.GameplayActionButton
                : GetPrimaryButtonSprite();
        }

        private static Sprite GetPauseWindowSprite()
        {
            return PremiumUISprites.PauseWindowCardMesh != null
                ? PremiumUISprites.PauseWindowCardMesh
                : GetHudPanelCreamGoldSprite();
        }

        private static Sprite GetHudCounterGoldSprite()
        {
            return PremiumUISprites.HudCounterGold != null
                ? PremiumUISprites.HudCounterGold
                : GetPrimaryButtonSprite();
        }

        private static Sprite GetHudPanelCreamGoldSprite()
        {
            return PremiumUISprites.HudPanelCreamGold != null
                ? PremiumUISprites.HudPanelCreamGold
                : GetCreamPanelSprite();
        }

        private static Sprite GetSecondaryButtonSprite()
        {
            return PremiumUISprites.SecondaryButton != null
                ? PremiumUISprites.SecondaryButton
                : GetRoundedButtonSprite();
        }

        private static Sprite GetGoldIconSprite()
        {
            return PremiumUISprites.IconCircle != null
                ? PremiumUISprites.IconCircle
                : GetRoundSprite();
        }

        private static Sprite GetCoinIconSprite()
        {
            return PremiumUISprites.CoinIcon != null
                ? PremiumUISprites.CoinIcon
                : GetGoldIconSprite();
        }

        private static Sprite GetMenuButtonSprite()
        {
            return PremiumUISprites.MenuButtonCardMesh != null
                ? PremiumUISprites.MenuButtonCardMesh
                : GetRoundSprite();
        }

        private static Sprite GetCreamPanelSprite()
        {
            return PremiumUISprites.CreamPanel != null
                ? PremiumUISprites.CreamPanel
                : GetRoundedPanelSprite();
        }

        private static Sprite GetRoundSprite()
        {
            return roundSprite != null
                ? roundSprite
                : roundSprite = CreateRoundedSprite(64, 32);
        }

        private static Sprite GetEmeraldIconSprite()
        {
            return PremiumUISprites.EmeraldIconCircle != null
                ? PremiumUISprites.EmeraldIconCircle
                : GetRoundSprite();
        }

        private static Sprite GetHeartSprite()
        {
            if (heartSprite != null)
            {
                return heartSprite;
            }

            heartSprite = Resources.Load<Sprite>("UI/Premium/icon_lives_heart");
            if (heartSprite != null)
            {
                return heartSprite;
            }

            const int size = 128;
            const int curvePoints = 160;
            const int samplesPerAxis = 3;
            Vector2[] points = new Vector2[curvePoints];
            for (int index = 0; index < curvePoints; index++)
            {
                float t = (Mathf.PI * 2f * index) / curvePoints;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = (13f * Mathf.Cos(t)) - (5f * Mathf.Cos(2f * t)) - (2f * Mathf.Cos(3f * t)) - Mathf.Cos(4f * t);
                points[index] = new Vector2(x / 18f, (y + 2.4f) / 17.2f);
            }

            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.name = "RuntimeHeartIcon";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int coveredSamples = 0;
                    for (int sampleY = 0; sampleY < samplesPerAxis; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < samplesPerAxis; sampleX++)
                        {
                            float px = (((x + ((sampleX + 0.5f) / samplesPerAxis)) / size) * 2f) - 1f;
                            float py = (((y + ((sampleY + 0.5f) / samplesPerAxis)) / size) * 2f) - 1f;
                            if (IsPointInsidePolygon(new Vector2(px, py), points))
                            {
                                coveredSamples++;
                            }
                        }
                    }

                    float alpha = coveredSamples / (float)(samplesPerAxis * samplesPerAxis);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            heartSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return heartSprite;
        }

        private static bool IsPointInsidePolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool isInside = false;
            int previousIndex = polygon.Count - 1;
            for (int index = 0; index < polygon.Count; index++)
            {
                Vector2 current = polygon[index];
                Vector2 previous = polygon[previousIndex];
                bool crossesY = current.y > point.y != previous.y > point.y;
                if (crossesY)
                {
                    float intersectionX = ((previous.x - current.x) * (point.y - current.y) / (previous.y - current.y)) + current.x;
                    if (point.x < intersectionX)
                    {
                        isInside = !isInside;
                    }
                }

                previousIndex = index;
            }

            return isInside;
        }

        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.name = $"RuntimeRounded_{radius}";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            float centerMin = radius - 0.5f;
            float centerMax = size - radius + 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x, centerMin, centerMax);
                    float nearestY = Mathf.Clamp(y, centerMin, centerMax);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite GetVignetteSprite()
        {
            if (vignetteSprite != null)
            {
                return vignetteSprite;
            }

            if (PremiumUISprites.SoftVignette != null)
            {
                return PremiumUISprites.SoftVignette;
            }

            int size = 256;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.name = "RuntimeGameplayVignette";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new(size * 0.5f, size * 0.48f);
            float maxDistance = Vector2.Distance(Vector2.zero, center);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                    float edge = Mathf.SmoothStep(0.42f, 1f, distance);
                    float highlight = Mathf.Clamp01(1f - (distance / 0.42f));
                    Color color = Color.Lerp(
                        new Color(1f, 0.95f, 0.42f, 0.08f * highlight),
                        new Color(0f, 0.05f, 0.02f, 0.16f * edge),
                        edge);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return vignetteSprite;
        }
    }
}
