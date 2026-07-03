# Next Development Plan

## Этап 1 - стабилизация ядра игры

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Написать playmode/unit tests на правила `CardColumn.CanAccept`, `GoalSlot.CanAcceptStack`, `DrawSlot` recycle | `Assets/Scripts/CardColumn.cs`, `GoalSlot.cs`, `DrawSlot.cs` | Medium | High | Medium |
| Зафиксировать expected rules для text/image/gold stack moves в docs/tests | `docs/project-snapshot/02_GAMEPLAY_LOGIC.md`, tests | Low | High | Small |
| Проверить undo после move/draw/goal completion/lose edge cases | `GameManager.cs` | High | High | Medium |
| Проверить stock recycle order и утвердить дизайн | `DrawSlot.cs`, gameplay QA | Medium | Medium | Small |
| Добавить profiler markers вокруг restart, draw, move, goal complete | `GameManager.cs`, `DrawSlot.cs` | Low | Medium | Small |

## Этап 2 - исправление критичных багов

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Убедиться, что `RuntimeDevTools` отключен в release builds | `RuntimeDevTools.cs`, build settings | High | Critical | Small |
| Подключить потребление `CardMesh.Boosts.ExtraMoves` или убрать reward из daily config | `DailyRewardsManager.cs`, `GameManager.cs`, `PlayerBoostInventory.cs` | Medium | High | Medium |
| Связать Collection Book с завершением категорий/уровней или пометить feature disabled | `GameManager.cs`, `CollectionBookManager.cs` | Medium | High | Medium |
| Реализовать column hover highlight или удалить ожидание подсветки | `CardColumn.cs` | Low | Medium | Small |
| Решить ownership goal label/header | `GoalSlot.cs`, `SolitaireSceneRefs.cs` | Medium | Medium | Medium |

## Этап 3 - UI/UX polish

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Реализовать или удалить пустые `UpdateGoalsSummary`/`UpdateProgress` | `UIManager.cs` | Medium | Medium | Small |
| Провести QA всех popups на 16:9, 19.5:9, tablet | `UIManager.cs`, `MainMenu/*.cs`, scenes | Medium | High | Medium |
| Проверить authored vs runtime UI ownership и убрать дублирующие элементы | `UIManager.cs`, `SolitaireSceneRefs.cs`, prefabs/scenes | High | Medium | Large |
| Добавить tap-to-move, если это требуется mobile UX | `CardView.cs`, `GameManager.cs` | High | Medium | Medium |
| Проверить main menu navigation and popup lifecycle | `MainMenuController.cs`, `SceneLoader.cs`, popup classes | Medium | Medium | Medium |

## Этап 4 - мобильная оптимизация

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Реализовать real safe area application | `SolitaireSceneRefs.cs`, `UIManager.cs` | Medium | High | Medium |
| Профилировать restart and win on low-end Android/iOS | Gameplay scene | Medium | High | Medium |
| Добавить pooling для cards/effects | `GameManager.cs`, `WinScreenView.cs`, `UIManager.cs` | High | Medium | Large |
| Убрать LINQ allocations in gameplay hot paths | `GameManager.cs` | Medium | Medium | Small |
| Проверить texture/sprite memory for image cards | `CardDatabase.cs`, image assets | Medium | High | Medium |

## Этап 5 - подготовка к релизу

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Выбрать target platform and connect real Ads provider | `AdsManager.cs`, packages/settings | High | Critical | Large |
| Подключить analytics implementation | `PlatformService.cs`, new service | Medium | High | Medium |
| Подключить IAP/store products или скрыть shop | `ShopPopup.cs`, store config | High | High | Large |
| Добавить privacy/consent flow if ads/analytics enabled | New UI/service | Medium | High | Medium |
| Создать release QA checklist and build verification | docs/build config | Low | High | Small |

## Этап 6 - post-release улучшения

| Задача | Затронутые файлы | Риск | Приоритет | Сложность |
| --- | --- | --- | --- | --- |
| Разделить `GameManager` на gameplay, input, rewards, navigation services | `GameManager.cs` | High | Low | Large |
| Разделить `UIManager` на HUD/modal/effects components | `UIManager.cs` | High | Low | Large |
| Вынести rewards/economy/lives/ad cadence в ScriptableObjects | Managers/config assets | Medium | Medium | Medium |
| Добавить cloud save / backup | Save service | High | Medium | Large |
| Улучшить content pipeline documentation | `Tools/*`, docs | Low | Low | Medium |

