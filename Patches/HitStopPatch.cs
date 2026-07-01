using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches;

/// <summary>
///     Patches NHitStop.SetTimeScale so hit stop becomes the base time scale
///     under this mod's multiplier instead of being overwritten on the next frame.
/// </summary>
public class HitStopPatch : IPatchMethod
{
    public static string PatchId => "hitstop_speed_preserve";
    public static string Description => "Preserve speed multiplier during hit stop effects";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NHitStop), "SetTimeScale", [typeof(float)])
        ];
    }

    public static void Prefix(ref float timeScale)
    {
        SpeedManager.ScaleExternalTimeScale(ref timeScale);
    }
}