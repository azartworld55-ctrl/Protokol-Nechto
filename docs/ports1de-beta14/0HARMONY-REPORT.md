# Технический отчёт: 0Harmony.dll — Ports1de Beta 14

**Источник:** текст технического анализа, предоставленный пользователем в переписке 16.11.2026.  
**Статус:** выводы внешнего анализа; DLL и таблицы метаданных независимо не перепроверялись при сохранении отчёта.

## 1. Идентификация

- Assembly name: `0Harmony`
- Assembly version: `3.0.0.0`
- Culture: neutral
- PublicKeyToken: null
- Module: `0Harmony.dll`
- Размер: 2 573 312 байт (примерно 2,45 MiB)
- PE format: PE32
- Machine: I386

В отчёте версия классифицирована как Harmony 3. Указано, что Assembly Version 3.0.0.0 согласуется с обнаруженными признаками Harmony 3, но не определяет исходный Git-коммит или конкретный релиз.

## 2. TargetFramework и CLR

В метаданных, согласно отчёту, обнаружено:

- TargetFramework: `.NETCoreApp,Version=v10.0`
- FrameworkDisplayName: `.NET 10.0`
- CLR metadata version: `v4.0.30319`

PE/CLR metadata сами по себе не доказывают, какой runtime фактически загружает сборку на Android.

## 3. Зависимости

В тексте анализа перечислены следующие идентификаторы зависимостей:

| Сборка | Версия |
|---|---:|
| `Mono.Cecil` | 0.11.6.0 |
| `Mono.Cecil.Mdb` | 0.11.6.0 |
| `Mono.Cecil.Pdb` | 0.11.6.0 |
| `Mono.Cecil.Rocks` | 0.11.6.0 |
| `MonoMod.Backports` | 1.1.2.0 |
| `MonoMod.Core` | 1.3.6.0 |
| `MonoMod.Iced` | 1.21.0.0 |
| `MonoMod.ILHelpers` | 1.1.0.0 |
| `MonoMod.Utils` | 25.0.14.0 |
| `System.Runtime` | 10.0.0.0 |

Оговорка исходного анализа: это идентификаторы, найденные в строковом содержимом DLL; полный перечень AssemblyRef не был независимо подтверждён декодированием таблицы метаданных. Поэтому список нельзя считать окончательным списком обязательных runtime-зависимостей.

## 4. Harmony API

В отчёте указано наличие пространства имён `HarmonyLib` и типов:

- `Harmony`
- `HarmonyMethod`
- `CodeInstruction`
- `AccessTools`
- `AccessToolsExtensions`
- `CodeInstructionExtensions`
- `CodeInstructionsExtensions`
- `HarmonyPatchType)

Также обнаружены имена методов/механизмов:

- `Patch`
- `PatchAll`
- `UnpatchAll`
- `PatchCategory`
- `UnpatchCategory`
- `GetPatchedMethods)

В отчёте отмечено наличие имён атрибутов/механизмов:

- `HarmonyPrefix`
- `HarmonyPostfix`
- `HarmonyTranspiler`
- `HarmonyFinalizer`

Также перечислены параметры патчей: `__instance`, `__originalMethod`, `__originalMember`, `__result`, `__resultRef`, `__state`, `__exception`, `__runOriginal`.

Наличие имён в метаданных/строках не заменяет проверку точных сигнатур перегрузок и практический тест каждого механизма.

## 5. MonoMod и динамические механизмы

В отчёте обнаружены ссылки/строки, связанные с:

- `MonoMod.Core`
- `MonoMod.Utils`
- `MonoMod.Backports`
- `MonoMod.ILHelpers`
- `MonoMod.Iced`
- `HarmonyDynamicMethod_`
- `HarmonyGenerated`
- `HarmonyInline`
- `HarmonyOuter`
- `System.Runtime.Loader.AssemblyLoadContext`
- `IsCollectible`
- `LoadFromAssemblyName`

Автор анализа интерпретировал их как признаки современной инфраструктуры Harmony 3 и динамической генерации/загрузки. Это не доказывает работоспособность этих механизмов в Android runtime Ports1de.

## 6. Android/Ports1de и совместимость

Специальная адаптация Harmony под Ports1de в отчёте не подтверждена. Не установлены:

- специальное пространство имён Ports1de;
- Android-специфичные Harmony-классы;
- фактическая поддержка динамической генерации методов на устройстве;
- фактическая загрузка Harmony и MonoMod в процессе игры.

Совпадение заявленного TargetFramework .NET 10.0 с данными из `Assembly-CSharp-firstpass.dll` является только совпадением метаданных, а не доказательством runtime-совместимости.

## 7. Предварительная рекомендация из исходного анализа

Внешний анализ рекомендовал рассматривать именно поставляемую Ports1de DLL `0Harmony.dll` как Harmony reference при проверке сборки, а не брать случайную Harmony 2.x или NuGet-сборку. В примере проекта предлагалось использовать ссылку на DLL с `Private=false`, если игра предоставляет её сама.

Эта рекомендация не подтверждает, что Harmony успешно загружается или работает в моде на устройстве. Не следует копировать MonoMod-зависимости в мод автоматически: сначала нужно установить, какие библиотеки предоставляет сама игра и как устроена загрузка сборок.

## 8. Что требуется проверить дальше

1. Точные сигнатуры публичных API Harmony 3.
2. Полную таблицу AssemblyRef и фактические зависимости.
3. Наличие и версии MonoMod DLL в сборке Ports1de.
4. Фактический runtime Android.
5. Минимальный тест загрузки мода и Harmony без игровых патчей.

**Важно:** этот документ фиксирует содержание предоставленного внешнего отчёта и не является самостоятельным подтверждением совместимости Harmony с Ports1de Beta 14.
