# Prison Colony — технический прототип

Цель: RimWorld 1.7, Ports1de Beta 14.

## Структура

- `About/About.xml` — метаданные мода.
- `Assemblies/` — сюда собирается `PrisonColony.dll`.
- `Source/PrisonColony/` — проект C# и исходники.
- `Defs/` — игровые определения (добавляются после проверки загрузки DLL и XML на Ports1de).

## Сборка

Требуется .NET SDK с поддержкой .NET Framework 4.7.2 reference assemblies и доступ к папке `Managed` именно той установки RimWorld, под которую собирается мод.

```sh
dotnet build Source/PrisonColony/PrisonColony.csproj -p:RimWorldManagedPath="/path/to/RimWorld*_Data/Managed"
```

Можно задать переменную окружения `RIMWORLD_MANAGED` вместо параметра MSBuild.

Не копируйте DLL из другой версии RimWorld: бинарная совместимость с Ports1de Beta 14 пока не подтверждена.

## Текущий статус

- [x] Отдельный каталог PrisonColony.
- [x] About.xml для RimWorld 1.7.
- [x] Проект DLL с внешними ссылками на игровые сборки.
- [x] Точка входа Mod и компонент игровых сохранений.
- [ ] Проверка компиляции против фактических библиотек Ports1de Beta 14.
- [ ] Проверка загрузки и сохранения в самой игре.
- [ ] Полный цикл контрактов.
