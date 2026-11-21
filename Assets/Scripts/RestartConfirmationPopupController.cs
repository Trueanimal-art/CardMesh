using System;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [DisallowMultipleComponent]
    public sealed class RestartConfirmationPopupController : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button closeButton;

        private Action confirmAction;
        private Action cancelAction;

        public void Configure(GameObject root, Button confirmTarget, Button cancelTarget, Button closeTarget)
        {
            popupRoot = root != null ? root : gameObject;
            confirmButton = confirmTarget;
            cancelButton = cancelTarget;
            closeButton = closeTarget;
            WireButtons();
        }

        public void SetActions(Action onConfirm, Action onCancel)
        {
            confirmAction = onConfirm;
            cancelAction = onCancel;
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
            GameplayModalPolish.Apply(popupRoot);
            GameplayModalPolish.SetButtonLabel(popupRoot, "ConfirmButton", "Начать заново");

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
            Debug.Log("RestartConfirmationPopupController: Restart confirmation opened.");
        }

        public void Hide()
        {
            popupRoot ??= gameObject;
            popupRoot.SetActive(false);
            cancelAction?.Invoke();
        }

        public void WireButtons()
        {
            ResolveMissingButtons();

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => confirmAction?.Invoke());
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => cancelAction?.Invoke());
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => cancelAction?.Invoke());
            }
        }

        private void ResolveMissingButtons()
        {
            Transform root = (popupRoot != null ? popupRoot : gameObject).transform;
            confirmButton ??= FindButton(root, "ConfirmButton");
            cancelButton ??= FindButton(root, "CancelButton");
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
