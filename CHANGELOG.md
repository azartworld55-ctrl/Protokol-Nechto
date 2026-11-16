# Changelog

## 2026-11-16 — Конфигурация квот профессий для 12 жителей

- Добавлен независимый проект `Protocol.ProfessionConfig` с версионированным JSON-контрактом и JSON Schema для явных квот профессий.
- Добавлена проверка уникальности ID и суммы квот ровно в 12 жителей.
- Добавлен детерминированный распределитель Fisher–Yates на SplitMix64 с устранением modulo bias.
- Добавлены регрессионные тесты границ JSON, null/whitespace input, нормализации ID, Unicode/escaping, 12 отдельных квот, крайних seed и 513 последовательных seed.
- GitHub Actions обновлён до `actions/checkout@v7` и `actions/setup-dotnet@v6` (Node 24).
- [PR #2](https://github.com/azartworld55-ctrl/Protokol-Nechto/pull/2) — [merge commit d32b52c](https://github.com/azartworld55-ctrl/Protokol-Nechto/commit/d32b52c4e8183c776011e8303c1a59065c8db541).

Версия проекта не назначалась: схема версионирования пока не утверждена.
