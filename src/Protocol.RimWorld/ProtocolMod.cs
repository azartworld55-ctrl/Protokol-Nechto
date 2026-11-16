using System.Reflection;
using HarmonyLib;
using Verse;

namespace Protocol.RimWorld;

/// <summary>
/// Minimal game-facing entry point and first runtime compatibility probe.
/// Gameplay integration is deliberately not started until the loader and
/// Harmony patch pipeline have been verified on the target Ports1de build.
/// </summary>
public sealed class ProtocolMod : Mod
{
    public ProtocolMod(ModContentPack content) : base(content)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var harmonyAssembly = typeof(Harmony).Assembly;
        var harmony = new Harmony("azartworld55.protocol.nechto");

        var patchProbePassed = ProtocolRuntimeProbe.Run(harmony, assembly);

        Log.Message(
            $"[ПРОТОКОЛ: НЕЧТО] Adapter constructor reached. " +
            $"Mod assembly: {assembly.GetName().Name} {assembly.GetName().Version?.ToString() ?? "unknown"}; " +
            $"Game API: {typeof(Mod).Assembly.GetName().Version?.ToString() ?? "unknown"}; " +
            $"Harmony: {harmonyAssembly.GetName().Version?.ToString() ?? "unknown"}; " +
            $".NET: {Environment.Version}; " +
            $"Harmony self-patch probe: {(patchProbePassed ? "PASS" : "FAIL")}.");
    }
}
