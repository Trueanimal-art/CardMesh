using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string AssociationsResourcePath = "Associations";

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
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private SolitaireSceneRefs sceneRefs;

        private readonly Dictionary<string, string> associationTitlesById = new();
        private readonly Dictionary<string, AssociationDefinition> associationDefinitionsById = new();
        private readonly Dictionary<string, CardData> cardsById = new();
        private readonly Dictionary<string, CardData> goalCardsByAssociation = new();
        private readonly HashSet<string> completedAssociations = new();
        private readonly List<CardView> spawnedCards = new();
        private readonly List<ObjectiveState> objectiveStates = new();
        private readonly List<string> objectiveOrder = new();

        private CardView draggedCard;
        private bool dragWasResolved;
        private int remainingMoves;
        private bool levelFinished;
        private bool dependenciesInitialized;

        public LevelDefinition AssignedLevelDefinition => levelDefinition;
        public bool HasPresentationBindings =>
            uiManager != null &&
            dragLayer != null &&
            cardPrefab != null &&
            columns.Count > 0 &&
            goalSlots.Count > 0 &&
            drawSlot != null;

        private void Awake()
        {
            InitializeDependencies();
        }

        private void Start()
        {
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
            if (levelFinished || drawSlot == null || drawSlot.CardCount <= 0)
            {
                return;
            }

            drawSlot.DrawNextCard();
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

            if (cardView.Container == null || !cardView.Container.IsTopCard(cardView))
            {
                return false;
            }

            draggedCard = cardView;
            dragWasResolved = false;
            return true;
        }

        public bool IsDragging(CardView cardView)
        {
            return draggedCard == cardView;
        }

        public void EndDrag(CardView cardView)
        {
            if (draggedCard != cardView)
            {
                return;
            }

            if (!dragWasResolved && cardView.Container != null)
            {
                cardView.Container.RefreshLayout();
            }

            draggedCard = null;
            dragWasResolved = false;
        }

        public bool TryMoveDraggedCardTo(CardColumn targetColumn)
        {
            return TryMoveDraggedCardToInternal(targetColumn);
        }

        public bool TryMoveDraggedCardTo(GoalSlot targetGoalSlot)
        {
            return TryMoveDraggedCardToInternal(targetGoalSlot);
        }

        public void RestartLevel()
        {
            if (levelDefinition == null)
            {
                Debug.LogError("GameManager: не назначен LevelDefinition.");
                return;
            }

            ClearBoard();
            completedAssociations.Clear();
            remainingMoves = levelDefinition.MovesCount;
            levelFinished = false;
            draggedCard = null;
            dragWasResolved = false;

            ApplyLayout();
            BuildLevelData();
            CreateStartingCards();

            uiManager.Initialize(this, BuildObjectiveStates());
            ResolveBoardState(false);
            platformService?.TrackEvent("level_restart");
        }

        private void ApplyLayout()
        {
            int visibleColumnsCount = columns.Count > 0 ? columns.Count : levelDefinition != null ? levelDefinition.ColumnsCount : 4;
            ColumnLayoutProfile profile = layoutConfig.GetProfile(visibleColumnsCount);

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

            int activeColumnsCount = visibleColumnsCount;
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

        private void BuildLevelData()
        {
            associationTitlesById.Clear();
            associationDefinitionsById.Clear();
            cardsById.Clear();
            goalCardsByAssociation.Clear();
            objectiveOrder.Clear();

            foreach (AssociationDefinition associationDefinition in Resources.LoadAll<AssociationDefinition>(AssociationsResourcePath))
            {
                if (associationDefinition == null || string.IsNullOrWhiteSpace(associationDefinition.AssociationId))
                {
                    continue;
                }

                associationDefinitionsById[associationDefinition.AssociationId] = associationDefinition;
            }

            List<CardDefinition> cardsFromLevel = new();

            foreach (ColumnDefinition columnDefinition in levelDefinition.Columns)
            {
                if (columnDefinition == null)
                {
                    continue;
                }

                foreach (CardDefinition cardDefinition in columnDefinition.CardsInColumn)
                {
                    if (cardDefinition == null)
                    {
                        continue;
                    }

                    cardsFromLevel.Add(cardDefinition);

                    if (!associationTitlesById.ContainsKey(cardDefinition.AssociationId))
                    {
                        associationTitlesById[cardDefinition.AssociationId] = GetAssociationTitle(cardDefinition.AssociationId, cardDefinition.Title);
                    }
                }
            }

            foreach (CardDefinition stockCardDefinition in levelDefinition.StockCards)
            {
                if (stockCardDefinition == null)
                {
                    continue;
                }

                cardsFromLevel.Add(stockCardDefinition);

                if (!associationTitlesById.ContainsKey(stockCardDefinition.AssociationId))
                {
                    associationTitlesById[stockCardDefinition.AssociationId] = GetAssociationTitle(stockCardDefinition.AssociationId, stockCardDefinition.Title);
                }
            }

            foreach (CardDefinition cardDefinition in cardsFromLevel)
            {
                if (string.IsNullOrWhiteSpace(cardDefinition.CardId))
                {
                    Debug.LogWarning("У карты отсутствует cardId.");
                    continue;
                }

                if (cardsById.ContainsKey(cardDefinition.CardId))
                {
                    Debug.LogWarning($"Повторяющийся cardId: {cardDefinition.CardId}");
                    continue;
                }

                string associationTitle = associationTitlesById.TryGetValue(cardDefinition.AssociationId, out string title)
                    ? title
                    : GetAssociationTitle(cardDefinition.AssociationId, cardDefinition.Title);

                CardData cardData = new(cardDefinition, associationTitle);
                cardsById[cardDefinition.CardId] = cardData;

                if (!cardData.IsGold)
                {
                    continue;
                }

                goalCardsByAssociation[cardData.AssociationId] = cardData;
                if (!objectiveOrder.Contains(cardData.AssociationId))
                {
                    objectiveOrder.Add(cardData.AssociationId);
                }
            }
        }

        private void CreateStartingCards()
        {
            List<CardColumn> activeColumns = GetActiveColumns();
            List<CardView> stockCardViews = new();

            for (int columnIndex = 0; columnIndex < activeColumns.Count; columnIndex++)
            {
                if (columnIndex >= levelDefinition.Columns.Count)
                {
                    break;
                }

                ColumnDefinition columnDefinition = levelDefinition.Columns[columnIndex];
                foreach (CardDefinition cardDefinition in columnDefinition.CardsInColumn)
                {
                    if (cardDefinition == null || !cardsById.TryGetValue(cardDefinition.CardId, out CardData cardData))
                    {
                        Debug.LogWarning("В уровне есть пустая или невалидная ссылка на карту.");
                        continue;
                    }

                    Transform cardsRoot = activeColumns[columnIndex].CardsRoot;
                    CardView cardView = Instantiate(cardPrefab, cardsRoot);
                    cardView.gameObject.hideFlags = HideFlags.None;
                    cardView.gameObject.SetActive(true);
                    cardView.name = cardData.Id;
                    cardView.Setup(cardData, this, dragLayer);
                    activeColumns[columnIndex].AddCard(cardView);
                    spawnedCards.Add(cardView);
                    Debug.Log($"GameManager: Spawned card '{cardData.Id}' into {SolitaireSceneRefs.GetPath(cardsRoot)}.");
                }
            }

            foreach (CardDefinition cardDefinition in levelDefinition.StockCards)
            {
                if (cardDefinition == null || !cardsById.TryGetValue(cardDefinition.CardId, out CardData cardData))
                {
                    Debug.LogWarning("В колоде уровня есть пустая или невалидная ссылка на карту.");
                    continue;
                }

                Transform cardRoot = drawSlot.CardRoot;
                CardView cardView = Instantiate(cardPrefab, cardRoot);
                cardView.gameObject.hideFlags = HideFlags.None;
                cardView.gameObject.SetActive(false);
                cardView.name = cardData.Id;
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

            sourceContainer.RemoveCard(cardView);
            targetContainer.AddCard(cardView);
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
                   && targetContainer.CanAccept(cardView);
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

                    completedAssociations.Add(goalCardView.Data.AssociationId);
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
                levelFinished = true;
                uiManager.ShowStatus("Победа!", new Color(0.76f, 0.96f, 0.74f, 1f));
                platformService?.TrackEvent("level_win");
            }
            else if (consumeMove && remainingMoves <= 0)
            {
                levelFinished = true;
                uiManager.ShowStatus("Ходы закончились", new Color(1f, 0.76f, 0.68f, 1f));
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

            if (!levelFinished)
            {
                uiManager.ShowStatus(string.Empty, new Color(1f, 1f, 1f, 0f));
            }
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

            platformService?.Initialize();

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

        private string GetAssociationTitle(string associationId, string fallbackTitle)
        {
            if (!string.IsNullOrWhiteSpace(associationId)
                && associationDefinitionsById.TryGetValue(associationId, out AssociationDefinition associationDefinition)
                && !string.IsNullOrWhiteSpace(associationDefinition.DisplayName))
            {
                return associationDefinition.DisplayName;
            }

            return fallbackTitle;
        }
    }
}
