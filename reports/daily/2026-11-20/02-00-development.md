# Ежедневная разработка — 20 ноября 2026, 02:00 (Europe/Moscow)

## Цель

Закрыть следующий незавершённый пункт этапа 4: соединить детерминированный ростер из 12 жителей с социальным ядром, не перенося расходящуюся старую ветку PR #1 и не подключая неподтверждённые игровые API.

## Проверка репозитория перед работой

Прочитаны:

- `README.md`;
- `docs/КОНЦЕПЦИЯ.md`;
- `docs/ПЛАН.md`;
- `docs/ТЕХНИЧЕСКАЯ_РАЗВЕДКА.md`;
- `docs/АРХИТЕКТУРА_КОДА.md`;
- отчёты 2026-11-19: `02-00-development.md`, `03-00-review.md`, `04-00-final.md`;
- отчёт разработки 2026-11-18.

Состояние на начало цикла:

- `main`: `e49c7ecaa591fd7b2cd67220a28e251f429820d1`.
- Ветка `daily/2026-11-20` отсутствовала; создана непосредственно от актуального `main`.
- Ветки: `main`, `feature/initial-social-core`, `daily/2026-11-16`, `daily/2026-11-17`, `daily/2026-11-18`, `daily/2026-11-19`.
- PR #5 уже слит в main: merge commit `89507ca58f81d9ce3b20a89f40f6bae9b0d89fb7`.
- PR #1 открыт как Draft; ветка расходится с main: 127 коммитов впереди и 51 позади. Его старый код не переносился.
- PR #4 открыт как Draft; ветка расходится с main: 20 коммитов впереди и 3 позади, GitHub сообщает `mergeable=false` / `dirty`. Формальных review submissions и inline comments у PR #4 нет. Его C#-исходники и тесты были просмотрены перед переносом.
- README объявляет Prison Colony текущим приоритетом репозитория, а «ПРОТОКОЛ: НЕЧТО» — отложенным проектом. По прямому scope этого цикла изменялись только файлы домена «ПРОТОКОЛ: НЕЧТО» и общие проверки его ядра; файлы PrisonColony не затрагивались.

## Что реально реализовано

1. В актуальную ветку добавлен независимый `Protocol.Determinism` с SplitMix64, unbiased bounded sampling, стабильной генерацией Guid и Fisher–Yates. `System.Random` не используется.
2. Добавлен `Protocol.Core`: неизменяемые профили ровно 12 жителей, уникальные ID/имена, возраст генерации 20–65, пять параметров личности, привычки, индивидуальные особенности и профессия из обязательной конфигурации квот.
3. `StationRosterGenerator` непосредственно использует существующий `ProfessionRosterComposer`. Встроенные профессии и игровые значения квот не добавлены.
4. `Protocol.ProfessionConfig` переведён на общий `StableRandom`; зависимость от `Protocol.Determinism` оформлена явно.
5. `Protocol.Social` теперь ссылается на `Protocol.Core` в одном направлении. Обратной ссылки из Core на Social нет.
6. Добавлен `StationSocialState`, который принимает один `StationRoster` и создаёт ровно 12 личных `MemoryJournal`. Сопоставление `Resident.Id` → `ResidentId.Value` использует тот же Guid без повторной генерации.
7. Добавлен `ResidentSocialView`: view открывает память только своего владельца, отношения с другим жителем и операции записи наблюдения/передачи слуха. Публичного перечисления всех журналов нет.
8. Агрегат отклоняет ID, наблюдателей, субъектов событий и участников разговора, не принадлежащих текущему ростеру.
9. Добавлен сквозной сценарий: генерация 12 жителей → создание 12 социальных views → личное наблюдение → два явных разговора → два пересказа со снижением достоверности 1000 → 750 → 562. Остальные жители не получают событие автоматически.

## Изменённые файлы

Новые:

- `src/Protocol.Determinism/Protocol.Determinism.csproj`
- `src/Protocol.Determinism/StableRandom.cs`
- `src/Protocol.Core/Protocol.Core.csproj`
- `src/Protocol.Core/Properties/AssemblyInfo.cs`
- `src/Protocol.Core/Domain/Resident.cs`
- `src/Protocol.Core/Domain/ResidentPersonality.cs`
- `src/Protocol.Core/Domain/StationRoster.cs`
- `src/Protocol.Core/Domain/StationRosterGenerator.cs`
- `src/Protocol.Social/Integration/StationSocialState.cs`
- `src/Protocol.Social/Integration/ResidentSocialView.cs`
- `tests/Protocol.Core.SmokeTests/Protocol.Core.SmokeTests.csproj`
- `tests/Protocol.Core.SmokeTests/Program.cs`
- `tests/Protocol.StationIntegration.Tests/Protocol.StationIntegration.Tests.csproj`
- `tests/Protocol.StationIntegration.Tests/Program.cs`
- `docs/РОСТЕР_ЖИТЕЛЕЙ.md`

Изменены:

