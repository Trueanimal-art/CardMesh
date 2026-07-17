using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations.Editor
{
    [InitializeOnLoad]
    public static class PremiumEventPrefabBootstrapper
    {
        private const string Folder = "Assets/Prefabs/UI/PremiumEvents";

        static PremiumEventPrefabBootstrapper()
        {
            EditorApplication.delayCall += EnsurePrefabs;
        }

        [MenuItem("Tools/CardMesh/Rebuild Premium Event UI Prefabs")]
        public static void EnsurePrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                Directory.CreateDirectory("Assets/Prefabs/UI");
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/UI", "PremiumEvents");
            }

            EnsureWindowFrame();
            EnsureCloseButton();
            EnsureInfoButton();
            EnsureRewardCard();
            EnsureMainActionButton();
            MaterializeScenePremiumPopups();
            AssetDatabase.SaveAssets();
        }

        private static void MaterializeScenePremiumPopups()
        {
            foreach (ShopPopup popup in Object.FindObjectsByType<ShopPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                popup.MaterializeAuthoringLayout();
                EditorUtility.SetDirty(popup.gameObject);
            }
        }

        private static void EnsureWindowFrame()
        {
            SaveOrUpdate("WindowFramePrefab", root =>
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1510f, 980f);
                Image image = root.AddComponent<Image>();
                image.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                Shadow shadow = root.AddComponent<Shadow>();
                shadow.effectColor = new Color(1f, 0.68f, 0.14f, 0.12f);
                shadow.effectDistance = new Vector2(0f, -4f);
            });
        }

        private static void EnsureCloseButton()
        {
            SaveOrUpdate("CloseButtonPrefab", root =>
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(86f, 86f);
                Image image = root.AddComponent<Image>();
                image.sprite = PremiumUISprites.EmeraldIconCircle != null ? PremiumUISprites.EmeraldIconCircle : PremiumUISprites.IconCircle;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                Button button = root.AddComponent<Button>();
                button.targetGraphic = image;
                root.AddComponent<UIJuicyButton>();
                CreateLabel(root.transform, "X", 42f, new Color(1f, 0.82f, 0.24f, 1f));
            });
        }

        private static void EnsureInfoButton()
        {
            SaveOrUpdate("InfoButtonPrefab", root =>
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(66f, 66f);
                Image image = root.AddComponent<Image>();
                image.sprite = PremiumUISprites.EmeraldIconCircle != null ? PremiumUISprites.EmeraldIconCircle : PremiumUISprites.IconCircle;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                Button button = root.AddComponent<Button>();
                button.targetGraphic = image;
                root.AddComponent<UIJuicyButton>();
                CreateLabel(root.transform, "i", 38f, new Color(1f, 0.82f, 0.24f, 1f));
            });
        }

        private static void EnsureRewardCard()
        {
            SaveOrUpdate("RewardCardPrefab", root =>
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(184f, 244f);
                Image image = root.AddComponent<Image>();
                image.sprite = RoyalEventSprites.RewardCard != null ? RoyalEventSprites.RewardCard : PremiumUISprites.EmeraldPanel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                Button button = root.AddComponent<Button>();
                button.targetGraphic = image;
                root.AddComponent<UIJuicyButton>();
            });
        }

        private static void EnsureMainActionButton()
        {
            SaveOrUpdate("MainActionButtonPrefab", root =>
            {
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(520f, 96f);
                Image image = root.AddComponent<Image>();
                image.sprite = PremiumUISprites.PrimaryButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                Button button = root.AddComponent<Button>();
                button.targetGraphic = image;
                root.AddComponent<UIJuicyButton>();
                CreateLabel(root.transform, "ОТКРЫТЬ", 36f, new Color(1f, 0.9f, 0.65f, 1f));
            });
        }

        private static void SaveOrUpdate(string name, System.Action<GameObject> configure)
        {
            string path = $"{Folder}/{name}.prefab";
            GameObject root = new(name, typeof(RectTransform), typeof(CanvasRenderer));
            root.layer = 5;
            configure(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateLabel(Transform parent, string text, float size, Color color)
        {
            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(12f, size * 0.55f);
            label.fontSizeMax = size;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
        }
    }
}
