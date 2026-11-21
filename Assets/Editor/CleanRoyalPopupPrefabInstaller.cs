using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh.EditorTools
{
    public static class CleanRoyalPopupPrefabInstaller
    {
        [MenuItem("Tools/CardMesh/Apply Clean Royal Popup Art")]
        public static void ApplyAllPrefabArt()
        {
            ApplyDaily();
            ApplyRoyal();
            ApplyCollection();
            AssetDatabase.SaveAssets();
            Debug.Log("Clean Royal: authored raster art applied to all three popup prefabs.");
        }

        [MenuItem("Tools/CardMesh/Apply Clean Royal Popup Art To Main Menu")]
        public static void ApplyAllPrefabAndSceneArt()
        {
            ApplyAllPrefabArt();
            Scene scene = EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (DailyRewardsPopup popup in sceneRoot.GetComponentsInChildren<DailyRewardsPopup>(true))
                {
                    popup.MaterializeAuthoringLayout();
                    ApplyDailyVisuals(popup.transform);
                }
                foreach (ChestChasePopup popup in sceneRoot.GetComponentsInChildren<ChestChasePopup>(true))
                {
                    ApplyRoyalVisuals(popup.transform);
                }
                foreach (CollectionBookPopup popup in sceneRoot.GetComponentsInChildren<CollectionBookPopup>(true))
                {
                    popup.MaterializeAuthoringLayout();
                    ApplyCollectionVisuals(popup.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Clean Royal: popup prefabs and authored MainMenu instances updated.");
        }

        [MenuItem("Tools/CardMesh/Apply Royal + Collection Final Polish To Main Menu")]
        public static void ApplyRoyalAndCollectionFinalPolish()
        {
            ApplyRoyal();
            ApplyCollection();
            Scene scene = EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (ChestChasePopup popup in sceneRoot.GetComponentsInChildren<ChestChasePopup>(true))
                {
                    ApplyRoyalVisuals(popup.transform);
                }
                foreach (CollectionBookPopup popup in sceneRoot.GetComponentsInChildren<CollectionBookPopup>(true))
                {
                    popup.MaterializeAuthoringLayout();
                    ApplyCollectionVisuals(popup.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Clean Royal: Royal + Collection final polish materialized in prefabs and MainMenu.");
        }

        [MenuItem("Tools/CardMesh/Apply Royal Series Final Polish To Main Menu")]
        public static void ApplyRoyalFinalPolish()
        {
            ApplyRoyal();
            Scene scene = EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (ChestChasePopup popup in sceneRoot.GetComponentsInChildren<ChestChasePopup>(true))
                {
                    ApplyRoyalVisuals(popup.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Clean Royal: Royal Series final polish materialized in prefab and MainMenu.");
        }

        private static void ApplyDaily()
        {
            const string path = "Assets/Prefabs/UI/MainMenu/DailyRewardsPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                DailyRewardsPopup popup = root.GetComponent<DailyRewardsPopup>()
                    ?? root.GetComponentInChildren<DailyRewardsPopup>(true);
                popup?.MaterializeAuthoringLayout();
                ApplyDailyVisuals(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyRoyal()
        {
            const string path = "Assets/Prefabs/UI/MainMenu/RoyalSeriesPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyRoyalVisuals(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyCollection()
        {
            const string path = "Assets/Prefabs/UI/MainMenu/CollectionBookPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CollectionBookPopup popup = root.GetComponent<CollectionBookPopup>()
                    ?? root.GetComponentInChildren<CollectionBookPopup>(true);
                popup?.MaterializeAuthoringLayout();
                ApplyCollectionVisuals(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ApplyDailyVisuals(Transform root)
        {
            for (int day = 1; day <= 7; day++)
            {
                Transform card = FindDeep(root, $"Day{day}Card");
                if (card == null) continue;
                RectTransform cardRect = card as RectTransform;
                if (cardRect != null) cardRect.sizeDelta = new Vector2(195f, 250f);
                LayoutElement layout = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 195f;
                layout.minHeight = layout.preferredHeight = 250f;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;
                Image image = card.GetComponent<Image>();
                image.sprite = day == 3 ? CleanRoyalPopupSprites.DailyCardAvailable : CleanRoyalPopupSprites.DailyCard;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                SetInactive(card.Find("CardFill"));
                SetInactive(card.Find("ActiveGlow"));
                NormalizeDailyCheck(card);
            }

            Transform hero = FindDeep(root, "RewardDetailsPanel");
            if (hero == null) return;
            Image heroImage = hero.GetComponent<Image>();
            heroImage.sprite = CleanRoyalPopupSprites.DailyHero;
            heroImage.type = Image.Type.Sliced;
            heroImage.color = Color.white;
            DisableLines(hero);
            CleanCloseButton(root);
        }

        private static void ApplyRoyalVisuals(Transform root)
        {
            for (int index = 1; index <= 5; index++)
            {
                Transform stage = FindDeep(root, $"Stage_{index}");
                if (stage == null) continue;
                Image rootImage = stage.GetComponent<Image>();
                if (rootImage != null) rootImage.enabled = false;
                RectTransform stageRect = stage as RectTransform;
                if (stageRect != null)
                {
                    stageRect.sizeDelta = new Vector2(260f, 380f);
                    if (index >= 4)
                    {
                        stageRect.anchoredPosition = new Vector2(stageRect.anchoredPosition.x, -210f);
                    }
                }
                SetInactive(stage.Find("ActiveGlow"));
                Image surface = stage.Find("CardFill")?.GetComponent<Image>();
                if (surface == null) continue;
                surface.rectTransform.sizeDelta = new Vector2(242f, 362f);
                surface.sprite = CleanRoyalPopupSprites.RoyalStageIdle;
                surface.type = Image.Type.Sliced;
                surface.color = Color.white;
            }

            Sprite cleanMilestone = CleanRoyalPopupSprites.RoyalMilestoneDiamond;
            for (int index = 1; index <= 5; index++)
            {
                Transform milestone = FindDeep(root, $"Milestone_{index}");
                Image image = milestone?.GetComponent<Image>();
                if (image != null && cleanMilestone != null)
                {
                    image.sprite = cleanMilestone;
                    image.material = null;
                    image.preserveAspect = true;
                    image.rectTransform.sizeDelta = new Vector2(28f, 28f);
                }
                // The second jewel produced the two side whiskers in Play Mode.
                // Remove it from authored data so runtime refreshes cannot revive it.
                Transform glow = milestone?.Find("ActiveGlow")
                    ?? milestone?.Find("LegacyMilestoneGlowDisabled");
                if (glow != null)
                {
                    UnityEngine.Object.DestroyImmediate(glow.gameObject);
                }
            }

            SetPanel(root, "RewardPanel", CleanRoyalPopupSprites.RoyalGoal);
            SetPanel(root, "ProgressBadge", CleanRoyalPopupSprites.RoyalProgress);
            RectTransform progressBadge = FindDeep(root, "ProgressBadge") as RectTransform;
            if (progressBadge != null) progressBadge.sizeDelta = new Vector2(520f, 140f);
            Transform fill = FindDeep(root, "ProgressFill");
            if (fill != null)
            {
                Image image = fill.GetComponent<Image>();
                image.sprite = CleanRoyalPopupSprites.RoyalProgressFill;
                image.type = Image.Type.Filled;
                image.color = Color.white;
                image.rectTransform.sizeDelta = new Vector2(450f, 64f);
            }
            RectTransform progressCount = FindDeep(root, "ProgressCount") as RectTransform;
            if (progressCount != null) progressCount.sizeDelta = new Vector2(480f, 94f);
            SetIcon(root, "Reward_2", CleanRoyalPopupSprites.GameplayHint, 8f);
            SetIcon(root, "Reward_3", CleanRoyalPopupSprites.GameplayUndo, -6f);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "RewardRow_2") SetChildIcon(child, CleanRoyalPopupSprites.GameplayHint, 8f);
                if (child.name == "RewardRow_3") SetChildIcon(child, CleanRoyalPopupSprites.GameplayUndo, -6f);
            }
            CleanCloseButton(root);
        }

        private static void ApplyCollectionVisuals(Transform root)
        {
            // The Royal Codex interior is fully scene-authored by its dedicated
            // installer. Generic popup polish must not replace its sprites,
            // rotations, states or approved shared chrome.
            if (FindDeep(root, "RoyalCodexInterior") != null)
            {
                return;
            }

            SetPanel(root, "ProgressBlock", CleanRoyalPopupSprites.CollectionProgress);
            SetPanel(root, "ScoreRules", CleanRoyalPopupSprites.CollectionInfo);
            SetPanel(root, "LevelBlock", CleanRoyalPopupSprites.CollectionInfo);
            Sprite cleanMilestone = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/MainMenuEmeraldVault/badge_diamond.png");
            for (int index = 1; index <= 6; index++)
            {
                SetPanel(root, $"RewardStage_{index}", CleanRoyalPopupSprites.CollectionReward);
                Transform stage = FindDeep(root, $"RewardStage_{index}");
                SetChildIcon(stage, index is 1 or 4 ? CleanRoyalPopupSprites.GameplayUndo
                    : index is 2 or 5 ? CleanRoyalPopupSprites.GameplayHint
                    : index == 6 ? CleanRoyalPopupSprites.GameplayMoves : null,
                    index is 1 or 4 ? 6f : index is 2 or 5 ? -8f : 0f);
                DisableLegacySurfaceLayers(stage);
                Image milestone = stage?.Find("MilestoneDiamond")?.GetComponent<Image>();
                if (milestone != null && cleanMilestone != null)
                {
                    milestone.sprite = cleanMilestone;
                    milestone.material = null;
                    milestone.preserveAspect = true;
                    milestone.rectTransform.sizeDelta = new Vector2(24f, 24f);
                }
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name is "CurrentProgressMarker" or "CurrentProgressLine" or "MilestoneLine"
                    or "MilestoneGlow" or "DividerStrip")
                {
                    child.gameObject.SetActive(false);
                }
            }
            Transform rewards = FindDeep(root, "RewardStages");
            if (rewards != null)
            {
                for (int index = rewards.childCount - 1; index >= 0; index--)
                {
                    Transform child = rewards.GetChild(index);
                    if (!child.name.StartsWith("RewardStage_")) child.gameObject.SetActive(false);
                }
            }
            CleanCloseButton(root);
        }

        private static void NormalizeDailyCheck(Transform card)
        {
            RectTransform preferred = null;
            foreach (RectTransform candidate in card.GetComponentsInChildren<RectTransform>(true))
            {
                if (candidate.name != "Check") continue;
                if (candidate.parent != null && candidate.parent.name == "StatusArea" && preferred == null)
                {
                    preferred = candidate;
                }
                candidate.gameObject.SetActive(false);
            }
            if (preferred == null) return;
            preferred.anchorMin = preferred.anchorMax = preferred.pivot = new Vector2(0.5f, 0.5f);
            preferred.anchoredPosition = new Vector2(-51f, 0f);
            preferred.sizeDelta = new Vector2(23f, 18f);
        }

        private static void SetPanel(Transform root, string name, Sprite sprite)
        {
            Transform target = FindDeep(root, name);
            if (target == null || sprite == null) return;
            Image image = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
            image.enabled = true;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            DisableLines(target);
            DisableLegacySurfaceLayers(target);
        }

        private static void CleanCloseButton(Transform root)
        {
            Transform close = FindDeep(root, "CloseButton");
            if (close == null) return;
            Image target = close.GetComponent<Image>();
            Button button = close.GetComponent<Button>();
            if (target != null)
            {
                target.sprite = null;
                target.color = Color.clear;
                target.raycastTarget = true;
                if (button != null) button.targetGraphic = target;
            }
            bool hasAuthoredStrokes = close.Find("CloseStrokeA") != null && close.Find("CloseStrokeB") != null;
            foreach (Image child in close.GetComponentsInChildren<Image>(true))
            {
                if (child.transform == close) continue;
                bool isStroke = hasAuthoredStrokes
                    ? child.name is "CloseStrokeA" or "CloseStrokeB"
                    : child.name is "StrokeA" or "StrokeB";
                child.gameObject.SetActive(isStroke);
            }
        }

        private static void DisableLegacySurfaceLayers(Transform target)
        {
            if (target == null) return;
            foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            {
                if (child == target) continue;
                bool legacy = child.name is "CardFill" or "ActiveGlow" or "InnerHighlight"
                    or "ProgressFrame" or "InfoFrame" or "InfoFill" or "InnerPanel"
                    or "BorderTop" or "BorderBottom" or "BorderLeft" or "BorderRight"
                    || child.name.StartsWith("CleanRoyal", System.StringComparison.Ordinal);
                if (legacy) child.gameObject.SetActive(false);
            }
        }

        private static void SetIcon(Transform root, string parentName, Sprite sprite, float rotation)
        {
            SetChildIcon(FindDeep(root, parentName), sprite, rotation);
        }

        private static void SetChildIcon(Transform parent, Sprite sprite, float rotation = 0f)
        {
            if (parent == null || sprite == null) return;
            Image icon = parent.Find("Icon")?.GetComponent<Image>() ?? parent.Find("RewardIcon")?.GetComponent<Image>();
            if (icon == null) return;
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void DisableLines(Transform target)
        {
            foreach (Outline outline in target.GetComponentsInChildren<Outline>(true)) outline.enabled = false;
            foreach (string name in new[] { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight", "ProgressFrame", "InfoFrame", "InfoFill", "InnerPanel", "ActiveGlow" })
            {
                foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name) SetInactive(child);
                }
            }
        }

        private static void SetInactive(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
