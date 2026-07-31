# AI Handoff Guide

## Краткое описание проекта

Unity `2022.3.43f1` карточная игра CardMesh / Card Mesh. Игрок собирает обычные карты одной association/type в золотые goal cards. Уровни берутся из generated manifest на 1000 уровней или fallback generator. Есть main menu, meta events, coins/lives/boosts, mock/WebGL ads facade, audio and runtime UI polish.

## Файлы, которые читать в первую очередь

1. `Assets/Scripts/GameManager.cs`
2. `Assets/Scripts/CardColumn.cs`
3. `Assets/Scripts/GoalSlot.cs`
4. `Assets/Scripts/DrawSlot.cs`
5. `Assets/Scripts/CardView.cs`
6. `Assets/Scripts/LevelManifest.cs`
7. `Assets/Scripts/LevelProgressionManager.cs`
8. `Assets/Scripts/UIManager.cs`
9. `Assets/Scripts/CardMeshSceneRefs.cs`
10. `Assets/Resources/GeneratedLevels/level_manifest.json`

## Классы, которые нельзя менять без осторожности

- `GameManager`: центральный state machine уровня.
- `CardColumn`: stack/reveal/drop rules.
- `GoalSlot`: goal acceptance/completion rules.
- `DrawSlot`: stock/waste semantics.
- `CardView`: drag callbacks and visuals.
- `LevelManifestDatabase` and `LevelGenerator`: level determinism.
- `UIManager` and `CardMeshSceneRefs`: scene/prefab UI ownership.
- `LevelProgressionManager`: progression and seed persistence.
- `CoinsManager`, `LivesManager`, `PlayerBoostInventory`: saved economy.

## Самые хрупкие системы

- Drag/drop stack movement with gold-headed stacks.
- Goal completion and removal animation.
- Undo snapshot restore.
- Runtime UI construction and scene reference binding.
- PlayerPrefs state across many managers.
- Ads/restart/lives flow.
- Generated level manifest fallback vs runtime generator.

## Самые важные баги сейчас

- `RuntimeDevTools` release safety.
- No current board save.
- ExtraMoves reward not consumed.
- CollectionBook not clearly wired to gameplay.
- Mobile safe area not implemented.
- Real mobile ads/IAP/analytics missing.
- No tests for core rules.

## Какие задачи делать первыми

1. Stabilization/QA of gameplay rules.
2. Release safety: dev tools, scene list, target platform.
3. Save/session expectations decision.
4. Mobile layout/safe area and profiling.
5. Ads/IAP/analytics only after target platform is clear.

## Какие задачи нельзя делать одновременно

- Не смешивать gameplay rules changes и UI refactor.
- Не менять level generator одновременно с card rules.
- Не менять save keys одновременно с economy/rewards balance.
- Не править ads/restart/lives flow вместе с pause UI refactor.
- Не делать prefab/scene migration одновременно с `UIManager` cleanup.

## Проверки после изменений

- Play level 1, mid-level 4-column, and 5-column level from manifest.
- Drag single regular card to column.
- Drag regular stack to column.
- Drag gold card/stack to empty goal slot.
- Complete goal slot and verify cards removed.
- Draw stock until recycle.
- Use undo after draw and after move.
- Use hint with boost and without boost/coins.
- Lose by running out of moves; retry spends one life.
- Win; verify coins, chest chase, ad flow, next level.
- Return to main menu from pause.
- Reopen main menu popups.

## Сцены открыть для проверки

- `Assets/MainMenu.unity`
- `Assets/Scenes/Main.unity`

Build Settings currently include only these two scenes.

## Build settings проверить

- Scenes in Build match runtime load names/fallbacks.
- Target platform-specific ads symbols/packages.
- `RuntimeDevTools` not present/enabled in release.
- Audio clips assigned to `MusicPlayer`.
- Development Build off for store build.
- Google Mobile Ads package/Android resolver state and production AdMob IDs.
- Orientation/resolution/safe area settings.

## Правила дальнейшей разработки

- Не переписывать архитектуру без необходимости.
- Не менять gameplay rules без явного запроса.
- Не смешивать исправление багов и рефакторинг в одной задаче.
- Перед изменениями читать связанные классы.
- После изменений обновлять документацию в `docs/project-snapshot/`.
- Если найден технический долг - фиксировать его в документации.
- Если задача рискованная - сначала объяснить риск.

## Дополнительные правила для AI-агента

- Рабочее дерево может быть грязным. Не откатывать чужие изменения.
- Не менять сцены/префабы/ScriptableObjects без явного запроса.
- При изменениях gameplay сначала добавить/обновить tests.
- Проверять `rg` по ключу PlayerPrefs перед изменением save keys.
- Не удалять fallback paths в scene loading без проверки build settings.
- Не предполагать, что runtime-created UI отсутствует в сцене. Сначала искать и authored, и generated paths.
- Любое изменение монетизации требует target platform decision.
