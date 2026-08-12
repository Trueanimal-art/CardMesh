using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    public sealed class ShopButton : MonoBehaviour
    {
        [SerializeField] private ShopPopup popup;

        private Button button;
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
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                QueueEditorRefresh();
            }
        }
#endif

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenShop);
            }
        }

        public void OpenShop()
        {
            popup ??= ShopPopup.SceneInstance;
            if (popup == null)
            {
                Debug.LogWarning("ShopButton: ShopPopup is missing from MainMenu scene.");
                return;
            }

            popup.Show();
        }

        private void BindReferences()
        {
            button = GetComponent<Button>();
            popup ??= ShopPopup.SceneInstance;
        }

        private void WireButton()
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(OpenShop);
            button.onClick.AddListener(OpenShop);
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

                StyleAuthoringButton();
                BindReferences();
                WireButton();
            };
        }

        private void StyleAuthoringButton()
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            if (GetComponent<UIJuicyButton>() == null)
            {
                gameObject.AddComponent<UIJuicyButton>();
            }

            Shadow shadow = GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(1f, 0.72f, 0.12f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;
        }
#endif
    }
}
