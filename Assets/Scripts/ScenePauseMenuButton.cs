using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [RequireComponent(typeof(Button))]
    public sealed class ScenePauseMenuButton : MonoBehaviour
    {
        [SerializeField] private GameObject pausePopup;

        private Button button;

        public void SetPopup(GameObject popup)
        {
            pausePopup = popup;
        }

        private void Awake()
        {
            Wire();
        }

        private void OnEnable()
        {
            Wire();
        }

        private void Wire()
        {
            button ??= GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(OpenPauseMenu);
            button.onClick.AddListener(OpenPauseMenu);
        }

        public void OpenPauseMenu()
        {
            GameObject popup = ResolvePopup();
            if (popup == null)
            {
                Debug.LogWarning("ScenePauseMenuButton: PauseSettingsPopup not found.");
                return;
            }

            PauseMenuPopupController controller = popup.GetComponent<PauseMenuPopupController>();
            if (controller != null)
            {
                controller.Show();
                return;
            }

            popup.SetActive(true);
            popup.transform.SetAsLastSibling();

            if (popup.transform is RectTransform popupRect)
            {
                StretchToFill(popupRect);
            }

            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            Time.timeScale = 0f;
            Debug.Log("ScenePauseMenuButton: Pause menu opened.");
        }

        private GameObject ResolvePopup()
        {
            if (pausePopup != null)
            {
                return pausePopup;
            }

            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate != null && candidate.name == "PauseSettingsPopup")
                {
                    pausePopup = candidate.gameObject;
                    return pausePopup;
                }
            }

            return null;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
