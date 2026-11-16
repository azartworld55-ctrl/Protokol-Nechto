# Prison Colony — технический прототип

Цель: RimWorld 1.7 / Ports1de Beta 14.

## Подтверждённые сведения о целевой сборке

Технический репозиторий `azartworld55-ctrl/Ports1de-technical-files` содержит статический анализ APK Beta 14 и вложенного `assets/bundled-game.zip`.

Из отчётов подтверждено:
- RimWorld `Assembly-CSharp.dll`: версия игры `1.7.9795.32073`.
- Target Framework основной игровой сборки: `.NETCoreApp,Version=v10.0`.
- В комплекте присутствуют отдельные `RimWorld.dll`, `UnityStubs.dll`, `UnityEngine.CoreModule.dll` и `0Harmony.dll`.
- `Assembly-CSharp.dll` содержит Verse/RimWorld игровую поверхность API; `UnityStubs.dll` не является заменой игровым типам.
- Порт имеет собственные ModCompatibility/ModPreparer и слой PortUI. Это не гарантирует совместимость любой DLL или Harmony-патча.

Источники:
- [Project knowledge snapshot](https://github.com/azartworld55-ctrl/Ports1de-technical-files/blob/main/docs/ports1de-beta14/PROJECT-KNOWLEDGE-SNAPSHOT.md)
- [ILSpy managed structure report](https://github.com/azartworld55-ctrl/Ports1de-technical-files/blob/main/docs/ports1de-beta14/ILSPY-MANAGED-STRUCTURE-REPORT.md)
- [Compatibility research](https://github.com/azartworld55-ctrl/Ports1de-technical-files/blob/main/docs/ports1de-beta14/PORTS1DE-COMPATIBILITY-RESEARCH.md)

Это статические сведения. Запуск DLL и сохранение игры на устройстве пока не проверены.

## Структура

- `About/About.xml` — метаданные мода.
- `Assemblies/` — сюда собирается `PrisonColony.dll`.
- `Source/PrisonColony/` — проект C# и исходники.
- `Defs/` — игровые определения, добавляются по мере реализации.

## Сборка

Проект нацелен на `.NET 10.0`, соответствующий Target Framework игровой сборки Ports1de Beta 14.

Нужно извлечь необходимые managed-сборки из собственной установленной копии игры и указать каталог, содержащий:
- `Assembly-CSharp.dll`
- `RimWorld.dll`
- `UnityStubs.dll`
- `UnityEngine.CoreModule.dll`

Пример:

```sh
dotnet build Source/PrisonColony/PrisonColony.csproj -p:RimWorldManagedPath="/path/to/Managed"
```

Можно задать переменную окружения `RIMWORLD_MANAGED`. Игровые DLL не хранятся в этом репозитории и не должны добавляться в него.

## Текущий статус

- [x] Отдельный каталог PrisonColony.
- [x] About.xml для RimWorld 1.7.
- [x] Проект DLL на .NET 10.0.
- [x] Ссылки на реальные игровые сборки из инвентаря Beta 14.
- [x] Точка входа Mod и компонент игровых сохранений.
- [ ] Компиляция против извлечённых сборок Beta 14.
- [ ] Проверка загрузки в Ports1de на устройстве.
- [ ] Проверка round-trip сохранения/загрузки.
- [ ] Полный цикл контрактов.
