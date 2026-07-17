#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations.EditorTools
{
    public static class RoyalSeriesSceneInstaller
    {
        private const string ScenePath = "Assets/MainMenu.unity";
        private const string PrefabPath = "Assets/Prefabs/UI/MainMenu/RoyalSeriesPopup.prefab";
        private const string LayoutMarker = "RoyalSeriesLayoutV4";
        private const string DailyRewardsAssetRoot = "Assets/Resources/UI/DailyRewards/DailyRewards";
        private static readonly Color Gold = new(1f, 0.81f, 0.22f, 1f);
        private static readonly Color Cream = new(1f, 0.95f, 0.76f, 1f);
        private static readonly Color Green = new(0.02f, 0.19f, 0.09f, 0.98f);

        [MenuItem("Tools/CardMesh/Rebuild Royal Series Popup")]
        public static void RebuildRoyalSeriesPopup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildScene(scene);
        }

        internal static void RepairLoadedSceneIfNeeded()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                return;
            }

            Transform royal = FindSceneObject("ChestChasePopup");
            if (royal == null || royal.Find($"WindowRoot/{LayoutMarker}") != null)
            {
                return;
            }

            RebuildScene(scene);
        }

        private static void RebuildScene(Scene scene)
        {
            Transform daily = FindSceneObject("DailyRewardsPopup");
            Transform royal = FindSceneObject("ChestChasePopup");
            if (daily == null || royal == null)
            {
                throw new InvalidOperationException("MainMenu.unity must contain DailyRewardsPopup and ChestChasePopup.");
            }

            Transform dailyWindow = daily.Find("WindowRoot");
            Transform dailyCard = daily.Find("WindowRoot/Content/RewardsColumn/RewardsRowTop/Day1Card");
            if (dailyWindow == null || dailyCard == null)
            {
                throw new InvalidOperationException("The approved Daily Rewards hierarchy is incomplete.");
            }

            CopyRootPresentation(daily, royal);
            DestroyChildren(royal);

            GameObject windowObject = UnityEngine.Object.Instantiate(dailyWindow.gameObject, royal, false);
            windowObject.name = "WindowRoot";
            RectTransform window = windowObject.GetComponent<RectTransform>();
            window.localScale = Vector3.one;
            RemoveDailyOnlyContent(window);
            ConfigureHeader(window);
            ConfigureBadgeAndButtons(window);
            BuildRoyalContent(window, dailyCard);
            CreateRect(window, LayoutMarker, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).gameObject.SetActive(false);
            ConfigurePersistentBindings(royal, window);

            royal.gameObject.SetActive(false);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Prefabs/UI/MainMenu");
            PrefabUtility.SaveAsPrefabAsset(royal.gameObject, PrefabPath);
            EditorUtility.SetDirty(royal.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Royal Series rebuilt as a static MainMenu scene hierarchy from approved Daily Rewards components.");
        }

        private static void ConfigurePersistentBindings(Transform royal, RectTransform window)
        {
            ChestChasePopup popup = royal.GetComponent<ChestChasePopup>();
            Button closeButton = window.Find("CloseButton")?.GetComponent<Button>();
            if (popup == null || closeButton == null)
            {
                throw new InvalidOperationException("Royal Series requires ChestChasePopup and CloseButton components.");
            }

            for (int i = closeButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(closeButton.onClick, i);
            }

            UnityEventTools.AddPersistentListener(closeButton.onClick, popup.Close);
            EditorUtility.SetDirty(closeButton);
        }

        private static void CopyRootPresentation(Transform source, Transform destination)
        {
            RectTransform sourceRect = source as RectTransform;
            RectTransform destinationRect = destination as RectTransform;
            CopyRect(sourceRect, destinationRect);

            Image sourceImage = source.GetComponent<Image>();
            Image destinationImage = destination.GetComponent<Image>() ?? destination.gameObject.AddComponent<Image>();
            if (sourceImage != null)
            {
                EditorUtility.CopySerialized(sourceImage, destinationImage);
            }

            CanvasGroup sourceGroup = source.GetComponent<CanvasGroup>();
            CanvasGroup destinationGroup = destination.GetComponent<CanvasGroup>() ?? destination.gameObject.AddComponent<CanvasGroup>();
            if (sourceGroup != null)
            {
                EditorUtility.CopySerialized(sourceGroup, destinationGroup);
            }

            destinationImage.raycastTarget = true;
            destinationRect.localScale = Vector3.one;
        }

        private static void RemoveDailyOnlyContent(RectTransform window)
        {
            string[] remove = { "Content", "InfoButton", "ClaimFlash", "TopGem" };
            foreach (string childName in remove)
            {
                Transform child = window.Find(childName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void ConfigureHeader(RectTransform window)
        {
            RectTransform header = window.Find("Header") as RectTransform;
            RectTransform titleRect = header?.Find("Title") as RectTransform;
            RectTransform subtitleRect = header?.Find("Subtitle") as RectTransform;
            if (header == null || titleRect == null || subtitleRect == null)
            {
                throw new InvalidOperationException("Daily Rewards Header is missing.");
            }

            SetRect(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1160f, 170f), new Vector2(0f, -72f));
            SetRect(titleRect, Center, Center, Center, new Vector2(1120f, 62f), new Vector2(0f, 44f));
            SetRect(subtitleRect, Center, Center, Center, new Vector2(1080f, 58f), new Vector2(0f, -19f));

            TMP_Text title = titleRect.GetComponent<TMP_Text>();
            TMP_Text subtitle = subtitleRect.GetComponent<TMP_Text>();
            title.text = "КОРОЛЕВСКАЯ СЕРИЯ";
            title.fontSize = 52f;
            title.enableAutoSizing = false;
            title.alignment = TextAlignmentOptions.Center;
            subtitle.text = "Побеждай уровни подряд и получай всё больше наград.\nПри проигрыше серия сбрасывается.";
            subtitle.fontSize = 23f;
            subtitle.enableAutoSizing = false;
            subtitle.lineSpacing = 4f;
            subtitle.alignment = TextAlignmentOptions.Center;

            Transform oldDivider = window.Find("DividerStrip");
            if (oldDivider != null)
            {
                UnityEngine.Object.DestroyImmediate(oldDivider.gameObject);
            }

            BuildHeaderDivider(window);
        }

        private static void BuildHeaderDivider(RectTransform window)
        {
            Sprite dividerSprite = LoadSprite($"{DailyRewardsAssetRoot}/DailyRewardsDividerStrip.asset");
            Material dividerMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{DailyRewardsAssetRoot}/DailyRewardsDividerStrip.mat");
            Image divider = CreateImage(
                window,
                "DividerStrip",
                dividerSprite,
                Image.Type.Simple,
                new Vector2(0f, -237f),
                new Vector2(1056f, 88f)).GetComponent<Image>();
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = new Vector2(0.5f, 1f);
            dividerRect.anchorMax = new Vector2(0.5f, 1f);
            dividerRect.pivot = Center;
            dividerRect.anchoredPosition = new Vector2(0f, -237f);
            divider.material = dividerMaterial;
            divider.preserveAspect = true;
            divider.raycastTarget = false;
        }

        private static void ConfigureBadgeAndButtons(RectTransform window)
        {
            RectTransform badge = window.Find("TopBadge") as RectTransform;
            SetRect(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Center, new Vector2(168f, 168f), new Vector2(0f, 52f));
            Image badgeImage = badge.GetComponent<Image>();
            badgeImage.sprite = LoadSprite("Assets/Resources/UI/RoyalEvent/royal_event_logo.png");
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            RectTransform close = window.Find("CloseButton") as RectTransform;
            if (close != null)
            {
                SetRect(close, Vector2.one, Vector2.one, Center, new Vector2(66f, 66f), new Vector2(-48f, -48f));
            }
        }

        private static void BuildRoyalContent(RectTransform window, Transform dailyCard)
        {
            RectTransform content = CreateRect(window, "RoyalContent", Center, Center, Center, new Vector2(1260f, 585f), new Vector2(0f, -42f));
            TMP_Text template = window.Find("Header/Subtitle")?.GetComponent<TMP_Text>();

            BuildGoalBlock(content, template);
            BuildProgressBlock(content, template);
            BuildMilestoneTrack(content, template);
            BuildStageRow(content, dailyCard, template);

            TMP_Text toast = CreateText(content, "RewardToast", template, "", 22f, Gold, new Vector2(0f, 8f), new Vector2(500f, 38f));
            toast.gameObject.SetActive(false);
        }

        private static void BuildGoalBlock(RectTransform parent, TMP_Text template)
        {
            RectTransform block = CreateRect(parent, "GoalBlock", Center, Center, Center, new Vector2(650f, 132f), new Vector2(0f, 154f));
            CreateText(block, "Title", template, "НАГРАДА ЗА ЦЕЛЬ", 29f, Gold, new Vector2(0f, 47f), new Vector2(650f, 40f));
            TMP_Text timer = CreateText(block, "Timer", template, "", 18f, Cream, new Vector2(270f, 47f), new Vector2(130f, 30f));
            timer.alignment = TextAlignmentOptions.Right;
            timer.gameObject.SetActive(false);

            RectTransform panel = CreateImage(block, "RewardPanel", LoadSprite("Assets/Resources/UI/Premium/category_slot_cardmesh.png"), Image.Type.Sliced, new Vector2(0f, -18f), new Vector2(540f, 82f));
            Image inner = CreateImage(panel, "InnerBorder", LoadSprite("Assets/Resources/UI/Premium/category_slot_emerald.png"), Image.Type.Sliced, Vector2.zero, new Vector2(514f, 60f)).GetComponent<Image>();
            inner.color = new Color(0.36f, 0.60f, 0.42f, 0.72f);
            BuildCompactReward(panel, template, "Reward_1", -170f, "Assets/Resources/UI/Premium/icon_coin_stack.png", "×300");
            BuildCompactReward(panel, template, "Reward_2", 0f, "Assets/Resources/UI/Premium/icon_hint_bulb.png", "×1");
            BuildCompactReward(panel, template, "Reward_3", 170f, "Assets/Resources/UI/Premium/icon_undo_gold.png", "–");
        }

        private static void BuildCompactReward(RectTransform parent, TMP_Text template, string name, float x, string iconPath, string value)
        {
            RectTransform item = CreateRect(parent, name, Center, Center, Center, new Vector2(162f, 56f), new Vector2(x, 0f));
            CreateImage(item, "Icon", LoadSprite(iconPath), Image.Type.Simple, new Vector2(-43f, 0f), new Vector2(40f, 40f));
            CreateText(item, "Value", template, value, 31f, Cream, new Vector2(21f, 0f), new Vector2(92f, 44f));
        }

        private static void BuildProgressBlock(RectTransform parent, TMP_Text template)
        {
            RectTransform block = CreateRect(parent, "ProgressBlock", Center, Center, Center, new Vector2(370f, 136f), new Vector2(0f, 30f));
            RectTransform badge = CreateImage(block, "ProgressBadge", LoadSprite("Assets/Resources/UI/Premium/category_slot_cardmesh.png"), Image.Type.Sliced, new Vector2(0f, 18f), new Vector2(336f, 84f));
            Image inner = CreateImage(badge, "InnerPanel", LoadSprite("Assets/Resources/UI/Premium/category_slot_emerald.png"), Image.Type.Sliced, Vector2.zero, new Vector2(312f, 62f)).GetComponent<Image>();
            inner.color = new Color(0.38f, 0.64f, 0.45f, 0.72f);
            Image fill = CreateImage(badge, "ProgressFill", LoadSprite("Assets/Resources/UI/Premium/category_slot_emerald.png"), Image.Type.Filled, Vector2.zero, new Vector2(300f, 52f)).GetComponent<Image>();
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            fill.color = new Color(0.18f, 0.65f, 0.30f, 0.28f);
            CreateText(badge, "ProgressCount", template, "0/20", 53f, Cream, Vector2.zero, new Vector2(300f, 68f));
            CreateText(block, "ProgressLabel", template, "ПОБЕД ПОДРЯД", 27f, Cream, new Vector2(0f, -25f), new Vector2(360f, 36f));
        }

        private static void BuildMilestoneTrack(RectTransform parent, TMP_Text template)
        {
            RectTransform track = CreateRect(parent, "MilestoneTrack", Center, Center, Center, new Vector2(1110f, 92f), new Vector2(0f, -73f));
            Image line = CreateImage(track, "Line", null, Image.Type.Simple, new Vector2(0f, -3f), new Vector2(920f, 3f)).GetComponent<Image>();
            line.color = new Color(1f, 0.84f, 0.26f, 1f);
            Sprite jewel = LoadSprite($"{DailyRewardsAssetRoot}/DailyRewardsTopFrameJewel.asset");
            Material jewelMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{DailyRewardsAssetRoot}/DailyRewardsTopFrameJewel.mat");
            string[] roman = { "I", "II", "III", "IV", "V" };
            for (int i = 0; i < 5; i++)
            {
                float x = -460f + i * 230f;
                RectTransform milestone = CreateImage(track, $"Milestone_{i + 1}", jewel, Image.Type.Simple, new Vector2(x, -3f), new Vector2(42f, 46f));
                milestone.GetComponent<Image>().material = jewelMaterial;
                Image glow = CreateImage(milestone, "ActiveGlow", jewel, Image.Type.Simple, Vector2.zero, new Vector2(58f, 64f)).GetComponent<Image>();
                glow.material = jewelMaterial;
                glow.color = new Color(0.4f, 1f, 0.35f, 0.36f);
                glow.gameObject.SetActive(i == 0);
                glow.transform.SetAsFirstSibling();
                CreateText(milestone, "Roman", template, roman[i], 32f, Gold, new Vector2(0f, 47f), new Vector2(90f, 40f));
            }
        }

        private static void BuildStageRow(RectTransform parent, Transform dailyCard, TMP_Text template)
        {
            RectTransform row = CreateRect(parent, "StageRow", Center, Center, Center, new Vector2(1160f, 242f), new Vector2(0f, -205f));
            Sprite[] icons =
            {
                LoadSprite("Assets/Resources/UI/Premium/icon_coin_stack.png"),
                LoadSprite("Assets/Resources/UI/Premium/icon_hint_bulb.png"),
                LoadSprite("Assets/Resources/UI/Premium/icon_undo_gold.png")
            };

            int[,] values = { { 300, 1, 0 }, { 500, 1, 1 }, { 700, 2, 2 }, { 1000, 3, 3 }, { 1500, 5, 5 } };
            for (int i = 0; i < 5; i++)
            {
                GameObject cardObject = UnityEngine.Object.Instantiate(dailyCard.gameObject, row, false);
                cardObject.name = $"Stage_{i + 1}";
                RectTransform card = cardObject.GetComponent<RectTransform>();
                SetRect(card, Center, Center, Center, new Vector2(222f, 232f), new Vector2(-460f + i * 230f, 0f));
                StripDailyCardContent(card);

                RectTransform glow = card.Find("ActiveGlow") as RectTransform;
                SetRect(glow, Center, Center, Center, new Vector2(238f, 248f), Vector2.zero);
                glow.GetComponent<Image>().color = new Color(1f, 0.82f, 0.18f, 0.14f);
                glow.gameObject.SetActive(i == 0);
                RectTransform fill = card.Find("CardFill") as RectTransform;
                SetRect(fill, Center, Center, Center, new Vector2(202f, 212f), Vector2.zero);
                fill.GetComponent<Image>().color = new Color(0.72f, 0.88f, 0.76f, 0.94f);

                for (int rowIndex = 0; rowIndex < 3; rowIndex++)
                {
                    RectTransform rewardRow = CreateRect(card, $"RewardRow_{rowIndex + 1}", Center, Center, Center, new Vector2(178f, 48f), new Vector2(0f, 62f - rowIndex * 62f));
                    CreateImage(rewardRow, "Icon", icons[rowIndex], Image.Type.Simple, new Vector2(-58f, 0f), new Vector2(36f, 36f));
                    string value = values[i, rowIndex] > 0 ? values[i, rowIndex].ToString() : "–";
                    TMP_Text valueText = CreateText(rewardRow, "Value", template, value, 30f, Cream, new Vector2(25f, 0f), new Vector2(116f, 42f));
                    valueText.alignment = TextAlignmentOptions.Left;
                }
            }
        }

        private static void StripDailyCardContent(RectTransform card)
        {
            string[] remove = { "DayBadge", "Icon", "RewardAmount", "RewardType", "StatusArea", "RewardIcon" };
            foreach (string name in remove)
            {
                Transform child = card.Find(name);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            Image frame = card.GetComponent<Image>();
            frame.raycastTarget = false;
            if (card.TryGetComponent(out Button button))
            {
                UnityEngine.Object.DestroyImmediate(button);
            }
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_Text template, string value, float size, Color color, Vector2 position, Vector2 dimensions)
        {
            GameObject instance = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            instance.name = name;
            TMP_Text text = instance.GetComponent<TMP_Text>();
            RectTransform rect = instance.GetComponent<RectTransform>();
            SetRect(rect, Center, Center, Center, dimensions, position);
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Image.Type type, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = CreateRect(parent, name, Center, Center, Center, dimensions, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = type == Image.Type.Simple;
            image.raycastTarget = false;
            image.color = sprite != null ? Color.white : Green;
            return rect;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchorMin, anchorMax, pivot, size, position);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void CopyRect(RectTransform source, RectTransform destination)
        {
            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.pivot = source.pivot;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
            destination.localScale = Vector3.one;
            destination.localRotation = Quaternion.identity;
        }

        private static Transform FindSceneObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindRecursive(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static readonly Vector2 Center = new(0.5f, 0.5f);
    }

    [InitializeOnLoad]
    internal static class RoyalSeriesSceneAutoRepair
    {
        static RoyalSeriesSceneAutoRepair()
        {
            EditorApplication.delayCall += Repair;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += Repair;
            }
        }

        private static void Repair()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Repair;
                return;
            }

            RoyalSeriesSceneInstaller.RepairLoadedSceneIfNeeded();
        }
    }
}
#endif
