using System.Reflection;
using HarmonyLib;
using Verse;

namespace Protocol.RimWorld;

/// <summary>
/// Minimal game-facing entry point. It verifies that the mod, Verse API and
/// externally supplied Harmony assembly load before any gameplay patches are
/// introduced. No Harmony binary is bundled with this mod.
/// </summary>
public sealed class ProtocolMod : Mod
{
    public ProtocolMod(ModContentPack content) : base(content)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var harmonyAssembly = typeof(Harmony).Assembly;
        var harmony = new Harmony("azartworld55.protocol.nechto");
        harmony.PatchAll(assembly);

        Log.Message(
            $"[ПРОТОКОЛ: НЕЧТО] Adapter loaded. " +
            $"Game API: {typeof(Mod).Assembly.GetName().Version?.ToString() ?? "unknown"}; " +
            $"Harmony: {harmonyAssembly.GetName().Version?.ToString() ?? "unknown"}; " +
            $".NET: {Environment.Version}.");
    }
}
