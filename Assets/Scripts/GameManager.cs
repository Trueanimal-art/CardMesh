using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class GameManager : MonoBehaviour
    {
        private const float LooseGoalDropPadding = 84f;
        private const float LooseColumnDropPadding = 76f;
        private const int HintCost = 900;
        private const float HintMoveDuration = 0.68f;
        private const float HintHoldDuration = 0.42f;
        private const float HintLiftOffset = 42f;
        private const float HintColumnDropOffset = 58f;
        private const float HintDeckDuration = 1f;
        private const float DragStackDefaultPeekHeight = 40f;
        private const float DragStackPeekPadding = 4f;
        private const float DragStackMaskOverflowHeight = 14f;
        private const float GoalCollapseDuration = 0.24f;
        private const float GoalCloudDuration = 0.22f;
        private const float RestartLifeSpendLockSeconds = 0.75f;
        private const float TouchDragLiftReferencePixels = 72f;
        private const int FirstManifestLevelNumber = 1;

        public readonly struct ObjectiveState
        {
            public ObjectiveState(string associationId, string title, int currentCount, int requiredCount, bool isComplete)
            {
                AssociationId = associationId;
                Title = title;
                CurrentCount = currentCount;
                RequiredCount = requiredCount;
                IsComplete = isComplete;
            }

            public string AssociationId { get; }
            public string Title { get; }
            public int CurrentCount { get; }
            public int RequiredCount { get; }
            public bool IsComplete { get; }
        }

        private enum HintKind
        {
            MoveToGoal,
            RevealCard,
            MoveBetweenColumns,
            OtherMove,
            DrawDeck
        }

        private sealed class HintMove
        {
            public HintMove(CardView card, ICardContainer targetContainer, HintKind kind, int priority, List<CardView> movingStack)
            {
                Card = card;
                TargetContainer = targetContainer;
                Kind = kind;
                Priority = priority;
                MovingStack = movingStack != null && movingStack.Count > 0
                    ? new List<CardView>(movingStack)
                    : new List<CardView> { card };
            }

            public CardView Card { get; }
            public ICardContainer TargetContainer { get; }
            public HintKind Kind { get; }
            public int Priority { get; }
            public IReadOnlyList<CardView> MovingStack { get; }
        }

        private readonly struct HintCardTransform
        {
            public HintCardTransform(
                Transform parent,
                int siblingIndex,
                Vector2 anchoredPosition,
                Vector3 scale,
                Quaternion rotation)
            {
                Parent = parent;
                SiblingIndex = siblingIndex;
                AnchoredPosition = anchoredPosition;
                Scale = scale;
                Rotation = rotation;
            }

            public Transform Parent { get; }
            public int SiblingIndex { get; }
            public Vector2 AnchoredPosition { get; }
            public Vector3 Scale { get; }
            public Quaternion Rotation { get; }
        }

        [SerializeField] private UIManager uiManager;
        [SerializeField] private PlatformService platformService;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private RectTransform columnsContainer;
        [SerializeField] private HorizontalLayoutGroup columnsLayoutGroup;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private List<CardColumn> columns = new();
        [SerializeField] private List<GoalSlot> goalSlots = new();
        [SerializeField] private DrawSlot drawSlot;
        [SerializeField] private GameLayoutConfig layoutConfig = new();
        [SerializeField] private CardVisualConfig visualConfig;
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private SolitaireSceneRefs sceneRefs;
        [SerializeField] private LevelProgressionManager progressionManager;
        [SerializeField] private WinScreenView winScreenView;
        [SerializeField] private CoinsManager coinsManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private PlayerBoostInventory boostInventory;
        [SerializeField] private ChestChaseManager chestChaseManager;
        [SerializeField] private AdsManager adsManager;
        [SerializeField, Min(1)] private int rewardedAddMovesAmount = 10;

        [Header("Image Cards")]
        [SerializeField] private Texture2D imageCardsAtlas;

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float moveDuration = 0.18f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float shakeStrength = 16f;
        [SerializeField] private Color highlightColor = new(1f, 0.92f, 0.34f, 0.45f);
        [SerializeField, Min(0.5f)] private float levelCompleteDelay = 1.75f;
        [SerializeField, Min(0f)] private float movesBonusDelay = 0.4f;

        private readonly Dictionary<string, CardData> cardsById = new();
        private readonly Dictionary<string, CardData> goalCardsByAssociation = new();
        private readonly HashSet<string> completedAssociations = new();
        private readonly List<CardView> spawnedCards = new();
        private readonly List<CardView> draggedStack = new();
        private readonly Dictionary<CardView, Vector3> draggedStackOffsets = new();
        private readonly List<ObjectiveState> objectiveStates = new();
        private readonly List<string> objectiveOrder = new();
        private readonly Stack<BoardSnapshot> moveHistory = new();

        private CardView draggedCard;
        private CardView touchSelectedCard;
        private int activePointerId = int.MinValue;
        private GeneratedLevelData generatedLevelData;
        private bool dragWasResolved;
        private int remainingMoves;
        private int completedUsedMovesCount;
        private bool levelFinished;
        private bool levelCompletionInProgress;
        private bool dependenciesInitialized;
        private bool hintAnimationInProgress;
        private bool rewardedAddMovesInProgress;
        private int? devOverrideTargetColumns;
        private int? devOverrideLevelNumber;
        private int? devOverrideSeed;
        private bool restartInProgress;
        private bool restartConfirmationOpen;
        private int lastRestartLifeSpendFrame = -1;
        private float lastRestartLifeSpendRealtime = -RestartLifeSpendLockSeconds;
        private Coroutine restartLockReleaseRoutine;
        private Coroutine levelCompleteRoutine;
        private Coroutine levelRewardRoutine;

        private sealed class CardSnapshot
        {
            public bool IsActive { get; set; }
            public bool IsFaceUp { get; set; }
            public bool IsPlayerStackedOnPrevious { get; set; }
        }

        private sealed class BoardSnapshot
        {
            public List<List<CardView>> Columns { get; } = new();
            public List<List<CardView>> GoalSlots { get; } = new();
            public List<CardView> QueuedDrawCards { get; set; } = new();
            public List<CardView> RevealedDrawCards { get; set; } = new();
            public HashSet<string> CompletedAssociations { get; set; } = new();
            public Dictionary<CardView, CardSnapshot> CardStates { get; } = new();
            public int RemainingMoves { get; set; }
        }

        public LevelDefinition AssignedLevelDefinition => levelDefinition;
        public LivesManager LivesManager => livesManager;
        public float MoveDuration => moveDuration;
        public float ShakeDuration => shakeDuration;
        public float ShakeStrength => shakeStrength;
        public Color HighlightColor => highlightColor;
        public bool HasPresentationBindings =>
            uiManager != null &&
            dragLayer != null &&
            cardPrefab != null &&
            columns.Count > 0 &&
            goalSlots.Count > 0 &&
            drawSlot != null;

        public ColumnLayoutProfile GetLayoutProfileForPreview(int targetColumnCount)
        {
            return GetGameplayLayoutProfile(targetColumnCount);
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name == "Main")
            {
                RuntimeSceneBootstrapper.ResetMainScenePlayState();
            }

            InitializeDependencies();
        }

        private void Start()
        {
            if (!HasPresentationBindings)
            {
                RuntimeSceneBootstrapper.BindManualScene(this);
            }

            if (HasPresentationBindings && levelDefinition != null)
            {
                RestartLevel();
            }
        }

        public void Configure(
            UIManager targetUiManager,
            PlatformService targetPlatformService,
            RectTransform targetDragLayer,
            RectTransform targetColumnsContainer,
            HorizontalLayoutGroup targetColumnsLayoutGroup,
            CardView targetCardPrefab,
            List<CardColumn> targetColumns,
            List<GoalSlot> targetGoalSlots,
            DrawSlot targetDrawSlot,
            GameLayoutConfig targetLayoutConfig,
            CardDatabase targetCardDatabase,
            LevelDefinition targetLevelDefinition,
            SolitaireSceneRefs targetSceneRefs)
        {
            uiManager = targetUiManager;
            platformService = targetPlatformService;
            dragLayer = targetDragLayer;
            columnsContainer = targetColumnsContainer;
            columnsLayoutGroup = targetColumnsLayoutGroup;
            cardPrefab = targetCardPrefab;
            columns = targetColumns ?? new List<CardColumn>();
            goalSlots = targetGoalSlots ?? new List<GoalSlot>();
            drawSlot = targetDrawSlot;
            layoutConfig = targetLayoutConfig ?? layoutConfig ?? GameLayoutConfig.CreateDefault();
            cardDatabase = targetCardDatabase ?? cardDatabase;
            sceneRefs = targetSceneRefs;

            if (targetLevelDefinition != null)
            {
                levelDefinition = targetLevelDefinition;
            }

            dependenciesInitialized = false;
            InitializeDependencies();
        }

        public void SetLevelDefinition(LevelDefinition targetLevelDefinition)
        {
            levelDefinition = targetLevelDefinition;
        }

        public void DrawNextStockCard()
        {
            if (levelFinished || drawSlot == null || !drawSlot.HasDrawableCards)
            {
                return;
            }

            BoardSnapshot snapshot = CaptureBoardSnapshot();
            if (!drawSlot.DrawNextCard())
            {
                return;
            }

            PushMoveSnapshot(snapshot);
            remainingMoves = Mathf.Max(0, remainingMoves - 1);
            UpdateMovesDisplay();
            platformService?.TrackEvent("stock_draw");
            ResolveBoardState(true);
        }

        public void UseHintForTesting()
        {
            TryUseHint(consumeCharge: true);
        }

        public void UseFreeHintForDevTools()
        {
            TryUseHint(consumeCharge: false);
        }

        private void TryUseHint(bool consumeCharge)
        {
            if (levelFinished)
            {
                Debug.Log("Hint: ignored because level is finished.");
                return;
            }

            if (hintAnimationInProgress || draggedCard != null)
            {
                Debug.Log("Hint: ignored because another hint or drag is active.");
                return;
            }

            if (consumeCharge && !TrySpendHintCharge())
            {
                uiManager?.ShowStatus("Недостаточно подсказок", new Color(1f, 0.82f, 0.52f, 1f));
                return;
            }

            Debug.Log(consumeCharge ? "Hint: started." : "Hint: started by dev tools.");
            HintMove hintMove = FindBestHintMove();
            if (hintMove == null)
            {
                Debug.Log("Hint: no moves available, pointing to deck.");
                StartCoroutine(PlayDeckHintAnimation());
                return;
            }

            Debug.Log(BuildHintFoundLog(hintMove));
            StartCoroutine(PlayMoveHintAnimation(hintMove));
        }

        private bool CanUseHint()
        {
            return coinsManager == null || coinsManager.CurrentCoins >= HintCost;
        }

        private bool SpendHintCost()
        {
            return coinsManager == null || coinsManager.TrySpendCoins(HintCost);
        }

        private bool TrySpendHintCharge()
        {
            // Gameplay consumes chest rewards first; coin fallback keeps the old paid-hint architecture intact.
            if (boostInventory != null && boostInventory.TrySpendHint())
            {
                uiManager?.UpdateHintCount(boostInventory.HintCount);
                return true;
            }

            bool spentCoins = SpendHintCost();
            if (spentCoins && coinsManager != null)
            {
                uiManager?.UpdateCoins(coinsManager.CurrentCoins);
            }

            return spentCoins;
        }

        public void TryUseUndo()
        {
            TryRestoreLastMove(
                consumeCharge: true,
                successMessage: "Ход отменен",
                analyticsEvent: "undo_used");
        }

        public bool TryUseUndoForDevTools()
        {
            return TryRestoreLastMove(
                consumeCharge: false,
                successMessage: "Ход отменен (DEV)",
                analyticsEvent: "dev_undo_used");
        }

        private bool TryRestoreLastMove(bool consumeCharge, string successMessage, string analyticsEvent)
        {
            if (levelFinished)
            {
                uiManager?.ShowStatus("Уровень уже завершен", new Color(1f, 0.82f, 0.52f, 1f));
                return false;
            }

            if (hintAnimationInProgress || draggedCard != null)
            {
                uiManager?.ShowStatus("Подождите завершения действия", new Color(1f, 0.82f, 0.52f, 1f));
                return false;
            }

            if (moveHistory.Count <= 0)
            {
                uiManager?.ShowStatus("Нечего отменять", new Color(1f, 0.82f, 0.52f, 1f));
                return false;
            }

            if (consumeCharge && (boostInventory == null || boostInventory.UndoCount <= 0))
            {
                uiManager?.ShowStatus("Нет отмен ходов", new Color(1f, 0.82f, 0.52f, 1f));
                return false;
            }

            BoardSnapshot snapshot = moveHistory.Pop();
            if (!RestoreBoardSnapshot(snapshot))
            {
                uiManager?.ShowStatus("Не удалось отменить ход", new Color(1f, 0.82f, 0.52f, 1f));
                return false;
            }

            if (consumeCharge && !boostInventory.TrySpendUndo())
            {
                return false;
            }

            if (consumeCharge)
            {
                uiManager?.UpdateUndoCount(boostInventory.UndoCount);
            }

            uiManager?.ShowStatus(successMessage, new Color(0.76f, 0.96f, 0.74f, 1f));
            platformService?.TrackEvent(analyticsEvent);
            Debug.Log(consumeCharge ? "Undo: restored previous move." : "Undo: restored previous move by dev tools.");
            return true;
        }

        public bool BeginDrag(CardView cardView, int pointerId)
        {
            if (cardView == null || levelFinished || draggedCard != null)
            {
                return false;
            }

            if (cardView.Container is GoalSlot)
            {
                return false;
            }

            List<CardView> stack = BuildDragStack(cardView);
            if (stack.Count == 0)
            {
                return false;
            }

            ClearTouchSelection();
            draggedCard = cardView;
            activePointerId = pointerId;
            draggedStack.Clear();
            draggedStack.AddRange(stack);
            dragWasResolved = false;
            return true;
        }

        public bool IsDragging(CardView cardView, int pointerId)
        {
            return draggedCard == cardView && activePointerId == pointerId;
        }

        public void BeginDragVisual(CardView cardView)
        {
            if (draggedCard != cardView || dragLayer == null)
            {
                return;
            }

            ICardContainer sourceContainer = cardView.Container;
            draggedStackOffsets.Clear();
            Vector3 anchorPosition = cardView.RectTransform.position;

            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard == null)
                {
                    continue;
                }

                draggedStackOffsets[stackCard] = stackCard.RectTransform.position - anchorPosition;
                stackCard.SetRaycastBlocks(false);
                stackCard.SetDragDetached(true);
                stackCard.SetDraggingVisual(true);
                ApplyDragStackTitleLayout(draggedStack, stackCard);
                stackCard.transform.SetParent(dragLayer, true);
                stackCard.transform.SetAsLastSibling();
            }

            ApplyDraggedStackProgressFooter();

            if (sourceContainer is CardColumn sourceColumn)
            {
                sourceColumn.RefreshLayoutForDragPreview();
            }
            else
            {
                sourceContainer?.RefreshLayout();
            }
        }

        public void UpdateDragVisual(CardView cardView, PointerEventData eventData)
        {
            if (draggedCard != cardView)
            {
                return;
            }

            Vector3 anchorPosition = eventData != null
                ? (Vector3)(eventData.position + GetTouchDragLift(eventData.pointerId))
                : cardView.RectTransform.position;

            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard == null)
                {
                    continue;
                }

                Vector3 offset = draggedStackOffsets.TryGetValue(stackCard, out Vector3 storedOffset)
                    ? storedOffset
                    : Vector3.zero;
                stackCard.RectTransform.position = anchorPosition + offset;
            }
        }

        public void EndDrag(CardView cardView, PointerEventData eventData)
        {
            if (draggedCard != cardView
                || (eventData != null && activePointerId != eventData.pointerId))
            {
                return;
            }

            bool resolved = dragWasResolved;
            if (!dragWasResolved)
            {
                Vector2 screenPoint = eventData != null
                    ? eventData.position
                    : RectTransformUtility.WorldToScreenPoint(null, cardView.RectTransform.position);
                Camera eventCamera = eventData != null ? eventData.pressEventCamera : null;

                resolved = TryResolveLooseDrop(cardView, screenPoint, eventCamera);
            }

            FinishDragVisual();
            if (!resolved && cardView.Container != null)
            {
                cardView.Container.RefreshLayout();
                PlayInvalidMoveFeedback(cardView);
            }

            draggedCard = null;
            activePointerId = int.MinValue;
            dragWasResolved = false;
            draggedStack.Clear();
            draggedStackOffsets.Clear();
            RestoreGameplayCardInteractivity();
        }

        public void HandleCardTap(CardView cardView, int clickCount)
        {
            if (cardView == null
                || levelFinished
                || hintAnimationInProgress
                || draggedCard != null
                || !cardView.IsFaceUp
                || cardView.Container is GoalSlot)
            {
                return;
            }

            List<CardView> stack = BuildDragStack(cardView);
            if (stack.Count == 0)
            {
                PlayInvalidMoveFeedback(cardView);
                return;
            }

            if (clickCount >= 2)
            {
                ClearTouchSelection();
                if (!TryAutoMoveCardToGoal(cardView, stack))
                {
                    PlayInvalidMoveFeedback(cardView);
                }

                return;
            }

            if (touchSelectedCard == cardView)
            {
                ClearTouchSelection();
                return;
            }

            if (touchSelectedCard != null
                && cardView.Container != null
                && cardView.Container != touchSelectedCard.Container
                && TryMoveSelectedCardTo(cardView.Container))
            {
                return;
            }

            ClearTouchSelection();
            touchSelectedCard = cardView;
            touchSelectedCard.SetTouchSelected(true);
        }

        public bool TryMoveSelectedCardTo(ICardContainer targetContainer)
        {
            CardView selected = touchSelectedCard;
            if (selected == null || targetContainer == null || selected.Container == targetContainer)
            {
                return false;
            }

            List<CardView> stack = BuildDragStack(selected);
            if (stack.Count == 0 || !CanMoveCard(selected, targetContainer))
            {
                PlayInvalidMoveFeedback(selected);
                return false;
            }

            touchSelectedCard = null;
            selected.SetTouchSelected(false);
            draggedStack.Clear();
            draggedStack.AddRange(stack);
            ExecuteMove(selected, targetContainer);
            draggedStack.Clear();
            RestoreGameplayCardInteractivity();
            return true;
        }

        public void ClearTouchSelection()
        {
            if (touchSelectedCard != null)
            {
                touchSelectedCard.SetTouchSelected(false);
                touchSelectedCard = null;
            }
        }

        private bool TryAutoMoveCardToGoal(CardView cardView, List<CardView> stack)
        {
            foreach (GoalSlot goalSlot in GetActiveGoalSlots())
            {
                if (goalSlot == null || !CanMoveCard(cardView, goalSlot))
                {
                    continue;
                }

                draggedStack.Clear();
                draggedStack.AddRange(stack);
                ExecuteMove(cardView, goalSlot);
                draggedStack.Clear();
                RestoreGameplayCardInteractivity();
                return true;
            }

            return false;
        }

        private static Vector2 GetTouchDragLift(int pointerId)
        {
            if (pointerId < 0)
            {
                return Vector2.zero;
            }

            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            float lift = TouchDragLiftReferencePixels * Mathf.Clamp(dpi / 160f, 0.8f, 2f);
            return Vector2.up * lift;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                CancelActivePointerInteraction();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelActivePointerInteraction();
            }
        }

        private void CancelActivePointerInteraction()
        {
            ClearTouchSelection();
            if (draggedCard == null)
            {
                return;
            }

            CardView canceledCard = draggedCard;
            FinishDragVisual();
            canceledCard.Container?.RefreshLayout();
            draggedCard = null;
            activePointerId = int.MinValue;
            dragWasResolved = false;
            draggedStack.Clear();
            draggedStackOffsets.Clear();
            RestoreGameplayCardInteractivity();
        }

        public bool TryMoveDraggedCardTo(CardColumn targetColumn)
        {
            return TryMoveDraggedCardToInternal(targetColumn);
        }

        public bool TryMoveDraggedCardTo(GoalSlot targetGoalSlot)
        {
            return TryMoveDraggedCardToInternal(targetGoalSlot);
        }

        public bool CanDropDraggedCardTo(ICardContainer targetContainer)
        {
            return draggedCard != null && CanMoveCard(draggedCard, targetContainer);
        }

        private HintMove FindBestHintMove()
        {
            List<HintMove> candidates = new();

            foreach (CardColumn sourceColumn in GetActiveColumns())
            {
                if (sourceColumn == null)
                {
                    continue;
                }

                for (int index = 0; index < sourceColumn.CardCount; index++)
                {
                    CardView card = sourceColumn.GetCardAt(index);
                    AddHintCandidatesForCard(card, sourceColumn, candidates);
                }
            }

            CardView drawCard = drawSlot != null ? drawSlot.TopCard : null;
            AddHintCandidatesForCard(drawCard, null, candidates);

            return candidates
                .OrderBy(candidate => candidate.Priority)
                .FirstOrDefault();
        }

        private void AddHintCandidatesForCard(CardView card, CardColumn sourceColumn, List<HintMove> candidates)
        {
            if (!CanUseCardAsHintSource(card))
            {
                return;
            }

            List<CardView> stack = BuildDragStack(card);
            if (stack.Count == 0)
            {
                return;
            }

            foreach (GoalSlot goalSlot in GetActiveGoalSlots())
            {
                if (CanMoveCard(card, goalSlot))
                {
                    candidates.Add(new HintMove(card, goalSlot, HintKind.MoveToGoal, 1, stack));
                }
            }

            foreach (CardColumn targetColumn in GetActiveColumns())
            {
                if (targetColumn == null || !CanMoveCard(card, targetColumn))
                {
                    continue;
                }

                bool revealsCard = sourceColumn != null && MoveRevealsCardOrColumn(sourceColumn, card, stack.Count);
                candidates.Add(revealsCard
                    ? new HintMove(card, targetColumn, HintKind.RevealCard, 2, stack)
                    : new HintMove(card, targetColumn, HintKind.MoveBetweenColumns, 3, stack));
            }
        }

        private static bool CanUseCardAsHintSource(CardView card)
        {
            return card != null
                   && card.gameObject.activeInHierarchy
                   && !card.IsDragDetached
                   && card.IsFaceUp
                   && card.Data != null
                   && card.Container != null
                   && card.Container is not GoalSlot;
        }

        private static bool MoveRevealsCardOrColumn(CardColumn sourceColumn, CardView card, int stackCount)
        {
            if (sourceColumn == null || card == null)
            {
                return false;
            }

            int cardIndex = -1;
            for (int index = 0; index < sourceColumn.CardCount; index++)
            {
                if (sourceColumn.GetCardAt(index) == card)
                {
                    cardIndex = index;
                    break;
                }
            }

            if (cardIndex < 0)
            {
                return false;
            }

            if (stackCount >= sourceColumn.CardCount)
            {
                return true;
            }

            CardView previousCard = cardIndex > 0 ? sourceColumn.GetCardAt(cardIndex - 1) : null;
            return previousCard != null && !previousCard.IsFaceUp;
        }

        private static string BuildHintFoundLog(HintMove hintMove)
        {
            string cardName = hintMove.Card != null && hintMove.Card.Data != null
                ? hintMove.Card.Data.DisplayText
                : "unknown";

            return hintMove.Kind switch
            {
                HintKind.MoveToGoal => $"Hint: found move to category for '{cardName}'.",
                HintKind.RevealCard => $"Hint: found move that reveals a new card for '{cardName}'.",
                HintKind.MoveBetweenColumns => $"Hint: found move between columns for '{cardName}'.",
                HintKind.OtherMove => $"Hint: found other available move for '{cardName}'.",
                _ => $"Hint: found move for '{cardName}'."
            };
        }

        private static Vector3 ResolveHintTargetPosition(HintMove hintMove)
        {
            if (hintMove == null || hintMove.Card == null || hintMove.TargetContainer == null)
            {
                return Vector3.zero;
            }

            RectTransform cardRect = hintMove.Card.RectTransform;
            if (hintMove.TargetContainer is GoalSlot goalSlot)
            {
                RectTransform slotRect = goalSlot.transform as RectTransform;
                RectTransform targetRect = slotRect != null ? slotRect : goalSlot.DropAreaRect;
                if (targetRect != null)
                {
                    return ResolveTargetPositionForCardPivot(cardRect, targetRect);
                }
            }

            if (hintMove.TargetContainer is CardColumn targetColumn)
            {
                CardView topCard = targetColumn.TopCard;
                if (topCard != null && topCard.RectTransform != null)
                {
                    return topCard.RectTransform.position + (Vector3.down * HintColumnDropOffset);
                }

                RectTransform dropArea = targetColumn.DropAreaRect;
                if (dropArea != null)
                {
                    return ResolveTargetPositionForCardPivot(cardRect, dropArea);
                }
            }

            if (hintMove.TargetContainer is MonoBehaviour targetBehaviour)
            {
                return targetBehaviour.transform.position;
            }

            return hintMove.Card.RectTransform.position;
        }

        private static Vector3 ResolveTargetPositionForCardPivot(RectTransform cardRect, RectTransform targetRect)
        {
            if (targetRect == null)
            {
                return Vector3.zero;
            }

            Vector2 pivot = cardRect != null ? cardRect.pivot : new Vector2(0.5f, 0.5f);
            Rect rect = targetRect.rect;
            Vector3 targetLocalPoint = new(
                Mathf.Lerp(rect.xMin, rect.xMax, pivot.x),
                Mathf.Lerp(rect.yMin, rect.yMax, pivot.y),
                0f);
            return targetRect.TransformPoint(targetLocalPoint);
        }

        public void RestartLevel()
        {
            if (levelDefinition == null)
            {
                Debug.LogError("GameManager: no LevelDefinition assigned.");
                return;
            }

            uiManager?.ResetPauseState();
            ClearTouchSelection();
            ClearBoard();
            moveHistory.Clear();
            completedAssociations.Clear();
            levelFinished = false;
            levelCompletionInProgress = false;
            completedUsedMovesCount = 0;
            draggedCard = null;
            activePointerId = int.MinValue;
            dragWasResolved = false;
            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
                levelCompleteRoutine = null;
            }

            if (levelRewardRoutine != null)
            {
                StopCoroutine(levelRewardRoutine);
                levelRewardRoutine = null;
            }

            winScreenView?.HideInstant();
            uiManager?.SetGameplayButtonsInteractable(true);
            uiManager?.HideMovesBonusTextInstant();

            bool useDevColumnsOverride = devOverrideTargetColumns.HasValue;
            int? targetColumnsCount = useDevColumnsOverride
                ? devOverrideTargetColumns.Value
                : progressionManager != null ? (int?)progressionManager.CurrentTargetColumns : null;
            int currentLevelNumber = devOverrideLevelNumber
                ?? (progressionManager != null ? progressionManager.CurrentLevelNumber : 1);
            int layoutVariantIndex = devOverrideLevelNumber.HasValue
                ? Mathf.Max(0, currentLevelNumber - 1)
                : progressionManager != null ? progressionManager.CurrentLayoutVariantIndex : 0;
            string targetLayoutId = ResolveDevTargetLayoutId(useDevColumnsOverride, currentLevelNumber);
            int levelSeed = useDevColumnsOverride
                ? devOverrideSeed.GetValueOrDefault(Random.Range(1, int.MaxValue))
                : progressionManager != null ? progressionManager.GetOrCreateSeed(currentLevelNumber) : Random.Range(1, int.MaxValue);
            bool generatedFromManifest = TryCreateManifestLevelForRestart(useDevColumnsOverride, currentLevelNumber, out generatedLevelData);
            if (useDevColumnsOverride && !generatedFromManifest)
            {
                Debug.LogError($"GameManager: cannot load verified manifest level for DEV columns={targetColumnsCount}. Runtime random fallback is disabled for balanced layouts.");
                return;
            }

            if (!generatedFromManifest
                && !LevelGenerator.TryGenerate(levelDefinition, cardDatabase, targetLayoutId, targetColumnsCount, layoutVariantIndex, levelSeed, currentLevelNumber, out generatedLevelData))
            {
                Debug.LogError("GameManager: failed to generate level from config.");
                return;
            }

            remainingMoves = generatedLevelData.ReferenceMoves;
            ApplyLayout();
            if (!BuildLevelData())
            {
                return;
            }

            CreateStartingCards();

            uiManager.Initialize(this, BuildObjectiveStates());
            UpdateMovesDisplay();
            if (coinsManager != null)
            {
                uiManager.UpdateCoins(coinsManager.CurrentCoins);
            }

            if (boostInventory != null)
            {
                uiManager.UpdateHintCount(boostInventory.HintCount);
                uiManager.UpdateUndoCount(boostInventory.UndoCount);
            }

            adsManager?.ResetRestartCounterForLevel(currentLevelNumber);

            ResolveBoardState(false);
            LogGeneratedLevelDiagnostics(currentLevelNumber);
            Debug.Log($"GameManager: Started level {currentLevelNumber} with seed {generatedLevelData.GenerationSeed}.");
            platformService?.TrackEvent("level_restart");
        }

        private string ResolveDevTargetLayoutId(bool useDevColumnsOverride, int levelNumber)
        {
            if (useDevColumnsOverride)
            {
                return devOverrideTargetColumns.HasValue
                    && LevelManifestDatabase.TryGetLevelWithColumns(devOverrideTargetColumns.Value, FirstManifestLevelNumber, out LevelManifestEntry entry)
                    ? entry.layoutId
                    : string.Empty;
            }

            if (devOverrideLevelNumber.HasValue)
            {
                return LevelManifestDatabase.TryGetLevel(levelNumber, out LevelManifestEntry entry)
                    ? entry.layoutId
                    : string.Empty;
            }

            return progressionManager != null ? progressionManager.CurrentLayoutId : string.Empty;
        }

        private bool TryCreateManifestLevelForRestart(bool useDevColumnsOverride, int levelNumber, out GeneratedLevelData manifestLevelData)
        {
            manifestLevelData = null;
            if (useDevColumnsOverride)
            {
                if (devOverrideTargetColumns.HasValue
                    && LevelManifestDatabase.TryGetLevelWithColumns(devOverrideTargetColumns.Value, FirstManifestLevelNumber, out LevelManifestEntry entry)
                    && LevelManifestDatabase.TryCreateGeneratedLevelData(entry, out manifestLevelData))
                {
                    Debug.Log(
                        $"GameManager: Loaded manifest dev layout. requestedColumns={devOverrideTargetColumns.Value}, manifestLevel={entry.levelNumber}, layout={entry.layoutId}, deck={entry.deckCount}, moves={entry.startingMoves}, goldCards={entry.goldenCards}.");
                    return true;
                }

                Debug.LogWarning(
                    $"GameManager: no manifest level found for DEV columns={devOverrideTargetColumns.GetValueOrDefault()}.");
                return false;
            }

            if (devOverrideLevelNumber.HasValue)
            {
                return LevelManifestDatabase.TryGetLevel(levelNumber, out LevelManifestEntry entry)
                    && LevelManifestDatabase.TryCreateGeneratedLevelData(entry, out manifestLevelData);
            }

            return progressionManager != null
                && progressionManager.TryGetCurrentManifestEntry(out LevelManifestEntry manifestEntry)
                && LevelManifestDatabase.TryCreateGeneratedLevelData(manifestEntry, out manifestLevelData);
        }

        private void LogGeneratedLevelDiagnostics(int levelNumber)
        {
            if (generatedLevelData == null)
            {
                return;
            }

            int columnCards = 0;
            int imageCards = 0;
            int goldCards = 0;
            foreach (List<string> column in generatedLevelData.Columns)
            {
                if (column == null)
                {
                    continue;
                }

                foreach (string cardId in column)
                {
                    columnCards++;
                    if (generatedLevelData.IsImageCard(cardId))
                    {
                        imageCards++;
                    }

                    if (generatedLevelData.TryGetGoldRequiredCount(cardId, out _))
                    {
                        goldCards++;
                    }
                }
            }

            foreach (string cardId in generatedLevelData.StockCards)
            {
                columnCards++;
                if (generatedLevelData.IsImageCard(cardId))
                {
                    imageCards++;
                }

                if (generatedLevelData.TryGetGoldRequiredCount(cardId, out _))
                {
                    goldCards++;
                }
            }

            int textCards = Mathf.Max(0, columnCards - imageCards - goldCards);
            string goalCategoryIds = objectiveOrder != null && objectiveOrder.Count > 0
                ? string.Join(",", objectiveOrder)
                : string.Empty;
            Debug.Log(
                $"GameManager: Level generation diagnostics. level={levelNumber}, seed={generatedLevelData.GenerationSeed}, layout={generatedLevelData.LayoutId}, cards={columnCards}, imageCards={imageCards}, textCards={textCards}, goalSlots={generatedLevelData.ColumnsCount}, goalCategories={objectiveOrder.Count}, goldCards={goldCards}, goalIds={goalCategoryIds}, attempts={generatedLevelData.GenerationAttempts}, solverVerified={generatedLevelData.SolverVerified}, solverResult={generatedLevelData.SolverResult}, solverMoves={generatedLevelData.SolverMoves}, solverStates={generatedLevelData.SolverStates}.");
        }

        private void ApplyLayout()
        {
            int configuredColumnsCount = generatedLevelData != null
                ? generatedLevelData.ColumnsCount
                : levelDefinition != null
                    ? levelDefinition.ColumnsCount
                    : 4;
            int visibleColumnsCount = Mathf.Clamp(configuredColumnsCount, 3, 5);
            ColumnLayoutProfile profile = GetGameplayLayoutProfile(visibleColumnsCount);

            if (columnsLayoutGroup != null)
            {
                columnsLayoutGroup.spacing = profile.columnSpacing;
            }

            if (columnsContainer != null)
            {
                LayoutElement layoutElement = columnsContainer.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.minHeight = profile.cardSize.y + (profile.verticalOffset * 5f);
                    layoutElement.preferredHeight = layoutElement.minHeight;
                }
            }

            int activeColumnsCount = Mathf.Min(visibleColumnsCount, columns.Count, goalSlots.Count);
            int activatedColumns = 0;
            sceneRefs?.HideAllGoalLabels();

            for (int index = 0; index < columns.Count; index++)
            {
                bool isActive = index < activeColumnsCount;
                CardColumn column = columns[index];
                column.gameObject.SetActive(isActive);

                if (!isActive)
                {
                    continue;
                }

                activatedColumns++;
                column.ApplyLayout(profile);
                column.SetBackgroundAlpha(0f);
            }

            for (int index = 0; index < goalSlots.Count; index++)
            {
                bool isActive = index < activeColumnsCount;
                GoalSlot goalSlot = goalSlots[index];
                goalSlot.gameObject.SetActive(isActive);

                if (isActive)
                {
                    goalSlot.ApplyLayout(profile);
                }
            }

            drawSlot?.ApplyLayout(profile);
            Debug.Log($"GameManager: Activated {activatedColumns}/{columns.Count} columns.");
        }

        public static ColumnLayoutProfile GetGameplayLayoutProfile(int visibleColumnsCount)
        {
            visibleColumnsCount = Mathf.Clamp(visibleColumnsCount, 3, 5);
            return NormalizeGameplayProfile(GameLayoutConfig.CreateDefault().GetProfile(visibleColumnsCount), visibleColumnsCount);
        }

        private static ColumnLayoutProfile NormalizeGameplayProfile(ColumnLayoutProfile profile, int visibleColumnsCount)
        {
            bool portrait = Screen.height > Screen.width;
            Vector2 targetCardSize = portrait
                ? visibleColumnsCount switch
                {
                    3 => new Vector2(176f, 256f),
                    4 => new Vector2(160f, 233f),
                    _ => new Vector2(148f, 216f)
                }
                : new Vector2(170f, 248f);
            float targetSpacing = portrait
                ? visibleColumnsCount <= 3
                    ? 58f
                    : visibleColumnsCount == 4
                        ? 36f
                        : 24f
                : visibleColumnsCount <= 3
                    ? 72f
                    : visibleColumnsCount == 4
                        ? 56f
                        : 48f;
            float targetVerticalOffset = portrait ? 52f : 40f;

            profile.columnCount = visibleColumnsCount;
            profile.cardSize = targetCardSize;
            profile.columnSpacing = targetSpacing;
            profile.verticalOffset = targetVerticalOffset;
            return profile;
        }

        private bool BuildLevelData()
        {
            cardsById.Clear();
            goalCardsByAssociation.Clear();
            objectiveOrder.Clear();

            if (cardDatabase == null)
            {
                Debug.LogError("GameManager: card database is not assigned.");
                return false;
            }

            if (!cardDatabase.Load())
            {
                Debug.LogError("GameManager: failed to load card database.");
                return false;
            }

            if (generatedLevelData == null)
            {
                Debug.LogError("GameManager: generated level data is missing.");
                return false;
            }

            foreach (List<string> columnDefinition in generatedLevelData.Columns)
            {
                if (columnDefinition == null)
                {
                    continue;
                }

                foreach (string cardId in columnDefinition)
                {
                    RegisterLevelCard(cardId);
                }
            }

            foreach (string cardId in generatedLevelData.StockCards)
            {
                RegisterLevelCard(cardId);
            }

            if (cardsById.Count == 0)
            {
                Debug.LogError("GameManager: no cards were resolved from the level definition.");
                return false;
            }

            return true;
        }

        private void RegisterLevelCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                Debug.LogWarning("GameManager: level contains an empty card id.");
                return;
            }

            if (cardsById.ContainsKey(cardId))
            {
                Debug.LogWarning($"GameManager: duplicate card id '{cardId}' in level definition.");
                return;
            }

            int requiredCountOverride = 0;
            bool hasRequiredCountOverride = generatedLevelData != null
                && generatedLevelData.TryGetGoldRequiredCount(cardId, out requiredCountOverride);

            CardData cardData;
            CardType regularCardType = generatedLevelData != null && generatedLevelData.IsImageCard(cardId)
                ? CardType.Image
                : CardType.Text;
            bool wasCreated = hasRequiredCountOverride
                ? cardDatabase.TryCreateCardInstance(cardId, requiredCountOverride, regularCardType, out cardData)
                : cardDatabase.TryCreateCardInstance(cardId, regularCardType, out cardData);

            if (!wasCreated)
            {
                Debug.LogWarning($"GameManager: card id '{cardId}' was not found in CardDatabase.");
                return;
            }

            if (cardData.IsGold && generatedLevelData != null)
            {
                cardData.SetImageGoal(generatedLevelData.IsImageGoal(cardData.AssociationId));
            }

            cardsById[cardId] = cardData;

            if (!cardData.IsGold)
            {
                return;
            }

            goalCardsByAssociation[cardData.AssociationId] = cardData;
            if (!objectiveOrder.Contains(cardData.AssociationId))
            {
                objectiveOrder.Add(cardData.AssociationId);
            }
        }

        private void CreateStartingCards()
        {
            List<CardColumn> activeColumns = GetActiveColumns();
            List<CardView> stockCardViews = new();

            for (int columnIndex = 0; columnIndex < activeColumns.Count; columnIndex++)
            {
                if (generatedLevelData == null || columnIndex >= generatedLevelData.Columns.Count)
                {
                    break;
                }

                List<string> columnDefinition = generatedLevelData.Columns[columnIndex];
                List<CardView> columnCards = new();
                foreach (string cardId in columnDefinition)
                {
                    if (string.IsNullOrWhiteSpace(cardId) || !cardsById.TryGetValue(cardId, out CardData cardData))
                    {
                        Debug.LogWarning("GameManager: level contains an invalid card id in a column.");
                        continue;
                    }

                    Transform cardsRoot = activeColumns[columnIndex].CardsRoot;
                    CardView cardView = Instantiate(cardPrefab, cardsRoot);
                    cardView.gameObject.hideFlags = HideFlags.None;
                    cardView.gameObject.SetActive(true);
                    cardView.name = cardData.Id;
                    cardView.SetVisualConfig(visualConfig);
                    cardView.Setup(cardData, this, dragLayer);
                    cardView.SetFaceUp(false);
                    columnCards.Add(cardView);
                    spawnedCards.Add(cardView);
                    Debug.Log($"GameManager: Spawned card '{cardData.Id}' into {SolitaireSceneRefs.GetPath(cardsRoot)}.");
                }

                activeColumns[columnIndex].AddCards(columnCards);
                activeColumns[columnIndex].RevealOnlyTopCard();
            }

            foreach (string cardId in generatedLevelData != null ? generatedLevelData.StockCards : new List<string>())
            {
                if (string.IsNullOrWhiteSpace(cardId) || !cardsById.TryGetValue(cardId, out CardData cardData))
                {
                    Debug.LogWarning("GameManager: stock contains an invalid card id.");
                    continue;
                }

                Transform cardRoot = drawSlot.CardRoot;
                CardView cardView = Instantiate(cardPrefab, cardRoot);
                cardView.gameObject.hideFlags = HideFlags.None;
                cardView.gameObject.SetActive(false);
                cardView.name = cardData.Id;
                cardView.SetVisualConfig(visualConfig);
                cardView.Setup(cardData, this, dragLayer);
                stockCardViews.Add(cardView);
                spawnedCards.Add(cardView);
                Debug.Log($"GameManager: Spawned stock card '{cardData.Id}' into {SolitaireSceneRefs.GetPath(cardRoot)}.");
            }

            drawSlot?.SetStockCards(stockCardViews);
        }

        private bool TryMoveDraggedCardToInternal(ICardContainer targetContainer)
        {
            if (draggedCard == null || targetContainer == null)
            {
                return false;
            }

            if (!CanMoveCard(draggedCard, targetContainer))
            {
                return false;
            }

            ExecuteMove(draggedCard, targetContainer);
            dragWasResolved = true;
            return true;
        }

        private void ExecuteMove(CardView cardView, ICardContainer targetContainer)
        {
            ICardContainer sourceContainer = cardView.Container;
            if (sourceContainer == null)
            {
                return;
            }

            BoardSnapshot snapshot = CaptureBoardSnapshot();
            Dictionary<CardView, Vector3> startPositions = CaptureMoveStartPositions(cardView);
            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard != null)
                {
                    stackCard.SetDragDetached(false);
                }
            }

            if (targetContainer is CardColumn targetColumn)
            {
                List<CardView> movingStack = sourceContainer is CardColumn sourceColumn
                    ? sourceColumn.RemoveStackStartingAt(cardView)
                    : new List<CardView>();
                if (movingStack.Count > 0)
                {
                    targetColumn.AddCardsFromPlayerMove(movingStack);
                }
                else
                {
                    sourceContainer.RemoveCard(cardView);
                    targetColumn.AddCardsFromPlayerMove(new List<CardView> { cardView });
                }
            }
            else if (sourceContainer is CardColumn goalSourceColumn && targetContainer is GoalSlot targetGoalSlot)
            {
                List<CardView> movingStack = goalSourceColumn.RemoveStackStartingAt(cardView);
                if (movingStack.Count > 0)
                {
                    targetGoalSlot.AddCards(NormalizeStackForGoalSlot(movingStack));
                }
                else
                {
                    sourceContainer.RemoveCard(cardView);
                    targetContainer.AddCard(cardView);
                }
            }
            else
            {
                sourceContainer.RemoveCard(cardView);
                targetContainer.AddCard(cardView);
            }

            PlayMoveAnimations(startPositions);
            global::MusicPlayer.Instance?.PlayCardDrop();
            if (targetContainer is GoalSlot acceptedGoalSlot)
            {
                foreach (CardView stackCard in draggedStack.Count > 0 ? draggedStack : new List<CardView> { cardView })
                {
                    stackCard?.PlayAcceptedFeedback();
                }

                acceptedGoalSlot.PlayAcceptFeedback((cardView.Data != null && cardView.Data.IsGold) || draggedStack.Count > 1);
            }

            remainingMoves = Mathf.Max(0, remainingMoves - 1);
            UpdateMovesDisplay();
            PushMoveSnapshot(snapshot);

            platformService?.TrackEvent("card_move");
            ResolveBoardState(true);
        }

        private static List<CardView> NormalizeStackForGoalSlot(List<CardView> movingStack)
        {
            if (movingStack == null || movingStack.Count <= 1)
            {
                return movingStack;
            }

            int goldIndex = movingStack.FindIndex(cardView => cardView != null && cardView.Data != null && cardView.Data.IsGold);
            if (goldIndex < 0)
            {
                return movingStack;
            }

            List<CardView> normalized = new() { movingStack[goldIndex] };
            for (int index = 0; index < movingStack.Count; index++)
            {
                if (index == goldIndex)
                {
                    continue;
                }

                normalized.Add(movingStack[index]);
            }

            return normalized;
        }

        private bool CanMoveCard(CardView cardView, ICardContainer targetContainer)
        {
            return cardView != null
                   && targetContainer != null
                   && cardView.Container != null
                   && targetContainer != cardView.Container
                   && CanMoveStackTo(cardView, targetContainer)
                   && targetContainer.CanAccept(cardView);
        }

        private List<CardView> BuildDragStack(CardView cardView)
        {
            if (cardView == null || cardView.Container == null)
            {
                return new List<CardView>();
            }

            if (cardView.Container is CardColumn sourceColumn)
            {
                List<CardView> stack = sourceColumn.GetMovableStack(cardView);
                if (stack.Count > 0)
                {
                    return stack;
                }

                if (sourceColumn.IsTopCard(cardView))
                {
                    return new List<CardView> { cardView };
                }

                return new List<CardView>();
            }

            return cardView.Container.IsTopCard(cardView)
                ? new List<CardView> { cardView }
                : new List<CardView>();
        }

        private bool CanMoveStackTo(CardView cardView, ICardContainer targetContainer)
        {
            List<CardView> stack = draggedStack.Count > 0
                ? draggedStack
                : BuildDragStack(cardView);

            if (targetContainer is CardColumn targetColumn)
            {
                if (targetColumn.TopCard == null)
                {
                    return IsValidStackForEmptyColumn(stack);
                }

                if (cardView.Data != null && cardView.Data.IsGold && stack.Count > 1)
                {
                    return false;
                }

                return true;
            }

            if (targetContainer is not GoalSlot)
            {
                return true;
            }

            if (targetContainer is GoalSlot targetGoalSlot)
            {
                return targetGoalSlot.CanAcceptStack(stack);
            }

            return stack.Count == 1 && (cardView.Container == null || cardView.Container.IsTopCard(cardView));
        }

        private static bool IsValidStackForEmptyColumn(List<CardView> stack)
        {
            if (stack == null || stack.Count == 0)
            {
                return false;
            }

            CardData firstData = stack[0]?.Data;
            if (firstData == null)
            {
                return false;
            }

            string associationId = firstData.AssociationId;
            bool stackUsesImages = firstData.IsGold ? firstData.IsImageGoal : firstData.IsImageCard;
            bool hasGold = false;

            for (int index = 0; index < stack.Count; index++)
            {
                CardView stackCard = stack[index];
                if (stackCard == null || stackCard.Data == null || !stackCard.IsFaceUp)
                {
                    return false;
                }

                CardData data = stackCard.Data;
                if (data.AssociationId != associationId)
                {
                    return false;
                }

                if (data.IsGold)
                {
                    if (hasGold || index != stack.Count - 1 || data.IsImageGoal != stackUsesImages)
                    {
                        return false;
                    }

                    hasGold = true;
                    continue;
                }

                if (data.IsImageCard != stackUsesImages)
                {
                    return false;
                }
            }

            return true;
        }

        private Dictionary<CardView, Vector3> CaptureMoveStartPositions(CardView cardView)
        {
            Dictionary<CardView, Vector3> positions = new();

            if (draggedStack.Count > 0)
            {
                foreach (CardView stackCard in draggedStack)
                {
                    if (stackCard != null)
                    {
                        positions[stackCard] = stackCard.RectTransform.position;
                    }
                }

                return positions;
            }

            if (cardView != null)
            {
                positions[cardView] = cardView.RectTransform.position;
            }

            return positions;
        }

        private void PlayMoveAnimations(Dictionary<CardView, Vector3> startPositions)
        {
            foreach (KeyValuePair<CardView, Vector3> entry in startPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.AnimateFromWorldPosition(entry.Value, moveDuration);
                }
            }
        }

        private void PlayInvalidMoveFeedback(CardView cardView)
        {
            cardView?.PlayShake(shakeDuration, shakeStrength);
        }

        private IEnumerator PlayMoveHintAnimation(HintMove hintMove)
        {
            hintAnimationInProgress = true;

            CardView card = hintMove?.Card;
            if (card == null || card.RectTransform == null)
            {
                hintAnimationInProgress = false;
                Debug.Log("Hint: completed.");
                yield break;
            }

            List<CardView> animatedCards = ResolveHintAnimationStack(hintMove);
            Vector3 startPosition = card.RectTransform.position;
            Vector3 liftPosition = startPosition + (Vector3.up * HintLiftOffset);
            Vector3 targetPosition = ResolveHintTargetPosition(hintMove);
            Transform animationParent = dragLayer != null
                ? dragLayer
                : card.RectTransform.parent != null ? card.RectTransform.parent : transform;
            ICardContainer sourceContainer = card.Container;
            Dictionary<CardView, HintCardTransform> originalTransforms = new();
            Dictionary<CardView, Vector3> anchorOffsets = new();

            foreach (CardView stackCard in animatedCards)
            {
                if (stackCard == null || stackCard.RectTransform == null)
                {
                    continue;
                }

                RectTransform stackRect = stackCard.RectTransform;
                originalTransforms[stackCard] = new HintCardTransform(
                    stackRect.parent,
                    stackRect.GetSiblingIndex(),
                    stackRect.anchoredPosition,
                    stackRect.localScale,
                    stackRect.localRotation);
                anchorOffsets[stackCard] = stackRect.position - startPosition;
                stackCard.PlayAcceptedFeedback();
                stackCard.SetRaycastBlocks(false);
                stackCard.SetDragDetached(true);
                ApplyDragStackTitleLayout(animatedCards, stackCard);
                stackRect.SetParent(animationParent, true);
                stackRect.SetAsLastSibling();
            }

            if (sourceContainer is CardColumn sourceColumn)
            {
                sourceColumn.RefreshLayoutForDragPreview();
            }
            else
            {
                sourceContainer?.RefreshLayout();
            }

            float travelDuration = HintMoveDuration * 0.5f;
            yield return AnimateHintStackWorld(animatedCards, anchorOffsets, startPosition, liftPosition, travelDuration * 0.45f);
            yield return AnimateHintStackWorld(animatedCards, anchorOffsets, liftPosition, targetPosition, travelDuration);
            yield return new WaitForSeconds(HintHoldDuration);
            yield return AnimateHintStackWorld(animatedCards, anchorOffsets, targetPosition, startPosition, travelDuration);

            foreach (CardView stackCard in animatedCards)
            {
                if (stackCard == null || stackCard.RectTransform == null)
                {
                    continue;
                }

                RectTransform stackRect = stackCard.RectTransform;
                if (originalTransforms.TryGetValue(stackCard, out HintCardTransform original))
                {
                    stackRect.SetParent(original.Parent, false);
                    stackRect.SetSiblingIndex(original.SiblingIndex);
                    stackRect.anchoredPosition = original.AnchoredPosition;
                    stackRect.localScale = original.Scale;
                    stackRect.localRotation = original.Rotation;
                }

                stackCard.SetDragDetached(false);
                stackCard.SetRaycastBlocks(stackCard.gameObject.activeInHierarchy
                    && stackCard.Container != null
                    && stackCard.Container is not GoalSlot);
            }

            sourceContainer?.RefreshLayout();
            hintAnimationInProgress = false;
            Debug.Log("Hint: completed.");
        }

        private static List<CardView> ResolveHintAnimationStack(HintMove hintMove)
        {
            List<CardView> result = new();
            if (hintMove?.MovingStack != null)
            {
                foreach (CardView stackCard in hintMove.MovingStack)
                {
                    if (stackCard != null && !result.Contains(stackCard))
                    {
                        result.Add(stackCard);
                    }
                }
            }

            if (hintMove?.Card != null && !result.Contains(hintMove.Card))
            {
                result.Add(hintMove.Card);
            }

            return result;
        }

        private IEnumerator AnimateHintStackWorld(
            IReadOnlyList<CardView> cardsToAnimate,
            IReadOnlyDictionary<CardView, Vector3> anchorOffsets,
            Vector3 startAnchorPosition,
            Vector3 targetAnchorPosition,
            float duration)
        {
            if (duration <= 0f)
            {
                PositionHintStack(cardsToAnimate, anchorOffsets, targetAnchorPosition);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                Vector3 anchorPosition = Vector3.LerpUnclamped(startAnchorPosition, targetAnchorPosition, easedProgress);
                PositionHintStack(cardsToAnimate, anchorOffsets, anchorPosition);
                yield return null;
            }

            PositionHintStack(cardsToAnimate, anchorOffsets, targetAnchorPosition);
        }

        private static void PositionHintStack(
            IReadOnlyList<CardView> cardsToAnimate,
            IReadOnlyDictionary<CardView, Vector3> anchorOffsets,
            Vector3 anchorPosition)
        {
            if (cardsToAnimate == null)
            {
                return;
            }

            foreach (CardView stackCard in cardsToAnimate)
            {
                if (stackCard == null || stackCard.RectTransform == null)
                {
                    continue;
                }

                Vector3 offset = anchorOffsets != null && anchorOffsets.TryGetValue(stackCard, out Vector3 storedOffset)
                    ? storedOffset
                    : Vector3.zero;
                stackCard.RectTransform.position = anchorPosition + offset;
            }
        }

        private IEnumerator PlayDeckHintAnimation()
        {
            hintAnimationInProgress = true;

            RectTransform deckRect = drawSlot != null ? drawSlot.transform as RectTransform : null;
            if (deckRect == null)
            {
                hintAnimationInProgress = false;
                Debug.Log("Hint: completed.");
                yield break;
            }

            Vector3 baseScale = deckRect.localScale;
            Quaternion baseRotation = deckRect.localRotation;
            float elapsed = 0f;

            while (elapsed < HintDeckDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / HintDeckDuration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                float wiggle = Mathf.Sin(progress * Mathf.PI * 8f) * (1f - progress);
                deckRect.localScale = baseScale * (1f + (pulse * 0.08f));
                deckRect.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wiggle * 5f);
                yield return null;
            }

            deckRect.localScale = baseScale;
            deckRect.localRotation = baseRotation;
            hintAnimationInProgress = false;
            Debug.Log("Hint: completed.");
        }

        private static IEnumerator AnimateRectWorld(RectTransform rectTransform, Vector3 from, Vector3 to, float duration)
        {
            if (rectTransform == null || duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - (2f * progress));
                rectTransform.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            rectTransform.position = to;
        }

        private void CompleteLevel()
        {
            if (levelCompletionInProgress || levelFinished)
            {
                return;
            }

            levelFinished = true;
            levelCompletionInProgress = true;
            completedUsedMovesCount = GetUsedMovesCount();
            SetGameplayInputEnabled(false);
            uiManager?.SetGameplayButtonsInteractable(false);

            if (levelRewardRoutine != null)
            {
                StopCoroutine(levelRewardRoutine);
            }

            levelRewardRoutine = StartCoroutine(EndLevelRewardSequence());
        }

        public bool CompleteLevelForDevTools()
        {
            InitializeDependencies();

            if (levelCompletionInProgress || levelFinished)
            {
                return false;
            }

            CompleteLevel();
            Debug.Log("[DEV] Complete level.");
            platformService?.TrackEvent("dev_complete_level");
            return true;
        }

        private IEnumerator EndLevelRewardSequence()
        {
            uiManager?.ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));

            if (movesBonusDelay > 0f)
            {
                yield return new WaitForSeconds(movesBonusDelay);
            }

            AwardLevelWinCoins();
            FinishLevelCompletion();
            levelRewardRoutine = null;
        }

        private void FinishLevelCompletion()
        {
            uiManager?.ShowStatus("\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u043f\u0440\u043e\u0439\u0434\u0435\u043d", new Color(0.76f, 0.96f, 0.74f, 1f));
            chestChaseManager?.RegisterLevelWon();
            if (coinsManager != null)
            {
                uiManager?.UpdateCoins(coinsManager.CurrentCoins);
            }

            if (boostInventory != null)
            {
                uiManager?.UpdateHintCount(boostInventory.HintCount);
                uiManager?.UpdateUndoCount(boostInventory.UndoCount);
            }

            global::MusicPlayer.Instance?.PlayWin();
            winScreenView?.Show(completedUsedMovesCount);
            platformService?.TrackEvent("level_win");

            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
            }

            levelCompleteRoutine = StartCoroutine(AdvanceToNextLevelAfterDelay());
        }

        private void SetGameplayInputEnabled(bool isEnabled)
        {
            foreach (CardView cardView in spawnedCards)
            {
                cardView?.SetRaycastBlocks(isEnabled && cardView.Container is not GoalSlot);
            }
        }

        private IEnumerator AdvanceToNextLevelAfterDelay()
        {
            yield return new WaitForSeconds(levelCompleteDelay);

            bool adFlowFinished = false;
            AdsManager activeAdsManager = ResolveAdsManager();
            if (activeAdsManager != null)
            {
                activeAdsManager.ShowInterstitialIfAllowed(
                    AdsManager.ReasonLevelComplete,
                    onCompleted: () => adFlowFinished = true,
                    onSkippedOrFailed: () => adFlowFinished = true);
            }
            else
            {
                adFlowFinished = true;
            }

            while (!adFlowFinished)
            {
                yield return null;
            }

            if (!devOverrideLevelNumber.HasValue && !devOverrideTargetColumns.HasValue)
            {
                progressionManager?.AdvanceToNextLevel();
            }

            levelCompleteRoutine = null;
            RestartLevel();
        }

        private void RetryCurrentLevel()
        {
            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
                levelCompleteRoutine = null;
            }

            RestartLevel();
        }

        public void RequestRestartWithConfirmation()
        {
            if (restartInProgress || restartConfirmationOpen)
            {
                return;
            }

            restartConfirmationOpen = true;
            if (uiManager != null)
            {
                uiManager.ShowRestartConfirmation(TrySpendLifeAndRestart, OnRestartConfirmationClosed);
            }
            else
            {
                OnRestartConfirmationClosed();
            }
        }

        private bool TrySpendLifeAndRestart()
        {
            restartConfirmationOpen = false;
            float nowRealtime = Time.unscaledTime;
            if (restartInProgress
                || lastRestartLifeSpendFrame == Time.frameCount
                || nowRealtime - lastRestartLifeSpendRealtime < RestartLifeSpendLockSeconds)
            {
                return false;
            }

            restartInProgress = true;
            lastRestartLifeSpendFrame = Time.frameCount;
            lastRestartLifeSpendRealtime = nowRealtime;
            int livesBeforeRestart = livesManager != null ? livesManager.CurrentLives : 0;
            if (livesManager == null || !livesManager.SpendLife())
            {
                restartInProgress = false;
                uiManager?.ShowLivesEmptyPopup(livesManager);
                return false;
            }

            Debug.Log($"GameManager: Restart spent one life. Lives {livesBeforeRestart}->{livesManager.CurrentLives}.");

            bool restartCallbackConsumed = false;
            void RestartOnce()
            {
                if (restartCallbackConsumed)
                {
                    return;
                }

                restartCallbackConsumed = true;
                RestartLevel();
                ReleaseRestartLockAfterRestart();
            }

            AdsManager activeAdsManager = ResolveAdsManager();
            if (activeAdsManager != null)
            {
                activeAdsManager.ShowInterstitialIfAllowed(
                    AdsManager.ReasonRestart,
                    onCompleted: RestartOnce,
                    onSkippedOrFailed: RestartOnce);
            }
            else
            {
                RestartOnce();
            }

            return true;
        }

        private void OnRestartConfirmationClosed()
        {
            restartConfirmationOpen = false;
        }

        private void ReleaseRestartLockAfterRestart()
        {
            if (!isActiveAndEnabled)
            {
                restartInProgress = false;
                restartLockReleaseRoutine = null;
                return;
            }

            if (restartLockReleaseRoutine != null)
            {
                StopCoroutine(restartLockReleaseRoutine);
            }

            restartLockReleaseRoutine = StartCoroutine(ReleaseRestartLockNextFrame());
        }

        private IEnumerator ReleaseRestartLockNextFrame()
        {
            yield return new WaitForSecondsRealtime(RestartLifeSpendLockSeconds);
            restartInProgress = false;
            restartLockReleaseRoutine = null;
        }

        public void ShowRewardedAddMoves()
        {
            if (rewardedAddMovesInProgress)
            {
                Debug.Log("[ADS] Rewarded failed");
                return;
            }

            if (levelFinished)
            {
                uiManager?.ShowStatus("Уровень уже завершен", new Color(1f, 0.82f, 0.52f, 1f));
                return;
            }

            AdsManager activeAdsManager = ResolveAdsManager();
            if (activeAdsManager == null)
            {
                Debug.LogWarning("[ADS] Rewarded failed");
                uiManager?.ShowStatus("Реклама недоступна", new Color(1f, 0.76f, 0.68f, 1f));
                return;
            }

            rewardedAddMovesInProgress = true;
            uiManager?.SetAddMovesButtonInteractable(false);
            activeAdsManager.ShowRewardedAddMoves(
                onCompleted: () =>
                {
                    AddRewardedMoves();
                    rewardedAddMovesInProgress = false;
                    uiManager?.SetAddMovesButtonInteractable(true);
                },
                onFailed: () =>
                {
                    rewardedAddMovesInProgress = false;
                    uiManager?.SetAddMovesButtonInteractable(true);
                    uiManager?.ShowStatus("Реклама не завершена", new Color(1f, 0.76f, 0.68f, 1f));
                });
        }

        private void AddRewardedMoves()
        {
            GrantMoves(
                Mathf.Max(1, rewardedAddMovesAmount),
                allowReviveLostLevel: false,
                statusPrefix: string.Empty,
                logPrefix: "[ADS] Rewarded grant",
                analyticsEvent: "rewarded_add_moves");
        }

        public void AddMovesForDevTools(int amount)
        {
            GrantMoves(
                amount,
                allowReviveLostLevel: true,
                statusPrefix: "DEV ",
                logPrefix: "[DEV] Grant",
                analyticsEvent: "dev_add_moves");
        }

        public void AddCoinsForDevTools(int amount)
        {
            InitializeDependencies();

            int safeAmount = Mathf.Max(0, amount);
            if (coinsManager == null)
            {
                coinsManager = GetComponent<CoinsManager>();
                if (coinsManager == null)
                {
                    coinsManager = gameObject.AddComponent<CoinsManager>();
                }
            }

            if (safeAmount <= 0 || coinsManager == null)
            {
                return;
            }

            int added = coinsManager.AddCoins(safeAmount);
            if (added <= 0)
            {
                return;
            }

            if (uiManager != null)
            {
                uiManager.PlayCoinsAward(coinsManager.CurrentCoins, added);
            }

            Debug.Log($"[DEV] Grant: Coins +{added}, coins={coinsManager.CurrentCoins}");
            platformService?.TrackEvent("dev_add_coins");
        }

        public void LoadDevLevelWithColumns(int columnsCount)
        {
            InitializeDependencies();

            int safeColumnsCount = Mathf.Clamp(columnsCount, 3, 5);
            devOverrideTargetColumns = safeColumnsCount;
            devOverrideLevelNumber = null;
            devOverrideSeed = null;
            RestartLevel();
            uiManager?.ShowStatus($"DEV: {safeColumnsCount} столб.", new Color(0.76f, 0.96f, 0.74f, 1f));
            Debug.Log($"[DEV] Load level: columns={safeColumnsCount}");
            platformService?.TrackEvent($"dev_load_{safeColumnsCount}_columns");
        }

        public void LoadDevLevelNumber(int levelNumber)
        {
            InitializeDependencies();

            int safeLevelNumber = Mathf.Clamp(levelNumber, 1, 1000);
            devOverrideLevelNumber = safeLevelNumber;
            devOverrideTargetColumns = null;
            devOverrideSeed = null;
            RestartLevel();
            uiManager?.ShowStatus($"DEV: уровень {safeLevelNumber}", new Color(0.76f, 0.96f, 0.74f, 1f));
            Debug.Log($"[DEV] Load level: level={safeLevelNumber}");
            platformService?.TrackEvent("dev_load_level_number");
        }

        private void GrantMoves(int amount, bool allowReviveLostLevel, string statusPrefix, string logPrefix, string analyticsEvent)
        {
            int safeAmount = Mathf.Max(1, amount);
            if (levelFinished)
            {
                if (!allowReviveLostLevel || levelCompletionInProgress)
                {
                    uiManager?.ShowStatus("Уровень уже завершен", new Color(1f, 0.82f, 0.52f, 1f));
                    return;
                }

                levelFinished = false;
                uiManager?.ResetPauseState();
                uiManager?.SetGameplayButtonsInteractable(true);
                SetGameplayInputEnabled(true);
            }

            remainingMoves += safeAmount;
            UpdateMovesDisplay();
            uiManager?.ShowStatus($"{statusPrefix}+{safeAmount} ходов", new Color(0.76f, 0.96f, 0.74f, 1f));
            Debug.Log($"{logPrefix}: AddMoves +{safeAmount}, moves={remainingMoves}");
            platformService?.TrackEvent(analyticsEvent);
        }

        public void RequestReturnToMainMenu()
        {
            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;

            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
                levelCompleteRoutine = null;
            }

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
                return;
            }

            if (Application.CanStreamedLevelBeLoaded("Assets/MainMenu.unity"))
            {
                SceneManager.LoadScene("Assets/MainMenu.unity");
                return;
            }

            Debug.LogWarning("GameManager: MainMenu scene is not in Build Settings.");
        }

        private int GetUsedMovesCount()
        {
            if (generatedLevelData == null)
            {
                return 0;
            }

            return Mathf.Max(0, generatedLevelData.ReferenceMoves - remainingMoves);
        }

        private void FinishDragVisual()
        {
            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard == null)
                {
                    continue;
                }

                stackCard.SetDragDetached(false);
                bool isGameplayCard = stackCard.gameObject.activeInHierarchy &&
                    stackCard.Container != null &&
                    stackCard.Container is not GoalSlot;
                stackCard.SetFooterTextOverride(null);
                stackCard.SetRaycastBlocks(isGameplayCard);
                stackCard.SetDraggingVisual(false);
            }
        }

        private void ApplyDraggedStackProgressFooter()
        {
            foreach (CardView stackCard in draggedStack)
            {
                stackCard?.SetFooterTextOverride(null);
            }

            if (draggedStack.Count <= 1)
            {
                return;
            }

            CardView progressCard = GetDragStackProgressCard(draggedStack);
            if (progressCard == null || progressCard.Data == null)
            {
                return;
            }

            string associationId = progressCard.Data.AssociationId;
            if (string.IsNullOrEmpty(associationId)
                || !goalCardsByAssociation.TryGetValue(associationId, out CardData goalCard)
                || goalCard.RequiredCount <= 0)
            {
                return;
            }

            int draggedCount = CountRegularCardsForAssociation(draggedStack, associationId, goalCard.IsImageGoal);
            if (draggedCount <= 0)
            {
                return;
            }

            progressCard.SetFooterTextOverride($"{Mathf.Min(draggedCount, goalCard.RequiredCount)}/{goalCard.RequiredCount}");
        }

        private static CardView GetDragStackProgressCard(IReadOnlyList<CardView> stack)
        {
            if (stack == null)
            {
                return null;
            }

            for (int index = stack.Count - 1; index >= 0; index--)
            {
                CardView stackCard = stack[index];
                if (stackCard != null && stackCard.Data != null && stackCard.IsFaceUp)
                {
                    return stackCard;
                }
            }

            return null;
        }

        private static int CountRegularCardsForAssociation(
            IReadOnlyList<CardView> stack,
            string associationId,
            bool expectsImageCard)
        {
            if (stack == null || string.IsNullOrEmpty(associationId))
            {
                return 0;
            }

            int count = 0;
            foreach (CardView stackCard in stack)
            {
                CardData data = stackCard != null ? stackCard.Data : null;
                if (data == null
                    || data.IsGold
                    || data.AssociationId != associationId
                    || data.IsImageCard != expectsImageCard)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static void ApplyDragStackTitleLayout(IReadOnlyList<CardView> stack, CardView stackCard)
        {
            if (stack == null || stackCard == null)
            {
                return;
            }

            int index = IndexOfCard(stack, stackCard);
            bool isPeekCard = index >= 0 && index < stack.Count - 1;
            if (!isPeekCard)
            {
                stackCard.SetRevealHistoryLayout(false);
                return;
            }

            stackCard.SetRevealHistoryLayout(
                true,
                ResolveDragStackPeekHeight(stack, index),
                stackCard.IsFaceUp,
                DragStackMaskOverflowHeight);
        }

        private static float ResolveDragStackPeekHeight(IReadOnlyList<CardView> stack, int index)
        {
            if (stack == null || index < 0 || index + 1 >= stack.Count)
            {
                return DragStackDefaultPeekHeight;
            }

            RectTransform currentRect = stack[index] != null ? stack[index].RectTransform : null;
            RectTransform nextRect = stack[index + 1] != null ? stack[index + 1].RectTransform : null;
            if (currentRect == null || nextRect == null)
            {
                return DragStackDefaultPeekHeight;
            }

            float verticalGap = Mathf.Abs(currentRect.position.y - nextRect.position.y);
            if (verticalGap <= 0.01f)
            {
                return DragStackDefaultPeekHeight;
            }

            return Mathf.Clamp(verticalGap + DragStackPeekPadding, DragStackDefaultPeekHeight, 52f);
        }

        private static int IndexOfCard(IReadOnlyList<CardView> stack, CardView stackCard)
        {
            if (stack == null || stackCard == null)
            {
                return -1;
            }

            for (int index = 0; index < stack.Count; index++)
            {
                if (stack[index] == stackCard)
                {
                    return index;
                }
            }

            return -1;
        }

        private bool TryResolveLooseDrop(CardView cardView, Vector2 screenPoint, Camera eventCamera)
        {
            if (cardView == null)
            {
                return false;
            }

            GoalSlot bestGoalSlot = FindBestLooseGoalSlot(cardView, screenPoint, eventCamera);
            if (bestGoalSlot != null)
            {
                ExecuteMove(cardView, bestGoalSlot);
                dragWasResolved = true;
                return true;
            }

            CardColumn bestColumn = FindBestLooseColumn(cardView, screenPoint, eventCamera);
            if (bestColumn != null)
            {
                ExecuteMove(cardView, bestColumn);
                dragWasResolved = true;
                return true;
            }

            return false;
        }

        private GoalSlot FindBestLooseGoalSlot(CardView cardView, Vector2 screenPoint, Camera eventCamera)
        {
            GoalSlot bestGoalSlot = null;
            float bestScore = float.MaxValue;

            foreach (GoalSlot goalSlot in GetActiveGoalSlots())
            {
                if (!CanMoveCard(cardView, goalSlot))
                {
                    continue;
                }

                float score = GetLooseGoalDropScore(goalSlot, screenPoint, eventCamera);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestGoalSlot = goalSlot;
            }

            return bestGoalSlot;
        }

        private static float GetLooseGoalDropScore(GoalSlot goalSlot, Vector2 screenPoint, Camera eventCamera)
        {
            if (goalSlot == null)
            {
                return float.MaxValue;
            }

            float cardsRootScore = GetLooseDropScore(goalSlot.DropAreaRect, screenPoint, eventCamera, LooseGoalDropPadding);
            RectTransform slotRect = goalSlot.transform as RectTransform;
            float wholeSlotScore = GetLooseDropScore(slotRect, screenPoint, eventCamera, LooseGoalDropPadding * 0.75f);
            return Mathf.Min(cardsRootScore, wholeSlotScore);
        }

        private CardColumn FindBestLooseColumn(CardView cardView, Vector2 screenPoint, Camera eventCamera)
        {
            CardColumn bestColumn = null;
            float bestScore = float.MaxValue;

            foreach (CardColumn column in GetActiveColumns())
            {
                if (!CanMoveCard(cardView, column))
                {
                    continue;
                }

                float score = GetLooseDropScore(
                    column.DropAreaRect,
                    screenPoint,
                    eventCamera,
                    LooseColumnDropPadding,
                    column.BlockedDropAreaRect);

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestColumn = column;
            }

            return bestColumn;
        }

        private static float GetLooseDropScore(
            RectTransform dropArea,
            Vector2 screenPoint,
            Camera eventCamera,
            float padding,
            RectTransform blockedArea = null)
        {
            if (dropArea == null)
            {
                return float.MaxValue;
            }

            if (blockedArea != null && TryContainsScreenPoint(blockedArea, screenPoint, eventCamera, padding * 0.4f))
            {
                return float.MaxValue;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dropArea, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return float.MaxValue;
            }

            Rect expandedRect = dropArea.rect;
            expandedRect.xMin -= padding;
            expandedRect.xMax += padding;
            expandedRect.yMin -= padding;
            expandedRect.yMax += padding;

            if (!expandedRect.Contains(localPoint))
            {
                return float.MaxValue;
            }

            return (localPoint - expandedRect.center).sqrMagnitude;
        }

        private static bool TryContainsScreenPoint(RectTransform rectTransform, Vector2 screenPoint, Camera eventCamera, float padding)
        {
            if (rectTransform == null)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            Rect expandedRect = rectTransform.rect;
            expandedRect.xMin -= padding;
            expandedRect.xMax += padding;
            expandedRect.yMin -= padding;
            expandedRect.yMax += padding;
            return expandedRect.Contains(localPoint);
        }

        private void ResolveBoardState(bool consumeMove)
        {
            RefreshGoalProgress();

            bool removedAny;
            do
            {
                removedAny = false;

                foreach (GoalSlot goalSlot in GetActiveGoalSlots())
                {
                    CardView goalCardView = goalSlot.GoalCard;
                    if (goalCardView == null || !goalCardView.Data.IsGold)
                    {
                        continue;
                    }

                    if (goalSlot.RegularCardsCount < goalCardView.Data.RequiredCount)
                    {
                        continue;
                    }

                    bool wasNewCompletion = completedAssociations.Add(goalCardView.Data.AssociationId);
                    if (wasNewCompletion)
                    {
                        goalSlot.PlayCompleteFeedback();
                        global::MusicPlayer.Instance?.PlayCardComplete();
                        uiManager.PlayGoalCompletedFeedback(goalSlot.transform as RectTransform, 0);
                    }

                    RemoveResolvedGoalSlot(goalSlot);
                    RestoreGameplayCardInteractivity();
                    removedAny = true;
                    platformService?.TrackEvent($"goal_complete_{goalCardView.Data.AssociationId}");
                }

                if (removedAny)
                {
                    RefreshGoalProgress();
                }
            } while (removedAny);

            UpdateUi();

            if (completedAssociations.Count == objectiveOrder.Count && objectiveOrder.Count > 0)
            {
                CompleteLevel();
            }
            else if (consumeMove && remainingMoves <= 0)
            {
                levelFinished = true;
                chestChaseManager?.RegisterLevelLost();
                uiManager.ShowStatus("Ходы закончились", new Color(1f, 0.76f, 0.68f, 1f));
                global::MusicPlayer.Instance?.PlayLose();
                uiManager.ShowLevelFailedPopup(TrySpendLifeAndRestart);
                platformService?.TrackEvent("level_lose");
            }
        }

        private void RemoveResolvedGoalSlot(GoalSlot goalSlot)
        {
            List<CardView> resolvedCards = new();
            while (goalSlot.TopCard != null)
            {
                CardView topCard = goalSlot.TopCard;
                goalSlot.RemoveCard(topCard);
                topCard.BindToContainer(null);
                topCard.SetRaycastBlocks(false);
                resolvedCards.Add(topCard);
            }

            if (resolvedCards.Count > 0 && isActiveAndEnabled)
            {
                StartCoroutine(AnimateResolvedGoalCards(resolvedCards, goalSlot.transform as RectTransform));
                return;
            }

            foreach (CardView card in resolvedCards)
            {
                if (card == null)
                {
                    continue;
                }

                card.SetVisible(false);
                card.gameObject.SetActive(false);
            }
        }

        private IEnumerator AnimateResolvedGoalCards(List<CardView> cards, RectTransform sourceRect)
        {
            Transform animationParent = dragLayer != null ? dragLayer : transform;
            Vector3 center = ResolveGoalAnimationCenter(cards, sourceRect);
            List<Vector3> startPositions = new(cards.Count);
            List<Vector3> startScales = new(cards.Count);

            foreach (CardView card in cards)
            {
                if (card == null)
                {
                    startPositions.Add(center);
                    startScales.Add(Vector3.one);
                    continue;
                }

                RectTransform rect = card.RectTransform;
                startPositions.Add(rect.position);
                startScales.Add(rect.localScale);
                card.gameObject.SetActive(true);
                card.SetVisible(true);
                card.SetRaycastBlocks(false);
                card.SetDragDetached(true);
                rect.SetParent(animationParent, true);
                rect.SetAsLastSibling();
            }

            float elapsed = 0f;
            while (elapsed < GoalCollapseDuration)
            {
                float t = Mathf.Clamp01(elapsed / GoalCollapseDuration);
                float eased = t * t * (3f - 2f * t);

                for (int index = 0; index < cards.Count; index++)
                {
                    CardView card = cards[index];
                    if (card == null)
                    {
                        continue;
                    }

                    RectTransform rect = card.RectTransform;
                    rect.position = Vector3.Lerp(startPositions[index], center, eased);
                    rect.localScale = Vector3.Lerp(startScales[index], Vector3.one * 0.08f, eased);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return PlayGoalCompletionCloud(center, animationParent);

            foreach (CardView card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.SetDragDetached(false);
                card.SetRaycastBlocks(false);
                card.SetVisible(false);
                card.RectTransform.localScale = Vector3.one;
                card.gameObject.SetActive(false);
            }

            RestoreGameplayCardInteractivity();
        }

        private static Vector3 ResolveGoalAnimationCenter(IReadOnlyList<CardView> cards, RectTransform sourceRect)
        {
            if (sourceRect != null)
            {
                return sourceRect.TransformPoint(sourceRect.rect.center);
            }

            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (CardView card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                center += card.RectTransform.position;
                count++;
            }

            return count > 0 ? center / count : Vector3.zero;
        }

        private IEnumerator PlayGoalCompletionCloud(Vector3 center, Transform animationParent)
        {
            GameObject cloud = new("GoalCompleteCloud", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rect = cloud.GetComponent<RectTransform>();
            rect.SetParent(animationParent != null ? animationParent : transform, false);
            rect.position = center;
            rect.sizeDelta = new Vector2(150f, 96f);
            rect.localScale = Vector3.one * 0.35f;
            rect.SetAsLastSibling();

            Image image = cloud.GetComponent<Image>();
            image.sprite = PremiumUISprites.IconCircle;
            image.color = new Color(0.78f, 0.75f, 0.66f, 0.28f);
            image.raycastTarget = false;

            CanvasGroup group = cloud.GetComponent<CanvasGroup>();
            group.alpha = 0.58f;

            float elapsed = 0f;
            while (elapsed < GoalCloudDuration)
            {
                float t = Mathf.Clamp01(elapsed / GoalCloudDuration);
                rect.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.1f, t);
                group.alpha = Mathf.Lerp(0.58f, 0f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(cloud);
        }

        private void RestoreGameplayCardInteractivity()
        {
            foreach (CardColumn column in GetActiveColumns())
            {
                column.RefreshLayout();
            }

            drawSlot?.RefreshLayout();

            foreach (GoalSlot goalSlot in GetActiveGoalSlots())
            {
                goalSlot.RefreshLayout();
            }

            foreach (CardView card in spawnedCards)
            {
                if (card == null || !card.gameObject.activeInHierarchy || card.IsDragDetached)
                {
                    continue;
                }

                card.SetRaycastBlocks(card.Container != null && card.Container is not GoalSlot);
            }
        }

        private void RefreshGoalProgress()
        {
            foreach (CardData goalCard in goalCardsByAssociation.Values)
            {
                goalCard.SetCurrentCount(completedAssociations.Contains(goalCard.AssociationId) ? goalCard.RequiredCount : 0);
            }

            foreach (GoalSlot goalSlot in GetActiveGoalSlots())
            {
                CardView goalCardView = goalSlot.GoalCard;
                if (goalCardView == null || !goalCardView.Data.IsGold)
                {
                    continue;
                }

                goalCardView.Data.SetCurrentCount(goalSlot.RegularCardsCount);
            }

            foreach (CardView cardView in spawnedCards)
            {
                cardView.RefreshVisual();
            }

            RefreshGoalLabels();
        }

        private List<ObjectiveState> BuildObjectiveStates()
        {
            objectiveStates.Clear();

            foreach (string associationId in objectiveOrder)
            {
                if (!goalCardsByAssociation.TryGetValue(associationId, out CardData goalCard))
                {
                    continue;
                }

                bool isComplete = completedAssociations.Contains(associationId);
                int currentCount = isComplete ? goalCard.RequiredCount : goalCard.CurrentCount;
                objectiveStates.Add(new ObjectiveState(
                    associationId,
                    goalCard.DisplayText,
                    Mathf.Min(currentCount, goalCard.RequiredCount),
                    goalCard.RequiredCount,
                    isComplete));
            }

            return objectiveStates;
        }

        private void UpdateUi()
        {
            UpdateMovesDisplay();
            uiManager.UpdateGoalsSummary(completedAssociations.Count, objectiveOrder.Count);
            uiManager.UpdateProgress(BuildObjectiveStates());
            uiManager.UpdateDeckCounter(drawSlot != null ? drawSlot.QueuedCardsCount : 0);
            if (coinsManager != null)
            {
                uiManager.UpdateCoins(coinsManager.CurrentCoins);
            }

            if (boostInventory != null)
            {
                uiManager.UpdateHintCount(boostInventory.HintCount);
                uiManager.UpdateUndoCount(boostInventory.UndoCount);
            }

            if (!levelFinished)
            {
                uiManager.ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            }
        }

        private void UpdateMovesDisplay()
        {
            sceneRefs?.UpdateMovesValue(remainingMoves);
            uiManager?.UpdateMoves(remainingMoves);
        }

        private void AwardLevelWinCoins()
        {
            if (coinsManager == null)
            {
                return;
            }

            int award = coinsManager.AddLevelWinReward();
            uiManager?.PlayCoinsAward(coinsManager.CurrentCoins, award);
            platformService?.TrackEvent("coins_awarded_level_win");
        }

        private BoardSnapshot CaptureBoardSnapshot()
        {
            BoardSnapshot snapshot = new()
            {
                RemainingMoves = remainingMoves,
                CompletedAssociations = new HashSet<string>(completedAssociations),
                QueuedDrawCards = drawSlot != null ? drawSlot.GetQueuedCardsSnapshot() : new List<CardView>(),
                RevealedDrawCards = drawSlot != null ? drawSlot.GetRevealedCardsSnapshot() : new List<CardView>()
            };

            foreach (CardColumn column in columns)
            {
                snapshot.Columns.Add(column != null ? column.GetCardsSnapshot() : new List<CardView>());
            }

            foreach (GoalSlot goalSlot in goalSlots)
            {
                snapshot.GoalSlots.Add(goalSlot != null ? goalSlot.GetCardsSnapshot() : new List<CardView>());
            }

            foreach (CardView cardView in spawnedCards)
            {
                if (cardView == null)
                {
                    continue;
                }

                snapshot.CardStates[cardView] = new CardSnapshot
                {
                    IsActive = cardView.gameObject.activeSelf,
                    IsFaceUp = cardView.IsFaceUp,
                    IsPlayerStackedOnPrevious = cardView.IsPlayerStackedOnPrevious
                };
            }

            return snapshot;
        }

        private void PushMoveSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            // History is written only after a successful move/draw, so failed drag attempts never consume undo.
            moveHistory.Push(snapshot);
        }

        private bool RestoreBoardSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            ClearTouchSelection();
            draggedCard = null;
            activePointerId = int.MinValue;
            dragWasResolved = false;
            draggedStack.Clear();
            draggedStackOffsets.Clear();

            foreach (CardView cardView in spawnedCards)
            {
                if (cardView == null)
                {
                    continue;
                }

                cardView.SetDragDetached(false);
                cardView.SetDraggingVisual(false);
                cardView.SetFooterTextOverride(null);
                cardView.SetRevealHistoryLayout(false);
                cardView.BindToContainer(null);

                if (snapshot.CardStates.TryGetValue(cardView, out CardSnapshot cardSnapshot))
                {
                    cardView.gameObject.SetActive(cardSnapshot.IsActive);
                    cardView.SetFaceUp(cardSnapshot.IsFaceUp);
                    cardView.SetPlayerStackedOnPrevious(cardSnapshot.IsPlayerStackedOnPrevious);
                    cardView.SetVisible(cardSnapshot.IsActive);
                }
                else
                {
                    cardView.SetPlayerStackedOnPrevious(false);
                    cardView.gameObject.SetActive(false);
                }
            }

            for (int index = 0; index < columns.Count; index++)
            {
                CardColumn column = columns[index];
                if (column == null)
                {
                    continue;
                }

                column.RestoreCards(index < snapshot.Columns.Count ? snapshot.Columns[index] : null);
            }

            for (int index = 0; index < goalSlots.Count; index++)
            {
                GoalSlot goalSlot = goalSlots[index];
                if (goalSlot == null)
                {
                    continue;
                }

                goalSlot.RestoreCards(index < snapshot.GoalSlots.Count ? snapshot.GoalSlots[index] : null);
            }

            drawSlot?.RestoreCards(snapshot.QueuedDrawCards, snapshot.RevealedDrawCards);
            completedAssociations.Clear();
            foreach (string associationId in snapshot.CompletedAssociations)
            {
                completedAssociations.Add(associationId);
            }

            remainingMoves = snapshot.RemainingMoves;
            levelFinished = false;
            levelCompletionInProgress = false;
            RefreshGoalProgress();
            UpdateUi();
            return true;
        }

        private List<CardColumn> GetActiveColumns()
        {
            return columns.Where(column => column != null && column.gameObject.activeSelf).ToList();
        }

        private List<GoalSlot> GetActiveGoalSlots()
        {
            return goalSlots.Where(goalSlot => goalSlot != null && goalSlot.gameObject.activeSelf).ToList();
        }

        private void ClearBoard()
        {
            foreach (CardView cardView in spawnedCards)
            {
                if (cardView != null)
                {
                    Destroy(cardView.gameObject);
                }
            }

            spawnedCards.Clear();
            generatedLevelData = null;

            foreach (CardColumn column in columns)
            {
                column?.ClearCards();
            }

            foreach (GoalSlot goalSlot in goalSlots)
            {
                goalSlot?.ClearCards();
            }

            drawSlot?.ClearCards();
            sceneRefs?.HideAllGoalLabels();
        }

        private void InitializeDependencies()
        {
            if (dependenciesInitialized)
            {
                return;
            }

            if (platformService == null)
            {
                platformService = GetComponent<PlatformService>();
            }

            if (cardDatabase == null)
            {
                cardDatabase = GetComponent<CardDatabase>();
            }

            if (progressionManager == null)
            {
                progressionManager = GetComponent<LevelProgressionManager>();
                if (progressionManager == null)
                {
                    progressionManager = gameObject.AddComponent<LevelProgressionManager>();
                }
            }

            if (coinsManager == null)
            {
                coinsManager = GetComponent<CoinsManager>();
                if (coinsManager == null)
                {
                    coinsManager = gameObject.AddComponent<CoinsManager>();
                }
            }

            if (livesManager == null)
            {
                livesManager = GetComponent<LivesManager>();
                if (livesManager == null)
                {
                    livesManager = gameObject.AddComponent<LivesManager>();
                }
            }

            if (boostInventory == null)
            {
                boostInventory = GetComponent<PlayerBoostInventory>();
                if (boostInventory == null)
                {
                    boostInventory = gameObject.AddComponent<PlayerBoostInventory>();
                }
            }

            if (chestChaseManager == null)
            {
                chestChaseManager = GetComponent<ChestChaseManager>();
                if (chestChaseManager == null)
                {
                    chestChaseManager = gameObject.AddComponent<ChestChaseManager>();
                }
            }

            adsManager = ResolveAdsManager();

            if (winScreenView == null)
            {
                winScreenView = FindFirstObjectByType<WinScreenView>(FindObjectsInactive.Include);
            }

            winScreenView?.Configure(RetryCurrentLevel, ReturnToMainMenu);

            if (imageCardsAtlas != null)
            {
                cardDatabase?.SetImageCardsAtlas(imageCardsAtlas);
            }

            cardDatabase?.Load();
            platformService?.Initialize();
            ApplyVisualConfig();

            foreach (CardColumn column in columns)
            {
                column?.Initialize(this);
            }

            foreach (GoalSlot goalSlot in goalSlots)
            {
                goalSlot?.Initialize(this);
            }

            drawSlot?.Initialize(this);
            dependenciesInitialized = true;
        }

        private AdsManager ResolveAdsManager()
        {
            if (adsManager != null)
            {
                return adsManager;
            }

            adsManager = AdsManager.Instance;
            if (adsManager != null)
            {
                return adsManager;
            }

            adsManager = FindFirstObjectByType<AdsManager>();
            if (adsManager != null)
            {
                return adsManager;
            }

            GameObject adsRoot = new("AdsManager");
            adsManager = adsRoot.AddComponent<AdsManager>();
            return adsManager;
        }

        private void ApplyVisualConfig()
        {
            if (cardPrefab != null)
            {
                cardPrefab.SetVisualConfig(visualConfig);
            }

            drawSlot?.SetVisualConfig(visualConfig);

            foreach (GoalSlot goalSlot in goalSlots)
            {
                goalSlot?.SetVisualConfig(visualConfig);
            }

            foreach (CardView cardView in spawnedCards)
            {
                cardView?.SetVisualConfig(visualConfig);
            }
        }

        private void RefreshGoalLabels()
        {
            if (sceneRefs == null)
            {
                return;
            }

            int count = Mathf.Min(sceneRefs.Columns.Count, goalSlots.Count);
            for (int index = 0; index < count; index++)
            {
                ColumnRef columnRef = sceneRefs.Columns[index];
                GoalSlot goalSlot = goalSlots[index];
                CardView goalCardView = goalSlot != null ? goalSlot.GoalCard : null;

                bool hasGoal = goalCardView != null && goalCardView.Data != null && goalCardView.Data.IsGold;
                string label = hasGoal ? goalCardView.Data.AssociationTitle : string.Empty;
                columnRef?.SetGoalLabel(hasGoal, label);
            }
        }
    }
}
