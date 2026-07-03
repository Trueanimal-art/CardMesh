using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations.Editor
{
    public static class SolitaireUILayoutApplier
    {
        private const float TopHudHeight = 124f;
        private const float TopHudTopOffset = 32f;
        private const float SideMargin = 56f;

        private const float MenuButtonSize = 74f;
        private const float MenuButtonX = 126f;
        private const float MenuButtonY = -52f;

        private const float CoinsPanelWidth = 250f;
        private const float CoinsPanelHeight = 68f;
        private const float CoinsPanelX = 356f;
        private const float CoinsPanelY = -52f;

        private const float RewardButtonWidth = 120f;
        private const float RewardButtonHeight = 68f;
        private const float RewardButtonX = -150f;
        private const float RewardButtonY = -52f;

        private const float MovesPanelWidth = 156.57f;
        private const float MovesPanelHeight = 153.8f;
        private const float MovesPanelX = 220.29f;
        private const float MovesPanelY = -240.9f;

        private const float DeckAreaWidth = 162f;
        private const float DeckAreaHeight = 214f;
        private const float DeckAreaX = -288f;
        private const float DeckAreaY = -263f;

        private const float BoardLeft = 26f;
        private const float BoardRight = 26f;
        private const float BoardTop = 142f;
        private const float BoardBottom = 228f;

        private const float BottomHudHeight = 146f;
        private const float BottomHudBottom = 72f;
        private static readonly Vector2 BottomButtonSize = new(208f, 52f);

        private const float AdBannerHeight = 64f;

        private const float ColumnSpacing = 12f;
        private const float ColumnWidth = 196f;
        private const float ColumnHeight = 500f;
        private const float CrownLabelWidth = 156f;
        private const float CrownLabelHeight = 34f;
        private const float CrownSlotWidth = 196f;
        private const float CrownSlotHeight = 286f;
        private const float CardsStackWidth = 196f;
        private const float CardsStackHeight = 312f;

        [MenuItem("Tools/Solitaire/Apply UI Layout")]
        public static void ApplyUILayout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Solitaire UI layout can only be applied in Edit Mode.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("Missing UI element: ActiveScene");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Solitaire UI Layout");

            int missingCount = 0;

            Transform canvas = FindRoot(scene, "Canvas", ref missingCount);
            if (canvas == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            RectTransform canvasRect = canvas as RectTransform;
            RectTransform safeAreaRoot = FindRect(canvas, "SafeAreaRoot", "SafeAreaRoot", ref missingCount);
            if (safeAreaRoot == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            ConfigureCanvasScaler(canvas.gameObject);

            RectTransform background = FindRect(safeAreaRoot, "Background", "Background", ref missingCount);
            RectTransform topHud = FindRect(safeAreaRoot, "TopHUD", "TopHUD", ref missingCount);
            RectTransform movesPanel = FindRect(safeAreaRoot, "MovesPanel", "MovesPanel", ref missingCount);
            RectTransform deckArea = FindRect(safeAreaRoot, "DeckArea", "DeckArea", ref missingCount);
            RectTransform boardArea = FindRect(safeAreaRoot, "BoardArea", "BoardArea", ref missingCount);
            RectTransform bottomHud = FindRect(safeAreaRoot, "BottomHUD", "BottomHUD", ref missingCount);
            RectTransform adBanner = FindRect(safeAreaRoot, "AdBannerPlaceholder", "AdBannerPlaceholder", ref missingCount);

            RectTransform menuButton = FindRect(topHud, "MenuButton", "MenuButton", ref missingCount);
            RectTransform coinsPanel = FindRect(topHud, "CoinsPanel", "CoinsPanel", ref missingCount);
            RectTransform rewardBonusButton = FindRect(topHud, "RewardBonusButton", "RewardBonusButton", ref missingCount);

            RectTransform coinIcon = FindRect(coinsPanel, "CoinIcon", "CoinIcon", ref missingCount);
            RectTransform coinsText = FindRect(coinsPanel, "CoinsText", "CoinsText", ref missingCount);
            RectTransform plusButton = FindRect(coinsPanel, "PlusButton", "PlusButton", ref missingCount);

            RectTransform movesLabel = FindRect(movesPanel, "MovesLabel", "MovesLabel", ref missingCount);
            RectTransform movesValue = FindRect(movesPanel, "MovesValue", "MovesValue", ref missingCount);

            RectTransform deckCardBack = FindRect(deckArea, "DeckCardBack", "DeckCardBack", ref missingCount);
            RectTransform deckCounter = FindRect(deckArea, "DeckCounter", "DeckCounter", ref missingCount);

            RectTransform columnSlotsRoot = FindRect(boardArea, "ColumnSlotsRoot", "ColumnSlotsRoot", ref missingCount);

            RectTransform hintButton = FindRect(bottomHud, "HintButton", "HintButton", ref missingCount);
            RectTransform undoButton = FindRect(bottomHud, "UndoButton", "UndoButton", ref missingCount);
            RectTransform boosterDeckButton = FindRect(bottomHud, "BoosterDeckButton", "BoosterDeckButton", ref missingCount);
            RectTransform badge = FindRect(boosterDeckButton, "Badge", "Badge", ref missingCount);

            if (canvasRect != null)
            {
                SetStretch(canvasRect, 0f, 0f, 0f, 0f, "Canvas");
            }

            SetStretch(safeAreaRoot, 0f, 0f, 0f, 0f, "SafeAreaRoot");

            if (background != null)
            {
                SetStretch(background, 0f, 0f, 0f, 0f, "Background");
            }

            if (topHud != null)
            {
                SetTopStretch(topHud, SideMargin, SideMargin, TopHudTopOffset, TopHudHeight, "TopHUD");
            }

            if (menuButton != null)
            {
                SetAnchored(menuButton, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(MenuButtonSize, MenuButtonSize), new Vector2(MenuButtonX, MenuButtonY), "MenuButton");
                ApplyImageColor(menuButton.gameObject, new Color(0.15f, 0.38f, 0.18f, 0.92f), "MenuButton");
                StretchFirstTMPChild(menuButton, "MenuButton");
                ClearAllTexts(menuButton, "MenuButton");
            }

            if (coinsPanel != null)
            {
                SetAnchored(coinsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(CoinsPanelWidth, CoinsPanelHeight), new Vector2(CoinsPanelX, CoinsPanelY), "CoinsPanel");
                ApplyImageColor(coinsPanel.gameObject, new Color(0.1f, 0.31f, 0.14f, 0.92f), "CoinsPanel");
            }

            if (coinIcon != null)
            {
                SetAnchored(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), new Vector2(40f, 0f), "CoinIcon");
                Image coinImage = coinIcon.GetComponent<Image>();
                if (coinImage != null)
                {
                    Undo.RecordObject(coinImage, "Apply CoinIcon Sprite");
                    coinImage.sprite = PremiumUISprites.CoinIcon;
                    coinImage.type = Image.Type.Simple;
                    coinImage.preserveAspect = true;
                    coinImage.color = Color.white;
                    EditorUtility.SetDirty(coinImage);
                }
            }

            if (coinsText != null)
            {
                SetAnchored(coinsText, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60f, 44f), new Vector2(24f, 0f), "CoinsText");
                TMP_Text text = coinsText.GetComponent<TMP_Text>();
                SetTextValue(text, "0", "CoinsText");
                if (text != null)
                {
                    Undo.RecordObject(text, "Apply CoinsText Style");
                    TMP_Text movesValueText = movesValue != null ? movesValue.GetComponent<TMP_Text>() : null;
                    if (movesValueText != null)
                    {
                        text.font = movesValueText.font;
                        text.fontSharedMaterial = movesValueText.fontSharedMaterial;
                    }

                    text.enableAutoSizing = false;
                    text.fontSize = 30f;
                    text.fontSizeMin = 30f;
                    text.fontSizeMax = 30f;
                    text.fontStyle = FontStyles.Normal;
                    text.fontWeight = FontWeight.Regular;
                    text.enableWordWrapping = false;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = movesValueText != null ? movesValueText.color : new Color(0.31f, 0.17f, 0.04f, 1f);

                    EditorUtility.SetDirty(text);
                }
            }

            if (plusButton != null)
            {
                SetAnchored(plusButton, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(42f, 42f), new Vector2(-22f, 0f), "PlusButton");
                ApplyImageColor(plusButton.gameObject, new Color(0.95f, 0.95f, 0.95f, 1f), "PlusButton");
                StretchFirstTMPChild(plusButton, "PlusButton");
                ClearAllTexts(plusButton, "PlusButton");
            }

            if (rewardBonusButton != null)
            {
                SetAnchored(rewardBonusButton, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(RewardButtonWidth, RewardButtonHeight), new Vector2(RewardButtonX, RewardButtonY), "RewardBonusButton");
                ApplyImageColor(rewardBonusButton.gameObject, new Color(0.97f, 0.93f, 0.82f, 1f), "RewardBonusButton");
                ClearAllTexts(rewardBonusButton, "RewardBonusButton");
            }

            if (movesPanel != null)
            {
                SetAnchored(movesPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(MovesPanelWidth, MovesPanelHeight), new Vector2(MovesPanelX, MovesPanelY), "MovesPanel");
                ApplyImageColor(movesPanel.gameObject, new Color(0.23f, 0.84f, 0.5f, 1f), "MovesPanel");
            }

            if (movesLabel != null)
            {
                SetAnchored(movesLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(120f, 36f), new Vector2(0f, -28f), "MovesLabel");
                SetTextValue(movesLabel.GetComponent<TMP_Text>(), "Ходы", "MovesLabel");
            }

            if (movesValue != null)
            {
                SetAnchored(movesValue, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(118f, 94f), new Vector2(0f, -8f), "MovesValue");
                SetTextValue(movesValue.GetComponent<TMP_Text>(), "0", "MovesValue");
            }

            if (deckArea != null)
            {
                SetAnchored(deckArea, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(DeckAreaWidth, DeckAreaHeight), new Vector2(DeckAreaX, DeckAreaY), "DeckArea");
                ApplyImageColor(deckArea.gameObject, new Color(1f, 1f, 1f, 0f), "DeckArea");
            }

            if (deckCardBack != null)
            {
                SetAnchored(deckCardBack, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(CrownSlotWidth, CrownSlotHeight), Vector2.zero, "DeckCardBack");
                ApplyImageColor(deckCardBack.gameObject, new Color(0.2f, 0.62f, 0.98f, 1f), "DeckCardBack");
            }

            if (deckCounter != null)
            {
                SetAnchored(deckCounter, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, 30f), new Vector2(-16f, -14f), "DeckCounter");
                SetTextValue(deckCounter.GetComponent<TMP_Text>(), "0", "DeckCounter");
            }

            if (boardArea != null)
            {
                SetStretch(boardArea, BoardLeft, BoardRight, BoardTop, BoardBottom, "BoardArea");
            }

            if (columnSlotsRoot != null)
            {
                SetStretch(columnSlotsRoot, 0f, 0f, 0f, 0f, "ColumnSlotsRoot");
                ConfigureColumnSlotsRoot(columnSlotsRoot);
                ApplyColumns(columnSlotsRoot, ref missingCount);
            }

            if (bottomHud != null)
            {
                SetBottomStretch(bottomHud, SideMargin, SideMargin, BottomHudBottom, BottomHudHeight, "BottomHUD");
            }

            if (hintButton != null)
            {
                SetAnchored(hintButton, new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0.5f, 0.5f), BottomButtonSize, Vector2.zero, "HintButton");
                ApplyImageColor(hintButton.gameObject, new Color(0.46f, 0.65f, 0.98f, 1f), "HintButton");
                StretchFirstTMPChild(hintButton, "HintButton");
                SetPrimaryText(hintButton, "Подсказка", "HintButton");
            }

            if (undoButton != null)
            {
                SetAnchored(undoButton, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), BottomButtonSize, Vector2.zero, "UndoButton");
                ApplyImageColor(undoButton.gameObject, new Color(0.46f, 0.65f, 0.98f, 1f), "UndoButton");
                StretchFirstTMPChild(undoButton, "UndoButton");
                SetPrimaryText(undoButton, "Отмена", "UndoButton");
            }

            if (boosterDeckButton != null)
            {
                SetAnchored(boosterDeckButton, new Vector2(0.8f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0.5f, 0.5f), BottomButtonSize, Vector2.zero, "BoosterDeckButton");
                ApplyImageColor(boosterDeckButton.gameObject, new Color(0.46f, 0.65f, 0.98f, 1f), "BoosterDeckButton");
                SetPrimaryText(boosterDeckButton, "+10 ходов", "BoosterDeckButton");
            }

            if (badge != null)
            {
                ConfigureBoosterBadgeAsLabel(badge, "+10 ходов", "Badge");
            }

            if (adBanner != null)
            {
                SetBottomStretch(adBanner, 0f, 0f, 0f, AdBannerHeight, "AdBannerPlaceholder");
            }

            if (safeAreaRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(safeAreaRoot);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Solitaire UI layout applied to scene '{scene.name}'. Missing elements: {missingCount}");
        }

        private static void ApplyColumns(RectTransform columnSlotsRoot, ref int missingCount)
        {
            for (int index = 1; index <= 6; index++)
            {
                string columnName = $"Column_{index:00}";
                RectTransform column = FindRect(columnSlotsRoot, columnName, columnName, ref missingCount);
                if (column == null)
                {
                    continue;
                }

                SetAnchored(column, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(ColumnWidth, ColumnHeight), Vector2.zero, columnName);
                ConfigureColumnLayoutElement(column.gameObject);

                RectTransform crownSlotLabelBg = FindRect(column, "CrownSlotLabelBg", $"{columnName}/CrownSlotLabelBg", ref missingCount);
                RectTransform crownSlot = FindRect(column, "CrownSlot", $"{columnName}/CrownSlot", ref missingCount);
                RectTransform cardsStackRoot = FindRect(column, "CardsStackRoot", $"{columnName}/CardsStackRoot", ref missingCount);

                if (crownSlotLabelBg != null)
                {
                    SetAnchored(crownSlotLabelBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(CrownLabelWidth, CrownLabelHeight), new Vector2(0f, -8f), $"{columnName}/CrownSlotLabelBg");
                    ApplyImageColor(crownSlotLabelBg.gameObject, new Color(0.98f, 0.79f, 0.24f, 1f), $"{columnName}/CrownSlotLabelBg");
                    SetActive(crownSlotLabelBg.gameObject, false, $"{columnName}/CrownSlotLabelBg");

                    RectTransform crownSlotLabelText = FindRect(crownSlotLabelBg, "CrownSlotLabelText", $"{columnName}/CrownSlotLabelText", ref missingCount);
                    if (crownSlotLabelText != null)
                    {
                        SetAnchored(crownSlotLabelText, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, $"{columnName}/CrownSlotLabelText");
                        ConfigureGoalLabelText(crownSlotLabelText, $"{columnName}/CrownSlotLabelText");
                    }
                }

                if (crownSlot != null)
                {
                    SetAnchored(crownSlot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(CrownSlotWidth, CrownSlotHeight), new Vector2(0f, -124f), $"{columnName}/CrownSlot");
                    ApplyImageColor(crownSlot.gameObject, new Color(0.24f, 0.31f, 0.28f, 0.35f), $"{columnName}/CrownSlot");
                }

                if (cardsStackRoot != null)
                {
                    SetAnchored(cardsStackRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(CardsStackWidth, CardsStackHeight), new Vector2(0f, -268f), $"{columnName}/CardsStackRoot");
                }
            }
        }

        private static void ConfigureColumnSlotsRoot(RectTransform columnSlotsRoot)
        {
            HorizontalLayoutGroup layoutGroup = columnSlotsRoot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = Undo.AddComponent<HorizontalLayoutGroup>(columnSlotsRoot.gameObject);
            }

            Undo.RecordObject(layoutGroup, "Configure ColumnSlotsRoot");
            layoutGroup.spacing = ColumnSpacing;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = false;
            EditorUtility.SetDirty(layoutGroup);
        }

        private static void ConfigureColumnLayoutElement(GameObject columnObject)
        {
            LayoutElement layoutElement = columnObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = Undo.AddComponent<LayoutElement>(columnObject);
            }

            Undo.RecordObject(layoutElement, "Configure Column LayoutElement");
            layoutElement.minWidth = ColumnWidth;
            layoutElement.preferredWidth = ColumnWidth;
            layoutElement.flexibleWidth = 0f;
            layoutElement.minHeight = ColumnHeight;
            layoutElement.preferredHeight = ColumnHeight;
            layoutElement.flexibleHeight = 0f;
            EditorUtility.SetDirty(layoutElement);
        }

        private static void ConfigureCanvasScaler(GameObject canvasObject)
        {
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
            }

            Undo.RecordObject(scaler, "Configure CanvasScaler");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        private static Transform FindRoot(Scene scene, string objectName, ref int missingCount)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject.transform;
                }
            }

            ReportMissing(objectName, ref missingCount);
            return null;
        }

        private static RectTransform FindRect(Transform parent, string path, string uiName, ref int missingCount)
        {
            if (parent == null)
            {
                ReportMissing(uiName, ref missingCount);
                return null;
            }

            Transform found = parent.Find(path);
            if (found == null)
            {
                ReportMissing(uiName, ref missingCount);
                return null;
            }

            RectTransform rect = found as RectTransform;
            if (rect == null)
            {
                ReportMissing(uiName, ref missingCount);
                return null;
            }

            return rect;
        }

        private static void ReportMissing(string uiName, ref int missingCount)
        {
            missingCount++;
            Debug.LogError($"Missing UI element: {uiName}");
        }

        private static void SetActive(GameObject target, bool isActive, string undoName)
        {
            if (target == null || target.activeSelf == isActive)
            {
                return;
            }

            Undo.RecordObject(target, $"Apply {undoName}");
            target.SetActive(isActive);
            EditorUtility.SetDirty(target);
        }

        private static void SetAnchored(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            string undoName)
        {
            if (rectTransform == null)
            {
                return;
            }

            Undo.RecordObject(rectTransform, $"Apply {undoName}");
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rectTransform);
        }

        private static void SetStretch(RectTransform rectTransform, float left, float right, float top, float bottom, string undoName)
        {
            if (rectTransform == null)
            {
                return;
            }

            Undo.RecordObject(rectTransform, $"Apply {undoName}");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rectTransform);
        }

        private static void SetTopStretch(RectTransform rectTransform, float left, float right, float top, float height, string undoName)
        {
            if (rectTransform == null)
            {
                return;
            }

            Undo.RecordObject(rectTransform, $"Apply {undoName}");
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -(top + height));
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rectTransform);
        }

        private static void SetBottomStretch(RectTransform rectTransform, float left, float right, float bottom, float height, string undoName)
        {
            if (rectTransform == null)
            {
                return;
            }

            Undo.RecordObject(rectTransform, $"Apply {undoName}");
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, bottom + height);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rectTransform);
        }

        private static void StretchFirstTMPChild(RectTransform parent, string undoName)
        {
            if (parent == null)
            {
                return;
            }

            TMP_Text childText = parent.GetComponentInChildren<TMP_Text>(true);
            if (childText == null)
            {
                return;
            }

            RectTransform textRect = childText.transform as RectTransform;
            if (textRect == null)
            {
                return;
            }

            SetAnchored(textRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, $"{undoName}/Text");
        }

        private static void ApplyImageColor(GameObject target, Color color, string undoName)
        {
            if (target == null)
            {
                return;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            Undo.RecordObject(image, $"Apply {undoName} Color");
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static void ClearAllTexts(RectTransform parent, string undoName)
        {
            if (parent == null)
            {
                return;
            }

            TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                SetTextValue(text, string.Empty, $"{undoName}/Text");
            }
        }

        private static void SetPrimaryText(RectTransform parent, string value, string undoName)
        {
            if (parent == null)
            {
                return;
            }

            TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length == 0)
            {
                return;
            }

            for (int index = 1; index < texts.Length; index++)
            {
                SetTextValue(texts[index], string.Empty, $"{undoName}/ExtraText");
            }

            TMP_Text primary = texts[0];
            SetTextValue(primary, value, $"{undoName}/PrimaryText");

            Undo.RecordObject(primary, $"Apply {undoName} Text Style");
            primary.enableAutoSizing = true;
            primary.fontSizeMin = 10f;
            primary.fontSizeMax = 18f;
            primary.fontSize = 16f;
            primary.enableWordWrapping = false;
            primary.alignment = TextAlignmentOptions.Center;
            primary.color = Color.white;
            EditorUtility.SetDirty(primary);
        }

        private static void ConfigureBoosterBadgeAsLabel(RectTransform badgeRect, string value, string undoName)
        {
            if (badgeRect == null)
            {
                return;
            }

            Image badgeImage = badgeRect.GetComponent<Image>();
            if (badgeImage != null)
            {
                Undo.DestroyObjectImmediate(badgeImage);
            }

            SetStretch(badgeRect, 12f, 12f, 8f, 8f, undoName);

            TextMeshProUGUI label = badgeRect.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = Undo.AddComponent<TextMeshProUGUI>(badgeRect.gameObject);
            }

            SetTextValue(label, "+10 ходов", $"{undoName}/Label");

            Undo.RecordObject(label, $"Apply {undoName} Label Style");
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 18f;
            label.fontSize = 16f;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.overflowMode = TextOverflowModes.Ellipsis;
            EditorUtility.SetDirty(label);
        }

        private static void SetTextValue(TMP_Text text, string value, string undoName)
        {
            if (text == null)
            {
                return;
            }

            Undo.RecordObject(text, $"Apply {undoName}");
            text.text = value;
            EditorUtility.SetDirty(text);
        }

        private static void ConfigureGoalLabelText(RectTransform textRect, string undoName)
        {
            if (textRect == null)
            {
                return;
            }

            TMP_Text text = textRect.GetComponent<TMP_Text>();
            if (text == null)
            {
                return;
            }

            Undo.RecordObject(text, $"Apply {undoName} Style");
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 22f;
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
            text.color = new Color(0.42f, 0.29f, 0.08f, 1f);
            EditorUtility.SetDirty(text);
        }
    }
}
