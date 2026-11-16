# UnityStubs.dll — технический отчёт Ports1de Beta 14

> Источник: технический анализ бинарного файла, предоставленный пользователем в чате. Ниже зафиксированы результаты именно этого отчёта; независимое повторное исследование DLL в рамках этого коммита не выполнялось.

## Идентификация

- Имя сборки: `UnityStubs`
- Версия сборки: `1.0.0.0`
- Размер: 964 096 байт (941,5 КиБ)
- Модуль: `UnityStubs.dll`
- PE: PE32, Machine I386 (0x014C)
- CLR Header: 2.5; CorFlags `0x00000001` (ILOnly)
- TargetFramework: `.NETCoreApp,Version=v10.0`
- CLR Metadata Version: `v4.0.30319` (само по себе не означает .NET Framework 4.0)

## Состав и зависимости

По отчёту, в AssemblyRef найдено 34 ссылки. Среди значимых:
- .NET 10: `System.Runtime`, `System.Collections`, `System.Linq`, `System.Threading`, `System.Net.Http`, `System.Text.Json` и другие.
- Waylith: `Waylith`, `Waylith.UnityCompat`, `Waylith.AssetBake`, `Waylith.VideoDecode`.
- Brutal: `Brutal.Core.Logging`, `Brutal.Vulkan`, `Brutal.Glfw`, `Brutal.Core.Numerics`.
- Также указаны `AssetsTools.NET`, `Fmod5Sharp`, `AndroidCompat`, Mono/.NET системные зависимости.

Это ссылки AssemblyRef; они не доказывают, что каждая зависимость реально используется при запуске.

## Типы и API

- Всего TypeDef: 941.
- Типов в пространстве имён `UnityEngine`: 286.
- Также обнаружены `UnityEngine.Rendering`, `UnityEngine.InputSystem`, `UnityEngine.UI`, `Unity.Collections`, `TMPro`, `UnityStubs.Internal`, `UnityStubs.Gui`, `UnityStubs.Rendering`, `Waylith.UnityCompat` и другие.
- Среди Unity API типов указаны `Object`, `GameObject`, `Component`, `MonoBehaviour`, `Transform`, `Camera`, `Texture2D`, `Sprite`, `Canvas`, `Physics`, `Vector2`, `Vector3`, `Mathf`, `AudioSource`, `Input`, `Screen`, `Application`, `Time`.
- В MethodDef обнаружены, например, `Object.Instantiate/Destroy/DontDestroyOnLoad`, `GameObject.AddComponent/GetComponent/Find/CreatePrimitive`, `Resources.Load/LoadAll`, методы `Time`, `Screen`, `Camera`, `Physics.Raycast`, `AudioSource.Play/Stop`, `Texture2D.SetPixel/GetPixels/Apply`.

Наличие типа или метода в метаданных не доказывает полноценную реализацию оригинального Unity-поведения.

## Признаки stub/compat слоя

Отчёт отмечает:
- сборка называется `UnityStubs`;
- есть `UnityStubs.Internal.StubWarner` с `LogStub`;
- присутствуют backend-интерфейсы `UnityStubs.Gui.IGuiBackend`, `GuiBackend`, `ITextMeshProBackend`, `UnityStubs.Rendering.ICameraTextureRenderer`, `IScreenCaptureBackend`;
- некоторые методы имеют IL-реализацию и вспомогательную логику, поэтому некорректно считать абсолютно все методы пустыми заглушками.

Рабочая характеристика из отчёта: управляемый Unity-совместимый слой, сочетающий stub-механизмы и собственную реализацию отдельных компонентов; не полноценная замена оригинальному Unity Engine.

## Связь с другими сборками

- В `Assembly-CSharp-firstpass.dll` ранее обнаружена AssemblyRef на `UnityStubs, Version=1.0.0.0`.
- В firstpass отчёте указаны 770 ExportedType, из них 641 с флагом Forwarder.
- Отчёт связывает это с перенаправлением Unity-типов в определения из `UnityStubs.dll`.

В самой `UnityStubs.dll` не обнаружены определения `Verse`, `RimWorld`, `Pawn`, `Map`, `Thing`, `Mod`, `Scribe`; это не игровая сборка RimWorld и не замена `Assembly-CSharp.dll`.

## Выводы и ограничения

Поддержано предоставленным анализом:
1. Это управляемая CLR-сборка с TargetFramework .NET 10.0.
2. Она содержит значительный набор собственных UnityEngine-типов и связана с Waylith/Brutal/AndroidCompat.
3. Она используется как Unity-совместимый слой в исследуемой структуре Ports1de.
4. Firstpass ссылается на неё и, согласно предыдущему отчёту, перенаправляет часть Unity-типов.

Не установлено одним только анализом DLL:
- какой Runtime фактически запускает Ports1de;
- что это Mono, IL2CPP или конкретная реализация .NET Runtime;
- что каждый Unity API метод полноценно работает на устройстве;
- что библиотека официально называется или является исключительно компонентом Ports1de.

## Практическое значение для мода

- Подключать эту DLL можно рассматривать для разрешения Unity-типов при компиляции под конкретную сборку Ports1de.
- Не следует одновременно подмешивать несовпадающие стандартные Unity DLL без проверки.
- Для игровых API RimWorld всё равно нужна `Assembly-CSharp.dll`.
- Фактическую доступность Unity API нужно проверять в Runtime, а не только по успешной компиляции.

## Следующий приоритет исследования

`Assembly-CSharp.dll`: проверить игровые пространства имён `Verse` и `RimWorld`, типы `Pawn`, `Map`, `Thing`, `Mod`, `Scribe`, а также реальные сигнатуры методов, используемых будущим адаптером мода.
