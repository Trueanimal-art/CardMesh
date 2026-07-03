# Animation And Feedback

## Общая система

В проекте не найдено DOTween/LeanTween. Анимации и feedback реализованы вручную через coroutines, `Vector3.Lerp`, `Mathf.Sin`, `Time.deltaTime` и `Time.unscaledDeltaTime`.

Основные классы:

- `CardView`
- `GameManager`
- `GoalSlot`
- `UIManager`
- `UIJuicyButton`
- `WinScreenView`
- `RoyalEventPulse`
- Main menu popup classes
- `MusicPlayer`

## Анимации карт

`CardView`:

- `AnimateFromWorldPosition`: перемещает карту из стартовой world position в текущую target position.
- `PlayShake`: shake для ошибочного хода.
- `PlayAcceptedFeedback`: glow/scale feedback.
- `SetDraggingVisual`: scale/outline при drag.
- `SetRevealHistoryLayout`: визуальный режим для peek cards в колонке.
- `SetDrawHistoryTitleLayout`: компактный режим для history cards в waste.

`GameManager`:

- `PlayMoveAnimations`: вызывает `CardView.AnimateFromWorldPosition` после успешного move.
- `PlayMoveHintAnimation`: поднимает карту/стопку, перемещает к target, держит, возвращает назад.
- `PlayDeckHintAnimation`: pulse/wiggle deck.
- `AnimateResolvedGoalCards`: схлопывает completed goal cards в центр.
- `PlayGoalCompletionCloud`: создает runtime cloud image и fade/scale.

## Визуальные эффекты

- Card glow: `CardView.EnsureDepthVisuals`, `GlowScaleRoutine`.
- Goal slot accept/complete feedback: `GoalSlot.FeedbackRoutine`.
- Goal complete cloud: runtime `GameObject("GoalCompleteCloud")`.
- Win confetti: `WinScreenView` создает 46 confetti pieces.
- Coins award: `UIManager.PulseCoins` and `SpawnFloatingText`.
- Goal sparkles: `UIManager.SpawnGoalSparkles`.
- Button hover/press: `UIJuicyButton`.
- Royal event pulse: `RoyalEventPulse.Update`.

## Подсветка доступных ходов

Hints:

- Работают как анимационная подсказка, не как постоянная подсветка.
- `GameManager.FindBestHintMove` ищет candidate move и запускает animation.
- При отсутствии ходов указывает на deck.

Drop hover:

- `GoalSlot.SetHighlight` меняет цвет background image.
- `CardColumn.SetHighlight` пустой. Поэтому hover highlight для колонок фактически отсутствует.

## Feedback при ошибочном ходе

Flow:

1. `GameManager.EndDrag` не находит valid target.
2. Карта возвращается через `Container.RefreshLayout`.
3. `PlayInvalidMoveFeedback` вызывает `CardView.PlayShake`.

Звук ошибки не найден.

## Win animation

`GameManager.FinishLevelCompletion`:

- статус "Уровень пройден";
- `MusicPlayer.PlayWin`;
- `WinScreenView.Show`;
- затем auto-advance coroutine.

`WinScreenView`:

- fade/scale panel;
- glow ring;
- confetti pieces;
- retry/menu buttons.

## Звук

Класс: `MusicPlayer`.

Реализовано:

- Singleton `MusicPlayer.Instance`.
- Background tracks array, random bag playback without immediate repeats.
- Separate SFX AudioSource.
- PlayerPrefs settings: `MusicVolume`, `SfxVolume`, `AudioMuted`.
- SFX methods: button click, card pick, drop, complete, win, lose, coin.
- Coin placeholder clip generated in code if not assigned.

Использование:

- `CardView.OnBeginDrag` -> card pick.
- `GameManager.ExecuteMove` -> card drop.
- `GameManager.ResolveBoardState` goal complete -> card complete.
- Win/lose flow -> win/lose.
- `UIJuicyButton.OnPointerClick` -> button click.

## Производительность эффектов

Риски:

- Win confetti creates/destroys many GameObjects every win.
- Goal completion cloud creates/destroys GameObject.
- Coins/sparkles/floating text create/destroy GameObjects.
- Runtime sprites/textures created in `UIManager`, `SolitaireSceneRefs`, `WinScreenView`.
- Many effects run on coroutines; conflicts handled locally by stopping previous coroutine, but not globally coordinated.

## Возможные конфликты анимаций и логики

- Drag animation and hint animation both detach cards to `dragLayer`; `GameManager` blocks hint during drag and blocks drag while `draggedCard` exists.
- `CardView.StartFeedbackCoroutine` stops previous movement/shake coroutine on the same card.
- `CardView.StartJuiceCoroutine` stops previous glow/scale routine.
- Goal completion removes cards and starts animation; undo during completion is blocked because `levelFinished`/input states change, but mid-goal animation + undo edge cases require manual testing.
- `Time.timeScale = 0` for popups; many UI animations use unscaled time, gameplay animations use scaled time.

## Требует проверки

- Whether all SFX clips are assigned in scenes/prefabs.
- Whether `MusicPlayer` exists in both MainMenu and gameplay scene without duplicate issues.
- Whether card move/hint animations remain correct on mobile aspect ratios.
- Whether goal completion animation can overlap with auto-win transition.

