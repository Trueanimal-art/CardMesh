using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [DisallowMultipleComponent]
    public sealed class LivesOfferPopupController : MonoBehaviour
    {
        public const int LifeCoinCost = 300;

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text livesCountText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button coinButton;
        [SerializeField] private Button rewardedButton;
        [SerializeField] private Button closeButton;

        private LivesManager livesManager;
        private CoinsManager coinsManager;
        private AdsManager adsManager;
        private Action closeAction;
        private Action<int> coinsChangedAction;
        private bool rewardInProgress;
        private float nextTimerRefreshAt;

        public void Configure(
            LivesManager targetLivesManager,
            CoinsManager targetCoinsManager,
            AdsManager targetAdsManager,
            Action onClose,
            Action<int> onCoinsChanged)
        {
            livesManager = targetLivesManager;
            coinsManager = targetCoinsManager;
            adsManager = targetAdsManager;
            closeAction = onClose;
            coinsChangedAction = onCoinsChanged;
            ResolveReferences();
            WireButtons();
            Refresh();
        }

        private void Awake()
        {
            popupRoot ??= gameObject;
            ResolveReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            ResolveReferences();
            WireButtons();
            nextTimerRefreshAt = 0f;
            SetFeedback(string.Empty);
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextTimerRefreshAt)
            {
                return;
            }

            nextTimerRefreshAt = Time.unscaledTime + 0.25f;
            livesManager?.RecalculateLivesByTime();
            Refresh();
        }

        private void BuyLifeWithCoins()
        {
            if (rewardInProgress || livesManager == null || coinsManager == null)
            {
                return;
            }

            if (livesManager.CurrentLives >= LivesManager.MaxLives)
            {
                SetFeedback("Жизни уже заполнены");
                Refresh();
                return;
            }

            if (!coinsManager.TrySpendCoins(LifeCoinCost))
            {
                SetFeedback("Не хватает монет");
                Refresh();
                return;
            }

            if (!livesManager.TryAddLife())
            {
                coinsManager.AddCoins(LifeCoinCost);
                SetFeedback("Не удалось добавить жизнь");
                Refresh();
                return;
            }

            coinsChangedAction?.Invoke(coinsManager.CurrentCoins);
            Close();
        }

        private void WatchRewardedForLife()
        {
            if (rewardInProgress || livesManager == null)
            {
                return;
            }

            if (livesManager.CurrentLives >= LivesManager.MaxLives)
            {
                SetFeedback("Жизни уже заполнены");
                Refresh();
                return;
            }

            adsManager ??= AdsManager.Instance;
            if (adsManager == null)
            {
                SetFeedback("Реклама сейчас недоступна");
                return;
            }

            rewardInProgress = true;
            SetButtonsInteractable(false);
            SetFeedback("Загрузка рекламы…");
            adsManager.ShowRewarded(
                "AddLife",
                onCompleted: () =>
                {
                    rewardInProgress = false;
                    if (livesManager.TryAddLife())
                    {
                        Close();
                        return;
                    }

                    SetFeedback("Жизни уже заполнены");
                    Refresh();
                },
                onFailed: () =>
                {
                    rewardInProgress = false;
                    SetFeedback("Реклама не завершена");
                    Refresh();
                });
        }

        private void Close()
        {
            if (rewardInProgress)
            {
                return;
            }

            closeAction?.Invoke();
        }

        private void Refresh()
        {
            int currentLives = livesManager != null ? livesManager.CurrentLives : 0;
            if (livesCountText != null)
            {
                livesCountText.SetText($"{currentLives}/{LivesManager.MaxLives}");
            }

            if (timerText != null)
            {
                TimeSpan remaining = livesManager != null
                    ? livesManager.GetTimeToNextLife()
                    : TimeSpan.FromSeconds(LivesManager.RestoreIntervalSeconds);
                int minutes = Mathf.Max(0, Mathf.FloorToInt((float)remaining.TotalMinutes));
                int seconds = Mathf.Clamp(remaining.Seconds, 0, 59);
                timerText.SetText(currentLives >= LivesManager.MaxLives
                    ? "Жизни заполнены"
                    : $"Следующая через {minutes:00}м {seconds:00}с");
            }

            SetButtonsInteractable(
                !rewardInProgress && currentLives < LivesManager.MaxLives);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (coinButton != null)
            {
                coinButton.interactable = interactable;
            }

            if (rewardedButton != null)
            {
                rewardedButton.interactable = interactable;
            }
        }

        private void SetFeedback(string value)
        {
            if (feedbackText == null)
            {
                return;
            }

            feedbackText.SetText(value ?? string.Empty);
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }

        private void ResolveReferences()
        {
            popupRoot ??= gameObject;
            livesCountText ??= FindText("LivesCount");
            timerText ??= FindText("TimerText");
            feedbackText ??= FindText("FeedbackText");
            coinButton ??= FindButton("CoinButton");
            rewardedButton ??= FindButton("RewardedButton");
            closeButton ??= FindButton("CloseButton");
        }

        private void WireButtons()
        {
            if (coinButton != null)
            {
                coinButton.onClick.RemoveListener(BuyLifeWithCoins);
                coinButton.onClick.AddListener(BuyLifeWithCoins);
            }

            if (rewardedButton != null)
            {
                rewardedButton.onClick.RemoveListener(WatchRewardedForLife);
                rewardedButton.onClick.AddListener(WatchRewardedForLife);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private TMP_Text FindText(string targetName)
        {
            foreach (TMP_Text text in popupRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.name == targetName)
                {
                    return text;
                }
            }

            return null;
        }

        private Button FindButton(string targetName)
        {
            foreach (Button button in popupRoot.GetComponentsInChildren<Button>(true))
            {
                if (button != null && button.name == targetName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
