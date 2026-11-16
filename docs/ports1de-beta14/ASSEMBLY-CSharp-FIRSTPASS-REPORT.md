# Технический отчёт: Assembly-CSharp-firstpass.dll

**Источник:** текст технического анализа, предоставленный пользователем в переписке 16.11.2026.  
**Статус:** выводы внешнего анализа; исходная DLL независимо не перепроверялась при сохранении отчёта.

## 1. Идентификация сборки

- Assembly name: `Assembly-CSharp-firstpass`
- Assembly version: `0.0.0.0`
- Culture: Neutral
- Public key: отсутствует
- Module: `Assembly-CSharp-firstpass.dll`
- MVID: `c42f9eda-c253-4650-890f-5f44ceb4f224`
- Hash algorithm: `0x00008004`
- Обнаруженные атрибуты: `CompilationRelaxationsAttribute`, `RuntimeCompatibilityAttribute`, `DebuggableAttribute`, `TargetFrameworkAttribute`
- FileVersion и InformationalVersion достоверно не установлены.

## 2. Платформа и CLR

В отчёте указано:

- TargetFramework: `.NETCoreApp,Version=v10.0`
- FrameworkDisplayName: `.NET 10.0`
- CLR header: 2.5
- CLR metadata version: `v4.0.30319`
- PE format: PE32
- Machine: I386 (`0x014C`)
- CorFlags: `0x00000001`
- ILOnly: True

Отчёт отдельно предупреждает: CLR metadata version `v4.0.30319` не следует путать с целевой платформой .NET 10.0.

Наличие CLR metadata не устанавливает фактический runtime Ports1de и не доказывает использование Mono, IL2CPP или конкретной версии .NET.

## 3. Ссылки на сборки

В отчёте заявлены две записи AssemblyRef:

| Сборка | Версия | PublicKeyToken |
|---|---:|---|
| `System.Runtime` | 10.0.0.0 | `b03f5f7f11d50a3a` |
| `UnityStubs` | 1.0.0.0 | отсутствует |

Это ссылки из метаданных, а не доказательство фактической загрузки этих библиотек.

## 4. Типы и пространства имён

По отчёту:

- В TypeDef присутствует только `<Module>` (1 запись).
- Обнаружено 770 ExportedType.
- 641 запись имеет флаг Forwarder (`0x00200000`) и перенаправляет типы в другую сборку.
- В числе пространств имён указаны:
  - UnityEngine — 276;
  - UnityEngine.Rendering — 32;
  - JetBrains.Annotations — 34;
  - Unity.Collections — 30;
  - UnityEngine.InputSystem — 18;
  - UnityEngine.UI — 17;
  - Unity.Mathematics — 17;
  - UnityEngine.Bindings — 16;
  - Unity.Collections.LowLevel.Unsafe — 15;
  - UnityEngine.Networking — 14;
  - TMPro — 12.

Пространства имён `Verse` и `RimWorld` не обнаружены. Unity-типы представлены как экспортируемые типы, а не собственные определения этой DLL.

## 5. Игровые API и Harmony

В отчёте не обнаружены определения или экспортируемые типы для:

- `Verse`, `RimWorld`;
- `Mod`, `Map`, `Pawn`, `Scribe`;
- `HarmonyLib`, `0Harmony`.

Также не обнаружены игровые API загрузки модов, карт, пешек или сохранений.

## 6. Вывод исходного анализа

Автор анализа классифицировал файл как вспомогательную Unity-сборку/сборку-посредник, а не как основную игровую логику RimWorld. В качестве следующего файла для анализа был указан `Assembly-CSharp.dll`.

## 7. Что остаётся неизвестным

- Фактический runtime Ports1de Beta 14.
- Используется ли Mono.
- Версия Unity игры.
- Наличие игровых API в других DLL.
- Точное назначение `UnityStubs.dll`.

**Важно:** это сохранение результатов стороннего анализа. Утверждения о назначении и структуре DLL пока не являются независимой проверкой исходного бинарного файла.
