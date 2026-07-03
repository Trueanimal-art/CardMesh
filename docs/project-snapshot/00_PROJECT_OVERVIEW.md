# Project Overview

## Что это за игра

Проект выглядит как мобильная/веб карточная игра CardMesh / Solitaire Associations, а не классический Klondike Solitaire. Игрок работает с колонками карт, stock/deck-слотом и goal/foundation слотами. Цель уровня: собрать обычные карты одной категории в соответствующую золотую goal-карту. Карты бывают текстовые и image-карты; image goal не должен смешиваться с text-картами.

Основные сцены в Build Settings:

- `Assets/MainMenu.unity`
- `Assets/Scenes/Main.unity`

Unity версия проекта: `2022.3.43f1`.

## Стадия разработки

Состояние похоже на playable MVP / production prototype:

- Есть основной gameplay loop: старт уровня, раздача, drag & drop, stock, goal completion, win/lose, рестарт, автопереход на следующий уровень.
- Есть 1000 предсгенерированных уровней в `Assets/Resources/GeneratedLevels/level_manifest.json`.
- Есть main menu и несколько liveops/мета-попапов: Daily Rewards, Chest Chase, Collection Book, Shop, Settings, Level Selection.
- Есть mock-реклама и WebGL/Yandex provider path.
- Сохранение сессии доски не реализовано.
- Реальная мобильная монетизация, аналитика, платежи и store readiness не выглядят завершенными.

## Основные игровые режимы

- Main Menu: запуск последнего выбранного уровня, выбор уровня, настройки, магазин, Daily Rewards, Chest Chase, Collection Book.
- Gameplay level: 3, 4 или 5 активных колонок, stock/deck, goal slots, ограничение ходов.
- Generated levels: текущий flow предпочитает `LevelManifestDatabase` для уровней из `level_manifest.json`, fallback - runtime `LevelGenerator`.
- Development tools: `RuntimeDevTools` может подгружать уровни, менять число колонок, добавлять ходы/монеты и смотреть image cards. Требует проверки в релизных билдах.

## Уже реализованные ключевые системы

- Card database из `Assets/Resources/Data/cards.json`.
- Content packs и image-card ресурсы в `Assets/Resources/ContentPacks/` и `Assets/Resources/ImageCards/Premium/`.
- Level manifest на 1000 уровней, 100% solverSolved по metadata.
- Runtime level generation fallback.
- Gameplay board: `GameManager`, `CardColumn`, `GoalSlot`, `DrawSlot`, `CardView`.
- Drag & drop и loose drop fallback.
- In-memory undo через снимки доски.
- Hint animation с поиском лучшего доступного хода.
- Moves counter, win/lose flow, restart with lives.
- Coins, lives, boosts, daily rewards, chest chase, collection book state.
- Main menu popups и gameplay HUD.
- Audio/SFX через `MusicPlayer`.
- Ads facade через `AdsManager`.

## Системы, которые выглядят незавершенными

- Сохранение текущей игровой сессии: нет сериализации доски, stock/waste, undo history и remaining moves после выхода.
- Analytics: `PlatformService.TrackEvent` пустой, `StubPlatformService` без реализации.
- Store purchases: `ShopPopup` логирует demo UI, платежи не подключены.
- Ads на Android/iOS: provider есть только mock и Yandex WebGL path. Требует проверки.
- Collection Book: менеджер и popup есть, но начисление очков из `GameManager` не видно.
- UI goals/progress: `UIManager.UpdateGoalsSummary` и `UIManager.UpdateProgress` пустые.
- Column hover highlight: `CardColumn.SetHighlight` пустой.
- GoalSlot internal header создается, но `RefreshHeader` очищает и выключает его; фактический label идет через `SolitaireSceneRefs`.
- Safe area: объект называется `SafeAreaRoot`, но применения `Screen.safeArea` не видно.
- Automated tests отсутствуют или не обнаружены в `Assets/Scripts`.

## Главные технические риски

- `GameManager` очень большой и совмещает gameplay, генерацию уровня, input, анимации, undo, economy, ads и navigation.
- `UIManager` и popup-классы активно создают/перестраивают UI в runtime, что усложняет scene/prefab ownership.
- Много связей через `FindFirstObjectByType`, singletons и автодобавление компонентов.
- PlayerPrefs используется как единое хранилище для экономики, событий, прогресса и рекламы без общей схемы/миграций.
- Нет object pooling для карт, confetti, sparkles и runtime UI.
- Worktree уже содержит много изменений в коде/ассетах; перед дальнейшей работой нужно отделить чужие изменения от новых.

## С чего начать новому разработчику

1. Открыть `ProjectSettings/EditorBuildSettings.asset` и проверить две сцены.
2. Читать `Assets/Scripts/GameManager.cs`.
3. Читать `Assets/Scripts/CardColumn.cs`, `Assets/Scripts/GoalSlot.cs`, `Assets/Scripts/DrawSlot.cs`, `Assets/Scripts/CardView.cs`.
4. Читать `Assets/Scripts/LevelProgressionManager.cs`, `Assets/Scripts/LevelManifest.cs`, `Assets/Scripts/LevelGenerator.cs`.
5. Проверить `Assets/Resources/GeneratedLevels/level_manifest.json` и `Assets/Resources/Data/cards.json`.
6. Затем переходить к `UIManager`, `SolitaireSceneRefs`, `RuntimeSceneBootstrapper` и main-menu popup systems.

