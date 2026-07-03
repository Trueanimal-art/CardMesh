# Save / Load / Progress

## Что сохраняется

Через `PlayerPrefs` сохраняются:

- Текущий выбранный уровень, max unlocked, max completed.
- Seeds уровней, если нет manifest seed.
- Coins и economy version.
- Lives и UTC timestamp последнего восстановления.
- Hint/undo boosts.
- Daily Rewards day/claim date/claim state.
- Chest Chase streak/stage/keys/end time.
- Collection Book points/claimed rewards.
- Audio volume/mute settings.
- Last interstitial timestamp.
- Некоторые reward counters для extra moves/chests/boosters как отдельные keys.

## Что не сохраняется

Текущая игровая сессия не сериализуется:

- Раскладка колонок.
- Stock/revealed cards.
- Goal slot contents.
- `remainingMoves`.
- `completedAssociations`.
- Undo history.
- Face-up state текущей партии.

После выхода/перезапуска gameplay новая доска создается из seed/manifest, а не из последнего runtime state. Это важно для product expectations.

## Где хранятся данные

Все runtime save data хранится в `PlayerPrefs`.

Ключи по классам:

| Система | Класс | Примеры ключей |
| --- | --- | --- |
| Progress | `LevelProgressionManager` | `CardMesh.Player.CurrentLevelNumber`, `CardMesh.Player.MaxUnlockedLevel`, `CardMesh.LevelSeed.{n}` |
| Coins | `CoinsManager` | `CardMesh.Coins`, `CardMesh.EconomyVersion` |
| Lives | `LivesManager` | `CardMesh.Lives.Current`, `CardMesh.Lives.LastRestoreUtc` |
| Boosts | `PlayerBoostInventory` | `CardMesh.Boosts.Hints`, `CardMesh.Boosts.Undo` |
| Daily rewards | `DailyRewardsManager` | `CardMesh.DailyRewards.CurrentDay`, `ClaimedToday`, `LastClaimDate` |
| Extra moves reward | `DailyRewardsManager` | `CardMesh.Boosts.ExtraMoves` |
| Chest chase | `ChestChaseManager` | `CardMesh.ChestChase.CurrentStreak`, `CurrentStage`, `CurrentKeys`, `EndsAtTicks` |
| Collection book | `CollectionBookManager` | `CardMesh.CollectionBook.Points`, `ClaimedCount`, `CardMesh.CollectionBook.Reward.*` |
| Ads | `AdsManager` | `CardMesh.Ads.LastInterstitialRealtime` |
| Audio | `MusicPlayer`, `SettingsPopup` | `MusicVolume`, `SfxVolume`, `AudioMuted`, `Settings.HintsEnabled` |

## Формат данных

- `PlayerPrefs` int/float/string.
- Dates: daily rewards uses local `DateTime.Now.ToString("yyyyMMdd")`.
- Lives and ads use Unix seconds strings.
- Chest Chase uses `DateTime.UtcNow.Ticks.ToString()`.
- Level manifest and card data are JSON resources loaded at runtime, not player saves.

## Когда происходит сохранение

- `LevelProgressionManager.Save`: after select, complete, reset, seed creation.
- `CoinsManager`: every add/spend and economy version reset.
- `LivesManager`: after spend, restore recalculation, rewarded life.
- `PlayerBoostInventory`: every add/spend.
- `DailyRewardsManager`: after claim/debug reset/day changes.
- `ChestChaseManager`: after win/loss/expiry/reward progress.
- `CollectionBookManager`: after adding points/claiming/reset.
- `MusicPlayer` and settings: on value changes.
- `AdsManager.MarkInterstitialShown`: after successful interstitial.

## Когда происходит загрузка

- `Awake` of each manager loads from `PlayerPrefs`.
- `LevelProgressionManager.Awake` also ensures current level seed.
- `DailyRewardsManager.TryClaimToday` calls `Load` again before claim.
- `ChestChaseManager.GetOrCreate` calls `Load` and `RefreshExpiredEvent` for newly created manager.

## Как восстанавливается игровая сессия

Не восстанавливается как session state. Восстановление идет на уровне progression:

1. `LevelProgressionManager` загружает current/max level.
2. `GameManager.RestartLevel` получает current level/seed.
3. `LevelManifestDatabase` отдает deterministic/generated data.
4. `GameManager` создает новую раскладку из manifest/generated data.

Если пользователь ожидал продолжить ровно с того же хода, это сейчас не поддержано.

## Защита от битых сохранений

Есть минимальная защита:

- `Mathf.Clamp`/`Mathf.Max` на числах.
- timestamp lives исправляется, если <=0 или в будущем.
- manifest JSON parse окружен `try/catch ArgumentException`.
- chest ticks fallback на новый event end time.
- invalid PlayerPrefs values часто просто заменяются default.

Нет:

- общей save schema;
- checksum;
- backup slot;
- atomic multi-system save;
- validation report;
- structured migration framework.

## Миграции

- `LevelProgressionManager.ClearSavedProgress` удаляет legacy keys, но только при reset.
- `CoinsManager` имеет `EconomyVersionKey` и при версии меньше 2 сбрасывает coins к `startingCoins`.
- Остальные системы миграций версий не имеют.

## Риски потери прогресса

- `PlayerPrefs` не подходит для надежной долгосрочной экономики без backup/cloud.
- Economy version reset может обнулить coins при bump.
- Daily Rewards использует local date, чувствителен к смене даты/timezone.
- Lives/ChestChase используют UTC, но пользовательская смена времени может влиять.
- ExtraMoves reward записывается в `CardMesh.Boosts.ExtraMoves`, но потребителя в gameplay не найдено.
- Session progress теряется при закрытии игры.

## Если сохранения не реализованы полностью

Текущее состояние: реализованы player progression/economy/meta saves, но не реализован save/load текущего уровня. Для полноценного релиза нужно добавить:

- Serializable board state.
- Versioned save payload.
- Save on app pause/quit and important actions.
- Load/resume UI.
- Corruption fallback.
- Migration path for existing PlayerPrefs keys.

