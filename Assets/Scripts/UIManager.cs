using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class UIManager : MonoBehaviour
    {
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

        public void Configure(
            SolitaireSceneRefs sceneRefs,
            TextMeshProUGUI targetStatusLabel,
            Button targetRestartButton)
        {
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
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(gameManager.RestartLevel);
            }

            WireButton(menuButton, "MenuButton");
            WireButton(plusButton, "PlusButton");
            WireButton(rewardBonusButton, "RewardBonusButton");
            WireButton(hintButton, "HintButton");
            WireButton(undoButton, "UndoButton");
            WireButton(boosterDeckButton, "BoosterDeckButton");

            ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            UpdateDeckCounter(0);
            SetBoosterBadgeVisible(boosterBadge != null && boosterBadge.activeSelf);
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

        private static void WireButton(Button button, string buttonName)
        {
            if (button == null)
            {
                return;
            }

            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Debug.Log($"UIManager: {buttonName} clicked."));
        }
    }
}
