using MegaCrit.Sts2.Core.Nodes;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Patches the game's main process loop to update progressive speed transitions.
    /// </summary>
    public class ProgressiveSpeedPatch : IPatchMethod
    {
        public static string PatchId => "progressive_speed_process";
        public static string Description => "Process progressive speed transitions each frame";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NRun), "_Process", [typeof(double)]),
            ];
        }

        public static void Postfix(double delta)
        {
            SpeedManager.ProcessFrame(delta);
        }
    }
}
