using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class LivesHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image heartIcon;

        private LivesManager livesManager;

        public void Configure(LivesManager targetLivesManager, TMP_Text targetCountText, Image targetHeartIcon)
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= HandleLivesChanged;
            }

            livesManager = targetLivesManager;
            countText = targetCountText;
            heartIcon = targetHeartIcon;

            if (livesManager != null)
            {
                livesManager.OnLivesChanged += HandleLivesChanged;
                livesManager.RecalculateLivesByTime();
                HandleLivesChanged(livesManager.CurrentLives, LivesManager.MaxLives);
            }

            gameObject.SetActive(livesManager != null);
        }

        private void OnDestroy()
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= HandleLivesChanged;
            }
        }

        private void HandleLivesChanged(int currentLives, int maxLives)
        {
            if (countText != null)
            {
                countText.text = $"{currentLives}/{maxLives}";
            }

            if (heartIcon != null)
            {
                heartIcon.color = currentLives > 0
                    ? new Color(1f, 0.2f, 0.24f, 1f)
                    : new Color(0.55f, 0.38f, 0.36f, 1f);
            }
        }
    }
}
