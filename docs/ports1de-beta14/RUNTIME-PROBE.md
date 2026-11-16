# Runtime Probe — Ports1de Beta 14

## Почему тест разделён на две стадии

Нельзя проверять загрузку DLL и Harmony одним модом: если загрузчик не поддерживает DLL, Harmony-тест не запустится и причина ошибки останется неясной.

Поэтому используем две независимые проверки:

- **A1 — Ports1deProbe:** отдельный минимальный мод без Harmony. Проверяет только обнаружение DLL, создание экземпляра Verse.Mod и фактический runtime.
- **A2 — Harmony self-patch:** основной адаптер Protocol.RimWorld. Запускается только после успешного A1; проверяет, что Harmony загружается и выполняет postfix на безопасном тестовом методе самого мода.

A2 не доказывает, что Ports1de разрешает патчить методы RimWorld. Для этого позже потребуется отдельный no-op postfix на одном точно проверенном игровом методе.

## A1 — изолированный DLL loader probe

Исходники:

- `tools/Ports1deProbe/Ports1deProbe.csproj`
- `tools/Ports1deProbe/ProbeMod.cs`
- `tools/Ports1deProbe/About/About.xml`
- инструкция: `tools/Ports1deProbe/README.md`

Пробник:
- не ссылается на Harmony;
- не содержит Harmony-патчей;
- не меняет пешек, карту, сохранения или игровой процесс;
- при вызове конструктора Verse.Mod пишет маркер `[Ports1deProbe] ENTRYPOINT_REACHED`;
- выводит полные имена сборок, runtime, target framework и архитектуру процесса/ОС.

### Сборка A1

Нужен каталог managed-сборок той самой игры, которую запускает пользователь. В нём должен находиться `Assembly-CSharp.dll`.

Bash:

```bash
export RIMWORLD_ASSEMBLY_DIR="/path/to/target/Managed"
dotnet build tools/Ports1deProbe/Ports1deProbe.csproj --configuration Release
```

PowerShell:

```powershell
$env:RIMWORLD_ASSEMBLY_DIR = "C:\path\to\target\Managed"
dotnet build tools/Ports1deProbe/Ports1deProbe.csproj --configuration Release
```

Ожидаемый файл: `tools/Ports1deProbe/bin/Release/Ports1deProbe.dll`.

Упаковать его в ZIP с такой структурой:

```text
Ports1deProbe/
  About/
    About.xml
  Assemblies/
    Ports1deProbe.dll
```

В архив не добавлять `Assembly-CSharp.dll`, Harmony или другие игровые DLL.

### Проверка A1 на устройстве

1. Сохранить текущий список модов и важные сохранения.
2. Импортировать ZIP штатным импортёром Ports1de.
3. Включить только Ports1de Runtime Probe; не добавлять Harmony как зависимость пробника.
4. Перезапустить игру.
5. Проверить лог на `[Ports1deProbe] ENTRYPOINT_REACHED`.
6. Сохранить строки о Runtime, Target framework, Verse assembly и архитектуре.
7. Если маркера нет — сохранить первую ошибку загрузчика и stack trace.

Сам факт появления мода в списке подтверждает только чтение About.xml, но не загрузку DLL.

### Критерий A1

A1 пройден только если маркер ENTRYPOINT_REACHED реально появился в логе Ports1de на Android. Успешная локальная сборка или появление в менеджере модов не засчитываются.

## A2 — Harmony self-patch в Protocol.RimWorld

A2 выполнять только после PASS по A1 и уточнения фактических версий игры/runtime.

Исходники:

- `src/Protocol.RimWorld/Protocol.RimWorld.csproj`
- `src/Protocol.RimWorld/ProtocolMod.cs`
- `src/Protocol.RimWorld/ProtocolRuntimeProbe.cs`

Адаптер:
- ссылается на `Assembly-CSharp.dll`;
- ссылается на Harmony DLL из установленной Harmony-зависимости;
- не включает игровые DLL или Harmony в пакет;
- регистрирует postfix на собственном безвредном sentinel-методе;
- проверяет, что результат изменился с `protocol-probe-original` на `protocol-probe-patched`.

Этот тест проверяет загрузку Harmony, регистрацию патча и исполнение postfix в сборке мода. Он **не** подтверждает возможность патчить методы Assembly-CSharp.

### Сборка A2

Нужны:
- `RIMWORLD_ASSEMBLY_DIR` — каталог с `Assembly-CSharp.dll` и, если есть, `UnityStubs.dll`;
- `RIMWORLD_HARMONY_DLL` — путь к Harmony DLL, соответствующей установленной игре.

Bash:

```bash
export RIMWORLD_ASSEMBLY_DIR="/path/to/target/Managed"
export RIMWORLD_HARMONY_DLL="/path/to/target/Harmony/0Harmony.dll"
dotnet build src/Protocol.RimWorld/Protocol.RimWorld.csproj --configuration Release
```

PowerShell:

```powershell
$env:RIMWORLD_ASSEMBLY_DIR = "C:\path\to\target\Managed"
$env:RIMWORLD_HARMONY_DLL = "C:\path\to\target\Harmony\0Harmony.dll"
dotnet build src/Protocol.RimWorld/Protocol.RimWorld.csproj --configuration Release
```

Выход адаптера настроен в `1.7/Assemblies/`. В пакет мода включить `About/` и собранные DLL проекта. Не копировать туда `Assembly-CSharp.dll`, `UnityStubs.dll` или `0Harmony.dll`.

### Ожидаемые сообщения A2

```text
[ПРОТОКОЛ: НЕЧТО] Harmony self-patch probe passed: the postfix executed and changed the sentinel result.
[ПРОТОКОЛ: НЕЧТО] Adapter constructor reached. ... Harmony self-patch probe: PASS.
```

Если появляется FAIL или исключение, сохранить весь текст ошибки и stack trace.

## Важное расхождение версий

В заметках по скриншотам устройства от 16.11.2026 записаны RimWorld `1.7.4990 (64-bit)`, Ports1de `0.0.20`, UI `0.0.20` и Harmony `3.0.1.0`. Статический отчёт по APK, загруженному в технический Release, содержит иные значения AssemblyVersion для некоторых сборок. Нельзя считать их одной и той же установкой без повторной сверки. Собирать нужно против DLL фактической установки, которую пользователь тестирует.

## Текущий статус

- Код A1 существует; сборка и запуск на устройстве не подтверждены.
- Код A2 self-patch добавлен в основной адаптер; сборка и запуск на устройстве не подтверждены.
- Точный runtime и framework для модовых DLL ещё не доказаны.
- Реальный Harmony patch на методе RimWorld ещё не проверен.
- Никакие игровые механики не должны подключаться до прохождения A1 и A2.

## Как фиксировать результат

Для каждого этапа записать:
- PASS/FAIL;
- Ports1de build и RimWorld version с экрана устройства;
- Verse/Assembly-CSharp assembly identity;
- runtime и target framework из лога A1;
- Harmony assembly identity (для A2);
- первую ошибку и полный stack trace при FAIL;
- commit SHA исходников, которые были собраны.

Ворота A считаются пройденными только после фактических успешных тестов на устройстве, а не по наличию исходников.
