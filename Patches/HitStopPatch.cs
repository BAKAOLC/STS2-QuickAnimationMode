using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2QuickAnimationMode.Patching.Models;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Patches NHitStop.SetTimeScale to preserve the speed multiplier.
    ///     When hit stop sets TimeScale to e.g. 0.1, we multiply by our speed
    ///     so the hit stop effect is proportional but the game stays fast.
    /// </summary>
    public class HitStopPatch : IPatchMethod
    {
        public static string PatchId => "hitstop_speed_preserve";
        public static string Description => "Preserve speed multiplier during hit stop effects";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NHitStop), "SetTimeScale", [typeof(float)]),
            ];
        }

        /// <summary>
        ///     Modify the timeScale parameter before it's applied to Engine.TimeScale.
        ///     This ensures hit stops still feel right at higher speeds.
        /// </summary>
        public static void Prefix(ref float timeScale)
        {
            timeScale *= SpeedManager.CurrentMultiplier;
        }
    }
}
