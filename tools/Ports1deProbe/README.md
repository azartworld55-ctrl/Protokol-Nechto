# Ports1de Runtime Probe

Изолированный диагностический мод. Он нужен только для ответа на один вопрос: загружает ли установленная Ports1de пользовательскую DLL и вызывает ли конструктор `Verse.Mod`.

Пробник:
- не ссылается на Harmony;
- не содержит Harmony-патчей;
- не меняет пешек, карту, сохранения или игровой процесс;
- при создании экземпляра пишет в лог маркер `[Ports1deProbe] ENTRYPOINT_REACHED`, версии сборок и сведения о runtime.

## Важное ограничение

Скриншоты устройства подтверждают RimWorld 1.7.4990 (64-bit), Ports1de 0.0.20 / UI 0.0.20 и Harmony 3.0.1.0. Они не раскрывают runtime загрузчика пользовательских DLL. Поэтому `net10.0` здесь — проверяемая гипотеза, унаследованная от текущего адаптера проекта, а не установленный факт. Если сборка не загрузится, сначала выясняем runtime и целевой framework, а не меняем игру или отключаем защиту Android.

## Сборка

Нужен каталог игровых managed-сборок именно той установки, под которую собирается пробник:

```bash
RIMWORLD_ASSEMBLY_DIR=/path/to/RimWorld_Data/Managed dotnet build tools/Ports1deProbe/Ports1deProbe.csproj -c Release
```

В PowerShell:

```powershell
$env:RIMWORLD_ASSEMBLY_DIR = "C:\path\to\Managed"
dotnet build tools/Ports1deProbe/Ports1deProbe.csproj -c Release
```

Ожидаемый выходной файл: `tools/Ports1deProbe/bin/Release/Ports1deProbe.dll`.

## Упаковка для импорта

Создать ZIP так, чтобы в корне архива лежали папки `About` и `Assemblies`:

```text
Ports1deProbe/
  About/
    About.xml
  Assemblies/
    Ports1deProbe.dll
```

Копировать DLL в `Assemblies` можно только после успешной сборки. Не добавлять в архив `Assembly-CSharp.dll`, Harmony или другие игровые DLL.

## Проверка на устройстве

1. Импортировать ZIP штатным импортёром Ports1de.
2. В менеджере модов включить `Ports1de Runtime Probe`.
3. Перезапустить игру.
4. Проверить лог игры на строку `[Ports1deProbe] ENTRYPOINT_REACHED`.
5. Если маркера нет, сохранить точный текст ошибки загрузчика/лога. Сам факт появления мода в списке означает только распознавание `About.xml`, а не успешную загрузку DLL.

Удалить пробник после проверки можно обычным способом через менеджер модов/управление установленными модами. Он не нужен для обычной игры.
