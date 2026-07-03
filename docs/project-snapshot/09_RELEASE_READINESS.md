# Release Readiness

Оценка сделана по коду/ресурсам без запуска Unity Editor, device builds, QA и profiler.

| Раздел | Оценка | Короткое объяснение |
| --- | --- | --- |
| Gameplay | Almost Ready | Core loop playable: generated levels, drag/drop, stock, goals, win/lose, undo, hints. Нужны QA edge cases и tests. |
| UI/UX | Needs Work | Есть много экранов и polish, но UI сильно code-driven, safe area не подтвержден, progress methods пустые. |
| Stability | Needs Work | Большие coupled classes, no tests, runtime scene binding masks issues. |
| Performance | Needs Work | Нет profiling; runtime instantiate/destroy/UI rebuild risks. |
| Save System | Needs Work | Progress/economy/meta сохраняются, но текущая партия не сохраняется. |
| Mobile Readiness | Not Ready | Нет подтвержденного safe area, real mobile ads, device profiling, orientation QA. |
| Monetization | Not Ready | Shop demo only, IAP config not found. Rewarded ads facade есть, provider mobile отсутствует. |
| Analytics | Not Ready | `PlatformService.TrackEvent` пустой, integrations not found. |
| Ads | Needs Work | Mock and WebGL/Yandex path есть; Android/iOS provider не найден. Требует platform QA. |
| Build Pipeline | Unknown | Build settings есть, но CI/build scripts не обнаружены. Tools генерируют контент, но build automation требует проверки. |
| Store Readiness | Not Ready | Нет store configs, privacy/consent/IAP/mobile ads evidence, device QA. |

## Общий вывод

Примерная готовность к релизу: 55-65% для internal/playable MVP, 35-45% для публичного mobile release.

Главные блокеры релиза:

- Нет сохранения текущей игровой сессии.
- Не подтверждены safe area/mobile layouts/performance.
- Нет реальной mobile ads/IAP/analytics реализации.
- Debug/dev tools and legacy quiz code require release cleanup.
- Нет automated tests for core gameplay rules.
- UI progress and meta reward integrations partially incomplete.

Что сделать в первую очередь:

1. Зафиксировать release target: WebGL/Yandex или Android/iOS.
2. Проверить production build на отсутствие `RuntimeDevTools`.
3. Провести manual QA gameplay на 3/4/5 columns, stock recycle, undo, hints, win/lose/restart.
4. Реализовать/подтвердить mobile safe area and ads provider for target.
5. Добавить минимальные tests для `CardColumn`, `GoalSlot`, `DrawSlot`, `LevelManifestDatabase`.

