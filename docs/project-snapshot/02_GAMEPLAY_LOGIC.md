# Gameplay Logic

## Общая модель

Игра использует не стандартную 52-карточную колоду. Уровень состоит из набора card ids: золотые category/goal cards и обычные cards той же ассоциации. Обычные карты могут быть текстовыми или image-картами. Игрок должен поместить goal card в goal slot и собрать под нее нужное число обычных карт того же типа.

Основной класс: `GameManager`.

Вспомогательные классы:

- `CardDatabase`
- `LevelManifestDatabase`
- `LevelGenerator`
- `CardColumn`
- `GoalSlot`
- `DrawSlot`
- `CardView`
- `ICardContainer`

## Как создается колода/уровень

Flow `GameManager.RestartLevel`:

1. Очищает текущую доску через `ClearBoard`.
2. Определяет текущий уровень из `LevelProgressionManager` или dev override.
3. Пытается взять уровень из `LevelManifestDatabase.TryCreateGeneratedLevelData`.
4. Если manifest недоступен, вызывает `LevelGenerator.TryGenerate`.
5. Устанавливает `remainingMoves = generatedLevelData.ReferenceMoves`.
6. Применяет layout 3/4/5 колонок.
7. Загружает card data через `BuildLevelData`.
8. Создает карты через `CreateStartingCards`.
9. Инициализирует UI и вызывает `ResolveBoardState(false)`.

`LevelManifest` содержит 1000 уровней. По metadata: solved 1000/1000, solvedRate 100%, layouts `layout_3_cols`, `layout_4_cols_v1/v2/v3`, `layout_5_cols_v1/v2`.

## Как происходит раздача карт

Метод `CreateStartingCards`:

- Берет `generatedLevelData.Columns`.
- Для каждой active column создает `CardView` из `cardPrefab`.
- Каждая карта получает `CardData`, `visualConfig`, owner `GameManager`, `dragLayer`.
- В колонках все карты сначала face down.
- После добавления вызывается `CardColumn.RevealOnlyTopCard`, открывающий только верхнюю карту.
- Stock cards создаются отдельно из `generatedLevelData.StockCards`, добавляются в `DrawSlot.SetStockCards` и изначально неактивны.

## Колонки/стопки

Класс: `CardColumn`.

Правила:

- Верхняя карта: `TopCard`.
- Пустая колонка принимает только не-gold карту.
- Непустая колонка принимает карту/стопку, если top card не gold, association совпадает, а text/image type совместим.
- Карты в player-created stack помечаются `IsPlayerStackedOnPrevious`.
- `GetMovableStack` строит переносимую стопку из face-up карт одной association/type.
- Для gold карты на вершине колонка может включить связанные обычные карты ниже gold, если они помечены как stack.
- После layout верхняя карта раскрыта, скрытые/peek cards получают history layout.

Ключевые методы:

- `AddCard`, `AddCards`, `AddCardsFromPlayerMove`
- `RemoveStackStartingAt`
- `GetMovableStack`
- `CanAccept`
- `RevealOnlyTopCard`
- `RefreshLayout`
- `OnDrop`

Хрупкие места:

- `SetHighlight` пустой, поэтому hover feedback колонок не работает.
- Stack rules завязаны на `IsPlayerStackedOnPrevious`; ошибка в restore/move может сломать переносимые стопки.
- Layout рассчитывает safe vertical offset от размеров RectTransform; требуется проверка на маленьких экранах.

## Foundation / goal piles

Класс: `GoalSlot`.

Правила:

- Пустой goal slot принимает gold card или gold-headed stack.
- Если goal уже содержит gold card, он принимает только regular cards той же association.
- Image goal принимает только image cards; text goal принимает только text cards.
- `RemainingRegularCapacity` ограничивает число обычных карт по `RequiredCount`.
- `CanAcceptStack` проверяет всю переносимую стопку.
- Если переносимая колонная стопка содержит gold не первым элементом, `GameManager.NormalizeStackForGoalSlot` переносит gold в начало списка goal slot.

Completion:

- `GameManager.ResolveBoardState` смотрит каждый active `GoalSlot`.
- Если `RegularCardsCount >= goalCard.RequiredCount`, association добавляется в `completedAssociations`.
- Затем `RemoveResolvedGoalSlot` удаляет карты из goal slot, отключает их и запускает collapse/cloud animation.
- Победа наступает, когда `completedAssociations.Count == objectiveOrder.Count`.

Хрупкие места:

- `GoalSlot.RefreshHeader` отключает `headerRoot` и очищает тексты. Фактический category label поддерживает `CardMeshSceneRefs.SetGoalLabel`.
- В goal slot визуально активной остается только top card, а progress берется из `GoalCard.Data`; эту связку нужно проверять при изменениях UI.

## Waste / stock

Класс: `DrawSlot`.

Flow:

1. Клик по `DrawSlot` вызывает `GameManager.DrawNextStockCard`.
2. `DrawNextStockCard` делает snapshot доски.
3. `DrawSlot.DrawNextCard` берет `queuedCards[0]`, удаляет его и добавляет в `revealedCards`.
4. Карта активируется, становится face up и получает контейнер `DrawSlot`.
5. Только top revealed card блокирует raycasts и может быть перетянута.
6. Если queued пуст, а revealed есть, `RecycleRevealedCardsToStock` переносит revealed обратно в queued.

Риски:

- `queuedCards.RemoveAt(0)` может быть O(n), но размер stock умеренный.
- Порядок recycle требует проверки gameplay-дизайном.
- В UI видны максимум 3 revealed cards.

## Возможность перемещения карты

Основная проверка: `GameManager.CanMoveCard`.

