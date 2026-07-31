# Architecture

## Основные папки

| Папка | Назначение | Комментарий |
| --- | --- | --- |
| `Assets/Scripts/` | Runtime C# код gameplay, UI, managers, ads, audio | Основной код игры. |
| `Assets/Scripts/MainMenu/` | Main menu popups/buttons/settings/level selection | Много runtime UI authoring и popup logic. |
| `Assets/Scripts/Ads/` | Ads facade и legacy/quiz-related helpers | Часть классов вне namespace. |
| `Assets/Scripts/Music/` | `MusicPlayer` | Singleton audio player. |
| `Assets/Editor/` | Editor utilities/builders/migrators | Не runtime. Не трогать при gameplay задачах без причины. |
| `Assets/Scenes/` | Gameplay scene `Main.unity` | В Build Settings включена `Assets/Scenes/Main.unity`. |
| `Assets/Prefabs/UI/` | UI prefabs | Есть gameplay HUD/premium event prefabs. |
| `Assets/Resources/Data/` | `cards.json` | Основная база категорий и карточек. |
| `Assets/Resources/GeneratedLevels/` | `level_manifest.json`, usage stats | Предсгенерированные уровни и статистика. |
| `Assets/Resources/ContentPacks/` | pack/card/matching metadata | Данные контентного pipeline. |
| `Assets/Resources/ImageCards/Premium/` | Premium image cards PNG | 402 PNG, из них 2 помечены unused в validation. |
| `Assets/Resources/UI/` | Runtime-loaded UI sprites | Premium/RoyalEvent/MainMenu sprites. |
| `Tools/` | Генерация/валидация контента | Python/JS scripts для pipeline и assets. |

## Namespaces

- `CardMesh`: основная runtime-логика, gameplay, data, UI helpers, meta managers.
- `CardMesh.Editor`: `PrototypeLevelFactory`.
- Global namespace: `MainMenuController`, `SceneLoader`, `MusicPlayer`, `AdsManager`, ads helper classes, `ShareButton`, `SoundToggleButton`. Это повышает риск конфликтов и усложняет dependency discovery.

## Основные слои

- Data/config layer: `CardData`, `GoldCardData`, `LevelDefinition`, `GeneratedLevelData`, `LevelManifest*`, JSON/resources.
- Gameplay/domain layer: `GameManager`, `LevelGenerator`, `CardColumn`, `GoalSlot`, `DrawSlot`, `ICardContainer`.
- Presentation layer: `CardView`, `UIManager`, `CardMeshSceneRefs`, `WinScreenView`, popup controllers.
- Meta/economy layer: `LevelProgressionManager`, `CoinsManager`, `LivesManager`, `PlayerBoostInventory`, `DailyRewardsManager`, `ChestChaseManager`, `CollectionBookManager`.
- Services layer: `PlatformService`, `StubPlatformService`, `AdsManager`, `MusicPlayer`.
- Editor/dev layer: `RuntimeDevTools`, `Assets/Editor/*`, `Tools/*`.

## Gameplay classes

- `GameManager`: центральный orchestrator уровня. Создает уровень, спавнит карты, обрабатывает drag/drop, undo, hints, win/lose, ads, rewards, restart.
- `CardColumn`: контейнер колонок, stacking rules, reveal top card, drop handling.
- `GoalSlot`: foundation/goal slot, принимает gold cards и matching regular cards.
- `DrawSlot`: stock/waste area, draw/recycle, top waste card.
- `CardView`: UI-представление карты, drag handlers, visuals, feedback.
- `LevelGenerator`: fallback generation и construction-based solvable deal.
- `LevelManifestDatabase`: загрузка предсгенерированных уровней.

## UI classes

