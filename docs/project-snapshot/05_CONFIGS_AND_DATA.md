# Configs And Data

## ScriptableObject

Обнаруженные типы:

- `LevelDefinition`: `Assets/Resources/Levels/Level_1.asset`.
- `CardDefinition`: old/manual card asset type, есть assets в `Assets/Resources/Cards/`.
- `AssociationDefinition`: old/manual association asset type, есть assets в `Assets/Resources/Associations/`.
- `CardVisualConfig`: тип есть, concrete asset path не найден по быстрому списку `Resources`; вероятно сериализуется в scene/prefab. Требует проверки.
- `ChestChaseRewardConfig`: тип есть, concrete asset path не найден. В `ChestChaseManager` есть default tiers.

## JSON/config файлы

- `Assets/Resources/Data/cards.json`: основная база gold/regular cards.
- `Assets/Resources/GeneratedLevels/level_manifest.json`: 1000 generated levels.
- `Assets/Resources/GeneratedLevels/content_usage_stats.json`: статистика использования контента.
- `Assets/Resources/ContentPacks/content_packs.json`: 10 packs.
- `Assets/Resources/ContentPacks/content_cards.json`: 400 text cards, 402 image cards, validation status valid.
- `Assets/Resources/ContentPacks/matching_metadata.json`: 400 matching relations and policy.
- `Packages/manifest.json`: Unity packages.
- `ProjectSettings/EditorBuildSettings.asset`: scenes in build.

## Настройки карт, уровней, экономики, рекламы, UI, баланса

- Карты/категории: `cards.json`, content pack JSON, premium PNG.
- Уровни/layouts: `Level_1.asset` and `level_manifest.json`.
- Экономика: serialized fields/defaults in `CoinsManager`, `DailyRewardsManager`, `ChestChaseManager`, `CollectionBookManager`.
- Реклама: serialized fields in `AdsManager`; provider selection compile-time.
- UI assets: `Assets/Resources/UI/Premium/`, `Assets/Resources/UI/RoyalEvent/`, `Assets/Prefabs/UI/`.
- Audio: `Assets/Audio/1.mp3` ... `4.mp3`, linked via scene/prefab requires verification.

## Данные, захардкоженные в коде

- Hint cost: `900` in `GameManager` and `UIManager`.
- Rewarded +moves amount default: `10` in `GameManager`.
- Coins win reward default: `50` in `CoinsManager`.
- Lives: max 5, restore interval 3600 seconds in `LivesManager`.
- Chest Chase default tiers in `ChestChaseManager`.
- Daily Rewards list in `DailyRewardsManager`.
- Collection Book rewards/titles/scoring in `CollectionBookManager`.
- Layout constants and UI sizes in `UIManager`, `CardMeshSceneRefs`, `CardColumn`, `GoalSlot`.
- Image key mapping dictionary in `CardDatabase`.
- Ads cooldown/restart count defaults in `AdsManager`.

## Что лучше вынести в конфиги позже

- Hint cost and boost pricing.
- Win coin rewards.
- Rewarded ad grants.
- Daily rewards schedule.
- Chest Chase tiers/duration.
- Collection scoring/rewards.
- Lives max/restore time.
- UI layout profiles per device orientation.
- Ads cooldown and show cadence.
- Tutorial/how-to-play content.
- Release build feature flags for dev tools.

## Критичные для релиза конфиги

- `EditorBuildSettings.asset`: корректные scenes in build.
- `level_manifest.json`: generated levels and solver metadata.
- `cards.json`: card/category data.
- `content_cards.json` and premium images: content integrity.
- Ads provider config per target platform.
- Store product IDs and purchase config: сейчас не найдено.
- Privacy/analytics/service config: сейчас не найдено.

## Таблица конфигов/assets

| Конфиг / Asset | Путь | Назначение | Кто использует | Риски |
| -------------- | ---- | ---------- | -------------- | ----- |
| Unity version | `ProjectSettings/ProjectVersion.txt` | Unity `2022.3.43f1` | Editor/build | Version drift |
| Build scenes | `ProjectSettings/EditorBuildSettings.asset` | MainMenu and Main scenes | Scene loading | Scene names in code include fallbacks |
| Packages | `Packages/manifest.json` | TMP, UGUI, Test Framework, etc. | Whole project | No ads SDK package visible |
| Level definition | `Assets/Resources/Levels/Level_1.asset` | Layout groups/reference moves/image settings | `GameManager`, `LevelGenerator` | Fallback generation depends on it |
| Level manifest | `Assets/Resources/GeneratedLevels/level_manifest.json` | 1000 levels, seeds, columns, stock, solver stats | `LevelManifestDatabase`, `LevelProgressionManager` | Large JSON in Resources, static cache |
| Content usage stats | `Assets/Resources/GeneratedLevels/content_usage_stats.json` | Usage audit, unused cards count 0 | Pipeline/dev | Runtime not clearly using it |
| Card DB JSON | `Assets/Resources/Data/cards.json` | 50 gold cards, 400 regular cards | `CardDatabase` | JsonUtility schema must match fields |
| Content packs | `Assets/Resources/ContentPacks/content_packs.json` | 10 packs, allowed levels/difficulty | Pipeline/dev | Runtime usage not obvious |
| Content cards | `Assets/Resources/ContentPacks/content_cards.json` | 400 text, 402 image, validation valid | Pipeline/dev | 2 unused premium PNG noted |
| Matching metadata | `Assets/Resources/ContentPacks/matching_metadata.json` | 400 relations, policy | Pipeline/dev | Runtime usage not obvious |
| Premium image cards | `Assets/Resources/ImageCards/Premium/*.png` | Sprite resources for image cards | `CardDatabase` | Resource path must match card IDs |
| Premium UI sprites | `Assets/Resources/UI/Premium/*.png` | HUD/buttons/cards visuals | `PremiumUISprites`, UI classes | Missing sprite falls back to runtime sprites |
| RoyalEvent UI sprites | `Assets/Resources/UI/RoyalEvent/*.png` | Event popup visuals | `RoyalEventSprites`, popup classes | Requires visual QA |
| MainMenu level selection prefab | `Assets/Resources/UI/MainMenu/LevelSelectionPopup.prefab` | Level selection popup | `LevelSelectionPopup.GetOrCreate` | New/untracked in current worktree |
| Gameplay UI prefabs | `Assets/Prefabs/UI/Game/*.prefab` | Authored HUD elements | Scene/prefab workflows | Runtime UI can diverge |
| Premium event prefabs | `Assets/Prefabs/UI/PremiumEvents/*.prefab` | Shared event popup visuals | Main menu popups | Requires prefab QA |
| Audio tracks | `Assets/Audio/*.mp3` | Background music clips | `MusicPlayer` if serialized | Requires scene/prefab binding check |
| Old card SOs | `Assets/Resources/Cards/*.asset` | Legacy/manual card definitions | Требует проверки | May be obsolete vs JSON |
| Association SOs | `Assets/Resources/Associations/*.asset` | Legacy/manual associations | Требует проверки | May be obsolete vs JSON |
| Tools pipeline | `Tools/generate_level_content_pipeline.py`, `Tools/validate_solvability.py` | Generate/validate content | Dev pipeline | Not part of Unity runtime |