Условия:

- card и target не null.
- У карты есть текущий контейнер.
- target не равен текущему контейнеру.
- `CanMoveStackTo` разрешает перенос стопки в target.
- `targetContainer.CanAccept(cardView)` подтверждает правила контейнера.

Для goal slot дополнительно вызывается `GoalSlot.CanAcceptStack`.

## Drag & drop

Основные методы:

- `CardView.OnBeginDrag`
- `GameManager.BeginDrag`
- `GameManager.BeginDragVisual`
- `CardView.OnDrag`
- `GameManager.UpdateDragVisual`
- `CardView.OnEndDrag`
- `GameManager.EndDrag`
- `CardColumn.OnDrop`
- `GoalSlot.OnDrop`
- `GameManager.TryResolveLooseDrop`

Flow:

1. `CardView.OnBeginDrag` просит `GameManager.BeginDrag`.
2. `BeginDrag` запрещает drag из `GoalSlot`, на завершенном уровне и при активном другом drag.
3. `BuildDragStack` находит переносимую стопку.
4. `BeginDragVisual` переносит карты в `dragLayer`, отключает raycasts, запоминает offsets.
5. `OnDrop` целевого контейнера пытается выполнить `TryMoveDraggedCardTo`.
6. Если drop handler не сработал, `EndDrag` пробует loose drop по padded rects goal/column.
7. При успешном move вызывается `ExecuteMove`.
8. При неуспешном move карта возвращается layout refresh и получает shake feedback.

## Tap/click перемещение

Реализован click по deck/stock (`DrawSlot.OnPointerClick`). Tap/click auto-move карты в goal/column в коде не обнаружен. Требует проверки в сцене, но по `CardView` есть только drag handlers.

## Победа

`ResolveBoardState` после каждого move/draw:

- обновляет goal progress;
- удаляет завершенные goal slots;
- обновляет UI;
- если все objectives завершены, вызывает `CompleteLevel`.

`CompleteLevel`:

- ставит `levelFinished` и `levelCompletionInProgress`;
- отключает input и gameplay buttons;
- запускает `EndLevelRewardSequence`.

`FinishLevelCompletion`:

- показывает статус "Уровень пройден";
- вызывает `chestChaseManager.RegisterLevelWon`;
- начисляет coins через `AwardLevelWinCoins`;
- играет win sound;
- показывает `WinScreenView`;
- запускает `AdvanceToNextLevelAfterDelay`.

После delay возможен interstitial, затем `LevelProgressionManager.AdvanceToNextLevel` и `RestartLevel`.

## Lose condition

Если после расхода хода `remainingMoves <= 0`, `ResolveBoardState`:

- ставит `levelFinished = true`;
- вызывает `chestChaseManager.RegisterLevelLost`;
- показывает статус "Ходы закончились";
- играет lose sound;
- показывает failed popup с retry callback;
- отправляет event `level_lose`.

Retry/restart тратит жизнь через `LivesManager.SpendLife`.

## Undo / redo

Undo реализован. Redo не обнаружен.

Механика:

- Перед успешным move/draw создается `BoardSnapshot`.
- После успешного действия snapshot пушится в `moveHistory`.
- `TryUseUndo` восстанавливает последний snapshot через `RestoreBoardSnapshot`.
- Undo расходует заряд `PlayerBoostInventory.UndoCount`.
- Dev undo не расходует заряд.

Snapshot хранит:

- списки карт в columns/goal slots;
- queued/revealed draw cards;
- completed associations;
- face up / active / stacked state каждой spawned card;
- remaining moves.

Ограничения:

- Undo history живет только в памяти.
- Snapshot держит ссылки на `CardView`, не сериализуется.
- Нет redo stack.

## Hint

Класс/методы: `GameManager.TryUseHint`, `FindBestHintMove`, `AddHintCandidatesForCard`.

Flow:

1. Проверяет, что уровень не завершен и нет drag/hint animation.
2. Расходует hint boost; если boostов нет, пытается списать 900 coins.
3. Собирает кандидаты из active columns и top draw card.
4. Приоритеты: move to goal, reveal card, move between columns.
5. Запускает animation подсказки, но не выполняет ход.
6. Если ходов не найдено, анимация указывает на deck.

Риски:

- Подсказка не использует полноценный solver.
- Если нет доступного move и deck пуст, поведение требует ручной проверки.

## Автоперемещения

Автоматическое перемещение карт по tap не найдено. Автоматически выполняется только:

- удаление completed goal slots;
- начисление rewards;
- авто-переход на следующий уровень после win delay.

## Edge cases

- Перетаскивание во время hint запрещено.
- Перетаскивание из `GoalSlot` запрещено.
- Drag может завершиться через drop handler или loose drop fallback.
- Restart имеет lock `RestartLifeSpendLockSeconds`, чтобы не списать жизнь дважды.
- При `levelFinished` rewarded +moves блокируется, кроме dev grant с revive flag.
- Goal completion может обрабатывать несколько slots в loop до стабилизации.
- Manifest level fallback: если manifest entry недоступен, используется runtime generator.
- Scene navigation ищет несколько имен/путей, но build settings содержат только MainMenu и Main.

## Хрупкие места

- Слишком много gameplay state хранится в `GameManager`.
- `completedAssociations` и `objectiveOrder` должны оставаться синхронными с generated data.
- `NormalizeStackForGoalSlot` меняет порядок переносимой стопки для goal slot.
- `RestoreBoardSnapshot` должен корректно вернуть parent/active/raycast/face-up state.
- `GoalSlot` visual state зависит от card order и `GoalCard == cards[0]`.
- Нет persist save для середины уровня.

