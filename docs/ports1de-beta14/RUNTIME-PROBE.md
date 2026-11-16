# Runtime Probe — Ports1de Beta 14

## Purpose

This is the first deliberately small runtime test for ПРОТОКОЛ: НЕЧТО. It is not a gameplay feature and it does not change RimWorld simulation.

The probe checks four separate things:

1. Ports1de discovers the mod and reads its metadata.
2. RimWorld constructs the mod's `Verse.Mod` entry point.
3. The mod can resolve the Harmony assembly supplied by the installed Harmony dependency.
4. Harmony can register and execute a postfix against a harmless sentinel method inside the mod's own assembly.

The fourth check is a **Harmony self-patch**. It proves that the Harmony patch pipeline can execute in the loaded mod assembly. It does **not** prove that Ports1de permits patching arbitrary methods in `Assembly-CSharp.dll`. A separate, narrowly scoped game-method patch test remains a later gate.

## Files involved

- `src/Protocol.RimWorld/ProtocolMod.cs` — Verse entry point, version diagnostics and probe result.
- `src/Protocol.RimWorld/ProtocolRuntimeProbe.cs` — sentinel method and postfix.
- `src/Protocol.RimWorld/Protocol.RimWorld.csproj` — references the game and Harmony DLLs supplied by the local test installation.

No copy of RimWorld, UnityStubs or Harmony is committed to this repository.

## Build prerequisites

Use the exact Ports1de Beta 14 game assemblies extracted from the APK already recorded in the private technical repository, plus the `0Harmony.dll` that the target installation actually supplies for the Harmony dependency. Do not substitute DLLs from a different RimWorld version.

The adapter project expects:

- `RIMWORLD_ASSEMBLY_DIR` — directory containing `Assembly-CSharp.dll` and, where present, `UnityStubs.dll`.
- `RIMWORLD_HARMONY_DLL` — full path to the target `0Harmony.dll`.

From the repository root, build with:

```bash
dotnet build src/Protocol.RimWorld/Protocol.RimWorld.csproj --configuration Release
```

The build output is configured for `1.7/Assemblies/`. The mod package must contain the existing `About/About.xml` and the adapter output, including `Protocol.RimWorld.dll` and its project-reference output `Protocol.Core.dll`. Do not copy `Assembly-CSharp.dll`, `UnityStubs.dll` or `0Harmony.dll` into the mod package.

Example environment-variable setup (adapt the paths to the local test environment):

PowerShell:

```powershell
$env:RIMWORLD_ASSEMBLY_DIR = "C:\path\to\Ports1deBeta14\Managed"
$env:RIMWORLD_HARMONY_DLL = "C:\path\to\Harmony\0Harmony.dll"
dotnet build src/Protocol.RimWorld/Protocol.RimWorld.csproj --configuration Release
```

Bash:

```bash
export RIMWORLD_ASSEMBLY_DIR="/path/to/Ports1deBeta14/Managed"
export RIMWORLD_HARMONY_DLL="/path/to/Harmony/0Harmony.dll"
dotnet build src/Protocol.RimWorld/Protocol.RimWorld.csproj --configuration Release
```

## Device test

1. Keep a backup of the current Ports1de save and mod configuration.
2. Make sure the Harmony dependency is installed and enabled before ПРОТОКОЛ: НЕЧТО.
3. Import the mod folder containing `About/` and `1.7/Assemblies/` using the mod-import route provided by the installed Ports1de build.
4. Enable ПРОТОКОЛ: НЕЧТО and start the game.
5. Check the game log for both expected messages below.
6. If startup fails, disable the probe mod, restore the previous mod configuration and capture the first relevant exception plus the surrounding stack trace.

Do not add gameplay patches or other mods during this first test. The goal is to isolate loader and Harmony behaviour.

## Expected successful log messages

The exact version numbers depend on the installed files, but the messages should include:

```text
[ПРОТОКОЛ: НЕЧТО] Harmony self-patch probe passed: the postfix executed and changed the sentinel result.
[ПРОТОКОЛ: НЕЧТО] Adapter constructor reached. ... Harmony self-patch probe: PASS.
```

The first message means the postfix changed the sentinel return value from `protocol-probe-original` to `protocol-probe-patched`. The second confirms that the Verse entry point completed its diagnostic path.

## How to record the result

- **PASS — loader:** Ports1de lists and enables the mod.
- **PASS — Verse entry:** the adapter constructor diagnostic appears.
- **PASS — Harmony self-patch:** the postfix diagnostic appears and reports PASS.
- **FAIL — build:** compiler output, exact target DLL versions and command used.
- **FAIL — loader:** first load exception and stack trace.
- **FAIL — Harmony:** whether the adapter constructor ran, plus the complete probe error.

Do not mark the runtime gate complete from a successful local compile alone. The gate is complete only after the successful messages are observed in Ports1de on the target Android device.

## Next gate after this probe

After the self-patch passes, add one separate no-op postfix against a carefully selected, harmless RimWorld method whose exact declaring type and signature have been verified against the same Beta 14 `Assembly-CSharp.dll`. Confirm that it runs once at the intended point, does not alter the game's result, and does not spam logs. Only then begin live Pawn/Map integration.
