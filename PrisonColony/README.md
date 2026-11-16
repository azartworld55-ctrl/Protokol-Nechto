# Prison Colony — технический прототип

Цель: RimWorld 1.7 / Ports1de Beta 14.

## Подтверждённая техническая база

Из технического репозитория `azartworld55-ctrl/Ports1de-technical-files`:
- RimWorld `1.7.9795.32073`;
- основная игровая сборка на `.NET 10.0`;
- игровые библиотеки: `Assembly-CSharp.dll`, `RimWorld.dll`, `UnityStubs.dll`, `UnityEngine.CoreModule.dll`;
- отдельный Harmony 3 и мобильный слой Ports1de.

См. [сводку](https://github.com/azartworld55-ctrl/Ports1de-technical-files/blob/main/docs/ports1de-beta14/PROJECT-KNOWLEDGE-SNAPSHOT.md) и [отчёт ILSpy](https://github.com/azartworld55-ctrl/Ports1de-technical-files/blob/main/docs/ports1de-beta14/ILSPY-MANAGED-STRUCTURE-REPORT.md).

## Структура

- `About/About.xml` — метаданные.
- `Assemblies/` — сюда собирается `PrisonColony.dll`.
- `Defs/MainButtonDefs/` — вкладка управления.
- `Source/PrisonColony/` — C#-проект и исходники.

## Сборка

Нужен .NET SDK 10 и каталог managed-сборок из собственной установки Beta 14:

```sh
dotnet build Source/PrisonColony/PrisonColony.csproj -p:RimWorldManagedPath="/path/to/Managed"
```

В каталоге должны находиться `Assembly-CSharp.dll`, `RimWorld.dll`, `UnityStubs.dll` и `UnityEngine.CoreModule.dll`. Можно задать `RIMWORLD_MANAGED`. Игровые DLL не копируются в репозиторий.

## Реализовано в исходниках

- Отдельная вкладка управления тюрьмой.
- Создание контрактного предложения: размер партии, срок 1–30 дней, дневная ставка.
- Принятие предложения с проверкой вместимости прототипа (8 мест).
- Учёт пешек при ручном тестовом приёме.
- Отсчёт срока только после регистрации всей партии.
- Ежедневное начисление внутренних кредитов.
- Перевод контракта в состояние ожидания отправления.
- Scribe-сохранение контрактов, записей заключённых и баланса.

## Пока не реализовано / не проверено

- Сборка DLL против реальных managed-сборок Beta 14.
- Запуск мода на Android/Ports1de.
- Фактический шаттл: текущий ручной приём выбранных пешек — тестовый адаптер.
- Фактическое отправление пешек после окончания срока.
- Обмен внутренних кредитов на серебро RimWorld.
- Автоматическое определение вместимости по кроватям/камерам.
- Round-trip тест сохранения на устройстве.
