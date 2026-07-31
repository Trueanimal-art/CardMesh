# Release Readiness

Оценка сделана по коду/ресурсам без запуска Unity Editor, device builds, QA и profiler.

| Раздел | Оценка | Короткое объяснение |
| --- | --- | --- |
| Gameplay | Almost Ready | Core loop playable: generated levels, drag/drop, stock, goals, win/lose, undo, hints. Нужны QA edge cases и tests. |
| UI/UX | Needs Work | Есть много экранов и polish, но UI сильно code-driven, safe area не подтвержден, progress methods пустые. |
| Stability | Needs Work | Большие coupled classes, no tests, runtime scene binding masks issues. |
| Performance | Needs Work | Нет profiling; runtime instantiate/destroy/UI rebuild risks. |
| Save System | Needs Work | Progress/economy/meta сохраняются, но текущая партия не сохраняется. |
| Mobile Readiness | Needs Work | Android API 36 ARM64 IL2CPP APK проверен на эмуляторе; остаются safe-area/performance QA на физических устройствах. |
| Monetization | Needs Work | AdMob interstitial/rewarded и UMP подключены; production ID и IAP еще не настроены. |
| Analytics | Not Ready | `PlatformService.TrackEvent` пустой, integrations not found. |
| Ads | Needs Work | Google Mobile Ads 11.3.0 и UMP работают на Android; нужны production ID, AdMob consent message и physical-device QA. |
| Build Pipeline | Needs Work | Unity menu/batch methods собирают test APK и release AAB; CI отсутствует, release требует keystore и production AdMob IDs. |
| Store Readiness | Needs Work | Android package/API/orientation настроены; остаются Play declarations, privacy policy, Data safety, app-ads.txt, keystore и device QA. |

## Общий вывод

Примерная готовность к релизу: 55-65% для internal/playable MVP, 35-45% для публичного mobile release.

Главные блокеры релиза:

- Нет сохранения текущей игровой сессии.
- Не подтверждены safe area/mobile layouts/performance.
- Production AdMob account configuration, IAP and analytics are still incomplete.
- Debug/dev tools and legacy quiz code require release cleanup.
- Нет automated tests for core gameplay rules.
- UI progress and meta reward integrations partially incomplete.

Что сделать в первую очередь:

1. Завершить Android/Google Play production configuration.
2. Проверить production build на отсутствие `RuntimeDevTools`.
3. Провести manual QA gameplay на 3/4/5 columns, stock recycle, undo, hints, win/lose/restart.
4. Подтвердить mobile safe area и AdMob flow на физических устройствах.
5. Добавить минимальные tests для `CardColumn`, `GoalSlot`, `DrawSlot`, `LevelManifestDatabase`.
