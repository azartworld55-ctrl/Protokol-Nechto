# Ежедневная разработка — 18 ноября 2026, 02:00 (Europe/Moscow)

## Цель

Продвинуть независимое ядро жителей по этапу 4: перенести минимальную чистую модель ростера в актуальную линию от main, напрямую подключить к ней уже слитый ProfessionRosterComposer и обеспечить полную воспроизводимость всех полей 12 жителей. Не переносить целиком расходящуюся ветку PR №1 и не придумывать квоты профессий.

## Состояние перед работой

Перед изменениями прочитаны:

- README.md;
- docs/КОНЦЕПЦИЯ.md;
- docs/ПЛАН.md;
- docs/ТЕХНИЧЕСКАЯ_РАЗВЕДКА.md;
- docs/АРХИТЕКТУРА_КОДА.md;
- отчёты 2026-11-17: 02-00-development.md, 03-00-review.md, 04-00-final.md;
- финальный отчёт 2026-11-16.

Состояние GitHub на момент старта:

- main: 8c72c531ad706226e6b446af8b58250cb62d1117.
- PR №1 открыт как Draft; ветка feature/initial-social-core расходится с main: 127 коммитов впереди и 48 позади; GitHub сообщает mergeable=false. В PR остаются замечания по переполнению шкал отношений, доступу к памяти, автоматическому превращению близости в разговор, PRNG и квотам.
- PR №3 и его composer уже слиты в main.
- Ветки daily/2026-11-16 и daily/2026-11-17 существуют; daily/2026-11-18 отсутствовала.
- Создана daily/2026-11-18 от актуального main. Ветка не отстаёт от main.
- README называет «Тюремную колонию» текущим приоритетом, а «ПРОТОКОЛ: НЕЧТО» — отложенным проектом. По прямой задаче этого цикла изменялся только код и документация ростера «ПРОТОКОЛ: НЕЧТО»; файлы PrisonColony не затрагивались.

## Что реально реализовано

1. Добавлен независимый проект Protocol.Determinism с общим SplitMix64 PRNG:
   - фиксированные константы и тест-векторы;
   - unbiased bounded sampling для целых диапазонов;
   - стабильная генерация GUID;
   - Fisher–Yates shuffle;
   - генератор не использует System.Random и не предназначен для криптографии.

2. ProfessionQuotaAllocator переведён на общий StableRandom. Реализация алгоритма больше не дублируется внутри модуля квот.

3. Добавлен независимый Protocol.Core с immutable-моделью:
   - StationRoster всегда содержит ровно 12 жителей;
   - Resident хранит уникальный ID, уникальное имя, возраст, профессию, личность, привычки и индивидуальные особенности;
   - личность содержит пять параметров от 0 до 100;
   - возраст генератора — от 20 до 65 лет;
   - у каждого жителя от одной до трёх привычек и от одной до двух индивидуальных особенностей;
   - коллекции жителей, привычек и особенностей выдаются через read-only снимки.

4. StationRosterGenerator напрямую использует ProfessionRosterComposer:
   - конфигурация квот обязательна;
   - каждому из 12 профилей назначается ровно одна профессия;
   - итоговые количества совпадают с каждой квотой;
   - порядок жителей сохраняется;
   - нет встроенного распределения и временной профессии-заглушки;
   - игрок и отдельный внутренний ID паразита принадлежат ростеру и не совпадают.

5. Полная генерация воспроизводима. Для seed 20261118 и синтетической тестовой конфигурации зафиксирован SHA-256 полного снимка: ceb0c2ec3677af20bef6a671758021887f94d9425d8417ac8d242d26fc4c5715.

6. Обновлены README, план, архитектура, документация квот, добавлен документ docs/РОСТЕР_ЖИТЕЛЕЙ.md и запись в CHANGELOG.

Это не полное социальное ядро. Отношения, память, слухи, автономные решения и модель фактического общения не переносились из PR №1 и остаются незавершёнными.

## Изменённые файлы

- .github/workflows/resident-core.yml — новый workflow сборки и тестирования общего PRNG, квот и ростера.
- .github/workflows/profession-config.yml — path filter включает Protocol.Determinism.
- CHANGELOG.md.
- README.md.
- docs/АРХИТЕКТУРА_КОДА.md.
- docs/КОНФИГУРАЦИЯ_ПРОФЕССИЙ.md.
- docs/ПЛАН.md.
- docs/РОСТЕР_ЖИТЕЛЕЙ.md — новый документ.
- src/Protocol.Determinism/Protocol.Determinism.csproj — новый проект.
- src/Protocol.Determinism/StableRandom.cs — новый общий PRNG.
- src/Protocol.ProfessionConfig/Protocol.ProfessionConfig.csproj.
- src/Protocol.ProfessionConfig/ProfessionQuotaAllocator.cs.
- src/Protocol.Core/Protocol.Core.csproj — новый проект.
- src/Protocol.Core/Properties/AssemblyInfo.cs.
- src/Protocol.Core/Domain/Resident.cs.
- src/Protocol.Core/Domain/ResidentPersonality.cs.
- src/Protocol.Core/Domain/StationRoster.cs.
- src/Protocol.Core/Domain/StationRosterGenerator.cs.
- tests/Protocol.Core.SmokeTests/Protocol.Core.SmokeTests.csproj — новый проект.
- tests/Protocol.Core.SmokeTests/Program.cs — новые smoke tests.

