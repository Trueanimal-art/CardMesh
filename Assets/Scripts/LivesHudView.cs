using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [DisallowMultipleComponent]
    public sealed class LivesHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image heartIcon;

        private LivesManager livesManager;

        private void Awake()
        {
            ResolveReferences();
            NormalizeVisuals();
        }

        private void OnEnable()
        {
            ResolveReferences();
            NormalizeVisuals();

            if (livesManager == null)
            {
                LivesManager sceneLivesManager = UnityEngine.Object.FindFirstObjectByType<LivesManager>();
                if (sceneLivesManager != null)
                {
                    Configure(sceneLivesManager, countText, heartIcon);
                }
            }
            else
            {
                HandleLivesChanged(livesManager.CurrentLives, LivesManager.MaxLives);
            }
        }

        public void Configure(LivesManager targetLivesManager, TMP_Text targetCountText, Image targetHeartIcon)
        {
            if (livesManager != null)
            {
                livesManager.OnLivesChanged -= HandleLivesChanged;
            }

            livesManager = targetLivesManager;
            countText = targetCountText;
            heartIcon = targetHeartIcon;

            ResolveReferences();
            NormalizeVisuals();
            gameObject.SetActive(livesManager != null);

            if (livesManager != null)
            {
                livesManager.OnLivesChanged += HandleLivesChanged;
                livesManager.RecalculateLivesByTime();
                HandleLivesChanged(livesManager.CurrentLives, LivesManager.MaxLives);
            }
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
                ApplyCountTextStyle(countText);

                int safeMaxLives = Mathf.Max(0, maxLives);
                int safeCurrentLives = Mathf.Clamp(currentLives, 0, safeMaxLives);
                string value = $"{safeCurrentLives}/{safeMaxLives}";
                countText.SetText(value);
                countText.ForceMeshUpdate(true, true);
            }

            if (heartIcon != null)
            {
                heartIcon.color = currentLives > 0
                    ? new Color(1f, 0.2f, 0.24f, 1f)
                    : new Color(0.55f, 0.38f, 0.36f, 1f);
            }
        }

        private void ResolveReferences()
        {
            if (countText == null)
            {
                countText = FindText("LivesCount") ?? FindText("Value");
            }

            if (heartIcon == null)
            {
                Transform heart = transform.Find("HeartIcon");
                if (heart != null)
                {
                    heartIcon = heart.GetComponent<Image>();
                }
            }
        }

        private void NormalizeVisuals()
        {
            if (countText != null)
            {
                TrimExtraTextComponents(countText.gameObject, countText);
                ApplyCountTextStyle(countText);
            }

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == countText)
                {
                    continue;
                }

                text.enabled = false;
                text.canvasRenderer.SetAlpha(0f);
            }

            if (heartIcon != null)
            {
                heartIcon.raycastTarget = false;
            }
        }

        private TMP_Text FindText(string targetName)
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.name == targetName)
                {
                    return text;
                }
            }

            return null;
        }

        private static void ApplyCountTextStyle(TMP_Text text)
        {
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
            text.maxVisibleCharacters = int.MaxValue;

            if (text.font != null && text.font.material != null)
            {
                text.fontSharedMaterial = text.font.material;
            }

            RemoveUiEffects(text.gameObject);
        }

        private static void TrimExtraTextComponents(GameObject target, TMP_Text keep)
        {
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
    }
}
