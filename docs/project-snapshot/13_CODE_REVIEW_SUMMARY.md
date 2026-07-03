# Code Review Summary

## Архитектура

Проект имеет рабочий MVP-level architecture, но runtime responsibilities сильно перемешаны. `GameManager` и `UIManager` являются главными центрами сложности. Data/config слой отделен частично: уровни и карты лежат в JSON/ScriptableObject, но экономика, reward balance и UI layout в значительной степени захардкожены.

## Читаемость

Плюсы:

- Многие методы названы понятно.
- Gameplay flow можно проследить сверху вниз в `GameManager`.
- Data classes простые.

Минусы:

- Большие файлы с несколькими responsibilities.
- Много hardcoded hierarchy names and pixel constants.
- Legacy/global namespace classes затрудняют поиск ownership.
- Runtime UI authoring делает сцену и код трудно разделимыми.

## SOLID

- Single Responsibility нарушен в `GameManager`, `UIManager`, popup classes.
- Open/Closed слабый: изменения UI/ads/economy требуют править central classes.
- Dependency Inversion почти отсутствует: зависимости задаются через serialized refs, `FindFirstObjectByType`, singletons and auto-add.
- Interface есть для `ICardContainer`, это полезное локальное разделение.

## Связанность

Сильная связанность:

- Gameplay <-> UI <-> economy <-> ads <-> progression in `GameManager`.
- Scene hierarchy names <-> runtime UI code.
- PlayerPrefs keys spread across managers.
- Ads/audio affect global state.

## Тестируемость

Низкая/средняя:

- Core rules можно тестировать, но текущий код завязан на MonoBehaviour/UI.
- `LevelGenerator` and data classes более тестопригодны.
- `GameManager` трудно тестировать без сцены.
- Automated tests не найдены.

## Расширяемость

Добавлять новые card rules, save system, meta events or monetization сейчас рискованно из-за central coupling. Без предварительного тестового покрытия лучше делать малые точечные изменения.

## Производительность

Для MVP вероятно приемлемо, но для mobile release есть риски:

- runtime instantiate/destroy;
- no pooling;
- UI rebuild/material allocation;
- Resources loading;
- LINQ/list allocations in gameplay methods.

## Мобильная пригодность

Needs Work:

- Safe area not confirmed.
- Fixed pixel layout constants.
- Ads provider absent for Android/iOS.
- No profiler/device evidence.

## Риски поддержки

- Любая правка `GameManager` может затронуть gameplay, rewards, ads and navigation.
- UI behavior может отличаться между authored scene and runtime-created fallback.
- Save migrations difficult due distributed PlayerPrefs keys.
- Legacy code from other quiz/game flows can confuse release behavior.

## Сильные стороны проекта

- Основной gameplay loop уже есть.
- 1000 generated levels with solver metadata.
- Card/content data separated into JSON/resources.
- Есть undo and hints.
- Есть main menu and meta systems.
- Runtime fallback/bootstrap makes scene less brittle for development.
- Visual polish effort already significant.

## Слабые стороны проекта

- God classes and code-authored UI.
- Нет session save.
- No tests.
- No real mobile monetization/analytics evidence.
- Safe area/device readiness not proven.
- Config/balance scattered in code.

## Самые опасные места

- `GameManager.ExecuteMove`, `ResolveBoardState`, `RestoreBoardSnapshot`.
- `CardColumn.GetMovableStack`.
- `GoalSlot.CanAcceptStack`.
- `DrawSlot.RecycleRevealedCardsToStock`.
- `UIManager.Initialize` and `ApplyGameplayPolish`.
- `RuntimeSceneBootstrapper.BindManualScene`.
- PlayerPrefs migration/economy keys.

## Что нельзя трогать без понимания

- Stack order contract between `CardColumn`, `GameManager.NormalizeStackForGoalSlot`, `GoalSlot`.
- Manifest level schema and `GeneratedLevelData`.
- `PlayerBoostInventory`/hint/undo costs.
- Restart/lives/ad callback flow.
- Scene hierarchy names used by `SolitaireSceneRefs`/`UIManager`.

## Что стоит улучшить первым

1. Tests for card movement and goal rules.
2. Release-gate `RuntimeDevTools`.
3. Fix or explicitly disable incomplete reward integrations.
4. Safe area implementation and device QA.
5. Decide session save requirements.

## Что можно оставить на потом

- Full architecture refactor.
- Full UI system rewrite.
- Pooling every effect.
- Cloud save.
- More advanced hint solver.
- Redo support, unless product requires it.