- `src/Protocol.ProfessionConfig/ProfessionQuotaAllocator.cs`
- `src/Protocol.ProfessionConfig/Protocol.ProfessionConfig.csproj`
- `src/Protocol.Social/Protocol.Social.csproj`
- `.github/workflows/social-core.yml`
- `.github/workflows/profession-config.yml`
- `docs/СОЦИАЛЬНОЕ_ЯДРО.md`
- `docs/АРХИТЕКТУРА_КОДА.md`
- `docs/ПЛАН.md`
- `CHANGELOG.md`
- этот отчёт.

В diff нет файлов `PrisonColony/`, игровых XML, DLL или APK.

## Архитектурные решения

- Направление зависимостей: `ProfessionConfig → Determinism`; `Core → Determinism + ProfessionConfig`; `Social → Core`. Циклических ссылок нет.
- Core и Social остаются обычными .NET-библиотеками без ссылок на RimWorld, Verse, Unity, Harmony или Ports1de.
- Социальное состояние создаётся из готового ростера, а не генерирует собственные resident ID.
- Журналы остаются приватными полями агрегата. View-style API ограничивает обычный доступ конкретным жителем, но не является криптографической границей внутри одного процесса: вызывающий слой обязан хранить и передавать NPC только его view.
- RelationshipLedger сохраняет симметричные отдельные шкалы Trust/Familiarity. Политика слухов обязательна; тестовые значения синтетические.
- Память остаётся последовательной и не имеет политики забывания/лимита размера. Потокобезопасность не заявляется.

## Источники и лицензии

- Core-код и smoke tests перенесены из собственной ветки этого же репозитория `daily/2026-11-18` после просмотра исходников. Это не сторонний код и не перенос старой ветки PR #1.
- Новые классы интеграции, интеграционные тесты и документация написаны для этого репозитория.
- SplitMix64 — опубликованный алгоритм; сторонняя реализация исходного кода не копировалась.
- NuGet-пакеты, внешние DLL, ассеты и APK не добавлялись.
- Workflow использует официальные GitHub Actions `actions/checkout@v7` и `actions/setup-dotnet@v6`; сторонний код в репозиторий не копировался.
- GitHub metadata сообщает `license=null`; файла LICENSE в репозитории нет. Лицензия проекта не назначалась.

## Тесты и результаты



- Push workflow [44637323112 — Social and station integration checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637323112): success. Release build Core, Core invariant checks, Release build Social, social domain checks и двенадцатичленный integration suite завершились успешно.
- Push workflow [44637323041 — Profession quota checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637323041): success. JSON Schema syntax, Release build и quota tests завершились успешно.
- PR workflow [44637425262 — Social and station integration checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637425262): success; все build/test steps завершились успешно.
- PR workflow [44637425185 — Profession quota checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637425185): success.
- В C#-проектах включены Nullable и TreatWarningsAsErrors.
- Локальный .NET SDK отсутствует (`dotnet: command not found`); локальная сборка и запуск C# тестов не выполнялись. Проверки подтверждены удалёнными GitHub Actions.
- Android / Ports1de device test: NOT RUN. Runtime, managed DLL и загрузчик конкретной сборки не подтверждены.
- JSON Schema проверена workflow командой `python3 -m json.tool`; содержимое схемы не менялось.
- После добавления отчёта PR workflow повторно прошёл на head `1d76183abc2b7d4c4931dc5fac90be5ce88a5806`: [44637607410 — Social and station integration checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637607410) и [44637607514 — Profession quota checks](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44637607514) завершились success. Последующий commit меняет только этот отчёт; C#-код и тесты не менялись.

## Commit, branch и PR

- Базовый main: [e49c7ec](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/e49c7ecaa591fd7b2cd67220a28e251f429820d1).
- Кодовый commit: [04c5673](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/04c5673032ef5a8b743e8e528d92b55ca3748763).
- Ветка: [daily/2026-11-20](https://github.com/azartworld55-ctrl/Protokol-Nechto/tree/daily/2026-11-20).
- Draft PR: [#6 — Интеграция ростера из 12 жителей с социальным ядром](https://github.com/azartworld55-ctrl/Protokol-Nechto/pull/6), open, `mergeable=true`, `mergeable_state=clean`.
- Сравнение `main...daily/2026-11-20` после добавления отчёта: 2 commits ahead, 0 behind; 25 файлов, +1499 / −192.
- PR #1 и PR #4 остаются открытыми Draft; PR #5 уже слит ранее.
- Изменения не слиты в main.

## Что оставить этапу 03:00

1. Провести независимый review `StationSocialState` / `ResidentSocialView`: владение view, недопуск чужих ID, отсутствие API для глобального перечисления памяти.
2. Проверить интеграционные инварианты на повторной генерации и крайних seed; отдельно оценить стабильность Guid-контракта при смене runtime.
3. Решить политику ограничения роста памяти, забывания и архивирования; не вводить произвольные игровые числа.
4. Следующая крупная задача по `docs/ПЛАН.md` после интеграции — расписания, потребности и автономная работа NPC. Начинать её только после ревью сегодняшнего интеграционного слоя.
5. Не добавлять Scribe/save API, игровые патчи или сенсорный UI до подтверждения runtime, managed DLL и загрузчика Ports1de.