- `UIManager`: gameplay HUD, buttons, popups, runtime polish, status, lives empty, failed popup, pause/restart wiring.
- `CardMeshSceneRefs`: scene reference resolver/layout normalizer for gameplay scene.
- `RuntimeSceneBootstrapper`: binds manual scene refs and can create runtime card prefab/win screen/event system.
- `WinScreenView`: win overlay/confetti/buttons.
- `PauseMenuPopupController`, `RestartConfirmationPopupController`, `ScenePauseMenuButton`: pause/restart flows.
- `MainMenuController`, `LevelSelectionPopup`, `SettingsPopup`, `DailyRewardsPopup`, `ChestChasePopup`, `CollectionBookPopup`, `ShopPopup`: main menu UX.

## Сохранения

- `LevelProgressionManager`: current/max levels and per-level seeds.
- `CoinsManager`: coins and economy version.
- `LivesManager`: lives and restore timestamp.
- `PlayerBoostInventory`: hints/undos.
- `DailyRewardsManager`, `ChestChaseManager`, `CollectionBookManager`: event/progression state.
- `MusicPlayer`, `SettingsPopup`: audio/settings keys.
- `AdsManager`: last interstitial timestamp.

Все это хранится в `PlayerPrefs`; текущая доска не сохраняется.

## Конфиги

- `Assets/Resources/Levels/Level_1.asset`: `LevelDefinition`, layouts 3/4/5 columns, reference moves, gold groups.
- `Assets/Resources/Data/cards.json`: 50 gold categories and 400 regular cards.
- `Assets/Resources/GeneratedLevels/level_manifest.json`: 1000 generated levels.
- `Assets/Resources/ContentPacks/*.json`: content pack and matching metadata.
- `CardVisualConfig`, `ChestChaseRewardConfig`, `AssociationDefinition`, `CardDefinition`: ScriptableObject types. Фактические asset instances для части этих типов требуют проверки.

## Анимации и звук

- Анимации выполнены вручную через coroutines и `Time.deltaTime`/`Time.unscaledDeltaTime`.
- DOTween/LeanTween в коде не найден.
- Карты: `CardView.AnimateFromWorldPosition`, `PlayShake`, glow/scale feedback.
- Hints/goals: `GameManager.PlayMoveHintAnimation`, `PlayDeckHintAnimation`, `AnimateResolvedGoalCards`, goal cloud.
- UI: `UIJuicyButton`, popup open animations, win confetti, `RoyalEventPulse`.
- Audio: `MusicPlayer` singleton, background tracks and SFX.

## Реклама/монетизация

- `AdsManager`: central facade; Android использует `GoogleMobileAdsProvider`, Editor — mock, остальные production-платформы — unavailable.
- `GameManager`: interstitial after level complete/restart and rewarded +moves.
- `ShopPopup`: demo purchase UI only by log message; real payments not connected.
- `InterstitialCounter`, `InterstitialOnRetryButton`, `SecretQuizButton`: legacy/adjacent ads helpers, not clearly part of main Card Mesh flow.

## Зависимости между системами

- `GameManager` depends on almost everything: UI, platform, layout, card DB, level, scene refs, progression, win screen, coins, lives, boosts, chest chase, ads.
- `UIManager` depends on scene hierarchy names and gameplay manager callbacks.
- `CardMeshSceneRefs` mutates scene layout and auto-adds buttons/components.
- `RuntimeSceneBootstrapper` can create missing runtime structures and attach managers.
- Meta managers use `PlayerPrefs` and often find/create services automatically.
- `MusicPlayer` and `AdsManager` are global singletons.

## Сильная связанность

- Gameplay and economy are coupled in `GameManager` (`coinsManager`, `livesManager`, `boostInventory`, `chestChaseManager`, `adsManager`).
- UI layout and gameplay references are mixed between `UIManager`, `CardMeshSceneRefs` and `RuntimeSceneBootstrapper`.
- Main menu buttons search objects by hardcoded names.
- Several systems create managers on the current context GameObject, which can accidentally duplicate responsibilities.

## Технический долг

