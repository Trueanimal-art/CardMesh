using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardMesh
{
    /// <summary>
    /// Readability pass for the scene-authored gameplay HUD (reference 1080x1920).
    /// The captions under the status plates ("ЖИЗНИ", "МОНЕТЫ", "ХОДОВ") were 11–14 units
    /// (about 5 pt on a phone) and the action-button labels 18–22 units; the raised
    /// "Отменить ход" medallion also dragged its label 14 units above the other two.
    /// </summary>
    public sealed class GameplayHudPolish : MonoBehaviour
    {
        private const float CaptionMax = 26f;
        private const float CaptionMin = 21f;
        private const float ActionLabelMax = 29f;
        private const float ActionLabelMin = 24f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Attach(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Attach(scene);
        }

        private static void Attach(Scene scene)
        {
            if (scene.name != "Main" || FindFirstObjectByType<GameplayHudPolish>() != null)
            {
                return;
            }

            GameObject host = new(nameof(GameplayHudPolish));
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<GameplayHudPolish>();
        }

        private IEnumerator Start()
        {
            // Other bootstrap code lays the HUD out during the first frames; apply after it.
            yield return null;
            Apply();
            yield return new WaitForSecondsRealtime(0.5f);
            Apply();
        }

        private static void Apply()
        {
            PlaceValueAndCaption("LivesCount", "LivesStatus", 170f);
            PlaceValueAndCaption("CoinsText", "CoinsLabel", 120f);
            PlaceValueAndCaption("MovesValue", "MovesLabel", 160f);
            WidenCoinsPlate();

            AlignActionLabels();
        }

        private static void PlaceValueAndCaption(string valueName, string captionName, float width)
        {
            TMP_Text caption = FindText(captionName);
            if (caption == null)
            {
                return;
            }

            RectTransform captionRect = caption.rectTransform;
            captionRect.sizeDelta = new Vector2(Mathf.Max(width, captionRect.sizeDelta.x), 30f);
            captionRect.anchoredPosition = new Vector2(captionRect.anchoredPosition.x, -24f);
            caption.enableAutoSizing = true;
            caption.fontSizeMax = CaptionMax;
            caption.fontSizeMin = CaptionMin;
            caption.overflowMode = TextOverflowModes.Overflow;

            TMP_Text value = FindText(valueName);
            if (value != null)
            {
                RectTransform valueRect = value.rectTransform;
                valueRect.anchoredPosition = new Vector2(valueRect.anchoredPosition.x, 14f);
            }
        }

        // The coins plate was 190 wide: "МОНЕТЫ" at a readable size touched the coin icon.
        private static void WidenCoinsPlate()
        {
            TMP_Text value = FindText("CoinsText");
            TMP_Text caption = FindText("CoinsLabel");
            RectTransform plate = value != null ? value.rectTransform.parent as RectTransform : null;
            if (plate == null)
            {
                return;
            }

            plate.sizeDelta = new Vector2(236f, plate.sizeDelta.y);
            Transform icon = plate.Find("CoinIcon");
            if (icon is RectTransform iconRect)
            {
                iconRect.anchoredPosition = new Vector2(-70f, iconRect.anchoredPosition.y);
            }

            foreach (TMP_Text text in new[] { value, caption })
            {
                if (text != null)
                {
                    RectTransform rect = text.rectTransform;
                    rect.sizeDelta = new Vector2(140f, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(26f, rect.anchoredPosition.y);
                }
            }
        }

        private static void AlignActionLabels()
        {
            string[] buttons = { "HintButton", "UndoButton", "BoosterDeckButton" };
            float? baseline = null;
            foreach (string buttonName in buttons)
            {
                RectTransform button = FindRect(buttonName);
                TMP_Text label = FindText(buttonName + "Label");
                if (button == null || label == null)
                {
                    continue;
                }

                label.enableAutoSizing = true;
                label.fontSizeMax = ActionLabelMax;
                label.fontSizeMin = ActionLabelMin;
                label.overflowMode = TextOverflowModes.Overflow;
                RectTransform labelRect = label.rectTransform;
                labelRect.sizeDelta = new Vector2(244f, 36f);

                // Keep every label at the same height on screen whatever the medallion offset.
                float labelLocalY = -80f;
                float worldY = button.anchoredPosition.y + labelLocalY;
                baseline ??= worldY;
                if (buttonName == "HintButton")
                {
                    baseline = worldY;
                }

                labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, labelLocalY);
            }

            if (baseline == null)
            {
                return;
            }

            foreach (string buttonName in buttons)
            {
                RectTransform button = FindRect(buttonName);
                TMP_Text label = FindText(buttonName + "Label");
                if (button == null || label == null)
                {
                    continue;
                }

                RectTransform labelRect = label.rectTransform;
                labelRect.anchoredPosition = new Vector2(labelRect.anchoredPosition.x, baseline.Value - button.anchoredPosition.y);
            }
        }

        private static TMP_Text FindText(string name)
        {
            foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text.name == name && text.gameObject.scene.name == "Main")
                {
                    return text;
                }
            }

            return null;
        }

        private static RectTransform FindRect(string name)
        {
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rect.name == name && rect.gameObject.scene.name == "Main" && rect.parent != null && rect.parent.name == "BottomHUD")
                {
                    return rect;
                }
            }

            return null;
        }
    }
}
