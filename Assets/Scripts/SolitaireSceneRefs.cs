using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [Serializable]
    public sealed class ColumnRef
    {
        [SerializeField] private Transform root;
        [SerializeField] private GameObject crownSlotLabelBg;
        [SerializeField] private TMP_Text crownSlotLabelText;
        [SerializeField] private Transform crownSlot;
        [SerializeField] private Transform cardsStackRoot;

        public Transform Root => root;
        public GameObject CrownSlotLabelBg => crownSlotLabelBg;
        public TMP_Text CrownSlotLabelText => crownSlotLabelText;
        public Transform CrownSlot => crownSlot;
        public Transform CardsStackRoot => cardsStackRoot;

        public ColumnRef(Transform targetRoot, GameObject targetCrownSlotLabelBg, TMP_Text targetCrownSlotLabelText, Transform targetCrownSlot, Transform targetCardsStackRoot)
        {
            root = targetRoot;
            crownSlotLabelBg = targetCrownSlotLabelBg;
            crownSlotLabelText = targetCrownSlotLabelText;
            crownSlot = targetCrownSlot;
            cardsStackRoot = targetCardsStackRoot;
        }

        public bool Validate(int index)
        {
            bool isValid = true;
            isValid &= SolitaireSceneRefs.ReportMissing(root, $"Columns[{index}].root");
            isValid &= SolitaireSceneRefs.ReportMissing(crownSlotLabelBg, $"Columns[{index}].crownSlotLabelBg");
            isValid &= SolitaireSceneRefs.ReportMissing(crownSlotLabelText, $"Columns[{index}].crownSlotLabelText");
            isValid &= SolitaireSceneRefs.ReportMissing(crownSlot, $"Columns[{index}].crownSlot");
            isValid &= SolitaireSceneRefs.ReportMissing(cardsStackRoot, $"Columns[{index}].cardsStackRoot");
            return isValid;
        }

        public void SetGoalLabel(bool isVisible, string categoryTitle)
        {
            if (crownSlotLabelBg != null)
            {
                if (isVisible)
                {
                    RectTransform labelRect = crownSlotLabelBg.GetComponent<RectTransform>();
                    if (labelRect != null)
                    {
                        labelRect.anchorMin = new Vector2(0.5f, 1f);
                        labelRect.anchorMax = new Vector2(0.5f, 1f);
                        labelRect.pivot = new Vector2(0.5f, 0.5f);
                        labelRect.sizeDelta = new Vector2(148f, 28f);
                        labelRect.anchoredPosition = new Vector2(0f, 16f);
                    }

                    Image labelImage = crownSlotLabelBg.GetComponent<Image>();
                    if (labelImage != null && labelImage.sprite == null)
                    {
                        labelImage.color = new Color(1f, 0.78f, 0.18f, 1f);
                    }

                    crownSlotLabelBg.transform.SetAsLastSibling();
                }

                crownSlotLabelBg.SetActive(isVisible);
            }

            if (crownSlotLabelText != null)
            {
                crownSlotLabelText.alignment = TextAlignmentOptions.Center;
                crownSlotLabelText.enableAutoSizing = true;
                crownSlotLabelText.fontSizeMin = 11f;
                crownSlotLabelText.fontSizeMax = 15f;
                crownSlotLabelText.enableWordWrapping = false;
                crownSlotLabelText.color = new Color(0.36f, 0.23f, 0.06f, 1f);
                crownSlotLabelText.text = isVisible ? categoryTitle : string.Empty;
            }

            if (isVisible)
            {
                Debug.Log($"SolitaireSceneRefs: Goal label set to '{categoryTitle}'.");
            }
        }
    }

    [ExecuteAlways]
    public sealed class SolitaireSceneRefs : MonoBehaviour
    {
        private const int MaxSupportedColumns = 5;
        private const float CrownSlotWidth = 148f;
        private const float CrownSlotHeight = 190f;
        private const float CrownSlotAnchoredY = -126f;
        private const float CrownToCardsGap = 6f;
        private const float CardsStackHeight = 520f;

        [Header("Roots")]
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform columnSlotsRoot;
        [SerializeField] private RectTransform deckCardBack;

        [Header("HUD")]
        [SerializeField] private TMP_Text movesValue;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text deckCounter;
        [SerializeField] private GameObject badge;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button rewardBonusButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button boosterDeckButton;

        [Header("Columns")]
        [SerializeField] private List<ColumnRef> columns = new();

        public RectTransform SafeAreaRoot => safeAreaRoot;
        public RectTransform ColumnSlotsRoot => columnSlotsRoot;
        public RectTransform DeckCardBack => deckCardBack;
        public TMP_Text MovesValue => movesValue;
        public TMP_Text CoinsText => coinsText;
        public TMP_Text DeckCounter => deckCounter;
        public GameObject Badge => badge;
        public Button MenuButton => menuButton;
        public Button PlusButton => plusButton;
        public Button RewardBonusButton => rewardBonusButton;
        public Button HintButton => hintButton;
        public Button UndoButton => undoButton;
        public Button BoosterDeckButton => boosterDeckButton;
        public IReadOnlyList<ColumnRef> Columns => columns;

        private void OnEnable()
        {
            AutoAssign();
            ApplyEditorPreviewColumns();
        }

        private void OnValidate()
        {
            AutoAssign();
            ApplyEditorPreviewColumns();
        }

        public void NormalizeSceneLayout()
        {
            AutoAssign();
        }

        public bool ResolveSceneReferences()
        {
            AutoAssign();

            bool isValid = true;
            isValid &= ReportMissing(safeAreaRoot, "SafeAreaRoot");
            isValid &= ReportMissing(movesValue, "MovesValue");
            isValid &= ReportMissing(coinsText, "CoinsText");
            isValid &= ReportMissing(deckCounter, "DeckCounter");
            isValid &= ReportMissing(columnSlotsRoot, "ColumnSlotsRoot");
            isValid &= ReportMissing(deckCardBack, "DeckCardBack");
            isValid &= ReportMissing(menuButton, "MenuButton");
            isValid &= ReportMissing(plusButton, "PlusButton");
            isValid &= ReportMissing(hintButton, "HintButton");
            isValid &= ReportMissing(undoButton, "UndoButton");
            isValid &= ReportMissing(badge, "Badge");

            if (columns == null || columns.Count == 0)
            {
                Debug.LogError("Missing reference: columns");
                isValid = false;
            }
            else
            {
                for (int index = 0; index < columns.Count; index++)
                {
                    ColumnRef column = columns[index];
                    if (column == null)
                    {
                        Debug.LogError($"Missing reference: Columns[{index}]");
                        isValid = false;
                        continue;
                    }

                    isValid &= column.Validate(index);
                }
            }

            if (isValid)
            {
                Debug.Log(
                    $"SolitaireSceneRefs: Found scene refs. " +
                    $"Columns={columns.Count}, " +
                    $"SafeAreaRoot={GetPath(safeAreaRoot)}, " +
                    $"ColumnSlotsRoot={GetPath(columnSlotsRoot)}");
            }

            return isValid;
        }

        public void HideAllGoalLabels()
        {
            if (columns == null)
            {
                return;
            }

            foreach (ColumnRef column in columns)
            {
                column?.SetGoalLabel(false, string.Empty);
            }
        }

        public static bool ReportMissing(UnityEngine.Object value, string referenceName)
        {
            if (value != null)
            {
                return true;
            }

            Debug.LogError($"Missing reference: {referenceName}");
            return false;
        }

        public static string GetPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            Stack<string> path = new();
            Transform current = target;
            while (current != null)
            {
                path.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }

        private void AutoAssign()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            Transform searchRoot = safeAreaRoot != null
                ? safeAreaRoot
                : canvas != null
                    ? canvas.transform
                    : transform;

            safeAreaRoot ??= FindChildRecursive<RectTransform>(searchRoot, "SafeAreaRoot");
            columnSlotsRoot ??= FindChildRecursive<RectTransform>(safeAreaRoot, "ColumnSlotsRoot");
            deckCardBack ??= FindChildRecursive<RectTransform>(safeAreaRoot, "DeckCardBack");

            movesValue ??= FindChildRecursive<TMP_Text>(safeAreaRoot, "MovesValue");
            coinsText ??= FindChildRecursive<TMP_Text>(safeAreaRoot, "CoinsText");
            deckCounter ??= FindChildRecursive<TMP_Text>(safeAreaRoot, "DeckCounter");

            badge ??= FindChildRecursive<Transform>(safeAreaRoot, "Badge")?.gameObject;

            menuButton ??= FindOrAddButton(safeAreaRoot, "MenuButton");
            plusButton ??= FindOrAddButton(safeAreaRoot, "PlusButton");
            rewardBonusButton ??= FindOrAddButton(safeAreaRoot, "RewardBonusButton");
            hintButton ??= FindOrAddButton(safeAreaRoot, "HintButton");
            undoButton ??= FindOrAddButton(safeAreaRoot, "UndoButton");
            boosterDeckButton ??= FindOrAddButton(safeAreaRoot, "BoosterDeckButton");

            int supportedSceneColumnsCount = CountSupportedSceneColumns(columnSlotsRoot);
            if (columns == null || columns.Count == 0 || columns.Count > MaxSupportedColumns || HasInvalidColumns() || (supportedSceneColumnsCount > 0 && columns.Count != supportedSceneColumnsCount))
            {
                columns = BuildColumns(columnSlotsRoot);
            }
        }

        private void ApplyEditorPreviewColumns()
        {
            if (Application.isPlaying || columnSlotsRoot == null || columns == null || columns.Count == 0)
            {
                return;
            }

            int previewColumnsCount = Mathf.Clamp(Mathf.Min(MaxSupportedColumns, columns.Count), 3, MaxSupportedColumns);
            ColumnLayoutProfile profile = GameLayoutConfig.CreateDefault().GetProfile(previewColumnsCount);

            HorizontalLayoutGroup layoutGroup = columnSlotsRoot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = profile.columnSpacing;
            }

            for (int index = 0; index < columns.Count; index++)
            {
                ColumnRef column = columns[index];
                if (column?.Root == null)
                {
                    continue;
                }

                column.Root.gameObject.SetActive(index < previewColumnsCount);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(columnSlotsRoot);
        }

        private void EnsureCanvasSetup()
        {
            Canvas canvas = safeAreaRoot != null ? safeAreaRoot.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
        }

        private void ApplyReferenceLayout()
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            RectTransform canvasRect = safeAreaRoot.parent as RectTransform;
            if (canvasRect != null)
            {
                StretchToFill(safeAreaRoot, 0f, 0f, 0f, 0f);
            }

            RectTransform background = FindChildRecursive<RectTransform>(safeAreaRoot, "Background");
            if (background != null)
            {
                StretchToFill(background, 0f, 0f, 0f, 0f);
                Image backgroundImage = EnsureGraphic<Image>(background.gameObject);
                if (backgroundImage.sprite == null)
                {
                    backgroundImage.color = new Color(0.09f, 0.63f, 0.29f, 1f);
                }
            }

            ApplyTopHudLayout();
            ApplyMovesPanelLayout();
            ApplyDeckAreaLayout();
            ApplyBoardLayout();
            ApplyBottomHudLayout();
            ApplyAdBannerLayout();
        }

        private void ApplyTopHudLayout()
        {
            RectTransform topHud = FindChildRecursive<RectTransform>(safeAreaRoot, "TopHUD");
            if (topHud == null)
            {
                return;
            }

            SetAnchored(topHud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 180f), new Vector2(0f, -24f));

            RectTransform menuRoot = menuButton != null ? menuButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(topHud, "MenuButton");
            if (menuRoot != null)
            {
                SetAnchored(menuRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), new Vector2(72f, -68f));
                Image menuImage = EnsureGraphic<Image>(menuRoot.gameObject);
                if (menuImage.sprite == null)
                {
                    menuImage.color = new Color(0.17f, 0.39f, 0.2f, 0.95f);
                }
                StretchChildText(menuRoot);
            }

            RectTransform coinsPanel = FindChildRecursive<RectTransform>(topHud, "CoinsPanel");
            if (coinsPanel != null)
            {
                SetAnchored(coinsPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(280f, 88f), new Vector2(260f, -68f));
                Image panelImage = EnsureGraphic<Image>(coinsPanel.gameObject);
                if (panelImage.sprite == null)
                {
                    panelImage.color = new Color(0.18f, 0.28f, 0.18f, 0.95f);
                }

                RectTransform coinIcon = FindChildRecursive<RectTransform>(coinsPanel, "CoinIcon");
                if (coinIcon != null)
                {
                    SetAnchored(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 56f), new Vector2(34f, 0f));
                    Image coinImage = EnsureGraphic<Image>(coinIcon.gameObject);
                    if (coinImage.sprite == null)
                    {
                        coinImage.color = new Color(0.98f, 0.84f, 0.2f, 1f);
                    }
                }

                if (coinsText != null)
                {
                    SetAnchored((RectTransform)coinsText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 44f), new Vector2(-10f, 0f));
                }

                RectTransform plusRoot = plusButton != null ? plusButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(coinsPanel, "PlusButton");
                if (plusRoot != null)
                {
                    SetAnchored(plusRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54f, 54f), new Vector2(-28f, 0f));
                    Image plusImage = EnsureGraphic<Image>(plusRoot.gameObject);
                    if (plusImage.sprite == null)
                    {
                        plusImage.color = new Color(0.93f, 0.93f, 0.93f, 1f);
                    }
                    StretchChildText(plusRoot);
                }
            }

            RectTransform rewardRoot = rewardBonusButton != null ? rewardBonusButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(topHud, "RewardBonusButton");
            if (rewardRoot != null)
            {
                SetAnchored(rewardRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(170f, 88f), new Vector2(-110f, -68f));
                Image rewardImage = EnsureGraphic<Image>(rewardRoot.gameObject);
                if (rewardImage.sprite == null)
                {
                    rewardImage.color = new Color(0.97f, 0.93f, 0.83f, 1f);
                }
            }
        }

        private void ApplyMovesPanelLayout()
        {
            RectTransform movesPanel = FindChildRecursive<RectTransform>(safeAreaRoot, "MovesPanel");
            if (movesPanel == null)
            {
                return;
            }

            SetAnchored(movesPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(220f, 260f), new Vector2(140f, -320f));
            Image panelImage = EnsureGraphic<Image>(movesPanel.gameObject);
            if (panelImage.sprite == null)
            {
                panelImage.color = new Color(0.24f, 0.85f, 0.52f, 1f);
            }

            RectTransform movesLabelRoot = FindChildRecursive<RectTransform>(movesPanel, "MovesLabel");
            if (movesLabelRoot != null)
            {
                SetAnchored(movesLabelRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(160f, 42f), new Vector2(0f, -36f));
            }

            if (movesValue != null)
            {
                SetAnchored((RectTransform)movesValue.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 110f), new Vector2(0f, -18f));
            }
        }

        private void ApplyDeckAreaLayout()
        {
            RectTransform deckArea = FindChildRecursive<RectTransform>(safeAreaRoot, "DeckArea");
            if (deckArea == null)
            {
                return;
            }

            SetAnchored(deckArea, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(180f, 240f), new Vector2(-110f, -320f));
            Image deckAreaImage = EnsureGraphic<Image>(deckArea.gameObject);
            if (deckAreaImage.sprite == null)
            {
                deckAreaImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (deckCardBack != null)
            {
                SetAnchored(deckCardBack, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(148f, 200f), new Vector2(0f, 0f));
                EnsureLayoutElement(deckCardBack.gameObject, 148f, 200f);
                Image deckBackImage = EnsureGraphic<Image>(deckCardBack.gameObject);
                if (deckBackImage.sprite == null)
                {
                    deckBackImage.color = new Color(0.2f, 0.62f, 0.98f, 1f);
                }
            }

            if (deckCounter != null)
            {
                SetAnchored((RectTransform)deckCounter.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(96f, 52f), new Vector2(-40f, -34f));
                deckCounter.enableWordWrapping = false;
                deckCounter.enableAutoSizing = true;
                deckCounter.fontSizeMin = 22f;
                deckCounter.fontSizeMax = 36f;
                deckCounter.overflowMode = TextOverflowModes.Overflow;
                deckCounter.alignment = TextAlignmentOptions.Center;
            }
        }

        private void ApplyBoardLayout()
        {
            RectTransform boardArea = FindChildRecursive<RectTransform>(safeAreaRoot, "BoardArea");
            if (boardArea == null || columnSlotsRoot == null)
            {
                return;
            }

            StretchToFill(boardArea, 120f, 120f, 470f, 340f);
            Image boardImage = EnsureGraphic<Image>(boardArea.gameObject);
            if (boardImage.sprite == null)
            {
                boardImage.color = new Color(1f, 1f, 1f, 0f);
            }
            StretchToFill(columnSlotsRoot, 0f, 0f, 0f, 0f);

            HorizontalLayoutGroup layoutGroup = EnsureComponent<HorizontalLayoutGroup>(columnSlotsRoot.gameObject);
            layoutGroup.spacing = 24f;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            foreach (ColumnRef column in columns)
            {
                if (column?.Root == null)
                {
                    continue;
                }

                RectTransform columnRoot = column.Root as RectTransform;
                if (columnRoot == null)
                {
                    continue;
                }

                EnsureLayoutElement(columnRoot.gameObject, 148f, 760f);
                SetAnchored(columnRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(148f, 760f), Vector2.zero);
                Image columnImage = EnsureGraphic<Image>(columnRoot.gameObject);
                if (columnImage.sprite == null)
                {
                    columnImage.color = new Color(1f, 1f, 1f, 0f);
                }

                if (column.CrownSlotLabelBg != null)
                {
                    RectTransform labelBg = column.CrownSlotLabelBg.GetComponent<RectTransform>();
                    if (labelBg != null)
                    {
                        SetAnchored(labelBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(132f, 34f), new Vector2(0f, -18f));
                    }

                    Image labelImage = EnsureGraphic<Image>(column.CrownSlotLabelBg);
                    if (labelImage.sprite == null)
                    {
                        labelImage.color = new Color(0.95f, 0.79f, 0.23f, 1f);
                    }
                }

                if (column.CrownSlotLabelText != null)
                {
                    SetAnchored((RectTransform)column.CrownSlotLabelText.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), Vector2.zero);
                }

                if (column.CrownSlot is RectTransform crownSlotRect)
                {
                    SetAnchored(
                        crownSlotRect,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(CrownSlotWidth, CrownSlotHeight),
                        new Vector2(0f, CrownSlotAnchoredY));
                    EnsureLayoutElement(crownSlotRect.gameObject, CrownSlotWidth, CrownSlotHeight);
                    Image crownSlotImage = EnsureGraphic<Image>(crownSlotRect.gameObject);
                    if (crownSlotImage.sprite == null)
                    {
                        crownSlotImage.color = new Color(1f, 1f, 1f, 0.18f);
                    }
                }

                if (column.CardsStackRoot is RectTransform cardsStackRect)
                {
                    float cardsStackAnchoredY = CrownSlotAnchoredY - (CrownSlotHeight * 0.5f) - CrownToCardsGap;
                    SetAnchored(
                        cardsStackRect,
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(0.5f, 1f),
                        new Vector2(CrownSlotWidth, CardsStackHeight),
                        new Vector2(0f, cardsStackAnchoredY));
                }
            }
        }

        private void ApplyBottomHudLayout()
        {
            RectTransform bottomHud = FindChildRecursive<RectTransform>(safeAreaRoot, "BottomHUD");
            if (bottomHud == null)
            {
                return;
            }

            SetAnchored(bottomHud, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(0f, 110f));
            Image bottomHudImage = EnsureGraphic<Image>(bottomHud.gameObject);
            if (bottomHudImage.sprite == null)
            {
                bottomHudImage.color = new Color(1f, 1f, 1f, 0f);
            }

            RectTransform hintRoot = hintButton != null ? hintButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(bottomHud, "HintButton");
            if (hintRoot != null)
            {
                SetAnchored(hintRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, 170f), new Vector2(-250f, 0f));
                Image hintImage = EnsureGraphic<Image>(hintRoot.gameObject);
                if (hintImage.sprite == null)
                {
                    hintImage.color = new Color(0.47f, 0.64f, 1f, 1f);
                }
                StretchChildText(hintRoot);
            }

            RectTransform undoRoot = undoButton != null ? undoButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(bottomHud, "UndoButton");
            if (undoRoot != null)
            {
                SetAnchored(undoRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, 170f), Vector2.zero);
                Image undoImage = EnsureGraphic<Image>(undoRoot.gameObject);
                if (undoImage.sprite == null)
                {
                    undoImage.color = new Color(0.47f, 0.64f, 1f, 1f);
                }
                StretchChildText(undoRoot);
            }

            RectTransform boosterRoot = boosterDeckButton != null ? boosterDeckButton.GetComponent<RectTransform>() : FindChildRecursive<RectTransform>(bottomHud, "BoosterDeckButton");
            if (boosterRoot != null)
            {
                SetAnchored(boosterRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, 170f), new Vector2(250f, 0f));
                Image boosterImage = EnsureGraphic<Image>(boosterRoot.gameObject);
                if (boosterImage.sprite == null)
                {
                    boosterImage.color = new Color(0.98f, 0.83f, 0.19f, 1f);
                }
            }

            if (badge != null)
            {
                RectTransform badgeRect = badge.GetComponent<RectTransform>();
                if (badgeRect != null)
                {
                    SetAnchored(badgeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(48f, 48f), new Vector2(-12f, -12f));
                    Image badgeImage = badge.GetComponent<Image>();
                    if (badgeImage != null && badgeImage.sprite == null)
                    {
                        badgeImage.color = new Color(0.91f, 0.19f, 0.2f, 1f);
                    }
                }
            }
        }

        private void ApplyAdBannerLayout()
        {
            RectTransform adBanner = FindChildRecursive<RectTransform>(safeAreaRoot, "AdBannerPlaceholder");
            if (adBanner == null)
            {
                return;
            }

            SetAnchored(adBanner, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), Vector2.zero);
            Image image = EnsureGraphic<Image>(adBanner.gameObject);
            image.color = new Color(0f, 0f, 0f, 0.12f);
            image.enabled = true;
            image.raycastTarget = false;
        }

        private bool HasInvalidColumns()
        {
            foreach (ColumnRef column in columns)
            {
                if (column == null || column.Root == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<ColumnRef> BuildColumns(Transform columnRoot)
        {
            List<ColumnRef> result = new();
            if (columnRoot == null)
            {
                return result;
            }

            List<Transform> columnTransforms = new();
            foreach (Transform child in columnRoot)
            {
                if (child.name.StartsWith("Column_", StringComparison.Ordinal))
                {
                    columnTransforms.Add(child);
                }
            }

            columnTransforms.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

            for (int index = 0; index < columnTransforms.Count; index++)
            {
                Transform columnTransform = columnTransforms[index];
                bool isSupported = index < MaxSupportedColumns;
                columnTransform.gameObject.SetActive(isSupported);

                if (!isSupported)
                {
                    continue;
                }

                result.Add(new ColumnRef(
                    columnTransform,
                    FindChildRecursive<Transform>(columnTransform, "CrownSlotLabelBg")?.gameObject,
                    FindChildRecursive<TMP_Text>(columnTransform, "CrownSlotLabelText"),
                    FindChildRecursive<Transform>(columnTransform, "CrownSlot"),
                    FindChildRecursive<Transform>(columnTransform, "CardsStackRoot")));
            }

            return result;
        }

        private static int CountSupportedSceneColumns(Transform columnRoot)
        {
            if (columnRoot == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Transform child in columnRoot)
            {
                if (child.name.StartsWith("Column_", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return Mathf.Min(count, MaxSupportedColumns);
        }

        private static Button FindOrAddButton(Transform root, string objectName)
        {
            Transform target = FindChildRecursive<Transform>(root, objectName);
            if (target == null)
            {
                return null;
            }

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                graphic = target.gameObject.AddComponent<Image>();
                graphic.color = new Color(1f, 1f, 1f, 0f);
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.gameObject.AddComponent<Button>();
            }

            if (button.targetGraphic == null)
            {
                button.targetGraphic = graphic;
            }

            return button;
        }

        private static T FindChildRecursive<T>(Transform root, string objectName) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    continue;
                }

                return child.GetComponent<T>();
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static T EnsureGraphic<T>(GameObject gameObject) where T : Graphic
        {
            T graphic = gameObject.GetComponent<T>();
            if (graphic == null)
            {
                graphic = gameObject.AddComponent<T>();
            }

            return graphic;
        }

        private static LayoutElement EnsureLayoutElement(GameObject gameObject, float width, float height)
        {
            LayoutElement layoutElement = EnsureComponent<LayoutElement>(gameObject);
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;
            return layoutElement;
        }

        private static void StretchChildText(RectTransform parent)
        {
            TMP_Text text = parent.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                return;
            }

            SetAnchored((RectTransform)text.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void StretchToFill(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static void SetAnchored(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
        }
    }
}
