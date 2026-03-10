using MegaCrit.Sts2.Core.Runs;
using STS2QuickAnimationMode.Patching.Models;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    public class RunCleanupPatch : IPatchMethod
    {
        public static string PatchId => "run_cleanup_speed_reset";
        public static string Description => "Reset speed when run is cleaned up";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(RunManager), nameof(RunManager.CleanUp), [typeof(bool)])];
        }

        public static void Prefix()
        {
            SpeedManager.ResetSpeed();
        }
    }
}