- Large god classes: `GameManager` ~2789 lines, `UIManager` ~2819 lines, `DailyRewardsPopup` ~1500 lines, `ChestChasePopup` >1000 lines.
- Empty/no-op public UI methods and placeholder service methods.
- PlayerPrefs keys spread across many classes.
- Runtime-generated UI/sprites/textures can hide missing prefab authoring problems.
- No visible automated gameplay tests.
- Payments and analytics integrations are still stubs; Android ads use the official Google Mobile Ads SDK.

## Карта ключевых классов

| Класс | Путь | Ответственность | Зависимости | Риски |
| ----- | ---- | --------------- | ----------- | ----- |
| `GameManager` | `Assets/Scripts/GameManager.cs` | Центральный gameplay flow уровня | UI, cards, levels, ads, lives, coins, boosts, scene refs | God class, high regression risk |
| `CardView` | `Assets/Scripts/CardView.cs` | Визуал карты, drag callbacks, feedback | `GameManager`, TMP, UI sprites/fonts | Runtime UI mutation, coroutines |
| `CardColumn` | `Assets/Scripts/CardColumn.cs` | Колонка карт, reveal/stack/drop rules | `CardView`, `GameManager`, `ICardContainer` | `SetHighlight` пустой, stack rules хрупкие |
| `GoalSlot` | `Assets/Scripts/GoalSlot.cs` | Goal/foundation slot, completion capacity | `CardView`, `GameManager`, TMP | Header disabled, stack normalization sensitive |
| `DrawSlot` | `Assets/Scripts/DrawSlot.cs` | Stock/waste draw/recycle | `CardView`, `GameManager` | `RemoveAt(0)`, recycle order требует проверки |
| `LevelGenerator` | `Assets/Scripts/LevelGenerator.cs` | Runtime generation/fallback | `LevelDefinition`, `CardDatabase` | Construction solver metadata не равно полноценному runtime solver |
| `LevelManifestDatabase` | `Assets/Scripts/LevelManifest.cs` | Load generated level manifest | `Resources`, `JsonUtility` | Static cache, parse failures silent-ish |
| `CardDatabase` | `Assets/Scripts/CardDatabase.cs` | Load card data and image sprites | JSON, Resources, atlas/sprites | Runtime sprite creation, resource path dependency |
| `UIManager` | `Assets/Scripts/UIManager.cs` | Gameplay HUD and modals | Scene names, TMP, sprites, `GameManager` | Very large, creates/destroys runtime UI |
| `CardMeshSceneRefs` | `Assets/Scripts/CardMeshSceneRefs.cs` | Scene refs and layout normalization | Canvas hierarchy names | ExecuteAlways can mutate scene in editor |
| `RuntimeSceneBootstrapper` | `Assets/Scripts/RuntimeSceneBootstrapper.cs` | Runtime scene binding/fallback objects | Scene refs, fonts, event system | Can mask missing authored setup |
| `LevelProgressionManager` | `Assets/Scripts/LevelProgressionManager.cs` | Level progress and seeds | `PlayerPrefs`, manifest | No session state |
| `CoinsManager` | `Assets/Scripts/CoinsManager.cs` | Coins/economy version | `PlayerPrefs` | Economy reset on version bump |
| `LivesManager` | `Assets/Scripts/LivesManager.cs` | Lives and timed restore | `PlayerPrefs`, time | Editor reset hook, clock manipulation risk |
| `PlayerBoostInventory` | `Assets/Scripts/PlayerBoostInventory.cs` | Hint/undo inventory | `PlayerPrefs` | ExtraMoves reward not integrated |
| `AdsManager` | `Assets/Scripts/Ads/AdsManager.cs` | Ads facade | AdMob/UMP provider, Editor mock, PlayerPrefs, AudioListener | Production AdMob IDs and Play declarations pending |
| `MusicPlayer` | `Assets/Scripts/Music/MusicPlayer.cs` | Music/SFX singleton | AudioSource, PlayerPrefs | Global singleton, Update polling |
| `MainMenuController` | `Assets/Scripts/MainMenuController.cs` | Main menu navigation/style/wiring | SceneManager, managers, GameObject names | Hardcoded names and scene fallback |
