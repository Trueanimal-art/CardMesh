# Performance

Это статический обзор по коду. Профайлер Unity не запускался, FPS/GC measurements отсутствуют.

## Лишние аллокации

Потенциальные источники:

- `GameManager.GetActiveColumns` / `GetActiveGoalSlots`: LINQ `Where(...).ToList()` на каждом вызове.
- `GameManager.FindBestHintMove`: создает `List<HintMove>`, сортирует через LINQ `OrderBy`.
- `GameManager.CaptureBoardSnapshot`: создает списки/словарь/HashSet для undo на каждый успешный ход/draw.
- `CardColumn.GetCardsSnapshot`, `DrawSlot.GetQueuedCardsSnapshot`, `GoalSlot.GetCardsSnapshot`: создают новые списки.
- `LevelGenerator`: много temporary lists/dictionaries при generation.
- UI popup classes: frequent `GetComponentsInChildren`, runtime hierarchy rebuild.
- `CardDatabase.CreateTrimmedTransparentSprite`: creates textures/sprites for atlas fallback.

## Возможные GC spikes

- Restart level: destroy old cards, instantiate all cards again.
- First load of image cards: `Resources.Load<Sprite>` and possible atlas sprite generation.
- Win screen: confetti pieces creation/destruction.
- Goal completion: cloud GameObject and card animation lists.
- UIManager polish: creates/removes UI components and materials.
- `ApplyGameplayButtonTextStyle`: creates new TMP Material instances.
- Main menu popup open/refresh: large UI traversal and creation.

## Update / LateUpdate / FixedUpdate

Найденные runtime Update:

- `LivesManager.Update`: gated to once per second via `nextRefreshAt`.
- `MusicPlayer.Update`: checks whether music track ended.
- `RoyalEventPulse.Update`: per-frame pulse for each component.
- `RuntimeDevTools.Update`: handles dev panel/runtime state.

`FixedUpdate` / `LateUpdate` in project scripts не обнаружены.

## LINQ в runtime-логике

- `GameManager` uses `System.Linq`.
- `FindBestHintMove` uses `OrderBy(...).FirstOrDefault()`.
- `GetActiveColumns` and `GetActiveGoalSlots` allocate lists via `Where(...).ToList()`.

Это не обязательно критично при малом числе колонок, но стоит убрать из hot paths перед mobile release.

## Создание объектов во время игры

- All cards are instantiated on each `RestartLevel`.
- Cards are destroyed on `ClearBoard`.
- Ads manager may be created at runtime by `GameManager.ResolveAdsManager`.
- EventSystem may be created by bootstrap/helpers.
- UI modals, sparkles, confetti, floating text, rounded sprites, vignettes, heart icon can be runtime-created.
- Manager singletons may be auto-created on current context GameObject.

## Object pooling

Object pooling не найден.

Candidates for pooling:

- `CardView` instances.
- Win confetti.
- Goal sparkles/cloud.
- Floating coin/text feedback.
- Runtime modal objects if repeatedly opened.

## UI rebuild risks

- Manual `RectTransform` changes across many components.
- `LayoutRebuilder.ForceRebuildLayoutImmediate` in editor preview.
- Frequent `ForceMeshUpdate`.
- Runtime adding/removing `Outline`, `Shadow`, TMP components.
- Large popups with many child elements and recursive searches.
- `Canvas` separation not clear; all effects may rebuild shared canvas.

## Android/iOS risks

- No mobile ad provider visible for Android/iOS.
- No `Screen.safeArea` handling found.
- Heavy runtime UI creation can stutter on lower-end devices.
- Premium image resources are loaded from `Resources`, potentially larger memory footprint.
- PlayerPrefs frequent `Save()` after many economy/meta actions.
- Audio/ads muting uses global `AudioListener.pause`, which can affect all audio.

## Места для первичного профилирования

1. `GameManager.RestartLevel` with 5-column high-card levels.
2. First image-card level load and `CardDatabase.ResolveImageCardSprite`.
3. Drag/drop flow with stack moves and undo snapshots.
4. Goal completion and win screen.
5. Main menu popup opening: Daily Rewards, Chest Chase, Shop, Collection Book.
6. Runtime UI polish in `UIManager.Initialize`.

## Приоритеты оптимизации

1. Критично до релиза

- Проверить `RuntimeDevTools` отключение в production.
- Профилировать restart/load of largest levels.
- Убрать/ограничить runtime UI recreation в gameplay hot path.
- Проверить mobile safe area и canvas rebuild spikes.
- Подтвердить memory footprint `Resources/ImageCards/Premium`.

2. Желательно до релиза

- Pool cards and common feedback objects.
- Replace LINQ list allocations in `GameManager` hot paths.
- Cache active columns/goal slots per layout instead of rebuilding lists.
- Reduce `PlayerPrefs.Save()` frequency where safe.
- Cache TMP materials rather than creating many material instances.

3. Можно после релиза

- Split `UIManager` into smaller components.
- Move economy/reward configs to assets.
- Replace runtime-generated rounded sprites with authored sprites.
- Add profiler markers around generation, restart, UI init and win flow.

