using System.Reflection;
using System.Runtime.InteropServices;
using Verse;

namespace Ports1deProbe;

/// <summary>
/// A deliberately small entry point to test whether Ports1de loads a custom
/// Verse.Mod assembly. It does not reference Harmony or patch game methods.
/// </summary>
public sealed class ProbeMod : Mod
{
    public ProbeMod(ModContentPack content) : base(content)
    {
        var gameAssembly = typeof(Mod).Assembly;
        var probeAssembly = Assembly.GetExecutingAssembly();

        Log.Message("[Ports1deProbe] ENTRYPOINT_REACHED");
        Log.Message($"[Ports1deProbe] Mod package: {content.PackageId}");
        Log.Message($"[Ports1deProbe] Probe assembly: {probeAssembly.FullName}");
        Log.Message($"[Ports1deProbe] Verse assembly: {gameAssembly.FullName}");
        Log.Message($"[Ports1deProbe] Runtime: {RuntimeInformation.FrameworkDescription}");
        Log.Message($"[Ports1deProbe] Environment.Version: {Environment.Version}");
        Log.Message($"[Ports1deProbe] Target framework: {AppContext.TargetFrameworkName ?? "unknown"}");
        Log.Message($"[Ports1deProbe] Process architecture: {RuntimeInformation.ProcessArchitecture}");
        Log.Message($"[Ports1deProbe] OS architecture: {RuntimeInformation.OSArchitecture}");
    }
}
