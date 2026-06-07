using System.Collections.Generic;
using System.Collections;
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
        private const float RightDeckInset = 440f;
        private static readonly Vector2 GameplayCardSize = new(196f, 286f);
        private const float BottomButtonWidth = 292f;
        private const float BottomButtonHeight = 66f;

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

        private readonly HashSet<Button> wiredButtons = new();

        private SolitaireSceneRefs sceneRefs;
        private RectTransform safeAreaRoot;
        private RectTransform coinsPanel;
        private GameObject pausePopup;
        private Coroutine coinsPulseRoutine;

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
            hintButton = sceneRefs != null ? sceneRefs.HintButton : null;
            undoButton = sceneRefs != null ? sceneRefs.UndoButton : null;
            boosterDeckButton = sceneRefs != null ? sceneRefs.BoosterDeckButton : null;
        }

        public void Initialize(GameManager gameManager, IReadOnlyList<GameManager.ObjectiveState> objectiveStates)
        {
            ResetPauseState();
            ApplyGameplayPolish();

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(gameManager.RestartLevel);
            }

            WireButton(menuButton, "MenuButton");
            WireButton(plusButton, "PlusButton");
            WirePauseButton();
            WireButton(hintButton, "HintButton");
            WireButton(undoButton, "UndoButton");
            WireButton(boosterDeckButton, "BoosterDeckButton");

            ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            UpdateDeckCounter(0);
            SetBoosterBadgeVisible(boosterBadge != null && boosterBadge.activeSelf);
        }

        public void ResetPauseState()
        {
            Time.timeScale = 1f;

            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void UpdateMoves(int remainingMoves)
        {
            if (movesLabel != null)
            {
                movesLabel.text = remainingMoves.ToString();
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
                boosterBadge.SetActive(isVisible);
            }
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

            EnsureBackgroundVignette();
            StyleTopHud();
            StyleMovesPanel();
            StyleDeckArea();
            StyleBottomButtons();
            HideAdPlaceholder();
        }

        private void StyleTopHud()
        {
            RectTransform topHud = FindChildRecursive<RectTransform>(safeAreaRoot, "TopHUD");
            if (topHud != null)
            {
                SetAnchored(topHud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 128f), new Vector2(0f, -18f));
            }

            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(false);
            }

            coinsPanel = FindChildRecursive<RectTransform>(safeAreaRoot, "CoinsPanel");
            if (coinsPanel != null)
            {
                SetAnchored(coinsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(252f, 70f), new Vector2(204f, -HudTopInset));
                Image panelImage = EnsureGraphic<Image>(coinsPanel.gameObject);
                panelImage.sprite = GetRoundedPanelSprite();
                panelImage.type = Image.Type.Sliced;
                panelImage.color = new Color(0.12f, 0.7f, 0.32f, 0.94f);
                AddShadow(coinsPanel.gameObject, new Color(0.01f, 0.11f, 0.04f, 0.28f), new Vector2(0f, -5f));

                RectTransform coinIcon = FindChildRecursive<RectTransform>(coinsPanel, "CoinIcon");
                if (coinIcon != null)
                {
                    SetAnchored(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 46f), new Vector2(36f, 0f));
                    Image coinImage = EnsureGraphic<Image>(coinIcon.gameObject);
                    coinImage.sprite = GetRoundSprite();
                    coinImage.type = Image.Type.Sliced;
                    coinImage.color = new Color(1f, 0.82f, 0.18f, 1f);
                    AddShadow(coinIcon.gameObject, new Color(0.46f, 0.2f, 0f, 0.32f), new Vector2(0f, -3f));

                    TextMeshProUGUI glyph = EnsureText(coinIcon, "CoinGlyph");
                    glyph.text = "C";
                    glyph.fontSize = 24f;
                    glyph.fontStyle = FontStyles.Bold;
                    glyph.alignment = TextAlignmentOptions.Center;
                    glyph.color = new Color(0.49f, 0.23f, 0.02f, 1f);
                    StretchToFill(glyph.rectTransform, 0f, 0f, 0f, 0f);
                }

                if (coinsLabel != null)
                {
                    RectTransform coinsRect = coinsLabel.rectTransform;
                    SetAnchored(coinsRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f, 46f), new Vector2(38f, 0f));
                    ConfigureText(coinsLabel, 38f, 24f, TextAlignmentOptions.MidlineLeft, Color.white);
                    coinsLabel.fontStyle = FontStyles.Bold;
                    AddTextOutline(coinsLabel, new Color(0f, 0.05f, 0.02f, 0.5f), new Vector2(1f, -1f));
                }
            }

            if (plusButton != null)
            {
                plusButton.gameObject.SetActive(false);
            }

            if (rewardBonusButton != null)
            {
                RectTransform pauseRoot = rewardBonusButton.GetComponent<RectTransform>();
                SetAnchored(pauseRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(68f, 68f), new Vector2(-112f, -92f));
                Image pauseImage = EnsureGraphic<Image>(rewardBonusButton.gameObject);
                pauseImage.sprite = GetRoundSprite();
                pauseImage.type = Image.Type.Sliced;
                pauseImage.color = new Color(0.1f, 0.62f, 0.31f, 0.96f);
                AddShadow(rewardBonusButton.gameObject, new Color(0f, 0.1f, 0.04f, 0.32f), new Vector2(0f, -5f));

                TextMeshProUGUI pauseIcon = EnsureText(pauseRoot, "PauseIcon");
                pauseIcon.text = string.Empty;
                pauseIcon.fontSize = 34f;
                pauseIcon.fontStyle = FontStyles.Bold;
                pauseIcon.alignment = TextAlignmentOptions.Center;
                pauseIcon.color = new Color(0.9f, 1f, 0.78f, 1f);
                StretchToFill(pauseIcon.rectTransform, 0f, 0f, 0f, 0f);

                EnsureMenuBar(pauseRoot, "MenuBarTop", new Vector2(0f, 14f));
                EnsureMenuBar(pauseRoot, "MenuBarMiddle", Vector2.zero);
                EnsureMenuBar(pauseRoot, "MenuBarBottom", new Vector2(0f, -14f));
            }
        }

        private void StyleMovesPanel()
        {
            RectTransform movesPanel = FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
            if (movesPanel == null)
            {
                return;
            }

            SetAnchored(movesPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(164f, 110f), new Vector2(158f, -MovesTopInset));
            Image panelImage = EnsureGraphic<Image>(movesPanel.gameObject);
            panelImage.sprite = GetRoundedPanelSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.12f, 0.76f, 0.38f, 0.93f);
            AddShadow(movesPanel.gameObject, new Color(0.01f, 0.11f, 0.04f, 0.28f), new Vector2(0f, -5f));

            RectTransform labelRoot = FindChildRecursive<RectTransform>(movesPanel, "MovesLabel");
            if (labelRoot != null)
            {
                SetAnchored(labelRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-18f, 30f), new Vector2(0f, -12f));
                TMP_Text label = labelRoot.GetComponent<TMP_Text>();
                if (label != null)
                {
                    label.text = "Ходы";
                    ConfigureText(label, 30f, 19f, TextAlignmentOptions.Center, new Color(0.9f, 1f, 0.72f, 1f));
                    label.fontStyle = FontStyles.Bold;
                    AddTextOutline(label, new Color(0f, 0.06f, 0.02f, 0.42f), new Vector2(1f, -1f));
                }
            }

            if (movesLabel != null)
            {
                SetAnchored(movesLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-16f, 56f), new Vector2(0f, 10f));
                ConfigureText(movesLabel, 54f, 32f, TextAlignmentOptions.Center, Color.white);
                movesLabel.fontStyle = FontStyles.Bold;
                AddTextOutline(movesLabel, new Color(0f, 0.05f, 0.02f, 0.52f), new Vector2(1.2f, -1.2f));
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
                SetAnchored(deckCardBack, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), GameplayCardSize, new Vector2(-98f, -2f));
                Image deckImage = EnsureGraphic<Image>(deckCardBack.gameObject);
                deckImage.raycastTarget = true;
            }

            if (deckCounterLabel != null)
            {
                RectTransform counterRect = deckCounterLabel.rectTransform;
                SetAnchored(counterRect, new Vector2(1f, 0.54f), new Vector2(1f, 0.54f), new Vector2(0.5f, 0.5f), new Vector2(52f, 40f), new Vector2(-24f, 120f));
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
            StyleActionButton(hintButton, "HintButton", "Подсказка", new Vector2(0.24f, 0.5f));
            StyleActionButton(undoButton, "UndoButton", "Отмена", new Vector2(0.5f, 0.5f));
            StyleActionButton(boosterDeckButton, "BoosterDeckButton", "Бустер", new Vector2(0.76f, 0.5f));

            if (boosterBadge != null)
            {
                boosterBadge.SetActive(false);
            }
        }

        private void StyleActionButton(Button button, string name, string label, Vector2 anchor)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            RectTransform root = button.GetComponent<RectTransform>();
            SetAnchored(root, anchor, anchor, new Vector2(0.5f, 0.5f), new Vector2(BottomButtonWidth, BottomButtonHeight), Vector2.zero);

            Image image = EnsureGraphic<Image>(button.gameObject);
            image.sprite = GetRoundedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.56f, 0.28f, 0.96f);
            AddShadow(button.gameObject, new Color(0.01f, 0.1f, 0.04f, 0.34f), new Vector2(0f, -5f));
            Outline imageOutline = button.GetComponent<Outline>();
            if (imageOutline != null)
            {
                imageOutline.enabled = false;
            }

            TextMeshProUGUI labelText = EnsureText(root, $"{name}Label");
            if (labelText != null)
            {
                labelText.text = label;
                ConfigureText(labelText, 30f, 19f, TextAlignmentOptions.Center, new Color(0.95f, 1f, 0.82f, 1f));
                labelText.fontStyle = FontStyles.Bold;
                SetAnchored(labelText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-24f, -6f), Vector2.zero);
                AddTextOutline(labelText, new Color(0.01f, 0.05f, 0.02f, 0.4f), new Vector2(1f, -1f));
                Shadow labelShadow = labelText.GetComponent<Shadow>();
                if (labelShadow != null)
                {
                    labelShadow.enabled = false;
                }
            }

            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == labelText)
                {
                    continue;
                }

                text.gameObject.SetActive(false);
            }
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

        private void WireButton(Button button, string buttonName)
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
                button.onClick.AddListener(() => Debug.Log($"UIManager: {buttonName} clicked."));
            }
        }

        private void WirePauseButton()
        {
            if (rewardBonusButton == null)
            {
                return;
            }

            if (rewardBonusButton.GetComponent<UIJuicyButton>() == null)
            {
                rewardBonusButton.gameObject.AddComponent<UIJuicyButton>();
            }

            if (wiredButtons.Add(rewardBonusButton))
            {
                rewardBonusButton.onClick.AddListener(ShowPausePopup);
            }
        }

        private void ShowPausePopup()
        {
            EnsurePausePopup();
            if (pausePopup != null)
            {
                pausePopup.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        private void ClosePausePopup()
        {
            if (pausePopup != null)
            {
                pausePopup.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private void EnsurePausePopup()
        {
            if (pausePopup != null || safeAreaRoot == null)
            {
                return;
            }

            GameObject overlayObject = new("PauseSettingsPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform overlay = overlayObject.GetComponent<RectTransform>();
            overlay.SetParent(safeAreaRoot, false);
            StretchToFill(overlay, 0f, 0f, 0f, 0f);
            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0.02f, 0.08f, 0.04f, 0.55f);
            overlayImage.raycastTarget = true;

            RectTransform panel = CreateRect("Panel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 220f), Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = GetCreamPanelSprite();
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(0.05f, 0.03f, 0f, 0.35f), new Vector2(0f, -9f));

            TextMeshProUGUI title = CreateText("Title", panel, "Пауза", 38f, new Color(0.22f, 0.15f, 0.04f, 1f));
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-44f, 70f), new Vector2(0f, -28f));

            Button continueButton = CreateButton("ContinueButton", panel, "Продолжить");
            SetAnchored(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(240f, 62f), new Vector2(0f, 34f));
            continueButton.onClick.AddListener(ClosePausePopup);
            if (continueButton.GetComponent<UIJuicyButton>() == null)
            {
                continueButton.gameObject.AddComponent<UIJuicyButton>();
            }

            pausePopup = overlayObject;
            pausePopup.SetActive(false);
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

            SpawnFloatingText(source, $"+{Mathf.Max(0, amount)}");
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
            image.sprite = GetPrimaryButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddShadow(root.gameObject, new Color(0.34f, 0.16f, 0f, 0.32f), new Vector2(0f, -6f));

            TextMeshProUGUI text = CreateText("Label", root, label, 26f, new Color(0.22f, 0.14f, 0.03f, 1f));
            text.fontStyle = FontStyles.Bold;
            StretchToFill(text.rectTransform, 12f, 12f, 0f, 0f);
            return button;
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
