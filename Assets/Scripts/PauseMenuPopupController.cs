using System;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button closeButton;

        private Action restartAction;
        private Action mainMenuAction;

        public void Configure(GameObject root, Button continueTarget, Button restartTarget, Button closeTarget)
        {
            Configure(root, continueTarget, restartTarget, null, closeTarget);
        }

        public void Configure(GameObject root, Button continueTarget, Button restartTarget, Button mainMenuTarget, Button closeTarget)
        {
            popupRoot = root != null ? root : gameObject;
            continueButton = continueTarget;
            restartButton = restartTarget;
            mainMenuButton = mainMenuTarget;
            closeButton = closeTarget;
            WireButtons();
        }

        public void SetRestartAction(Action action)
        {
            restartAction = action;
            WireButtons();
        }

        public void SetMainMenuAction(Action action)
        {
            mainMenuAction = action;
            WireButtons();
        }

        private void Awake()
        {
            popupRoot ??= gameObject;
            ResolveMissingButtons();
            WireButtons();
        }

        private void OnEnable()
        {
            ResolveMissingButtons();
            WireButtons();
        }

        public void Show()
        {
            popupRoot ??= gameObject;
            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();

            if (popupRoot.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            CanvasGroup group = popupRoot.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            Image image = popupRoot.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            Time.timeScale = 0f;
            Debug.Log("PauseMenuPopupController: Pause menu opened.");
        }

        public void Close()
        {
            popupRoot ??= gameObject;
            popupRoot.SetActive(false);
            Time.timeScale = 1f;
            Debug.Log("PauseMenuPopupController: Pause menu closed.");
        }

        public void RequestRestart()
        {
            Debug.Log("PauseMenuPopupController: Restart requested.");
            popupRoot ??= gameObject;
            popupRoot.SetActive(false);

            if (restartAction != null)
            {
                restartAction.Invoke();
                return;
            }

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.RequestRestartWithConfirmation();
            }
        }

        public void RequestMainMenu()
        {
            Debug.Log("PauseMenuPopupController: Main menu requested.");
            popupRoot ??= gameObject;
            popupRoot.SetActive(false);
            Time.timeScale = 1f;

            if (mainMenuAction != null)
            {
                mainMenuAction.Invoke();
                return;
            }

            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.RequestReturnToMainMenu();
            }
        }

        public void WireButtons()
        {
            ResolveMissingButtons();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RequestRestart);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(RequestMainMenu);
            }
        }

        private void ResolveMissingButtons()
        {
            Transform root = (popupRoot != null ? popupRoot : gameObject).transform;
            continueButton ??= FindButton(root, "ContinueButton");
            restartButton ??= FindButton(root, "RestartButton");
            mainMenuButton ??= FindButton(root, "MainMenuButton");
            closeButton ??= FindButton(root, "CloseButton");
        }

        private static Button FindButton(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
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
