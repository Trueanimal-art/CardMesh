# Known Bugs And Current Problems

Список основан на чтении кода и ресурсов. Он не заменяет QA в Unity Editor/device builds.

| ID | Проблема | Где находится | Вероятная причина | Риск | Приоритет |
| -- | -------- | ------------- | ----------------- | ---- | --------- |
| BUG-001 | Текущая доска не сохраняется между сессиями | `GameManager`, save managers | Нет сериализуемого session save | Потеря прогресса уровня | High |
| BUG-002 | `UIManager.UpdateGoalsSummary` пустой | `Assets/Scripts/UIManager.cs` | UI progress не реализован/перенесен | Игрок может не видеть objective summary | Medium |
| BUG-003 | `UIManager.UpdateProgress` пустой | `Assets/Scripts/UIManager.cs` | UI progress не реализован/перенесен | Неполная обратная связь по целям | Medium |
| BUG-004 | Hover highlight колонок не работает | `CardColumn.SetHighlight` | Метод пустой | Слабый drag/drop feedback | Medium |
| BUG-005 | `GoalSlot.RefreshHeader` отключает header и очищает тексты | `Assets/Scripts/GoalSlot.cs` | Старый header заменен label flow через `CardMeshSceneRefs` | Путаница UI ownership, риск регрессий | Medium |
| BUG-006 | Collection Book points не начисляются из gameplay | `CollectionBookManager`, `GameManager` | Вызов `AddCategoryCompletion` в gameplay не найден | Meta-progression может не работать | High |
| BUG-007 | Daily reward ExtraMoves записывается, но не потребляется | `DailyRewardsManager`, `GameManager` | `CardMesh.Boosts.ExtraMoves` не читается gameplay | Награда дня 5 может быть бесполезной | High |
| BUG-008 | Shop не подключен к платежам | `ShopPopup` | Demo UI only, логирует покупки | Monetization not ready | High |
| BUG-009 | Android/iOS ads provider не найден | `AdsManager` | Есть mock и Yandex WebGL only | Mobile ads not ready | High |
| BUG-010 | Analytics/service layer пустой | `PlatformService`, `StubPlatformService` | Нет реализации TrackEvent/SaveProgress | Нет продуктовой аналитики/progress sync | Medium |
| BUG-011 | `GameManager` совмещает слишком много responsibilities | `Assets/Scripts/GameManager.cs` | God class | Высокая цена любых изменений | High |
| BUG-012 | `UIManager` runtime-строит и рестайлит UI | `Assets/Scripts/UIManager.cs` | UI ownership смешан с code polish | Mobile stutters, prefab drift | High |
| BUG-013 | Safe area не применяет `Screen.safeArea` | `CardMeshSceneRefs`, `UIManager` | Только `SafeAreaRoot` name/stretch | Notch/home indicator overlap | High |
| BUG-014 | Undo history только in-memory | `GameManager.moveHistory` | Snapshot держит refs на `CardView` | Undo пропадает после restart/scene reload | Medium |
| BUG-015 | Redo отсутствует | `GameManager` | Не реализовано | Если UX требует redo, feature gap | Low |
| BUG-016 | Tap-to-move карты не обнаружен | `CardView`, `GameManager` | Только drag handlers | Mobile UX может быть медленным | Medium |
| BUG-017 | `DrawSlot` использует `RemoveAt(0)` | `DrawSlot.DrawNextCard` | List as queue | Легкая аллокация/shift cost на больших stock | Low |
| BUG-018 | Recycle order stock/waste требует проверки | `DrawSlot.RecycleRevealedCardsToStock` | Revealed возвращается в queued в текущем порядке | Правила deck cycling могут отличаться от дизайна | Medium |
| BUG-019 | Runtime creation/destroy for win/confetti/sparkles | `WinScreenView`, `UIManager` | Нет pooling | GC spikes на win/rewards | Medium |
| BUG-020 | `RuntimeDevTools` может попасть в release | `RuntimeDevTools` | Runtime bootstrap via attributes требует проверки | Debug UI/cheats в production | Critical |
| BUG-021 | Scene names/fallbacks неоднородны | `MainMenuController`, `SceneLoader`, `GameManager` | Используются `Game`, `Main`, asset paths | Navigation failures if build settings change | Medium |
| BUG-022 | `SecretQuizButton` ссылается на `Game Secret`, сцены нет в Build Settings | `SecretQuizButton`, build settings | Legacy/other game code | Broken button if present | Low |
| BUG-023 | Legacy quiz/share strings не соответствуют CardMesh | `ShareButton`, ads helpers | Остатки другого проекта | Product polish risk | Low |
| BUG-024 | `CoinsManager` economy version reset может обнулить coins | `CoinsManager.Awake` | Version bump resets to startingCoins | Потеря валюты при миграции | Medium |
| BUG-025 | PlayerPrefs saves scattered and frequent | Many managers | Нет centralized save service | Hard to migrate/debug saves | Medium |
| BUG-026 | No tests found for gameplay rules | `Assets/Scripts` | Нет unit/playmode tests | Regression risk | High |
| BUG-027 | Generated runtime sprites/textures may leak until scene/app end | `UIManager`, `WinScreenView`, `CardMeshSceneRefs`, `CardDatabase` | Cached static sprites and Texture2D | Memory growth risk | Medium |
| BUG-028 | `GoalSlot.CanAcceptGoldHeadedStack` assumes gold card at `movingCards[^1]` | `GoalSlot`, `CardColumn.GetMovableStack` | Stack order contract implicit | Easy to break by refactor | Medium |
| BUG-029 | UI popup classes are very large | `DailyRewardsPopup`, `ChestChasePopup`, `ShopPopup`, `CollectionBookPopup` | Code-authored UI | Maintenance risk | Medium |
| BUG-030 | `GetComponentsInChildren` and `FindFirstObjectByType` used broadly | UI/managers | Dynamic discovery pattern | Runtime cost and hidden dependencies | Medium |
| BUG-031 | TODO only found in TMP example, not game code | `Assets/TextMesh Pro/Examples & Extras/...` | Vendor/example script | Not project-specific | Low |

## Недоделанные системы

- Current session save/load.
- Real analytics.
- Real mobile ads/provider config.
- IAP/shop.
- Safe area/device QA.
- Collection Book gameplay integration.
- ExtraMoves inventory consumption.
- Tests.

## Подозрительные места

- `RuntimeSceneBootstrapper` and `CardMeshSceneRefs` can auto-create/auto-bind missing objects, masking scene regressions.
- `UIManager` and `CardMeshSceneRefs` both manipulate HUD layout.
- Several systems auto-add managers to the current GameObject.
- Popups rely on exact hierarchy names.
- Worktree has many existing modified assets/scripts. Before fixing issues, isolate current branch scope.

