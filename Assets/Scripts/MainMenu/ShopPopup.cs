using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    /// <summary>
    /// Runtime behaviour for the scene-authored shop popup.
    /// All visual objects and RectTransform values live in MainMenu.unity.
    /// </summary>
    public sealed class ShopPopup : MonoBehaviour
    {
        [Header("Runtime bindings")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;

        private Coroutine openRoutine;
        private bool hasShownRuntime;

        public static ShopPopup SceneInstance =>
            FindFirstObjectByType<ShopPopup>(FindObjectsInactive.Include);

        private void Awake()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();

            if (!hasShownRuntime)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void Show()
        {
            hasShownRuntime = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
            StartOpenAnimation();
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void ShowInfo()
        {
            Debug.Log("Shop: purchases are demo UI only. Payments are not connected.");
        }

        private void Buy(string productName, string price, string purchaseType)
        {
            Debug.Log($"Shop purchase clicked: {productName} | {price} | {purchaseType}");
        }

        private void BindSceneReferences()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panel = transform.Find("Panel") as RectTransform;
            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            infoButton = transform.Find("Panel/InfoButton")?.GetComponent<Button>();
        }

        private void WireButtons()
        {
            BindButton(closeButton, Hide);
            BindButton(infoButton, ShowInfo);
            BindBuyButtons(transform);
        }

        private void UnwireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            if (infoButton != null)
            {
                infoButton.onClick.RemoveListener(ShowInfo);
            }
        }

        private static void BindButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void BindBuyButtons(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                ShopProductButton product = button.GetComponent<ShopProductButton>();
                if (product == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Buy(product.ProductName, product.Price, product.PurchaseType));
            }
        }

        private void StartOpenAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(PlayOpenAnimation());
        }

        private IEnumerator PlayOpenAnimation()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.96f;

            if (panel != null)
            {
                panel.localScale = startScale;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }

                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }
        }

#if UNITY_EDITOR
        // Kept for the existing editor bootstrapper. The shop is already authored
        // in MainMenu.unity, so this deliberately performs no layout generation.
        public void MaterializeAuthoringLayout()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
        }
#endif
    }
}
