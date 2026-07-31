# Entry Points

| Entry Point | Тип | Путь | Что запускает | Что зависит от него |
| ----------- | --- | ---- | ------------- | ------------------- |
| `Assets/MainMenu.unity` | Scene | `Assets/MainMenu.unity` | Main menu | `MainMenuController`, popups, managers |
| `Assets/Scenes/Main.unity` | Scene | `Assets/Scenes/Main.unity` | Gameplay | `GameManager`, `UIManager`, board refs |
| `MainMenuController.OnPlayClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Select last level, ensure seed, load gameplay scene | Main play button |
| `MainMenuController.OnLevelsClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Open level selection | `LevelSelectionPopup`, progression |
| `MainMenuController.OnSettingsClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Open settings | `SettingsPopup` |
| `MainMenuController.OnDailyRewardsClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Open daily rewards | `DailyRewardsManager`, `DailyRewardsPopup` |
| `MainMenuController.OnChestChaseClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Open chest chase | `ChestChaseManager`, `ChestChasePopup` |
| `MainMenuController.OnShopClicked` | UI callback | `Assets/Scripts/MainMenuController.cs` | Open shop | `ShopPopup` |
| `RuntimeSceneBootstrapper.EnsurePrototypeScene` | Runtime initialize | `Assets/Scripts/RuntimeSceneBootstrapper.cs` | Ensures/binds gameplay scene | `GameManager`, scene refs, ads |
| `GameManager.Awake` | MonoBehaviour | `Assets/Scripts/GameManager.cs` | Reset play state for Main, initialize deps | Managers/services/containers |
| `GameManager.Start` | MonoBehaviour | `Assets/Scripts/GameManager.cs` | Bind manual scene if needed and restart level | Gameplay scene startup |
| `GameManager.RestartLevel` | Gameplay bootstrap | `Assets/Scripts/GameManager.cs` | New/restarted level, clear board, generate/load data, spawn cards | Almost all gameplay |
| `GameManager.DrawNextStockCard` | Gameplay action | `Assets/Scripts/GameManager.cs` | Draw from stock/waste | `DrawSlot`, move history, UI |
| `CardView.OnBeginDrag` | Input | `Assets/Scripts/CardView.cs` | Start card drag | `GameManager.BeginDrag` |
| `CardColumn.OnDrop` | Input/drop | `Assets/Scripts/CardColumn.cs` | Drop card/stack to column | `GameManager.TryMoveDraggedCardTo` |
| `GoalSlot.OnDrop` | Input/drop | `Assets/Scripts/GoalSlot.cs` | Drop card/stack to goal | `GameManager.TryMoveDraggedCardTo` |
| `GameManager.ResolveBoardState` | State resolver | `Assets/Scripts/GameManager.cs` | Complete goals, win/lose checks, UI update | Every move/draw |
| `GameManager.CompleteLevel` | Win flow | `Assets/Scripts/GameManager.cs` | Lock input and start reward sequence | Win screen, coins, chest chase |
| `GameManager.AdvanceToNextLevelAfterDelay` | Win continuation | `Assets/Scripts/GameManager.cs` | Interstitial, progress advance, restart next level | Ads/progression |
| `GameManager.RequestRestartWithConfirmation` | Restart flow | `Assets/Scripts/GameManager.cs` | Open restart confirmation | `UIManager`, lives |
| `GameManager.TrySpendLifeAndRestart` | Restart/lives | `Assets/Scripts/GameManager.cs` | Spend life, optional ad, restart | `LivesManager`, `AdsManager` |
| `GameManager.RequestReturnToMainMenu` | Navigation | `Assets/Scripts/GameManager.cs` | Return to main menu | Pause popup, win screen |
| `UIManager.Initialize` | UI bootstrap | `Assets/Scripts/UIManager.cs` | Gameplay HUD polish/wiring | Gameplay buttons/modals |
| `CardMeshSceneRefs.OnEnable` | Scene refs | `Assets/Scripts/CardMeshSceneRefs.cs` | Auto assign refs and preview columns | Gameplay scene layout |
| `LevelProgressionManager.Awake` | Save bootstrap | `Assets/Scripts/LevelProgressionManager.cs` | Load level progress and seed | Gameplay/main menu |
| `LevelManifestDatabase.TryGetLevel` | Data entry | `Assets/Scripts/LevelManifest.cs` | Lazy load generated levels | Progression/generation |
| `CardDatabase.Awake` | Data entry | `Assets/Scripts/CardDatabase.cs` | Load card JSON | Gameplay/generator |
| `AdsInitializer.Awake` | Ads bootstrap | `Assets/Scripts/Ads/AdsInitializer.cs` | Ensure `AdsManager` | Ads flow |
| `AdsManager.Awake` | Ads service | `Assets/Scripts/Ads/AdsManager.cs` | Singleton provider setup | Rewarded/interstitial |
| `MusicPlayer.Awake` | Audio service | `Assets/Scripts/Music/MusicPlayer.cs` | Singleton audio sources/settings | SFX/music |
| `RuntimeDevTools.Bootstrap` | Dev bootstrap | `Assets/Scripts/RuntimeDevTools.cs` | Creates dev panel | Development/debug only, release risk |

## Где начинается новая партия

- `GameManager.RestartLevel`.

## Где происходит рестарт

- Button/popup -> `GameManager.RequestRestartWithConfirmation` -> `TrySpendLifeAndRestart` -> optional ad -> `RestartLevel`.

## Где происходит победа

- `GameManager.ResolveBoardState` -> `CompleteLevel` -> `EndLevelRewardSequence` -> `FinishLevelCompletion`.

## Где происходит выход в меню

- `GameManager.RequestReturnToMainMenu` -> `ReturnToMainMenu`.
- `PauseMenuPopupController.RequestMainMenu` delegates to GameManager.

## Save/load manager

- Нет единого save/load manager. Системы сохраняются раздельно через `PlayerPrefs`.

## Analytics manager

- Нет отдельного analytics manager. Есть `PlatformService.TrackEvent`, но реализация пустая.

## Ads manager

- `AdsManager` singleton.

