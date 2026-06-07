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

        [Header("Feedback")]
        [SerializeField, Min(0f)] private float moveDuration = 0.18f;
        [SerializeField, Min(0f)] private float shakeDuration = 0.18f;
        [SerializeField, Min(0f)] private float shakeStrength = 16f;
        [SerializeField] private Color highlightColor = new(1f, 0.92f, 0.34f, 0.45f);
        [SerializeField, Min(0.5f)] private float levelCompleteDelay = 1.75f;

        private readonly Dictionary<string, CardData> cardsById = new();
        private readonly Dictionary<string, CardData> goalCardsByAssociation = new();
        private readonly HashSet<string> completedAssociations = new();
        private readonly List<CardView> spawnedCards = new();
        private readonly List<CardView> draggedStack = new();
        private readonly Dictionary<CardView, Vector3> draggedStackOffsets = new();
        private readonly List<ObjectiveState> objectiveStates = new();
        private readonly List<string> objectiveOrder = new();

        private CardView draggedCard;
        private GeneratedLevelData generatedLevelData;
        private bool dragWasResolved;
        private int remainingMoves;
        private bool levelFinished;
        private bool dependenciesInitialized;
        private Coroutine levelCompleteRoutine;

        public LevelDefinition AssignedLevelDefinition => levelDefinition;
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
            layoutConfig ??= GameLayoutConfig.CreateDefault();
            return layoutConfig.GetProfile(targetColumnCount);
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

            if (!drawSlot.DrawNextCard())
            {
                return;
            }

            remainingMoves = Mathf.Max(0, remainingMoves - 1);
            platformService?.TrackEvent("stock_draw");
            ResolveBoardState(true);
        }

        public bool BeginDrag(CardView cardView)
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

            draggedCard = cardView;
            draggedStack.Clear();
            draggedStack.AddRange(stack);
            dragWasResolved = false;
            return true;
        }

        public bool IsDragging(CardView cardView)
        {
            return draggedCard == cardView;
        }

        public void BeginDragVisual(CardView cardView)
        {
            if (draggedCard != cardView || dragLayer == null)
            {
                return;
            }

            draggedStackOffsets.Clear();
            Vector3 anchorPosition = cardView.RectTransform.position;
            ICardContainer sourceContainer = cardView.Container;

            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard == null)
                {
                    continue;
                }

                draggedStackOffsets[stackCard] = stackCard.RectTransform.position - anchorPosition;
                stackCard.SetRaycastBlocks(false);
                stackCard.SetDragDetached(true);
                stackCard.SetRevealHistoryLayout(false);
                stackCard.SetDraggingVisual(true);
                stackCard.transform.SetParent(dragLayer, true);
                stackCard.transform.SetAsLastSibling();
            }

            sourceContainer?.RefreshLayout();
        }

        public void UpdateDragVisual(CardView cardView, PointerEventData eventData)
        {
            if (draggedCard != cardView)
            {
                return;
            }

            Vector3 anchorPosition = eventData != null
                ? eventData.position
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
            if (draggedCard != cardView)
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
            dragWasResolved = false;
            draggedStack.Clear();
            draggedStackOffsets.Clear();
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

        public void RestartLevel()
        {
            if (levelDefinition == null)
            {
                Debug.LogError("GameManager: no LevelDefinition assigned.");
                return;
            }

            ClearBoard();
            completedAssociations.Clear();
            levelFinished = false;
            draggedCard = null;
            dragWasResolved = false;
            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
                levelCompleteRoutine = null;
            }

            winScreenView?.HideInstant();

            int? targetColumnsCount = progressionManager != null ? progressionManager.CurrentTargetColumns : null;
            int layoutVariantIndex = progressionManager != null ? progressionManager.CurrentLayoutVariantIndex : 0;
            int levelSeed = progressionManager != null ? progressionManager.GetOrCreateSeedForCurrentLevel() : Random.Range(1, int.MaxValue);
            if (!LevelGenerator.TryGenerate(levelDefinition, cardDatabase, targetColumnsCount, layoutVariantIndex, levelSeed, out generatedLevelData))
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
            if (coinsManager != null)
            {
                uiManager.UpdateCoins(coinsManager.CurrentCoins);
            }

            ResolveBoardState(false);
            Debug.Log($"GameManager: Started level {progressionManager?.CurrentLevelNumber ?? 1} with seed {levelSeed}.");
            platformService?.TrackEvent("level_restart");
        }

        private void ApplyLayout()
        {
            int configuredColumnsCount = generatedLevelData != null
                ? generatedLevelData.ColumnsCount
                : levelDefinition != null
                    ? levelDefinition.ColumnsCount
                    : 4;
            int visibleColumnsCount = Mathf.Clamp(configuredColumnsCount, 3, 5);
            ColumnLayoutProfile profile = NormalizeGameplayProfile(layoutConfig.GetProfile(visibleColumnsCount), visibleColumnsCount);

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

        private static ColumnLayoutProfile NormalizeGameplayProfile(ColumnLayoutProfile profile, int visibleColumnsCount)
        {
            Vector2 targetCardSize = visibleColumnsCount switch
            {
                3 => new Vector2(196f, 286f),
                4 => new Vector2(186f, 272f),
                _ => new Vector2(170f, 248f)
            };

            float targetSpacing = visibleColumnsCount switch
            {
                3 => 112f,
                4 => 76f,
                _ => 48f
            };

            float targetVerticalOffset = visibleColumnsCount switch
            {
                3 => 48f,
                4 => 44f,
                _ => 40f
            };

            profile.columnCount = visibleColumnsCount;
            profile.cardSize = targetCardSize;
            profile.columnSpacing = Mathf.Max(profile.columnSpacing, targetSpacing);
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
            bool wasCreated = hasRequiredCountOverride
                ? cardDatabase.TryCreateCardInstance(cardId, requiredCountOverride, out cardData)
                : cardDatabase.TryCreateCardInstance(cardId, out cardData);

            if (!wasCreated)
            {
                Debug.LogWarning($"GameManager: card id '{cardId}' was not found in CardDatabase.");
                return;
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

            Dictionary<CardView, Vector3> startPositions = CaptureMoveStartPositions(cardView);
            foreach (CardView stackCard in draggedStack)
            {
                if (stackCard != null)
                {
                    stackCard.SetDragDetached(false);
                }
            }

            if (sourceContainer is CardColumn sourceColumn && targetContainer is CardColumn targetColumn)
            {
                List<CardView> movingStack = sourceColumn.RemoveStackStartingAt(cardView);
                if (movingStack.Count > 0)
                {
                    targetColumn.AddCards(movingStack);
                }
                else
                {
                    sourceContainer.RemoveCard(cardView);
                    targetContainer.AddCard(cardView);
                }
            }
            else if (sourceContainer is CardColumn goalSourceColumn && targetContainer is GoalSlot targetGoalSlot)
            {
                List<CardView> movingStack = goalSourceColumn.RemoveStackStartingAt(cardView);
                if (movingStack.Count > 0)
                {
                    targetGoalSlot.AddCards(movingStack);
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

            platformService?.TrackEvent("card_move");
            ResolveBoardState(true);
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
            if (targetContainer is not GoalSlot)
            {
                return true;
            }

            List<CardView> stack = draggedStack.Count > 0
                ? draggedStack
                : BuildDragStack(cardView);

            if (targetContainer is GoalSlot targetGoalSlot)
            {
                return targetGoalSlot.CanAcceptStack(stack);
            }

            return stack.Count == 1 && (cardView.Container == null || cardView.Container.IsTopCard(cardView));
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

        private void CompleteLevel()
        {
            if (levelFinished)
            {
                return;
            }

            levelFinished = true;
            uiManager.ShowStatus("\u0423\u0440\u043e\u0432\u0435\u043d\u044c \u043f\u0440\u043e\u0439\u0434\u0435\u043d", new Color(0.76f, 0.96f, 0.74f, 1f));
            global::MusicPlayer.Instance?.PlayWin();
            winScreenView?.Show(GetUsedMovesCount());
            platformService?.TrackEvent("level_win");

            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
            }

            levelCompleteRoutine = StartCoroutine(AdvanceToNextLevelAfterDelay());
        }

        private IEnumerator AdvanceToNextLevelAfterDelay()
        {
            yield return new WaitForSeconds(levelCompleteDelay);
            progressionManager?.AdvanceToNextLevel();
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

        private void ReturnToMainMenu()
        {
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
                stackCard.SetRaycastBlocks(stackCard.Container is not GoalSlot);
                stackCard.SetDraggingVisual(false);
            }
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

                float score = GetLooseDropScore(goalSlot.DropAreaRect, screenPoint, eventCamera, LooseGoalDropPadding);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestGoalSlot = goalSlot;
            }

            return bestGoalSlot;
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
                    int coinsAward = wasNewCompletion ? AwardGoldCategoryCoins() : 0;
                    if (wasNewCompletion)
                    {
                        goalSlot.PlayCompleteFeedback();
                        global::MusicPlayer.Instance?.PlayCardComplete();
                        uiManager.PlayGoalCompletedFeedback(goalSlot.transform as RectTransform, coinsAward);
                    }

                    RemoveResolvedGoalSlot(goalSlot);
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
                uiManager.ShowStatus("Ходы закончились", new Color(1f, 0.76f, 0.68f, 1f));
                global::MusicPlayer.Instance?.PlayLose();
                platformService?.TrackEvent("level_lose");
            }
        }

        private void RemoveResolvedGoalSlot(GoalSlot goalSlot)
        {
            while (goalSlot.TopCard != null)
            {
                CardView topCard = goalSlot.TopCard;
                goalSlot.RemoveCard(topCard);
                spawnedCards.Remove(topCard);
                Destroy(topCard.gameObject);
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
            uiManager.UpdateMoves(remainingMoves);
            uiManager.UpdateGoalsSummary(completedAssociations.Count, objectiveOrder.Count);
            uiManager.UpdateProgress(BuildObjectiveStates());
            uiManager.UpdateDeckCounter(drawSlot != null ? drawSlot.QueuedCardsCount : 0);
            if (coinsManager != null)
            {
                uiManager.UpdateCoins(coinsManager.CurrentCoins);
            }

            if (!levelFinished)
            {
                uiManager.ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            }
        }

        private int AwardGoldCategoryCoins()
        {
            if (coinsManager == null)
            {
                return 0;
            }

            int award = coinsManager.AddGoldCategoryReward();
            uiManager.PlayCoinsAward(coinsManager.CurrentCoins, award);
            platformService?.TrackEvent("coins_awarded_gold_category");
            return award;
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

            if (winScreenView == null)
            {
                winScreenView = FindFirstObjectByType<WinScreenView>(FindObjectsInactive.Include);
            }

            winScreenView?.Configure(RetryCurrentLevel, ReturnToMainMenu);

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
