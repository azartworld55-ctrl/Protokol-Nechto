## 2026-11-18 — Детерминированная генерация 12 жителей с точными квотами

- Добавлен независимый Protocol.Determinism с фиксированным SplitMix64, unbiased bounded sampling, стабильными GUID и Fisher–Yates.
- ProfessionQuotaAllocator переведён на общий PRNG без дублирования алгоритма.
- Добавлен чистый Protocol.Core: immutable-профили 12 жителей с именами, возрастом, личностью, привычками, индивидуальными особенностями и профессиями из явной конфигурации.
- StationRosterGenerator напрямую использует ProfessionRosterComposer; реальные игровые квоты не задаются по умолчанию.
- Добавлены smoke tests на точные квоты, полный повторяемый снимок ростера, диапазоны, уникальность, стабильные PRNG-векторы и неизменяемость коллекций.
- Ports1de/Verse/Unity/Harmony API и игровые DLL не добавлялись; Android-загрузка и mobile UI не проверялись.

# Changelog

## 2026-11-17 — Композиция ростера из квот профессий

- Добавлен независимый `ProfessionRosterComposer`: он применяет ровно 12 детерминированных назначений к существующему упорядоченному списку жителей, сохраняет порядок и возвращает read-only коллекцию.
- Проверка входного ростера использует ограниченный снимок через явное перечисление и не доверяет пользовательскому `ICollection<T>.CopyTo`.
- Конфигурация квот ограничивает число положительных записей двенадцатью; JSON-десериализатор отклоняет 13-ю запись до добавления в рабочий список.
- Добавлены регрессионные тесты для несовпадения Count/перечисления, некорректного CopyTo, изменения исходной коллекции, предельного числа квот и JSON-записей.
- GitHub Actions [run 43999262462](https://github.com/azartworld55-ctrl/Protokol-Nechto/actions/runs/43999262462): success; Release build — 0 warnings / 0 errors; smoke tests passed.
- [PR #3](https://github.com/azartworld55-ctrl/Protokol-Nechto/pull/3) — [squash commit e8af350](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/e8af35009fb7e3dac9fc2bd746d7994ed32d7368).

## 2026-11-16 — Конфигурация квот профессий для 12 жителей

- Добавлен независимый проект `Protocol.ProfessionConfig` с версионированным JSON-контрактом и JSON Schema для явных квот профессий.
- Добавлена проверка уникальности ID и суммы квот ровно в 12 жителей.
- Добавлен детерминированный распределитель Fisher–Yates на SplitMix64 с устранением modulo bias.
- Добавлены регрессионные тесты границ JSON, null/whitespace input, нормализации ID, Unicode/escaping, 12 отдельных квот, крайних seed и 513 последовательных seed.
- GitHub Actions обновлён до `actions/checkout@v7` и `actions/setup-dotnet@v6` (Node 24).
- [PR #2](https://github.com/azartworld55-ctrl/Protokol-Nechto/pull/2) — [merge commit d32b52c](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/d32b52c4e8183c776011e8303c1a59065c8db541).

Версия проекта не назначалась: схема версионирования пока не утверждена.
