using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    /// <summary>
    /// Shared finishing touches for the scene-authored gameplay popups (Пауза, Начать заново?).
    /// Their "X" close glyph was a bare red letter floating outside the window frame while the
    /// popups already have an explicit "Продолжить"/"Отмена" button, and the board behind them
    /// was not dimmed like in every other modal.
    /// </summary>
    public static class GameplayModalPolish
    {
        private static readonly Color BackdropDim = new(0.01f, 0.045f, 0.028f, 0.66f);

        public static void Apply(GameObject popupRoot)
        {
            if (popupRoot == null)
            {
                return;
            }

            Transform close = FindDeep(popupRoot.transform, "CloseButton");
            if (close != null)
            {
                close.gameObject.SetActive(false);
            }

            Image backdrop = popupRoot.GetComponent<Image>();
            if (backdrop != null && backdrop.color.a < 0.3f)
            {
                backdrop.sprite = null;
                backdrop.color = BackdropDim;
            }
        }

        public static void SetButtonLabel(GameObject popupRoot, string buttonName, string label)
        {
            Transform button = popupRoot != null ? FindDeep(popupRoot.transform, buttonName) : null;
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
