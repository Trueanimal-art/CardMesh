# Project Snapshot

Технический снимок текущего состояния Unity-проекта Solitaire / CardMesh на 2026-07-03.

Документация нужна для передачи проекта другому разработчику или AI-агенту без старого контекста. Она описывает фактическое состояние кода, сцен, ресурсов и известных рисков. Это не дизайн-документ будущей архитектуры и не список пожеланий.

Рекомендуемый порядок чтения:

1. `00_PROJECT_OVERVIEW.md`
2. `12_ENTRY_POINTS.md`
3. `01_ARCHITECTURE.md`
4. `02_GAMEPLAY_LOGIC.md`
5. `08_KNOWN_BUGS_AND_CURRENT_PROBLEMS.md`
6. `11_AI_HANDOFF_GUIDE.md`
7. Остальные файлы по теме задачи.

Использование для handoff:

- Сначала прочитать overview и entry points.
- Затем открыть ключевые классы из таблиц и сверить их с текущим состоянием ветки.
- Перед любыми изменениями проверить `08_KNOWN_BUGS_AND_CURRENT_PROBLEMS.md` и `10_NEXT_DEVELOPMENT_PLAN.md`.
- После изменений обновлять затронутые файлы в этой папке.

Обновлять этот snapshot нужно после изменений в gameplay rules, level generation, save/load, UI flow, рекламе, монетизации, build settings, ресурсах уровней и любых исправлениях из `08_KNOWN_BUGS_AND_CURRENT_PROBLEMS.md`.

