using HarmonyLib;
using Verse;

namespace Protocol.RimWorld;

/// <summary>
/// Verifies that Harmony can patch and execute a harmless method in the
/// loaded mod assembly. This is a loader/runtime smoke test, not proof that
/// arbitrary RimWorld methods can be patched on Ports1de.
/// </summary>
internal static class ProtocolRuntimeProbe
{
    private const string OriginalValue = "protocol-probe-original";
    private const string PatchedValue = "protocol-probe-patched";

    internal static bool Run(Harmony harmony, System.Reflection.Assembly assembly)
    {
        try
        {
            harmony.PatchAll(assembly);

            var actual = Sentinel();
            if (actual != PatchedValue)
            {
                Log.Error(
                    $"[ПРОТОКОЛ: НЕЧТО] Harmony self-patch probe failed. " +
                    $"Expected '{PatchedValue}', received '{actual}'.");
                return false;
            }

            Log.Message(
                "[ПРОТОКОЛ: НЕЧТО] Harmony self-patch probe passed: " +
                "the postfix executed and changed the sentinel result.");
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(
                "[ПРОТОКОЛ: НЕЧТО] Runtime probe could not register or execute " +
                $"the Harmony self-patch: {exception}");
            return false;
        }
    }

    internal static string Sentinel() => OriginalValue;

    [HarmonyPatch(typeof(ProtocolRuntimeProbe), nameof(Sentinel))]
    private static class SentinelPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            __result = PatchedValue;
        }
    }
}
