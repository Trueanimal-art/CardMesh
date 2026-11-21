using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh.Editor
{
    /// <summary>
    /// Explicit editor-only materializer for the Collection Book interior.
    /// It deliberately owns only named content roots under Panel and never
    /// changes Panel, LevelFrameChrome, HeaderIcon, CloseButton or frame art.
    /// </summary>
    public static class CollectionBookRoyalCodexInstaller
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainMenu/CollectionBookPopup.prefab";
        private const string ScenePath = "Assets/MainMenu.unity";
        private const string ArtRoot = "Assets/Resources/UI/CleanRoyal/CollectionCodex/";

        private static readonly string[] OwnedPanelChildren =
        {
            "RoyalCodexInterior", "ProgressBlock", "RewardStages", "CodexBottom",
            "ScoreRules", "LevelBlock", "BookHero", "RewardTitle", "OpenBookButton",
            "MilestoneLine", "DividerStrip"
        };

        [MenuItem("Tools/CardMesh/Rebuild Collection Book Royal Codex")]
        public static void RebuildRoyalCodexAssets()
        {
            ConfigureImports();
            RebuildPrefab();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CollectionBookPopup scenePopup = UnityEngine.Object.FindFirstObjectByType<CollectionBookPopup>(FindObjectsInactive.Include);
            if (scenePopup == null)
            {
                throw new InvalidOperationException("CollectionBookPopup is missing in MainMenu.unity.");
            }

            BuildInterior(scenePopup.gameObject);
            EditorUtility.SetDirty(scenePopup);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Collection Book Royal Codex: rebuilt prefab and MainMenu scene. Approved outer chrome was not modified.");
        }

        private static void RebuildPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CollectionBookPopup popup = root.GetComponent<CollectionBookPopup>()
                    ?? root.GetComponentInChildren<CollectionBookPopup>(true);
                if (popup == null)
                {
                    throw new InvalidOperationException("CollectionBookPopup component is missing in prefab.");
                }

                BuildInterior(popup.gameObject);
                EditorUtility.SetDirty(popup);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildInterior(GameObject popupRoot)
        {
            RectTransform panel = popupRoot.transform.Find("Panel") as RectTransform;
            if (panel == null)
            {
                throw new InvalidOperationException("CollectionBookPopup/Panel is missing.");
            }

            RectTransform header = panel.Find("Header") as RectTransform;
            int headerSibling = header != null ? header.GetSiblingIndex() : panel.childCount;

            foreach (string childName in OwnedPanelChildren)
            {
                Transform child = panel.Find(childName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            RemoveComponent<CollectionBookPremiumMotion>(popupRoot);
            BuildHeader(panel, header);

            RectTransform interior = CreateRect(panel, "RoyalCodexInterior", new Vector2(900f, 820f), new Vector2(0f, -930f), true);
            interior.SetSiblingIndex(Mathf.Clamp(headerSibling, 0, panel.childCount - 1));
            CreateImage(interior, "OpenBookArt", Sprite("collection_codex_book_spread.png"), new Vector2(900f, 780f), new Vector2(0f, -5f), Color.white, false);

            BuildProgress(panel);
            BuildRewards(panel);
            BuildBottom(panel);
            ApplyCodexTypography(panel);
            ApplyOwnedSiblingOrder(panel);
            BindSerializedReferences(popupRoot.GetComponent<CollectionBookPopup>(), panel);

            foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
            {
                if (child.name is "CurrentProgressLine" or "CurrentProgressMarker" or "MilestoneGlow")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void BuildHeader(RectTransform panel, RectTransform existingHeader)
        {
            RectTransform header = existingHeader ?? CreateRect(panel, "Header", new Vector2(880f, 120f), new Vector2(0f, -300f), true);
            SetTopRect(header, new Vector2(880f, 122f), new Vector2(0f, -354f));

            TMP_Text title = FindOrCreateText(header, "Title", "КНИГА КОЛЛЕКЦИЙ", 56f,
                new Color(1f, 0.83f, 0.28f, 1f), new Vector2(850f, 90f), new Vector2(0f, 12f));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text subtitle = FindOrCreateText(header, "Subtitle", "Собирай категории, пополняй книгу и получай ценные награды!", 14f,
                new Color(1f, 0.9f, 0.68f, 0.58f), new Vector2(820f, 28f), new Vector2(0f, -42f));
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void BuildProgress(RectTransform panel)
        {
            RectTransform root = CreateRect(panel, "ProgressBlock", new Vector2(780f, 160f), new Vector2(0f, -510f), true);
            Image ribbon = root.gameObject.AddComponent<Image>();
            ribbon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_progress_panel.png");
            ribbon.type = Image.Type.Simple;
            ribbon.raycastTarget = false;

            TMP_Text title = CreateText(root, "ProgressTitle", "ПРОГРЕСС ДО НАГРАДЫ", 18f,
                new Color(1f, 0.79f, 0.2f, 1f), new Vector2(390f, 30f), new Vector2(-125f, 37f));
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text value = CreateText(root, "ProgressValue", "0 / 10", 34f,
                new Color(1f, 0.92f, 0.62f, 1f), new Vector2(190f, 46f), new Vector2(205f, 35f));
            value.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform track = CreateRect(root, "ProgressTrack", new Vector2(590f, 36f), new Vector2(0f, -12f));
            Image trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/royal_progress_track.png");
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(0.18f, 0.48f, 0.35f, 1f);
            trackImage.raycastTarget = false;
            Image fill = CreateStretchImage(track, "ProgressFill",
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/royal_progress_fill.png"), Color.white);
            fill.rectTransform.offsetMin = new Vector2(8f, 7f);
            fill.rectTransform.offsetMax = new Vector2(-8f, -7f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            TMP_Text next = CreateText(root, "NextReward", "Следующая награда: +1 Отмена", 16f,
                new Color(1f, 0.91f, 0.7f, 1f), new Vector2(600f, 26f), new Vector2(0f, -57f));
            next.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void BuildRewards(RectTransform panel)
        {
            RectTransform root = CreateRect(panel, "RewardStages", new Vector2(820f, 610f), new Vector2(0f, -932f), true);
            int[] points = { 10, 25, 50, 75, 100, 150 };
            string[] rewards = { "+1 Отмена", "+1 Подсказка", "50 монет", "+2 Отмены", "+2 Подсказки", "+3 хода" };
            Sprite[] icons =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Premium/icon_undo_gold.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Premium/icon_hint_bulb.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Premium/icon_coin_stack.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Premium/icon_undo_gold.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Premium/icon_hint_bulb.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/GameActions/VariantA/icon_add_moves_cards_final.png")
            };

            for (int index = 0; index < 6; index++)
            {
                int column = index % 3;
                int row = index / 3;
                RectTransform card = CreateRect(root, $"RewardStage_{index + 1}", new Vector2(230f, 226f),
                    new Vector2((column - 1) * 258f, row == 0 ? 130f : -130f));
                card.localRotation = Quaternion.identity;

                Image current = CreateStretchImage(card, "StateCurrent",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_reward_card.png"),
                    new Color(0.18f, 1f, 0.5f, 0.8f));
                current.rectTransform.offsetMin = new Vector2(-8f, -8f);
                current.rectTransform.offsetMax = new Vector2(8f, 8f);
                current.gameObject.SetActive(index == 0);

                Image frame = CreateStretchImage(card, "StageFrame",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_reward_card.png"), Color.white);
                frame.preserveAspect = false;

                Image claimed = CreateStretchImage(card, "StateClaimed",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_reward_card.png"),
                    new Color(0.78f, 0.66f, 0.25f, 0.18f));
                claimed.gameObject.SetActive(false);
                Image locked = CreateStretchImage(card, "StateLocked",
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_reward_card.png"),
                    new Color(0.02f, 0.07f, 0.045f, 0.34f));
                locked.gameObject.SetActive(index != 0);

                CreateText(card, "Points", points[index].ToString(), 27f,
                    new Color(1f, 0.85f, 0.3f, 1f), new Vector2(160f, 38f), new Vector2(0f, 73f));
                Image icon = CreateImage(card, "RewardIcon", icons[index], new Vector2(72f, 72f), new Vector2(0f, 5f), Color.white, true);
                icon.rectTransform.localRotation = Quaternion.identity;
                TMP_Text reward = CreateText(card, "Reward", rewards[index], 18f,
                    new Color(1f, 0.91f, 0.7f, 1f), new Vector2(190f, 48f), new Vector2(0f, -72f));
                reward.textWrappingMode = TextWrappingModes.Normal;
                CreateText(card, "Status", index == 0 ? "ТЕКУЩАЯ" : string.Empty, 12f,
                    new Color(0.55f, 1f, 0.68f, 1f), new Vector2(190f, 24f), new Vector2(0f, 101f));

                Image seal = CreateImage(card, "ClaimedSeal", Sprite("collection_codex_reward_claimed_seal.png"),
                    new Vector2(42f, 42f), new Vector2(88f, -82f), Color.white, true);
                seal.gameObject.SetActive(false);
            }
        }

        private static void BuildBottom(RectTransform panel)
        {
            RectTransform rules = CreateRect(panel, "ScoreRules", new Vector2(410f, 220f), new Vector2(-212f, -1450f), true);
            Image rulesBackground = rules.gameObject.AddComponent<Image>();
            rulesBackground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_info_panel.png");
            rulesBackground.type = Image.Type.Simple;
            rulesBackground.raycastTarget = false;
            CreateText(rules, "Title", "КАК ПОЛУЧАТЬ ОЧКИ?", 20f,
                new Color(1f, 0.79f, 0.2f, 1f), new Vector2(370f, 32f), new Vector2(0f, 67f));
            TMP_Text rows = CreateText(rules, "Rows",
                "Обычная категория  +1 очко\nКатегория с картинками  +2 очка\nБез подсказок  +1 бонус\nБез отмены  +1 бонус",
                16.5f, new Color(1f, 0.92f, 0.75f, 1f), new Vector2(360f, 140f), new Vector2(0f, -20f));
            rows.alignment = TextAlignmentOptions.Left;
            rows.margin = new Vector4(20f, 8f, 14f, 8f);
            rows.lineSpacing = 1f;

            RectTransform level = CreateRect(panel, "LevelBlock", new Vector2(410f, 220f), new Vector2(212f, -1450f), true);
            Image levelBackground = level.gameObject.AddComponent<Image>();
            levelBackground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/CleanRoyal/collection_info_panel.png");
            levelBackground.type = Image.Type.Simple;
            levelBackground.raycastTarget = false;
            CreateText(level, "Title", "УРОВЕНЬ КОЛЛЕКЦИИ", 20f,
                new Color(1f, 0.79f, 0.2f, 1f), new Vector2(370f, 32f), new Vector2(0f, 67f));
            CreateImage(level, "RankCrest", Sprite("collection_codex_rank_crest.png"), new Vector2(82f, 88f), new Vector2(-118f, -5f), Color.white, true);
            CreateText(level, "RankNumeral", "I", 28f,
                new Color(1f, 0.88f, 0.42f, 1f), new Vector2(42f, 42f), new Vector2(-118f, -4f));
            CreateText(level, "LevelTitle", "КОЛЛЕКЦИОНЕР I", 19f,
                new Color(1f, 0.88f, 0.46f, 1f), new Vector2(250f, 34f), new Vector2(58f, 18f));
            CreateText(level, "LevelProgress", "0 / 10 ОЧКОВ", 17f,
                new Color(0.48f, 1f, 0.62f, 1f), new Vector2(250f, 28f), new Vector2(58f, -25f));
            CreateText(level, "LevelRemaining", "До следующего уровня: 10 очков", 14f,
                new Color(1f, 0.9f, 0.7f, 1f), new Vector2(270f, 38f), new Vector2(58f, -64f));
        }

        private static void ApplyOwnedSiblingOrder(RectTransform panel)
        {
            Transform chrome = panel.Find("LevelFrameChrome");
            int index = chrome != null ? chrome.GetSiblingIndex() + 1 : 0;
            foreach (string name in new[] { "RoyalCodexInterior", "ProgressBlock", "RewardStages", "ScoreRules", "LevelBlock", "Header" })
            {
                Transform child = panel.Find(name);
                if (child != null) child.SetSiblingIndex(Mathf.Min(index++, panel.childCount - 1));
            }

            // Approved shared chrome keeps its authored hierarchy unchanged.
        }

        private static void BindSerializedReferences(CollectionBookPopup popup, RectTransform panel)
        {
            if (popup == null) return;
            SerializedObject serialized = new(popup);
            SetObject(serialized, "panel", panel);
            SetObject(serialized, "canvasGroup", popup.GetComponent<CanvasGroup>());
            SetObject(serialized, "titleText", panel.Find("Header/Title")?.GetComponent<TMP_Text>());
            SetObject(serialized, "subtitleText", panel.Find("Header/Subtitle")?.GetComponent<TMP_Text>());
            SetObject(serialized, "progressValueText", panel.Find("ProgressBlock/ProgressValue")?.GetComponent<TMP_Text>());
            SetObject(serialized, "nextRewardText", panel.Find("ProgressBlock/NextReward")?.GetComponent<TMP_Text>());
            SetObject(serialized, "levelTitleText", panel.Find("LevelBlock/LevelTitle")?.GetComponent<TMP_Text>());
            SetObject(serialized, "levelProgressText", panel.Find("LevelBlock/LevelProgress")?.GetComponent<TMP_Text>());
            SetObject(serialized, "levelRemainingText", panel.Find("LevelBlock/LevelRemaining")?.GetComponent<TMP_Text>());
            SetObject(serialized, "progressFill", panel.Find("ProgressBlock/ProgressTrack/ProgressFill")?.GetComponent<Image>());
            SetObject(serialized, "levelProgressFill", null);
            SetObject(serialized, "rewardCardsRoot", panel.Find("RewardStages") as RectTransform);
            SetObject(serialized, "scoreRowsRoot", panel.Find("ScoreRules") as RectTransform);
            SetObject(serialized, "premiumMotion", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyCodexTypography(RectTransform panel)
        {
            TMP_FontAsset prata = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Resources/Fonts/Prata Cyrillic SDF.asset");
            if (prata == null) return;

            TMP_Text mainTitle = panel.Find("Header/Title")?.GetComponent<TMP_Text>();
            if (mainTitle != null)
            {
                mainTitle.font = prata;
                mainTitle.fontSharedMaterial = prata.material;
                mainTitle.fontStyle = FontStyles.Normal;
                mainTitle.enableAutoSizing = false;
                mainTitle.fontSize = 56f;
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void ConfigureImports()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot.TrimEnd('/') });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = path.Contains("icon_") || path.Contains("seal") ? 512 : 2048;
                importer.SaveAndReimport();
            }
        }

        private static Sprite Sprite(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + fileName);

        private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 position, bool topAnchored = false)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            if (topAnchored)
            {
                SetTopRect(rect, size, position);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = position;
            }
            return rect;
        }

        private static void SetTopRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, Color color, bool preserveAspect)
        {
            RectTransform rect = CreateRect(parent, name, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateStretchImage(RectTransform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text FindOrCreateText(Transform parent, string name, string value, float size, Color color, Vector2 rectSize, Vector2 position)
        {
            TMP_Text existing = parent.Find(name)?.GetComponent<TMP_Text>();
            if (existing == null)
            {
                return CreateText(parent, name, value, size, color, rectSize, position);
            }

            SetRect(existing.rectTransform, rectSize, position);
            ConfigureText(existing, value, size, color);
            return existing;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, Vector2 rectSize, Vector2 position)
        {
            RectTransform rect = CreateRect(parent, name, rectSize, position);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            ConfigureText(text, value, size, color);
            return text;
        }

        private static void ConfigureText(TMP_Text text, string value, float size, Color color)
        {
            text.text = value;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = size;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(8f, size * 0.72f);
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            RuntimeCyrillicFontProvider.Apply(text, value);
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void RemoveComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }
    }
}
