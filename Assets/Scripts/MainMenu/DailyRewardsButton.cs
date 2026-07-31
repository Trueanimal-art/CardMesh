using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    public sealed class DailyRewardsButton : MonoBehaviour
    {
        [SerializeField] private DailyRewardsPopup popup;
        [SerializeField] private DailyRewardsManager manager;
        [SerializeField] private GameObject notificationDot;
        [SerializeField] private TMP_Text notificationText;

        private Button button;
        private int lastOpenFrame = -1;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        private void Reset()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRefresh();
            }
#endif
            BindReferences();
        }

        private void Awake()
        {
            BindReferences();
            WireButton();
            RefreshState();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRefresh();
            }
#endif
            BindReferences();
            WireButton();

            manager ??= DailyRewardsManager.GetOrCreate(this);
            manager.StateChanged -= RefreshState;
            manager.StateChanged += RefreshState;
            RefreshState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            QueueEditorRefresh();
        }
#endif

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.StateChanged -= RefreshState;
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenDailyRewards);
            }
        }

        public void OpenDailyRewards()
        {
            if (lastOpenFrame == Time.frameCount)
            {
                return;
            }

            lastOpenFrame = Time.frameCount;
            manager ??= DailyRewardsManager.GetOrCreate(this);
            popup ??= DailyRewardsPopup.SceneInstance;

            if (popup == null)
            {
                Debug.LogWarning("DailyRewardsButton: DailyRewardsPopup is missing from MainMenu scene.");
                return;
            }

            popup.Show(manager);
        }

        private void BindReferences()
        {
            button = GetComponent<Button>();
            popup ??= DailyRewardsPopup.SceneInstance;
            manager ??= FindFirstObjectByType<DailyRewardsManager>(FindObjectsInactive.Include);
            notificationDot ??= transform.Find("NotificationDot")?.gameObject;
            notificationText ??= transform.Find("NotificationDot/Text")?.GetComponent<TMP_Text>();
        }

        private void WireButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(OpenDailyRewards);
            button.onClick.AddListener(OpenDailyRewards);
        }

        private void RefreshState()
        {
            if (notificationDot == null)
            {
                return;
            }

            manager ??= DailyRewardsManager.GetOrCreate(this);
            bool canClaim = manager == null || manager.CanClaimToday;
            notificationDot.SetActive(canClaim);
            if (notificationText != null)
            {
                notificationText.text = "!";
                RuntimeCyrillicFontProvider.Apply(notificationText, notificationText.text);
            }
        }

#if UNITY_EDITOR
        private void QueueEditorRefresh()
        {
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
                BindReferences();
                RefreshState();
            };
        }

        private void EnsureAuthoringLayout()
        {
            Image rootImage = GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Color.white;
                rootImage.raycastTarget = true;
                rootImage.preserveAspect = true;
            }

            if (GetComponent<UIJuicyButton>() == null)
            {
                gameObject.AddComponent<UIJuicyButton>();
            }
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, Color color, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(8f, fontSize * 0.55f);
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(text, value);
            return text;
        }
#endif
    }
}