## Архитектурные решения

- Два независимых экземпляра StableRandom получают один seed: один для личных профилей, другой внутри распределителя профессий. Изменение порядка квот не сдвигает последовательность имён, возраста и личности.
- Профессии назначаются только через валидную ProfessionQuotaConfiguration. Значения test-profession-alpha/beta/gamma используются только в тестах и не являются игровым балансом.
- Composer сначала меняет immutable ResidentDraft, затем профиль материализуется в Resident. Неподтверждённая профессия-заглушка не попадает в доменную модель.
- ID паразита хранится отдельно от публичных свойств Resident и доступен только внутреннему коду сборки. Логика паразита и его всеведение не реализованы.
- Protocol.Core и Protocol.Determinism не имеют ссылок на RimWorld, Verse, Unity, Harmony или Ports1de.

## Источники и лицензии

- Сторонние исходники, DLL, ассеты и NuGet-пакеты не добавлялись.
- StableRandom реализован в этом репозитории на основе опубликованного алгоритма SplitMix64; исходный код сторонних реализаций не копировался.
- Workflow использует официальные actions/checkout@v7 и actions/setup-dotnet@v6.
- В репозитории нет LICENSE; лицензия самого проекта остаётся невыбранной. Этот цикл не добавляет и не предполагает лицензию.

## Проверки и результаты

- Release build Protocol.Determinism: PASS, 0 warnings / 0 errors.
- Release build Protocol.ProfessionConfig: PASS, 0 warnings / 0 errors.
- Existing profession quota regression tests: PASS.
- Release build Protocol.Core: PASS, 0 warnings / 0 errors.
- Resident core smoke tests: PASS, 2091 assertions.
- SplitMix64 test vectors: PASS.
- Full roster golden snapshot: PASS.
- JSON Schema syntax via python3 -m json.tool: PASS в profession-config workflow.
- GitHub Actions resident core push run: [44203944505 — success](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44203944505).
- GitHub Actions profession quota PR run: [44203950975 — success](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/44203950975).
- Повторный resident-core PR run 44203951038 на момент записи ещё выполнялся; успешный push run выше относится к тому же head SHA 0249465bc16c92a235da96526f7c3658a37b6eaf.
- Первый CI запуск на исходном коммите выявил ошибку generic-типа в callback composer. Она исправлена в commit e6041f3; после исправления сборка и тесты на итоговом кодовом commit прошли.
- Локальный dotnet build не выполнялся: в рабочем контейнере .NET SDK отсутствует. Проверки выполнены GitHub Actions.
- XML About.xml не изменялся и в diff отсутствует; это не загрузочный пакет RimWorld-мода. .csproj разобраны успешной сборкой.
- Android/Ports1de device test: NOT RUN. Runtime и DLL не подтверждены; mobile UI не реализован и не заявляется.

## Commit, branch и PR

- Branch: [daily/2026-11-18](https://github.com/azartworld55-ctrl/Protokol-Nechto/tree/daily/2026-11-18).
- Основной commit: [e8f51d2](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/e8f51d29688a452b1fd0ab78cfdfce7aa8ccb3e5).
- Исправление интеграции composer: [e6041f3](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/e6041f3faa79a2ee4c520513f795f774d3652cb6).
- Итоговый проверенный кодовый commit: [0249465](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/0249465bc16c92a235da96526f7c3658a37b6eaf).
- PR: [#4 — Генерация 12 жителей с детерминированными квотами профессий](https://github.com/azartworld55-ctrl/Protokol-Nechto/pull/4), открыт как Draft.
- Compare main...daily/2026-11-18: 3 commits ahead, 0 behind, 20 файлов в code/documentation diff на момент проверки до добавления этого отчёта.
- main не изменён. PR #1 не сливался.

## Что оставить этапу 03:00

1. Проверить итоговый diff PR #4, workflow statuses и документацию после добавления отчёта.
2. Отдельно решить, что делать с PR #1: не сливать расходящуюся ветку без исправления review-замечаний.
3. Следующая крупная задача социального ядра:
   - безопасная арифметика доверия и знакомства через Int64 до clamp;
   - закрытый MemoryJournal и player-scoped read model вместо публичного доступа ко всем ResidentMind;
   - разделение физической достижимости и фактического события разговора;
   - передача слухов только при подтверждённом событии общения и затухание достоверности при каждом пересказе.
4. После социальной модели добавить интеграционные тесты полного жизненного цикла памяти и слуха. Только затем переходить к автономному поведению NPC.
5. Не добавлять игровой адаптер, Verse/Harmony hooks, save API или мобильный UI до подтверждения runtime и DLL Ports1de. Не менять стандартное сенсорное управление порта.

